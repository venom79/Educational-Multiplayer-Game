using System;
using System.Collections.Generic;

public class TaskGenerator
{
	private int nextTaskId = 1;

	private readonly Random random = new Random();
	
	private readonly HashSet<string> usedQuestions =
		new HashSet<string>();
	
	public List<GameTask> GenerateTasks(
		GameMode gameMode,
		int taskCount
	)
	{
		List<GameTask> tasks = new List<GameTask>();

		int attempts = 0;
		int maxAttempts = taskCount * 20;

		while (
			tasks.Count < taskCount &&
			attempts < maxAttempts
		)
		{
			attempts++;

			GameTask task = GenerateTask(gameMode);

			if (task == null)
				continue;

			if (usedQuestions.Contains(task.Question))
				continue;

			usedQuestions.Add(task.Question);
			tasks.Add(task);
		}

		return tasks;
	}

	private GameTask GenerateTask(GameMode gameMode)
	{
		int taskId = nextTaskId++;

		switch (gameMode)
		{
			case GameMode.Maths:
				return GenerateMathTask(taskId);

			case GameMode.Science:
				return GenerateScienceTask(taskId);

			case GameMode.Language:
				return GenerateLanguageTask(taskId);

			default:
				return null;
		}
	}

	private GameTask GenerateMathTask(int taskId)
	{
		int operation = random.Next(0, 3);

		switch (operation)
		{
			case 0:
				return GenerateAdditionTask(taskId);

			case 1:
				return GenerateSubtractionTask(taskId);

			default:
				return GenerateMultiplicationTask(taskId);
		}
	}

	private GameTask GenerateAdditionTask(int taskId)
	{
		int number1 = random.Next(10, 100);
		int number2 = random.Next(10, 100);

		int answer = number1 + number2;

		return new CalculationTask(
			taskId,
			$"What is {number1} + {number2}?",
			answer.ToString()
		);
	}

	private GameTask GenerateSubtractionTask(int taskId)
	{
		int number1 = random.Next(20, 100);
		int number2 = random.Next(1, number1);

		int answer = number1 - number2;

		return new CalculationTask(
			taskId,
			$"What is {number1} - {number2}?",
			answer.ToString()
		);
	}

	private GameTask GenerateMultiplicationTask(int taskId)
	{
		int number1 = random.Next(2, 20);
		int number2 = random.Next(2, 12);

		int answer = number1 * number2;

		return new CalculationTask(
			taskId,
			$"What is {number1} × {number2}?",
			answer.ToString()
		);
	}

	private GameTask GenerateScienceTask(int taskId)
	{
		int questionNumber = random.Next(0, 10);

		switch (questionNumber)
		{
			case 0:
				return new MultipleChoiceTask(
					taskId,
					"Which gas do humans need to breathe?",
					new string[]
					{
						"Oxygen",
						"Helium",
						"Hydrogen",
						"Carbon Dioxide"
					},
					0
				);

			case 1:
				return new MultipleChoiceTask(
					taskId,
					"What is H₂O commonly known as?",
					new string[]
					{
						"Oxygen",
						"Water",
						"Hydrogen",
						"Carbon Dioxide"
					},
					1
				);

			case 2:
				return new MultipleChoiceTask(
					taskId,
					"Which planet is known as the Red Planet?",
					new string[]
					{
						"Earth",
						"Mars",
						"Jupiter",
						"Venus"
					},
					1
				);

			case 3:
				return new MultipleChoiceTask(
					taskId,
					"What force pulls objects toward Earth?",
					new string[]
					{
						"Magnetism",
						"Friction",
						"Gravity",
						"Electricity"
					},
					2
				);

			case 4:
				return new MultipleChoiceTask(
					taskId,
					"Which organ pumps blood through the human body?",
					new string[]
					{
						"Lungs",
						"Brain",
						"Heart",
						"Kidneys"
					},
					2
				);

			case 5:
				return new MultipleChoiceTask(
					taskId,
					"Which part of a plant absorbs water from the soil?",
					new string[]
					{
						"Flower",
						"Leaf",
						"Root",
						"Fruit"
					},
					2
				);

			case 6:
				return new MultipleChoiceTask(
					taskId,
					"How many planets are in our Solar System?",
					new string[]
					{
						"7",
						"8",
						"9",
						"10"
					},
					1
				);

			case 7:
				return new MultipleChoiceTask(
					taskId,
					"Which star is closest to Earth?",
					new string[]
					{
						"Sirius",
						"Polaris",
						"The Sun",
						"Betelgeuse"
					},
					2
				);

			case 8:
				return new MultipleChoiceTask(
					taskId,
					"Which state of matter has a fixed shape?",
					new string[]
					{
						"Solid",
						"Liquid",
						"Gas",
						"Plasma"
					},
					0
				);

			default:
				return new MultipleChoiceTask(
					taskId,
					"What do plants use to make food?",
					new string[]
					{
						"Photosynthesis",
						"Digestion",
						"Respiration",
						"Fermentation"
					},
					0
				);
		}
	}

	private GameTask GenerateLanguageTask(int taskId)
	{
		int questionNumber = random.Next(0, 6);

		switch (questionNumber)
		{
			case 0:
				return new TranslationTask(
					taskId,
					"Translate 'Water' to Hindi.",
                    "पानी"
				);

			case 1:
				return new TranslationTask(
					taskId,
					"Translate 'Fire' to Hindi.",
                    "आग"
				);

			case 2:
				return new TranslationTask(
					taskId,
					"Translate 'Book' to Hindi.",
                    "किताब"
				);

			case 3:
				return new TranslationTask(
					taskId,
					"Translate 'House' to Hindi.",
                    "घर"
				);

			case 4:
				return new TranslationTask(
					taskId,
					"Translate 'Friend' to Hindi.",
                    "दोस्त"
				);

			default:
				return new TranslationTask(
					taskId,
					"Translate 'School' to Hindi.",
                    "विद्यालय"
				);
		}
	}
}
