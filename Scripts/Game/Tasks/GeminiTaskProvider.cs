
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

	private static string? GetApiKey()
	{
		string? apiKey =
			System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");

		if (!string.IsNullOrWhiteSpace(apiKey))
			return apiKey.Trim();

		string envPath = ProjectSettings.GlobalizePath("res://.env");

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
				"GEMINI_API_KEY is missing. Check your environment " +
				"variable or project-root .env file.");
		}

		// --------------------------------------------------
		// MODE-SPECIFIC INSTRUCTIONS
		// --------------------------------------------------

		string taskInstructions = gameMode switch
		{
			GameMode.Maths =>
				"Generate educational maths tasks using calculations, " +
				"multiple-choice questions, and matching mathematical " +
				"expressions, fractions, formulas, numbers, and concepts.",

			GameMode.Science =>
				"Generate educational science tasks using factual " +
				"multiple-choice questions, calculations involving " +
				"scientific concepts, and matching scientific terms, " +
				"concepts, processes, and definitions.",

			GameMode.Language =>
				"Generate educational language tasks involving English " +
				"and Hindi vocabulary, multiple-choice questions, " +
				"translations, meanings, and matching words with meanings.",

			_ => throw new ArgumentException(
				$"Unsupported game mode: {gameMode}")
		};

		// --------------------------------------------------
		// PROMPT
		// --------------------------------------------------

		string prompt =
			$"Generate exactly {taskCount} unique educational tasks " +
			$"for the {gameMode} mode. " +
			$"{taskInstructions} " +

			"Make questions moderately challenging, age-appropriate, " +
			"clear, and solvable. Require some reasoning or application " +
			"of knowledge. " +

			"Use a varied mix of eligible task types whenever possible. " +

			"Maths allows MultipleChoice, Calculation, and MatchTheFollowing. " +
			"Science allows MultipleChoice, Calculation, and MatchTheFollowing. " +
			"Language allows MultipleChoice, Translation, and MatchTheFollowing. " +

			"Never generate task types outside the selected mode's " +
			"allowed types. " +

			"For Calculation, put the expected answer in correctAnswer. " +

			"For Translation, put the expected translation in correctAnswer. " +

			"For MultipleChoice, provide exactly four distinct options. " +
			"correctOptionIndex must be the zero-based index of the " +
			"correct option, and correctAnswer must match that option. " +

			"For MatchTheFollowing, provide between 3 and 6 leftItems " +
			"and the same number of rightItems. Shuffle rightItems so " +
			"the correct pairs are not necessarily in the same order. " +
			"correctMatches must contain one zero-based right-item index " +
			"for each left item, in left-item order. Each right item " +
			"must be used exactly once. " +

			"For matching tasks, correctAnswer must be an empty string, " +
			"options must be an empty array, and correctOptionIndex " +
			"must be -1. " +

			"For non-matching tasks, leftItems, rightItems, and " +
			"correctMatches must be empty arrays. " +

			"For non-multiple-choice tasks, options must be empty and " +
			"correctOptionIndex must be -1. " +

			"Never include the correct answer in the question text. " +
			"Ensure all answers and matching pairs are correct. " +
			"Return JSON matching the requested schema.";

		// --------------------------------------------------
		// JSON SCHEMA
		// --------------------------------------------------

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
									"Translation",
                                    "MatchTheFollowing"
								}
							},
							question = new
							{
								type = "STRING"
							},
							correctAnswer = new
							{
								type = "STRING"
							},
							options = new
							{
								type = "ARRAY",
								items = new { type = "STRING" }
							},
							correctOptionIndex = new
							{
								type = "INTEGER"
							},
							leftItems = new
							{
								type = "ARRAY",
								items = new { type = "STRING" }
							},
							rightItems = new
							{
								type = "ARRAY",
								items = new { type = "STRING" }
							},
							correctMatches = new
							{
								type = "ARRAY",
								items = new { type = "INTEGER" }
							}
						},
						required = new[]
						{
							"taskType",
							"question",
							"correctAnswer",
							"options",
							"correctOptionIndex",
							"leftItems",
							"rightItems",
                            "correctMatches"
						},
						propertyOrdering = new[]
						{
							"taskType",
							"question",
							"correctAnswer",
							"options",
							"correctOptionIndex",
							"leftItems",
							"rightItems",
                            "correctMatches"
						}
					}
				}
			},
			required = new[] { "tasks" }
		};

		// --------------------------------------------------
		// API REQUEST
		// --------------------------------------------------

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
			"https://generativelanguage.googleapis.com/v1beta/test");
			//"https://generativelanguage.googleapis.com/v1beta/interactions");

		request.Headers.Add("x-goog-api-key", apiKey);

		request.Content = new StringContent(
			json,
			Encoding.UTF8,
			"application/json");

		using HttpResponseMessage response = await Http.SendAsync(request);

		string responseText = await response.Content.ReadAsStringAsync();

		GD.Print($"Gemini HTTP status: {(int)response.StatusCode}");

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(
				$"Gemini API returned {(int)response.StatusCode} " +
				$"({response.StatusCode}): {responseText}");
		}

		// --------------------------------------------------
		// EXTRACT GENERATED JSON
		// --------------------------------------------------

		string generatedJson = ExtractGeneratedJson(responseText);

		using JsonDocument taskDocument = JsonDocument.Parse(generatedJson);

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

		// --------------------------------------------------
		// VALIDATE AND CREATE TASKS
		// --------------------------------------------------

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

			string[] leftItems = item.GetProperty("leftItems")
				.EnumerateArray()
				.Select(element => element.GetString() ?? string.Empty)
				.ToArray();

			string[] rightItems = item.GetProperty("rightItems")
				.EnumerateArray()
				.Select(element => element.GetString() ?? string.Empty)
				.ToArray();

			int[] correctMatches = item.GetProperty("correctMatches")
				.EnumerateArray()
				.Select(element => element.GetInt32())
				.ToArray();

			if (string.IsNullOrWhiteSpace(question))
			{
				throw new InvalidOperationException(
					"Gemini returned an empty question.");
			}

			if (string.IsNullOrWhiteSpace(type))
			{
				throw new InvalidOperationException(
					"Gemini returned an empty task type.");
			}

			if (type != "MatchTheFollowing" &&
				string.IsNullOrWhiteSpace(answer))
			{
				throw new InvalidOperationException(
					"Gemini returned an empty answer.");
			}

			if (!seenQuestions.Add(question.Trim()))
			{
				throw new InvalidOperationException(
					$"Gemini returned a duplicate question: {question}");
			}

			if (type != "MatchTheFollowing" &&
				(leftItems.Length != 0 ||
				 rightItems.Length != 0 ||
				 correctMatches.Length != 0))
			{
				throw new InvalidOperationException(
					"A non-matching task contains matching data.");
			}

			GameTask task;

			switch (type)
			{
				case "Calculation":
				{
					if (!TaskTypeRules.IsAllowed(
							gameMode, TaskType.Calculation) ||
						options.Length != 0 ||
						correctIndex != -1)
					{
						throw new InvalidOperationException(
							$"Invalid calculation task data for {gameMode}.");
					}

					task = new CalculationTask(
						id++,
						question,
						answer!);

					break;
				}

				case "MultipleChoice":
				{
					if (!TaskTypeRules.IsAllowed(
							gameMode, TaskType.MultipleChoice) ||
						options.Length != 4 ||
						correctIndex < 0 ||
						correctIndex >= options.Length ||
						options.Any(string.IsNullOrWhiteSpace) ||
						options.Distinct(
							StringComparer.OrdinalIgnoreCase).Count() != 4)
					{
						throw new InvalidOperationException(
							$"Invalid multiple-choice task data for {gameMode}.");
					}

					if (!string.Equals(
							options[correctIndex].Trim(),
							answer!.Trim(),
							StringComparison.OrdinalIgnoreCase))
					{
						throw new InvalidOperationException(
							"The correct answer does not match " +
							"the selected option.");
					}

					task = new MultipleChoiceTask(
						id++,
						question,
						options,
						correctIndex);

					break;
				}

				case "Translation":
				{
					if (!TaskTypeRules.IsAllowed(
							gameMode, TaskType.Translation) ||
						options.Length != 0 ||
						correctIndex != -1)
					{
						throw new InvalidOperationException(
							$"Invalid translation task data for {gameMode}.");
					}

					task = new TranslationTask(
						id++,
						question,
						answer!);

					break;
				}

				case "MatchTheFollowing":
				{
					if (!TaskTypeRules.IsAllowed(
							gameMode, TaskType.MatchTheFollowing))
					{
						throw new InvalidOperationException(
							$"Matching tasks are not supported in {gameMode}.");
					}

					if (leftItems.Length < 3 ||
						leftItems.Length > 6 ||
						rightItems.Length != leftItems.Length ||
						correctMatches.Length != leftItems.Length)
					{
						throw new InvalidOperationException(
							"Matching task must contain 3 to 6 complete pairs.");
					}

					if (leftItems.Any(string.IsNullOrWhiteSpace) ||
						rightItems.Any(string.IsNullOrWhiteSpace))
					{
						throw new InvalidOperationException(
							"Matching task contains an empty item.");
					}

					if (leftItems.Distinct(
							StringComparer.OrdinalIgnoreCase).Count()
						!= leftItems.Length)
					{
						throw new InvalidOperationException(
							"Matching task contains duplicate left items.");
					}

					if (rightItems.Distinct(
							StringComparer.OrdinalIgnoreCase).Count()
						!= rightItems.Length)
					{
						throw new InvalidOperationException(
							"Matching task contains duplicate right items.");
					}

					if (correctMatches.Any(
							index => index < 0 ||
									 index >= rightItems.Length))
					{
						throw new InvalidOperationException(
							"Matching task contains an invalid index.");
					}

					if (correctMatches.Distinct().Count()
						!= correctMatches.Length)
					{
						throw new InvalidOperationException(
							"Every right-side item must be used exactly once.");
					}

					if (options.Length != 0 || correctIndex != -1)
					{
						throw new InvalidOperationException(
							"Matching task contains multiple-choice data.");
					}

					if (!string.IsNullOrEmpty(answer))
					{
						throw new InvalidOperationException(
							"Matching task must have an empty correctAnswer.");
					}

					task = new MatchTheFollowingTask(
						id++,
						question,
						leftItems,
						rightItems,
						correctMatches);

					break;
				}

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

	// --------------------------------------------------
	// GEMINI INTERACTIONS API RESPONSE EXTRACTION
	// --------------------------------------------------

	private static string ExtractGeneratedJson(string responseText)
	{
		using JsonDocument document = JsonDocument.Parse(responseText);

		JsonElement root = document.RootElement;

		if (root.TryGetProperty(
				"output_text", out JsonElement outputText) &&
			outputText.ValueKind == JsonValueKind.String)
		{
			string? value = outputText.GetString();

			if (!string.IsNullOrWhiteSpace(value))
				return value;
		}

		if (root.TryGetProperty(
				"steps", out JsonElement steps) &&
			steps.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement step in steps.EnumerateArray())
			{
				if (!step.TryGetProperty(
						"type", out JsonElement stepType) ||
					stepType.GetString() != "model_output")
				{
					continue;
				}

				if (!step.TryGetProperty(
						"content", out JsonElement content) ||
					content.ValueKind != JsonValueKind.Array)
				{
					continue;
				}

				foreach (JsonElement item in content.EnumerateArray())
				{
					if (!item.TryGetProperty(
							"type", out JsonElement itemType) ||
						itemType.GetString() != "text")
					{
						continue;
					}

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
