#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetPhase1Validation : Node
{
	private const string ScenePath = "res://scenes/world/ashwood/main_street.tscn";
	private const string SavePath = "user://ashwood_main_street_phase1_save_v1.json";

	public override async void _Ready()
	{
		try
		{
			SaveGameManager.DeleteSaveFile(SavePath);
			Node3D world = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ValidateStreetProfile(world);
			ValidatePlayerSpawn(world);
			ValidateBakery(world);
			ValidateDaylight(world);
			ValidateGameplayComposition(world);
			ValidateNavigation(world);
			await ValidateDoor(world);
			ValidateSaveLoad(world);

			SaveGameManager.DeleteSaveFile(SavePath);
			GD.Print("MAIN_STREET_PHASE1_VALIDATION: PASS");
			QuitAfterManagedCleanup(0);
		}
		catch (Exception exception)
		{
			SaveGameManager.DeleteSaveFile(SavePath);
			GD.PushError($"MAIN_STREET_PHASE1_VALIDATION: FAIL - {exception.Message}");
			QuitAfterManagedCleanup(1);
		}
	}

	private void QuitAfterManagedCleanup(int exitCode)
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GetTree().Quit(exitCode);
	}

	private static void ValidateStreetProfile(Node3D world)
	{
		BoxMesh road = (BoxMesh)world.GetNode<MeshInstance3D>(
			"Environment/RoadSurface/Mesh").Mesh;
		Require(IsNear(road.Size.X, 220.0f), "Main Street is 220 metres long");
		Require(IsNear(road.Size.Z, 11.6f),
			"road surface includes two 3.5 m lanes and two 2.3 m parking lanes");

		Node intersections = world.GetNode("Environment/Intersections");
		Require(intersections.GetChildCount() == 2, "two intersections are present");
		Require(IsNear(Mathf.Abs(
			intersections.GetNode<Node3D>("WestIntersection").Position.X), 70.0f),
			"west intersection is correctly spaced");
		Require(IsNear(
			intersections.GetNode<Node3D>("EastIntersection").Position.X, 70.0f),
			"east intersection is correctly spaced");

		BoxMesh sidewalk = (BoxMesh)world.GetNode<MeshInstance3D>(
			"Environment/Sidewalks/NorthCentral/Mesh").Mesh;
		Require(IsNear(sidewalk.Size.Z, 3.0f), "sidewalks are 3 metres wide");
		Require(world.GetNode("Environment/Curbs").GetChildCount() == 6,
			"curbs stop cleanly at both intersections");
		Require(world.GetNode("Environment/PlaceholderStorefrontPlots")
			.GetChildCount() == 14, "fourteen varied storefront plots frame the street");
	}

	private static void ValidateBakery(Node3D world)
	{
		Node3D bakery = world.GetNode<Node3D>("BakeryRoot");
		Node3D exterior = bakery.GetNode<Node3D>("Exterior/BakeryExterior");
		Require(
			exterior.SceneFilePath ==
				"res://assets/environment/buildings/ashwood/bakery_open.glb",
			"bakery uses the final open asset");
		Require(IsNear(bakery.GlobalPosition.X, -59.5f) &&
			IsNear(bakery.GlobalPosition.Z, -12.021713f) &&
			IsNear(bakery.RotationDegrees.Y, -90.0f, 0.1f),
			"bakery occupies the documented west-end north corner lot");

		Aabb bounds = GetCombinedBounds(exterior);
		Require(IsNear(bounds.Size.X, 9.02f, 0.08f) &&
			IsNear(bounds.Size.Y, 6.54f, 0.08f) &&
			IsNear(bounds.Size.Z, 9.02f, 0.08f),
			$"rotated bakery retains believable real-world dimensions " +
			$"(actual {bounds.Size})");
		Require(IsNear(bounds.Position.Y, 0.0f, 0.03f),
			$"bakery exterior is grounded to street grade " +
			$"(actual {bounds.Position.Y})");
	}

	private static void ValidatePlayerSpawn(Node3D world)
	{
		CharacterBody3D player =
			world.GetNode<CharacterBody3D>("Gameplay/Player");
		CollisionShape3D collision =
			player.GetNode<CollisionShape3D>("CollisionShape3D");
		CapsuleShape3D capsule = (CapsuleShape3D)collision.Shape;
		BoxMesh sidewalk = (BoxMesh)world.GetNode<MeshInstance3D>(
			"Environment/Sidewalks/NorthWest/Mesh").Mesh;
		Node3D sidewalkBody = world.GetNode<Node3D>(
			"Environment/Sidewalks/NorthWest");
		float sidewalkTop = sidewalkBody.GlobalPosition.Y + (sidewalk.Size.Y * 0.5f);
		float collisionBottom =
			collision.GlobalPosition.Y - (capsule.Height * 0.5f);

		Require(IsNear(player.GlobalPosition.X, -88.0f) &&
			IsNear(player.GlobalPosition.Z, -7.3f),
			"player starts on the north sidewalk");
		Require(collisionBottom >= sidewalkTop - 0.005f,
			"player capsule begins above the sidewalk surface");
		Require(IsNear(
			player.GlobalPosition.Y - sidewalkTop,
			capsule.Height * 0.5f,
			0.01f), "player root uses the established centred-capsule floor offset");
	}

	private static void ValidateDaylight(Node3D world)
	{
		WorldTime time = world.GetNode<WorldTime>("Gameplay/WorldTime");
		Require(IsNear(time.StartingHour, 16.75f) &&
			IsNear(time.DayAmbientEnergy, 1.02f) &&
			IsNear(time.DaySkyEnergy, 0.95f) &&
			IsNear(time.DayDirectionalEnergy, 1.2f) &&
			IsNear(time.GoldenHourColorStrength, 1.0f),
			"Main Street starts with sustained golden-hour lighting");
	}

	private static async System.Threading.Tasks.Task ValidateDoor(Node3D world)
	{
		DoorController controller = world.GetNode<DoorController>("BakeryRoot");
		Node3D pivot = controller.GetNode<Node3D>("FrontDoorPivot");
		Node3D doorModel = controller.GetNode<Node3D>(
			"FrontDoorPivot/ShopFrontDoor/DoorModel");
		Require(controller.HasNode("FrontDoorPivot/ShopFrontDoor/CollisionShape3D"),
			"door collision follows the hinge pivot");
		Require(controller.HasNode("DoorInteraction") &&
			controller.GetNode("DoorInteraction") is Interactable,
			"door uses the existing interaction component");

		Aabb closedBounds = GetCombinedBounds(doorModel);
		Vector3 closedCentre = closedBounds.GetCenter();
		Require(IsNear(closedBounds.Size.X, 0.994f, 0.02f) &&
			IsNear(closedBounds.Size.Y, 1.844f, 0.02f),
			"closed shop door matches the imported doorway dimensions");
		Require(IsNear(closedCentre.X, -59.608f, 0.03f) &&
			IsNear(closedBounds.Position.Y, 0.748f, 0.03f) &&
			IsNear(closedCentre.Z, -9.622f, 0.03f),
			$"closed shop door fills the real bakery doorway " +
			$"(centre {closedCentre}, minimum Y {closedBounds.Position.Y})");
		Require(IsNear(pivot.GlobalPosition.X, closedBounds.Position.X, 0.02f),
			"door hinge is on the physical left edge");

		float closedRotation = pivot.Rotation.Y;
		controller.ToggleDoor();
		await controller.ToSignal(
			controller.GetTree().CreateTimer(0.65),
			SceneTreeTimer.SignalName.Timeout);
		Require(controller.IsOpen && !controller.IsAnimating,
			"door completes its opening state");
		Require(IsNear(
			Mathf.RadToDeg(pivot.Rotation.Y - closedRotation),
			-100.0f,
			0.5f), "door rotates around its hinge");
		Aabb openBounds = GetCombinedBounds(doorModel);
		Require(openBounds.End.Z > -8.72f,
			"open door swings outward clear of the storefront wall");
		controller.ToggleDoor();
		await controller.ToSignal(
			controller.GetTree().CreateTimer(0.65),
			SceneTreeTimer.SignalName.Timeout);
		Require(!controller.IsOpen &&
			IsNear(pivot.Rotation.Y, closedRotation, 0.01f),
			"door closes and restores collision alignment");
	}

	private static void ValidateGameplayComposition(Node3D world)
	{
		Node gameplay = world.GetNode("Gameplay");
		Require(gameplay.HasNode("Player/CameraRig/SpringArm3D/Camera3D"),
			"existing third-person camera composition is present");
		Require(gameplay.HasNode("Player/Health") &&
			gameplay.HasNode("Player/Stamina") &&
			gameplay.HasNode("Player/Needs") &&
			gameplay.HasNode("Player/Inventory") &&
			gameplay.HasNode("Player/MeleeCombat") &&
			gameplay.HasNode("Player/Interaction"),
			"player gameplay systems are reused");
		Require(gameplay.HasNode("GameplayHUD") &&
			gameplay.HasNode("SaveGameManager"),
			"HUD and save manager are reused");

		Node zombies = gameplay.GetNode("Zombies");
		int zombieCount = 0;
		for (int childIndex = 0; childIndex < zombies.GetChildCount(); childIndex++)
		{
			if (zombies.GetChild(childIndex) is PrototypeZombie)
			{
				zombieCount++;
			}
		}
		Require(zombieCount == 5, "exactly five zombies are placed");
	}

	private static void ValidateNavigation(Node3D world)
	{
		NavigationRegion3D region =
			world.GetNode<NavigationRegion3D>("NavigationRegion3D");
		NavigationMesh? mesh = region.NavigationMesh;
		Require(mesh is not null && mesh.GetPolygonCount() == 6,
			"street, intersections and bakery approach have navigation coverage");
		Require(region.GetNavigationMap().IsValid,
			"navigation region is connected to a valid map");
	}

	private static void ValidateSaveLoad(Node3D world)
	{
		SaveGameManager save = world.GetNode<SaveGameManager>(
			"Gameplay/SaveGameManager");
		save.SaveFilePath = SavePath;
		Require(save.MinimumContainerCount == 4 && save.MinimumZombieCount == 5 &&
			save.PersistenceRootPath.ToString() == "../..",
			"version-1 save validation covers the production persistence root");
		Require(save.SaveGame(), "Main Street state saves through the existing manager");
		Require(save.LoadGame(), "Main Street state loads through the existing manager");
	}

	private static Aabb GetCombinedBounds(Node root)
	{
		bool found = false;
		Aabb combined = default;
		AccumulateMeshBounds(root, ref found, ref combined);
		Require(found, "expected at least one bakery mesh");
		return combined;
	}

	private static void AccumulateMeshBounds(
		Node root,
		ref bool found,
		ref Aabb combined)
	{
		for (int childIndex = 0; childIndex < root.GetChildCount(); childIndex++)
		{
			Node child = root.GetChild(childIndex);
			if (child is MeshInstance3D mesh)
			{
				Aabb bounds = mesh.GlobalTransform * mesh.GetAabb();
				combined = found ? combined.Merge(bounds) : bounds;
				found = true;
			}

			AccumulateMeshBounds(child, ref found, ref combined);
		}
	}

	private static bool IsNear(float value, float expected, float tolerance = 0.01f)
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
