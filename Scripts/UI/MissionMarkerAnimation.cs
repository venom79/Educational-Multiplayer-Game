
using Godot;

public partial class MissionMarkerAnimation : AnimatedSprite2D
{
	public override void _Ready()
	{
		GD.Print("[Marker] Node ready.");

		if (SpriteFrames == null)
		{
			GD.PrintErr("[Marker] SpriteFrames resource is missing.");
			return;
		}

		GD.Print($"[Marker] Animations: {string.Join(", ", SpriteFrames.GetAnimationNames())}");

		if (!SpriteFrames.HasAnimation("spin"))
		{
			GD.PrintErr("[Marker] Animation 'spin' not found.");
			return;
		}

		int frameCount = SpriteFrames.GetFrameCount("spin");
		GD.Print($"[Marker] Frames: {frameCount}");

		if (frameCount == 0)
		{
			GD.PrintErr("[Marker] No frames in spin.");
			return;
		}

		SpriteFrames.SetAnimationLoop("spin", true);
		SpeedScale = 1.0f;

		Play("spin");

		GD.Print($"[Marker] Playing: {IsPlaying()}");
	}
}
