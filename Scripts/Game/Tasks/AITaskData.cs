
using System.Collections.Generic;

public class AITaskData
{
	public string TaskType { get; set; } = "";
	public string Question { get; set; } = "";
	public string CorrectAnswer { get; set; } = "";
	public List<string> Options { get; set; } = new();
	public int CorrectOptionIndex { get; set; } = -1;
}
