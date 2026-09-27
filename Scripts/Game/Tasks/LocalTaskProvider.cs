using System.Collections.Generic;

public class LocalTaskProvider : ITaskProvider
{
	private readonly TaskGenerator taskGenerator =
		new TaskGenerator();

	public List<GameTask> GenerateTasks(
		GameMode gameMode,
		int count
	)
	{
		return taskGenerator.GenerateTasks(
			gameMode,
			count
		);
	}
}
