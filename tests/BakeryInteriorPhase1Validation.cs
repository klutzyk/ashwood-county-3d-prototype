#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class BakeryInteriorPhase1Validation : Node
{
	private const string ScenePath = "res://scenes/world/ashwood/main_street.tscn";

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Node3D bakery = world.GetNode<Node3D>("BakeryRoot");
			ValidateModularShell(bakery);
			ValidateRoomClearances(bakery);
			ValidateCollision(bakery);
			await ValidateNavigation(bakery);
			ValidateTemporaryLighting(bakery);

			GD.Print("BAKERY_INTERIOR_PHASE1_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"BAKERY_INTERIOR_PHASE1_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateModularShell(Node3D bakery)
	{
		foreach (string path in new[]
		{
			"Interior/Floor",
			"Interior/Ceiling",
			"Interior/Walls",
			"Interior/Counter",
			"Interior/KitchenPartition",
			"Interior/StoragePartition",
			"Interior/UtilityPartition",
		})
		{
			Require(bakery.HasNode(path), $"modular node {path} is present");
		}

		BoxMesh floor = GetBoxMesh(bakery, "Interior/Floor/Mesh");
		BoxMesh ceiling = GetBoxMesh(bakery, "Interior/Ceiling/Mesh");
		Require(IsNear(floor.Size.X, 4.5f) && IsNear(floor.Size.Z, 9.4f),
			"finished floor matches the measured 4.5 m by 9.4 m footprint");
		Require(IsNear(
			bakery.GetNode<Node3D>("Interior/Floor").Position.Y + floor.Size.Y * 0.5f,
			0.742f), "finished floor aligns with the existing front threshold");
		Require(IsNear(
			bakery.GetNode<Node3D>("Interior/Ceiling").Position.Y -
				ceiling.Size.Y * 0.5f,
			3.542f), "ceiling provides 2.8 m of clear interior height");
	}

	private static void ValidateRoomClearances(Node3D bakery)
	{
		Node3D counter = bakery.GetNode<Node3D>("Interior/Counter");
		BoxMesh counterMesh = GetBoxMesh(bakery, "Interior/Counter/Mesh");
		float clearEntryDepth = 2.25f -
			(counter.Position.X + counterMesh.Size.X * 0.5f);
		float staffPassageWidth =
			(counter.Position.Z - counterMesh.Size.Z * 0.5f) - (-4.7f);

		Require(clearEntryDepth >= 1.5f && clearEntryDepth <= 2.0f,
			"retail entrance retains 1.5 to 2.0 m of clear depth");
		Require(staffPassageWidth >= 1.2f,
			"counter passage clears both player and zombie capsules");

		Node3D storagePartition =
			bakery.GetNode<Node3D>("Interior/StoragePartition");
		BoxMesh storageMesh =
			GetBoxMesh(bakery, "Interior/StoragePartition/Mesh");
		float storageOpening =
			-0.15f - (storagePartition.Position.X + storageMesh.Size.X * 0.5f);
		Require(storageOpening >= 0.9f,
			"storage connects directly to the kitchen with character clearance");
	}

	private static void ValidateCollision(Node3D bakery)
	{
		foreach (StaticBody3D body in bakery.GetNode("Interior")
			.FindChildren("*", "StaticBody3D", true, false)
			.OfType<StaticBody3D>())
		{
			Require(body.CollisionLayer == 1 && body.CollisionMask == 1,
				$"{body.Name} uses the established world collision layer");
			Require(body.FindChildren("*", "CollisionShape3D", true, false)
				.OfType<CollisionShape3D>().Any(shape => !shape.Disabled),
				$"{body.Name} has active collision");
		}

		Node shell = bakery.GetNode("Collision/Shell");
		Require(shell.HasNode("LeftWall") &&
			!bakery.HasNode("RearServiceDoor") &&
			!bakery.HasNode("Interior/ServiceExitTransition"),
			"side and rear shell remain solid while the back door is deferred");
	}

	private static async Task ValidateNavigation(Node3D bakery)
	{
		NavigationRegion3D region =
			bakery.GetNode<NavigationRegion3D>("InteriorNavigationRegion");
		NavigationMesh mesh = region.NavigationMesh ??
			throw new InvalidOperationException("bakery navigation mesh is assigned");
		Require(mesh.GetPolygonCount() == 16,
			"navigation covers retail, staff passage, kitchen, storage and front entry");
		Require(IsNear(mesh.AgentRadius, 0.45f) &&
			IsNear(mesh.AgentHeight, 1.8f) &&
			IsNear(mesh.AgentMaxClimb, 0.3f),
			"bakery navigation matches the existing zombie agent");

		Rid map = region.GetNavigationMap();
		Require(map.IsValid, "bakery navigation region is attached to a valid map");
		uint initialIteration = NavigationServer3D.MapGetIterationId(map);
		for (int frame = 0; frame < 10; frame++)
		{
			await bakery.ToSignal(
				bakery.GetTree(),
				SceneTree.SignalName.PhysicsFrame);
		}
		Require(NavigationServer3D.MapGetIterationId(map) > initialIteration,
			"navigation map synchronized after the bakery region entered the tree");

		Vector3 retail = bakery.ToGlobal(new Vector3(1.5f, 0.762f, 0.1f));
		Vector3 frontOutside = bakery.ToGlobal(new Vector3(4.0f, 0.02f, 0.1f));

		Vector3[] frontPath = NavigationServer3D.MapGetPath(
			map, retail, frontOutside, true);
		Require(frontPath.Length >= 2,
			"navigation connects the retail floor to Main Street through the front door");
	}

	private static void ValidateTemporaryLighting(Node3D bakery)
	{
		Node lights = bakery.GetNode("Interior/TemporaryLighting");
		OmniLight3D[] fixtures = lights.GetChildren().OfType<OmniLight3D>().ToArray();
		Require(fixtures.Length == 2 &&
			fixtures.All(light => light.LightEnergy >= 0.3f && light.OmniRange >= 5.5f),
			"two inspection-only lights make both bakery zones readable");
	}

	private static BoxMesh GetBoxMesh(Node root, string path)
	{
		return (BoxMesh)root.GetNode<MeshInstance3D>(path).Mesh;
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
