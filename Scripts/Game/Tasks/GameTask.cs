public abstract class GameTask
{
	public int Id { get; }

	public string Question { get; }

	public bool IsCompleted { get; private set; }

	protected GameTask(
		int id,
		string question
	)
	{
		Id = id;
		Question = question;
		IsCompleted = false;
	}

	public bool SubmitAnswer(string answer)
	{
		if (IsCompleted)
			return false;

		if (!ValidateAnswer(answer))
			return false;

		IsCompleted = true;

		return true;
	}

	protected abstract bool ValidateAnswer(
		string answer
	);
}
