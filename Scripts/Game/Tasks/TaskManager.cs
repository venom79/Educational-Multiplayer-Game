using Godot;
using System.Collections.Generic;

public partial class TaskManager : Node
{
	public static TaskManager Instance { get; private set; }

	private readonly Dictionary<long, List<PlayerTask>> playerTasks =
		new Dictionary<long, List<PlayerTask>>();

	private readonly TaskGenerator taskGenerator =
		new TaskGenerator();

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		GD.Print("TaskManager initialized.");
	}

	// --------------------------------------------------
	// TASK ASSIGNMENT
	// --------------------------------------------------

	public void AssignTasks(
		long playerId,
		List<PlayerTask> tasks
	)
	{
		playerTasks[playerId] = tasks;

		GD.Print(
			$"Assigned {tasks.Count} tasks to player {playerId}."
		);
	}

	public List<PlayerTask> GetPlayerTasks(long playerId)
	{
		if (playerTasks.TryGetValue(
				playerId,
				out List<PlayerTask> tasks))
		{
			return tasks;
		}

		return new List<PlayerTask>();
	}

	// --------------------------------------------------
	// TASK LOOKUP
	// --------------------------------------------------

	public GameTask GetTask(
		long playerId,
		int taskId
	)
	{
		List<PlayerTask> tasks =
			GetPlayerTasks(playerId);

		foreach (PlayerTask playerTask in tasks)
		{
			if (playerTask.Task.Id == taskId)
				return playerTask.Task;
		}

		return null;
	}

	public PlayerTask GetTaskAtStation(
		long playerId,
		string stationId
	)
	{
		List<PlayerTask> tasks =
			GetPlayerTasks(playerId);

		foreach (PlayerTask playerTask in tasks)
		{
			if (playerTask.StationId == stationId)
				return playerTask;
		}

		return null;
	}

	// --------------------------------------------------
	// TASK COUNTS
	// --------------------------------------------------

	public int GetCompletedTaskCount(long playerId)
	{
		List<PlayerTask> tasks =
			GetPlayerTasks(playerId);

		int completed = 0;

		foreach (PlayerTask playerTask in tasks)
		{
			if (playerTask.Task.IsCompleted)
				completed++;
		}

		return completed;
	}

	public int GetTotalTaskCount(long playerId)
	{
		return GetPlayerTasks(playerId).Count;
	}

	public int GetTeamCompletedTaskCount()
	{
		int completed = 0;

		foreach (List<PlayerTask> tasks in playerTasks.Values)
		{
			foreach (PlayerTask playerTask in tasks)
			{
				if (playerTask.Task.IsCompleted)
					completed++;
			}
		}

		return completed;
	}

	public int GetTeamTotalTaskCount()
	{
		int total = 0;

		foreach (List<PlayerTask> tasks in playerTasks.Values)
		{
			total += tasks.Count;
		}

		return total;
	}

	// --------------------------------------------------
	// COMPLETION CHECK
	// --------------------------------------------------

	public bool AreAllTasksCompleted()
	{
		if (playerTasks.Count == 0)
			return false;

		foreach (List<PlayerTask> tasks in playerTasks.Values)
		{
			foreach (PlayerTask playerTask in tasks)
			{
				if (!playerTask.Task.IsCompleted)
					return false;
			}
		}

		return true;
	}

	// --------------------------------------------------
	// NORMAL TASK GENERATION
	// --------------------------------------------------

	public void GenerateAndAssignTasks(
		long playerId,
		GameMode gameMode,
		int taskCount
	)
	{
		List<GameTask> generatedTasks =
			taskGenerator.GenerateTasks(
				gameMode,
				taskCount
			);

		List<PlayerTask> assignments =
			new List<PlayerTask>();

		for (int i = 0; i < generatedTasks.Count; i++)
		{
			assignments.Add(
				new PlayerTask(
					$"station_{i + 1:00}",
					generatedTasks[i]
				)
			);
		}

		AssignTasks(
			playerId,
			assignments
		);
	}

	// --------------------------------------------------
	// STATION-BASED TASK GENERATION
	// --------------------------------------------------

	public void GenerateAndAssignStationTasks(
		long playerId,
		GameMode gameMode,
		string[] stationIds
	)
	{
		List<GameTask> generatedTasks =
			taskGenerator.GenerateTasks(
				gameMode,
				stationIds.Length
			);

		List<PlayerTask> assignments =
			new List<PlayerTask>();

		int count =
			Mathf.Min(
				generatedTasks.Count,
				stationIds.Length
			);

		for (int i = 0; i < count; i++)
		{
			assignments.Add(
				new PlayerTask(
					stationIds[i],
					generatedTasks[i]
				)
			);
		}

		AssignTasks(
			playerId,
			assignments
		);
	}
}
