
using Godot;

public partial class HostSetupController : Control
{
	public override void _Ready()
	{
		GameSession.Instance.SetState(GameState.HostSetup);

		SetupSubjectCard("HostContainer/ModeCards/ScienceButton");
		SetupSubjectCard("HostContainer/ModeCards/MathsButton");
		SetupSubjectCard("HostContainer/ModeCards/LanguageButton");
	}

	private void SetupSubjectCard(string nodePath)
	{
		Button button = GetNode<Button>(nodePath);

		// Slightly darken the entire card while hovering.
		button.MouseEntered += () =>
		{
			button.Modulate = new Color(0.78f, 0.78f, 0.78f, 1f);
		};

		button.MouseExited += () =>
		{
			button.Modulate = Colors.White;
		};
	}

	public void SelectScience()
	{
		SelectMode(GameMode.Science);
	}

	public void SelectMaths()
	{
		SelectMode(GameMode.Maths);
	}

	public void SelectLanguage()
	{
		SelectMode(GameMode.Language);
	}

	private void SelectMode(GameMode mode)
	{
		GameSession.Instance.SetGameMode(mode);
		GameSession.Instance.IsHost = true;

		NetworkManager.Instance.HostGame();

		GetTree().ChangeSceneToFile(
            "res://Scenes/Lobby/WaitingRoom.tscn"
		);
	}
	
	public void OnBackPressed()
	{
		GetTree().ChangeSceneToFile(
            "res://Scenes/MainMenu/MainMenu.tscn"
		);
	}
}
