#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class Interactable : Node3D
{
	public static readonly StringName GroupName = new("interactable");

	[Signal]
	public delegate void InteractedEventHandler(Node interactor);

	[Export] public string InteractionName { get; set; } = "Object";
	[Export] public string InteractionPrompt { get; set; } = "Interact with";
	[Export] public bool Enabled { get; set; } = true;

	public string PromptText => $"Press E to {InteractionPrompt.Trim()} {InteractionName.Trim()}";

	public override void _Ready()
	{
		AddToGroup(GroupName);
	}

	public void Interact(Node interactor)
	{
		if (Enabled)
		{
			EmitSignal(SignalName.Interacted, interactor);
		}
	}
}
