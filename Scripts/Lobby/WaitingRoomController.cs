using Godot;
using System.Linq;

public partial class WaitingRoomController : Control
{
	private Label roomCodeLabel;
	private Label modeLabel;
	private Label playerListLabel;

	private Button readyButton;
	private Button startButton;

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
		UpdateReadyButton();
		UpdateStartButton();
	}

	public override void _Process(double delta)
	{
		UpdatePlayerList();
		UpdateReadyButton();
		UpdateStartButton();
	}

	private void UpdateRoomUI()
	{
		roomCodeLabel.Text =
			$"Room Code: {GameSession.Instance.RoomCode}";

		modeLabel.Text =
			$"Mode: {GameSession.Instance.SelectedMode}";
	}

	private void UpdatePlayerList()
	{
		NetworkPlayer[] players =
			LobbyManager.Instance.GetPlayers().ToArray();

		string text = "Players:\n";

		int number = 1;

		foreach (NetworkPlayer player in players)
		{
			string readyStatus =
				player.IsReady ? "READY" : "NOT READY";

			string hostLabel =
				player.IsHost ? " (Host)" : "";

			text +=
				$"{number}. {player.PlayerName}{hostLabel} - {readyStatus}\n";

			number++;
		}

		playerListLabel.Text = text;
	}

	private void UpdateReadyButton()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		if (localPlayer == null)
			return;

		readyButton.Text =
			localPlayer.IsReady
				? "UNREADY"
				: "READY";
	}

	private void UpdateStartButton()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		// Until our player exists, hide Start.
		if (localPlayer == null)
		{
			startButton.Visible = false;
			return;
		}

		// Only the host can see the Start button.
		if (!localPlayer.IsHost)
		{
			startButton.Visible = false;
			return;
		}

		// Host can see it.
		startButton.Visible = true;

		// Everyone must be ready.
		startButton.Disabled =
			!LobbyManager.Instance.AreAllPlayersReady();
	}

	public void OnReadyPressed()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		if (localPlayer == null)
			return;

		bool newReadyState =
			!localPlayer.IsReady;

		NetworkManager.Instance.SetLocalReady(
			newReadyState
		);

		GD.Print(
			$"Local ready requested: {newReadyState}"
		);
	}

	public void OnStartPressed()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		if (localPlayer == null)
			return;

		if (!localPlayer.IsHost)
		{
			GD.Print(
				"Only the host can start the game."
			);

			return;
		}

		if (!localPlayer.IsReady)
		{
			GD.Print(
				"Host must be ready before starting."
			);

			return;
		}

		if (!LobbyManager.Instance.AreAllPlayersReady())
		{
			GD.Print(
				"Cannot start: not all players are ready."
			);

			return;
		}

		GD.Print(
			"Requesting game start..."
		);

		NetworkManager.Instance.RequestStartGame();
	}
}
