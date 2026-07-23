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
	[Export] public float EyeHeight { get; set; } = 0.8f;
	[Export(PropertyHint.Layers3DPhysics)] public uint LineOfSightCollisionMask { get; set; } = 1;

	public Interactable? CurrentInteractable { get; private set; }
	public string CurrentPromptText => CurrentInteractable?.PromptText ?? string.Empty;
	public bool IsInteracting => _activeInteractable is not null;

	private ThirdPersonPlayer _player = null!;
	private Interactable? _activeInteractable;
	private float _interactionElapsed;
	private PhysicsRayQueryParameters3D _lineOfSightQuery = null!;

	public override void _Ready()
	{
		_player = GetParent<ThirdPersonPlayer>();
		_lineOfSightQuery = PhysicsRayQueryParameters3D.Create(Vector3.Zero, Vector3.Zero);
		_lineOfSightQuery.CollisionMask = LineOfSightCollisionMask;
		_lineOfSightQuery.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_player.CanUseWorldInteractions)
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
		if (!_player.CanUseWorldInteractions || @event is InputEventKey { Echo: true })
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
		EmitSignal(SignalName.PromptChanged, CurrentPromptText);
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
			if (node is not Interactable interactable || !interactable.Enabled ||
				!interactable.IsInsideTree())
			{
				continue;
			}

			float distanceSquared = _player.GlobalPosition.DistanceSquaredTo(interactable.GlobalPosition);
			if (distanceSquared > closestDistanceSquared || !HasLineOfSight(interactable))
			{
				continue;
			}

			closest = interactable;
			closestDistanceSquared = distanceSquared;
		}

		return closest;
	}

	private bool HasLineOfSight(Interactable interactable)
	{
		_lineOfSightQuery.From = _player.GlobalPosition + (Vector3.Up * EyeHeight);
		_lineOfSightQuery.To = interactable.GlobalPosition;
		_lineOfSightQuery.CollisionMask = LineOfSightCollisionMask;
		Godot.Collections.Dictionary hit =
			_player.GetWorld3D().DirectSpaceState.IntersectRay(_lineOfSightQuery);
		if (hit.Count == 0)
		{
			return true;
		}

		Node? collider = hit["collider"].AsGodotObject() as Node;
		Node interactionOwner = FindInteractionOwner(interactable);
		return collider == interactionOwner ||
			(collider is not null && interactionOwner.IsAncestorOf(collider));
	}

	private static Node FindInteractionOwner(Interactable interactable)
	{
		Node? ancestor = interactable.GetParent();
		Node fallback = ancestor ?? interactable;
		while (ancestor is not null)
		{
			if (ancestor is CollisionObject3D)
			{
				return ancestor;
			}
			ancestor = ancestor.GetParent();
		}
		return fallback;
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

		if (CurrentInteractable is not null)
		{
			CurrentInteractable.PromptConfigurationChanged -= OnPromptConfigurationChanged;
			CurrentInteractable.AvailabilityChanged -= OnAvailabilityChanged;
		}
		CurrentInteractable = interactable;
		if (CurrentInteractable is not null)
		{
			CurrentInteractable.PromptConfigurationChanged += OnPromptConfigurationChanged;
			CurrentInteractable.AvailabilityChanged += OnAvailabilityChanged;
		}
		EmitSignal(SignalName.PromptChanged, CurrentPromptText);
	}

	private void OnPromptConfigurationChanged()
	{
		EmitSignal(SignalName.PromptChanged, CurrentPromptText);
	}

	private void OnAvailabilityChanged(bool enabled)
	{
		if (!enabled)
		{
			SetCurrentInteractable(null);
		}
	}
}
