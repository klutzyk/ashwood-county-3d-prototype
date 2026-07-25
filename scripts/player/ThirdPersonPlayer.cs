using Godot;
using AshwoodCounty3DPrototype.Gameplay;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Settings;

namespace AshwoodCounty3DPrototype.Player;

public partial class ThirdPersonPlayer : CharacterBody3D
{
	[Export] public float WalkSpeed { get; set; } = 4.0f;
	[Export] public float SprintSpeed { get; set; } = 7.0f;
	[Export] public float CrouchSpeed { get; set; } = 2.2f;
	[Export] public float Acceleration { get; set; } = 18.0f;
	[Export] public float Gravity { get; set; } = 24.0f;
	[Export] public float JumpVelocity { get; set; } = 8.0f;
	[Export] public float MouseSensitivity { get; set; } = 0.0025f;
	[Export] public float SprintNoiseRadius { get; set; } = 9.0f;
	[Export] public float SprintNoiseInterval { get; set; } = 0.6f;
	[Export] public float MeleeImpactShakeStrength { get; set; } = 0.035f;
	[Export] public float MeleeImpactShakeDuration { get; set; } = 0.08f;
	[Export] public float CameraHeight { get; set; } = 1.15f;

	private const float MinimumPitch = -1.05f;
	private const float MaximumPitch = 0.65f;
	private const float TurnSpeed = 12.0f;

	private Node3D _cameraRig = null!;
	private SpringArm3D _springArm = null!;
	private CollisionShape3D _collisionShape = null!;
	private CapsuleShape3D _collisionCapsule = null!;
	private PlayerHealth _health = null!;
	private PlayerStamina _stamina = null!;
	private PlayerInteraction _interaction = null!;
	private PlayerMeleeCombat _meleeCombat = null!;
	private float _cameraPitch = -0.2f;
	private bool _inventoryUiOpen;
	private float _sprintNoiseElapsed;
	private bool _wasSprinting;
	private float _meleeImpactShakeRemaining;
	private float _meleeImpactShakeElapsed;
	private float _standingCollisionHeight;
	private Vector3 _standingCollisionPosition;

	public bool IsSprinting { get; private set; }
	public bool IsCrouching { get; private set; }
	public bool IsAirborne => !IsOnFloor();
	public bool IsInventoryUiOpen => _inventoryUiOpen;
	public bool IsMeleeImpactFeedbackActive => _meleeImpactShakeRemaining > 0.0f;
	public bool CanUseWorldInteractions =>
		!_health.IsDead && !_inventoryUiOpen && !GetTree().Paused;

