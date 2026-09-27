using Godot;
using System.Collections.Generic;

public partial class TaskPoolManager : Node
{
	public static TaskPoolManager Instance { get; private set; }

	private readonly TaskPool taskPool =
		new TaskPool();

	private readonly ITaskProvider taskProvider =
		new LocalTaskProvider();

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		GD.Print("TaskPoolManager initialized.");
	}

	public void PreparePool(
		GameMode gameMode,
		int taskCount
	)
	{
		taskPool.Clear();

		List<GameTask> tasks =
			taskProvider.GenerateTasks(
				gameMode,
				taskCount
			);

		taskPool.AddTasks(tasks);

		GD.Print(
			$"Task pool prepared: {taskPool.Count} tasks."
		);
	}

	public GameTask TakeTask()
	{
		GameTask task =
			taskPool.TakeTask();

		if (task == null)
		{
			GD.PrintErr(
                "Task pool is empty!"
			);
		}

		return task;
	}

	public List<GameTask> TakeTasks(int count)
	{
		return taskPool.TakeTasks(count);
	}

	public int GetAvailableTaskCount()
	{
		return taskPool.Count;
	}

	public void ClearPool()
	{
		taskPool.Clear();
	}
}
