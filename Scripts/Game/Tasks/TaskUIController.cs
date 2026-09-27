using Godot;

public partial class TaskUIController : CanvasLayer
{
	public static TaskUIController Instance { get; private set; }
	
	public bool IsTaskOpen { get; private set; }
	
	private Label titleLabel;
	private Label progressLabel;
	private Label questionLabel;

	private LineEdit answerInput;

	private VBoxContainer optionsContainer;

	private Button submitButton;
	private Button closeButton;

	private GameTask currentTask;
	private TaskStation currentStation;
	
	private NetworkTaskData currentNetworkTask;
	private bool isNetworkTask;

	private int selectedOption = -1;
	private int networkSelectedOption = -1;

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;
		
		titleLabel =
			GetNode<Label>(
                "Panel/VBoxContainer/TitleLabel"
			);

		progressLabel =
			GetNode<Label>(
                "Panel/VBoxContainer/ProgressLabel"
			);

		questionLabel =
			GetNode<Label>(
                "Panel/VBoxContainer/QuestionLabel"
			);

		answerInput =
			GetNode<LineEdit>(
                "Panel/VBoxContainer/AnswerInput"
			);

		optionsContainer =
			GetNode<VBoxContainer>(
                "Panel/VBoxContainer/OptionsContainer"
			);

		submitButton =
			GetNode<Button>(
                "Panel/VBoxContainer/SubmitButton"
			);

		closeButton =
			GetNode<Button>(
                "Panel/VBoxContainer/CloseButton"
			);

		submitButton.Pressed += OnSubmitPressed;
		closeButton.Pressed += OnClosePressed;

