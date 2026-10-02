using Godot;

public partial class GameUIController : CanvasLayer
{
	private Label timerLabel;

	private Label playerProgressLabel;
	private ProgressBar playerProgressBar;

	private Label teamProgressLabel;
	private ProgressBar teamProgressBar;

	public override void _Ready()
	{
		timerLabel =
			GetNode<Label>("TimerLabel");

		playerProgressLabel =
			GetNode<Label>("PlayerProgressLabel");

		playerProgressBar =
			GetNode<ProgressBar>("PlayerProgressBar");

		teamProgressLabel =
			GetNode<Label>("TeamProgressLabel");

		teamProgressBar =
			GetNode<ProgressBar>("TeamProgressBar");

		SetupUI();

		UpdateTimer();
		UpdateProgress();
	}

	public override void _Process(double delta)
	{
		UpdateTimer();
		UpdateProgress();
	}

	private void SetupUI()
	{
		// -------------------------
		// TIMER
		// -------------------------

		timerLabel.Position =
			new Vector2(40, 30);

		timerLabel.AddThemeFontSizeOverride(
			"font_size",
			32
		);


		// -------------------------
		// PLAYER PROGRESS
		// -------------------------

		playerProgressLabel.Position =
			new Vector2(40, 85);

		playerProgressLabel.AddThemeFontSizeOverride(
			"font_size",
			20
		);

		playerProgressBar.Position =
			new Vector2(40, 115);

		playerProgressBar.Size =
			new Vector2(250, 25);


		// -------------------------
		// TEAM PROGRESS
		// -------------------------

		teamProgressLabel.Position =
			new Vector2(40, 155);

		teamProgressLabel.AddThemeFontSizeOverride(
			"font_size",
			20
		);

		teamProgressBar.Position =
			new Vector2(40, 185);

		teamProgressBar.Size =
			new Vector2(250, 25);
	}

	private void UpdateTimer()
	{
		if (GameManager.Instance == null)
			return;

		double remaining =
			GameManager.Instance.TimeRemaining;

		int totalSeconds =
			Mathf.CeilToInt((float)remaining);

		int minutes =
			totalSeconds / 60;

		int seconds =
			totalSeconds % 60;

		timerLabel.Text =
			$"{minutes:00}:{seconds:00}";
	}

	private void UpdateProgress()
	{
		if (GameManager.Instance == null)
			return;

		int playerCompleted =
			GameManager.Instance.PlayerCompletedTasks;

		int playerTotal =
			GameManager.Instance.PlayerTotalTasks;

		int teamCompleted =
			GameManager.Instance.SynchronizedTeamCompletedTasks;

		int teamTotal =
			GameManager.Instance.SynchronizedTeamTotalTasks;


		playerProgressLabel.Text =
			$"YOUR TASKS  {playerCompleted} / {playerTotal}";

		playerProgressBar.MaxValue =
			playerTotal;

		playerProgressBar.Value =
			playerCompleted;


		teamProgressLabel.Text =
			$"TEAM PROGRESS  {teamCompleted} / {teamTotal}";

		teamProgressBar.MaxValue =
			teamTotal;

		teamProgressBar.Value =
			teamCompleted;
	}
}
