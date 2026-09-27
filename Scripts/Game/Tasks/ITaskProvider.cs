using System.Collections.Generic;

public interface ITaskProvider
{
	List<GameTask> GenerateTasks(
		GameMode gameMode,
		int count
	);
}
