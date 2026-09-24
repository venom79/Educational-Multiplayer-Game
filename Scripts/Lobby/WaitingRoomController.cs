using Godot;

public partial class WaitingRoomController : Control
{
	private Label roomCodeLabel;
	private Label modeLabel;
	private Label playerListLabel;

	private Button readyButton;
	private Button startButton;

	private bool isReady = false;

	public override void _Ready()
	{
		GameSession.Instance.SetState(GameState.WaitingRoom);

		roomCodeLabel = GetNode<Label>(
            "WaitingRoomContainer/RoomCode"
		);

		modeLabel = GetNode<Label>(
            "WaitingRoomContainer/SelectedMode"
		);

		playerListLabel = GetNode<Label>(
            "WaitingRoomContainer/PlayerList"
		);

		readyButton = GetNode<Button>(
            "WaitingRoomContainer/ReadyButton"
		);

		startButton = GetNode<Button>(
            "WaitingRoomContainer/StartButton"
		);

		UpdateRoomUI();
	}

	private void UpdateRoomUI()
	{
		roomCodeLabel.Text = "Room Code: LOCAL";

		modeLabel.Text =
			$"Mode: {GameSession.Instance.SelectedMode}";

		playerListLabel.Text =
			"Players:\n1. Host";

		readyButton.Text = "READY";
	}

	public void OnReadyPressed()
	{
		isReady = !isReady;

		if (isReady)
		{
			readyButton.Text = "UNREADY";
			GD.Print("Host ready: True");
		}
		else
		{
			readyButton.Text = "READY";
			GD.Print("Host ready: False");
		}
	}

	public void OnStartPressed()
	{
		if (!isReady)
		{
			GD.Print("Host must be ready before starting.");
			return;
		}

		GD.Print("Starting game...");

		GameSession.Instance.SetState(GameState.Playing);
	}
}
