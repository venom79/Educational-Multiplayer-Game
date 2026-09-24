using Godot;

public partial class HostSetupController : Control
{
	public override void _Ready()
	{
		GameSession.Instance.SetState(GameState.HostSetup);
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

		GetTree().ChangeSceneToFile(
            "res://Scenes/Lobby/WaitingRoom.tscn"
		);
	}
}
