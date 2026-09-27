using Godot;
using System.Collections.Generic;

public partial class TaskManager : Node
{
	public static TaskManager Instance { get; private set; }
	
	public const int TasksPerPlayer = 3;
	
	private readonly Dictionary<long, List<PlayerTask>> playerTasks =
		new Dictionary<long, List<PlayerTask>>();

	private readonly TaskGenerator taskGenerator =
		new TaskGenerator();
	private readonly Dictionary<long, List<NetworkTaskData>> networkPlayerTasks =
		new Dictionary<long, List<NetworkTaskData>>();
	
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
	
	public List<NetworkTaskData> CreateNetworkTaskData(
		long playerId
	)
	{
		List<NetworkTaskData> networkTasks =
			new List<NetworkTaskData>();

		List<PlayerTask> tasks =
			GetPlayerTasks(playerId);

		foreach (PlayerTask playerTask in tasks)
		{
			GameTask task = playerTask.Task;

			string[] options = null;

			if (task is MultipleChoiceTask multipleChoiceTask)
			{
				options = multipleChoiceTask.Options;
			}

			networkTasks.Add(
				new NetworkTaskData(
					task.Id,
					playerTask.StationId,
					task.Type,
					task.Question,
					options
				)
			);
		}

		return networkTasks;
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
	
	public void AssignTasksFromPool(
		long playerId,
		string[] stationIds
	)
	{
		List<GameTask> generatedTasks =
			TaskPoolManager.Instance.TakeTasks(
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
	
	public void AssignTasksFromPool(long playerId)
	{
		string[] stationIds =
		{
			"station_01",
			"station_02",
	        "station_03"
		};

		List<GameTask> generatedTasks =
			TaskPoolManager.Instance.TakeTasks(
				TasksPerPlayer
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

		GD.Print(
			$"Assigned {assignments.Count} pooled tasks " +
			$"to player {playerId}."
		);
	}
	
	public void ApplyNetworkTasks(
		long playerId,
		List<NetworkTaskData> tasks
	)
	{
		networkPlayerTasks[playerId] = tasks;

		GD.Print(
			$"Received {tasks.Count} network tasks " +
			$"for player {playerId}."
		);

		foreach (NetworkTaskData task in tasks)
		{
			GD.Print(
				$"Network task received → " +
				$"Station: {task.StationId}, " +
				$"Type: {task.TaskType}, " +
				$"ID: {task.TaskId}"
			);
		}
	}
	public List<NetworkTaskData> GetNetworkTasks(
		long playerId
	)
	{
		if (networkPlayerTasks.TryGetValue(
			playerId,
			out List<NetworkTaskData> tasks))
		{
			return tasks;
		}

		return new List<NetworkTaskData>();
	}
	
	public NetworkTaskData GetNetworkTaskAtStation(
		long playerId,
		string stationId
	)
	{
		if (!networkPlayerTasks.TryGetValue(
			playerId,
			out List<NetworkTaskData> tasks))
		{
			return null;
		}

		foreach (NetworkTaskData task in tasks)
		{
			if (task.StationId == stationId)
				return task;
		}

		return null;

	}
	
	public void MarkNetworkTaskCompleted(
		long playerId,
		int taskId
	)
	{
		if (!networkPlayerTasks.TryGetValue(
			playerId,
			out List<NetworkTaskData> tasks))
		{
			return;
		}

		for (int i = 0; i < tasks.Count; i++)
		{
			NetworkTaskData task = tasks[i];

			if (task.TaskId != taskId)
				continue;

			tasks.RemoveAt(i);

			GD.Print(
				$"Network task {taskId} completed for player {playerId}."
			);

			return;
		}
	}
}
