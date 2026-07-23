#nullable enable

using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerMeleeCombat : Node3D
{
	[Signal]
	public delegate void AttackStartedEventHandler();

	[Signal]
	public delegate void AttackFinishedEventHandler();

	[Export] public MeleeWeaponDefinition? WeaponDefinition { get; set; }
	[Export] public float AttackDuration { get; set; } = 0.68f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float HitMoment { get; set; } = 0.72f;
	[Export(PropertyHint.Range, "0,1,0.01")] public float ComboQueueOpenMoment { get; set; } = 0.55f;
	[Export(PropertyHint.Range, "1,3,1")] public int MaximumComboAttacks { get; set; } = 3;
	[Export(PropertyHint.Range, "0,0.3,0.01")] public float InputBufferDuration { get; set; } = 0.12f;
	[Export] public float ReadyPoseBlendSpeed { get; set; } = 10.0f;

	private ThirdPersonPlayer _player = null!;
	private PlayerAnimationController _animationController = null!;
	private Node3D _weaponPivot = null!;
	private float _attackElapsed;
	private float _cooldownRemaining;
	private float _bufferedAttackRemaining;
	private float _readyPoseBlend = 1.0f;
	private bool _hasAppliedHit;
	private bool _comboAttackQueued;

	public bool IsAttacking { get; private set; }
	public bool CanAttack => !IsAttacking && _cooldownRemaining <= 0.0f;
	public bool IsShowingReadyFeedback => !IsAttacking && _readyPoseBlend >= 0.95f;
	public int ComboStep { get; private set; }
	private MeleeWeaponDefinition Weapon => WeaponDefinition
		?? throw new System.InvalidOperationException("Melee combat requires a weapon definition.");

	public override void _Ready()
	{
		_player = GetParent<ThirdPersonPlayer>();
		_animationController = _player.GetNode<PlayerAnimationController>("AnimationTree");
		_weaponPivot = GetNode<Node3D>("WeaponPivot");
		_animationController.SetTwoHandedWeaponEquipped(true);
		SetWeaponRestPose(1.0f);
	}

	public override void _Process(double delta)
	{
		float deltaTime = (float)delta;
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
			float readyTarget = CanAttack ? 1.0f : 0.0f;
			_readyPoseBlend = Mathf.MoveToward(
				_readyPoseBlend,
				readyTarget,
				Mathf.Max(ReadyPoseBlendSpeed, 0.0f) * deltaTime);
			SetWeaponRestPose(_readyPoseBlend);
			return;
		}

		_attackElapsed += deltaTime;
		float duration = Mathf.Max(AttackDuration, 0.05f);
		float progress = Mathf.Clamp(_attackElapsed / duration, 0.0f, 1.0f);
		SetWeaponPose(progress);

		if (!_hasAppliedHit && progress >= Mathf.Clamp(HitMoment, 0.0f, 1.0f))
		{
			_hasAppliedHit = true;
			ApplyAttackHit();
		}

		if (progress >= 1.0f)
		{
			FinishAttack();
		}
	}

	public bool TryAttack()
	{
		if (!CanAttack || !CanAcceptAttackInput())
		{
			return false;
		}

		ComboStep = 1;
		StartAttackStep();
		return true;
	}

	public bool RequestAttack()
	{
		if (TryAttack())
		{
			return true;
		}

		if (!CanAcceptAttackInput())
		{
			return false;
		}

		if (IsAttacking)
		{
			float progress = _attackElapsed / Mathf.Max(AttackDuration, 0.05f);
			if (_comboAttackQueued ||
				ComboStep >= Mathf.Clamp(MaximumComboAttacks, 1, 3) ||
				progress < Mathf.Clamp(ComboQueueOpenMoment, 0.0f, 1.0f))
			{
				return false;
			}

			_comboAttackQueued = true;
			return true;
		}

		if (_cooldownRemaining > Mathf.Max(InputBufferDuration, 0.0f))
		{
			return false;
		}

		_bufferedAttackRemaining = Mathf.Max(InputBufferDuration, 0.0f);
		return _bufferedAttackRemaining > 0.0f;
	}

	private bool ApplyAttackHit()
	{
		Vector3 origin = _player.GlobalPosition;
		Vector3 forward = _player.GlobalBasis.Z;
		forward.Y = 0.0f;
		forward = forward.Normalized();
		float maximumRange = Mathf.Max(Weapon.Range, 0.0f);
		float minimumDot = Mathf.Cos(Mathf.DegToRad(
			Mathf.Clamp(Weapon.AttackArcDegrees, 1.0f, 180.0f) * 0.5f));
		HashSet<PrototypeZombie> hitZombies = new();
		bool hitAnything = false;

		foreach (Node node in GetTree().GetNodesInGroup(PrototypeZombie.ZombieGroupName))
		{
			if (node is not PrototypeZombie zombie || !zombie.IsAlive || !hitZombies.Add(zombie))
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

			hitAnything |= zombie.ReceiveMeleeHit(
				Mathf.Max(Weapon.Damage, 0.0f),
				direction * Mathf.Max(Weapon.Knockback, 0.0f));
		}

		return hitAnything;
	}

	private void FinishAttack()
	{
		if (_comboAttackQueued &&
			ComboStep < Mathf.Clamp(MaximumComboAttacks, 1, 3))
		{
			ComboStep++;
			StartAttackStep();
			return;
		}

		IsAttacking = false;
		_attackElapsed = 0.0f;
		_comboAttackQueued = false;
		_cooldownRemaining = Mathf.Max(Weapon.Cooldown, 0.0f);
		_readyPoseBlend = 0.0f;
		SetWeaponRestPose(_readyPoseBlend);
		EmitSignal(SignalName.AttackFinished);
	}

	private void StartAttackStep()
	{
		IsAttacking = true;
		_attackElapsed = 0.0f;
		_bufferedAttackRemaining = 0.0f;
		_comboAttackQueued = false;
		_hasAppliedHit = false;
		SetWeaponPose(0.0f);
		_animationController.PlayMeleeAttack(ComboStep, AttackDuration);
		_player.EmitMeleeAttackNoise(Weapon.NoiseRadius);
		EmitSignal(SignalName.AttackStarted);
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
	}

	private void SetWeaponRestPose(float readyBlend)
	{
		float blend = Mathf.Clamp(readyBlend, 0.0f, 1.0f);
		_weaponPivot.RotationDegrees = new Vector3(
			Mathf.Lerp(-8.0f, -18.0f, blend),
			Mathf.Lerp(48.0f, 68.0f, blend),
			Mathf.Lerp(8.0f, 0.0f, blend));
	}

	private bool CanAcceptAttackInput()
	{
		PlayerHealth health = _player.GetNode<PlayerHealth>("Health");
		return !health.IsDead &&
			!_player.IsInventoryUiOpen &&
			!_player.GetNode<Interactions.PlayerInteraction>("Interaction").IsInteracting;
	}
}
