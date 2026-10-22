using Godot;

public partial class GameController : Node2D
{
	public override void _Ready()
	{
		
		ulong start = Time.GetTicksMsec();
		GD.Print($"[TIMING] Game root _Ready START: {start}");
	
		GameSession.Instance.SetState(
			GameState.Playing
		);

		GD.Print(
			"Game scene loaded."
		);

		if (GameManager.Instance == null)
		{
			GD.PrintErr(
				"GameManager instance is NULL!"
			);

			return;
		}

		GD.Print(
			"Reporting game loaded to GameManager..."
		);

		GameManager.Instance.MarkLocalGameLoaded();
		GD.Print(
			$"[TIMING] Game root _Ready END: " +
			$"{Time.GetTicksMsec() - start} ms"
		);
	}
}
