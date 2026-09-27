using Godot;

public partial class GameUIController : CanvasLayer
{
	private Label timerLabel;

	private Label playerProgressLabel;
	private ProgressBar playerProgressBar;

	private Label teamProgressLabel;
	private ProgressBar teamProgressBar;

	private Control resultPanel;
	private Label resultTitleLabel;
	private Label resultStatsLabel;
	private Button continueButton;

	private bool resultShown = false;

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

		resultPanel =
			GetNode<Control>("ResultPanel");

		resultTitleLabel =
			GetNode<Label>(
				"ResultPanel/ResultTitleLabel"
			);

		resultStatsLabel =
			GetNode<Label>(
				"ResultPanel/ResultStatsLabel"
			);

		continueButton =
			GetNode<Button>(
				"ResultPanel/ContinueButton"
			);

		SetupUI();

		resultPanel.Visible = false;

		UpdateTimer();
		UpdateProgress();
	}

	public override void _Process(double delta)
	{
		UpdateTimer();
		UpdateProgress();
		UpdateGameResult();
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


		// -------------------------
		// RESULT PANEL
		// -------------------------

		resultPanel.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		resultPanel.MouseFilter =
			Control.MouseFilterEnum.Stop;


		// -------------------------
		// RESULT TITLE
		// -------------------------

		resultTitleLabel.Position =
			new Vector2(0, 180);

		resultTitleLabel.Size =
			new Vector2(1152, 80);

		resultTitleLabel.HorizontalAlignment =
			HorizontalAlignment.Center;

		resultTitleLabel.AddThemeFontSizeOverride(
			"font_size",
			48
		);


		// -------------------------
		// RESULT STATS
		// -------------------------

		resultStatsLabel.Position =
			new Vector2(0, 280);

		resultStatsLabel.Size =
			new Vector2(1152, 100);

		resultStatsLabel.HorizontalAlignment =
			HorizontalAlignment.Center;

		resultStatsLabel.AddThemeFontSizeOverride(
			"font_size",
			24
		);


		// -------------------------
		// CONTINUE BUTTON
		// -------------------------

		continueButton.Position =
			new Vector2(476, 420);

		continueButton.Size =
			new Vector2(200, 60);

		continueButton.Text =
			"Continue";

		continueButton.AddThemeFontSizeOverride(
			"font_size",
			22
		);

		continueButton.Pressed +=
			OnContinuePressed;
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

	private void UpdateGameResult()
	{
		if (GameManager.Instance == null)
			return;

		GameState currentState =
			GameSession.Instance.CurrentState;

		if (
			currentState != GameState.Victory &&
			currentState != GameState.AliensWin
		)
		{
			return;
		}

		if (resultShown)
			return;

		ShowResult(currentState);
	}

	private void ShowResult(GameState result)
	{
		resultShown = true;

		resultPanel.Visible = true;

		int completed =
			GameManager.Instance.SynchronizedTeamCompletedTasks;

		int total =
			GameManager.Instance.SynchronizedTeamTotalTasks;

		double remaining =
			GameManager.Instance.TimeRemaining;

		int totalSeconds =
			Mathf.CeilToInt((float)remaining);

		int minutes =
			totalSeconds / 60;

		int seconds =
			totalSeconds % 60;

		if (result == GameState.Victory)
		{
			resultTitleLabel.Text =
				"TEAM VICTORY!";

			resultStatsLabel.Text =
				$"All tasks completed!\n\n" +
				$"Team Tasks: {completed} / {total}\n" +
				$"Time Remaining: {minutes:00}:{seconds:00}";
		}
		else
		{
			resultTitleLabel.Text =
				"ALIENS WIN!";

			resultStatsLabel.Text =
				$"The aliens have taken over.\n\n" +
				$"Team Tasks: {completed} / {total}\n" +
				$"Time Remaining: 00:00";
		}

		GD.Print(
			$"Result screen displayed: {result}"
		);
	}

	private void OnContinuePressed()
	{
		GD.Print(
			"Continue button pressed."
		);
	}
}
