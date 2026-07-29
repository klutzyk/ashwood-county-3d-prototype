#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class PoliceBasementStairTraversalValidation : Node
{
	private const string StationScenePath =
		"res://assets/environment/buildings/AshwoodPoliceStation/ashwood_police_station.tscn";
	private const string PlayerScenePath =
		"res://scenes/player/third_person_player.tscn";
	private const string StairwellPath =
		"AuthoredEnvironment/MainFloor/BasementStairwell";
	private const string MainFloorCollisionPath =
		"AuthoredEnvironment/MainFloor/Architecture/MainFloorStairFront/Collision";
	private const string BasementFloorCollisionPath =
		"AuthoredEnvironment/Basement/Architecture/BasementFloor/Collision";

	private ThirdPersonPlayer _player = null!;

	public override async void _Ready()
	{
		try
		{
			Node3D station = GD.Load<PackedScene>(StationScenePath)
				.Instantiate<Node3D>();
			AddChild(station);

			_player = GD.Load<PackedScene>(PlayerScenePath)
				.Instantiate<ThirdPersonPlayer>();
			_player.Name = "PoliceStairTraversalPlayer";
			AddChild(_player);

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Node3D stairwell = station.GetNode<Node3D>(StairwellPath);
			StaticBody3D[] steps = stairwell.GetChildren()
				.OfType<StaticBody3D>()
				.Where(step => step.Name.ToString().StartsWith(
					"VisibleStep_",
					StringComparison.Ordinal))
				.OrderBy(step => step.Name.ToString())
				.ToArray();
			Require(steps.Length == 19,
				$"real police stair flight has 19 authored steps " +
				$"(found {steps.Length})");

			CollisionShape3D rampCollision =
				stairwell.GetNode<CollisionShape3D>(
					"InvisibleWalkableStairRamp/Collision");
			Require(rampCollision.Shape is BoxShape3D &&
				!rampCollision.Disabled,
				"real police stair flight has an active walkable ramp");

			Vector3 descentDirection =
				steps[^1].GlobalPosition - steps[0].GlobalPosition;
			descentDirection.Y = 0.0f;
			descentDirection = descentDirection.Normalized();
			Require(!descentDirection.IsZeroApprox(),
				"real police stair flight has a horizontal travel direction");

			float mainFloorTop = GetBoxTop(
				station.GetNode<CollisionShape3D>(MainFloorCollisionPath));
			float basementFloorTop = GetBoxTop(
				station.GetNode<CollisionShape3D>(BasementFloorCollisionPath));
			float standingHalfHeight = GetPlayerStandingHalfHeight();
			float stairCentreZ =
				stairwell.GetNode<Node3D>(
					"InvisibleWalkableStairRamp").GlobalPosition.Z;

			Vector3 topStart =
				steps[0].GlobalPosition -
				(descentDirection * 1.25f);
			topStart.Y = mainFloorTop + standingHalfHeight + 0.02f;
			topStart.Z = stairCentreZ;

			Vector3 basementTarget =
				steps[^1].GlobalPosition +
				(descentDirection * 0.85f);
			basementTarget.Y =
				basementFloorTop + standingHalfHeight;
			basementTarget.Z = stairCentreZ;

			await ValidateDescent(
				topStart,
				basementTarget,
				descentDirection,
				basementFloorTop + standingHalfHeight);

			Vector3 bottomStart =
				steps[^1].GlobalPosition +
				(descentDirection * 1.10f);
			bottomStart.Y =
				basementFloorTop + standingHalfHeight + 0.02f;
			bottomStart.Z = stairCentreZ;

			Vector3 mainFloorTarget =
				steps[0].GlobalPosition -
				(descentDirection * 0.85f);
			mainFloorTarget.Y = mainFloorTop + standingHalfHeight;
			mainFloorTarget.Z = stairCentreZ;

			await ValidateAscent(
				bottomStart,
				mainFloorTarget,
				-descentDirection,
				mainFloorTop + standingHalfHeight);

			ReleaseMovementInput();
			GD.Print("POLICE_BASEMENT_STAIR_TRAVERSAL_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			ReleaseMovementInput();
			GD.PushError(
				"POLICE_BASEMENT_STAIR_TRAVERSAL_VALIDATION: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
		}
	}

	private async Task ValidateDescent(
		Vector3 start,
		Vector3 target,
		Vector3 direction,
		float expectedStandingHeight)
	{
		await ResetPlayer(start, direction);
		bool reachedBasement = false;
		int consecutiveAirborneFrames = 0;
		int longestAirborneRun = 0;
		float lowestHeight = _player.GlobalPosition.Y;

		Input.ActionPress("move_forward");
		for (int frame = 0; frame < 300; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Require(!Input.IsActionPressed("jump"),
				"descent test never supplies jump input");
			lowestHeight = Mathf.Min(
				lowestHeight,
				_player.GlobalPosition.Y);
			if (!_player.IsOnFloor())
			{
				consecutiveAirborneFrames++;
				longestAirborneRun = Mathf.Max(
					longestAirborneRun,
					consecutiveAirborneFrames);
			}
			else
			{
				consecutiveAirborneFrames = 0;
			}
			if ((_player.GlobalPosition - target).Dot(direction) >= 0.0f)
			{
				reachedBasement = true;
				break;
			}
		}
		Input.ActionRelease("move_forward");
		await WaitForGrounded(12);

		Require(reachedBasement,
			$"holding forward walks down the real police basement stairs " +
			$"without jumping (stopped at {_player.GlobalPosition})");
		Require(lowestHeight <= expectedStandingHeight + 0.12f,
			"descent reaches the basement floor elevation");
		Require(
			Mathf.Abs(_player.GlobalPosition.Y - expectedStandingHeight) <= 0.16f,
			$"descent finishes at basement standing height " +
			$"({_player.GlobalPosition.Y:0.000} versus " +
			$"{expectedStandingHeight:0.000})");
		Require(_player.IsOnFloor(),
			"descent finishes grounded on the basement floor");
		Require(longestAirborneRun <= 8,
			$"descent remains grounded instead of becoming a fall " +
			$"({longestAirborneRun} consecutive airborne frames)");
	}

	private async Task ValidateAscent(
		Vector3 start,
		Vector3 target,
		Vector3 direction,
		float expectedStandingHeight)
	{
		await ResetPlayer(start, direction);
		bool reachedMainFloor = false;
		float highestHeight = _player.GlobalPosition.Y;

		Input.ActionPress("move_forward");
		for (int frame = 0; frame < 300; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			Require(!Input.IsActionPressed("jump"),
				"ascent test never supplies jump input");
			highestHeight = Mathf.Max(
				highestHeight,
				_player.GlobalPosition.Y);
			if ((_player.GlobalPosition - target).Dot(direction) >= 0.0f)
			{
				reachedMainFloor = true;
				break;
			}
		}
		Input.ActionRelease("move_forward");
		await WaitForGrounded(12);

		Require(reachedMainFloor,
			$"holding forward walks back up the real police basement stairs " +
			$"without jumping (stopped at {_player.GlobalPosition})");
		Require(highestHeight >= expectedStandingHeight - 0.12f,
			"ascent reaches the main-floor elevation");
		Require(
			Mathf.Abs(_player.GlobalPosition.Y - expectedStandingHeight) <= 0.16f,
			$"ascent finishes at main-floor standing height " +
			$"({_player.GlobalPosition.Y:0.000} versus " +
			$"{expectedStandingHeight:0.000})");
		Require(_player.IsOnFloor(),
			"ascent finishes grounded on the main floor");
	}

	private async Task ResetPlayer(Vector3 position, Vector3 direction)
	{
		ReleaseMovementInput();
		_player.Velocity = Vector3.Zero;
		_player.GlobalPosition = position;
		_player.GlobalRotation = Vector3.Zero;

		float cameraYaw =
			Mathf.Atan2(-direction.X, -direction.Z);
		Node3D cameraRig = _player.GetNode<Node3D>("CameraRig");
		cameraRig.GlobalBasis =
			new Basis(Vector3.Up, cameraYaw);

		for (int frame = 0; frame < 12; frame++)
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

	private float GetPlayerStandingHalfHeight()
	{
		CollisionShape3D playerCollision =
			_player.GetNode<CollisionShape3D>("CollisionShape3D");
		Require(playerCollision.Shape is CapsuleShape3D,
			"actual player uses its production capsule collision");
		return ((CapsuleShape3D)playerCollision.Shape).Height * 0.5f;
	}

	private static float GetBoxTop(CollisionShape3D collision)
	{
		if (collision.Shape is not BoxShape3D box)
		{
			throw new InvalidOperationException(
				$"{collision.GetPath()} is not a box floor collision");
		}

		return collision.GlobalPosition.Y + (box.Size.Y * 0.5f);
	}

	private static void ReleaseMovementInput()
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
