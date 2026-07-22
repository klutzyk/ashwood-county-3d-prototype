#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class PlayerInteraction : Node
{
	[Signal]
	public delegate void PromptChangedEventHandler(string promptText);

	[Signal]
	public delegate void InteractionProgressChangedEventHandler(float progress, bool visible);

	[Export] public float InteractionRange { get; set; } = 3.0f;

	public Interactable? CurrentInteractable { get; private set; }
	public string CurrentPromptText => CurrentInteractable?.PromptText ?? string.Empty;
	public bool IsInteracting => _activeInteractable is not null;

	private Node3D _player = null!;
	private PlayerHealth _health = null!;
	private Interactable? _activeInteractable;
	private float _interactionElapsed;

	public override void _Ready()
	{
		_player = GetParent<Node3D>();
		_health = _player.GetNode<PlayerHealth>("Health");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_health.IsDead)
		{
			CancelInteraction();
			SetCurrentInteractable(null);
			return;
		}

		SetCurrentInteractable(FindClosestInteractable());
		UpdateActiveInteraction((float)delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_health.IsDead || @event is InputEventKey { Echo: true })
		{
			return;
		}

		if (@event.IsActionReleased("interact"))
		{
			CancelInteraction();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (!@event.IsActionPressed("interact") || CurrentInteractable is null)
		{
			return;
		}

		if (CurrentInteractable.HoldDuration > 0.0f)
		{
			StartInteraction(CurrentInteractable);
		}
		else
		{
			CurrentInteractable.Interact(_player);
		}
		GetViewport().SetInputAsHandled();
	}

	private void StartInteraction(Interactable interactable)
	{
		_activeInteractable = interactable;
		_interactionElapsed = 0.0f;
		EmitSignal(SignalName.InteractionProgressChanged, 0.0f, true);
	}

	private void UpdateActiveInteraction(float delta)
	{
		if (_activeInteractable is null)
		{
			return;
		}

		if (CurrentInteractable != _activeInteractable || !_activeInteractable.Enabled)
		{
			CancelInteraction();
			return;
		}

		float duration = Mathf.Max(_activeInteractable.HoldDuration, 0.001f);
		_interactionElapsed = Mathf.Min(_interactionElapsed + delta, duration);
		EmitSignal(
			SignalName.InteractionProgressChanged,
			_interactionElapsed / duration,
			true);

		if (_interactionElapsed < duration)
		{
			return;
		}

		Interactable completedInteractable = _activeInteractable;
		CancelInteraction();
		completedInteractable.Interact(_player);
	}

	private void CancelInteraction()
	{
		if (_activeInteractable is null)
		{
			return;
		}

		_activeInteractable = null;
		_interactionElapsed = 0.0f;
		EmitSignal(SignalName.InteractionProgressChanged, 0.0f, false);
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

		if (_activeInteractable is not null && _activeInteractable != interactable)
		{
			CancelInteraction();
		}

		CurrentInteractable = interactable;
		EmitSignal(SignalName.PromptChanged, CurrentPromptText);
	}
}
