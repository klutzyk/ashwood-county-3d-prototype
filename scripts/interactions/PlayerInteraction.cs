#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class PlayerInteraction : Node
{
	[Signal]
	public delegate void PromptChangedEventHandler(string promptText);

	[Export] public float InteractionRange { get; set; } = 3.0f;

	public Interactable? CurrentInteractable { get; private set; }
	public string CurrentPromptText => CurrentInteractable?.PromptText ?? string.Empty;

	private Node3D _player = null!;
	private PlayerHealth _health = null!;

	public override void _Ready()
	{
		_player = GetParent<Node3D>();
		_health = _player.GetNode<PlayerHealth>("Health");
	}

	public override void _PhysicsProcess(double delta)
	{
		SetCurrentInteractable(_health.IsDead ? null : FindClosestInteractable());
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_health.IsDead || CurrentInteractable is null || !@event.IsActionPressed("interact"))
		{
			return;
		}

		CurrentInteractable.Interact(_player);
		GetViewport().SetInputAsHandled();
	}

	private Interactable? FindClosestInteractable()
	{
		Interactable? closest = null;
		float closestDistanceSquared = Mathf.Pow(Mathf.Max(InteractionRange, 0.0f), 2.0f);
		foreach (Node node in GetTree().GetNodesInGroup(Interactable.GroupName))
		{
			if (node is not Interactable interactable || !interactable.Enabled || !interactable.IsInsideTree())
			{
				continue;
			}

			float distanceSquared = _player.GlobalPosition.DistanceSquaredTo(interactable.GlobalPosition);
			if (distanceSquared > closestDistanceSquared)
			{
				continue;
			}

			closest = interactable;
			closestDistanceSquared = distanceSquared;
		}

		return closest;
	}

	private void SetCurrentInteractable(Interactable? interactable)
	{
		if (CurrentInteractable == interactable)
		{
			return;
		}

		CurrentInteractable = interactable;
		EmitSignal(SignalName.PromptChanged, CurrentPromptText);
	}
}
