using Godot;
using System.Collections.Generic;

public partial class TaskTest : Node
{
	public override void _Ready()
	{
		GD.Print("==============================");
		GD.Print("TASK POOL → TASK MANAGER TEST");
		GD.Print("==============================");

		if (TaskPoolManager.Instance == null)
		{
			GD.PrintErr(
                "ERROR: TaskPoolManager.Instance is NULL!"
			);

			return;
		}

		if (TaskManager.Instance == null)
		{
			GD.PrintErr(
                "ERROR: TaskManager.Instance is NULL!"
			);

			return;
		}

		long playerId =
			Multiplayer.GetUniqueId();

		GameMode mode =
			GameSession.Instance.SelectedMode;

		GD.Print(
			$"Local player ID: {playerId}"
		);

		GD.Print(
			$"Selected mode: {mode}"
		);

		// Prepare the room's task pool.
		TaskPoolManager.Instance.PreparePool(
			mode,
			5
		);

		GD.Print(
			$"Tasks available in pool: " +
			$"{TaskPoolManager.Instance.GetAvailableTaskCount()}"
		);

		// Let TaskManager take tasks from the pool
		// and assign them to the player's stations.
		TaskManager.Instance.AssignTasksFromPool(
			playerId
		);
	
		List<NetworkTaskData> networkTasks =
			TaskManager.Instance.CreateNetworkTaskData(
				playerId
			);

		GD.Print(
			$"Network task data count: {networkTasks.Count}"
		);

		foreach (NetworkTaskData networkTask in networkTasks)
		{
			GD.Print(
				$"Network Task → " +
				$"Station: {networkTask.StationId}, " +
				$"Type: {networkTask.TaskType}, " +
				$"ID: {networkTask.TaskId}"
			);
		}

		// Verify the assignments.
		List<PlayerTask> playerTasks =
			TaskManager.Instance.GetPlayerTasks(
				playerId
			);

		GD.Print(
			$"Tasks assigned to player: " +
			$"{playerTasks.Count}"
		);

		GD.Print("------------------------------");

		foreach (PlayerTask playerTask in playerTasks)
		{
			GD.Print(
				$"Station: {playerTask.StationId}"
			);

			GD.Print(
				$"Task ID: {playerTask.Task.Id}"
			);

			GD.Print(
				$"Question: {playerTask.Task.Question}"
			);

			GD.Print("------------------------------");
		}

		GD.Print(
			$"Remaining tasks in pool: " +
			$"{TaskPoolManager.Instance.GetAvailableTaskCount()}"
		);

		GD.Print("==============================");
		GD.Print("TASK POOL → TASK MANAGER TEST COMPLETE");
		GD.Print("==============================");
	}
	
	
}
