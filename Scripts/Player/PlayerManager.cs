using Godot;
using System.Collections.Generic;

public partial class PlayerManager : Node2D
{
	public static PlayerManager Instance { get; private set; }

	private const string PlayerScenePath =
		"res://Scenes/Player/Player.tscn";

	private readonly Dictionary<long, Player> players =
		new Dictionary<long, Player>();

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		SpawnAllPlayers();
	}

	private void SpawnAllPlayers()
	{
		foreach (NetworkPlayer networkPlayer
			in LobbyManager.Instance.GetPlayers())
		{
			SpawnPlayer(networkPlayer);
		}
	}

	private void SpawnPlayer(NetworkPlayer networkPlayer)
	{
		if (players.ContainsKey(networkPlayer.PeerId))
			return;

		PackedScene playerScene =
			GD.Load<PackedScene>(PlayerScenePath);

		if (playerScene == null)
		{
			GD.PrintErr(
                "Could not load Player.tscn."
			);

			return;
		}

		Player player =
			playerScene.Instantiate<Player>();

		player.Name =
			$"Player_{networkPlayer.PeerId}";

		AddChild(player);

		player.SetMultiplayerAuthority(
			(int)networkPlayer.PeerId
		);

		player.Position =
			GetSpawnPosition(players.Count);

		players.Add(
			networkPlayer.PeerId,
			player
		);

		GD.Print(
			$"Spawned {networkPlayer.PlayerName} " +
			$"with peer ID {networkPlayer.PeerId}"
		);
	}

	private Vector2 GetSpawnPosition(int index)
	{
		Vector2[] spawnPositions =
		{
			new Vector2(3270, 625),
			new Vector2(3410, 625),
			new Vector2(3340, 675),
			new Vector2(3270, 725),
			new Vector2(3410, 725)
		};

		if (index < spawnPositions.Length)
			return spawnPositions[index];

		return new Vector2(3340, 675);
	}
	
	public Player GetPlayer(long peerId)
	{
		if (players.TryGetValue(peerId, out Player player))
			return player;

		return null;
	}
}
