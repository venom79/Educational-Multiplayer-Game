using System.Collections.Generic;

public class TaskPool
{
	private readonly Queue<GameTask> availableTasks =
		new Queue<GameTask>();

	public int Count =>
		availableTasks.Count;

	public void AddTasks(
		List<GameTask> tasks
	)
	{
		foreach (GameTask task in tasks)
		{
			if (task == null)
				continue;

			availableTasks.Enqueue(task);
		}
	}

	public GameTask TakeTask()
	{
		if (availableTasks.Count == 0)
			return null;

		return availableTasks.Dequeue();
	}

	public List<GameTask> TakeTasks(int count)
	{
		List<GameTask> tasks =
			new List<GameTask>();

		while (
			tasks.Count < count &&
			availableTasks.Count > 0
		)
		{
			GameTask task = TakeTask();

			if (task != null)
				tasks.Add(task);
		}

		return tasks;
	}

	public void Clear()
	{
		availableTasks.Clear();
	}
}
