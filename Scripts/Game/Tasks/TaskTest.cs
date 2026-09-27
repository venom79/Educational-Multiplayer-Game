using Godot;
using System.Collections.Generic;

public partial class TaskTest : Node
{
	public override void _Ready()
	{
		GD.Print("==============================");
		GD.Print("TASK STATION TEST");
		GD.Print("==============================");

		if (TaskManager.Instance == null)
		{
			GD.PrintErr(
				"ERROR: TaskManager.Instance is NULL!"
			);

			return;
		}

		long localPlayerId =
			Multiplayer.GetUniqueId();

		GD.Print(
			$"Local player ID: {localPlayerId}"
		);

		GameMode mode =
			GameSession.Instance.SelectedMode;

		GD.Print(
			$"Selected mode: {mode}"
		);

		// Temporary test station.
		string[] stationIds =
		{
			"station_01"
		};

		// Generate exactly one task for this station.
		TaskManager.Instance.GenerateAndAssignStationTasks(
			localPlayerId,
			mode,
			stationIds
		);

		List<PlayerTask> tasks =
			TaskManager.Instance.GetPlayerTasks(
				localPlayerId
			);

		GD.Print(
			$"Tasks assigned to local player: {tasks.Count}"
		);

		foreach (PlayerTask playerTask in tasks)
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

		if (tasks.Count == 0)
		{
			GD.PrintErr(
				"ERROR: No station tasks were generated!"
			);

			return;
		}

		GD.Print(
			"Task assignment successful."
		);

		GD.Print(
			"Task UI will NOT open automatically."
		);

		GD.Print(
			"Walk to station_01 and press E."
		);

		GD.Print("==============================");
	}
}
