using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }
	
	private const double GameDuration = 300.0;

	private GameTimer gameTimer;
	private double synchronizedTimeRemaining = GameDuration;
	
	private readonly HashSet<long> loadedPlayers =
		new HashSet<long>();
	
	private const int TasksPerPlayer = 6;
	private const int MaximumPlayers = 8;
	
	public bool GameStarted { get; private set; }
	public bool GameEnded { get; private set; }
	
	private const string GameScenePath =
		"res://Scenes/Game/game.tscn";
		
	private PackedScene preloadedGameScene;
	private bool gameSceneLoadRequested;
	private bool gameSceneLoadFailed;

	private const string ResultScenePath =
	"res://Scenes/Results/ResultScreen.tscn";
	
	
	private readonly Dictionary<long, int> completedTasks =
		new Dictionary<long, int>();

	
	private int blueCompletedTasks = 0;
	private int redCompletedTasks = 0;

	private int synchronizedBlueCompletedTasks = 0;
	private int synchronizedRedCompletedTasks = 0;

	public int BlueCompletedTasks =>
		NetworkManager.Instance.IsServer
			? blueCompletedTasks
			: synchronizedBlueCompletedTasks;

	public int RedCompletedTasks =>
		NetworkManager.Instance.IsServer
			? redCompletedTasks
			: synchronizedRedCompletedTasks;

	public int BlueTotalTasks =>
		LobbyManager.Instance.GetPlayers()
			.Count(p => p.Team == PlayerTeam.Blue)
			* TasksPerPlayer;

	public int RedTotalTasks =>
		LobbyManager.Instance.GetPlayers()
			.Count(p => p.Team == PlayerTeam.Red)
			* TasksPerPlayer;

	// Retain these properties for any existing UI that uses them.
	public int TeamCompletedTasks =>
		blueCompletedTasks + redCompletedTasks;

	
	public int TeamTotalTasks =>
		LobbyManager.Instance.GetPlayerCount() * TasksPerPlayer;
	
	private int synchronizedPlayerCompletedTasks = 0;
	private int synchronizedPlayerTotalTasks = 0;

	private int synchronizedTeamCompletedTasks = 0;
	private int synchronizedTeamTotalTasks = 0;

	public int PlayerCompletedTasks =>
		NetworkManager.Instance.IsServer
			? GetPlayerCompletedTasks(Multiplayer.GetUniqueId())
			: synchronizedPlayerCompletedTasks;

	public int PlayerTotalTasks =>
		NetworkManager.Instance.IsServer
			? TasksPerPlayer
			: synchronizedPlayerTotalTasks;

	public int SynchronizedTeamCompletedTasks =>
		NetworkManager.Instance.IsServer
			? blueCompletedTasks + redCompletedTasks
			: synchronizedTeamCompletedTasks;

	public int SynchronizedTeamTotalTasks =>
		NetworkManager.Instance.IsServer
			? TeamTotalTasks
			: synchronizedTeamTotalTasks;
			
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
		CallDeferred(nameof(BeginGameScenePreload));
		
		GD.Print("GameManager initialized.");
	}
	
	
	
	public override void _Process(double delta)
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		if (!GameStarted)
			return;
		
		if (GameEnded)
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
			GD.Print("CLIENT: Sending game-loaded RPC to server...");
			Rpc(nameof(ReportGameLoadedRpc));
			GD.Print("CLIENT: Game-loaded RPC call completed.");
	
		}
	}

	[Rpc(
		MultiplayerApi.RpcMode.AnyPeer,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReportGameLoadedRpc()
	{
		GD.Print(
			$"REPORT GAME LOADED RPC RECEIVED. " +
			$"IsServer={Multiplayer.IsServer()}, " +
			$"Sender={Multiplayer.GetRemoteSenderId()}"
		);
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
		
	private void InitializeTaskProgress()
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		completedTasks.Clear();

		foreach (NetworkPlayer player in LobbyManager.Instance.GetPlayers())
		{
			completedTasks[player.PeerId] = 0;
		}

		blueCompletedTasks = 0;
		redCompletedTasks = 0;

		BroadcastProgress();

		GD.Print("Task progress initialized.");
		GD.Print($"Blue: 0/{BlueTotalTasks}");
		GD.Print($"Red: 0/{RedTotalTasks}");
	}

	
	
	public void RegisterTaskCompletion(long playerId)
	{
		if (!NetworkManager.Instance.IsServer || GameEnded)
			return;

		if (!completedTasks.ContainsKey(playerId))
		{
			GD.PrintErr($"Unknown player: {playerId}");
			return;
		}

		NetworkPlayer player =
			LobbyManager.Instance.GetPlayer(playerId);

		if (player == null)
			return;

		completedTasks[playerId]++;

		if (player.Team == PlayerTeam.Blue)
			blueCompletedTasks++;
		else
			redCompletedTasks++;

		GD.Print(
			$"{player.PlayerName} completed a task for Team {player.Team}."
		);

		GD.Print(
			$"Blue: {blueCompletedTasks}/{BlueTotalTasks} | " +
			$"Red: {redCompletedTasks}/{RedTotalTasks}"
		);

		BroadcastProgress();

		// A team wins as soon as it completes all its required tasks.
		if (BlueTotalTasks > 0 &&
			blueCompletedTasks >= BlueTotalTasks)
		{
			EndGame(GameState.BlueVictory);
			return;
		}

		if (RedTotalTasks > 0 &&
			redCompletedTasks >= RedTotalTasks)
		{
			EndGame(GameState.RedVictory);
		}
	}

	
	public int GetPlayerCompletedTasks(long playerId)
	{
		if (completedTasks.TryGetValue(
			playerId,
			out int completed))
		{
			return completed;
		}

		return 0;
	}

	public int GetPlayerTotalTasks(long playerId)
	{
		if (!NetworkManager.Instance.IsServer)
		{
			return TaskManager.Instance
				.GetNetworkTasks(playerId)
				.Count;
		}

		return TaskManager.Instance
			.GetTotalTaskCount(playerId);
	}
	
	public int GetTeamCompletedTasks()
	{
		return blueCompletedTasks + redCompletedTasks;
	}

	public int GetTeamTotalTasks()
	{
		return TeamTotalTasks;
	}
	
	
	private void StartGameTimer()
	{
		if (!NetworkManager.Instance.IsServer || GameStarted)
			return;

		InitializeTaskProgress();

		GameEnded = false;
		GameStarted = true;

		GD.Print("================================");
		GD.Print("ALL PLAYERS LOADED");
		GD.Print("GAME START AUTHORIZED");
		GD.Print("================================");

		gameTimer.Start();

		// CallLocal ensures the host and clients use the same
		// authorization path.
		Rpc(nameof(StartGameTimerRpc));
	}

	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = true,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void StartGameTimerRpc()
	{
		GameEnded = false;
		GameStarted = true;

		GD.Print("GAME START AUTHORIZED: Gameplay enabled.");
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
		if (!NetworkManager.Instance.IsServer || GameEnded)
			return;

		GD.Print("GAME TIMER FINISHED.");

		if (blueCompletedTasks > redCompletedTasks)
		{
			EndGame(GameState.BlueVictory);
		}
		else if (redCompletedTasks > blueCompletedTasks)
		{
			EndGame(GameState.RedVictory);
		}
		else
		{
			EndGame(GameState.Draw);
		}
	}

	
	private void EndGame(GameState result)
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		if (GameEnded)
			return;

		GameEnded = true;

		gameTimer.Stop();

		GameSession.Instance.SetState(result);

		GD.Print("================================");
		GD.Print("GAME ENDED");
		GD.Print($"RESULT: {result}");
		GD.Print(
			$"FINAL TEAM PROGRESS → " +
			$"Blue: {blueCompletedTasks}/{BlueTotalTasks} | " +
			$"Red: {redCompletedTasks}/{RedTotalTasks}"
		);
		GD.Print("================================");

		Rpc(
			nameof(ReceiveGameResultRpc),
			(int)result
		);

		ChangeToResultScreen();
	}
	
	private void ChangeToResultScreen()
	{
		GD.Print(
			"Changing to Result Screen..."
		);

		GetTree().ChangeSceneToFile(
			ResultScenePath
		);
	}
	
	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReceiveGameResultRpc(int result)
	{
		GameState gameResult =
			(GameState)result;

		GameSession.Instance.SetState(gameResult);

		GD.Print(
			$"Game result received: {gameResult}"
		);

		ChangeToResultScreen();
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
	
	public void PrepareTaskPool()
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		GameMode mode =
			GameSession.Instance.SelectedMode;

		int requiredTasks =
			LobbyManager.Instance.GetPlayerCount()
			* TasksPerPlayer;

		GD.Print(
			$"Preparing task pool for {mode}."
		);

		GD.Print(
			$"Target pool size: {requiredTasks}"
		);

		TaskPoolManager.Instance.PreparePool(
			mode,
			requiredTasks
		);

		GD.Print(
			$"Task pool ready: " +
			$"{TaskPoolManager.Instance.GetAvailableTaskCount()} tasks."
		);
	}
	
	public void AssignTasksToAllPlayers()
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		GD.Print("================================");
		GD.Print("ASSIGNING TASKS TO PLAYERS");
		GD.Print("================================");

		foreach (
			NetworkPlayer player
			in LobbyManager.Instance.GetPlayers()
		)
		{
			TaskManager.Instance.AssignTasksFromPool(
				player.PeerId
			);

			GD.Print(
				$"Tasks assigned to {player.PlayerName} " +
				$"({player.PeerId})"
			);
		}

		GD.Print(
			$"Remaining pool tasks: " +
			$"{TaskPoolManager.Instance.GetAvailableTaskCount()}"
		);

		GD.Print("================================");
	}
	
	
	public void BroadcastProgress()
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		foreach (NetworkPlayer player in LobbyManager.Instance.GetPlayers())
		{
			RpcId(
				player.PeerId,
				nameof(ReceiveProgressRpc),
				GetPlayerCompletedTasks(player.PeerId),
				GetPlayerTotalTasks(player.PeerId),
				blueCompletedTasks,
				BlueTotalTasks,
				redCompletedTasks,
				RedTotalTasks
			);
		}
	}

	[Rpc(
		MultiplayerApi.RpcMode.Authority,
		CallLocal = false,
		TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
	)]
	private void ReceiveProgressRpc(
		int playerCompleted,
		int playerTotal,
		int blueCompleted,
		int blueTotal,
		int redCompleted,
		int redTotal
	)
	{
		synchronizedPlayerCompletedTasks = playerCompleted;
		synchronizedPlayerTotalTasks = playerTotal;

		synchronizedBlueCompletedTasks = blueCompleted;
		synchronizedRedCompletedTasks = redCompleted;

		synchronizedTeamCompletedTasks = blueCompleted + redCompleted;
		synchronizedTeamTotalTasks = blueTotal + redTotal;

		GD.Print(
			$"Progress → Player {playerCompleted}/{playerTotal} | " +
			$"Blue {blueCompleted}/{blueTotal} | " +
			$"Red {redCompleted}/{redTotal}"
		);
	}
	
	public Error PreloadGameScene()
	{
		if (preloadedGameScene != null ||
			gameSceneLoadRequested)
		{
			return Error.Ok;
		}

		GD.Print("[PRELOAD] Starting background load...");

		Error error = ResourceLoader.LoadThreadedRequest(
			GameScenePath,
			"PackedScene",
			true
		);

		if (error != Error.Ok)
		{
			GD.PrintErr(
				$"[PRELOAD] Failed to start: {error}"
			);
			return error;
		}

		gameSceneLoadRequested = true;
		return Error.Ok;
	}
	
	private void BeginGameScenePreload()
	{
		PreloadGameScene();
	}
	
	public bool IsGameSceneReady()
	{
		if (preloadedGameScene != null)
			return true;

		if (!gameSceneLoadRequested || gameSceneLoadFailed)
			return false;

		var status = ResourceLoader.LoadThreadedGetStatus(
			GameScenePath
		);

		if (status == ResourceLoader.ThreadLoadStatus.Loaded)
		{
			preloadedGameScene =
				ResourceLoader.LoadThreadedGet(
					GameScenePath
				) as PackedScene;

			if (preloadedGameScene == null)
			{
				gameSceneLoadFailed = true;
				GD.PrintErr("[PRELOAD] Loaded scene is null.");
				return false;
			}

			GD.Print("[PRELOAD] Game scene resources are ready.");
			return true;
		}

		if (status == ResourceLoader.ThreadLoadStatus.Failed ||
			status == ResourceLoader.ThreadLoadStatus.InvalidResource)
		{
			gameSceneLoadFailed = true;
			GD.PrintErr("[PRELOAD] Background loading failed.");
		}

		return false;
	}
	
	
	public PackedScene GetPreloadedGameScene()
	{
		if (!IsGameSceneReady())
			return null;

		return preloadedGameScene;
	}

}
