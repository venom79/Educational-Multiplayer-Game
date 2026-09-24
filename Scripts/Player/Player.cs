using Godot;

public partial class Player : CharacterBody2D
{
	[Export]
	public float Speed = 300.0f;

	[Export]
	public Texture2D UpSprite;

	[Export]
	public Texture2D DownSprite;

	[Export]
	public Texture2D LeftSprite;

	[Export]
	public Texture2D RightSprite;

	private Sprite2D sprite;

	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
            "move_down"
		);

		Velocity = direction * Speed;

		MoveAndSlide();

		UpdateDirection(direction);
	}

	private void UpdateDirection(Vector2 direction)
	{
		if (direction.Y < 0)
		{
			sprite.Texture = DownSprite;
		}
		else if (direction.Y > 0)
		{
			sprite.Texture = UpSprite;
		}
		else if (direction.X < 0)
		{
			sprite.Texture = LeftSprite;
		}
		else if (direction.X > 0)
		{
			sprite.Texture = RightSprite;
		}
	}
}
