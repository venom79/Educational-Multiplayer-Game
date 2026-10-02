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
	
	private const int TasksPerPlayer = 3;
	private const int MaximumPlayers = 2;
	
	public bool GameStarted { get; private set; }
	public bool GameEnded { get; private set; }
	
	private const string ResultScenePath =
	"res://Scenes/Results/ResultScreen.tscn";
	
	private readonly Dictionary<long, int> completedTasks =
		new Dictionary<long, int>();

	private int teamCompletedTasks = 0;

	public int TeamCompletedTasks =>
		teamCompletedTasks;

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
			? teamCompletedTasks
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
	
	private void InitializeTaskProgress()
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		completedTasks.Clear();

		foreach (
			NetworkPlayer player
			in LobbyManager.Instance.GetPlayers()
		)
		{
			completedTasks[player.PeerId] = 0;
		}

		teamCompletedTasks = 0;
		BroadcastProgress();
		
		GD.Print(
			"Task progress initialized."
		);

		GD.Print(
			$"Team progress: " +
			$"{teamCompletedTasks} / " +
			$"{TeamTotalTasks}"
		);
	}
	
	public void RegisterTaskCompletion(long playerId)
	{
		if (!NetworkManager.Instance.IsServer)
			return;

		if (GameEnded)
			return;

		if (!completedTasks.ContainsKey(playerId))
		{
			GD.PrintErr(
				$"Cannot register task completion. " +
				$"Unknown player: {playerId}"
			);

			return;
		}

		completedTasks[playerId]++;

		teamCompletedTasks++;
		
		BroadcastProgress();

		GD.Print(
			$"Player {playerId} completed a task."
		);

		GD.Print(
			$"Player progress: " +
			$"{completedTasks[playerId]} / " +
			$"{TasksPerPlayer}"
		);

		GD.Print(
			$"Team progress: " +
			$"{teamCompletedTasks} / " +
			$"{TeamTotalTasks}"
		);

		if (teamCompletedTasks >= TeamTotalTasks)
		{
			EndGame(GameState.Victory);
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
		return teamCompletedTasks;
	}

	public int GetTeamTotalTasks()
	{
		return TeamTotalTasks;
	}
	
	private void StartGameTimer()
	{
		if (GameStarted)
			return;

		InitializeTaskProgress();
		
		GameStarted = true;
		GameEnded = false;
		
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
		if (!NetworkManager.Instance.IsServer)
			return;

		if (GameEnded)
			return;

		GD.Print(
			"GAME TIMER FINISHED."
		);

		EndGame(GameState.AliensWin);
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
			$"FINAL TEAM PROGRESS: " +
			$"{teamCompletedTasks}/{TeamTotalTasks}"
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
			MaximumPlayers * TasksPerPlayer;

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

		foreach (
			NetworkPlayer player
			in LobbyManager.Instance.GetPlayers()
		)
		{
			int playerCompleted =
				GetPlayerCompletedTasks(player.PeerId);

			int playerTotal =
				GetPlayerTotalTasks(player.PeerId);

			RpcId(
				player.PeerId,
				nameof(ReceiveProgressRpc),
				playerCompleted,
				playerTotal,
				teamCompletedTasks,
				TeamTotalTasks
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
		int teamCompleted,
		int teamTotal
	)
	{
		synchronizedPlayerCompletedTasks =
			playerCompleted;

		synchronizedPlayerTotalTasks =
			playerTotal;

		synchronizedTeamCompletedTasks =
			teamCompleted;

		synchronizedTeamTotalTasks =
			teamTotal;

		GD.Print(
			$"Progress synchronized → " +
			$"Player: {playerCompleted}/{playerTotal}, " +
			$"Team: {teamCompleted}/{teamTotal}"
		);
	}
}
