using Godot;

public partial class ResultScreenController : Control
{
	private ColorRect background;

	private Label resultTitle;
	private Label resultMessage;
	private Label teamProgress;
	private Label timeResult;

	private Button continueButton;

	public override void _Ready()
	{
		background =
			GetNode<ColorRect>("Background");

		resultTitle =
			GetNode<Label>("ResultTitle");

		resultMessage =
			GetNode<Label>("ResultMessage");

		teamProgress =
			GetNode<Label>("TeamProgress");

		timeResult =
			GetNode<Label>("TimeResult");

		continueButton =
			GetNode<Button>("ContinueButton");

		SetupUI();

		continueButton.Pressed +=
			OnContinuePressed;
		
		DisplayResult();
	}

	private void SetupUI()
	{
		// -------------------------
		// BACKGROUND
		// -------------------------

		background.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		// -------------------------
		// TITLE
		// -------------------------

		resultTitle.Position =
			new Vector2(0, 140);

		resultTitle.Size =
			new Vector2(1152, 80);

		resultTitle.HorizontalAlignment =
			HorizontalAlignment.Center;

		resultTitle.AddThemeFontSizeOverride(
			"font_size",
			52
		);


		// -------------------------
		// MESSAGE
		// -------------------------

		resultMessage.Position =
			new Vector2(0, 230);

		resultMessage.Size =
			new Vector2(1152, 50);

		resultMessage.HorizontalAlignment =
			HorizontalAlignment.Center;

		resultMessage.AddThemeFontSizeOverride(
			"font_size",
			24
		);


		// -------------------------
		// TEAM PROGRESS
		// -------------------------

		teamProgress.Position =
			new Vector2(0, 310);

		teamProgress.Size =
			new Vector2(1152, 40);

		teamProgress.HorizontalAlignment =
			HorizontalAlignment.Center;

		teamProgress.AddThemeFontSizeOverride(
			"font_size",
			22
		);


		// -------------------------
		// TIME
		// -------------------------

		timeResult.Position =
			new Vector2(0, 360);

		timeResult.Size =
			new Vector2(1152, 40);

		timeResult.HorizontalAlignment =
			HorizontalAlignment.Center;

		timeResult.AddThemeFontSizeOverride(
			"font_size",
			22
		);


		// -------------------------
		// BUTTON
		// -------------------------

		continueButton.Position =
			new Vector2(476, 450);

		continueButton.Size =
			new Vector2(200, 60);

		continueButton.Text =
			"Continue";

		continueButton.AddThemeFontSizeOverride(
			"font_size",
			22
		);
	}
	
	private void DisplayResult()
	{
		GameState result =
			GameSession.Instance.CurrentState;

		if (result == GameState.Victory)
		{
			resultTitle.Text =
				"TEAM VICTORY!";

			resultMessage.Text =
				"All tasks completed!";
		}
		else if (result == GameState.AliensWin)
		{
			resultTitle.Text =
				"ALIENS WIN!";

			resultMessage.Text =
				"The aliens have taken over.";
		}

		GD.Print(
			$"Result screen loaded with state: {result}"
		);
	}
	
	private void OnContinuePressed()
	{
		GD.Print(
			"Returning to Main Menu..."
		);
		NetworkManager.Instance.Disconnect();
		
		GameSession.Instance.SetState(
			GameState.MainMenu
		);

		GetTree().ChangeSceneToFile(
			"res://Scenes/MainMenu/MainMenu.tscn"
		);
	}
}
