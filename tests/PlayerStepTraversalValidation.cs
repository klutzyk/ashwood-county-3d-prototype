#nullable enable

using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class PlayerStepTraversalValidation : Node
{
	private const string PlayerScenePath =
		"res://scenes/player/third_person_player.tscn";
	private ThirdPersonPlayer _player = null!;

	public override async void _Ready()
	{
		try
		{
			CreatePhysicsCourse();
			_player = GD.Load<PackedScene>(PlayerScenePath)
				.Instantiate<ThirdPersonPlayer>();
			_player.Name = "TraversalTestPlayer";
			AddChild(_player);
			await ResetPlayer(new Vector3(0.0f, 0.92f, 0.25f));

			ValidateMovementTuning();
			await ValidateCurbAscent();
			await ValidateStairAscentAndDescent();
			await ValidateTallObstacleRejection();
			await ValidateLowCeilingRejection();
			await ValidateJumpVelocity();

			ReleaseMovementInput();
			GD.Print("PLAYER_STEP_TRAVERSAL_VALIDATION: PASS");
			_player.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			ReleaseMovementInput();
			GD.PushError(
				$"PLAYER_STEP_TRAVERSAL_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private void ValidateMovementTuning()
	{
		Require(IsNear(_player.MaxStepHeight, 0.32f),
			"normal step height is capped at 0.32 metres");
		Require(IsNear(_player.GroundSnapDistance, 0.36f),
			"downward ground snap covers ordinary stair risers");
		Require(IsNear(_player.MaxWalkableSlopeDegrees, 45.0f),
			"walkable slope tuning remains 45 degrees");
		Require(
			_player.MotionMode == CharacterBody3D.MotionModeEnum.Grounded &&
			IsNear(_player.FloorSnapLength, 0.36f) &&
			IsNear(Mathf.RadToDeg(_player.FloorMaxAngle), 45.0f, 0.1f),
			"grounded character physics receives the exported traversal tuning");
	}

	private async Task ValidateCurbAscent()
	{
		await ResetPlayer(new Vector3(0.0f, 0.92f, 0.25f));
		bool crossedCurb = await WalkForwardUntil(
			() => _player.GlobalPosition.Z < -2.0f,
			90);

		Require(crossedCurb,
			$"holding forward crosses the 0.20 metre curb " +
			$"(position {_player.GlobalPosition}, velocity {_player.Velocity}, " +
			$"floor {_player.IsOnFloor()}, wall {_player.IsOnWall()})");
		Require(_player.GlobalPosition.Y >= 1.07f &&
			_player.GlobalPosition.Y <= 1.14f,
			"the player lands on the raised curb platform");
		Require(_player.IsOnFloor(),
			"curb ascent leaves the player grounded");
	}

	private async Task ValidateStairAscentAndDescent()
	{
		await ResetPlayer(new Vector3(10.0f, 0.92f, 0.25f));
		float highestPosition = _player.GlobalPosition.Y;
		bool reachedLanding = false;
		bool completedDescent = false;
		int airborneDescentFrames = 0;

		Input.ActionPress("move_forward");
		for (int frame = 0; frame < 240; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			highestPosition = Mathf.Max(
				highestPosition,
				_player.GlobalPosition.Y);
			if (_player.GlobalPosition.Z < -4.35f)
			{
				reachedLanding = true;
			}
			if (_player.GlobalPosition.Z < -6.0f && !_player.IsOnFloor())
			{
				airborneDescentFrames++;
			}
			if (_player.GlobalPosition.Z < -8.65f)
			{
				completedDescent = true;
				break;
			}
		}
		Input.ActionRelease("move_forward");

		Require(reachedLanding && highestPosition >= 1.39f,
			"holding forward climbs three 0.18 metre stair risers");
		Require(completedDescent,
			"holding forward continues down the matching stair flight");
		Require(_player.GlobalPosition.Y >= 0.87f &&
			_player.GlobalPosition.Y <= 0.96f,
			"ground snap returns the player to pavement height");
		Require(airborneDescentFrames <= 1,
			"ordinary stair descent does not become a fall");
	}

	private async Task ValidateTallObstacleRejection()
	{
		await ResetPlayer(new Vector3(20.0f, 0.92f, 0.25f));
		Input.ActionPress("move_forward");
		for (int frame = 0; frame < 90; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Input.ActionRelease("move_forward");

		Require(_player.GlobalPosition.Z > -1.15f,
			"the 0.48 metre obstacle remains too tall to auto-step");
		Require(_player.GlobalPosition.Y < 1.0f,
			"the player is not teleported onto the tall obstacle");
	}

	private async Task ValidateLowCeilingRejection()
	{
		await ResetPlayer(new Vector3(30.0f, 0.92f, 0.25f));
		Input.ActionPress("move_forward");
		for (int frame = 0; frame < 90; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Input.ActionRelease("move_forward");

		Require(_player.GlobalPosition.Z > -1.15f,
			"the player cannot step into insufficient headroom");
		Require(_player.GlobalPosition.Y < 1.0f,
			"the headroom sweep prevents upward teleport through a ceiling");
	}

	private async Task ValidateJumpVelocity()
	{
		await ResetPlayer(new Vector3(40.0f, 0.92f, 0.25f));
		Require(_player.IsOnFloor(),
			"jump validation starts with the player grounded");

		float startingHeight = _player.GlobalPosition.Y;
		MethodInfo applyJump = typeof(ThirdPersonPlayer).GetMethod(
			"ApplyJump",
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(bool) },
			modifiers: null)
			?? throw new InvalidOperationException(
				"grounded jump implementation is unavailable");
		bool accepted = (bool)(applyJump.Invoke(
			_player,
			new object[] { true }) ?? false);

		Require(accepted &&
			IsNear(_player.Velocity.Y, _player.JumpVelocity, 0.01f),
			$"jump still applies the configured vertical velocity " +
			$"({_player.Velocity.Y:0.000} of {_player.JumpVelocity:0.000})");
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		Require(_player.GlobalPosition.Y > startingHeight + 0.08f,
			"jump still detaches and raises the player");
	}

	private async Task ResetPlayer(Vector3 position)
	{
		ReleaseMovementInput();
		_player.Velocity = Vector3.Zero;
		_player.GlobalPosition = position;
		_player.GlobalRotation = Vector3.Zero;
		Node3D cameraRig = _player.GetNode<Node3D>("CameraRig");
		cameraRig.GlobalBasis = Basis.Identity;

		for (int frame = 0; frame < 8; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private async Task<bool> WalkForwardUntil(
		Func<bool> completion,
		int maximumFrames)
	{
		Input.ActionPress("move_forward");
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (completion())
			{
				Input.ActionRelease("move_forward");
				return true;
			}
		}

		Input.ActionRelease("move_forward");
		return false;
	}

	private void CreatePhysicsCourse()
	{
		Node3D course = new() { Name = "PhysicsCourse" };
		AddChild(course);
		AddBox(
			course,
			"Ground",
			new Vector3(20.0f, -0.10f, -5.0f),
			new Vector3(60.0f, 0.20f, 30.0f));

		AddBox(
			course,
			"CurbPlatform",
			new Vector3(0.0f, 0.10f, -3.0f),
			new Vector3(4.0f, 0.20f, 4.0f));

		AddBox(course, "Stair01",
			new Vector3(10.0f, 0.09f, -1.5f),
			new Vector3(4.0f, 0.18f, 1.0f));
		AddBox(course, "Stair02",
			new Vector3(10.0f, 0.18f, -2.5f),
			new Vector3(4.0f, 0.36f, 1.0f));
		AddBox(course, "Stair03",
			new Vector3(10.0f, 0.27f, -3.5f),
			new Vector3(4.0f, 0.54f, 1.0f));
		AddBox(course, "StairLanding",
			new Vector3(10.0f, 0.27f, -5.0f),
			new Vector3(4.0f, 0.54f, 2.0f));
		AddBox(course, "StairDown02",
			new Vector3(10.0f, 0.18f, -6.5f),
			new Vector3(4.0f, 0.36f, 1.0f));
		AddBox(course, "StairDown01",
			new Vector3(10.0f, 0.09f, -7.5f),
			new Vector3(4.0f, 0.18f, 1.0f));

		AddBox(
			course,
			"TallObstacle",
			new Vector3(20.0f, 0.24f, -2.0f),
			new Vector3(4.0f, 0.48f, 1.0f));

		AddBox(
			course,
			"LowCeilingStep",
			new Vector3(30.0f, 0.10f, -2.75f),
			new Vector3(4.0f, 0.20f, 2.5f));
		AddBox(
			course,
			"LowCeiling",
			new Vector3(30.0f, 1.96f, -2.7f),
			new Vector3(4.0f, 0.20f, 3.0f));
	}

	private static void AddBox(
		Node parent,
		string name,
		Vector3 position,
		Vector3 size)
	{
		StaticBody3D body = new()
		{
			Name = name,
			Position = position,
		};
		CollisionShape3D collision = new()
		{
			Name = "Collision",
			Shape = new BoxShape3D { Size = size },
		};
		body.AddChild(collision);
		parent.AddChild(body);
	}

	private static void ReleaseMovementInput()
	{
		Input.ActionRelease("move_forward");
		Input.ActionRelease("jump");
	}

	private static bool IsNear(
		float value,
		float expected,
		float tolerance = 0.01f)
	{
		return Mathf.Abs(value - expected) <= tolerance;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
