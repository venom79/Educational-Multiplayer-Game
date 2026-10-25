
using System;
using System.Linq;

public class MatchTheFollowingTask : GameTask
{
	public override TaskType Type => TaskType.MatchTheFollowing;

	public string[] LeftItems { get; }
	public string[] RightItems { get; }

	// Correct answers remain private to the server.
	private readonly int[] correctMatches;

	public MatchTheFollowingTask(
		int id,
		string question,
		string[] leftItems,
		string[] rightItems,
		int[] correctMatches
	) : base(id, question)
	{
		if (leftItems == null || rightItems == null || correctMatches == null)
			throw new ArgumentNullException("Matching task data cannot be null.");

		if (leftItems.Length < 3 ||
			leftItems.Length > 6 ||
			rightItems.Length != leftItems.Length ||
			correctMatches.Length != leftItems.Length)
		{
			throw new ArgumentException(
				"Matching tasks must contain 3–6 complete pairs.");
		}

		if (leftItems.Any(string.IsNullOrWhiteSpace) ||
			rightItems.Any(string.IsNullOrWhiteSpace))
		{
			throw new ArgumentException(
				"Matching items cannot be empty.");
		}

		if (leftItems.Distinct(StringComparer.OrdinalIgnoreCase).Count()
				!= leftItems.Length ||
			rightItems.Distinct(StringComparer.OrdinalIgnoreCase).Count()
				!= rightItems.Length)
		{
			throw new ArgumentException(
				"Matching items must be unique on each side.");
		}

		if (correctMatches.Any(i => i < 0 || i >= rightItems.Length) ||
			correctMatches.Distinct().Count() != correctMatches.Length)
		{
			throw new ArgumentException(
				"Correct matches must be valid unique indices.");
		}

		LeftItems = (string[])leftItems.Clone();
		RightItems = (string[])rightItems.Clone();
		this.correctMatches = (int[])correctMatches.Clone();
	}

	protected override bool ValidateAnswer(string answer)
	{
		if (string.IsNullOrWhiteSpace(answer))
			return false;

		string[] selectedMatches = answer.Split(',');

		if (selectedMatches.Length != correctMatches.Length)
			return false;

		int[] selectedIndices = new int[selectedMatches.Length];

		for (int i = 0; i < selectedMatches.Length; i++)
		{
			if (!int.TryParse(selectedMatches[i].Trim(), out int index))
				return false;

			if (index < 0 || index >= RightItems.Length)
				return false;

			selectedIndices[i] = index;
		}

		// Each right-side item must be selected exactly once.
		if (selectedIndices.Distinct().Count() != selectedIndices.Length)
			return false;

		// Compare against the server's correct matching order.
		for (int i = 0; i < correctMatches.Length; i++)
		{
			if (selectedIndices[i] != correctMatches[i])
				return false;
		}

		return true;
	}
}
