using Godot;


public partial class NetworkManager : Node
{
	public static NetworkManager Instance { get; private set; }
	public const int Port = 7777;
	private const int DiscoveryPort = 7778;

	private const string RoomCharacters =
		"ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

	private PacketPeerUdp discoverySocket;

	private bool discoveryRunning = false;
	private string searchingRoomCode = "";

	public bool IsServer { get; private set; }

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		ProcessMode = ProcessModeEnum.Always;
		
		//ConnectedPlayers = new Dictionary<long, string>();
		
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
	}

	public override void _Process(double delta)
	{
		if (!discoveryRunning || discoverySocket == null)
			return;

		while (discoverySocket.GetAvailablePacketCount() > 0)
		{
			byte[] packet = discoverySocket.GetPacket();

			string message = packet.GetStringFromUtf8();

			string senderIp = discoverySocket.GetPacketIP();
			int senderPort = discoverySocket.GetPacketPort();

			GD.Print(
				$"Discovery packet from {senderIp}:{senderPort}: {message}"
			);

			HandleDiscoveryMessage(
				message,
				senderIp,
				senderPort
			);
		}
	}

	public void HostGame()
	{
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();

		Error error = peer.CreateServer(Port);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to create server: {error}");
			return;
		}

		Multiplayer.MultiplayerPeer = peer;

		IsServer = true;

		LobbyManager.Instance.AddPlayer(
			Multiplayer.GetUniqueId(),
			"Host",
			true
		);

		string roomCode = GenerateRoomCode();

		GameSession.Instance.RoomCode = roomCode;
		GameSession.Instance.HostPort = Port;

		StartHostDiscovery();

		GD.Print("================================");
		GD.Print("SERVER CREATED");
		GD.Print($"Room Code: {roomCode}");
		GD.Print($"Port: {Port}");
		GD.Print($"Host Peer ID: {Multiplayer.GetUniqueId()}");
		GD.Print("================================");
	}

	private void StartHostDiscovery()
	{
		discoverySocket = new PacketPeerUdp();

		Error error = discoverySocket.Bind(
			DiscoveryPort,
            "0.0.0.0"
		);

		if (error != Error.Ok)
		{
			GD.PrintErr(
				$"Failed to bind discovery port: {error}"
			);

			return;
		}

		discoverySocket.SetBroadcastEnabled(true);

		discoveryRunning = true;

		GD.Print(
			$"LAN discovery started on port {DiscoveryPort}"
		);
	}

	public void JoinGame(string roomCode)
	{
		searchingRoomCode = roomCode.Trim().ToUpper();

		discoverySocket = new PacketPeerUdp();

		Error error = discoverySocket.Bind(
			0,
            "0.0.0.0"
		);

		if (error != Error.Ok)
		{
			GD.PrintErr(
				$"Failed to start discovery listener: {error}"
			);

			return;
		}

		discoverySocket.SetBroadcastEnabled(true);

		discoveryRunning = true;

		GD.Print(
			$"Searching LAN for room: {searchingRoomCode}"
		);

		SendRoomSearch();
	}

	private void SendRoomSearch()
	{
		if (discoverySocket == null)
			return;

		string message =
			$"SEARCH|{searchingRoomCode}";

		byte[] data =
			message.ToUtf8Buffer();

		discoverySocket.SetDestAddress(
			"255.255.255.255",
			DiscoveryPort
		);

		discoverySocket.PutPacket(data);

		GD.Print(
			$"Searching for room {searchingRoomCode}"
		);
	}

	private void HandleDiscoveryMessage(
		string message,
		string senderIp,
		int senderPort
	)
	{
		string[] parts = message.Split('|');

		if (parts.Length < 2)
			return;

		string messageType = parts[0];

		if (messageType == "SEARCH")
		{
			HandleRoomSearch(parts, senderIp, senderPort);
		}
		else if (messageType == "ROOM")
		{
			HandleRoomResponse(parts, senderIp);
		}
	}

	private void HandleRoomSearch(
		string[] parts,
		string senderIp,
		int senderPort
	)
	{
		if (!IsServer)
			return;

		if (parts.Length < 2)
			return;

		string requestedRoomCode = parts[1];

		if (requestedRoomCode != GameSession.Instance.RoomCode)
			return;

		string response =
			$"ROOM|{GameSession.Instance.RoomCode}|{Port}";

		byte[] data =
			response.ToUtf8Buffer();

		// Send directly back to the device that searched.
		discoverySocket.SetDestAddress(
			senderIp,
			senderPort
		);

		discoverySocket.PutPacket(data);

		GD.Print(
			$"Room {requestedRoomCode} discovered a joiner."
		);

		GD.Print(
			$"Sent room response to {senderIp}:{senderPort}"
		);
	}

	private void HandleRoomResponse(
		string[] parts,
		string senderIp
	)
	{
		if (IsServer)
			return;

		if (parts.Length < 3)
			return;

		string roomCode = parts[1];

		if (roomCode != searchingRoomCode)
			return;

		if (!int.TryParse(parts[2], out int port))
		{
			GD.PrintErr("Invalid port received from host.");
			return;
		}

		string hostAddress = senderIp;

		GD.Print(
			$"Found room {roomCode} at {hostAddress}:{port}"
		);

		discoveryRunning = false;

		discoverySocket.Close();
		discoverySocket = null;

		ConnectToHost(hostAddress, port);
	}

	private void ConnectToHost(
		string address,
		int port
	)
	{
		ENetMultiplayerPeer peer =
			new ENetMultiplayerPeer();

		Error error =
			peer.CreateClient(address, port);

		if (error != Error.Ok)
		{
			GD.PrintErr(
				$"Failed to create client: {error}"
			);

			return;
		}

		Multiplayer.MultiplayerPeer = peer;

		IsServer = false;

		GameSession.Instance.HostAddress = address;
		GameSession.Instance.HostPort = port;

		GD.Print(
			$"Connecting to host at {address}:{port}"
		);
	}

	private string GenerateRoomCode()
	{
		RandomNumberGenerator random =
			new RandomNumberGenerator();

		string code = "";

		for (int i = 0; i < 6; i++)
		{
			int index = random.RandiRange(
				0,
				RoomCharacters.Length - 1
			);

			code += RoomCharacters[index];
		}

		return code;
	}

	public void Disconnect()
	{
		if (Multiplayer.MultiplayerPeer != null)
		{
			Multiplayer.MultiplayerPeer.Close();

			Multiplayer.MultiplayerPeer = null;
		}

		if (discoverySocket != null)
		{
			discoverySocket.Close();
			discoverySocket = null;
		}

		discoveryRunning = false;
		IsServer = false;

		GD.Print("Disconnected.");
	}

	private void OnPeerConnected(long peerId)
	{
		GD.Print($"Peer connected: {peerId}");

		if (!IsServer)
			return;

		LobbyManager.Instance.AddPlayer(
			peerId,
			$"Player {peerId}",
			false
		);

		SyncLobbyToAllPlayers();
	}

	

	private void OnPeerDisconnected(long peerId)
	{
		GD.Print($"Peer disconnected: {peerId}");

		if (!IsServer)
			return;

		LobbyManager.Instance.RemovePlayer(peerId);
		
		SyncLobbyToAllPlayers();
	}

	private void OnConnectedToServer()
	{
		GD.Print("Connected to server.");

		GD.Print(
			$"My peer ID: {Multiplayer.GetUniqueId()}"
		);

		GameSession.Instance.SetState(
			GameState.WaitingRoom
		);

		GetTree().ChangeSceneToFile(
			"res://Scenes/Lobby/WaitingRoom.tscn"
		);
	}

	private void OnConnectionFailed()
	{
		GD.PrintErr("Connection to server failed.");
	}

	private void OnServerDisconnected()
	{
		GD.Print("Disconnected from server.");
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReceivePlayerList(string[] players)
	{
		GD.Print("Received player list:");

		foreach (string player in players)
		{
			GD.Print(player);
		}

		GameSession.Instance.UpdatePlayerList(players);
	}
	private void SyncLobbyToAllPlayers()
	{
		if (!IsServer)
			return;

		var snapshot =
			LobbyManager.Instance.CreateNetworkSnapshot();

		Rpc(
			nameof(ReceiveLobbySnapshot),
			snapshot
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReceiveLobbySnapshot(
		Godot.Collections.Array<Godot.Collections.Dictionary> snapshot
	)
	{
		LobbyManager.Instance.ApplyNetworkSnapshot(snapshot);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	
	public void SetLocalReady(bool ready)
	{
		if (Multiplayer.MultiplayerPeer == null)
			return;

		// Host is already the server.
		if (IsServer)
		{
			long hostPeerId = Multiplayer.GetUniqueId();

			NetworkPlayer host =
				LobbyManager.Instance.GetPlayer(hostPeerId);

			if (host == null)
				return;

			host.SetReady(ready);

			GD.Print(
				$"Player {hostPeerId} ready: {ready}"
			);

			SyncLobbyToAllPlayers();

			return;
		}

		// Client asks the server to change its ready state.
		Rpc(
			nameof(RequestReadyChangeRpc),
			ready
		);

		GD.Print(
			$"Sent ready request: {ready}"
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void RequestReadyChangeRpc(bool ready)
	{
		if (!IsServer)
			return;

		long peerId = Multiplayer.GetRemoteSenderId();

		NetworkPlayer player =
			LobbyManager.Instance.GetPlayer(peerId);

		if (player == null)
			return;

		player.SetReady(ready);

		GD.Print(
			$"Player {peerId} ready: {ready}"
		);

		SyncLobbyToAllPlayers();
	}
	
	public void RequestStartGame()
	{
		if (!IsServer)
			return;

		if (!LobbyManager.Instance.AreAllPlayersReady())
		{
			GD.Print(
				"Cannot start: not all players are ready."
			);

			return;
		}

		StartGameForEveryone();
	}
	
	private void StartGameForEveryone()
	{
		GD.Print("SERVER: Starting game for everyone.");

		GameSession.Instance.SetState(
			GameState.Playing
		);

		Rpc(
			nameof(StartGameRpc)
		);

		GetTree().ChangeSceneToFile(
			"res://Scenes/Game/game.tscn"
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void StartGameRpc()
	{
		GD.Print("GAME START received from server.");

		GameSession.Instance.SetState(
			GameState.Playing
		);

		GetTree().ChangeSceneToFile(
			"res://Scenes/Game/game.tscn"
		);
	}
	
	public void SendPlayerPosition(Vector2 position)
	{	
	
		if (IsServer)
		{
			long peerId = Multiplayer.GetUniqueId();

			BroadcastPlayerPosition(
				peerId,
				position
			);

			return;
		}

		Rpc(
			nameof(ReceivePlayerPositionRpc),
			position
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable
	)]
	private void ReceivePlayerPositionRpc(Vector2 position)
	{
		if (!IsServer)
			return;

		long peerId =
			Multiplayer.GetRemoteSenderId();
		
		
		BroadcastPlayerPosition(
			peerId,
			position
		);
	}
	
	private void BroadcastPlayerPosition(
		long peerId,
		Vector2 position
	)
	{
		
		Rpc(
			nameof(UpdatePlayerPositionRpc),
			peerId,
			position
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable
	)]
	private void UpdatePlayerPositionRpc(
		long peerId,
		Vector2 position
	)
	{

		if (PlayerManager.Instance == null)
		{
			GD.PrintErr(
	            "PlayerManager.Instance is NULL"
			);

			return;
		}

		Player player =
			PlayerManager.Instance.GetPlayer(peerId);

		if (player == null)
		{
			GD.PrintErr(
				$"NO PLAYER FOUND FOR PEER {peerId}"
			);

			return;
		}


		if (player.IsMultiplayerAuthority())
		{
			return;
		}

		player.GlobalPosition = position;
	}
}
