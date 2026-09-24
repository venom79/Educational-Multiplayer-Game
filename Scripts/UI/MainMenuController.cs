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
}
