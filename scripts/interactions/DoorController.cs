#nullable enable

using Godot;

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

	public bool IsOpen { get; private set; }

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
		IsOpen = !IsOpen;
		_activeTween?.Kill();
		_activeTween = CreateTween()
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		_activeTween.TweenProperty(
			_hinge,
			"rotation:y",
			GetTargetRotation(),
			Mathf.Max(AnimationDuration, 0.01f));
		_activeTween.TweenCallback(Callable.From(() =>
			EmitSignal(SignalName.DoorStateChanged, IsOpen)));
		UpdatePrompt();
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
		_interactable.InteractionName = "Door";
		_interactable.InteractionPrompt = IsOpen ? "Close" : "Open";
		_interactable.HoldDuration = 0.0f;
	}
}
