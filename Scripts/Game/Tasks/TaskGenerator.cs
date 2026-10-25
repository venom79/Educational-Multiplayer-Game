
using System;
using System.Collections.Generic;
using System.Linq;

public class TaskGenerator
{
	private int nextTaskId = 1;

	private readonly Random random = new Random();

	private readonly HashSet<string> usedQuestions =
		new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public List<GameTask> GenerateTasks(GameMode gameMode, int taskCount)
	{
		if (taskCount <= 0)
		{
			throw new ArgumentOutOfRangeException(nameof(taskCount));
		}

		TaskType[] allowedTypes = TaskTypeRules.GetAllowedTypes(gameMode);

		var tasks = new List<GameTask>();
		var questionsThisBatch = new HashSet<string>(
			StringComparer.OrdinalIgnoreCase);

		int attempts = 0;
		int maxAttempts = Math.Max(taskCount * 50, 100);

		while (tasks.Count < taskCount && attempts < maxAttempts)
		{
			attempts++;

			TaskType type = allowedTypes[random.Next(allowedTypes.Length)];
			GameTask task = GenerateTask(gameMode, type);

			if (task == null)
				continue;

			if (!questionsThisBatch.Add(task.Question))
				continue;

			tasks.Add(task);
			usedQuestions.Add(task.Question);
		}

		if (tasks.Count < taskCount)
		{
			throw new InvalidOperationException(
				$"Could only generate {tasks.Count} unique tasks for " +
				$"{gameMode}; {taskCount} were requested.");
		}

		return tasks;
	}

	private GameTask GenerateTask(GameMode mode, TaskType type)
	{
		int id = nextTaskId++;

		return type switch
		{
			TaskType.Calculation => GenerateCalculation(id, mode),
			TaskType.MultipleChoice => GenerateMultipleChoice(id, mode),
			TaskType.Translation => GenerateTranslation(id),
			TaskType.MatchTheFollowing => GenerateMatching(id, mode),
			_ => throw new ArgumentOutOfRangeException(
				nameof(type), type, "Unsupported task type.")
		};
	}

	private GameTask GenerateCalculation(int id, GameMode mode)
	{
		if (mode == GameMode.Maths)
		{
			int a = random.Next(10, 100);
			int b = random.Next(2, 20);

			switch (random.Next(3))
			{
				case 0:
					return new CalculationTask(
						id,
						$"What is {a} + {b}?",
						(a + b).ToString());

				case 1:
					if (b > a)
						(a, b) = (b, a);

					return new CalculationTask(
						id,
						$"What is {a} - {b}?",
						(a - b).ToString());

				default:
					int x = random.Next(2, 13);
					int y = random.Next(2, 11);

					return new CalculationTask(
						id,
						$"What is {x} × {y}?",
						(x * y).ToString());
			}
		}

		if (mode == GameMode.Science)
		{
			int mass = random.Next(2, 21);
			int acceleration = random.Next(2, 11);

			return new CalculationTask(
				id,
				$"Using force = mass × acceleration, calculate the force " +
				$"for a mass of {mass} kg and acceleration of " +
				$"{acceleration} m/s².",
				(mass * acceleration).ToString());
		}

		throw new InvalidOperationException(
			$"Calculation tasks are not allowed in {mode} mode.");
	}

	private GameTask GenerateMultipleChoice(int id, GameMode mode)
	{
		var questions = new List<MultipleChoiceQuestion>();

		if (mode == GameMode.Maths)
		{
			int a = random.Next(5, 50);
			int b = random.Next(5, 50);
			int answer = a + b;

			questions.Add(new MultipleChoiceQuestion(
				$"What is {a} + {b}?",
				answer.ToString(),
				MakeOptions(answer, answer + 2, answer - 3, answer + 5)));
			
			int n = random.Next(2, 13);
			int square = n * n;

			questions.Add(new MultipleChoiceQuestion(
				$"What is {n}²?",
				square.ToString(),
				MakeOptions(square, square + 1, square - 2, square + 5)));
		}
		else if (mode == GameMode.Science)
		{
			questions.Add(new MultipleChoiceQuestion(
				"Which gas do humans need for respiration?",
				"Oxygen",
				new[] { "Oxygen", "Helium", "Hydrogen", "Carbon Dioxide" }));

			questions.Add(new MultipleChoiceQuestion(
				"What is H₂O commonly known as?",
				"Water",
				new[] { "Oxygen", "Water", "Hydrogen", "Carbon Dioxide" }));

			questions.Add(new MultipleChoiceQuestion(
				"Which planet is known as the Red Planet?",
				"Mars",
				new[] { "Earth", "Mars", "Jupiter", "Venus" }));

			questions.Add(new MultipleChoiceQuestion(
				"Which force pulls objects toward Earth?",
				"Gravity",
				new[] { "Magnetism", "Friction", "Gravity", "Electricity" }));

			questions.Add(new MultipleChoiceQuestion(
				"Which organ pumps blood through the human body?",
				"Heart",
				new[] { "Lungs", "Brain", "Heart", "Kidneys" }));

			questions.Add(new MultipleChoiceQuestion(
				"Which part of a plant mainly absorbs water from soil?",
				"Root",
				new[] { "Flower", "Leaf", "Root", "Fruit" }));

			questions.Add(new MultipleChoiceQuestion(
				"How many planets are in our Solar System?",
				"8",
				new[] { "7", "8", "9", "10" }));

			questions.Add(new MultipleChoiceQuestion(
				"Which state of matter has a fixed shape?",
				"Solid",
				new[] { "Solid", "Liquid", "Gas", "Plasma" }));

			questions.Add(new MultipleChoiceQuestion(
				"What process do plants use to make food?",
				"Photosynthesis",
				new[] { "Photosynthesis", "Digestion", "Respiration", "Fermentation" }));
		}
		else if (mode == GameMode.Language)
		{
			questions.Add(new MultipleChoiceQuestion(
				"What is the Hindi word for 'Water'?",
				"पानी",
				new[] { "आग", "पानी", "घर", "किताब" }));

			questions.Add(new MultipleChoiceQuestion(
				"What is the Hindi word for 'Fire'?",
				"आग",
				new[] { "पानी", "आग", "दोस्त", "विद्यालय" }));

			questions.Add(new MultipleChoiceQuestion(
				"What is the Hindi word for 'Book'?",
				"किताब",
				new[] { "किताब", "घर", "आग", "पानी" }));

			questions.Add(new MultipleChoiceQuestion(
				"What is the Hindi word for 'House'?",
				"घर",
				new[] { "दोस्त", "पानी", "घर", "किताब" }));

			questions.Add(new MultipleChoiceQuestion(
				"What is the Hindi word for 'Friend'?",
				"दोस्त",
				new[] { "विद्यालय", "दोस्त", "आग", "घर" }));
		}

		if (questions.Count == 0)
		{
			throw new InvalidOperationException(
				$"No multiple-choice questions are defined for {mode}.");
		}

		MultipleChoiceQuestion selected =
			questions[random.Next(questions.Count)];

		return new MultipleChoiceTask(
			id,
			selected.Question,
			selected.Options,
			Array.IndexOf(selected.Options, selected.Answer));
	}

