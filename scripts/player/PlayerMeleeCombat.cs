#nullable enable

using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Weapons;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerMeleeCombat : Node3D
{
	[Signal]
	public delegate void AttackStartedEventHandler();

	[Signal]
	public delegate void AttackFinishedEventHandler();

	[Signal]
	public delegate void WeaponEquippedEventHandler(int slot, string displayName);

	[Signal]
	public delegate void HitConfirmedEventHandler(int targetCount);

	[Signal]
	public delegate void AttackRejectedEventHandler(float requiredStamina);

	[Export] public MeleeWeaponDefinition? WeaponDefinition { get; set; }
	[Export] public Godot.Collections.Array<MeleeWeaponDefinition> WeaponSlots { get; set; } = new();
	[Export] public float AttackDuration { get; set; } = 0.68f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float HitMoment { get; set; } = 0.23f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float AttackRestartMoment { get; set; } = 0.53f;
	[Export(PropertyHint.Range, "1,3,1")] public int MaximumComboAttacks { get; set; } = 1;
	[Export(PropertyHint.Range, "0,0.3,0.01")] public float InputBufferDuration { get; set; } = 0.12f;
	[Export(PropertyHint.Range, "0,0.8,0.01")]
	public float InputBufferOpenMoment { get; set; } = 0.22f;
	[Export(PropertyHint.Range, "0.2,0.9,0.01")]
	public float OneHandAttackCancelMoment { get; set; } = 0.45f;
	[Export(PropertyHint.Range, "0,50,0.5")]
	public float AttackStaminaCost { get; set; } = 12.0f;
	[Export(PropertyHint.Range, "0.5,1,0.01")]
	public float ComboStaminaCostMultiplier { get; set; } = 0.92f;
	[Export(PropertyHint.Range, "0.1,1,0.01")]
	public float WindupMovementSpeedMultiplier { get; set; } = 0.58f;
	[Export(PropertyHint.Range, "0.1,1,0.01")]
	public float RecoveryMovementSpeedMultiplier { get; set; } = 0.78f;
	[Export] public float ReadyPoseBlendSpeed { get; set; } = 10.0f;
	[Export(PropertyHint.Range, "1,4,0.05")]
	public float TargetAssistRange { get; set; } = 2.35f;
	[Export(PropertyHint.Range, "10,180,1")]
	public float TargetAssistArcDegrees { get; set; } = 115.0f;
	[Export(PropertyHint.Range, "1,5,0.05")]
	public float TargetAssistBreakDistance { get; set; } = 3.25f;
	[Export(PropertyHint.Range, "30,180,1")]
	public float TargetTrackingArcDegrees { get; set; } = 120.0f;
	[Export(PropertyHint.Range, "1,40,0.5")]
	public float TargetTurnSpeed { get; set; } = 18.0f;
	[Export(PropertyHint.Range, "1,4,1")]
	public int MaximumTargetsPerSwing { get; set; } = 2;
	[Export(PropertyHint.Range, "0.25,1,0.01")]
	public float SecondaryTargetDamageMultiplier { get; set; } = 0.78f;
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float SwingSoundMoment { get; set; } = 0.2f;
	[Export(PropertyHint.Range, "0,0.1,0.005")]
	public float ConfirmedHitPauseDuration { get; set; } = 0.055f;
	[Export(PropertyHint.Range, "0.01,0.12,0.005")]
	public float SwingTrailWidth { get; set; } = 0.045f;
	[Export(PropertyHint.Range, "0,0.8,0.01")]
	public float SwingTrailStartMoment { get; set; } = 0.12f;
	[Export(PropertyHint.Range, "0.2,1,0.01")]
	public float SwingTrailEndMoment { get; set; } = 0.64f;
	[Export(PropertyHint.Range, "-20,20,0.5")]
	public float WeaponWindupRollDegrees { get; set; } = -5.0f;
	[Export(PropertyHint.Range, "-20,20,0.5")]
	public float WeaponImpactRollDegrees { get; set; } = 8.0f;
	[Export(PropertyHint.Range, "-20,20,0.5")]
	public float WeaponRecoveryRollDegrees { get; set; } = -2.0f;
	[Export(PropertyHint.Range, "0,0.08,0.002")]
	public float WeaponPoseLift { get; set; } = 0.018f;
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float WeaponContactAlignmentStrength { get; set; } = 1.0f;
	[Export(PropertyHint.Range, "0.45,0.9,0.01")]
	public float WeaponContactAlignmentEndMoment { get; set; } = 0.68f;
	[Export(PropertyHint.Range, "0.05,0.5,0.01")]
	public float MaximumContactGap { get; set; } = 0.24f;
	[Export(PropertyHint.Range, "0,0.2,0.005")]
	public float TwoHandLungeDistance { get; set; } = 0.105f;
	[Export(PropertyHint.Range, "0,12,0.25")]
	public float TwoHandWeightTransferDegrees { get; set; } = 6.5f;
	[Export(PropertyHint.Layers3DPhysics)]
	public uint HitCollisionMask { get; set; } = 1;
	[Export(PropertyHint.Range, "0.3,1.8,0.05")]
	public float StrikeHeight { get; set; } = 0.9f;
	[Export] public NodePath WeaponAttachmentPath { get; set; } =
		new("../Visual/Warrior/Skeleton3D/RightHandWeaponAttachment");

	private ThirdPersonPlayer _player = null!;
	private PlayerHealth _health = null!;
	private PlayerStamina _stamina = null!;
	private Interactions.PlayerInteraction _interaction = null!;
	private PlayerAnimationController _animationController = null!;
	private PlayerMeleeAudioFeedback _audioFeedback = null!;
	private Node3D _weaponPivot = null!;
	private WeaponAttachmentController _weaponAttachment = null!;
	private Node3D _characterVisual = null!;
	private Transform3D _characterVisualBaseTransform = Transform3D.Identity;
	private float _attackElapsed;
	private float _cooldownRemaining;
	private float _bufferedAttackRemaining;
	private float _confirmedHitPauseRemaining;
	private float _readyPoseBlend = 1.0f;
	private bool _hasAppliedHit;
	private bool _hasPlayedSwingSound;
	private int _queuedComboAttacks;
	private PrototypeZombie? _assistedTarget;
	private Vector3 _attackFacingDirection = Vector3.Forward;
	private readonly List<Vector3> _swingTrailPoints = new(8);
	private ImmediateMesh _swingTrailMesh = null!;
	private MeshInstance3D _swingTrailVisual = null!;
	private StandardMaterial3D _swingTrailMaterial = null!;
	private Node3D? _equippedWeaponVisual;
	private Transform3D _equippedWeaponBaseTransform = Transform3D.Identity;
	private MeshInstance3D? _weaponTipMesh;
	private Vector3 _weaponTipLocalPosition;
	private const int MaximumSwingTrailPoints = 8;
	private const float MinimumSwingTrailPointDistance = 0.025f;

	public bool IsAttacking { get; private set; }
	public bool CanAttack => !IsAttacking &&
		_cooldownRemaining <= 0.0f &&
		(_stamina is null || _stamina.CanSpend(GetAttackStaminaCost(1)));
	public bool IsShowingReadyFeedback => !IsAttacking && _readyPoseBlend >= 0.95f;
	public int ComboStep { get; private set; }
	public int QueuedComboAttacks => _queuedComboAttacks;
	public int EquippedWeaponSlot { get; private set; }
	public PrototypeZombie? AssistedTarget => IsTargetUsable(_assistedTarget)
		? _assistedTarget
		: null;
	public bool HasAssistedTarget => AssistedTarget is not null;
	public float AttackProgress => !IsAttacking
		? 0.0f
		: Mathf.Clamp(_attackElapsed / Mathf.Max(AttackDuration, 0.05f), 0.0f, 1.0f);
	public float MovementSpeedMultiplier => !IsAttacking
		? 1.0f
		: AttackProgress < Mathf.Clamp(HitMoment, 0.0f, 1.0f)
			? Mathf.Clamp(WindupMovementSpeedMultiplier, 0.1f, 1.0f)
			: Mathf.Clamp(RecoveryMovementSpeedMultiplier, 0.1f, 1.0f);
	public Vector3 CombatFacingDirection => GetCombatFacingDirection();
	public float RequiredStaminaForNextAttack => GetAttackStaminaCost(
		IsAttacking ? GetNextComboStep() : 1);
	public bool LastAttackConnected { get; private set; }
	public float LastHitAttemptContactGap { get; private set; } = float.PositiveInfinity;
	public bool IsConfirmedHitPaused => _confirmedHitPauseRemaining > 0.0f;
	public int SwingTrailPointCount => _swingTrailPoints.Count;
	public Vector3 WeaponContactWorldPosition => GetWeaponContactWorldPosition();
	public float WeaponStrikeSegmentLength => WeaponContactWorldPosition.DistanceTo(
		_equippedWeaponVisual?.GlobalPosition ?? _weaponAttachment.GlobalPosition);
	public float AssistedTargetContactGap => AssistedTarget is PrototypeZombie target
		? GetDistanceToWeaponStrikeSegment(GetDesiredTargetContactWorldPosition(target))
		: float.PositiveInfinity;
	public float AttackBodyOffsetMagnitude =>
		(_characterVisual.Transform.Origin - _characterVisualBaseTransform.Origin).Length();
	public float WeaponStrikeDistanceTo(Vector3 worldPoint) =>
		GetDistanceToWeaponStrikeSegment(worldPoint);
	private MeleeWeaponDefinition Weapon => WeaponDefinition
		?? throw new System.InvalidOperationException("Melee combat requires a weapon definition.");

	public override void _Ready()
	{
		_player = GetParent<ThirdPersonPlayer>();
		_health = _player.GetNode<PlayerHealth>("Health");
		_stamina = _player.GetNode<PlayerStamina>("Stamina");
		_interaction = _player.GetNode<Interactions.PlayerInteraction>("Interaction");
		_animationController = _player.GetNode<PlayerAnimationController>("AnimationTree");
		_audioFeedback = GetNodeOrNull<PlayerMeleeAudioFeedback>("AudioFeedback")
			?? CreateFallbackAudioFeedback();
		_weaponPivot = GetNode<Node3D>("WeaponPivot");
		_weaponAttachment = GetNode<WeaponAttachmentController>(WeaponAttachmentPath);
		_characterVisual = _player.GetNode<Node3D>("Visual");
		_characterVisualBaseTransform = _characterVisual.Transform;
		CreateSwingTrailPresentation();
		EquipWeapon(Weapon, EquippedWeaponSlot, emitSignal: false);
	}

	private PlayerMeleeAudioFeedback CreateFallbackAudioFeedback()
	{
		PlayerMeleeAudioFeedback feedback = new()
		{
			Name = "AudioFeedback",
			Position = Vector3.Up,
			MaxDistance = 16.0f,
			UnitSize = 2.0f,
		};
		AddChild(feedback);
		return feedback;
	}

	public override void _Process(double delta)
	{
		float deltaTime = (float)delta;
		if (IsAttacking &&
			(_health.IsDead || _player.IsInventoryUiOpen || _interaction.IsInteracting))
		{
			CancelAttack();
		}
		deltaTime = ConsumeConfirmedHitPause(deltaTime);
		if (deltaTime <= 0.0f)
		{
			return;
		}
		_cooldownRemaining = Mathf.Max(_cooldownRemaining - deltaTime, 0.0f);
		if (_bufferedAttackRemaining > 0.0f)
		{
			_bufferedAttackRemaining = Mathf.Max(_bufferedAttackRemaining - deltaTime, 0.0f);
			if (CanAttack && TryAttack())
			{
				_bufferedAttackRemaining = 0.0f;
			}
		}

		if (!IsAttacking)
		{
			UpdateRestGripPose();
			float readyTarget = CanAttack ? 1.0f : 0.0f;
			_readyPoseBlend = Mathf.MoveToward(
				_readyPoseBlend,
				readyTarget,
				Mathf.Max(ReadyPoseBlendSpeed, 0.0f) * deltaTime);
			SetWeaponRestPose(_readyPoseBlend);
			return;
		}

		float duration = Mathf.Max(AttackDuration, 0.05f);
		float hitProgress = Mathf.Clamp(HitMoment, 0.0f, 1.0f);
		float nextAttackElapsed = _attackElapsed + deltaTime;
		bool reachesHitMoment = !_hasAppliedHit &&
			nextAttackElapsed >= duration * hitProgress;
		_attackElapsed = reachesHitMoment
			? Mathf.Clamp(duration * hitProgress, _attackElapsed, nextAttackElapsed)
			: nextAttackElapsed;
		float progress = Mathf.Clamp(_attackElapsed / duration, 0.0f, 1.0f);
		SetWeaponPose(progress);
		UpdateSwingTrail(progress);
		if (!_hasPlayedSwingSound &&
			progress >= Mathf.Clamp(SwingSoundMoment, 0.0f, 1.0f))
		{
			_hasPlayedSwingSound = true;
			_audioFeedback.PlayCue(IsOneHandedWeapon()
				? PlayerMeleeAudioCue.SwingLight
				: PlayerMeleeAudioCue.SwingHeavy);
		}

		if (!_hasAppliedHit && progress >= hitProgress)
		{
			_hasAppliedHit = true;
			bool connected = ApplyAttackHit();
			if (IsConfirmedHitPaused)
			{
				return;
			}
			if (!connected && reachesHitMoment)
			{
				_attackElapsed = nextAttackElapsed;
				progress = Mathf.Clamp(_attackElapsed / duration, 0.0f, 1.0f);
				SetWeaponPose(progress);
				UpdateSwingTrail(progress);
			}
		}

		if (IsOneHandedWeapon() &&
			_queuedComboAttacks > 0 &&
			progress >= GetOneHandAttackCancelMoment())
		{
			TryStartNextComboAttack();
			return;
		}

		if (!IsOneHandedWeapon() &&
			_queuedComboAttacks > 0 &&
			progress >= Mathf.Clamp(AttackRestartMoment, 0.0f, 1.0f))
		{
			ComboStep = 1;
			if (!TryStartAttackStep())
			{
				FinishAttack();
			}
			return;
		}

		if (progress >= 1.0f)
		{
			FinishAttack();
		}
	}

	public bool TryAttack()
	{
		if (IsAttacking || _cooldownRemaining > 0.0f || !CanAcceptAttackInput())
		{
			return false;
		}
		if (!_stamina.CanSpend(GetAttackStaminaCost(1)))
		{
			RejectAttack(GetAttackStaminaCost(1));
			return false;
		}

		ComboStep = 1;
		_assistedTarget = AcquireAssistedTarget();
		CaptureAttackFacingDirection();
		return TryStartAttackStep();
	}

	public bool TryEquipWeaponSlot(int slot)
	{
		if (IsAttacking ||
			!CanAcceptAttackInput() ||
			slot < 0 ||
			slot >= WeaponSlots.Count ||
			WeaponSlots[slot] is not MeleeWeaponDefinition weapon)
		{
			return false;
		}

		EquipWeapon(weapon, slot, emitSignal: true);
		return true;
	}

	public bool RequestAttack()
	{
		if (TryAttack())
		{
			return true;
		}
		if (!IsAttacking && _cooldownRemaining <= 0.0f)
		{
			return false;
		}

		if (!CanAcceptAttackInput())
		{
			return false;
		}

		if (IsAttacking)
		{
			float progress = _attackElapsed / Mathf.Max(AttackDuration, 0.05f);
			int maximumCombo = GetMaximumComboAttacks();
			int nextComboStep = maximumCombo <= 1 ? 1 : GetNextComboStep();
			float staminaCost = GetAttackStaminaCost(nextComboStep);
			if (!_stamina.CanSpend(staminaCost))
			{
				RejectAttack(staminaCost);
				return false;
			}
			if (maximumCombo <= 1)
			{
				if (progress >= Mathf.Clamp(AttackRestartMoment, 0.0f, 1.0f))
				{
					ComboStep = 1;
					return TryStartAttackStep();
				}

				if (progress < Mathf.Clamp(InputBufferOpenMoment, 0.0f, 0.8f) ||
					_queuedComboAttacks > 0)
				{
					return false;
				}
				_queuedComboAttacks = 1;
				return true;
			}

			if (_queuedComboAttacks > 0)
			{
				return false;
			}

			if (progress >= GetOneHandAttackCancelMoment())
			{
				return TryStartNextComboAttack();
			}

			_queuedComboAttacks = 1;
			return true;
		}

		if (_cooldownRemaining > Mathf.Max(InputBufferDuration, 0.0f))
		{
			return false;
		}

		if (!_stamina.CanSpend(GetAttackStaminaCost(1)))
		{
			RejectAttack(GetAttackStaminaCost(1));
			return false;
		}

		_bufferedAttackRemaining = Mathf.Max(InputBufferDuration, 0.0f);
		return _bufferedAttackRemaining > 0.0f;
	}

	private bool ApplyAttackHit()
	{
		Vector3 origin = _player.GlobalPosition;
		Vector3 forward = GetCombatFacingDirection();
		if (forward.IsZeroApprox())
		{
			forward = _player.GlobalBasis.Z.Normalized();
		}
		float maximumRange = Mathf.Max(Weapon.Range, 0.0f);
		float minimumDot = Mathf.Cos(Mathf.DegToRad(
			Mathf.Clamp(Weapon.AttackArcDegrees, 1.0f, 180.0f) * 0.5f));
		List<MeleeHitCandidate> candidates = new();
		int hitCount = 0;
		LastHitAttemptContactGap = float.PositiveInfinity;

		foreach (Node node in GetTree().GetNodesInGroup(PrototypeZombie.ZombieGroupName))
		{
			if (node is not PrototypeZombie zombie || !zombie.IsAlive)
			{
				continue;
			}

			Vector3 offset = zombie.GlobalPosition - origin;
			offset.Y = 0.0f;
			float distance = offset.Length();
			if (distance > maximumRange || distance <= 0.001f)
			{
				continue;
			}

			Vector3 direction = offset / distance;
			if (forward.Dot(direction) < minimumDot)
			{
				continue;
			}
			if (IsMeleePathBlocked(zombie))
			{
				continue;
			}
			float contactGap = GetDistanceToWeaponStrikeSegment(
				GetDesiredTargetContactWorldPosition(zombie));
			LastHitAttemptContactGap = Mathf.Min(
				LastHitAttemptContactGap,
				contactGap);
			if (contactGap > Mathf.Max(MaximumContactGap, 0.05f))
			{
				continue;
			}

			float priority = distance + ((1.0f - forward.Dot(direction)) * 2.0f);
			if (zombie == AssistedTarget)
			{
				priority -= 4.0f;
			}
			candidates.Add(new MeleeHitCandidate(zombie, direction, priority));
		}

		candidates.Sort((first, second) => first.Priority.CompareTo(second.Priority));
		int targetLimit = Mathf.Min(
			candidates.Count,
			Mathf.Max(MaximumTargetsPerSwing, 1));
		for (int targetIndex = 0; targetIndex < targetLimit; targetIndex++)
		{
			MeleeHitCandidate candidate = candidates[targetIndex];
			float damageMultiplier = targetIndex == 0
				? 1.0f
				: Mathf.Clamp(SecondaryTargetDamageMultiplier, 0.25f, 1.0f);

			if (candidate.Zombie.ReceiveMeleeHit(
				Mathf.Max(Weapon.Damage, 0.0f) * damageMultiplier,
				candidate.Direction * Mathf.Max(Weapon.Knockback, 0.0f),
				GetWeaponContactPointForTarget(candidate.Zombie)))
			{
				hitCount++;
			}
		}

		LastAttackConnected = hitCount > 0;
		if (hitCount > 0)
		{
			StartConfirmedHitPause();
			_player.RequestMeleeImpactFeedback();
			_audioFeedback.PlayCue(PlayerMeleeAudioCue.FleshImpact);
			EmitSignal(SignalName.HitConfirmed, hitCount);
		}
		return hitCount > 0;
	}

	private bool IsMeleePathBlocked(PrototypeZombie zombie)
	{
		Vector3 heightOffset = Vector3.Up * Mathf.Max(StrikeHeight, 0.0f);
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(
			_player.GlobalPosition + heightOffset,
			zombie.GlobalPosition + heightOffset);
		query.CollisionMask = HitCollisionMask;
		query.CollideWithAreas = false;
		query.Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
		Godot.Collections.Dictionary result =
			_player.GetWorld3D().DirectSpaceState.IntersectRay(query);
		return result.Count > 0 &&
			result["collider"].AsGodotObject() != zombie;
	}

	private readonly struct MeleeHitCandidate
	{
		public MeleeHitCandidate(PrototypeZombie zombie, Vector3 direction, float priority)
		{
			Zombie = zombie;
			Direction = direction;
			Priority = priority;
		}

		public PrototypeZombie Zombie { get; }
		public Vector3 Direction { get; }
		public float Priority { get; }
	}

	private void FinishAttack()
	{
		if (_queuedComboAttacks > 0 &&
			ComboStep < GetMaximumComboAttacks())
		{
			_queuedComboAttacks = 0;
			ComboStep++;
			if (TryStartAttackStep())
			{
				return;
			}
		}

		CompleteAttack(applyRecoveryCooldown: true);
	}

	private void CompleteAttack(bool applyRecoveryCooldown)
	{
		IsAttacking = false;
		_attackElapsed = 0.0f;
		_queuedComboAttacks = 0;
		_cooldownRemaining = !applyRecoveryCooldown || IsOneHandedWeapon()
			? 0.0f
			: Mathf.Max(Weapon.Cooldown, 0.0f);
		_readyPoseBlend = 0.0f;
		_assistedTarget = null;
		ClearSwingTrail();
		ResetWeaponPresentationPose();
		UpdateRestGripPose();
		SetWeaponRestPose(_readyPoseBlend);
		EmitSignal(SignalName.AttackFinished);
	}

	private bool TryStartAttackStep()
	{
		float staminaCost = GetAttackStaminaCost(ComboStep);
		if (!_stamina.TrySpend(staminaCost))
		{
			RejectAttack(staminaCost);
			return false;
		}

		if (!IsTargetUsable(_assistedTarget) ||
			HorizontalDistanceTo(_assistedTarget!.GlobalPosition) >
				Mathf.Max(TargetAssistBreakDistance, TargetAssistRange))
		{
			_assistedTarget = AcquireAssistedTarget();
		}
		CaptureAttackFacingDirection();
		EndConfirmedHitPause();
		IsAttacking = true;
		_attackElapsed = 0.0f;
		_bufferedAttackRemaining = 0.0f;
		_queuedComboAttacks = 0;
		_hasAppliedHit = false;
		_hasPlayedSwingSound = false;
		LastAttackConnected = false;
		LastHitAttemptContactGap = float.PositiveInfinity;
		ClearSwingTrail();
		SetWeaponPose(0.0f);
		_weaponAttachment.SetGripPose(
			WeaponAttachmentController.MeleeAttackPoseName);
		_animationController.PlayMeleeAttack(ComboStep, AttackDuration);
		_player.EmitMeleeAttackNoise(Weapon.NoiseRadius);
		EmitSignal(SignalName.AttackStarted);
		return true;
	}

	private void SetWeaponPose(float progress)
	{
		float impactProgress = Mathf.Clamp(HitMoment, 0.1f, 0.9f);
		float swingProgress = progress <= impactProgress
			? 0.72f * Mathf.SmoothStep(0.0f, 1.0f, progress / impactProgress)
			: Mathf.Lerp(
				0.72f,
				1.0f,
				(progress - impactProgress) / (1.0f - impactProgress));
		float yaw = Mathf.Lerp(68.0f, -72.0f, swingProgress);
		float roll = Mathf.Sin(progress * Mathf.Pi) * -30.0f;
		_weaponPivot.RotationDegrees = new Vector3(-18.0f, yaw, roll);
		ApplyAttackBodyPose(progress, impactProgress);
		ApplyWeaponPresentationPose(progress, impactProgress);
		ApplyWeaponContactAlignment(progress, impactProgress);
	}

	private void SetWeaponRestPose(float readyBlend)
	{
		float blend = Mathf.Clamp(readyBlend, 0.0f, 1.0f);
		_weaponPivot.RotationDegrees = new Vector3(
			Mathf.Lerp(-8.0f, -18.0f, blend),
			Mathf.Lerp(48.0f, 68.0f, blend),
			Mathf.Lerp(8.0f, 0.0f, blend));
		ResetAttackBodyPose();
		ResetWeaponPresentationPose();
	}

	private bool CanAcceptAttackInput()
	{
		return !_health.IsDead &&
			!_player.IsInventoryUiOpen &&
			!_interaction.IsInteracting &&
			!GetTree().Paused;
	}

	private void UpdateRestGripPose(bool immediate = false)
	{
		float horizontalSpeed = new Vector2(_player.Velocity.X, _player.Velocity.Z).Length();
		WeaponHandedness handedness =
			Weapon.Attachment?.Handedness ?? WeaponHandedness.TwoHanded;
		StringName targetPose = horizontalSpeed >= 0.1f
			? WeaponAttachmentController.LocomotionPoseName
			: handedness == WeaponHandedness.TwoHanded
				? WeaponAttachmentController.TwoHandIdlePoseName
				: WeaponAttachmentController.OneHandIdlePoseName;
		if (_weaponAttachment.CurrentPoseName == targetPose && !immediate)
		{
			return;
		}

		_weaponAttachment.SetGripPose(targetPose, immediate);
	}

	private void ApplyAttackBodyPose(float progress, float impactProgress)
	{
		float anticipation;
		float followThrough;
		if (progress <= impactProgress)
		{
			anticipation = Mathf.SmoothStep(
				0.0f,
				1.0f,
				progress / Mathf.Max(impactProgress, 0.001f));
			followThrough = 0.0f;
		}
		else
		{
			anticipation = 1.0f;
			followThrough = Mathf.SmoothStep(
				0.0f,
				1.0f,
				(progress - impactProgress) /
					Mathf.Max(1.0f - impactProgress, 0.001f));
		}

		float handednessStrength = IsOneHandedWeapon() ? 0.55f : 1.0f;
		float lunge = Mathf.Lerp(-0.025f, Mathf.Max(TwoHandLungeDistance, 0.0f),
			anticipation);
		lunge = Mathf.Lerp(lunge, 0.0f, followThrough);
		float weightEnvelope = Mathf.Sin(Mathf.Clamp(progress, 0.0f, 1.0f) * Mathf.Pi);
		float rotationStrength = Mathf.DegToRad(
			Mathf.Max(TwoHandWeightTransferDegrees, 0.0f)) *
			handednessStrength;
		Vector3 localOffset = new(
			-0.035f * weightEnvelope * handednessStrength,
			-0.025f * weightEnvelope * handednessStrength,
			lunge * handednessStrength);
		Basis weightTransfer = Basis.FromEuler(new Vector3(
			-rotationStrength * 0.42f * weightEnvelope,
			Mathf.Lerp(-rotationStrength, rotationStrength * 0.75f, anticipation) *
				(1.0f - (followThrough * 0.8f)),
			-rotationStrength * 0.32f * weightEnvelope));
		_characterVisual.Transform = _characterVisualBaseTransform *
			new Transform3D(weightTransfer, localOffset);
	}

	private void ResetAttackBodyPose()
	{
		_characterVisual.Transform = _characterVisualBaseTransform;
	}

	private int GetMaximumComboAttacks()
	{
		return IsOneHandedWeapon()
			? 3
			: Mathf.Clamp(MaximumComboAttacks, 1, 3);
	}

	private bool IsOneHandedWeapon()
	{
		return Weapon.Attachment?.Handedness == WeaponHandedness.OneHanded;
	}

	private float GetOneHandAttackCancelMoment()
	{
		return Mathf.Clamp(OneHandAttackCancelMoment, 0.2f, 0.9f);
	}

	private bool TryStartNextComboAttack()
	{
		_queuedComboAttacks = 0;
		ComboStep = GetNextComboStep();
		if (TryStartAttackStep())
		{
			return true;
		}

		CompleteAttack(applyRecoveryCooldown: true);
		return false;
	}

	private int GetNextComboStep()
	{
		return ComboStep >= GetMaximumComboAttacks()
			? 1
			: ComboStep + 1;
	}

	private float GetAttackStaminaCost(int comboStep)
	{
		float comboMultiplier = Mathf.Pow(
			Mathf.Clamp(ComboStaminaCostMultiplier, 0.5f, 1.0f),
			Mathf.Max(comboStep - 1, 0));
		return Mathf.Max(AttackStaminaCost, 0.0f) * comboMultiplier;
	}

	private void RejectAttack(float requiredStamina)
	{
		_audioFeedback.PlayCue(PlayerMeleeAudioCue.Exhausted);
		EmitSignal(SignalName.AttackRejected, Mathf.Max(requiredStamina, 0.0f));
	}

	private void StartConfirmedHitPause()
	{
		_confirmedHitPauseRemaining = Mathf.Max(ConfirmedHitPauseDuration, 0.0f);
		if (_confirmedHitPauseRemaining > 0.0f)
		{
			_animationController.Active = false;
		}
	}

	private float ConsumeConfirmedHitPause(float delta)
	{
		float availableDelta = Mathf.Max(delta, 0.0f);
		if (_confirmedHitPauseRemaining <= 0.0f)
		{
			return availableDelta;
		}

		float consumedDelta = Mathf.Min(
			availableDelta,
			_confirmedHitPauseRemaining);
		_confirmedHitPauseRemaining -= consumedDelta;
		if (_confirmedHitPauseRemaining <= 0.0f)
		{
			_confirmedHitPauseRemaining = 0.0f;
			_animationController.Active = true;
		}
		return availableDelta - consumedDelta;
	}

	private void EndConfirmedHitPause()
	{
		_confirmedHitPauseRemaining = 0.0f;
		_animationController.Active = true;
	}

	private void CancelAttack()
	{
		if (!IsAttacking)
		{
			return;
		}

		EndConfirmedHitPause();
		CompleteAttack(applyRecoveryCooldown: false);
	}

	private PrototypeZombie? AcquireAssistedTarget()
	{
		Vector3 aimDirection = _player.GetCombatAimDirection();
		if (aimDirection.IsZeroApprox())
		{
			aimDirection = _player.GlobalBasis.Z.Normalized();
		}

		float assistRange = Mathf.Max(TargetAssistRange, Weapon.Range);
		float minimumDot = Mathf.Cos(Mathf.DegToRad(
			Mathf.Clamp(TargetAssistArcDegrees, 10.0f, 180.0f) * 0.5f));
		PrototypeZombie? bestTarget = null;
		float bestScore = float.PositiveInfinity;
		foreach (Node node in GetTree().GetNodesInGroup(PrototypeZombie.ZombieGroupName))
		{
			if (node is not PrototypeZombie zombie || !zombie.IsAlive)
			{
				continue;
			}

			Vector3 offset = zombie.GlobalPosition - _player.GlobalPosition;
			offset.Y = 0.0f;
			float distance = offset.Length();
			if (distance <= 0.001f || distance > assistRange)
			{
				continue;
			}

			Vector3 direction = offset / distance;
			float facingDot = aimDirection.Dot(direction);
			if (facingDot < minimumDot || IsMeleePathBlocked(zombie))
			{
				continue;
			}

			float score = distance + ((1.0f - facingDot) * 3.25f);
			if (score < bestScore)
			{
				bestScore = score;
				bestTarget = zombie;
			}
		}

		return bestTarget;
	}

	private void CaptureAttackFacingDirection()
	{
		PrototypeZombie? target = AssistedTarget;
		Vector3 direction = target is null
			? _player.GetCombatAimDirection()
			: target.GlobalPosition - _player.GlobalPosition;
		direction.Y = 0.0f;
		if (!direction.IsZeroApprox())
		{
			_attackFacingDirection = direction.Normalized();
		}
	}

	private Vector3 GetCombatFacingDirection()
	{
		PrototypeZombie? target = AssistedTarget;
		if (target is not null)
		{
			Vector3 targetOffset = target.GlobalPosition - _player.GlobalPosition;
			targetOffset.Y = 0.0f;
			if (!targetOffset.IsZeroApprox() &&
				targetOffset.Length() <= Mathf.Max(TargetAssistBreakDistance, TargetAssistRange))
			{
				Vector3 targetDirection = targetOffset.Normalized();
				float minimumTrackingDot = Mathf.Cos(Mathf.DegToRad(
					Mathf.Clamp(TargetTrackingArcDegrees, 30.0f, 180.0f) * 0.5f));
				if (_attackFacingDirection.Normalized().Dot(targetDirection) >=
					minimumTrackingDot)
				{
					return targetDirection;
				}
			}
		}

		return _attackFacingDirection.IsZeroApprox()
			? _player.GlobalBasis.Z.Normalized()
			: _attackFacingDirection.Normalized();
	}

	private static bool IsTargetUsable(PrototypeZombie? target)
	{
		return target is not null &&
			GodotObject.IsInstanceValid(target) &&
			target.IsInsideTree() &&
			target.IsAlive;
	}

	private float HorizontalDistanceTo(Vector3 worldPosition)
	{
		return new Vector2(_player.GlobalPosition.X, _player.GlobalPosition.Z)
			.DistanceTo(new Vector2(worldPosition.X, worldPosition.Z));
	}

	private void CreateSwingTrailPresentation()
	{
		_swingTrailMesh = new ImmediateMesh();
		_swingTrailMaterial = new StandardMaterial3D
		{
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			AlbedoColor = Colors.White,
		};
		_swingTrailVisual = new MeshInstance3D
		{
			Name = "SwingTrail",
			Mesh = _swingTrailMesh,
			TopLevel = true,
		};
		AddChild(_swingTrailVisual);
		_swingTrailVisual.GlobalTransform = Transform3D.Identity;
	}

	private void CacheWeaponPresentationNodes()
	{
		_equippedWeaponVisual = _weaponAttachment.EquippedWeapon;
		_weaponTipMesh = _equippedWeaponVisual is null
			? null
			: FindDescendant<MeshInstance3D>(_equippedWeaponVisual);
		if (_equippedWeaponVisual is null)
		{
			_equippedWeaponBaseTransform = Transform3D.Identity;
			_weaponTipLocalPosition = Vector3.Zero;
			return;
		}

		_equippedWeaponBaseTransform = _equippedWeaponVisual.Transform;
		if (_weaponTipMesh is null || _weaponTipMesh.Mesh is null)
		{
			_weaponTipLocalPosition = Vector3.Zero;
			return;
		}

		Aabb bounds = _weaponTipMesh.GetAabb();
		Vector3 attachmentPosition = _weaponAttachment.GlobalPosition;
		float greatestDistanceSquared = -1.0f;
		for (int corner = 0; corner < 8; corner++)
		{
			Vector3 localCorner = bounds.Position + new Vector3(
				(corner & 1) == 0 ? 0.0f : bounds.Size.X,
				(corner & 2) == 0 ? 0.0f : bounds.Size.Y,
				(corner & 4) == 0 ? 0.0f : bounds.Size.Z);
			Vector3 worldCorner = _weaponTipMesh.ToGlobal(localCorner);
			float distanceSquared = worldCorner.DistanceSquaredTo(attachmentPosition);
			if (distanceSquared <= greatestDistanceSquared)
			{
				continue;
			}
			greatestDistanceSquared = distanceSquared;
			_weaponTipLocalPosition = localCorner;
		}
	}

	private void ApplyWeaponPresentationPose(float progress, float impactProgress)
	{
		if (_equippedWeaponVisual is null ||
			!GodotObject.IsInstanceValid(_equippedWeaponVisual))
		{
			return;
		}

		float rollDegrees;
		if (progress <= impactProgress)
		{
			float windupProgress = Mathf.SmoothStep(
				0.0f,
				1.0f,
				progress / Mathf.Max(impactProgress, 0.001f));
			rollDegrees = Mathf.Lerp(
				WeaponWindupRollDegrees,
				WeaponImpactRollDegrees,
				windupProgress);
		}
		else
		{
			float recoveryProgress = Mathf.SmoothStep(
				0.0f,
				1.0f,
				(progress - impactProgress) /
					Mathf.Max(1.0f - impactProgress, 0.001f));
			rollDegrees = Mathf.Lerp(
				WeaponImpactRollDegrees,
				WeaponRecoveryRollDegrees,
				recoveryProgress);
		}

		Transform3D poseOffset = new(
			Basis.FromEuler(new Vector3(0.0f, 0.0f, Mathf.DegToRad(rollDegrees))),
			Vector3.Up * (Mathf.Sin(progress * Mathf.Pi) *
				Mathf.Max(WeaponPoseLift, 0.0f)));
		_equippedWeaponVisual.Transform = _equippedWeaponBaseTransform * poseOffset;
	}

	private void ResetWeaponPresentationPose()
	{
		if (_equippedWeaponVisual is not null &&
			GodotObject.IsInstanceValid(_equippedWeaponVisual))
		{
			_equippedWeaponVisual.Transform = _equippedWeaponBaseTransform;
		}
	}

	private void ApplyWeaponContactAlignment(float progress, float impactProgress)
	{
		PrototypeZombie? target = AssistedTarget;
		if (target is null || _equippedWeaponVisual is null ||
			!GodotObject.IsInstanceValid(_equippedWeaponVisual))
		{
			return;
		}

		float startMoment = Mathf.Clamp(SwingTrailStartMoment, 0.0f, impactProgress);
		float endMoment = Mathf.Clamp(
			WeaponContactAlignmentEndMoment,
			impactProgress + 0.01f,
			0.9f);
		if (progress < startMoment || progress > endMoment)
		{
			return;
		}

		float envelope = progress <= impactProgress
			? Mathf.SmoothStep(
				0.0f,
				1.0f,
				(progress - startMoment) /
					Mathf.Max(impactProgress - startMoment, 0.001f))
			: 1.0f - Mathf.SmoothStep(
				0.0f,
				1.0f,
				(progress - impactProgress) /
					Mathf.Max(endMoment - impactProgress, 0.001f));
		float strength = envelope *
			Mathf.Clamp(WeaponContactAlignmentStrength, 0.0f, 1.0f);
		if (strength <= 0.001f)
		{
			return;
		}

		Vector3 pivotPosition = _equippedWeaponVisual.GlobalPosition;
		Vector3 currentDirection =
			GetWeaponContactWorldPosition() - pivotPosition;
		Vector3 desiredDirection =
			GetDesiredTargetContactWorldPosition(target) - pivotPosition;
		if (currentDirection.IsZeroApprox() || desiredDirection.IsZeroApprox())
		{
			return;
		}

		Quaternion fullAlignment = new(
			currentDirection.Normalized(),
			desiredDirection.Normalized());
		Quaternion blendedAlignment = Quaternion.Identity.Slerp(
			fullAlignment,
			strength);
		_equippedWeaponVisual.GlobalBasis =
			new Basis(blendedAlignment) * _equippedWeaponVisual.GlobalBasis;
	}

	private Vector3 GetWeaponContactWorldPosition()
	{
		if (_weaponTipMesh is null || !GodotObject.IsInstanceValid(_weaponTipMesh))
		{
			return _weaponAttachment.GlobalPosition;
		}
		return _weaponTipMesh.ToGlobal(_weaponTipLocalPosition);
	}

	private Vector3 GetWeaponContactPointForTarget(PrototypeZombie target)
	{
		Vector3 gripPosition = _equippedWeaponVisual?.GlobalPosition ??
			_weaponAttachment.GlobalPosition;
		Vector3 tipPosition = GetWeaponContactWorldPosition();
		Vector3 weaponSegment = tipPosition - gripPosition;
		if (weaponSegment.LengthSquared() <= 0.0001f)
		{
			return target.GlobalPosition +
				(Vector3.Up * Mathf.Max(target.HitContactHeight, 0.0f));
		}

		Vector3 targetCenter = GetDesiredTargetContactWorldPosition(target);
		float segmentProgress = Mathf.Clamp(
			(targetCenter - gripPosition).Dot(weaponSegment) /
				weaponSegment.LengthSquared(),
			0.0f,
			1.0f);
		return gripPosition + (weaponSegment * segmentProgress);
	}

	private Vector3 GetDesiredTargetContactWorldPosition(PrototypeZombie target)
	{
		Vector3 impactDirection = target.GlobalPosition - _player.GlobalPosition;
		impactDirection.Y = 0.0f;
		impactDirection = impactDirection.IsZeroApprox()
			? GetCombatFacingDirection()
			: impactDirection.Normalized();
		return target.GlobalPosition +
			(Vector3.Up * Mathf.Max(target.HitContactHeight, 0.0f)) -
			(impactDirection * Mathf.Max(target.HitContactSurfaceOffset, 0.0f));
	}

	private float GetDistanceToWeaponStrikeSegment(Vector3 worldPoint)
	{
		Vector3 segmentStart = _equippedWeaponVisual?.GlobalPosition ??
			_weaponAttachment.GlobalPosition;
		Vector3 segmentEnd = GetWeaponContactWorldPosition();
		Vector3 segment = segmentEnd - segmentStart;
		if (segment.LengthSquared() <= 0.0001f)
		{
			return worldPoint.DistanceTo(segmentStart);
		}
		float segmentProgress = Mathf.Clamp(
			(worldPoint - segmentStart).Dot(segment) / segment.LengthSquared(),
			0.0f,
			1.0f);
		Vector3 closestPoint = segmentStart + (segment * segmentProgress);
		return worldPoint.DistanceTo(closestPoint);
	}

	private void UpdateSwingTrail(float progress)
	{
		float startMoment = Mathf.Clamp(SwingTrailStartMoment, 0.0f, 0.8f);
		float endMoment = Mathf.Clamp(
			SwingTrailEndMoment,
			startMoment + 0.01f,
			1.0f);
		if (progress < startMoment)
		{
			return;
		}
		if (progress > endMoment)
		{
			ClearSwingTrail();
			return;
		}

		Vector3 contactPoint = GetWeaponContactWorldPosition();
		if (_swingTrailPoints.Count == 0 ||
			_swingTrailPoints[^1].DistanceSquaredTo(contactPoint) >=
				MinimumSwingTrailPointDistance * MinimumSwingTrailPointDistance)
		{
			_swingTrailPoints.Add(contactPoint);
			if (_swingTrailPoints.Count > MaximumSwingTrailPoints)
			{
				_swingTrailPoints.RemoveAt(0);
			}
		}
		RebuildSwingTrailMesh();
	}

	private void RebuildSwingTrailMesh()
	{
		_swingTrailMesh.ClearSurfaces();
		if (_swingTrailPoints.Count < 2)
		{
			return;
		}

		Camera3D? camera = GetViewport().GetCamera3D();
		_swingTrailVisual.GlobalTransform = Transform3D.Identity;
		_swingTrailMesh.SurfaceBegin(
			Mesh.PrimitiveType.TriangleStrip,
			_swingTrailMaterial);
		for (int pointIndex = 0; pointIndex < _swingTrailPoints.Count; pointIndex++)
		{
			Vector3 point = _swingTrailPoints[pointIndex];
			Vector3 previous = _swingTrailPoints[Mathf.Max(pointIndex - 1, 0)];
			Vector3 next = _swingTrailPoints[
				Mathf.Min(pointIndex + 1, _swingTrailPoints.Count - 1)];
			Vector3 tangent = next - previous;
			Vector3 viewDirection = camera is null
				? Vector3.Forward
				: camera.GlobalPosition - point;
			Vector3 widthDirection = tangent.Cross(viewDirection);
			if (widthDirection.IsZeroApprox())
			{
				widthDirection = Vector3.Up;
			}
			widthDirection = widthDirection.Normalized();
			float normalizedIndex = pointIndex /
				Mathf.Max(_swingTrailPoints.Count - 1.0f, 1.0f);
			float width = Mathf.Max(SwingTrailWidth, 0.005f) *
				Mathf.Lerp(0.35f, 1.0f, normalizedIndex);
			Color color = new(
				0.93f,
				0.77f,
				0.52f,
				Mathf.Lerp(0.025f, 0.34f, normalizedIndex));
			_swingTrailMesh.SurfaceSetColor(color);
			_swingTrailMesh.SurfaceAddVertex(point - (widthDirection * width));
			_swingTrailMesh.SurfaceSetColor(color);
			_swingTrailMesh.SurfaceAddVertex(point + (widthDirection * width));
		}
		_swingTrailMesh.SurfaceEnd();
	}

	private void ClearSwingTrail()
	{
		_swingTrailPoints.Clear();
		if (_swingTrailMesh is not null)
		{
			_swingTrailMesh.ClearSurfaces();
		}
	}

	private static T? FindDescendant<T>(Node node) where T : Node
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is T match)
			{
				return match;
			}
			T? descendant = FindDescendant<T>(child);
			if (descendant is not null)
			{
				return descendant;
			}
		}
		return null;
	}

	private void EquipWeapon(
		MeleeWeaponDefinition weapon,
		int slot,
		bool emitSignal)
	{
		WeaponAttachmentDefinition attachment = weapon.Attachment
			?? throw new System.InvalidOperationException(
				"Melee weapon requires an attachment definition.");
		WeaponDefinition = weapon;
		EquippedWeaponSlot = slot;
		_weaponAttachment.Equip(attachment);
		CacheWeaponPresentationNodes();
		_animationController.SetWeaponHandedness(attachment.Handedness);
		_cooldownRemaining = 0.0f;
		_bufferedAttackRemaining = 0.0f;
		_queuedComboAttacks = 0;
		_assistedTarget = null;
		ComboStep = 0;
		LastAttackConnected = false;
		ClearSwingTrail();
		UpdateRestGripPose(immediate: true);
		SetWeaponRestPose(1.0f);
		if (emitSignal)
		{
			EmitSignal(SignalName.WeaponEquipped, slot, weapon.DisplayName);
		}
	}
}
