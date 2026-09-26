using Godot;

public partial class GameUIController : CanvasLayer
{
	private Label timerLabel;

	public override void _Ready()
	{
		timerLabel =
			GetNode<Label>("TimerLabel");

		timerLabel.Position =
			new Vector2(40, 30);

		timerLabel.AddThemeFontSizeOverride(
			"font_size",
			32
		);

		UpdateTimer();
	}

	public override void _Process(double delta)
	{
		UpdateTimer();
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
}
