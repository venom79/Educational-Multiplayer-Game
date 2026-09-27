using System;

public class TranslationTask : GameTask
{
	private readonly string correctAnswer;

	public TranslationTask(
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
		return string.Equals(
			answer.Trim(),
			correctAnswer.Trim(),
			StringComparison.OrdinalIgnoreCase
		);
	}
}
