using Godot;
using System.Collections.Generic;
using System;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	private const double GameDuration = 300.0;

	private GameTimer gameTimer;
	private double synchronizedTimeRemaining = GameDuration;
	
	private readonly HashSet<long> loadedPlayers =
		new HashSet<long>();

	public bool GameStarted { get; private set; }

	public double TimeRemaining
	{
		get
		{
			if (NetworkManager.Instance != null &&
				NetworkManager.Instance.IsServer)
			{
				return gameTimer?.RemainingSeconds ?? GameDuration;
			}

			return synchronizedTimeRemaining;
		}
	}
	
	private int lastBroadcastSecond = -1;
	
	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		ProcessMode = ProcessModeEnum.Always;

		gameTimer = new GameTimer(GameDuration);

		gameTimer.TimeChanged += OnTimeChanged;
		gameTimer.TimerFinished += OnTimerFinished;

		GD.Print("GameManager initialized.");
	}

	public override void _Process(double delta)
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		if (!GameStarted)
			return;

		gameTimer.Update(delta);
	}

	public void MarkLocalGameLoaded()
	{
		long localPeerId =
			Multiplayer.GetUniqueId();

		GD.Print(
			$"Game scene loaded for peer {localPeerId}"
		);

		if (NetworkManager.Instance == null)
		{
			GD.PrintErr(
				"NetworkManager instance is NULL!"
			);

			return;
		}

		if (NetworkManager.Instance.IsServer)
		{
			GD.Print(
				"Local player is the SERVER."
			);

			MarkPlayerLoaded(localPeerId);
		}
		else
		{
			GD.Print(
				"Local player is a CLIENT. " +
				"Reporting to server..."
			);

			Rpc(
				nameof(ReportGameLoadedRpc)
			);
		}
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReportGameLoadedRpc()
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		long peerId =
			Multiplayer.GetRemoteSenderId();

		MarkPlayerLoaded(peerId);
	}

	private void MarkPlayerLoaded(long peerId)
	{
		loadedPlayers.Add(peerId);

		GD.Print(
			$"Game loaded: {peerId} " +
			$"({loadedPlayers.Count}/" +
			$"{LobbyManager.Instance.GetPlayerCount()})"
		);

		if (AllPlayersLoaded())
		{
			StartGameTimer();
		}
	}

	private bool AllPlayersLoaded()
	{
		int playerCount =
			LobbyManager.Instance.GetPlayerCount();

		return playerCount > 0 &&
			loadedPlayers.Count >= playerCount;
	}

	private void StartGameTimer()
	{
		if (GameStarted)
			return;

		GameStarted = true;

		GD.Print(
			"================================"
		);

		GD.Print(
			"ALL PLAYERS LOADED"
		);

		GD.Print(
			"GAME TIMER STARTED"
		);

		GD.Print(
			$"Duration: {GameDuration} seconds"
		);

		GD.Print(
			"================================"
		);

		gameTimer.Start();

		Rpc(
			nameof(StartGameTimerRpc)
		);
	}

	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void StartGameTimerRpc()
	{
		GD.Print(
			"Server started the game timer."
		);
	}

	private void OnTimeChanged(double remaining)
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		int currentSecond =
			(int)Math.Ceiling(remaining);

		if (currentSecond == lastBroadcastSecond)
			return;

		lastBroadcastSecond = currentSecond;

		Rpc(
			nameof(SyncTimerRpc),
			remaining
		);
	}

	private void OnTimerFinished()
	{
		GD.Print(
			"GAME TIMER FINISHED."
		);

		// Later:
		// GameState = AliensWin
		// Check task completion
		// Broadcast result
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void SyncTimerRpc(double remaining)
	{
		synchronizedTimeRemaining = remaining;

		GD.Print(
			$"Time remaining: {remaining:F0}"
		);
	}
}
