#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Gameplay;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class DoorController : Node3D
{
	[Signal]
	public delegate void DoorStateChangedEventHandler(bool isOpen);

	[Export] public NodePath HingePath { get; set; } = new("Hinge");
	[Export] public NodePath InteractablePath { get; set; } = new("Interactable");
	[Export] public float OpenAngleDegrees { get; set; } = -95.0f;
	[Export] public float AnimationDuration { get; set; } = 0.45f;
	[Export] public bool StartsOpen { get; set; }
	[Export] public float NoiseRadius { get; set; } = 10.0f;

	public bool IsOpen { get; private set; }
	public bool IsAnimating { get; private set; }

	private Node3D _hinge = null!;
	private Interactable _interactable = null!;
	private float _closedRotation;
	private Tween? _activeTween;

	public override void _Ready()
	{
		_hinge = GetNode<Node3D>(HingePath);
		_interactable = GetNode<Interactable>(InteractablePath);
		_closedRotation = _hinge.Rotation.Y;
		IsOpen = StartsOpen;
		SetHingeRotation(GetTargetRotation());
		_interactable.Interacted += OnInteracted;
		UpdatePrompt();
	}

	public void ToggleDoor()
	{
		if (IsAnimating)
		{
			return;
		}

		IsOpen = !IsOpen;
		IsAnimating = true;
		GameplayNoise.Emit(GlobalPosition, NoiseRadius, GameplayNoiseCategory.Door);
		_interactable.SetPromptOverride(IsOpen ? "Opening Door…" : "Closing Door…");
		_activeTween = CreateTween()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_activeTween.TweenProperty(
			_hinge,
			"rotation:y",
			GetTargetRotation(),
			Mathf.Max(AnimationDuration, 0.01f));
		_activeTween.TweenCallback(Callable.From(FinishAnimation));
	}

	private void OnInteracted(Node interactor)
	{
		ToggleDoor();
	}

	private float GetTargetRotation()
	{
		return _closedRotation + (IsOpen ? Mathf.DegToRad(OpenAngleDegrees) : 0.0f);
	}

	private void SetHingeRotation(float rotationY)
	{
		Vector3 rotation = _hinge.Rotation;
		rotation.Y = rotationY;
		_hinge.Rotation = rotation;
	}

	private void UpdatePrompt()
	{
		_interactable.SetPromptOverride(string.Empty);
		_interactable.ConfigurePrompt(IsOpen ? "Close" : "Open", "Door", 0.0f);
	}

	private void FinishAnimation()
	{
		IsAnimating = false;
		UpdatePrompt();
		EmitSignal(SignalName.DoorStateChanged, IsOpen);
	}
}
