#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodSchoolEntranceTraversalValidation : Node
{
	private const string WorldPath =
		"res://scenes/world/ashwood/main_street.tscn";
	private static readonly Vector3 SchoolOrigin =
		new(88.5f, 0.2f, -21.9f);

	private Node3D? _world;
	private ThirdPersonPlayer _player = null!;

	public override async void _Ready()
	{
		try
		{
			_world = GD.Load<PackedScene>(WorldPath)
				.Instantiate<Node3D>();
			AddChild(_world);
			_world.GetNode("Gameplay/Zombies").ProcessMode =
				ProcessModeEnum.Disabled;
			_player = _world.GetNode<ThirdPersonPlayer>("Gameplay/Player");

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Node3D school =
				_world.GetNode<Node3D>("Environment/AshwoodSchool");
			Require(
				school.GlobalPosition.DistanceTo(SchoolOrigin) < 0.01f,
				"test uses the school at its real Main Street placement");
			Require(
				school.HasNode(
					"AuthoredSchool/ExteriorIdentity/OpenDoubleEntrance"),
				"real school entrance is an open double-door composition");
			Require(
				school.HasNode(
					"AuthoredSchool/ExteriorIdentity/EntranceThresholdRamp"),
				"real school entrance has its authored shallow threshold");

			float halfHeight = GetStandingHalfHeight();
			Vector3 start =
				SchoolOrigin +
				new Vector3(0.0f, halfHeight + 0.02f, 14.55f);
			Vector3 direction = Vector3.Forward;
			await ResetPlayer(start, direction);

			bool reachedInterior = false;
			int longestAirborneRun = 0;
			int airborneRun = 0;
			Input.ActionPress("move_forward");
			for (int frame = 0; frame < 240; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				Require(
					!Input.IsActionPressed("jump"),
					"school entrance traversal never supplies jump input");
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

				if (_player.GlobalPosition.Z <=
					SchoolOrigin.Z + 11.5f)
				{
					reachedInterior = true;
					break;
				}
			}
			Input.ActionRelease("move_forward");
			for (int frame = 0; frame < 12; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}

			float expectedY =
				SchoolOrigin.Y + 0.12f + halfHeight;
			Require(
				reachedInterior,
				$"holding forward walks from the real sidewalk through " +
				$"the school doors (stopped at {_player.GlobalPosition})");
			Require(
				Mathf.Abs(_player.GlobalPosition.Y - expectedY) <= 0.16f,
				$"school entrance finishes at interior floor height " +
				$"({_player.GlobalPosition.Y:0.000} versus " +
				$"{expectedY:0.000})");
			Require(
				_player.IsOnFloor(),
				"school entrance traversal finishes grounded");
			Require(
				longestAirborneRun <= 6,
				"threshold behaves as a walkable incline, not a jump");

			ReleaseInput();
			GD.Print(
				"ASHWOOD_SCHOOL_ENTRANCE_TRAVERSAL_VALIDATION: PASS " +
				"(real sidewalk, real doors, forward only, no jump)");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			ReleaseInput();
			GD.PushError(
				"ASHWOOD_SCHOOL_ENTRANCE_TRAVERSAL_VALIDATION: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
		}
	}

	private async System.Threading.Tasks.Task ResetPlayer(
		Vector3 position,
		Vector3 direction)
	{
		ReleaseInput();
		_player.Velocity = Vector3.Zero;
		_player.GlobalPosition = position;
		_player.GlobalRotation = Vector3.Zero;
		float yaw = Mathf.Atan2(-direction.X, -direction.Z);
		_player.GetNode<Node3D>("CameraRig").GlobalBasis =
			new Basis(Vector3.Up, yaw);
		for (int frame = 0; frame < 14; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
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
