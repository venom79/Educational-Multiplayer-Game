
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GeminiTaskProvider
{
	private const string Model = "gemini-3.8-flash";

	private static readonly System.Net.Http.HttpClient Http =
		new System.Net.Http.HttpClient();

	private static string GetApiKey()
	{
		string? apiKey =
			System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");

		if (!string.IsNullOrWhiteSpace(apiKey))
			return apiKey.Trim();

		string envPath =
			ProjectSettings.GlobalizePath("res://.env");

		if (!File.Exists(envPath))
		{
			GD.PrintErr($".env file not found: {envPath}");
			return null;
		}

		foreach (string line in File.ReadAllLines(envPath))
		{
			string trimmed = line.Trim();

			if (string.IsNullOrWhiteSpace(trimmed) ||
				trimmed.StartsWith("#"))
				continue;

			int separator = trimmed.IndexOf('=');

			if (separator < 0)
				continue;

			string name = trimmed[..separator].Trim();

			if (name != "GEMINI_API_KEY")
				continue;

			string value = trimmed[(separator + 1)..].Trim();

			if (value.Length >= 2 &&
				((value.StartsWith("\"") && value.EndsWith("\"")) ||
				 (value.StartsWith("'") && value.EndsWith("'"))))
			{
				value = value[1..^1];
			}

			return string.IsNullOrWhiteSpace(value) ? null : value;
		}

		GD.PrintErr("GEMINI_API_KEY was not found in .env.");
		return null;
	}

	public async Task<List<GameTask>> GenerateTasksAsync(
		GameMode gameMode,
		int taskCount)
	{
		if (taskCount <= 0 || taskCount > 100)
			throw new ArgumentOutOfRangeException(nameof(taskCount));

		string? apiKey = GetApiKey();

		if (string.IsNullOrWhiteSpace(apiKey))
		{
			throw new InvalidOperationException(
				"GEMINI_API_KEY is missing. Check your environment variable or project-root .env file.");
		}

		string taskInstructions = gameMode switch
		{
			GameMode.Maths =>
				"Generate only Calculation tasks containing arithmetic questions and correct numeric answers.",

			GameMode.Science =>
				"Generate only MultipleChoice tasks containing factual science questions, exactly four distinct options, and the correct option index.",

			GameMode.Language =>
				"Generate only Translation tasks asking users to translate English words into Hindi.",

			_ => throw new ArgumentException(
				$"Unsupported game mode: {gameMode}")
		};

		string expectedTaskType = gameMode switch
		{
			GameMode.Maths => "Calculation",
			GameMode.Science => "MultipleChoice",
			GameMode.Language => "Translation",
			_ => throw new ArgumentException(
				$"Unsupported game mode: {gameMode}")
		};

		string prompt =
			$"Generate exactly {taskCount} unique, age-appropriate educational " +
			$"tasks in English for the selected mode: {gameMode}. " +
			$"{taskInstructions} " +
			$"Every task must have taskType \"{expectedTaskType}\". " +
			"Do not generate tasks from any other subject or task type. " +
			"For MultipleChoice, provide exactly four distinct options and a zero-based CorrectOptionIndex. " +
			"For Calculation and Translation, Options must be an empty array and CorrectOptionIndex must be -1. " +
			"Ensure every correct answer matches the question and the selected option. " +
			"Return JSON matching the requested schema.";

		var schema = new
		{
			type = "OBJECT",
			properties = new
			{
				tasks = new
				{
					type = "ARRAY",
					items = new
					{
						type = "OBJECT",
						properties = new
						{
							taskType = new
							{
								type = "STRING",
								@enum = new[]
								{
									"Calculation",
									"MultipleChoice",
                                    "Translation"
								}
							},
							question = new { type = "STRING" },
							correctAnswer = new { type = "STRING" },
							options = new
							{
								type = "ARRAY",
								items = new { type = "STRING" }
							},
							correctOptionIndex = new { type = "INTEGER" }
						},
						required = new[]
						{
							"taskType",
							"question",
							"correctAnswer",
							"options",
                            "correctOptionIndex"
						},
						propertyOrdering = new[]
						{
							"taskType",
							"question",
							"correctAnswer",
							"options",
                            "correctOptionIndex"
						}
					}
				}
			},
			required = new[] { "tasks" }
		};

		var requestBody = new
		{
			model = Model,
			input = prompt,
			response_format = new
			{
				type = "text",
				mime_type = "application/json",
				schema
			}
		};

		string json = JsonSerializer.Serialize(requestBody);

		using var request = new HttpRequestMessage(
			HttpMethod.Post,
			"https://generativelanguage.googleapis.com/v1beta/interactions");

		request.Headers.Add("x-goog-api-key", apiKey);

		request.Content = new StringContent(
			json,
			Encoding.UTF8,
			"application/json");

		using HttpResponseMessage response =
			await Http.SendAsync(request);
		
		GD.Print($"GEMINI RESPONSE: {response}");
		string responseText =
			await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(
				$"Gemini API returned {(int)response.StatusCode} " +
				$"({response.StatusCode}): {responseText}");
		}

		// FIX: Extract text from the actual Interactions API response.
		// It may be inside steps -> model_output -> content -> text.
		string generatedJson = ExtractGeneratedJson(responseText);

		using JsonDocument taskDocument =
			JsonDocument.Parse(generatedJson);

		if (!taskDocument.RootElement.TryGetProperty(
				"tasks", out JsonElement taskArray) ||
			taskArray.ValueKind != JsonValueKind.Array)
		{
			throw new InvalidOperationException(
				"Gemini output does not contain a valid tasks array.");
		}

		if (taskArray.GetArrayLength() != taskCount)
		{
			throw new InvalidOperationException(
				$"Expected {taskCount} tasks, but Gemini returned " +
				$"{taskArray.GetArrayLength()}.");
		}

		var tasks = new List<GameTask>();
		var seenQuestions = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase);

		int id = 1;

		foreach (JsonElement item in taskArray.EnumerateArray())
		{
			string? type = item.GetProperty("taskType").GetString();
			string? question = item.GetProperty("question").GetString();
			string? answer = item.GetProperty("correctAnswer").GetString();

			string[] options = item.GetProperty("options")
				.EnumerateArray()
				.Select(option => option.GetString() ?? string.Empty)
				.ToArray();

			int correctIndex =
				item.GetProperty("correctOptionIndex").GetInt32();

			if (string.IsNullOrWhiteSpace(question) ||
				string.IsNullOrWhiteSpace(answer))
			{
				throw new InvalidOperationException(
					"Gemini returned an empty question or answer.");
			}

			if (!seenQuestions.Add(question.Trim()))
			{
				throw new InvalidOperationException(
					$"Gemini returned a duplicate question: {question}");
			}

			GameTask task;

			switch (type)
			{
				case "Calculation":
					if (gameMode != GameMode.Maths ||
						options.Length != 0 ||
						correctIndex != -1)
					{
						throw new InvalidOperationException(
							"Invalid calculation task data.");
					}

					task = new CalculationTask(id++, question, answer);
					break;

				case "MultipleChoice":
					if (gameMode != GameMode.Science ||
						options.Length != 4 ||
						correctIndex < 0 ||
						correctIndex >= options.Length ||
						options.Any(string.IsNullOrWhiteSpace) ||
						options.Distinct(
							StringComparer.OrdinalIgnoreCase).Count() != 4)
					{
						throw new InvalidOperationException(
							"Invalid multiple-choice task data.");
					}

					// Ensure the correct option matches the answer.
					if (!string.Equals(
							options[correctIndex].Trim(),
							answer.Trim(),
							StringComparison.OrdinalIgnoreCase))
					{
						throw new InvalidOperationException(
							"The correct answer does not match the selected option.");
					}

					task = new MultipleChoiceTask(
						id++, question, options, correctIndex);
					break;

				case "Translation":
					if (gameMode != GameMode.Language ||
						options.Length != 0 ||
						correctIndex != -1)
					{
						throw new InvalidOperationException(
							"Invalid translation task data.");
					}

					task = new TranslationTask(id++, question, answer);
					break;

				default:
					throw new InvalidOperationException(
						$"Unsupported task type: {type}");
			}

			tasks.Add(task);
		}

		GD.Print(
			$"Gemini generated {tasks.Count} valid {gameMode} tasks.");

		return tasks;
	}

	private static string ExtractGeneratedJson(string responseText)
	{
		using JsonDocument document =
			JsonDocument.Parse(responseText);

		JsonElement root = document.RootElement;

		// Support a root-level output_text if present.
		if (root.TryGetProperty(
				"output_text", out JsonElement outputText) &&
			outputText.ValueKind == JsonValueKind.String)
		{
			string? value = outputText.GetString();

			if (!string.IsNullOrWhiteSpace(value))
				return value;
		}

		// Interactions API response:
		// steps -> model_output -> content -> text
		if (root.TryGetProperty(
				"steps", out JsonElement steps) &&
			steps.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement step in steps.EnumerateArray())
			{
				if (!step.TryGetProperty(
						"type", out JsonElement stepType) ||
					stepType.GetString() != "model_output")
					continue;

				if (!step.TryGetProperty(
						"content", out JsonElement content) ||
					content.ValueKind != JsonValueKind.Array)
					continue;

				foreach (JsonElement item in content.EnumerateArray())
				{
					if (!item.TryGetProperty(
							"type", out JsonElement itemType) ||
						itemType.GetString() != "text")
						continue;

					if (item.TryGetProperty(
							"text", out JsonElement textElement) &&
						textElement.ValueKind == JsonValueKind.String)
					{
						string? value = textElement.GetString();

						if (!string.IsNullOrWhiteSpace(value))
							return value;
					}
				}
			}
		}

		string status = root.TryGetProperty(
			"status", out JsonElement statusElement)
				? statusElement.ToString()
				: "unknown";

		throw new InvalidOperationException(
			$"Gemini response contained no generated text. Status: {status}.");
	}
}
