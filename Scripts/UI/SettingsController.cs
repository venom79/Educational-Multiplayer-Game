
using Godot;

public partial class SettingsController : Control
{
	private LineEdit nameInput;
	private HSlider musicSlider;
	private Label musicValue;

	public override void _Ready()
	{
		nameInput = GetNode<LineEdit>(
            "CenterContainer/SettingsPanel/SettingsContainer/NameRow/NameInput"
		);

		musicSlider = GetNode<HSlider>(
            "CenterContainer/SettingsPanel/SettingsContainer/MusicRow/MusicSlider"
		);

		musicValue = GetNode<Label>(
            "CenterContainer/SettingsPanel/SettingsContainer/MusicRow/MusicValue"
		);

		nameInput.MaxLength = 16;
		nameInput.Text = GameSession.Instance.PlayerName;

		// Music volume slider: 0% to 100%.
		musicSlider.MinValue = 0;
		musicSlider.MaxValue = 100;
		musicSlider.Step = 1;

		// Load the current volume from MusicManager.
		musicSlider.SetValueNoSignal(
			DbToPercent(GetMusicManager().GetMusicVolume())
		);

		UpdateMusicValueLabel(musicSlider.Value);

		// Only control background music, not sound effects.
		musicSlider.ValueChanged += OnMusicVolumeChanged;
	}

	private void OnMusicVolumeChanged(double value)
	{
		float volumeDb = PercentToDb((float)value);

		GetMusicManager().SetMusicVolume(volumeDb);
		UpdateMusicValueLabel(value);
	}

	private void UpdateMusicValueLabel(double value)
	{
		musicValue.Text = $"{(int)value}%";
	}

	private float PercentToDb(float percent)
	{
		if (percent <= 0.0f)
			return -80.0f;

		return Mathf.LinearToDb(percent / 100.0f);
	}

	private float DbToPercent(float volumeDb)
	{
		if (volumeDb <= -80.0f)
			return 0.0f;

		return Mathf.DbToLinear(volumeDb) * 100.0f;
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
		nameInput.Text = GameSession.Instance.PlayerName;

		GD.Print(
			$"Name after saving: {GameSession.Instance.PlayerName}"
		);

		GD.Print($"Music volume saved for this session: {musicSlider.Value}%");
	}

	public void OnBackPressed()
	{
		GetTree().ChangeSceneToFile(
            "res://Scenes/MainMenu/MainMenu.tscn"
		);
	}
	
	private MusicManager GetMusicManager()
	{
		return GetNode<MusicManager>("/root/MusicManager");
	}
}
