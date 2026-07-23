#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Save;

namespace AshwoodCounty3DPrototype.UI;

public partial class SaveStatusDisplay : Label
{
	[Export] public NodePath SaveManagerPath { get; set; } = new("../../SaveGameManager");
	[Export] public float MessageDuration { get; set; } = 2.5f;

	private float _messageRemaining;

	public override void _Ready()
	{
		GetNode<SaveGameManager>(SaveManagerPath).StatusMessageRequested += ShowMessage;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		if (_messageRemaining <= 0.0f)
		{
			return;
		}

		_messageRemaining = Mathf.Max(_messageRemaining - (float)delta, 0.0f);
		Visible = _messageRemaining > 0.0f;
	}

	private void ShowMessage(string message)
	{
		Text = message;
		Visible = true;
		_messageRemaining = Mathf.Max(MessageDuration, 0.0f);
	}
}
