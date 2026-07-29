#nullable enable

using System;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodSchoolStairTraversalValidation : Node
{
	private const string SchoolPath =
		"res://assets/environment/buildings/AshwoodSchool/ashwood_school.tscn";
	private const string PlayerPath =
		"res://scenes/player/third_person_player.tscn";
	private const float GroundFloorTop = 0.12f;
	private const float UpperFloorTop = 3.48f;
	private const float StairCentreZ = 1.6f;

	private ThirdPersonPlayer _player = null!;
	private Node3D? _school;

	public override async void _Ready()
	{
		try
		{
			_school = GD.Load<PackedScene>(SchoolPath)
				.Instantiate<Node3D>();
			AddChild(_school);
			_player = GD.Load<PackedScene>(PlayerPath)
				.Instantiate<ThirdPersonPlayer>();
			_player.Name = "SchoolStairTraversalPlayer";
			AddChild(_player);

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			CollisionShape3D ramp = _school.GetNode<CollisionShape3D>(
				"AuthoredSchool/Architecture/Stairwell/" +
				"InvisibleWalkableStairRamp/Collision");
			Require(
				ramp.Shape is BoxShape3D && !ramp.Disabled,
				"production school stair ramp is active");

			float halfHeight = GetStandingHalfHeight();
			await WalkForwardOnly(
				"ascent",
				new Vector3(
					8.0f,
					GroundFloorTop + halfHeight + 0.02f,
					StairCentreZ),
				Vector3.Left,
				position => position.X <= 2.72f,
				UpperFloorTop + halfHeight,
				expectAscending: true);
			await WalkForwardOnly(
				"descent",
				new Vector3(
					2.72f,
					UpperFloorTop + halfHeight + 0.02f,
					StairCentreZ),
				Vector3.Right,
				position => position.X >= 7.85f,
				GroundFloorTop + halfHeight,
				expectAscending: false);

			ReleaseInput();
			GD.Print(
				"ASHWOOD_SCHOOL_STAIR_TRAVERSAL_VALIDATION: PASS " +
				"(real player, ascent + descent, forward only, no jump)");
			_player.QueueFree();
			_school.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			ReleaseInput();
			GD.PushError(
				"ASHWOOD_SCHOOL_STAIR_TRAVERSAL_VALIDATION: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
		}
	}

	private async Task WalkForwardOnly(
		string directionName,
		Vector3 start,
		Vector3 direction,
		Func<Vector3, bool> reachedTarget,
		float expectedStandingHeight,
		bool expectAscending)
	{
		await ResetPlayer(start, direction);
		float extremeHeight = _player.GlobalPosition.Y;
		int longestAirborneRun = 0;
		int airborneRun = 0;
		bool reached = false;

		Input.ActionPress("move_forward");
		for (int frame = 0; frame < 360; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Require(
				!Input.IsActionPressed("jump"),
				$"{directionName} never supplies jump input");
			extremeHeight = expectAscending
				? Mathf.Max(extremeHeight, _player.GlobalPosition.Y)
				: Mathf.Min(extremeHeight, _player.GlobalPosition.Y);
			if (_player.IsOnFloor())
			{
				airborneRun = 0;
			}
			else
			{
				airborneRun++;
				longestAirborneRun =
					Mathf.Max(longestAirborneRun, airborneRun);
			}

			if (reachedTarget(_player.GlobalPosition))
			{
				reached = true;
				break;
			}
		}
		Input.ActionRelease("move_forward");
		await WaitForGrounded(18);

		Require(
			reached,
			$"holding only forward completes school stair {directionName} " +
				$"(stopped at {_player.GlobalPosition})");
		Require(
			Mathf.Abs(
				_player.GlobalPosition.Y - expectedStandingHeight) <= 0.18f,
			$"{directionName} ends at the expected floor elevation " +
				$"({_player.GlobalPosition.Y:0.000} versus " +
				$"{expectedStandingHeight:0.000})");
		Require(
			_player.IsOnFloor(),
			$"{directionName} finishes grounded");
		Require(
			longestAirborneRun <= 8,
			$"{directionName} remains a walk instead of becoming a fall " +
				$"({longestAirborneRun} airborne frames)");
		if (expectAscending)
		{
			Require(
				extremeHeight >= expectedStandingHeight - 0.14f,
				"ascent reaches the upper storey");
		}
		else
		{
			Require(
				extremeHeight <= expectedStandingHeight + 0.14f,
				"descent reaches the ground storey");
		}
	}

	private async Task ResetPlayer(Vector3 position, Vector3 direction)
	{
		ReleaseInput();
		_player.Velocity = Vector3.Zero;
		_player.GlobalPosition = position;
		_player.GlobalRotation = Vector3.Zero;
		Node3D cameraRig = _player.GetNode<Node3D>("CameraRig");
		float cameraYaw = Mathf.Atan2(-direction.X, -direction.Z);
		cameraRig.GlobalBasis = new Basis(Vector3.Up, cameraYaw);

		for (int frame = 0; frame < 14; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task WaitForGrounded(int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (_player.IsOnFloor())
			{
				return;
			}
		}
	}

	private float GetStandingHalfHeight()
	{
		CollisionShape3D collision =
			_player.GetNode<CollisionShape3D>("CollisionShape3D");
		Require(
			collision.Shape is CapsuleShape3D,
			"production player uses its normal capsule collision");
		return ((CapsuleShape3D)collision.Shape).Height * 0.5f;
	}

	private static void ReleaseInput()
	{
		Input.ActionRelease("move_forward");
		Input.ActionRelease("jump");
		Input.ActionRelease("run");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
