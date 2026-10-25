
using Godot;

public partial class MusicManager : Node
{
	public override void _EnterTree()
	{
		GD.Print("🔥 MusicManager entered the scene tree!");
	}
	
	private const string MusicPath =
		"res://Assets/Music/menu_ambient.ogg";

	private const string MainMenuPath =
		"res://Scenes/MainMenu/MainMenu.tscn";

	private const string WaitingRoomPath =
		"res://Scenes/Lobby/WaitingRoom.tscn";
	
	private const string HostSetupPath =
	"res://Scenes/Lobby/HostSetup.tscn";

	private const string JoinRoomPath =
		"res://Scenes/Lobby/JoinGame.tscn";

	private const string SettingsPath =
		"res://Scenes/MainMenu/Settings.tscn";
	
	private AudioStreamPlayer musicPlayer;

	
public override void _Ready()
{
	GD.Print("[Music] _Ready() started.");

	ProcessMode = ProcessModeEnum.Always;

	musicPlayer = new AudioStreamPlayer
	{
		Name = "BackgroundMusic",
		VolumeDb = -6.0f,
		Bus = "Master"
	};

	AddChild(musicPlayer);
	GD.Print("[Music] AudioStreamPlayer created.");

	AudioStream music = GD.Load<AudioStream>(MusicPath);

	if (music == null)
	{
		GD.PushError($"[Music] Failed to load: {MusicPath}");
		return;
	}

	GD.Print("[Music] Audio file loaded successfully.");

	musicPlayer.Stream = music;
	musicPlayer.Finished += OnMusicFinished;

	GetTree().SceneChanged += OnSceneChanged;

	GD.Print("[Music] Connected SceneChanged.");

	CallDeferred(nameof(UpdateMusicForCurrentScene));
}


	private void OnSceneChanged()
	{
		UpdateMusicForCurrentScene();
	}

	
	private void UpdateMusicForCurrentScene()
	{
		string scenePath =
			GetTree().CurrentScene?.SceneFilePath ?? "";

		GD.Print($"[Music] Current scene: '{scenePath}'");

		bool shouldPlay =
			scenePath == MainMenuPath ||
			scenePath == HostSetupPath ||
			scenePath == JoinRoomPath ||
			scenePath == SettingsPath ||
			scenePath == WaitingRoomPath;

		if (shouldPlay)
		{
			if (!musicPlayer.Playing)
			{
				musicPlayer.Play();
				GD.Print($"[Music] Playback started: {scenePath}");
			}
		}
		else
		{
			if (musicPlayer.Playing)
			{
				musicPlayer.Stop();
				GD.Print($"[Music] Playback stopped: {scenePath}");
			}
		}
	}

	private void OnMusicFinished()
	{
		if (musicPlayer?.Stream != null)
			musicPlayer.Play();
	}

	public float GetMusicVolume()
	{
		return musicPlayer != null
			? musicPlayer.VolumeDb
			: -6.0f;
	}

	public void SetMusicVolume(float volumeDb)
	{
		if (musicPlayer != null)
		{
			musicPlayer.VolumeDb = volumeDb;
		}
	}
	
}
