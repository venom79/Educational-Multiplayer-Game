using Godot;

public partial class TaskStation : Area2D
{
	[Export]
	public string StationId = "station_01";

	private Label interactionLabel;
	private bool playerNearby = false;

	public override void _Ready()
	{
		interactionLabel = GetNode<Label>("InteractionLabel");

		interactionLabel.Visible = false;

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _Process(double delta)
	{
		if (!playerNearby)
			return;

		if (Input.IsActionJustPressed("interact"))
		{
			Interact();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player player)
			return;

		if (!player.IsMultiplayerAuthority())
			return;

		playerNearby = true;

		UpdateInteractionLabel();

		GD.Print($"Player entered {StationId}");
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player player)
			return;

		if (!player.IsMultiplayerAuthority())
			return;

		playerNearby = false;

		interactionLabel.Visible = false;

		GD.Print($"Player left {StationId}");
	}

	private void Interact()
	{
		long playerId = Multiplayer.GetUniqueId();

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

		// Do not allow a completed task to be opened again.
		if (playerTask.Task.IsCompleted)
		{
			GD.Print($"Task at {StationId} is already completed.");
			return;
		}

		GD.Print($"Interacting with {StationId}");
		GD.Print($"Opening task: {playerTask.Task.Question}");

		TaskUIController taskUI =
			GetTree()
				.CurrentScene
				.GetNodeOrNull<TaskUIController>("TaskUI");

		if (taskUI == null)
		{
			GD.PrintErr("TaskUIController not found.");
			return;
		}

		taskUI.ShowTask(
			playerTask.Task,
			this
		);
	}

	public void DisableForPlayer()
	{
		playerNearby = false;
		interactionLabel.Visible = false;

		GD.Print($"Station {StationId} disabled for this player.");
	}

	private void UpdateInteractionLabel()
	{
		long playerId = Multiplayer.GetUniqueId();

		PlayerTask playerTask =
			TaskManager.Instance.GetTaskAtStation(
				playerId,
				StationId
			);

		if (playerTask == null)
		{
			interactionLabel.Visible = false;
			return;
		}

		if (playerTask.Task.IsCompleted)
		{
			interactionLabel.Visible = false;
			return;
		}

		interactionLabel.Text = "Press E to interact";
		interactionLabel.Visible = true;
	}
}