	private GameTask GenerateTranslation(int id)
	{
		var questions = new (string Question, string Answer)[]
		{
			("Translate 'Water' to Hindi.", "पानी"),
			("Translate 'Fire' to Hindi.", "आग"),
			("Translate 'Book' to Hindi.", "किताब"),
			("Translate 'House' to Hindi.", "घर"),
			("Translate 'Friend' to Hindi.", "दोस्त"),
			("Translate 'School' to Hindi.", "विद्यालय"),
			("Translate 'Sun' to Hindi.", "सूरज"),
			("Translate 'Moon' to Hindi.", "चाँद"),
			("Translate 'Tree' to Hindi.", "पेड़"),
			("Translate 'Flower' to Hindi.", "फूल")
		};

		var selected = questions[random.Next(questions.Length)];

		return new TranslationTask(
			id,
			selected.Question,
			selected.Answer);
	}

	private GameTask GenerateMatching(int id, GameMode mode)
	{
		List<(string Left, string Right)> pairs = mode switch
		{
			GameMode.Maths => new List<(string, string)>
			{
				("2 + 3", "5"),
				("4 × 3", "12"),
				("10 - 6", "4"),
				("5 × 5", "25"),
				("18 ÷ 3", "6"),
				("7 + 8", "15")
			},

			GameMode.Science => new List<(string, string)>
			{
				("H₂O", "Water"),
				("Mars", "Red Planet"),
				("Heart", "Pumps blood"),
				("Root", "Absorbs water"),
				("Gravity", "Attractive force"),
				("Oxygen", "Needed for respiration")
			},

			GameMode.Language => new List<(string, string)>
			{
				("Water", "पानी"),
				("Fire", "आग"),
				("Book", "किताब"),
				("House", "घर"),
				("Friend", "दोस्त"),
				("School", "विद्यालय")
			},

			_ => throw new ArgumentException(
				$"Unsupported game mode: {mode}")
		};

		// Select 3–6 pairs, then shuffle the right-side items.
		int pairCount = random.Next(3, pairs.Count + 1);

		pairs = pairs
			.OrderBy(_ => random.Next())
			.Take(pairCount)
			.ToList();

		string[] leftItems = pairs.Select(pair => pair.Left).ToArray();
		string[] rightItems = pairs.Select(pair => pair.Right).ToArray();

		// Track each right-side item by its original index.
		int[] rightOriginalIndices = Enumerable
			.Range(0, rightItems.Length)
			.ToArray();

		for (int i = rightItems.Length - 1; i > 0; i--)
		{
			int j = random.Next(i + 1);

			(rightItems[i], rightItems[j]) =
				(rightItems[j], rightItems[i]);

			(rightOriginalIndices[i], rightOriginalIndices[j]) =
				(rightOriginalIndices[j], rightOriginalIndices[i]);
		}

		int[] correctMatches = new int[leftItems.Length];

		for (int leftIndex = 0; leftIndex < pairs.Count; leftIndex++)
		{
			correctMatches[leftIndex] =
				Array.IndexOf(rightOriginalIndices, leftIndex);
		}

		return new MatchTheFollowingTask(
			id,
			"Match each item on the left with its correct pair on the right.",
			leftItems,
			rightItems,
			correctMatches);
	}

	private static string[] MakeOptions(
		int correct,
		int wrong1,
		int wrong2,
		int wrong3)
	{
		var options = new HashSet<string>
		{
			correct.ToString(),
			wrong1.ToString(),
			wrong2.ToString(),
			wrong3.ToString()
		};

		int candidate = correct + 1;

		while (options.Count < 4)
		{
			options.Add(candidate.ToString());
			candidate++;
		}

		return options
			.OrderBy(_ => Guid.NewGuid())
			.ToArray();
	}

	private sealed class MultipleChoiceQuestion
	{
		public string Question { get; }
		public string Answer { get; }
		public string[] Options { get; }

		public MultipleChoiceQuestion(
			string question,
			string answer,
			string[] options)
		{
			Question = question;
			Answer = answer;
			Options = options;
		}
	}
}
