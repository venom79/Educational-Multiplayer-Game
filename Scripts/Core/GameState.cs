
public enum GameState
{
	MainMenu,
	HostSetup,
	JoinGame,
	WaitingRoom,
	Playing,

	BlueVictory,
	RedVictory,
	Draw,

	// Retained temporarily so older references still compile.
	Victory,
	AliensWin,

	Results
}
