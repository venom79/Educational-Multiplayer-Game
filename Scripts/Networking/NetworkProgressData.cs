public class NetworkProgressData
{
	public int PlayerCompletedTasks { get; }
	public int PlayerTotalTasks { get; }

	public int TeamCompletedTasks { get; }
	public int TeamTotalTasks { get; }

	public NetworkProgressData(
		int playerCompletedTasks,
		int playerTotalTasks,
		int teamCompletedTasks,
		int teamTotalTasks
	)
	{
		PlayerCompletedTasks = playerCompletedTasks;
		PlayerTotalTasks = playerTotalTasks;

		TeamCompletedTasks = teamCompletedTasks;
		TeamTotalTasks = teamTotalTasks;
	}
}
