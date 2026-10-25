
using Godot;
using System;
using System.Linq;

public partial class MatchTheFollowingController : Control
{
	[Signal]
	public delegate void AnswerSubmittedEventHandler(string answer);

	private VBoxContainer leftColumn;
	private VBoxContainer rightColumn;
	private Button submitButton;

	private Button[] leftButtons = Array.Empty<Button>();
	private Button[] rightButtons = Array.Empty<Button>();
	private int[] matches = Array.Empty<int>();

	private int selectedLeft = -1;
	private bool inputDisabled;

	// Each left row has its own color, shared with its matched right button.
	private readonly Color[] pairColors =
	{
		new Color("#4FC3F7"), // Blue
		new Color("#FFB74D"), // Orange
		new Color("#81C784"), // Green
		new Color("#BA68C8"), // Purple
		new Color("#F06292"), // Pink
		new Color("#4DB6AC"), // Teal
		new Color("#FFF176"), // Yellow
		new Color("#7986CB")  // Indigo
	};

	public override void _Ready()
	{
		leftColumn = GetNode<VBoxContainer>(
			"Layout/Columns/LeftColumn");

		rightColumn = GetNode<VBoxContainer>(
			"Layout/Columns/RightColumn");

		submitButton = GetNode<Button>(
			"Layout/SubmitButton");

		submitButton.Pressed += OnSubmitPressed;
	}

	public void SetItems(string[] leftItems, string[] rightItems)
	{
		if (!IsNodeReady())
		{
			CallDeferred(nameof(SetItems), leftItems, rightItems);
			return;
		}

		if (leftItems == null || rightItems == null ||
			leftItems.Length == 0 ||
			leftItems.Length != rightItems.Length)
		{
			GD.PushError(
				"MatchTheFollowing requires equal, non-empty item lists.");
			return;
		}

		ClearColumn(leftColumn);
		ClearColumn(rightColumn);

		matches = Enumerable.Repeat(-1, leftItems.Length).ToArray();
		selectedLeft = -1;
		inputDisabled = false;

		leftButtons = new Button[leftItems.Length];
		rightButtons = new Button[rightItems.Length];

		for (int i = 0; i < leftItems.Length; i++)
		{
			int index = i;
			Button button = CreateItemButton(leftItems[i]);

			button.Pressed += () => OnLeftItemPressed(index);

			leftColumn.AddChild(button);
			leftButtons[i] = button;
		}

		for (int i = 0; i < rightItems.Length; i++)
		{
			int index = i;
			Button button = CreateItemButton(rightItems[i]);

			button.Pressed += () => OnRightItemPressed(index);

			rightColumn.AddChild(button);
			rightButtons[i] = button;
		}

		UpdateButtonStyles();
	}

	private Button CreateItemButton(string text)
	{
		return new Button
		{
			Text = text,
			CustomMinimumSize = new Vector2(0, 44),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			FocusMode = FocusModeEnum.All
		};
	}

	private void ClearColumn(VBoxContainer column)
	{
		foreach (Node child in column.GetChildren())
		{
			column.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void OnLeftItemPressed(int index)
	{
		if (inputDisabled)
			return;

		selectedLeft = index;
		UpdateButtonStyles();
	}

	private void OnRightItemPressed(int rightIndex)
	{
		if (inputDisabled || selectedLeft < 0)
			return;

		// Remove this right item from any previous pairing.
		for (int i = 0; i < matches.Length; i++)
		{
			if (matches[i] == rightIndex)
				matches[i] = -1;
		}

		// Pair the selected left row with this right row.
		matches[selectedLeft] = rightIndex;

		selectedLeft = -1;
		UpdateButtonStyles();
	}

	private Color GetPairColor(int leftIndex)
	{
		// Use a distinct palette color for each row.
		// For a normal four-item matching question, all pair colors differ.
		return pairColors[leftIndex % pairColors.Length];
	}

	private void UpdateButtonStyles()
	{
		for (int i = 0; i < leftButtons.Length; i++)
		{
			if (i == selectedLeft)
			{
				leftButtons[i].Modulate = Colors.White;
				leftButtons[i].SelfModulate = Colors.White;
			}
			else if (matches[i] >= 0)
			{
				leftButtons[i].Modulate = GetPairColor(i);
				leftButtons[i].SelfModulate = Colors.White;
			}
			else
			{
				leftButtons[i].Modulate = Colors.White;
				leftButtons[i].SelfModulate = Colors.White;
			}

			leftButtons[i].Disabled = inputDisabled;
		}

		// Match each right button's color to its paired left button.
		for (int rightIndex = 0;
			 rightIndex < rightButtons.Length;
			 rightIndex++)
		{
			int leftIndex = Array.IndexOf(matches, rightIndex);

			rightButtons[rightIndex].Modulate =
				leftIndex >= 0
					? GetPairColor(leftIndex)
					: Colors.White;

			rightButtons[rightIndex].SelfModulate = Colors.White;
			rightButtons[rightIndex].Disabled = inputDisabled;
		}

		submitButton.Disabled =
			inputDisabled ||
			matches.Length == 0 ||
			matches.Any(match => match < 0);
	}

	private void OnSubmitPressed()
	{
		if (inputDisabled ||
			matches.Length == 0 ||
			matches.Any(match => match < 0))
		{
			return;
		}

		string answer = string.Join(",", matches);
		EmitSignal(SignalName.AnswerSubmitted, answer);
	}

	public void SetInputDisabled(bool disabled)
	{
		inputDisabled = disabled;

		if (disabled)
			selectedLeft = -1;

		UpdateButtonStyles();
	}

	public void SetMatchFeedback(bool correct)
	{
		if (!correct)
		{
			// An incorrect answer clears every pair and color.
			ResetMatches();
			return;
		}

		for (int i = 0; i < leftButtons.Length; i++)
			leftButtons[i].Modulate = Colors.Green;

		foreach (Button button in rightButtons)
			button.Modulate = Colors.Green;
	}

	public void ResetMatches()
	{
		selectedLeft = -1;
		inputDisabled = false;

		for (int i = 0; i < matches.Length; i++)
			matches[i] = -1;

		UpdateButtonStyles();
	}
}
