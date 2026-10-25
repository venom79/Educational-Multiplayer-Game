using Godot;

public partial class TranslationController : Control
{
	[Signal]
	public delegate void AnswerSubmittedEventHandler(string answer);

	private Label directionLabel;
	private Label sourceTextLabel;
	private LineEdit answerInput;
	private Button submitButton;

	public override void _Ready()
	{
		directionLabel = GetNode<Label>("Layout/DirectionLabel");
		sourceTextLabel = GetNode<Label>("Layout/SourceTextLabel");
		answerInput = GetNode<LineEdit>("Layout/AnswerInput");
		submitButton = GetNode<Button>("Layout/SubmitButton");

		submitButton.Pressed += SubmitAnswer;
		answerInput.TextSubmitted += OnTextSubmitted;
	}

	public void SetTranslation(string sourceText, string direction)
	{
		if (!IsNodeReady())
		{
			CallDeferred(nameof(SetTranslation), sourceText, direction);
			return;
		}

		directionLabel.Text = direction;
		sourceTextLabel.Text = sourceText;
		ResetInput();
	}

	private void OnTextSubmitted(string text)
	{
		SubmitAnswer();
	}

	private void SubmitAnswer()
	{
		string answer = answerInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(answer))
		{
			answerInput.PlaceholderText = "Enter a translation first";
			return;
		}

		EmitSignal(SignalName.AnswerSubmitted, answer);
	}

	public void ResetInput()
	{
		answerInput.Clear();
		answerInput.PlaceholderText = "Type your translation";
		answerInput.Editable = true;
		submitButton.Disabled = false;
	}

	public void SetInputDisabled(bool disabled)
	{
		answerInput.Editable = !disabled;
		submitButton.Disabled = disabled;
	}
}
