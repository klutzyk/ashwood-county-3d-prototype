#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetStorefrontTraversalValidation : Node
{
	private sealed record StorefrontRoute(
		string Name,
		string BuildingPath,
		Vector3 LocalDoorCentre,
		Vector3 LocalOutward,
		string[] DoorPaths,
		string[] ThresholdCollisionPaths);

	private static readonly IReadOnlyList<StorefrontRoute> Routes =
		new[]
		{
			new StorefrontRoute(
				"Glen's Bakery",
				"BakeryRoot",
				new Vector3(2.4f, 0.0f, 0.1f),
				Vector3.Right,
				new[] { "." },
				new[]
				{
					"EntrySteps/LowCollision",
					"EntrySteps/MidCollision",
					"EntrySteps/HighCollision",
				}),
			new StorefrontRoute(
				"Ashwood Grocery",
				"Environment/Presentation/Storefronts/NorthGrocery",
				new Vector3(-8.0f, 0.0f, 0.0f),
				Vector3.Left,
				new[] { "FrontDoor" },
				new[] { "Exterior/EntryRampCollision" }),
			new StorefrontRoute(
				"Greenleaf Pharmacy",
				"Environment/Presentation/Storefronts/NorthPharmacy",
				new Vector3(3.5f, 0.0f, -0.35f),
				Vector3.Right,
				new[] { "FrontDoor" },
				new[] { "Exterior/EntryRampCollision" }),
			new StorefrontRoute(
				"Willow Outfitters",
				"Environment/Presentation/Storefronts/SouthSportingGoods",
				new Vector3(-6.2f, 0.0f, -4.12f),
				Vector3.Left,
				new[] { "FrontDoor" },
				new[] { "Exterior/ThresholdCollision" }),
			new StorefrontRoute(
				"Miller Hardware",
				"Environment/Presentation/Storefronts/SouthMillerHardware",
				new Vector3(-7.0f, 0.0f, -6.65f),
				Vector3.Left,
				new[] { "FrontDoor" },
				new[] { "Exterior/EntranceRampCollision" }),
			new StorefrontRoute(
				"Silver Spoon Diner",
				"Environment/Presentation/Storefronts/SouthDiner",
				new Vector3(-4.0f, 0.0f, -0.8f),
				Vector3.Left,
				new[] { "FrontDoor", "FrontDoorRight" },
				new[] { "Exterior/EntryRampCollision" }),
			new StorefrontRoute(
				"Ashwood Police Station",
				"Environment/Presentation/Storefronts/SouthPoliceStation",
				new Vector3(-9.2f, 0.0f, 0.0f),
				Vector3.Left,
				new[]
				{
					"FrontEntrance/LeftDoor",
					"FrontEntrance/RightDoor",
				},
				new[]
				{
					"AuthoredEnvironment/Exterior/FrontThreshold/Collision",
					"AuthoredEnvironment/Exterior/AccessibleApron/Collision",
				}),
		};

	private ThirdPersonPlayer _player = null!;

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			AddChild(world);
			world.GetNode("Gameplay/Zombies").ProcessMode =
				ProcessModeEnum.Disabled;
			_player = world.GetNode<ThirdPersonPlayer>("Gameplay/Player");

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			foreach (StorefrontRoute route in Routes)
			{
				await ValidateRoute(world, route);
			}

			ReleaseMovementInput();
			GD.Print("MAIN_STREET_STOREFRONT_TRAVERSAL_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			ReleaseMovementInput();
			GD.PushError(
				$"MAIN_STREET_STOREFRONT_TRAVERSAL_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private async Task ValidateRoute(Node3D world, StorefrontRoute route)
	{
		Node3D building = world.GetNode<Node3D>(route.BuildingPath);
		foreach (string collisionPath in route.ThresholdCollisionPaths)
		{
			CollisionShape3D collision =
				building.GetNode<CollisionShape3D>(collisionPath);
			Require(collision.Shape is BoxShape3D && !collision.Disabled,
				$"{route.Name} has an active textured threshold route");
		}

		foreach (string doorPath in route.DoorPaths)
		{
			DoorController door = doorPath == "."
				? (DoorController)building
				: building.GetNode<DoorController>(doorPath);
			door.AnimationDuration = 0.01f;
			if (!door.IsOpen)
			{
				door.ToggleDoor();
			}
		}
		await ToSignal(
			GetTree().CreateTimer(0.06f),
			SceneTreeTimer.SignalName.Timeout);

		Vector3 outward =
			(building.GlobalBasis * route.LocalOutward).Normalized();
		outward.Y = 0.0f;
		outward = outward.Normalized();
		Vector3 inward = -outward;
		Vector3 doorway = building.ToGlobal(route.LocalDoorCentre);
		Vector3 start = doorway + (outward * 1.45f);
		start.Y = 1.14f;
		await ResetPlayer(start, inward);

		Input.ActionPress("move_forward");
		bool crossed = false;
		for (int frame = 0; frame < 180; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			float inwardProgress =
				(_player.GlobalPosition - doorway).Dot(inward);
			if (inwardProgress >= 1.15f)
			{
				crossed = true;
				break;
			}
		}
		Input.ActionRelease("move_forward");

		Require(crossed,
			$"holding forward walks from the sidewalk into {route.Name} " +
			$"without jumping (stopped at {_player.GlobalPosition})");
		Require(_player.IsOnFloor(),
			$"{route.Name} threshold traversal ends grounded");
	}

	private async Task ResetPlayer(Vector3 position, Vector3 inward)
	{
		ReleaseMovementInput();
		_player.Velocity = Vector3.Zero;
		_player.GlobalPosition = position;
		float cameraYaw = inward.Z > 0.5f ? Mathf.Pi : 0.0f;
		Node3D cameraRig = _player.GetNode<Node3D>("CameraRig");
		cameraRig.GlobalBasis = new Basis(Vector3.Up, cameraYaw);

		for (int frame = 0; frame < 10; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static void ReleaseMovementInput()
	{
		Input.ActionRelease("move_forward");
		Input.ActionRelease("jump");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
