#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.UI;

public partial class InteractionPromptDisplay : Label
{
	[Export] public NodePath InteractionPath { get; set; } = new("../../Player/Interaction");

	private PlayerInteraction _playerInteraction = null!;

	public override void _Ready()
	{
		_playerInteraction = GetNode<PlayerInteraction>(InteractionPath);
		_playerInteraction.PromptChanged += UpdatePrompt;
		UpdatePrompt(_playerInteraction.CurrentPromptText);
	}

	private void UpdatePrompt(string promptText)
	{
		Text = promptText;
		Visible = !string.IsNullOrWhiteSpace(promptText);
	}
}
