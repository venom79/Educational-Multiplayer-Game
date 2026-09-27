public class MultipleChoiceTask : GameTask
{
	public override TaskType Type =>
		TaskType.MultipleChoice;
		
	public string[] Options { get; }

	private readonly int correctOptionIndex;

	public MultipleChoiceTask(
		int id,
		string question,
		string[] options,
		int correctOptionIndex
	)
		: base(id, question)
	{
		Options = options;
		this.correctOptionIndex = correctOptionIndex;
	}

	protected override bool ValidateAnswer(
		string answer
	)
	{
		if (!int.TryParse(
			answer,
			out int selectedIndex))
		{
			return false;
		}

		return selectedIndex == correctOptionIndex;
	}
}
