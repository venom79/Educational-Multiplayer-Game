
using Godot;

public partial class SettingsController : Control
{
	private LineEdit nameInput;

	public override void _Ready()
	{
		nameInput = GetNode<LineEdit>(
            "CenterContainer/SettingsPanel/SettingsContainer/NameInput"
		);

		nameInput.MaxLength = 16;
		nameInput.Text = GameSession.Instance.PlayerName;
	}

	public void OnSavePressed()
	{
		GD.Print("Save button pressed.");

		string playerName = nameInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(playerName))
		{
			GD.PrintErr("Player name cannot be empty.");
			return;
		}

		GameSession.Instance.SetPlayerName(playerName);

		// Display the name that GameSession actually saved.
		nameInput.Text = GameSession.Instance.PlayerName;

		GD.Print(
			$"Name after saving: {GameSession.Instance.PlayerName}"
		);
	}

	public void OnBackPressed()
	{
		GetTree().ChangeSceneToFile(
            "res://Scenes/MainMenu/MainMenu.tscn"
		);
	}
}
