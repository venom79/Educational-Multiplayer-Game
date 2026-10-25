using Godot;

public partial class ResultScreenController : Control
{
	private const string BlueHex = "#4da6ff";
	private const string RedHex = "#ff4d4d";

	private TextureRect background;
	private TextureRect trophy;

	private RichTextLabel resultTitle;
	private RichTextLabel teamResult;
	private Label timeResult;

	private Button continueButton;

	public override void _Ready()
	{
		background = GetNode<TextureRect>("Background");
		trophy = GetNode<TextureRect>("trophy");

		resultTitle = GetNode<RichTextLabel>("ResultTitle");
		teamResult = GetNode<RichTextLabel>("TeamResult");

		continueButton = GetNode<Button>("ContinueButton");

		SetupUI();

		continueButton.Pressed += OnContinuePressed;

		DisplayResult();
	}

	private void SetupUI()
	{
		// BACKGROUND
		background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		background.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		background.StretchMode = TextureRect.StretchModeEnum.Scale;

		// TROPHY (size and position are set in the editor)
		trophy.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		trophy.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

		// RICH TEXT LABELS
		StyleRichLabel(resultTitle, 52);
		StyleRichLabel(teamResult, 22);

		// TIME (plain label, only if it exists)
		if (timeResult != null)
		{
			timeResult.HorizontalAlignment = HorizontalAlignment.Center;
			timeResult.AddThemeColorOverride("font_color", new Color(1, 1, 1));
		}

		// BUTTON
		continueButton.Text = "Play Again";
	}

	private void StyleRichLabel(RichTextLabel label, int fontSize)
	{
		label.BbcodeEnabled = true;
		label.FitContent = true;
		label.ScrollActive = false;
		label.AddThemeFontSizeOverride("normal_font_size", fontSize);
		label.AddThemeColorOverride("default_color", new Color(1, 1, 1));
	}

	private void DisplayResult()
	{
		GameState result = GameSession.Instance.CurrentState;

		switch (result)
		{
			case GameState.BlueVictory:
				resultTitle.Text = $"[center]TEAM [color={BlueHex}]BLUE[/color] WINS![/center]";
				break;

			case GameState.RedVictory:
				resultTitle.Text = $"[center]TEAM [color={RedHex}]RED[/color] WINS![/center]";
				break;

			case GameState.Draw:
				resultTitle.Text = "[center]IT'S A DRAW![/center]";
				break;

			default:
				resultTitle.Text = "[center]GAME OVER[/center]";
				break;
		}

		if (GameManager.Instance != null)
		{
			teamResult.Text =
				$"[center][color={BlueHex}]BLUE[/color]: " +
				$"{GameManager.Instance.BlueCompletedTasks}/{GameManager.Instance.BlueTotalTasks}" +
				$"     [color={RedHex}]RED[/color]: " +
				$"{GameManager.Instance.RedCompletedTasks}/{GameManager.Instance.RedTotalTasks}[/center]";
		}

		if (timeResult != null)
		{
			timeResult.Text =
				$"Time remaining: {GameManager.Instance?.TimeRemaining ?? 0:F0}s";
		}

		GD.Print($"Result screen loaded with state: {result}");
	}

	private void OnContinuePressed()
	{
		GD.Print("Returning to Main Menu...");
		NetworkManager.Instance.Disconnect();

		GameSession.Instance.SetState(GameState.MainMenu);

		GetTree().ChangeSceneToFile("res://Scenes/MainMenu/MainMenu.tscn");
	}
}
