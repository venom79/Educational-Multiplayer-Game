
using Godot;
using System.Linq;

public partial class WaitingRoomController : Control
{
	private Label roomCodeLabel;
	private Label modeLabel;

	private Label blueTeamCountLabel;
	private Label redTeamCountLabel;

	private VBoxContainer bluePlayerList;
	private VBoxContainer redPlayerList;

	private Button readyButton;
	private Button startButton;
	private Button switchTeamButton;

	public override void _Ready()
	{
		GameSession.Instance.SetState(GameState.WaitingRoom);

		roomCodeLabel = GetNode<Label>(
            "WaitingRoomContainer/RoomCode"
		);

		modeLabel = GetNode<Label>(
            "WaitingRoomContainer/SelectedMode"
		);

		blueTeamCountLabel = GetNode<Label>(
            "WaitingRoomContainer/TeamsContainer/BlueTeamPanel/BlueTeamContent/BlueTeamCount"
		);

		redTeamCountLabel = GetNode<Label>(
            "WaitingRoomContainer/TeamsContainer/RedTeamPanel/RedTeamContent/RedTeamCount"
		);

		bluePlayerList = GetNode<VBoxContainer>(
            "WaitingRoomContainer/TeamsContainer/BlueTeamPanel/BlueTeamContent/BluePlayerList"
		);

		redPlayerList = GetNode<VBoxContainer>(
            "WaitingRoomContainer/TeamsContainer/RedTeamPanel/RedTeamContent/RedPlayerList"
		);

		readyButton = GetNode<Button>(
            "WaitingRoomContainer/ReadyButton"
		);

		startButton = GetNode<Button>(
            "WaitingRoomContainer/StartButton"
		);
		
		switchTeamButton = GetNode<Button>(
		    "WaitingRoomContainer/SwitchTeamButton"
		);

		UpdateRoomUI();
		UpdatePlayerLists();
		UpdateReadyButton();
		UpdateStartButton();
	}

	public override void _Process(double delta)
	{
		UpdateRoomUI();
		UpdatePlayerLists();
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

	
	private void UpdatePlayerLists()
	{
		NetworkPlayer[] players =
			LobbyManager.Instance.GetPlayers().ToArray();

		foreach (Node child in bluePlayerList.GetChildren())
			child.QueueFree();

		foreach (Node child in redPlayerList.GetChildren())
			child.QueueFree();

		int blueCount = 0;
		int redCount = 0;

		foreach (NetworkPlayer player in players)
		{
			string hostLabel = player.IsHost ? " (Host)" : "";
			string readyStatus = player.IsReady ? "READY" : "NOT READY";

			Label playerLabel = new Label
			{
				Text = $"{player.PlayerName}{hostLabel} - {readyStatus}"
			};

			if (player.Team == PlayerTeam.Blue)
			{
				bluePlayerList.AddChild(playerLabel);
				blueCount++;
			}
			else
			{
				redPlayerList.AddChild(playerLabel);
				redCount++;
			}
		}

		blueTeamCountLabel.Text = $"Players: {blueCount}/4";
		redTeamCountLabel.Text = $"Players: {redCount}/4";
	}


	private void TrimPlayerList(
		VBoxContainer list,
		int requiredCount)
	{
		while (list.GetChildCount() > requiredCount)
		{
			Node child = list.GetChild(
				list.GetChildCount() - 1
			);

			list.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void UpdateReadyButton()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		if (localPlayer == null)
			return;

		readyButton.Text =
			localPlayer.IsReady ? "UNREADY" : "READY";
	}

	private void UpdateStartButton()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		if (localPlayer == null || !localPlayer.IsHost)
		{
			startButton.Visible = false;
			return;
		}

		startButton.Visible = true;

		startButton.Disabled =
			!localPlayer.IsReady ||
			!LobbyManager.Instance.AreAllPlayersReady();
	}

	public void OnReadyPressed()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		if (localPlayer == null)
			return;

		bool newReadyState = !localPlayer.IsReady;

		NetworkManager.Instance.SetLocalReady(newReadyState);

		GD.Print($"Local ready requested: {newReadyState}");
	}

	public void OnStartPressed()
	{
		NetworkPlayer localPlayer =
			LobbyManager.Instance.GetLocalPlayer();

		if (localPlayer == null)
			return;

		if (!localPlayer.IsHost)
		{
			GD.Print("Only the host can start the game.");
			return;
		}

		if (!localPlayer.IsReady)
		{
			GD.Print("Host must be ready before starting.");
			return;
		}

		if (!LobbyManager.Instance.AreAllPlayersReady())
		{
			GD.Print("Cannot start: not all players are ready.");
			return;
		}

		GD.Print("Requesting game start...");
		NetworkManager.Instance.RequestStartGame();
	}
	
	public void OnSwitchTeamPressed()
	{
		NetworkManager.Instance.RequestSwitchTeam();
	}
}
