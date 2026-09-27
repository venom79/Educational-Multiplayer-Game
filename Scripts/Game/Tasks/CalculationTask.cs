public class CalculationTask : GameTask
{
	private readonly string correctAnswer;

	public CalculationTask(
		int id,
		string question,
		string correctAnswer
	)
		: base(id, question)
	{
		this.correctAnswer = correctAnswer;
	}

	protected override bool ValidateAnswer(
		string answer
	)
	{
		return answer.Trim() == correctAnswer.Trim();
	}
}
