
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class TaskPoolManager : Node
{
	public static TaskPoolManager Instance { get; private set; }

	private readonly TaskPool taskPool = new TaskPool();
	private readonly ITaskProvider localProvider = new LocalTaskProvider();
	private readonly GeminiTaskProvider geminiProvider = new GeminiTaskProvider();

	public bool IsPreparing { get; private set; }

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

	public async Task PreparePoolAsync(GameMode gameMode, int taskCount)
	{
		if (IsPreparing)
			throw new InvalidOperationException("Task pool is already preparing.");

		IsPreparing = true;
		taskPool.Clear();

		try
		{
			GD.Print($"Generating {taskCount} {gameMode} tasks with Gemini...");

			List<GameTask> tasks = await geminiProvider.GenerateTasksAsync(
				gameMode,
				taskCount
			);

			if (tasks.Count != taskCount)
				throw new InvalidOperationException("Gemini returned an unexpected task count.");

			taskPool.AddTasks(tasks);
			GD.Print($"Gemini task pool ready: {taskPool.Count} tasks.");
		}
		catch (Exception exception)
		{
			GD.PrintErr($"Gemini generation failed: {exception.Message}");
			GD.Print("Falling back to locally generated tasks.");

			List<GameTask> tasks = localProvider.GenerateTasks(
				gameMode,
				taskCount
			);

			taskPool.Clear();
			taskPool.AddTasks(tasks);

			GD.Print($"Local fallback pool ready: {taskPool.Count} tasks.");
		}
		finally
		{
			IsPreparing = false;
		}

		if (taskPool.Count != taskCount)
		{
			taskPool.Clear();
			throw new InvalidOperationException(
				$"Could not prepare all tasks. Required {taskCount}, got {taskPool.Count}."
			);
		}
	}

	// Retain the synchronous method temporarily for compatibility.
	// New match-start code should call PreparePoolAsync instead.
	public void PreparePool(GameMode gameMode, int taskCount)
	{
		taskPool.Clear();

		List<GameTask> tasks = localProvider.GenerateTasks(
			gameMode,
			taskCount
		);

		taskPool.AddTasks(tasks);
		GD.Print($"Local task pool prepared: {taskPool.Count} tasks.");
	}

	public GameTask TakeTask()
	{
		GameTask task = taskPool.TakeTask();

		if (task == null)
			GD.PrintErr("Task pool is empty!");

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
