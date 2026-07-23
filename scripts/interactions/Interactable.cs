#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class Interactable : Node3D
{
	public static readonly StringName GroupName = new("interactable");

	[Signal]
	public delegate void InteractedEventHandler(Node interactor);

	[Signal]
	public delegate void PromptConfigurationChangedEventHandler();

	[Signal]
	public delegate void AvailabilityChangedEventHandler(bool enabled);

	[Export] public string InteractionName { get; set; } = "Object";
	[Export] public string InteractionPrompt { get; set; } = "Interact with";
	[Export] public float HoldDuration { get; set; }
	[Export]
	public bool Enabled
	{
		get => _enabled;
		set
		{
			if (_enabled == value)
			{
				return;
			}
			_enabled = value;
			EmitSignal(SignalName.AvailabilityChanged, _enabled);
		}
	}

	public string PromptText => string.IsNullOrWhiteSpace(_promptOverride)
		? $"{(HoldDuration > 0.0f ? "Hold [E]" : "Press [E]")} to " +
			$"{InteractionPrompt.Trim()} {InteractionName.Trim()}"
		: _promptOverride;

	private bool _enabled = true;
	private string _promptOverride = string.Empty;

	public override void _Ready()
	{
		AddToGroup(GroupName);
	}

	public bool Interact(Node interactor)
	{
		if (!Enabled ||
			(interactor is ThirdPersonPlayer player && !player.CanUseWorldInteractions))
		{
			return false;
		}

		EmitSignal(SignalName.Interacted, interactor);
		return true;
	}

	public void ConfigurePrompt(string action, string displayName, float holdDuration)
	{
		InteractionPrompt = action.Trim();
		InteractionName = displayName.Trim();
		HoldDuration = Mathf.Max(holdDuration, 0.0f);
		EmitSignal(SignalName.PromptConfigurationChanged);
	}

	public void SetPromptOverride(string promptText)
	{
		string trimmedPrompt = promptText.Trim();
		if (_promptOverride == trimmedPrompt)
		{
			return;
		}
		_promptOverride = trimmedPrompt;
		EmitSignal(SignalName.PromptConfigurationChanged);
	}
}
