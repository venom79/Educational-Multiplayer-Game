using Godot;
using System.Collections.Generic;

public partial class LobbyManager : Node
{
	public static LobbyManager Instance { get; private set; }

	private readonly Dictionary<long, NetworkPlayer> players =
		new Dictionary<long, NetworkPlayer>();

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		ProcessMode = ProcessModeEnum.Always;
	}

	public void AddPlayer(
		long peerId,
		string playerName,
		bool isHost
	)
	{
		if (players.ContainsKey(peerId))
			return;

		NetworkPlayer player = new NetworkPlayer(
			peerId,
			playerName,
			isHost
		);

		players.Add(peerId, player);

		GD.Print(
			$"Player added: {playerName} ({peerId})"
		);
	}

	public void RemovePlayer(long peerId)
	{
		if (!players.ContainsKey(peerId))
			return;

		NetworkPlayer player = players[peerId];

		players.Remove(peerId);

		GD.Print(
			$"Player removed: {player.PlayerName} ({peerId})"
		);
	}

	public NetworkPlayer GetPlayer(long peerId)
	{
		if (players.TryGetValue(peerId, out NetworkPlayer player))
			return player;

		return null;
	}

	public IReadOnlyCollection<NetworkPlayer> GetPlayers()
	{
		return players.Values;
	}

	public int GetPlayerCount()
	{
		return players.Count;
	}
	
	public LobbyPlayerData[] CreatePlayerSnapshot()
	{
		LobbyPlayerData[] snapshot =
			new LobbyPlayerData[players.Count];

		int index = 0;

		foreach (NetworkPlayer player in players.Values)
		{
			snapshot[index] = new LobbyPlayerData(
				player.PeerId,
				player.PlayerName,
				player.IsHost,
				player.IsReady
			);

			index++;
		}

		return snapshot;
	}
	
	public Godot.Collections.Array<Godot.Collections.Dictionary>
		CreateNetworkSnapshot()
	{
		var snapshot =
			new Godot.Collections.Array<Godot.Collections.Dictionary>();

		foreach (NetworkPlayer player in players.Values)
		{
			var data =
				new Godot.Collections.Dictionary();

			data["peer_id"] = player.PeerId;
			data["name"] = player.PlayerName;
			data["is_host"] = player.IsHost;
			data["is_ready"] = player.IsReady;

			snapshot.Add(data);
		}

		return snapshot;
	}
	
	public void ApplyNetworkSnapshot(
		Godot.Collections.Array<Godot.Collections.Dictionary> snapshot
	)
	{
		players.Clear();

		foreach (Godot.Collections.Dictionary data in snapshot)
		{
			long peerId = (long)data["peer_id"];
			string playerName = (string)data["name"];
			bool isHost = (bool)data["is_host"];

			NetworkPlayer player = new NetworkPlayer(
				peerId,
				playerName,
				isHost
			);

			player.SetReady(
				(bool)data["is_ready"]
			);

			players.Add(peerId, player);
			GD.Print(
				$"Player {playerName} | Ready: {player.IsReady}"
			);
		}

		GD.Print(
			$"Lobby snapshot applied. Players: {players.Count}"
		);
	}
	
	public NetworkPlayer GetLocalPlayer()
	{
		long localPeerId = Multiplayer.GetUniqueId();

		return GetPlayer(localPeerId);
	}
	
	public bool AreAllPlayersReady()
	{
		if (players.Count == 0)
			return false;

		foreach (NetworkPlayer player in players.Values)
		{
			if (!player.IsReady)
				return false;
		}

		return true;
	}
}
