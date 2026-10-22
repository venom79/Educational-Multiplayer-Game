using Godot;

public partial class StudentCharacter : AnimatedSprite2D
{
	public override void _Ready()
	{
		Play("walk");
	}
}
