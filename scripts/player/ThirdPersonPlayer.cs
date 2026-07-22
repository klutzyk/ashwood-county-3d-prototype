using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class ThirdPersonPlayer : CharacterBody3D
{
	[Export] public float WalkSpeed { get; set; } = 4.0f;
	[Export] public float RunSpeed { get; set; } = 7.0f;
	[Export] public float Acceleration { get; set; } = 18.0f;
	[Export] public float Gravity { get; set; } = 24.0f;
	[Export] public float MouseSensitivity { get; set; } = 0.0025f;

	private const float CameraHeight = 0.75f;
	private const float MinimumPitch = -1.05f;
	private const float MaximumPitch = 0.65f;
	private const float TurnSpeed = 12.0f;

	private Node3D _cameraRig = null!;
	private SpringArm3D _springArm = null!;
	private PlayerHealth _health = null!;
	private float _cameraPitch = -0.2f;

	public override void _Ready()
	{
		_cameraRig = GetNode<Node3D>("CameraRig");
		_springArm = GetNode<SpringArm3D>("CameraRig/SpringArm3D");
		_health = GetNode<PlayerHealth>("Health");
		_cameraRig.TopLevel = true;
		FollowPlayerWithCamera();
		_springArm.Rotation = new Vector3(_cameraPitch, 0.0f, 0.0f);
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GetViewport().SetInputAsHandled();
			return;
		}

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
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
			StopHorizontalMovement();
			ApplyGravity(deltaTime);
			MoveAndSlide();
			FollowPlayerWithCamera();
			return;
		}

		Vector3 movementDirection = GetMovementDirection();
		float targetSpeed = Input.IsActionPressed("run") ? RunSpeed : WalkSpeed;

		ApplyHorizontalMovement(movementDirection, targetSpeed, deltaTime);
		ApplyGravity(deltaTime);

		if (!movementDirection.IsZeroApprox())
		{
			RotateTowardMovement(movementDirection, deltaTime);
		}

		MoveAndSlide();
		FollowPlayerWithCamera();
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
