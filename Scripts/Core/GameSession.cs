using Godot;

public partial class GameSession : Node
{
	public static GameSession Instance { get; private set; }

	public GameMode SelectedMode { get; private set; } = GameMode.Science;
	
	public string PlayerName { get; private set; } = "Player";

	private const string SettingsPath = "user://settings.cfg";
	
	public string RoomCode { get; set; } = "";
	
	public string HostAddress { get; set; } = "";
	
	public int HostPort { get; set; } = 7777;

	public bool IsHost { get; set; } = false;

	public GameState CurrentState { get; private set; } = GameState.MainMenu;

	public string[] PlayerList { get; private set; } = [];

	public void UpdatePlayerList(string[] players)
	{
		PlayerList = players;
	}

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		ProcessMode = ProcessModeEnum.Always;
		
		LoadPlayerSettings();
	}

	public void SetGameMode(GameMode mode)
	{
		SelectedMode = mode;

		GD.Print($"Game mode selected: {SelectedMode}");
	}

	public void SetState(GameState state)
	{
		CurrentState = state;

		GD.Print($"Game State → {CurrentState}");
	}
	
	public void SetPlayerName(string playerName)
	{
		string cleanedName = playerName.Trim();

		if (string.IsNullOrWhiteSpace(cleanedName))
		{
			GD.PrintErr("Player name cannot be empty.");
			return;
		}

		if (cleanedName.Length > 16)
		{
			cleanedName = cleanedName[..16];
		}

		PlayerName = cleanedName;
		SavePlayerSettings();

		GD.Print($"Player name saved: {PlayerName}");
	}

	private void LoadPlayerSettings()
	{
		ConfigFile config = new ConfigFile();

		Error error = config.Load(SettingsPath);

		if (error != Error.Ok)
		{
			GD.Print("No saved settings found. Using default player name.");
			return;
		}

		PlayerName = (string)config.GetValue(
			"player",
			"name",
	        "Player"
		);

		GD.Print($"Loaded player name: {PlayerName}");
	}

	private void SavePlayerSettings()
	{
		ConfigFile config = new ConfigFile();

		config.SetValue("player", "name", PlayerName);

		Error error = config.Save(SettingsPath);

		if (error != Error.Ok)
		{
			GD.PrintErr($"Failed to save player settings: {error}");
		}
	}
}
