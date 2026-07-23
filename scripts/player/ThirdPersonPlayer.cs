using Godot;
using AshwoodCounty3DPrototype.Gameplay;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Settings;

namespace AshwoodCounty3DPrototype.Player;

public partial class ThirdPersonPlayer : CharacterBody3D
{
	[Export] public float WalkSpeed { get; set; } = 4.0f;
	[Export] public float SprintSpeed { get; set; } = 7.0f;
	[Export] public float Acceleration { get; set; } = 18.0f;
	[Export] public float Gravity { get; set; } = 24.0f;
	[Export] public float MouseSensitivity { get; set; } = 0.0025f;
	[Export] public float SprintNoiseRadius { get; set; } = 9.0f;
	[Export] public float SprintNoiseInterval { get; set; } = 0.6f;

	private const float CameraHeight = 0.75f;
	private const float MinimumPitch = -1.05f;
	private const float MaximumPitch = 0.65f;
	private const float TurnSpeed = 12.0f;

	private Node3D _cameraRig = null!;
	private SpringArm3D _springArm = null!;
	private PlayerHealth _health = null!;
	private PlayerStamina _stamina = null!;
	private PlayerInteraction _interaction = null!;
	private PlayerMeleeCombat _meleeCombat = null!;
	private float _cameraPitch = -0.2f;
	private bool _inventoryUiOpen;
	private float _sprintNoiseElapsed;
	private bool _wasSprinting;

	public bool IsSprinting { get; private set; }
	public bool IsInventoryUiOpen => _inventoryUiOpen;
	public bool CanUseWorldInteractions =>
		!_health.IsDead && !_inventoryUiOpen && !GetTree().Paused;

	public override void _Ready()
	{
		_cameraRig = GetNode<Node3D>("CameraRig");
		_springArm = GetNode<SpringArm3D>("CameraRig/SpringArm3D");
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

		if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetViewport().SetInputAsHandled();
			return;
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
		bool wantsToSprint = Input.IsActionPressed("run") && !movementDirection.IsZeroApprox();
		IsSprinting = wantsToSprint && _stamina.CanSprint;
		_stamina.UpdateStamina(IsSprinting, deltaTime);
		if (!_stamina.CanSprint)
		{
			IsSprinting = false;
		}
		UpdateSprintNoise(deltaTime);
		float targetSpeed = IsSprinting ? SprintSpeed : WalkSpeed;

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
		_cameraRig.GlobalPosition = GlobalPosition + (Vector3.Up * CameraHeight);
	}
}
