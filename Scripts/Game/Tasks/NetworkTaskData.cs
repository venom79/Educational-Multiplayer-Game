public class NetworkTaskData
{
	public int TaskId { get; }

	public string StationId { get; }

	public TaskType TaskType { get; }

	public string Question { get; }

	public string[] Options { get; }

	public NetworkTaskData(
		int taskId,
		string stationId,
		TaskType taskType,
		string question,
		string[] options
	)
	{
		TaskId = taskId;
		StationId = stationId;
		TaskType = taskType;
		Question = question;
		Options = options;
	}
}