		HideTaskUI();
	}

	public void ShowTask(
		GameTask task,
		TaskStation station
	)
	{
		if (task == null)
		{
			GD.PrintErr("Cannot display a null task.");
			return;
		}

		// Safety check.
		if (task.IsCompleted)
		{
			GD.Print("This task is already completed.");
			return;
		}

		currentTask = task;
		currentStation = station;

		IsTaskOpen = true;
		Visible = true;

		titleLabel.Text = "TASK";

		questionLabel.Text =
			currentTask.Question;

		answerInput.Clear();

		ClearOptions();

		if (currentTask is MultipleChoiceTask multipleChoiceTask)
		{
			ShowMultipleChoiceTask(
				multipleChoiceTask
			);
		}
		else
		{
			ShowTextAnswerTask();
		}

		UpdateProgress();

		GD.Print(
			$"Task UI opened: {currentTask.Question}"
		);
	}
	
	public void ShowTask(
		NetworkTaskData task,
		TaskStation station
	)
	{
		if (task == null)
		{
			GD.PrintErr(
				"Cannot display a null network task."
			);

			return;
		}

		currentTask = null;
		currentNetworkTask = task;

		currentStation = station;
		isNetworkTask = true;

		IsTaskOpen = true;
		Visible = true;

		titleLabel.Text = "TASK";

		questionLabel.Text =
			task.Question;

		answerInput.Clear();

		ClearOptions();

		if (task.TaskType == TaskType.MultipleChoice &&
			task.Options != null)
		{
			ShowNetworkMultipleChoiceTask(task);
		}
		else
		{
			ShowNetworkTextAnswerTask();
		}

		UpdateNetworkProgress();

		GD.Print(
			$"Network task UI opened: {task.Question}"
		);
	}
	
	private void ShowMultipleChoiceTask(
		MultipleChoiceTask task
	)
	{
		answerInput.Visible = false;
		optionsContainer.Visible = true;

		for (int i = 0; i < task.Options.Length; i++)
		{
			int optionIndex = i;

			Button optionButton = new Button();

			optionButton.Text = task.Options[i];

			optionButton.Disabled = false;

			optionButton.Pressed +=
				() => SelectOption(optionIndex);

			optionsContainer.AddChild(
				optionButton
			);
		}
	}
	
	private void ShowNetworkMultipleChoiceTask(
		NetworkTaskData task
	)
	{
		answerInput.Visible = false;
		optionsContainer.Visible = true;

		for (int i = 0; i < task.Options.Length; i++)
		{
			int optionIndex = i;

			Button optionButton = new Button();

			optionButton.Text =
				task.Options[i];

			optionButton.Disabled = false;

			optionButton.Pressed +=
				() => OnNetworkOptionSelected(optionIndex);

			optionsContainer.AddChild(
				optionButton
			);
		}
	}

	private void ShowTextAnswerTask()
	{
		answerInput.Visible = true;
		optionsContainer.Visible = false;
	}
	
	private void ShowNetworkTextAnswerTask()
	{
		answerInput.Visible = true;
		optionsContainer.Visible = false;
	}
	
	private void OnNetworkOptionSelected(
		int optionIndex
	)
	{
		if (currentNetworkTask == null)
			return;

		networkSelectedOption = optionIndex;

		Button selectedButton =
			optionsContainer.GetChild<Button>(
				optionIndex
			);

		SetButtonColor(
			selectedButton,
			Colors.Yellow
		);

		SetOptionButtonsDisabled(true);

		GD.Print(
			$"Client selected option: {optionIndex}"
		);

		SubmitNetworkAnswer(
			optionIndex.ToString()
		);
	}
	
	private void SetOptionButtonsDisabled(bool disabled)
	{
		foreach (Node child in optionsContainer.GetChildren())
		{
			if (child is Button button)
			{
				button.Disabled = disabled;
			}
		}
	}
	
	private void SetButtonColor(
		Button button,
		Color color
	)
	{
		button.Modulate = color;
	}
	
	
	private async void SelectOption(int optionIndex)
	{
		if (currentTask == null)
			return;

		if (currentTask.IsCompleted)
			return;

		// Prevent multiple clicks while feedback is showing.
		SetOptionButtonsDisabled(true);

		selectedOption = optionIndex;

		Button selectedButton =
			optionsContainer.GetChild<Button>(optionIndex);

		bool correct =
			currentTask.SubmitAnswer(
				optionIndex.ToString()
			);

		if (correct)
		{
			// Correct → green
			SetButtonColor(
				selectedButton,
				Colors.Green
			);

			GD.Print("CORRECT OPTION!");

			// Keep green visible for 1 second.
			await ToSignal(
				GetTree().CreateTimer(1.0),
				SceneTreeTimer.SignalName.Timeout
			);

			OnTaskCompleted();
		}
		else
		{
			// Wrong → red
			SetButtonColor(
				selectedButton,
				Colors.Red
			);

			GD.Print("INCORRECT OPTION!");

			// Keep red visible for 0.7 seconds.
			await ToSignal(
				GetTree().CreateTimer(0.7),
				SceneTreeTimer.SignalName.Timeout
			);

			// Return button to normal.
			SetButtonColor(
				selectedButton,
				Colors.White
			);

			SetOptionButtonsDisabled(false);

			selectedOption = -1;
		}
	}

	private void OnSubmitPressed()
	{
		if (isNetworkTask)
		{
			SubmitNetworkTextAnswer();
			return;
		}
		
		if (currentTask == null)
			return;

		// Multiple-choice tasks are answered
		// immediately when an option is clicked.
		if (currentTask is MultipleChoiceTask)
			return;

		string answer = answerInput.Text.Trim();

		if (string.IsNullOrEmpty(answer))
		{
			GD.Print("Please enter an answer first.");
			return;
		}

		bool correct =
			currentTask.SubmitAnswer(answer);

		if (correct)
		{
			OnTaskCompleted();
		}
		else
		{
			GD.Print("INCORRECT ANSWER!");
		}
	}

	private void OnTaskCompleted()
	{
		GD.Print("TASK COMPLETED!");
		
		// HOST completed a task locally.
		if (NetworkManager.Instance.IsServer &&
			currentTask != null)
		{
			long playerId =
				Multiplayer.GetUniqueId();

			GameManager.Instance.RegisterTaskCompletion(
				playerId
			);
		}
		
		// Disable this station for this player.
		if (currentStation != null)
		{
			currentStation.DisableForPlayer();
		}

		UpdateProgress();

		HideTaskUI();
	}

	private void OnClosePressed()
	{
		GD.Print(
            "Task UI closed. Task remains incomplete."
		);

		HideTaskUI();
	}

	private void HideTaskUI()
	{
		IsTaskOpen = false;
		Visible = false;

		currentTask = null;
		currentStation = null;

		selectedOption = -1;

		answerInput.Clear();
		ClearOptions();
	}

	private void ClearOptions()
	{
		selectedOption = -1;

		foreach (Node child
			in optionsContainer.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void UpdateProgress()
	{
		if (TaskManager.Instance == null)
			return;

		long playerId =
			Multiplayer.GetUniqueId();

		int completed =
			TaskManager.Instance
				.GetCompletedTaskCount(playerId);

		int total =
			TaskManager.Instance
				.GetTotalTaskCount(playerId);

		progressLabel.Text =
			$"Tasks: {completed} / {total}";
	}
	
	private void UpdateNetworkProgress()
	{
		if (currentNetworkTask == null)
			return;

		long playerId =
			Multiplayer.GetUniqueId();

		int total =
			TaskManager.Instance
				.GetNetworkTasks(playerId)
				.Count;

		progressLabel.Text =
			$"Tasks: 0 / {total}";
	}
	
	public async void HandleNetworkAnswerResult(
		int taskId,
		bool correct
	)
	{
		if (currentNetworkTask == null)
			return;

		if (currentNetworkTask.TaskId != taskId)
			return;

		if (networkSelectedOption < 0)
			return;

		Button selectedButton =
			optionsContainer.GetChild<Button>(
				networkSelectedOption
			);

		if (correct)
		{
			SetButtonColor(
				selectedButton,
				Colors.Green
			);

			GD.Print(
				"NETWORK TASK CORRECT!"
			);

			await ToSignal(
				GetTree().CreateTimer(1.0),
				SceneTreeTimer.SignalName.Timeout
			);

			long playerId =
				Multiplayer.GetUniqueId();

			TaskManager.Instance.MarkNetworkTaskCompleted(
				playerId,
				taskId
			);

			OnTaskCompleted();
		}
		else
		{
			SetButtonColor(
				selectedButton,
				Colors.Red
			);

			GD.Print(
				"NETWORK TASK INCORRECT!"
			);

			await ToSignal(
				GetTree().CreateTimer(0.7),
				SceneTreeTimer.SignalName.Timeout
			);

			SetButtonColor(
				selectedButton,
				Colors.White
			);

			SetOptionButtonsDisabled(false);

			networkSelectedOption = -1;
		}
	}
	
	private void SubmitNetworkAnswer(string answer)
	{
		if (currentNetworkTask == null)
			return;

		if (NetworkManager.Instance == null)
		{
			GD.PrintErr("NetworkManager not found.");
			return;
		}

		GD.Print(
			$"Sending answer for network task " +
			$"{currentNetworkTask.TaskId}"
		);

		NetworkManager.Instance.SubmitTaskAnswer(
			currentNetworkTask.TaskId,
			answer
		);
	}
	
	private void SubmitNetworkTextAnswer()
	{
		if (currentNetworkTask == null)
			return;

		string answer =
			answerInput.Text.Trim();

		if (string.IsNullOrEmpty(answer))
		{
			GD.Print(
				"Please enter an answer first."
			);

			return;
		}

		SubmitNetworkAnswer(answer);
	}
}