	public override void _Ready()
	{
		_cameraRig = GetNode<Node3D>("CameraRig");
		_springArm = GetNode<SpringArm3D>("CameraRig/SpringArm3D");
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		_collisionCapsule =
			(CapsuleShape3D)_collisionShape.Shape.Duplicate();
		_collisionShape.Shape = _collisionCapsule;
		_standingCollisionHeight = _collisionCapsule.Height;
		_standingCollisionPosition = _collisionShape.Position;
		_health = GetNode<PlayerHealth>("Health");
		_stamina = GetNode<PlayerStamina>("Stamina");
		_interaction = GetNode<PlayerInteraction>("Interaction");
		_meleeCombat = GetNode<PlayerMeleeCombat>("MeleeCombat");
		if (SettingsManager.Instance is not null)
		{
			ApplySettings();
			SettingsManager.Instance.SettingsChanged += ApplySettings;
		}
		_cameraRig.TopLevel = true;
		FollowPlayerWithCamera();
		_springArm.Rotation = new Vector3(_cameraPitch, 0.0f, 0.0f);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _ExitTree()
	{
		if (SettingsManager.Instance is not null)
		{
			SettingsManager.Instance.SettingsChanged -= ApplySettings;
		}
	}

	private void ApplySettings()
	{
		MouseSensitivity = SettingsManager.Instance?.Current.MouseSensitivity ?? MouseSensitivity;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_inventoryUiOpen)
		{
			return;
		}

		string[] weaponActions =
			{ "weapon_slot_1", "weapon_slot_2", "weapon_slot_3" };
		for (int slot = 0; slot < weaponActions.Length; slot++)
		{
			if (@event.IsActionPressed(weaponActions[slot]) &&
				_meleeCombat.TryEquipWeaponSlot(slot))
			{
				GetViewport().SetInputAsHandled();
				return;
			}
		}

		if (@event.IsActionPressed("melee_attack"))
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
			_meleeCombat.RequestAttack();
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			RotateCamera(mouseMotion.Relative);
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaTime = (float)delta;
		if (_health.IsDead)
		{
			IsSprinting = false;
			UpdateSprintNoise(deltaTime);
			_stamina.UpdateStamina(isSprinting: false, deltaTime);
			StopHorizontalMovement();
			ApplyGravity(deltaTime);
			MoveAndSlide();
			FollowPlayerWithCamera();
			return;
		}

		if (_interaction.IsInteracting || _inventoryUiOpen)
		{
			IsSprinting = false;
			UpdateSprintNoise(deltaTime);
			_stamina.UpdateStamina(isSprinting: false, deltaTime);
			StopHorizontalMovement();
			ApplyGravity(deltaTime);
			MoveAndSlide();
			FollowPlayerWithCamera();
			return;
		}

		Vector3 movementDirection = GetMovementDirection();
		UpdateCrouch();
		ApplyJump();
		bool wantsToSprint =
			Input.IsActionPressed("run") &&
			!IsCrouching &&
			!movementDirection.IsZeroApprox();
		IsSprinting = wantsToSprint && _stamina.CanSprint;
		_stamina.UpdateStamina(IsSprinting, deltaTime);
		if (!_stamina.CanSprint)
		{
			IsSprinting = false;
		}
		UpdateSprintNoise(deltaTime);
		float targetSpeed = IsCrouching
			? CrouchSpeed
			: IsSprinting
				? SprintSpeed
				: WalkSpeed;

		ApplyHorizontalMovement(movementDirection, targetSpeed, deltaTime);
		ApplyGravity(deltaTime);

		if (!movementDirection.IsZeroApprox())
		{
			RotateTowardMovement(movementDirection, deltaTime);
		}

		MoveAndSlide();
		FollowPlayerWithCamera();
	}

	public void SetInventoryUiOpen(bool isOpen)
	{
		_inventoryUiOpen = isOpen;
		if (isOpen)
		{
			IsSprinting = false;
			StopHorizontalMovement();
		}
	}

	public void EmitMeleeAttackNoise(float noiseRadius)
	{
		if (!_health.IsDead)
		{
			GameplayNoise.Emit(
				GlobalPosition,
				Mathf.Max(noiseRadius, 0.0f),
				GameplayNoiseCategory.Melee);
		}
	}

	public void RequestMeleeImpactFeedback()
	{
		_meleeImpactShakeRemaining = Mathf.Max(MeleeImpactShakeDuration, 0.0f);
		_meleeImpactShakeElapsed = 0.0f;
	}

	private void UpdateSprintNoise(float delta)
	{
		if (!IsSprinting)
		{
			_wasSprinting = false;
			_sprintNoiseElapsed = 0.0f;
			return;
		}

		_sprintNoiseElapsed += delta;
		float interval = Mathf.Max(SprintNoiseInterval, 0.05f);
		if (!_wasSprinting || _sprintNoiseElapsed >= interval)
		{
			GameplayNoise.Emit(GlobalPosition, SprintNoiseRadius, GameplayNoiseCategory.Sprint);
			_sprintNoiseElapsed = 0.0f;
		}
		_wasSprinting = true;
	}

	private void StopHorizontalMovement()
	{
		Vector3 velocity = Velocity;
		velocity.X = 0.0f;
		velocity.Z = 0.0f;
		Velocity = velocity;
	}

