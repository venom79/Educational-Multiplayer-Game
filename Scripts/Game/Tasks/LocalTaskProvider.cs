
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public class LocalTaskProvider : ITaskProvider
{
	private readonly Random random = new Random();

	public List<GameTask> GenerateTasks(GameMode gameMode, int count)
	{
		if (count <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}

		// Validate the mode and get its permitted task types.
		// Each mode must include at least one task of every permitted type.
		// Translation is disabled for Science/Maths; Calculation is disabled for Language.
		TaskType[] allowedTypes = gameMode switch
		{
			GameMode.Science => new[]
			{
				TaskType.Calculation,
				TaskType.MultipleChoice,
				TaskType.MatchTheFollowing
			},
			GameMode.Maths => new[]
			{
				TaskType.Calculation,
				TaskType.MultipleChoice,
				TaskType.MatchTheFollowing
			},
			GameMode.Language => new[]
			{
				TaskType.Translation,
				TaskType.MultipleChoice,
				TaskType.MatchTheFollowing
			},
			_ => throw new ArgumentException(
				$"Unsupported game mode: {gameMode}")
		};

		string filePath = gameMode switch
		{
			GameMode.Maths => "res://Data/Tasks/maths_tasks.json",
			GameMode.Science => "res://Data/Tasks/science_tasks.json",
			GameMode.Language => "res://Data/Tasks/language_tasks.json",
			_ => throw new ArgumentException(
				$"Unsupported game mode: {gameMode}")
		};

		if (!Godot.FileAccess.FileExists(filePath))
		{
			throw new InvalidOperationException(
				$"JSON task bank not found: {filePath}");
		}

		string json = Godot.FileAccess.GetFileAsString(filePath);

		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement root = document.RootElement;

		if (!root.TryGetProperty("mode", out JsonElement modeElement) ||
			modeElement.ValueKind != JsonValueKind.String ||
			!string.Equals(
				modeElement.GetString(),
				gameMode.ToString(),
				StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException(
				$"The task bank mode does not match {gameMode}: {filePath}");
		}

		if (!root.TryGetProperty(
				"questions",
				out JsonElement questionArray) ||
			questionArray.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException(
				$"The task bank has no valid questions array: {filePath}");
		}

		var uniqueQuestions = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase);

		var validQuestions = new List<JsonElement>();

		foreach (JsonElement item in questionArray.EnumerateArray())
		{
			if (item.ValueKind != JsonValueKind.Object)
			{
				continue;
			}

			if (!item.TryGetProperty(
					"taskType",
					out JsonElement typeElement) ||
				typeElement.ValueKind != JsonValueKind.String)
			{
				continue;
			}

			string? typeText = typeElement.GetString();

			if (!Enum.TryParse(
					typeText,
					true,
					out TaskType parsedType))
			{
				continue;
			}

			// Only accept task types permitted for this game mode.
			if (!allowedTypes.Contains(parsedType))
			{
				continue;
			}

			if (!item.TryGetProperty(
					"question",
					out JsonElement questionElement) ||
				questionElement.ValueKind != JsonValueKind.String)
			{
				continue;
			}

			string? question = questionElement.GetString();

			if (string.IsNullOrWhiteSpace(question))
			{
				continue;
			}

			if (!IsValidQuestion(item, parsedType))
			{
				continue;
			}

			if (!uniqueQuestions.Add(question.Trim()))
			{
				continue;
			}

			validQuestions.Add(item);
		}

		if (validQuestions.Count < count)
		{
			string availableByType = string.Join(
				", ",
				allowedTypes.Select(type =>
				{
					int typeCount = validQuestions.Count(item =>
						Enum.TryParse(
							item.GetProperty("taskType").GetString(),
							true,
							out TaskType parsed) &&
						parsed == type);
					return $"{type}: {typeCount}";
				}));

			throw new InvalidOperationException(
				$"The {gameMode} bank contains only {validQuestions.Count} " +
				$"valid unique tasks, but {count} are required. " +
				$"Valid tasks by type: {availableByType}. " +
				$"Check the task fields and allowed types in {filePath}.");
		}

		if (count < allowedTypes.Length)
		{
			throw new InvalidOperationException(
				$"{gameMode} requires at least {allowedTypes.Length} tasks to include " +
				$"every permitted task type, but only {count} were requested.");
		}

		// Build one bucket for each permitted type. Every bucket must have at least
		// one valid question, otherwise this mode cannot guarantee its required mix.
		var tasksByType = new Dictionary<TaskType, List<JsonElement>>();

		foreach (TaskType type in allowedTypes)
		{
			List<JsonElement> bucket = validQuestions
				.Where(item =>
					Enum.TryParse(
						item.GetProperty("taskType").GetString(),
						true,
						out TaskType parsed) &&
					parsed == type)
				.ToList();

			if (bucket.Count == 0)
			{
				throw new InvalidOperationException(
					$"{gameMode} is missing a valid '{type}' question in {filePath}. " +
					$"Add at least one valid question of this type to guarantee the required mix.");
			}

			for (int i = bucket.Count - 1; i > 0; i--)
			{
				int j = random.Next(i + 1);
				(bucket[i], bucket[j]) = (bucket[j], bucket[i]);
			}

			tasksByType[type] = bucket;
		}

		var selectedQuestions = new List<JsonElement>(count);

		while (selectedQuestions.Count < count)
		{
			bool addedAny = false;

			foreach (TaskType type in allowedTypes)
			{
				List<JsonElement> bucket = tasksByType[type];

				if (bucket.Count == 0)
				{
					continue;
				}

				selectedQuestions.Add(bucket[0]);
				bucket.RemoveAt(0);
				addedAny = true;

				if (selectedQuestions.Count == count)
				{
					break;
				}
			}

			if (!addedAny)
			{
				break;
			}
		}

		var tasks = new List<GameTask>();

		for (int i = 0; i < count; i++)
		{
			JsonElement item = selectedQuestions[i];

			string question = item.GetProperty("question").GetString()!;
			string typeText = item.GetProperty("taskType").GetString()!;

			if (!Enum.TryParse(
					typeText,
					true,
					out TaskType taskType))
			{
				throw new InvalidOperationException(
					$"Unsupported task type: {typeText}");
			}

			int taskId = i + 1;

			switch (taskType)
			{
				case TaskType.Calculation:
				{
					string answer =
						item.GetProperty("correctAnswer").GetString()!;

					tasks.Add(new CalculationTask(
						taskId,
						question,
						answer));

					break;
				}

				case TaskType.MultipleChoice:
				{
					string[] options = ReadStringArray(
						item,
						"options");

					int correctIndex =
						item.GetProperty("correctOptionIndex").GetInt32();

					tasks.Add(new MultipleChoiceTask(
						taskId,
						question,
						options,
						correctIndex));

					break;
				}

				case TaskType.Translation:
				{
					string answer =
						item.GetProperty("correctAnswer").GetString()!;

					tasks.Add(new TranslationTask(
						taskId,
						question,
						answer));

					break;
				}

				case TaskType.MatchTheFollowing:
				{
					string[] leftItems = ReadStringArray(
						item,
						"leftItems");

					string[] rightItems = ReadStringArray(
						item,
						"rightItems");

					int[] correctMatches = ReadIntArray(
						item,
						"correctMatches");

					// Correct matches are kept in the task object on the
					// host/server. They are not included in client task data.
					tasks.Add(new MatchTheFollowingTask(
						taskId,
						question,
						leftItems,
						rightItems,
						correctMatches));

					break;
				}

				default:
				{
					throw new InvalidOperationException(
						$"Unsupported task type: {taskType}");
				}
			}
		}

		GD.Print(
			$"Loaded {tasks.Count} unique {gameMode} tasks from JSON.");

		foreach (TaskType type in allowedTypes)
		{
			int typeCount = tasks.Count(task => task.Type == type);

			GD.Print(
				$"  {gameMode} - {type}: {typeCount} task(s)");
		}

		return tasks;
	}

	private static bool IsValidQuestion(
		JsonElement item,
		TaskType taskType)
	{
		switch (taskType)
		{
			case TaskType.Calculation:
			case TaskType.Translation:
			{
				return item.TryGetProperty(
						   "correctAnswer",
						   out JsonElement answerElement) &&
					   answerElement.ValueKind == JsonValueKind.String &&
					   !string.IsNullOrWhiteSpace(
						   answerElement.GetString()) &&
					   HasEmptyArray(item, "options") &&
					   HasCorrectOptionIndex(item, -1) &&
					   HasEmptyArray(item, "leftItems") &&
					   HasEmptyArray(item, "rightItems") &&
					   HasEmptyArray(item, "correctMatches");
			}

			case TaskType.MultipleChoice:
			{
				if (!item.TryGetProperty(
						"options",
						out JsonElement optionsElement) ||
					optionsElement.ValueKind != JsonValueKind.Array ||
					optionsElement.GetArrayLength() != 4 ||
					!item.TryGetProperty(
						"correctOptionIndex",
						out JsonElement indexElement) ||
					indexElement.ValueKind != JsonValueKind.Number ||
					!indexElement.TryGetInt32(out int index) ||
					index < 0 ||
					index >= 4 ||
					!item.TryGetProperty(
						"correctAnswer",
						out JsonElement answerElement) ||
					answerElement.ValueKind != JsonValueKind.String ||
					!HasEmptyArray(item, "leftItems") ||
					!HasEmptyArray(item, "rightItems") ||
					!HasEmptyArray(item, "correctMatches"))
				{
					return false;
				}

				string? correctAnswer = answerElement.GetString();

				string?[] options = optionsElement
					.EnumerateArray()
					.Select(option =>
						option.ValueKind == JsonValueKind.String
							? option.GetString()
							: null)
					.ToArray();

				return options.All(
						   option => !string.IsNullOrWhiteSpace(option)) &&
					   options.Distinct(
						   StringComparer.OrdinalIgnoreCase).Count() == 4 &&
					   string.Equals(
						   options[index]?.Trim(),
						   correctAnswer?.Trim(),
						   StringComparison.OrdinalIgnoreCase) &&
					   HasCorrectOptionIndex(item, index);
			}

			case TaskType.MatchTheFollowing:
			{
				if (!item.TryGetProperty(
						"leftItems",
						out JsonElement leftElement) ||
					leftElement.ValueKind != JsonValueKind.Array ||
					!item.TryGetProperty(
						"rightItems",
						out JsonElement rightElement) ||
					rightElement.ValueKind != JsonValueKind.Array ||
					!item.TryGetProperty(
						"correctMatches",
						out JsonElement matchesElement) ||
					matchesElement.ValueKind != JsonValueKind.Array ||
					!HasEmptyArray(item, "options") ||
					!HasCorrectOptionIndex(item, -1) ||
					!item.TryGetProperty(
						"correctAnswer",
						out JsonElement answerElement) ||
					answerElement.ValueKind != JsonValueKind.String ||
					!string.IsNullOrEmpty(answerElement.GetString()))
				{
					return false;
				}

				string?[] leftItems = leftElement
					.EnumerateArray()
					.Select(value =>
						value.ValueKind == JsonValueKind.String
							? value.GetString()
							: null)
					.ToArray();

				string?[] rightItems = rightElement
					.EnumerateArray()
					.Select(value =>
						value.ValueKind == JsonValueKind.String
							? value.GetString()
							: null)
					.ToArray();

				if (leftItems.Length < 3 ||
					leftItems.Length > 6 ||
					rightItems.Length != leftItems.Length ||
					matchesElement.GetArrayLength() != leftItems.Length)
				{
					return false;
				}

				if (leftItems.Any(string.IsNullOrWhiteSpace) ||
					rightItems.Any(string.IsNullOrWhiteSpace))
				{
					return false;
				}

				if (leftItems.Distinct(
						StringComparer.OrdinalIgnoreCase).Count()
					!= leftItems.Length)
				{
					return false;
				}

				if (rightItems.Distinct(
						StringComparer.OrdinalIgnoreCase).Count()
					!= rightItems.Length)
				{
					return false;
				}

				int[] matches = new int[matchesElement.GetArrayLength()];
				int matchPosition = 0;

				foreach (JsonElement match in matchesElement.EnumerateArray())
				{
					if (match.ValueKind != JsonValueKind.Number ||
						!match.TryGetInt32(out int matchIndex) ||
						matchIndex < 0 ||
						matchIndex >= rightItems.Length)
					{
						return false;
					}

					matches[matchPosition++] = matchIndex;
				}

				return matches.Distinct().Count() == matches.Length;
			}

			default:
				return false;
		}
	}

	private static string[] ReadStringArray(
		JsonElement item,
		string propertyName)
	{
		if (!item.TryGetProperty(
				propertyName,
				out JsonElement arrayElement) ||
			arrayElement.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException(
				$"Task is missing a valid '{propertyName}' array.");
		}

		var values = new List<string>();

		foreach (JsonElement element in arrayElement.EnumerateArray())
		{
			if (element.ValueKind != JsonValueKind.String)
			{
				throw new InvalidOperationException(
					$"Task property '{propertyName}' contains a non-string value.");
			}

			string? value = element.GetString();

			if (string.IsNullOrWhiteSpace(value))
			{
				throw new InvalidOperationException(
					$"Task property '{propertyName}' contains an empty value.");
			}

			values.Add(value);
		}

		return values.ToArray();
	}

	private static int[] ReadIntArray(
		JsonElement item,
		string propertyName)
	{
		if (!item.TryGetProperty(
				propertyName,
				out JsonElement arrayElement) ||
			arrayElement.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException(
				$"Task is missing a valid '{propertyName}' array.");
		}

		var values = new List<int>();

		foreach (JsonElement element in arrayElement.EnumerateArray())
		{
			if (element.ValueKind != JsonValueKind.Number ||
				!element.TryGetInt32(out int value))
			{
				throw new InvalidOperationException(
					$"Task property '{propertyName}' contains a non-integer value.");
			}

			values.Add(value);
		}

		return values.ToArray();
	}

	private static bool HasEmptyArray(
		JsonElement item,
		string propertyName)
	{
		if (!item.TryGetProperty(
				propertyName,
				out JsonElement element))
		{
			return true;
		}

		return element.ValueKind == JsonValueKind.Array
			&& element.GetArrayLength() == 0;
	}

	private static bool HasCorrectOptionIndex(
		JsonElement item,
		int expectedIndex)
	{
		return item.TryGetProperty(
				   "correctOptionIndex",
				   out JsonElement indexElement) &&
			   indexElement.ValueKind == JsonValueKind.Number &&
			   indexElement.TryGetInt32(out int index) &&
			   index == expectedIndex;
	}
}
