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
	[Export] public float MaxStepHeight { get; set; } = 0.32f;
	[Export] public float GroundSnapDistance { get; set; } = 0.36f;
	[Export] public float MaxWalkableSlopeDegrees { get; set; } = 45.0f;
	[Export] public float MouseSensitivity { get; set; } = 0.0025f;
	[Export] public float SprintNoiseRadius { get; set; } = 9.0f;
	[Export] public float SprintNoiseInterval { get; set; } = 0.6f;
	[Export] public float MeleeImpactShakeStrength { get; set; } = 0.035f;
	[Export] public float MeleeImpactShakeDuration { get; set; } = 0.08f;
	[Export(PropertyHint.Range, "0,4,0.1")]
	public float MeleeImpactFovKick { get; set; } = 1.4f;
	[Export(PropertyHint.Range, "0,0.2,0.005")]
	public float DamageImpactShakeStrength { get; set; } = 0.075f;
	[Export(PropertyHint.Range, "0.05,0.5,0.01")]
	public float DamageImpactShakeDuration { get; set; } = 0.19f;
	[Export(PropertyHint.Range, "0,6,0.1")]
	public float DamageImpactFovKick { get; set; } = 2.4f;
	[Export(PropertyHint.Range, "0,0.6,0.01")]
	public float DamageStaggerDuration { get; set; } = 0.16f;
	[Export(PropertyHint.Range, "0.1,1,0.01")]
	public float DamageStaggerMovementMultiplier { get; set; } = 0.48f;
	[Export] public float CameraHeight { get; set; } = 1.15f;

	private const float MinimumPitch = -1.05f;
	private const float MaximumPitch = 0.65f;
	private const float TurnSpeed = 12.0f;
	private const float MinimumStepRise = 0.02f;
	private const float MinimumStepForwardDistance = 0.11f;
	private const float CameraStepSmoothingSpeed = 2.4f;

	private Node3D _cameraRig = null!;
	private SpringArm3D _springArm = null!;
	private Camera3D _camera = null!;
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
	private float _damageImpactShakeRemaining;
	private float _damageImpactShakeElapsed;
	private float _damageStaggerRemaining;
	private float _damageImpactSide = 1.0f;
	private float _standingCollisionHeight;
	private Vector3 _standingCollisionPosition;
	private float _cameraStepVerticalOffset;
	private float _baseCameraFov;

	// ---- dev-only noclip fly, N to toggle -----------------------------------
	// Bound directly to a Key rather than an input action so this stays a
	// single self-contained addition with nothing to wire into the project's
	// input map. N was picked because nothing else in the game binds it.
	private const float DevFlySpeed = 14.0f;
	private const float DevFlySpeedFast = 40.0f;
	private bool _devFlying;
	private bool _devFlyKeyWasDown;
	private uint _savedCollisionLayer;
	private uint _savedCollisionMask;
	private readonly PhysicsTestMotionParameters3D _stepMotionParameters = new();
	private readonly PhysicsTestMotionResult3D _stepMotionResult = new();

	public bool IsSprinting { get; private set; }
	public bool IsCrouching { get; private set; }
	public bool IsAirborne => !IsOnFloor();
	public bool IsInventoryUiOpen => _inventoryUiOpen;
	public bool IsMeleeImpactFeedbackActive => _meleeImpactShakeRemaining > 0.0f;
	public bool IsDamageFeedbackActive => _damageImpactShakeRemaining > 0.0f;
	public bool IsDamageStaggered => _damageStaggerRemaining > 0.0f;
	public bool CanUseWorldInteractions =>
		!_health.IsDead && !_inventoryUiOpen && !GetTree().Paused;

	public override void _Ready()
	{
		_cameraRig = GetNode<Node3D>("CameraRig");
		_springArm = GetNode<SpringArm3D>("CameraRig/SpringArm3D");
		_camera = GetNode<Camera3D>("CameraRig/SpringArm3D/Camera3D");
		_baseCameraFov = _camera.Fov;
		_collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");
		_collisionCapsule =
			(CapsuleShape3D)_collisionShape.Shape.Duplicate();
		_collisionShape.Shape = _collisionCapsule;
		_standingCollisionHeight = _collisionCapsule.Height;
		_standingCollisionPosition = _collisionShape.Position;
		MotionMode = MotionModeEnum.Grounded;
		UpDirection = Vector3.Up;
		FloorMaxAngle = Mathf.DegToRad(
			Mathf.Clamp(MaxWalkableSlopeDegrees, 0.0f, 89.0f));
		FloorSnapLength = Mathf.Max(GroundSnapDistance, 0.0f);
		FloorBlockOnWall = true;
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

		HandleDevFlyToggle();
		if (_devFlying)
		{
			ApplyDevFlyMovement(deltaTime);
			FollowPlayerWithCamera();
			return;
		}

		_damageStaggerRemaining = Mathf.Max(_damageStaggerRemaining - deltaTime, 0.0f);
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

		bool startedOnFloor = IsOnFloor();
		Vector3 movementDirection = GetMovementDirection();
		UpdateCrouch();
		bool jumpedThisFrame = ApplyJump();
		bool wantsToSprint =
			Input.IsActionPressed("run") &&
			!IsCrouching &&
			!_meleeCombat.IsAttacking &&
			!IsDamageStaggered &&
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
		targetSpeed *= _meleeCombat.MovementSpeedMultiplier;
		if (IsDamageStaggered)
		{
			targetSpeed *= Mathf.Clamp(DamageStaggerMovementMultiplier, 0.1f, 1.0f);
		}

		ApplyHorizontalMovement(movementDirection, targetSpeed, deltaTime);
		ApplyGravity(deltaTime);

		if (_meleeCombat.IsAttacking)
		{
			RotateTowardDirection(
				_meleeCombat.CombatFacingDirection,
				deltaTime,
				_meleeCombat.TargetTurnSpeed);
		}
		else if (!movementDirection.IsZeroApprox())
		{
			RotateTowardMovement(movementDirection, deltaTime);
		}

		MoveWithStepHandling(
			deltaTime,
			startedOnFloor,
			jumpedThisFrame,
			movementDirection);
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

	public Vector3 GetCombatAimDirection()
	{
		Vector3 direction = -_cameraRig.GlobalBasis.Z;
		direction.Y = 0.0f;
		return direction.IsZeroApprox()
			? GlobalBasis.Z.Normalized()
			: direction.Normalized();
	}

	public void RequestMeleeImpactFeedback()
	{
		_meleeImpactShakeRemaining = Mathf.Max(MeleeImpactShakeDuration, 0.0f);
		_meleeImpactShakeElapsed = 0.0f;
	}

	public void RequestZombieHitFeedback(Vector3 damageSource)
	{
		_damageImpactShakeRemaining = Mathf.Max(DamageImpactShakeDuration, 0.0f);
		_damageImpactShakeElapsed = 0.0f;
		_damageStaggerRemaining = Mathf.Max(DamageStaggerDuration, 0.0f);

		Vector3 sourceDirection = damageSource - GlobalPosition;
		sourceDirection.Y = 0.0f;
		Vector3 cameraRight = _cameraRig.GlobalBasis.X;
		cameraRight.Y = 0.0f;
		if (!sourceDirection.IsZeroApprox() && !cameraRight.IsZeroApprox())
		{
			_damageImpactSide = Mathf.Sign(
				cameraRight.Normalized().Dot(sourceDirection.Normalized()));
			if (Mathf.IsZeroApprox(_damageImpactSide))
			{
				_damageImpactSide = 1.0f;
			}
		}
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

	private bool ApplyJump()
	{
		return ApplyJump(Input.IsActionJustPressed("jump"));
	}

	private bool ApplyJump(bool jumpRequested)
	{
		if (!IsCrouching &&
			!_meleeCombat.IsAttacking &&
			!IsDamageStaggered &&
			IsOnFloor() &&
			jumpRequested)
		{
			Vector3 velocity = Velocity;
			velocity.Y = JumpVelocity;
			Velocity = velocity;
			return true;
		}

		return false;
	}

	private void MoveWithStepHandling(
		float delta,
		bool startedOnFloor,
		bool jumpedThisFrame,
		Vector3 movementDirection)
	{
		Transform3D startTransform = GlobalTransform;
		Vector3 horizontalMotion = new(
			Velocity.X * delta,
			0.0f,
			Velocity.Z * delta);
		Transform3D stepLanding = startTransform;
		bool hasStepLanding =
			startedOnFloor &&
			!jumpedThisFrame &&
			!movementDirection.IsZeroApprox() &&
			TryFindStepLanding(
				startTransform,
				horizontalMotion,
				out stepLanding);

		MoveAndSlide();

		if (hasStepLanding)
		{
			GlobalTransform = stepLanding;
			Vector3 velocity = Velocity;
			velocity.Y = Mathf.Max(velocity.Y, 0.0f);
			Velocity = velocity;
			ApplyFloorSnap();
			SmoothCameraOverGroundHeightChange(
				stepLanding.Origin.Y - startTransform.Origin.Y);
			return;
		}

		if (startedOnFloor && !jumpedThisFrame)
		{
			SmoothCameraOverGroundHeightChange(
				GlobalPosition.Y - startTransform.Origin.Y);
		}
	}

	private bool TryFindStepLanding(
		Transform3D startTransform,
		Vector3 horizontalMotion,
		out Transform3D landingTransform)
	{
		landingTransform = startTransform;
		float stepHeight = Mathf.Max(MaxStepHeight, 0.0f);
		if (stepHeight <= MinimumStepRise ||
			horizontalMotion.LengthSquared() <= 0.000001f ||
			!MotionHitsWall(startTransform, horizontalMotion))
		{
			return false;
		}

		Vector3 up = UpDirection.Normalized();
		Vector3 upwardMotion = up * stepHeight;
		if (TestBodyMotion(startTransform, upwardMotion))
		{
			return false;
		}

		Vector3 stepForwardMotion =
			horizontalMotion.Normalized() *
			Mathf.Max(
				horizontalMotion.Length(),
				MinimumStepForwardDistance);
		Transform3D raisedTransform = startTransform;
		raisedTransform.Origin += upwardMotion;
		if (TestBodyMotion(raisedTransform, stepForwardMotion))
		{
			return false;
		}

		Transform3D forwardTransform = raisedTransform;
		forwardTransform.Origin += stepForwardMotion;
		Vector3 downwardMotion =
			-up * (stepHeight + Mathf.Max(GroundSnapDistance, 0.0f));
		if (!TestBodyMotion(forwardTransform, downwardMotion) ||
			!HasWalkableLandingNormal())
		{
			return false;
		}

		Vector3 landingOrigin =
			forwardTransform.Origin + _stepMotionResult.GetTravel();
		float rise = (landingOrigin - startTransform.Origin).Dot(up);
		if (rise < MinimumStepRise || rise > stepHeight + SafeMargin + 0.01f)
		{
			return false;
		}

		landingTransform = startTransform;
		landingTransform.Origin = landingOrigin;
		return true;
	}

	private bool MotionHitsWall(Transform3D from, Vector3 motion)
	{
		if (!TestBodyMotion(from, motion))
		{
			return false;
		}

		Vector3 up = UpDirection.Normalized();
		float walkableFloorDot = Mathf.Cos(FloorMaxAngle);
		for (int collisionIndex = 0;
			collisionIndex < _stepMotionResult.GetCollisionCount();
			collisionIndex++)
		{
			float upDot = _stepMotionResult
				.GetCollisionNormal(collisionIndex)
				.Dot(up);
			if (Mathf.Abs(upDot) < walkableFloorDot)
			{
				return true;
			}
		}

		return false;
	}

	private bool HasWalkableLandingNormal()
	{
		Vector3 up = UpDirection.Normalized();
		float walkableFloorDot = Mathf.Cos(FloorMaxAngle);
		for (int collisionIndex = 0;
			collisionIndex < _stepMotionResult.GetCollisionCount();
			collisionIndex++)
		{
			if (_stepMotionResult
				.GetCollisionNormal(collisionIndex)
				.Dot(up) >= walkableFloorDot)
			{
				return true;
			}
		}

		return false;
	}

	private bool TestBodyMotion(Transform3D from, Vector3 motion)
	{
		_stepMotionParameters.From = from;
		_stepMotionParameters.Motion = motion;
		_stepMotionParameters.Margin = Mathf.Max(SafeMargin, 0.001f);
		_stepMotionParameters.MaxCollisions = 4;
		_stepMotionParameters.RecoveryAsCollision = false;
		return PhysicsServer3D.BodyTestMotion(
			GetRid(),
			_stepMotionParameters,
			_stepMotionResult);
	}

	private void SmoothCameraOverGroundHeightChange(float verticalChange)
	{
		float snapDistance = Mathf.Max(GroundSnapDistance, 0.0f);
		float maximumSmoothedChange =
			Mathf.Max(Mathf.Max(MaxStepHeight, 0.0f), snapDistance) + 0.03f;
		if (Mathf.Abs(verticalChange) < MinimumStepRise ||
			Mathf.Abs(verticalChange) > maximumSmoothedChange)
		{
			return;
		}

		_cameraStepVerticalOffset = Mathf.Clamp(
			_cameraStepVerticalOffset - verticalChange,
			-snapDistance,
			snapDistance);
	}

	private void UpdateCrouch()
	{
		if (_meleeCombat.IsAttacking || IsDamageStaggered)
		{
			return;
		}

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
		RotateTowardDirection(direction, delta, TurnSpeed);
	}

	private void RotateTowardDirection(Vector3 direction, float delta, float turnSpeed)
	{
		if (direction.IsZeroApprox())
		{
			return;
		}

		float targetRotation = Mathf.Atan2(direction.X, direction.Z);
		Rotation = new Vector3(
			0.0f,
			Mathf.LerpAngle(Rotation.Y, targetRotation, Mathf.Max(turnSpeed, 0.0f) * delta),
			0.0f);
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
		float delta = (float)GetPhysicsProcessDeltaTime();
		_cameraStepVerticalOffset = Mathf.MoveToward(
			_cameraStepVerticalOffset,
			0.0f,
			CameraStepSmoothingSpeed * delta);
		Vector3 shakeOffset = Vector3.Zero;
		float impactFade = 0.0f;
		if (_meleeImpactShakeRemaining > 0.0f)
		{
			_meleeImpactShakeRemaining = Mathf.Max(_meleeImpactShakeRemaining - delta, 0.0f);
			_meleeImpactShakeElapsed += delta;
			float duration = Mathf.Max(MeleeImpactShakeDuration, 0.001f);
			float fade = _meleeImpactShakeRemaining / duration;
			impactFade = fade;
			float strength = Mathf.Max(MeleeImpactShakeStrength, 0.0f) * fade;
			shakeOffset = new Vector3(
				Mathf.Sin(_meleeImpactShakeElapsed * 115.0f),
				Mathf.Sin(_meleeImpactShakeElapsed * 83.0f),
				0.0f) * strength;
		}
		float damageFade = 0.0f;
		if (_damageImpactShakeRemaining > 0.0f)
		{
			_damageImpactShakeRemaining = Mathf.Max(
				_damageImpactShakeRemaining - delta,
				0.0f);
			_damageImpactShakeElapsed += delta;
			float duration = Mathf.Max(DamageImpactShakeDuration, 0.001f);
			damageFade = _damageImpactShakeRemaining / duration;
			float strength = Mathf.Max(DamageImpactShakeStrength, 0.0f) *
				Mathf.Pow(damageFade, 1.35f);
			shakeOffset += new Vector3(
				_damageImpactSide * Mathf.Sin(_damageImpactShakeElapsed * 72.0f),
				-Mathf.Abs(Mathf.Sin(_damageImpactShakeElapsed * 54.0f)) * 0.7f,
				0.0f) * strength;
		}
		_camera.Fov = _baseCameraFov -
			(Mathf.Max(MeleeImpactFovKick, 0.0f) * impactFade) +
			(Mathf.Max(DamageImpactFovKick, 0.0f) * damageFade);

		_cameraRig.GlobalPosition =
			GlobalPosition +
			(Vector3.Up * (CameraHeight + _cameraStepVerticalOffset)) +
			shakeOffset;
	}
}
