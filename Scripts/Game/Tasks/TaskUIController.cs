
using Godot;
using System;
using System.Collections.Generic;

public partial class TaskUIController : CanvasLayer
{
	public static TaskUIController Instance { get; private set; }

	[Export]
	public PackedScene MultipleChoiceScene { get; set; }

	public bool IsTaskOpen { get; private set; }

	private Label titleLabel;
	private Label progressLabel;
	private Label questionLabel;
	private Label feedbackLabel;
	private Button closeButton;
	private Control taskContent;
	private VBoxContainer contentRoot;

	private LineEdit answerInput;
	private Button submitButton;
	private MultipleChoiceController multipleChoiceController;

	private GameTask currentTask;
	private TaskStation currentStation;
	private NetworkTaskData currentNetworkTask;

	private bool awaitingNetworkResult;
	private bool isNetworkTask;
	private bool isMatchingTask;

	private int selectedOption = -1;
	private int networkSelectedOption = -1;

	private readonly List<OptionButton> matchingDropdowns = new();

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		titleLabel =
			GetNode<Label>("Panel/MainLayout/Header/TitleLabel");

		progressLabel =
			GetNode<Label>("Panel/MainLayout/Header/ProgressLabel");

		closeButton =
			GetNode<Button>("Panel/MainLayout/Header/CloseButton");

		questionLabel =
			GetNode<Label>("Panel/MainLayout/QuestionLabel");

		taskContent =
			GetNode<Control>("Panel/MainLayout/TaskContent");

		feedbackLabel =
			GetNode<Label>("Panel/MainLayout/FeedbackLabel");

		closeButton.Pressed += OnClosePressed;

		contentRoot = new VBoxContainer
		{
			Name = "GeneratedTaskContent",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill
		};

		contentRoot.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		taskContent.AddChild(contentRoot);

