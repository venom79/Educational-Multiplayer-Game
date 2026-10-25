
using System;

public static class TaskTypeRules
{
	public static TaskType[] GetAllowedTypes(GameMode mode)
	{
		return mode switch
		{
			GameMode.Maths => new[]
			{
				TaskType.MultipleChoice,
				TaskType.Calculation,
				TaskType.MatchTheFollowing
			},

			GameMode.Science => new[]
			{
				TaskType.MultipleChoice,
				TaskType.Calculation,
				TaskType.MatchTheFollowing
			},

			GameMode.Language => new[]
			{
				TaskType.MultipleChoice,
				TaskType.Translation,
				TaskType.MatchTheFollowing
			},

			_ => throw new ArgumentOutOfRangeException(
				nameof(mode),
				mode,
                "Unsupported game mode."
			)
		};
	}

	public static bool IsAllowed(GameMode mode, TaskType taskType)
	{
		foreach (TaskType allowedType in GetAllowedTypes(mode))
		{
			if (allowedType == taskType)
				return true;
		}

		return false;
	}
}
