
using Godot;

public partial class LoadingScreen : Control
{
	private Label titleLabel;
	private Label statusLabel;
	private Label instructionsLabel;
	private ProgressBar progressBar;

	private double animationTime;

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var background = new ColorRect();
		background.Color = new Color(0.055f, 0.065f, 0.10f);
		background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(background);

		var center = new CenterContainer();
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(center);

		var layout = new VBoxContainer();
		layout.CustomMinimumSize = new Vector2(560, 0);
		layout.AddThemeConstantOverride("separation", 20);
		center.AddChild(layout);

		titleLabel = new Label();
		titleLabel.Text = "THE LAST BELL";
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.AddThemeFontSizeOverride("font_size", 36);
		layout.AddChild(titleLabel);

		statusLabel = new Label();
		statusLabel.Text = "Preparing the classroom...";
		statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
		statusLabel.AddThemeFontSizeOverride("font_size", 20);
		layout.AddChild(statusLabel);

		progressBar = new ProgressBar();
		progressBar.CustomMinimumSize = new Vector2(560, 28);
		progressBar.MinValue = 0;
		progressBar.MaxValue = 100;
		progressBar.ShowPercentage = false;
		layout.AddChild(progressBar);

		instructionsLabel = new Label();
		instructionsLabel.Text =
			"Complete your team's tasks.\n" +
			"Team Red vs Team Blue.\n" +
			"The first team to complete all its tasks wins!";
		instructionsLabel.HorizontalAlignment =
			HorizontalAlignment.Center;
		instructionsLabel.AutowrapMode =
			TextServer.AutowrapMode.Word;
		instructionsLabel.AddThemeFontSizeOverride("font_size", 18);
		layout.AddChild(instructionsLabel);

		SetStatus("Preparing the classroom...");
	}

	public override void _Process(double delta)
	{
		// Temporary indeterminate animation.
		// We will replace this with real loading status where available.
		animationTime += delta;
		progressBar.Value =
			15.0 + (System.Math.Sin(animationTime * 2.0) + 1.0) * 35.0;
	}

	public void SetStatus(string message)
	{
		if (statusLabel != null)
			statusLabel.Text = message;
	}
}
