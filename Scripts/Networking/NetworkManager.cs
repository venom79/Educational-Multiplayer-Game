using Godot;
using System.Collections.Generic;	

public partial class NetworkManager : Node
{
	public static NetworkManager Instance { get; private set; }
	public const int Port = 7777;
	private const int DiscoveryPort = 7778;
	
	private bool isStartingGame = false;
	private const string RoomCharacters =
		"ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

	private PacketPeerUdp discoverySocket;

	private bool discoveryRunning = false;
	private string searchingRoomCode = "";

	public bool IsServer { get; private set; }
	public bool RoomClosed { get; private set; }
	
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
		PacketPeerUdp socket = discoverySocket;

		if (!discoveryRunning || socket == null)
			return;

		while (discoveryRunning &&
			   socket != null &&
			   socket == discoverySocket &&
			   socket.GetAvailablePacketCount() > 0)
		{
			byte[] packet = socket.GetPacket();

			string message = packet.GetStringFromUtf8();
			string senderIp = socket.GetPacketIP();
			int senderPort = socket.GetPacketPort();

			GD.Print(
				$"Discovery packet from {senderIp}:{senderPort}: {message}"
			);

			HandleDiscoveryMessage(message, senderIp, senderPort);
		}
	}

	public void HostGame()
	{
		LobbyManager.Instance.ClearPlayers();
		
		ENetMultiplayerPeer peer = new ENetMultiplayerPeer();

		Error error = peer.CreateServer(Port);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to create server: {error}");
			return;
		}

		Multiplayer.MultiplayerPeer = peer;

		IsServer = true;
		RoomClosed = false;
		
		LobbyManager.Instance.AddPlayer(
			Multiplayer.GetUniqueId(),
			GameSession.Instance.PlayerName,
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
	
		if (RoomClosed)
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
		GD.PrintErr("DEBUG: NetworkManager.Disconnect() was called!");
		GD.PrintErr(System.Environment.StackTrace);
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
		RoomClosed = false;

		LobbyManager.Instance.ClearPlayers();

		GameSession.Instance.RoomCode = "";
		GameSession.Instance.HostAddress = "";

		GD.Print("Disconnected and local lobby state cleared.");
	}

	private void OnPeerConnected(long peerId)
	{
		GD.Print($"Peer connected: {peerId}");

		if (!IsServer)
			return;
		
		if (RoomClosed)
		{
			GD.Print(
				$"Rejected peer {peerId}: room is already closed."
			);
			GD.PrintErr(
				$"DEBUG: Rejecting peer {peerId} because RoomClosed is true."
			);
			Multiplayer.MultiplayerPeer.DisconnectPeer(
				(int)peerId
			);

			return;
		}
		
		LobbyManager.Instance.AddPlayer(
			peerId,
			$"Player {peerId}",
			false
		);

		SyncLobbyToAllPlayers();
		SyncGameModeToAllPlayers();
	}

	

	private void OnPeerDisconnected(long peerId)
	{
		GD.Print($"Peer disconnected: {peerId}");
		GD.PrintErr(
			$"DEBUG: IsServer={IsServer}, " +
			$"ConnectionStatus={Multiplayer.MultiplayerPeer.GetConnectionStatus()}"
		);

		if (Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enetPeer)
		{
			GD.PrintErr(
				$"DEBUG: ENet connection status: {enetPeer.GetConnectionStatus()}"
			);
		}
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
	
		Rpc(
			nameof(RequestPlayerNameRpc),
			GameSession.Instance.PlayerName
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
	
		
	private void SendTaskAssignmentsToPlayer(
		long playerId,
		List<NetworkTaskData> tasks
	)
	{
		if (!IsServer)
			return;

		Godot.Collections.Array<Godot.Collections.Dictionary> networkTasks =
			new Godot.Collections.Array<Godot.Collections.Dictionary>();

		foreach (NetworkTaskData task in tasks)
		{
			Godot.Collections.Array options =
				new Godot.Collections.Array();

			Godot.Collections.Array leftItems =
				new Godot.Collections.Array();

			Godot.Collections.Array rightItems =
				new Godot.Collections.Array();

			if (task.Options != null)
			{
				foreach (string option in task.Options)
					options.Add(option);
			}

			if (task.LeftItems != null)
			{
				foreach (string item in task.LeftItems)
					leftItems.Add(item);
			}

			if (task.RightItems != null)
			{
				foreach (string item in task.RightItems)
					rightItems.Add(item);
			}

			Godot.Collections.Dictionary taskData =
				new Godot.Collections.Dictionary
				{
					{ "task_id", task.TaskId },
					{ "station_id", task.StationId },
					{ "task_type", (int)task.TaskType },
					{ "question", task.Question },
					{ "options", options },
					{ "left_items", leftItems },
					{ "right_items", rightItems }
				};

			networkTasks.Add(taskData);
		}

		RpcId(
			playerId,
			nameof(ReceiveTaskAssignmentsRpc),
			networkTasks
		);

		GD.Print(
			$"Sent {tasks.Count} tasks to player {playerId}."
		);
	}

	
	
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReceiveTaskAssignmentsRpc(
		Godot.Collections.Array<Godot.Collections.Dictionary> networkTasks
	)
	{
		long localPlayerId = Multiplayer.GetUniqueId();

		List<NetworkTaskData> tasks =
			new List<NetworkTaskData>();

		foreach (Godot.Collections.Dictionary data in networkTasks)
		{
			int taskId = (int)data["task_id"];
			string stationId = (string)data["station_id"];
			TaskType taskType = (TaskType)(int)data["task_type"];
			string question = (string)data["question"];

			// Read multiple-choice options.
			string[] options = ReadStringArray(data, "options");

			// Read matching task items.
			string[] leftItems = ReadStringArray(data, "left_items");
			string[] rightItems = ReadStringArray(data, "right_items");

			tasks.Add(
				new NetworkTaskData(
					taskId,
					stationId,
					taskType,
					question,
					options,
					leftItems,
					rightItems
				)
			);
		}

		TaskManager.Instance.ApplyNetworkTasks(
			localPlayerId,
			tasks
		);

		GD.Print(
			$"Received {tasks.Count} task assignments from server."
		);
	}

	private static string[] ReadStringArray(
		Godot.Collections.Dictionary data,
		string key
	)
	{
		if (!data.ContainsKey(key))
			return System.Array.Empty<string>();

		Godot.Collections.Array receivedItems =
			data[key].AsGodotArray();

		string[] items = new string[receivedItems.Count];

		for (int i = 0; i < receivedItems.Count; i++)
			items[i] = receivedItems[i].AsString();

		return items;
	}

	
	public void SubmitTaskAnswer(
		int taskId,
		string answer
	)
	{
		if (IsServer)
		{
			GD.Print(
				"Server cannot submit a network answer " +
				"through the client RPC."
			);

			return;
		}

		Rpc(
			nameof(RequestTaskAnswerRpc),
			taskId,
			answer
		);

		GD.Print(
			$"Submitted task answer: Task {taskId}"
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void RequestTaskAnswerRpc(
		int taskId,
		string answer
	)
	{
		if (!IsServer)
			return;

		long playerId =
			Multiplayer.GetRemoteSenderId();

		GD.Print(
			$"Task answer received from {playerId}: " +
			$"Task {taskId}"
		);

		GameTask task =
			TaskManager.Instance.GetTask(
				playerId,
				taskId
			);

		if (task == null)
		{
			GD.PrintErr(
				$"Task {taskId} not found for player {playerId}."
			);

			RpcId(
				playerId,
				nameof(ReceiveTaskAnswerResultRpc),
				taskId,
				false
			);

			return;
		}

		if (task.IsCompleted)
		{
			GD.Print(
				$"Task {taskId} is already completed."
			);

			RpcId(
				playerId,
				nameof(ReceiveTaskAnswerResultRpc),
				taskId,
				false
			);

			return;
		}

		bool correct =
			task.SubmitAnswer(answer);
		
		if (correct)
		{
			GameManager.Instance.RegisterTaskCompletion(
				playerId
			);
		}

		GD.Print(
			$"Task {taskId} answer result: {correct}"
		);

		RpcId(
			playerId,
			nameof(ReceiveTaskAnswerResultRpc),
			taskId,
			correct
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReceiveTaskAnswerResultRpc(
		int taskId,
		bool correct
	)
	{
		GD.Print(
			$"Task {taskId} result received: {correct}"
		);

		if (TaskUIController.Instance != null)
		{
			TaskUIController.Instance
				.HandleNetworkAnswerResult(
					taskId,
					correct
				);
		}
	}
	
	private void SyncGameModeToAllPlayers()
	{
		if (!IsServer)
			return;

		int mode = (int)GameSession.Instance.SelectedMode;

		Rpc(
			nameof(ReceiveGameModeRpc),
			mode
		);
	}
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReceiveGameModeRpc(int mode)
	{
		GameMode selectedMode = (GameMode)mode;

		GameSession.Instance.SetGameMode(selectedMode);

		GD.Print(
			$"Game mode synchronized: {selectedMode}"
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
		if (!IsServer || RoomClosed || isStartingGame)
			return;

		if (!LobbyManager.Instance.AreAllPlayersReady())
		{
			GD.Print("Cannot start: not all players are ready.");
			return;
		}

		_ = StartGameForEveryoneAsync();
	}

	private async System.Threading.Tasks.Task StartGameForEveryoneAsync()
	{
		if (!IsServer || isStartingGame || RoomClosed)
			return;

		isStartingGame = true;

		try
		{
			var players = LobbyManager.Instance.GetPlayers();

			if (players.Count == 0 || players.Count > 8)
			{
				GD.PrintErr(
					$"Invalid player count: {players.Count}. Expected 1–8."
				);
				return;
			}

			if (!LobbyManager.Instance.AreAllPlayersReady())
			{
				GD.PrintErr("Players are no longer all ready.");
				return;
			}
			
			// Show the loading screen on both machines immediately.
			GameSession.Instance.SetState(GameState.Loading);

			Rpc(nameof(ShowLoadingScreenRpc));
			ShowLoadingScreen();

			// Ensure the loading screen gets a chance to render
			// before task generation begins.
			await ToSignal(
				RenderingServer.Singleton,
				RenderingServer.SignalName.FramePostDraw
			);
			
			GameMode mode = GameSession.Instance.SelectedMode;
			int requiredTasks = players.Count * TaskManager.TasksPerPlayer;

			GD.Print("================================");
			GD.Print("SERVER: GENERATING TASKS");
			GD.Print($"Mode: {mode}");
			GD.Print($"Players: {players.Count}");
			GD.Print($"Required tasks: {requiredTasks}");
			GD.Print("================================");

			await TaskPoolManager.Instance.PreparePoolAsync(
				mode,
				requiredTasks
			);

			// Recheck the lobby after waiting for the AI response.
			players = LobbyManager.Instance.GetPlayers();

			if (players.Count == 0 ||
				players.Count > 8 ||
				!LobbyManager.Instance.AreAllPlayersReady() ||
				TaskPoolManager.Instance.GetAvailableTaskCount()
					!= players.Count * TaskManager.TasksPerPlayer)
			{
				GD.PrintErr(
					"Lobby or task pool changed while generating tasks. " +
	                "Returning to the waiting room."
				);
				return;
			}

			// Assign four tasks to each player.
			foreach (NetworkPlayer player in players)
			{
				TaskManager.Instance.AssignTasksFromPool(player.PeerId);

				if (TaskManager.Instance.GetTotalTaskCount(player.PeerId)
					!= TaskManager.TasksPerPlayer)
				{
					throw new System.InvalidOperationException(
						$"Could not assign four tasks to player {player.PeerId}."
					);
				}
			}

			// Send only display-safe task data to each client.
			foreach (NetworkPlayer player in players)
			{
				List<NetworkTaskData> networkTasks =
					TaskManager.Instance.CreateNetworkTaskData(player.PeerId);

				if (player.PeerId == Multiplayer.GetUniqueId())
				{
					TaskManager.Instance.ApplyNetworkTasks(
						player.PeerId,
						networkTasks
					);
				}
				else
				{
					SendTaskAssignmentsToPlayer(
						player.PeerId,
						networkTasks
					);
				}
			}

			GD.Print("================================");
			GD.Print("TASK ASSIGNMENT COMPLETE");
			GD.Print("================================");

			RoomClosed = true;
			StopHostDiscovery();

			GD.Print($"[TIMING] Host entering game scene: {Time.GetTicksMsec()}");

			Rpc(nameof(StartGameRpc));

			ChangeToPreloadedGameScene();
		}
		catch (System.Exception exception)
		{
			GD.PrintErr(
				$"Failed to prepare the game: {exception}"
			);
		}
		finally
		{
			isStartingGame = false;
		}
	}

	
	private void StopHostDiscovery()
	{
		if (!discoveryRunning)
			return;

		discoveryRunning = false;

		if (discoverySocket != null)
		{
			discoverySocket.Close();
			discoverySocket = null;
		}

		GD.Print("Room discovery stopped. Room is closed.");
	}
	
	
	
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void StartGameRpc()
	{
		GD.Print("GAME START received. Preparing loading screen...");

		GameSession.Instance.SetState(GameState.Loading);

		ChangeToPreloadedGameScene();
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

		player.SetNetworkPosition(position);
	}
	
	
	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void RequestPlayerNameRpc(string requestedName)
	{
		if (!IsServer)
			return;

		long peerId = Multiplayer.GetRemoteSenderId();

		NetworkPlayer player =
			LobbyManager.Instance.GetPlayer(peerId);

		if (player == null)
			return;

		string cleanName = requestedName.Trim();

		if (string.IsNullOrWhiteSpace(cleanName))
			cleanName = $"Player {peerId}";

		if (cleanName.Length > 16)
			cleanName = cleanName[..16];

		player.SetPlayerName(cleanName);

		GD.Print($"Player {peerId} chose name: {cleanName}");

		SyncLobbyToAllPlayers();
	}

	
	public void RequestSwitchTeam()
	{
		if (Multiplayer.MultiplayerPeer == null)
			return;

		if (GameSession.Instance.CurrentState != GameState.WaitingRoom)
			return;

		if (IsServer)
		{
			SwitchPlayerTeam(Multiplayer.GetUniqueId());
			return;
		}

		Rpc(nameof(RequestSwitchTeamRpc));
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void RequestSwitchTeamRpc()
	{
		if (!IsServer)
			return;

		long peerId = Multiplayer.GetRemoteSenderId();

		SwitchPlayerTeam(peerId);
	}

	private void SwitchPlayerTeam(long peerId)
	{
		if (!IsServer)
			return;

		if (RoomClosed)
			return;

		NetworkPlayer player =
			LobbyManager.Instance.GetPlayer(peerId);

		if (player == null)
			return;

		PlayerTeam targetTeam =
			player.Team == PlayerTeam.Blue
				? PlayerTeam.Red
				: PlayerTeam.Blue;

		int targetTeamCount =
			System.Linq.Enumerable.Count(
				LobbyManager.Instance.GetPlayers(),
				p => p.Team == targetTeam
			);

		if (targetTeamCount >= 4)
		{
			GD.Print(
				$"Cannot switch {player.PlayerName}: Team {targetTeam} is full."
			);
			return;
		}

		player.SetTeam(targetTeam);

		// Switching teams requires the player to ready up again.
		player.SetReady(false);

		GD.Print(
			$"{player.PlayerName} switched to Team {targetTeam}"
		);

		SyncLobbyToAllPlayers();
	}
	
	
	
	private async void ChangeToPreloadedGameScene()
	{
		GD.Print("[SCENE] Waiting for game scene resources...");

		while (GameManager.Instance == null ||
			   !GameManager.Instance.IsGameSceneReady())
		{
			await ToSignal(
				GetTree(),
				SceneTree.SignalName.ProcessFrame
			);
		}

		PackedScene gameScene =
			GameManager.Instance.GetPreloadedGameScene();

		if (gameScene == null)
		{
			GD.PrintErr("[SCENE] Preloaded game scene is null!");
			return;
		}

		GD.Print("[SCENE] Switching from loading screen to game...");

		Error error = GetTree().ChangeSceneToPacked(gameScene);

		GD.Print($"[SCENE] Game scene change result: {error}");
	}
	
	private void ShowLoadingScreen()
	{
		GameSession.Instance.SetState(GameState.Loading);

		Error error = GetTree().ChangeSceneToFile(
	        "res://Scenes/Loading/LoadingScreen.tscn"
		);

		if (error != Error.Ok)
			GD.PrintErr($"Failed to show loading screen: {error}");
		else
			GD.Print("[SCENE] Loading screen displayed.");
	}

	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ShowLoadingScreenRpc()
	{
		ShowLoadingScreen();
	}
}
