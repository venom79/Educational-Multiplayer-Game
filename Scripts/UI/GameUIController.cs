
using Godot;

public partial class GameUIController : CanvasLayer
{
	private Label timerLabel;

	private Label playerProgressLabel;
	private ProgressBar playerProgressBar;

	private Label blueProgressLabel;
	private ProgressBar blueProgressBar;

	private Label redProgressLabel;
	private ProgressBar redProgressBar;

	public override void _Ready()
	{
		timerLabel = GetNode<Label>("TimerLabel");

		playerProgressLabel =
			GetNode<Label>("PlayerProgressLabel");

		playerProgressBar =
			GetNode<ProgressBar>("PlayerProgressBar");

		blueProgressLabel =
			GetNode<Label>("BlueProgressLabel");

		blueProgressBar =
			GetNode<ProgressBar>("BlueProgressBar");

		redProgressLabel =
			GetNode<Label>("RedProgressLabel");

		redProgressBar =
			GetNode<ProgressBar>("RedProgressBar");

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
		timerLabel.Position = new Vector2(40, 30);
		timerLabel.AddThemeFontSizeOverride("font_size", 32);

		playerProgressLabel.Position = new Vector2(40, 85);
		playerProgressLabel.AddThemeFontSizeOverride(
			"font_size", 20
		);

		playerProgressBar.Position = new Vector2(40, 115);
		playerProgressBar.Size = new Vector2(250, 22);

		blueProgressLabel.Position = new Vector2(40, 155);
		blueProgressLabel.AddThemeFontSizeOverride(
			"font_size", 20
		);
		blueProgressLabel.AddThemeColorOverride(
			"font_color", new Color("#3987FF")
		);

		blueProgressBar.Position = new Vector2(40, 185);
		blueProgressBar.Size = new Vector2(250, 22);
		blueProgressBar.Modulate = new Color("#3987FF");

		redProgressLabel.Position = new Vector2(40, 220);
		redProgressLabel.AddThemeFontSizeOverride(
			"font_size", 20
		);
		redProgressLabel.AddThemeColorOverride(
			"font_color", new Color("#FF6262")
		);

		redProgressBar.Position = new Vector2(40, 250);
		redProgressBar.Size = new Vector2(250, 22);
		redProgressBar.Modulate = new Color("#FF6262");
	}

	private void UpdateTimer()
	{
		if (GameManager.Instance == null)
			return;

		double remaining = GameManager.Instance.TimeRemaining;

		int totalSeconds =
			Mathf.Max(0, Mathf.CeilToInt((float)remaining));

		int minutes = totalSeconds / 60;
		int seconds = totalSeconds % 60;

		timerLabel.Text = $"{minutes:00}:{seconds:00}";
	}

	private void UpdateProgress()
	{
		if (GameManager.Instance == null)
			return;

		GameManager game = GameManager.Instance;

		int playerCompleted = game.PlayerCompletedTasks;
		int playerTotal = game.PlayerTotalTasks;

		int blueCompleted = game.BlueCompletedTasks;
		int blueTotal = game.BlueTotalTasks;

		int redCompleted = game.RedCompletedTasks;
		int redTotal = game.RedTotalTasks;

		playerProgressLabel.Text =
			$"YOUR TASKS  {playerCompleted} / {playerTotal}";

		SetProgress(playerProgressBar, playerCompleted, playerTotal);

		blueProgressLabel.Text =
			$"TEAM BLUE  {blueCompleted} / {blueTotal}";

		SetProgress(blueProgressBar, blueCompleted, blueTotal);

		redProgressLabel.Text =
			$"TEAM RED  {redCompleted} / {redTotal}";

		SetProgress(redProgressBar, redCompleted, redTotal);
	}

	private void SetProgress(
		ProgressBar bar,
		int completed,
		int total)
	{
		bar.MaxValue = total > 0 ? total : 1;
		bar.Value = completed;
	}
}
