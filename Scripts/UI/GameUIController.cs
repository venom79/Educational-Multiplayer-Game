
using Godot;

public partial class GameUIController : CanvasLayer
{
	private Label timerLabel;
	private Label teamNameLabel;
	private ProgressBar teamProgressBar;
	private Label opponentNameLabel;
	private ProgressBar opponentProgressBar;
	private Label playerProgressLabel;
	private ProgressBar playerProgressBar;
	private StyleBoxFlat teamFillStyle;
	private StyleBoxFlat opponentFillStyle;
	private StyleBoxFlat playerFillStyle;

	private static readonly Color BlueColor = new("#3987FF");
	private static readonly Color RedColor = new("#FF6262");
	private static readonly Color PersonalColor = new("#60A5FA");
	private static readonly Color WhiteColor = new("#F1F5F9");

	public override void _Ready()
	{
		timerLabel = GetNode<Label>(
            "HUD/ScreenMargin/Layout/TopBar/TimerPanel/TimerContent/TimerLabel"
		);

		teamNameLabel = GetNode<Label>(
            "HUD/ScreenMargin/Layout/TopBar/OwnTeamPanel/TeamContent/TeamNameLabel"
		);

		teamProgressBar = GetNode<ProgressBar>(
            "HUD/ScreenMargin/Layout/TopBar/OwnTeamPanel/TeamContent/TeamProgressBar"
		);

		opponentNameLabel = GetNode<Label>(
            "HUD/ScreenMargin/Layout/TopBar/OpponentTeamPanel/OpponentContent/OpponentNameLabel"
		);

		opponentProgressBar = GetNode<ProgressBar>(
            "HUD/ScreenMargin/Layout/TopBar/OpponentTeamPanel/OpponentContent/OpponentProgressBar"
		);

		playerProgressLabel = GetNode<Label>(
            "HUD/ScreenMargin/Layout/PlayerProgressPanel/PlayerContent/PlayerProgressLabel"
		);

		playerProgressBar = GetNode<ProgressBar>(
            "HUD/ScreenMargin/Layout/PlayerProgressPanel/PlayerContent/PlayerProgressBar"
		);
		
		teamFillStyle = CreateFillStyle(BlueColor);
		opponentFillStyle = CreateFillStyle(RedColor);
		playerFillStyle = CreateFillStyle(PersonalColor);

		teamProgressBar.AddThemeStyleboxOverride(
			"fill", teamFillStyle
		);

		opponentProgressBar.AddThemeStyleboxOverride(
			"fill", opponentFillStyle
		);

		playerProgressBar.AddThemeStyleboxOverride(
			"fill", playerFillStyle
		);
		
		teamProgressBar.ShowPercentage = false;
		opponentProgressBar.ShowPercentage = false;
		playerProgressBar.ShowPercentage = false;

		timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
		timerLabel.AddThemeColorOverride("font_color", WhiteColor);

		UpdateHUD();
	}

	public override void _Process(double delta)
	{
		UpdateHUD();
	}

	private void UpdateHUD()
	{
		GameManager game = GameManager.Instance;

		if (game == null)
			return;

		UpdateTimer(game);
		UpdateTaskProgress(game);
	}

	private void UpdateTimer(GameManager game)
	{
		int totalSeconds = Mathf.Max(
			0,
			Mathf.CeilToInt((float)game.TimeRemaining)
		);

		int minutes = totalSeconds / 60;
		int seconds = totalSeconds % 60;

		timerLabel.Text = $"{minutes:00}:{seconds:00}";
	}

	
	private void UpdateTaskProgress(GameManager game)
	{
		int playerCompleted = game.PlayerCompletedTasks;
		int playerTotal = game.PlayerTotalTasks;

		int blueCompleted = game.BlueCompletedTasks;
		int blueTotal = game.BlueTotalTasks;

		int redCompleted = game.RedCompletedTasks;
		int redTotal = game.RedTotalTasks;

		NetworkPlayer localPlayer = null;

		if (LobbyManager.Instance != null)
		{
			localPlayer = LobbyManager.Instance.GetPlayer(
				Multiplayer.GetUniqueId()
			);
		}

		// Wait until the local player's team is available.
		if (localPlayer == null)
			return;

		bool isBlue = localPlayer.Team == PlayerTeam.Blue;

		// Determine own-team and opponent progress.
		int ownCompleted = isBlue ? blueCompleted : redCompleted;
		int ownTotal = isBlue ? blueTotal : redTotal;

		int enemyCompleted = isBlue ? redCompleted : blueCompleted;
		int enemyTotal = isBlue ? redTotal : blueTotal;

		// Determine colors dynamically from the local player's team.
		Color ownColor = isBlue ? BlueColor : RedColor;
		Color enemyColor = isBlue ? RedColor : BlueColor;

		// Update own-team label.
		teamNameLabel.Text = isBlue ? "TEAM BLUE" : "TEAM RED";
		teamNameLabel.AddThemeColorOverride(
			"font_color",
			Colors.White
		);

		// Update opponent label.
		opponentNameLabel.Text = isBlue ? "TEAM RED" : "TEAM BLUE";
		opponentNameLabel.AddThemeColorOverride(
			"font_color",
			Colors.White
		);

		// Change the fill colors without tinting the backgrounds.
		teamFillStyle.BgColor = ownColor;
		opponentFillStyle.BgColor = enemyColor;
		playerFillStyle.BgColor = PersonalColor;

		// Update personal task text.
		playerProgressLabel.Text =
			$"YOUR TASKS  {playerCompleted} / {playerTotal}";

		// Update all progress bars.
		SetProgress(teamProgressBar, ownCompleted, ownTotal);
		SetProgress(opponentProgressBar, enemyCompleted, enemyTotal);
		SetProgress(playerProgressBar, playerCompleted, playerTotal);
	}


	private static void SetProgress(
		ProgressBar bar,
		int completed,
		int total)
	{
		bar.MaxValue = Mathf.Max(1, total);
		bar.Value = Mathf.Clamp(completed, 0, Mathf.Max(1, total));
	}
	
	private static StyleBoxFlat CreateFillStyle(Color color)
	{
		var style = new StyleBoxFlat
		{
			BgColor = color
		};

		style.SetCornerRadiusAll(4);
		return style;
	}
}
