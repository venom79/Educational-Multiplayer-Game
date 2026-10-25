
using Godot;
using System;

public partial class CalculationController : Control
{
	[Signal]
	public delegate void AnswerSubmittedEventHandler(string answer);

	private LineEdit answerInput;
	private Button submitButton;

	public override void _Ready()
	{
		answerInput = GetNode<LineEdit>("Layout/AnswerInput");
		submitButton = GetNode<Button>("Layout/SubmitButton");

		submitButton.Pressed += OnSubmitPressed;
		answerInput.TextSubmitted += OnTextSubmitted;
	}

	private void OnSubmitPressed()
	{
		SubmitAnswer();
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
			answerInput.PlaceholderText = "Enter an answer first";
			return;
		}

		EmitSignal(SignalName.AnswerSubmitted, answer);
	}

	public void ResetInput()
	{
		answerInput.Clear();
		answerInput.PlaceholderText = "Enter your answer";
		answerInput.Editable = true;
		submitButton.Disabled = false;
	}

	public void SetInputDisabled(bool disabled)
	{
		answerInput.Editable = !disabled;
		submitButton.Disabled = disabled;
	}
}
