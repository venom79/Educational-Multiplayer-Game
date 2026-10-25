
using Godot;

public partial class LoadingScreen : Control
{
	private const string NextScene = "res://scenes/main_menu.tscn"; // change to your real scene path

	private TextureProgressBar bar;
	private Label label;

	private Godot.Collections.Array progress = new Godot.Collections.Array();
	private float shown = 0f;

	public override void _Ready()
	{
		bar = GetNode<TextureProgressBar>("TextureProgressBar");
		label = GetNode<Label>("Label");

		ResourceLoader.LoadThreadedRequest(NextScene);
	}

	public override void _Process(double delta)
	{
		var status = ResourceLoader.LoadThreadedGetStatus(NextScene, progress);

		if (status == ResourceLoader.ThreadLoadStatus.InvalidResource)
		{
			GD.PrintErr("Wrong scene path: " + NextScene);
			SetProcess(false);
			return;
		}

		float real = progress[0].AsSingle();
		shown = Mathf.MoveToward(shown, real, (float)delta);

		bar.Value = shown * 100f;
		label.Text = $"Loading... {(int)(shown * 100f)}%";

		if (status == ResourceLoader.ThreadLoadStatus.Loaded && shown >= 1f)
		{
			SetProcess(false);
			var scene = (PackedScene)ResourceLoader.LoadThreadedGet(NextScene);
			GetTree().ChangeSceneToPacked(scene);
		}
	}
}
