
using Godot;

public partial class MultipleChoiceController : Control
{
	[Signal]
	public delegate void OptionSelectedEventHandler(int optionIndex);

	private Button[] _optionButtons = System.Array.Empty<Button>();

	public override void _Ready()
	{
		_optionButtons = new Button[]
		{
			GetNode<Button>("OptionsGrid/Option1"),
			GetNode<Button>("OptionsGrid/Option2"),
			GetNode<Button>("OptionsGrid/Option3"),
			GetNode<Button>("OptionsGrid/Option4")
		};

		for (int i = 0; i < _optionButtons.Length; i++)
		{
			int index = i;
			_optionButtons[i].Pressed += () => OnOptionPressed(index);
		}
	}

	public void SetOptions(string[] options)
	{
		if (!IsNodeReady())
		{
			CallDeferred(nameof(SetOptions), options);
			return;
		}

		for (int i = 0; i < _optionButtons.Length; i++)
		{
			bool available = i < options.Length;

			_optionButtons[i].Visible = available;
			_optionButtons[i].Disabled = false;
			_optionButtons[i].Modulate = Colors.White;

			if (available)
				_optionButtons[i].Text = options[i];
		}
	}

	public void SetOptionFeedback(int optionIndex, bool correct)
	{
		if (optionIndex < 0 || optionIndex >= _optionButtons.Length)
			return;

		_optionButtons[optionIndex].Modulate =
			correct ? Colors.Green : Colors.Red;
	}

	public void SetOptionsDisabled(bool disabled)
	{
		foreach (Button button in _optionButtons)
			button.Disabled = disabled;
	}

	public void ResetOption(int optionIndex)
	{
		if (optionIndex < 0 || optionIndex >= _optionButtons.Length)
			return;

		_optionButtons[optionIndex].Modulate = Colors.White;
		_optionButtons[optionIndex].Disabled = false;
	}

	private void OnOptionPressed(int index)
	{
		EmitSignal(SignalName.OptionSelected, index);
	}
}
