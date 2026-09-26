using Godot;

public partial class GameSession : Node
{
	public static GameSession Instance { get; private set; }

	public GameMode SelectedMode { get; private set; } = GameMode.Science;

	public string RoomCode { get; set; } = "";
	
	public string HostAddress { get; set; } = "";
	
	public int HostPort { get; set; } = 7777;

	public bool IsHost { get; set; } = false;

	public GameState CurrentState { get; private set; } = GameState.MainMenu;

	public override void _Ready()
	{
		if (Instance != null && Instance != this)
		{
			QueueFree();
			return;
		}

		Instance = this;

		ProcessMode = ProcessModeEnum.Always;
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
}
