
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
			throw new ArgumentOutOfRangeException(nameof(count));

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
				"questions", out JsonElement questionArray) ||
			questionArray.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException(
				$"The task bank has no valid questions array: {filePath}");
		}

		string expectedType = gameMode switch
		{
			GameMode.Maths => "Calculation",
			GameMode.Science => "MultipleChoice",
			GameMode.Language => "Translation",
			_ => throw new ArgumentException(
				$"Unsupported game mode: {gameMode}")
		};

		var uniqueQuestions = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase);

		var validQuestions = new List<JsonElement>();

		foreach (JsonElement item in questionArray.EnumerateArray())
		{
			if (item.ValueKind != JsonValueKind.Object)
				continue;

			if (!item.TryGetProperty(
					"taskType", out JsonElement typeElement) ||
				typeElement.ValueKind != JsonValueKind.String ||
				!string.Equals(
					typeElement.GetString(),
					expectedType,
					StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!item.TryGetProperty(
					"question", out JsonElement questionElement) ||
				questionElement.ValueKind != JsonValueKind.String)
			{
				continue;
			}

			string? question = questionElement.GetString();

			if (string.IsNullOrWhiteSpace(question))
				continue;

			if (!IsValidQuestion(item, gameMode))
				continue;

			if (!uniqueQuestions.Add(question.Trim()))
				continue;

			validQuestions.Add(item);
		}

		if (validQuestions.Count < count)
		{
			throw new InvalidOperationException(
				$"The {gameMode} bank contains only " +
				$"{validQuestions.Count} valid unique questions, " +
				$"but {count} are required.");
		}

		// Shuffle the valid questions before selecting the required amount.
		for (int i = validQuestions.Count - 1; i > 0; i--)
		{
			int j = random.Next(i + 1);

			(validQuestions[i], validQuestions[j]) =
				(validQuestions[j], validQuestions[i]);
		}

		var tasks = new List<GameTask>();

		for (int i = 0; i < count; i++)
		{
			JsonElement item = validQuestions[i];

			string question =
				item.GetProperty("question").GetString()!;

			int taskId = i + 1;

			switch (gameMode)
			{
				case GameMode.Maths:
				{
					string answer =
						item.GetProperty("correctAnswer").GetString()!;

					tasks.Add(new CalculationTask(
						taskId, question, answer));

					break;
				}

				case GameMode.Science:
				{
					string[] options = item
						.GetProperty("options")
						.EnumerateArray()
						.Select(option => option.GetString()!)
						.ToArray();

					int correctIndex =
						item.GetProperty("correctOptionIndex").GetInt32();

					tasks.Add(new MultipleChoiceTask(
						taskId, question, options, correctIndex));

					break;
				}

				case GameMode.Language:
				{
					string answer =
						item.GetProperty("correctAnswer").GetString()!;

					tasks.Add(new TranslationTask(
						taskId, question, answer));

					break;
				}
			}
		}

		GD.Print(
			$"Loaded {tasks.Count} unique {gameMode} tasks from JSON.");

		return tasks;
	}

	private static bool IsValidQuestion(
		JsonElement item,
		GameMode gameMode)
	{
		if (gameMode == GameMode.Maths ||
			gameMode == GameMode.Language)
		{
			return item.TryGetProperty(
					   "correctAnswer", out JsonElement answer) &&
				   answer.ValueKind == JsonValueKind.String &&
				   !string.IsNullOrWhiteSpace(answer.GetString());
		}

		if (gameMode == GameMode.Science)
		{
			if (!item.TryGetProperty(
					"options", out JsonElement optionsElement) ||
				optionsElement.ValueKind != JsonValueKind.Array ||
				optionsElement.GetArrayLength() != 4 ||
				!item.TryGetProperty(
					"correctOptionIndex", out JsonElement indexElement) ||
				indexElement.ValueKind != JsonValueKind.Number ||
				!indexElement.TryGetInt32(out int index) ||
				index < 0 || index >= 4 ||
				!item.TryGetProperty(
					"correctAnswer", out JsonElement answerElement) ||
				answerElement.ValueKind != JsonValueKind.String)
			{
				return false;
			}

			string? correctAnswer = answerElement.GetString();

			string[] options = optionsElement
				.EnumerateArray()
				.Select(option =>
					option.ValueKind == JsonValueKind.String
						? option.GetString()
						: null)
				.ToArray()!;

			return options.All(option =>
					   !string.IsNullOrWhiteSpace(option)) &&
				   options.Distinct(
					   StringComparer.OrdinalIgnoreCase).Count() == 4 &&
				   string.Equals(
					   options[index]?.Trim(),
					   correctAnswer?.Trim(),
					   StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}
}
