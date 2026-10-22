
using Godot;

public partial class MainMenuController : Control
{
	public void OnHostPressed()
	{
		GetTree().ChangeSceneToFile(
            "res://Scenes/Lobby/HostSetup.tscn"
		);
	}

	public void OnJoinPressed()
	{
		GetTree().ChangeSceneToFile(
            "res://Scenes/Lobby/JoinGame.tscn"
		);
	}

	public void OnSettingsPressed()
	{
		GetTree().ChangeSceneToFile(
            "res://Scenes/MainMenu/Settings.tscn"
		);
	}

	public void OnExitPressed()
	{
		GetTree().Quit();
	}
}
