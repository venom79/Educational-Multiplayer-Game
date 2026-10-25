
using Godot;

public partial class TaskStation : Area2D
{
	[Export]
	public string StationId = "station_01";

	[Export]
	public Texture2D StationTexture { get; set; }

	[Export]
	public float RotationSpeed = 1.2f;

	[Export]
	public float FloatHeight = 5.0f;

	[Export]
	public float FloatSpeed = 2.5f;

	private Node2D markerVisual;
	private Line2D markerRing;
	private AnimatedSprite2D markerSprite;
	private CollisionShape2D areaCollision;
	private Label interactionLabel;

	private Vector2 markerStartPosition;
	private float animationTime;
	private bool playerNearby;

	public override void _Ready()
	{
		// Find the required nodes.
		interactionLabel = GetNodeOrNull<Label>("InteractionLabel");
		markerVisual = GetNodeOrNull<Node2D>("MarkerVisual");
		markerRing = GetNodeOrNull<Line2D>("MarkerVisual/Ring");

		areaCollision = GetNodeOrNull<CollisionShape2D>(
            "CollisionShape2D"
		);

		// Keep the interaction area active.
		Monitoring = true;
		Monitorable = true;

		if (areaCollision != null)
			areaCollision.Disabled = false;
		else
			GD.PrintErr($"[{StationId}] Area CollisionShape2D not found.");

		if (interactionLabel != null)
			interactionLabel.Visible = false;
		else
			GD.PrintErr($"[{StationId}] InteractionLabel not found.");

		if (markerVisual != null)
		{
			markerStartPosition = markerVisual.Position;

			// Look for an animated sprite anywhere under MarkerVisual.
			markerSprite =
				markerVisual.GetNodeOrNull<AnimatedSprite2D>(
                    "AnimatedSprite2D"
				);

			if (markerSprite == null)
			{
				markerSprite =
					markerVisual.FindChild(
						"AnimatedSprite2D",
						true,
						false
					) as AnimatedSprite2D;
			}

			if (markerSprite != null)
			{
				if (markerSprite.SpriteFrames != null &&
					markerSprite.SpriteFrames.HasAnimation("default") &&
					markerSprite.SpriteFrames.GetFrameCount("default") > 0)
				{
					markerSprite.Play("default");
					GD.Print($"[{StationId}] Marker animation started.");
				}
				else
				{
					GD.PrintErr(
						$"[{StationId}] AnimatedSprite2D has no valid " +
                        "'default' animation frames."
					);
				}
			}
			else
			{
				GD.Print(
					$"[{StationId}] No AnimatedSprite2D found. " +
                    "The ring animation will still run."
				);
			}
		}
		else
		{
			GD.PrintErr($"[{StationId}] MarkerVisual node not found.");
		}

		if (markerRing != null)
			CreateMarkerRing();

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;

		GD.Print($"Task station ready: {StationId}");
	}

	public override void _Process(double delta)
	{
		animationTime += (float)delta;

		// Animate the visual only. Do not rotate the detection area.
		if (markerVisual != null)
		{
			markerVisual.Rotation += RotationSpeed * (float)delta;

			Vector2 floatingPosition = markerStartPosition;
			floatingPosition.Y +=
				Mathf.Sin(animationTime * FloatSpeed) * FloatHeight;

			markerVisual.Position = floatingPosition;
		}

		if (!playerNearby)
			return;

		if (Input.IsActionJustPressed("interact"))
			Interact();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player player)
			return;

		// Only respond to the locally controlled player.
		if (!player.IsMultiplayerAuthority())
			return;

		playerNearby = true;
		UpdateInteractionLabel();

		GD.Print($"Player entered task station: {StationId}");
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player player)
			return;

		if (!player.IsMultiplayerAuthority())
			return;

		playerNearby = false;

		if (interactionLabel != null)
			interactionLabel.Visible = false;

		GD.Print($"Player left task station: {StationId}");
	}

	private void Interact()
	{
		if (NetworkManager.Instance == null ||
			TaskManager.Instance == null)
		{
			GD.PrintErr("NetworkManager or TaskManager is unavailable.");
			return;
		}

		long playerId = Multiplayer.GetUniqueId();

		// HOST
		if (NetworkManager.Instance.IsServer)
		{
			PlayerTask playerTask =
				TaskManager.Instance.GetTaskAtStation(
					playerId,
					StationId
				);

			if (playerTask == null)
			{
				GD.Print($"No task assigned at {StationId}");
				return;
			}

			if (playerTask.Task.IsCompleted)
			{
				GD.Print($"Task at {StationId} is already completed.");
				return;
			}

			TaskUIController taskUI =
				GetTree().CurrentScene
					.GetNodeOrNull<TaskUIController>("TaskUI");

			if (taskUI == null)
			{
				GD.PrintErr("TaskUIController not found in game scene.");
				return;
			}

			GD.Print($"Opening task at {StationId}: {playerTask.Task.Question}");
			taskUI.ShowTask(playerTask.Task, this);
			return;
		}

		// CLIENT
		NetworkTaskData networkTask =
			TaskManager.Instance.GetNetworkTaskAtStation(
				playerId,
				StationId
			);

		if (networkTask == null)
		{
			GD.Print($"No network task assigned at {StationId}");
			return;
		}

		TaskUIController clientTaskUI =
			GetTree().CurrentScene
				.GetNodeOrNull<TaskUIController>("TaskUI");

		if (clientTaskUI == null)
		{
			GD.PrintErr("TaskUIController not found in game scene.");
			return;
		}

		GD.Print(
			$"Opening network task at {StationId}: " +
			networkTask.Question
		);

		clientTaskUI.ShowTask(networkTask, this);
	}

	public void DisableForPlayer()
	{
		playerNearby = false;

		if (interactionLabel != null)
			interactionLabel.Visible = false;

		GD.Print($"Station {StationId} disabled for this player.");
	}

	private void UpdateInteractionLabel()
	{
		if (interactionLabel == null ||
			NetworkManager.Instance == null ||
			TaskManager.Instance == null)
		{
			return;
		}

		long playerId = Multiplayer.GetUniqueId();

		if (NetworkManager.Instance.IsServer)
		{
			PlayerTask playerTask =
				TaskManager.Instance.GetTaskAtStation(
					playerId,
					StationId
				);

			if (playerTask == null || playerTask.Task.IsCompleted)
			{
				interactionLabel.Visible = false;
				return;
			}
		}
		else
		{
			NetworkTaskData networkTask =
				TaskManager.Instance.GetNetworkTaskAtStation(
					playerId,
					StationId
				);

			if (networkTask == null)
			{
				interactionLabel.Visible = false;
				return;
			}
		}

		interactionLabel.Text = "Press E to interact";
		interactionLabel.Visible = true;
	}

	private void CreateMarkerRing()
	{
		markerRing.ClearPoints();

		int segments = 12;
		float radiusX = 22.0f;
		float radiusY = 10.0f;

		for (int i = 0; i <= segments; i++)
		{
			float angle = Mathf.Tau * i / segments;

			markerRing.AddPoint(
				new Vector2(
					Mathf.Cos(angle) * radiusX,
					Mathf.Sin(angle) * radiusY
				)
			);
		}
	}
}
