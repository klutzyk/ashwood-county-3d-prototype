#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.UI;

public partial class InteractionPromptDisplay : Label
{
	[Export] public NodePath InteractionPath { get; set; } = new("../../Player/Interaction");
	[Export] public NodePath ProgressBarPath { get; set; } = new("../InteractionProgress");

	private PlayerInteraction _playerInteraction = null!;
	private ProgressBar _progressBar = null!;

	public override void _Ready()
	{
		_playerInteraction = GetNode<PlayerInteraction>(InteractionPath);
		_progressBar = GetNode<ProgressBar>(ProgressBarPath);
		_playerInteraction.PromptChanged += UpdatePrompt;
		_playerInteraction.InteractionProgressChanged += UpdateProgress;
		UpdatePrompt(_playerInteraction.CurrentPromptText);
		UpdateProgress(0.0f, visible: false);
	}

	private void UpdatePrompt(string promptText)
	{
		Text = promptText;
		Visible = !string.IsNullOrWhiteSpace(promptText);
	}

	private void UpdateProgress(float progress, bool visible)
	{
		_progressBar.Value = Mathf.Clamp(progress, 0.0f, 1.0f);
		_progressBar.Visible = visible;
	}
}