		HideTaskUI();
	}

	// LOCAL TASK
	public void ShowTask(GameTask task, TaskStation station)
	{
		if (task == null || task.IsCompleted)
			return;

		currentTask = task;
		currentNetworkTask = null;
		currentStation = station;

		isNetworkTask = false;
		isMatchingTask = false;
		awaitingNetworkResult = false;
		selectedOption = -1;
		networkSelectedOption = -1;

		IsTaskOpen = true;
		Visible = true;

		titleLabel.Text = "TASK";
		questionLabel.Text = task.Question;
		feedbackLabel.Text = "";

		ClearTaskContent();

		if (task is MultipleChoiceTask mcTask)
		{
			ShowMultipleChoiceTask(mcTask.Options);
		}
		else if (task is MatchTheFollowingTask matchingTask)
		{
			isMatchingTask = true;
			ShowLocalMatchingTask(matchingTask);
		}
		else
		{
			ShowTextAnswerTask();
		}

		UpdateProgress();
	}

	// NETWORK TASK
	public void ShowTask(NetworkTaskData task, TaskStation station)
	{
		if (task == null)
			return;

		currentTask = null;
		currentNetworkTask = task;
		currentStation = station;

		isNetworkTask = true;
		isMatchingTask =
			task.TaskType == TaskType.MatchTheFollowing;

		awaitingNetworkResult = false;
		selectedOption = -1;
		networkSelectedOption = -1;

		IsTaskOpen = true;
		Visible = true;

		titleLabel.Text = "TASK";
		questionLabel.Text = task.Question;
		feedbackLabel.Text = "";

		ClearTaskContent();

		if (task.TaskType == TaskType.MultipleChoice)
		{
			ShowMultipleChoiceTask(task.Options);
		}
		else if (isMatchingTask)
		{
			ShowNetworkMatchingTask(task);
		}
		else
		{
			ShowTextAnswerTask();
		}

		UpdateNetworkProgress();
	}

	// MULTIPLE CHOICE
	private void ShowMultipleChoiceTask(string[] options)
	{
		if (MultipleChoiceScene == null)
		{
			GD.PrintErr(
                "MultipleChoiceScene is not assigned in the Inspector."
			);

			feedbackLabel.Text =
				"Multiple-choice scene is not configured.";

			return;
		}

		Control instance = MultipleChoiceScene.Instantiate<Control>();

		instance.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		instance.SizeFlagsVertical =
			Control.SizeFlags.ExpandFill;

		contentRoot.AddChild(instance);

		multipleChoiceController =
			instance as MultipleChoiceController;

		if (multipleChoiceController == null)
		{
			GD.PrintErr(
                "MultipleChoice.tscn root must use MultipleChoiceController.cs."
			);

			feedbackLabel.Text =
				"Multiple-choice scene setup error.";

			return;
		}

		multipleChoiceController.OptionSelected += OnOptionSelected;

		multipleChoiceController.SetOptions(
			options ?? Array.Empty<string>()
		);
	}

	private void OnOptionSelected(int optionIndex)
	{
		if (!IsTaskOpen || awaitingNetworkResult)
			return;

		if (isNetworkTask)
		{
			OnNetworkOptionSelected(optionIndex);
			return;
		}

		SelectOption(optionIndex);
	}

	private async void SelectOption(int optionIndex)
	{
		if (currentTask == null ||
			currentTask.IsCompleted ||
			multipleChoiceController == null)
		{
			return;
		}

		awaitingNetworkResult = true;
		selectedOption = optionIndex;

		multipleChoiceController.SetOptionsDisabled(true);

		bool correct = currentTask.SubmitAnswer(
			optionIndex.ToString()
		);

		multipleChoiceController.SetOptionFeedback(
			optionIndex,
			correct
		);

		feedbackLabel.Text = correct
			? "Correct!"
			: "Incorrect. Try again.";

		if (correct)
		{
			GD.Print("CORRECT OPTION!");

			await ToSignal(
				GetTree().CreateTimer(1.0),
				SceneTreeTimer.SignalName.Timeout
			);

			if (!IsTaskOpen || currentTask == null)
				return;

			OnTaskCompleted();
		}
		else
		{
			GD.Print("INCORRECT OPTION!");

			await ToSignal(
				GetTree().CreateTimer(0.7),
				SceneTreeTimer.SignalName.Timeout
			);

			if (!IsTaskOpen || currentTask == null)
				return;

			multipleChoiceController.ResetOption(optionIndex);
			multipleChoiceController.SetOptionsDisabled(false);

			selectedOption = -1;
			awaitingNetworkResult = false;
			feedbackLabel.Text = "";
		}
	}

	private void OnNetworkOptionSelected(int optionIndex)
	{
		if (currentNetworkTask == null ||
			awaitingNetworkResult ||
			!IsTaskOpen ||
			multipleChoiceController == null)
		{
			return;
		}

		awaitingNetworkResult = true;
		networkSelectedOption = optionIndex;

		multipleChoiceController.SetOptionsDisabled(true);

		feedbackLabel.Text = "Checking answer...";

		GD.Print($"Client selected option: {optionIndex}");

		// The server validates the answer.
		SubmitNetworkAnswer(optionIndex.ToString());
	}

	// TEXT ANSWERS
	private void ShowTextAnswerTask()
	{
		answerInput = new LineEdit
		{
			Name = "AnswerInput",
			PlaceholderText = "Enter your answer",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		contentRoot.AddChild(answerInput);

		submitButton = new Button
		{
			Name = "SubmitButton",
			Text = "Submit",
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
		};

		submitButton.Pressed += OnSubmitPressed;
		contentRoot.AddChild(submitButton);
	}

	// SUBMIT TEXT OR MATCHING ANSWERS
	private void OnSubmitPressed()
	{
		if (!IsTaskOpen)
			return;

		if (isMatchingTask)
		{
			string matchingAnswer = BuildMatchingAnswer();

			if (string.IsNullOrEmpty(matchingAnswer))
			{
				feedbackLabel.Text =
					"Please select a match for every item.";

				return;
			}

			if (isNetworkTask)
			{
				awaitingNetworkResult = true;
				SetMatchingDropdownsDisabled(true);

				feedbackLabel.Text = "Checking answers...";

				SubmitNetworkAnswer(matchingAnswer);
			}
			else if (currentTask != null)
			{
				bool correct =
					currentTask.SubmitAnswer(matchingAnswer);

				if (correct)
				{
					OnTaskCompleted();
				}
				else
				{
					feedbackLabel.Text =
						"Incorrect matches. Try again.";
				}
			}

			return;
		}

		string answer = answerInput?.Text.Trim() ?? "";

		if (string.IsNullOrEmpty(answer))
		{
			feedbackLabel.Text =
				"Please enter an answer first.";

			return;
		}

		if (isNetworkTask)
		{
			awaitingNetworkResult = true;

			if (submitButton != null)
				submitButton.Disabled = true;

			feedbackLabel.Text = "Checking answer...";

			SubmitNetworkAnswer(answer);
			return;
		}

		if (currentTask == null)
			return;

		bool answerCorrect =
			currentTask.SubmitAnswer(answer);

		if (answerCorrect)
		{
			feedbackLabel.Text = "Correct!";
			OnTaskCompleted();
		}
		else
		{
			feedbackLabel.Text =
				"Incorrect answer. Try again.";
		}
	}

	// COMPLETE TASK
	private void OnTaskCompleted()
	{
		GD.Print("TASK COMPLETED!");

		if (NetworkManager.Instance != null &&
			NetworkManager.Instance.IsServer &&
			currentTask != null &&
			GameManager.Instance != null)
		{
			long playerId = Multiplayer.GetUniqueId();

			GameManager.Instance.RegisterTaskCompletion(playerId);
		}

		if (currentStation != null)
			currentStation.DisableForPlayer();

		if (isNetworkTask)
			UpdateNetworkProgress();
		else
			UpdateProgress();

		HideTaskUI();
	}

	private void OnClosePressed()
	{
		GD.Print("Task UI closed. Task remains incomplete.");
		HideTaskUI();
	}

	private void HideTaskUI()
	{
		IsTaskOpen = false;
		Visible = false;

		currentTask = null;
		currentNetworkTask = null;
		currentStation = null;

		isNetworkTask = false;
		isMatchingTask = false;
		awaitingNetworkResult = false;

		selectedOption = -1;
		networkSelectedOption = -1;

		matchingDropdowns.Clear();

		if (feedbackLabel != null)
			feedbackLabel.Text = "";

		ClearTaskContent();
	}

	private void ClearTaskContent()
	{
		if (contentRoot == null)
			return;

		foreach (Node child in contentRoot.GetChildren())
		{
			contentRoot.RemoveChild(child);
			child.QueueFree();
		}

		answerInput = null;
		submitButton = null;
		multipleChoiceController = null;

		matchingDropdowns.Clear();
	}

	// PROGRESS
	private void UpdateProgress()
	{
		if (TaskManager.Instance == null)
			return;

		long playerId = Multiplayer.GetUniqueId();

		int completed =
			TaskManager.Instance.GetCompletedTaskCount(playerId);

		int total =
			TaskManager.Instance.GetTotalTaskCount(playerId);

		progressLabel.Text = $"Tasks: {completed} / {total}";
	}

	private void UpdateNetworkProgress()
	{
		if (TaskManager.Instance == null)
			return;

		long playerId = Multiplayer.GetUniqueId();

		int completed =
			TaskManager.Instance.GetNetworkCompletedTaskCount(playerId);

		int total =
			TaskManager.Instance.GetNetworkTotalTaskCount(playerId);

		progressLabel.Text = $"Tasks: {completed} / {total}";
	}

	// SERVER ANSWER RESULT
	public async void HandleNetworkAnswerResult(
		int taskId,
		bool correct
	)
	{
		if (currentNetworkTask == null ||
			currentNetworkTask.TaskId != taskId ||
			!awaitingNetworkResult ||
			!IsTaskOpen)
		{
			return;
		}

		// MATCHING TASK RESULT
		if (isMatchingTask)
		{
			if (correct)
			{
				feedbackLabel.Text = "Correct!";

				await ToSignal(
					GetTree().CreateTimer(1.0),
					SceneTreeTimer.SignalName.Timeout
				);

				if (!IsCurrentNetworkTask(taskId))
					return;

				int localPlayerId = Multiplayer.GetUniqueId();

				TaskManager.Instance.MarkNetworkTaskCompleted(
					localPlayerId,
					taskId
				);

				OnTaskCompleted();
			}
			else
			{
				feedbackLabel.Text =
					"Incorrect matches. Try again.";

				await ToSignal(
					GetTree().CreateTimer(0.7),
					SceneTreeTimer.SignalName.Timeout
				);

				if (!IsCurrentNetworkTask(taskId))
					return;

				awaitingNetworkResult = false;
				SetMatchingDropdownsDisabled(false);
			}

			return;
		}

		// MULTIPLE-CHOICE RESULT
		if (multipleChoiceController != null &&
			networkSelectedOption >= 0)
		{
			int index = networkSelectedOption;

			if (correct)
			{
				feedbackLabel.Text = "Correct!";

				multipleChoiceController.SetOptionFeedback(
					index,
					true
				);

				await ToSignal(
					GetTree().CreateTimer(1.0),
					SceneTreeTimer.SignalName.Timeout
				);

				if (!IsCurrentNetworkTask(taskId))
					return;

				int localPlayerId = Multiplayer.GetUniqueId();

				TaskManager.Instance.MarkNetworkTaskCompleted(
					localPlayerId,
					taskId
				);

				OnTaskCompleted();
			}
			else
			{
				feedbackLabel.Text =
					"Incorrect. Try again.";

				multipleChoiceController.SetOptionFeedback(
					index,
					false
				);

				await ToSignal(
					GetTree().CreateTimer(0.7),
					SceneTreeTimer.SignalName.Timeout
				);

				if (!IsCurrentNetworkTask(taskId))
					return;

				multipleChoiceController.ResetOption(index);
				multipleChoiceController.SetOptionsDisabled(false);

				networkSelectedOption = -1;
				awaitingNetworkResult = false;
				feedbackLabel.Text = "";
			}

			return;
		}

		// TEXT ANSWER RESULT
		if (correct)
		{
			feedbackLabel.Text = "Correct!";

			await ToSignal(
				GetTree().CreateTimer(0.8),
				SceneTreeTimer.SignalName.Timeout
			);

			if (!IsCurrentNetworkTask(taskId))
				return;

			int localPlayerId = Multiplayer.GetUniqueId();

			TaskManager.Instance.MarkNetworkTaskCompleted(
				localPlayerId,
				taskId
			);

			OnTaskCompleted();
		}
		else
		{
			feedbackLabel.Text =
				"Incorrect answer. Try again.";

			await ToSignal(
				GetTree().CreateTimer(0.7),
				SceneTreeTimer.SignalName.Timeout
			);

			if (!IsCurrentNetworkTask(taskId))
				return;

			awaitingNetworkResult = false;

			if (submitButton != null)
				submitButton.Disabled = false;
		}
	}

	private bool IsCurrentNetworkTask(int taskId)
	{
		return IsTaskOpen &&
			   currentNetworkTask != null &&
			   currentNetworkTask.TaskId == taskId;
	}

	private void SubmitNetworkAnswer(string answer)
	{
		if (currentNetworkTask == null)
			return;

		if (NetworkManager.Instance == null)
		{
			GD.PrintErr("NetworkManager not found.");

			awaitingNetworkResult = false;

			if (submitButton != null)
				submitButton.Disabled = false;

			return;
		}

		GD.Print(
			$"Sending answer for network task {currentNetworkTask.TaskId}"
		);

		NetworkManager.Instance.SubmitTaskAnswer(
			currentNetworkTask.TaskId,
			answer
		);
	}

	// LOCAL MATCHING TASK
	private void ShowLocalMatchingTask(
		MatchTheFollowingTask task
	)
	{
		if (task.LeftItems == null || task.RightItems == null)
			return;

		for (int i = 0; i < task.LeftItems.Length; i++)
		{
			HBoxContainer row = new HBoxContainer();

			Label leftLabel = new Label
			{
				Text = task.LeftItems[i],
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};

			OptionButton dropdown =
				CreateMatchingDropdown(task.RightItems);

			row.AddChild(leftLabel);
			row.AddChild(dropdown);
			contentRoot.AddChild(row);

			matchingDropdowns.Add(dropdown);
		}

		AddMatchingSubmitButton();
	}

	// NETWORK MATCHING TASK
	private void ShowNetworkMatchingTask(NetworkTaskData task)
	{
		if (task.LeftItems == null ||
			task.RightItems == null ||
			task.LeftItems.Length == 0 ||
			task.RightItems.Length == 0)
		{
			GD.PrintErr("Matching task has no items.");
			feedbackLabel.Text = "Matching task has no items.";
			return;
		}

		for (int i = 0; i < task.LeftItems.Length; i++)
		{
			HBoxContainer row = new HBoxContainer();

			Label leftLabel = new Label
			{
				Text = task.LeftItems[i],
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
			};

			OptionButton dropdown =
				CreateMatchingDropdown(task.RightItems);

			row.AddChild(leftLabel);
			row.AddChild(dropdown);
			contentRoot.AddChild(row);

			matchingDropdowns.Add(dropdown);
		}

		AddMatchingSubmitButton();
	}

	private OptionButton CreateMatchingDropdown(
		string[] rightItems
	)
	{
		OptionButton dropdown = new OptionButton
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		dropdown.AddItem("Select match", 0);

		for (int i = 0; i < rightItems.Length; i++)
			dropdown.AddItem(rightItems[i], i + 1);

		dropdown.Select(0);

		return dropdown;
	}

	private void AddMatchingSubmitButton()
	{
		submitButton = new Button
		{
			Text = "Submit",
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
		};

		submitButton.Pressed += OnSubmitPressed;
		contentRoot.AddChild(submitButton);
	}

	private string BuildMatchingAnswer()
	{
		List<string> selectedIndices = new();

		foreach (OptionButton dropdown in matchingDropdowns)
		{
			// Zero means no match has been selected.
			if (dropdown.Selected <= 0)
				return "";

			// Csonvert to zero-based indices.
			selectedIndices.Add(
				(dropdown.Selected - 1).ToString()
			);
		}

		return string.Join(",", selectedIndices);
	}

	private void SetMatchingDropdownsDisabled(bool disabled)
	{
		foreach (OptionButton dropdown in matchingDropdowns)
			dropdown.Disabled = disabled;
	}
}
