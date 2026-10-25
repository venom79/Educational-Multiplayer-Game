using Godot;

public partial class GameController : Node2D
{
	private AudioStreamPlayer _music;

	public override void _Ready()
	{
		GD.Print("StartMusic about to run");
		StartMusic();

		ulong start = Time.GetTicksMsec();
		GD.Print($"[TIMING] Game root _Ready START: {start}");
	

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

	private void StartMusic()
	{
		_music = GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer");

		if (_music == null)
		{
			GD.PrintErr("Music player NOT found");
			return;
		}

		GD.Print($"Stream: {_music.Stream}");
		GD.Print($"Bus: {_music.Bus}, Volume: {_music.VolumeDb}");
		_music.Play();
		GD.Print($"Playing: {_music.Playing}");
	}

	public override void _ExitTree()
	{
		if (_music != null)
		{
			_music.Stop();
		}
	}
}