	private Vector3 GetMovementDirection()
	{
		Vector2 input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 cameraRight = _cameraRig.GlobalBasis.X;
		Vector3 cameraBack = _cameraRig.GlobalBasis.Z;

		cameraRight.Y = 0.0f;
		cameraBack.Y = 0.0f;

		Vector3 direction = (cameraRight.Normalized() * input.X) + (cameraBack.Normalized() * input.Y);
		return direction.IsZeroApprox() ? Vector3.Zero : direction.Normalized();
	}

	private void ApplyHorizontalMovement(Vector3 direction, float targetSpeed, float delta)
	{
		Vector3 velocity = Velocity;
		Vector3 targetVelocity = direction * targetSpeed;
		velocity.X = Mathf.MoveToward(velocity.X, targetVelocity.X, Acceleration * delta);
		velocity.Z = Mathf.MoveToward(velocity.Z, targetVelocity.Z, Acceleration * delta);
		Velocity = velocity;
	}

	private void ApplyGravity(float delta)
	{
		if (IsOnFloor())
		{
			return;
		}

		Vector3 velocity = Velocity;
		velocity.Y -= Gravity * delta;
		Velocity = velocity;
	}

	private void ApplyJump()
	{
		if (!IsCrouching &&
			IsOnFloor() &&
			Input.IsActionJustPressed("jump"))
		{
			Vector3 velocity = Velocity;
			velocity.Y = JumpVelocity;
			Velocity = velocity;
		}
	}

	private void UpdateCrouch()
	{
		if (IsCrouching && Input.IsActionPressed("run"))
		{
			SetCrouching(false);
			return;
		}

		if (IsOnFloor() && Input.IsActionJustPressed("crouch"))
		{
			SetCrouching(!IsCrouching);
		}
	}

	private void SetCrouching(bool isCrouching)
	{
		if (IsCrouching == isCrouching)
		{
			return;
		}

		IsCrouching = isCrouching;
		float crouchHeight = Mathf.Max(
			_collisionCapsule.Radius * 2.0f,
			_standingCollisionHeight * 0.62f);
		_collisionCapsule.Height =
			IsCrouching ? crouchHeight : _standingCollisionHeight;
		_collisionShape.Position = _standingCollisionPosition +
			(Vector3.Down *
				((_standingCollisionHeight - _collisionCapsule.Height) * 0.5f));
	}

	private void RotateTowardMovement(Vector3 direction, float delta)
	{
		float targetRotation = Mathf.Atan2(direction.X, direction.Z);
		Rotation = new Vector3(0.0f, Mathf.LerpAngle(Rotation.Y, targetRotation, TurnSpeed * delta), 0.0f);
	}

	private void RotateCamera(Vector2 mouseMovement)
	{
		_cameraRig.RotateY(-mouseMovement.X * MouseSensitivity);
		_cameraPitch = Mathf.Clamp(
			_cameraPitch - (mouseMovement.Y * MouseSensitivity),
			MinimumPitch,
			MaximumPitch);
		_springArm.Rotation = new Vector3(_cameraPitch, 0.0f, 0.0f);
	}

	private void FollowPlayerWithCamera()
	{
		Vector3 shakeOffset = Vector3.Zero;
		if (_meleeImpactShakeRemaining > 0.0f)
		{
			float delta = (float)GetPhysicsProcessDeltaTime();
			_meleeImpactShakeRemaining = Mathf.Max(_meleeImpactShakeRemaining - delta, 0.0f);
			_meleeImpactShakeElapsed += delta;
			float duration = Mathf.Max(MeleeImpactShakeDuration, 0.001f);
			float fade = _meleeImpactShakeRemaining / duration;
			float strength = Mathf.Max(MeleeImpactShakeStrength, 0.0f) * fade;
			shakeOffset = new Vector3(
				Mathf.Sin(_meleeImpactShakeElapsed * 115.0f),
				Mathf.Sin(_meleeImpactShakeElapsed * 83.0f),
				0.0f) * strength;
		}

		_cameraRig.GlobalPosition =
			GlobalPosition + (Vector3.Up * CameraHeight) + shakeOffset;
	}
}
