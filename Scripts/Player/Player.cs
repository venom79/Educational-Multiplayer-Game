using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float Speed = 200.0f;

	[Export] public SpriteFrames RedFrames;
	[Export] public SpriteFrames BlueFrames;

	private Label _nameLabel;
	private double networkUpdateTimer = 0.0;

	private const double NetworkUpdateInterval = 0.05;

	private Vector2 networkTargetPosition;
	private bool hasNetworkPosition = false;

	private const float NetworkInterpolationSpeed = 12.0f;

	private AnimatedSprite2D sprite;
	private Camera2D camera;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		camera = GetNode<Camera2D>("Camera2D");
		_nameLabel = GetNode<Label>("NameLabel");
		_nameLabel.Text = GameSession.Instance.PlayerName;
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

		// Keep the local player frozen until the host confirms
		// that every player has loaded the game scene.
		if (GameManager.Instance == null ||
			!GameManager.Instance.GameStarted)
		{
			Velocity = Vector2.Zero;
			return;
		}

		camera.Enabled = true;

		TaskUIController taskUI =
			GetTree()
				.CurrentScene
				.GetNodeOrNull<TaskUIController>("TaskUI");

		if (taskUI != null && taskUI.IsTaskOpen)
		{
			Velocity = Vector2.Zero;
			return;
		}

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

	public void SetTeam(PlayerTeam team)
	{
		sprite.SpriteFrames = team == PlayerTeam.Red ? RedFrames : BlueFrames;
		sprite.Play("idle");
	}

	private void UpdateDirection(Vector2 direction)
	{
		if (direction == Vector2.Zero)
		{
			if (sprite.Animation != "idle")
				sprite.Play("idle");
			return;
		}

		string anim;

		if (direction.Y < 0) anim = "walk_up";
		else if (direction.Y > 0) anim = "walk_down";
		else if (direction.X < 0) anim = "walk_left";
		else anim = "walk_right";

		if (sprite.Animation != anim)
			sprite.Play(anim);
	}

	public void SetNetworkPosition(Vector2 position)
	{
		networkTargetPosition = position;
		hasNetworkPosition = true;
	}
}
