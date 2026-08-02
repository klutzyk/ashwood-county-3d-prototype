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
	[ExportGroup("Audio")]
	[Export] public AudioStream? OpenSound { get; set; }
	[Export] public AudioStream? CloseSound { get; set; }
	[Export(PropertyHint.Range, "-40,0,0.5")]
	public float AudioVolumeDb { get; set; } = -9.0f;
	[Export(PropertyHint.Range, "1,40,0.5")]
	public float AudioMaxDistance { get; set; } = 18.0f;

	public bool IsOpen { get; private set; }
	public bool IsAnimating { get; private set; }

	private Node3D _hinge = null!;
	private Interactable _interactable = null!;
	private float _closedRotation;
	private Tween? _activeTween;
	private AudioStreamPlayer3D _audioPlayer = null!;

	public override void _Ready()
	{
		_hinge = GetNode<Node3D>(HingePath);
		_interactable = GetNode<Interactable>(InteractablePath);
		_closedRotation = _hinge.Rotation.Y;
		CreateAudioPlayer();
		OpenSound ??= GD.Load<AudioStream>(
			"res://assets/third_party/audio/kenney_rpg_audio/Audio/doorOpen_1.ogg");
		CloseSound ??= GD.Load<AudioStream>(
			"res://assets/third_party/audio/kenney_rpg_audio/Audio/doorClose_2.ogg");
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
		PlayDoorSound(IsOpen ? OpenSound : CloseSound);
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

	private void CreateAudioPlayer()
	{
		_audioPlayer = GetNodeOrNull<AudioStreamPlayer3D>("DoorAudio") ?? new AudioStreamPlayer3D
		{
			Name = "DoorAudio",
			UnitSize = 2.6f,
		};
		if (_audioPlayer.GetParent() is null)
		{
			AddChild(_audioPlayer);
		}
		_audioPlayer.VolumeDb = AudioVolumeDb;
		_audioPlayer.MaxDistance = Mathf.Max(AudioMaxDistance, 1.0f);
		_audioPlayer.Bus = "Effects";
	}

	private void PlayDoorSound(AudioStream? sound)
	{
		if (sound is null)
		{
			return;
		}

		_audioPlayer.Stream = sound;
		_audioPlayer.PitchScale = GD.Randf() * 0.08f + 0.96f;
		_audioPlayer.Play();
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
