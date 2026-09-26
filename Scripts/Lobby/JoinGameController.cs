using Godot;

public partial class JoinGameController : Control
{
	private LineEdit roomCodeInput;
	private Button joinButton;

	public override void _Ready()
	{
		GameSession.Instance.SetState(GameState.JoinGame);

		roomCodeInput = GetNode<LineEdit>(
            "JoinContainer/RoomCodeInput"
		);

		joinButton = GetNode<Button>(
            "JoinContainer/JoinButton"
		);

		joinButton.Pressed += OnJoinPressed;
	}

	private void OnJoinPressed()
	{
		string roomCode =
			roomCodeInput.Text.Trim().ToUpper();

		if (roomCode.Length != 6)
		{
			GD.PrintErr(
                "Room code must contain 6 characters."
			);

			return;
		}

		GD.Print(
			$"Joining room: {roomCode}"
		);

		GameSession.Instance.IsHost = false;
		GameSession.Instance.RoomCode = roomCode;

		NetworkManager.Instance.JoinGame(
			roomCode
		);
	}
}
