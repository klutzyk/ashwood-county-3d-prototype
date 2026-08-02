#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

/// <summary>
/// Distance-driven footstep playback stays in sync when movement speed changes,
/// unlike a fixed timer, while dual emitters preserve natural overlap between
/// left and right steps.
/// </summary>
public partial class PlayerFootstepFeedback : Node
{
	[Signal]
	public delegate void FootstepPlayedEventHandler(int variationIndex, float loudness);

	[Export] public NodePath PlayerPath { get; set; } = new("..");
	[Export] public NodePath FirstEmitterPath { get; set; } = new("StepA");
	[Export] public NodePath SecondEmitterPath { get; set; } = new("StepB");
	[Export] public Godot.Collections.Array<AudioStream> FootstepSounds { get; set; } = new();
	[Export(PropertyHint.Range, "0.2,3,0.05")]
	public float WalkStepDistance { get; set; } = 1.42f;
	[Export(PropertyHint.Range, "0.2,3,0.05")]
	public float SprintStepDistance { get; set; } = 1.68f;
	[Export(PropertyHint.Range, "0.2,3,0.05")]
	public float CrouchStepDistance { get; set; } = 1.12f;
	[Export(PropertyHint.Range, "0,3,0.05")]
	public float MinimumMovementSpeed { get; set; } = 0.65f;
	[Export(PropertyHint.Range, "-40,0,0.5")]
	public float WalkVolumeDb { get; set; } = -13.0f;
	[Export(PropertyHint.Range, "-12,12,0.5")]
	public float SprintVolumeBoostDb { get; set; } = 3.5f;
	[Export(PropertyHint.Range, "-12,0,0.5")]
	public float CrouchVolumeReductionDb { get; set; } = -5.5f;
	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	public float MinimumPitch { get; set; } = 0.93f;
	[Export(PropertyHint.Range, "0.5,1.5,0.01")]
	public float MaximumPitch { get; set; } = 1.07f;
	[Export(PropertyHint.Range, "1,20,0.5")]
	public float TeleportResetDistance { get; set; } = 4.0f;

	public int PlayedStepCount { get; private set; }

	private readonly RandomNumberGenerator _random = new();
	private ThirdPersonPlayer _player = null!;
	private AudioStreamPlayer3D _firstEmitter = null!;
	private AudioStreamPlayer3D _secondEmitter = null!;
	private Vector3 _previousPosition;
	private float _travelledDistance;
	private int _lastVariation = -1;
	private bool _useFirstEmitter = true;

	public override void _Ready()
	{
		_player = GetNode<ThirdPersonPlayer>(PlayerPath);
		_firstEmitter = GetNode<AudioStreamPlayer3D>(FirstEmitterPath);
		_secondEmitter = GetNode<AudioStreamPlayer3D>(SecondEmitterPath);
		_firstEmitter.Bus = "Effects";
		_secondEmitter.Bus = "Effects";
		_previousPosition = _player.GlobalPosition;
		_random.Randomize();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 currentPosition = _player.GlobalPosition;
		Vector3 horizontalTravel = currentPosition - _previousPosition;
		horizontalTravel.Y = 0.0f;
		_previousPosition = currentPosition;

		float distance = horizontalTravel.Length();
		if (distance > Mathf.Max(TeleportResetDistance, 0.5f))
		{
			_travelledDistance = 0.0f;
			return;
		}

		Vector3 velocity = _player.Velocity;
		float horizontalSpeed = new Vector2(velocity.X, velocity.Z).Length();
		if (!_player.IsOnFloor() ||
			horizontalSpeed < MinimumMovementSpeed ||
			_player.IsInventoryUiOpen)
		{
			_travelledDistance = Mathf.Min(_travelledDistance, CurrentStepDistance() * 0.45f);
			return;
		}

		_travelledDistance += distance;
		float stepDistance = CurrentStepDistance();
		if (_travelledDistance < stepDistance)
		{
			return;
		}

		_travelledDistance = Mathf.PosMod(_travelledDistance, stepDistance);
		PlayStep();
	}

	private float CurrentStepDistance()
	{
		if (_player.IsCrouching)
		{
			return Mathf.Max(CrouchStepDistance, 0.2f);
		}

		return Mathf.Max(
			_player.IsSprinting ? SprintStepDistance : WalkStepDistance,
			0.2f);
	}

	private void PlayStep()
	{
		if (FootstepSounds.Count == 0)
		{
			return;
		}

		int variation = SelectVariation();
		AudioStream? stream = FootstepSounds[variation];
		if (stream is null)
		{
			return;
		}

		AudioStreamPlayer3D emitter = _useFirstEmitter ? _firstEmitter : _secondEmitter;
		_useFirstEmitter = !_useFirstEmitter;
		emitter.Stream = stream;
		emitter.PitchScale = _random.RandfRange(
			Mathf.Min(MinimumPitch, MaximumPitch),
			Mathf.Max(MinimumPitch, MaximumPitch));
		float movementVolume = WalkVolumeDb;
		if (_player.IsSprinting)
		{
			movementVolume += SprintVolumeBoostDb;
		}
		else if (_player.IsCrouching)
		{
			movementVolume += CrouchVolumeReductionDb;
		}
		emitter.VolumeDb = movementVolume + _random.RandfRange(-1.2f, 0.8f);
		emitter.Play();

		PlayedStepCount++;
		float loudness = Mathf.DbToLinear(movementVolume);
		EmitSignal(SignalName.FootstepPlayed, variation, loudness);
	}

	private int SelectVariation()
	{
		if (FootstepSounds.Count == 1)
		{
			_lastVariation = 0;
			return 0;
		}

		int variation = _random.RandiRange(0, FootstepSounds.Count - 1);
		if (variation == _lastVariation)
		{
			variation = (variation + _random.RandiRange(1, FootstepSounds.Count - 1)) %
				FootstepSounds.Count;
		}
		_lastVariation = variation;
		return variation;
	}
}
