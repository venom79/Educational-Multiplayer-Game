public class PlayerTask
{
	public string StationId { get; }

	public GameTask Task { get; }

	public PlayerTask(
		string stationId,
		GameTask task
	)
	{
		StationId = stationId;
		Task = task;
	}
}
