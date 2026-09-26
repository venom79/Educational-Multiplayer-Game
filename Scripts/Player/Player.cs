using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 200.0f;

	[Export] public Texture2D UpSprite;
	[Export] public Texture2D DownSprite;
	[Export] public Texture2D LeftSprite;
	[Export] public Texture2D RightSprite;

	private double networkUpdateTimer = 0.0;

	private const double NetworkUpdateInterval = 0.05;
	
	private Vector2 networkTargetPosition;
	private bool hasNetworkPosition = false;

	private const float NetworkInterpolationSpeed = 12.0f;
	
	private Sprite2D sprite;
	private Camera2D camera;

	public override void _Ready()
	{
		sprite = GetNode<Sprite2D>("Sprite2D");
		camera = GetNode<Camera2D>("Camera2D");
		networkTargetPosition = GlobalPosition;
		camera.Enabled = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			if (hasNetworkPosition)
			{
				GlobalPosition =
					GlobalPosition.Lerp(
						networkTargetPosition,
						NetworkInterpolationSpeed *
						(float)delta
					);
			}

			return;
		}

		camera.Enabled = true;

		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
	        "move_down"
		);

		Velocity = direction * Speed;

		MoveAndSlide();

		UpdateDirection(direction);

		networkUpdateTimer += delta;

		if (networkUpdateTimer >= NetworkUpdateInterval)
		{
			networkUpdateTimer = 0.0;

			NetworkManager.Instance.SendPlayerPosition(
				GlobalPosition
			);
		}
	}
	
	private void UpdateDirection(Vector2 direction)
	{
		if (direction.Y < 0)
			sprite.Texture = DownSprite;
		else if (direction.Y > 0)
			sprite.Texture = UpSprite;
		else if (direction.X < 0)
			sprite.Texture = LeftSprite;
		else if (direction.X > 0)
			sprite.Texture = RightSprite;
	}
		
	public void SetNetworkPosition(Vector2 position)
	{
		networkTargetPosition = position;
		hasNetworkPosition = true;
	}
}
