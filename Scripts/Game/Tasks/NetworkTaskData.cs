
using System;

public class NetworkTaskData
{
	public int TaskId { get; }
	public string StationId { get; }
	public TaskType TaskType { get; }
	public string Question { get; }

	// Multiple-choice task data.
	public string[] Options { get; }

	// Match-the-following task data.
	public string[] LeftItems { get; }
	public string[] RightItems { get; }

	public NetworkTaskData(
		int taskId,
		string stationId,
		TaskType taskType,
		string question,
		string[]? options = null,
		string[]? leftItems = null,
		string[]? rightItems = null)
	{
		TaskId = taskId;
		StationId = stationId;
		TaskType = taskType;
		Question = question;

		Options = options ?? Array.Empty<string>();
		LeftItems = leftItems ?? Array.Empty<string>();
		RightItems = rightItems ?? Array.Empty<string>();
	}
}
