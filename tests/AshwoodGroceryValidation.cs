#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodGroceryValidation : Node
{
	private const string GroceryScenePath =
		"res://assets/environment/buildings/AshwoodGrocery/ashwood_grocery.tscn";

	public override async void _Ready()
	{
		try
		{
			Node3D grocery = GD.Load<PackedScene>(GroceryScenePath)
				.Instantiate<Node3D>();
			AddChild(grocery);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ValidateAssembly(grocery);
			ValidateExterior(grocery);
			ValidateArchitecture(grocery);
			ValidateDepartments(grocery);
			ValidateProductionFixtures(grocery);
			ValidateContentDensity(grocery);
			ValidatePbrSurfaces(grocery);
			ValidateClearance(grocery);
			ValidateCollisionAndPerformance(grocery);
			await ValidateEntrance(grocery);

			GD.Print("ASHWOOD_GROCERY_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"ASHWOOD_GROCERY_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateAssembly(Node3D grocery)
	{
		Require(grocery.Name == "AshwoodGrocery",
			"production wrapper carries the Ashwood Grocery identity");
		Require(grocery.GetNode("Exterior") is StaticBody3D &&
			grocery.GetNode("Interior") is StaticBody3D &&
			grocery.GetNode("FrontDoor") is DoorController,
			"standalone exterior, interior, and interactive entrance are assembled");
		Require(grocery.GetNode<Node3D>("Exterior").SceneFilePath.EndsWith(
				"/AshwoodGrocery/exterior.tscn", StringComparison.Ordinal) &&
			grocery.GetNode<Node3D>("Interior").SceneFilePath.EndsWith(
				"/AshwoodGrocery/interior.tscn", StringComparison.Ordinal),
			"production scenes remain independently authorable");
	}

	private static void ValidateExterior(Node3D grocery)
	{
		StaticBody3D exterior = grocery.GetNode<StaticBody3D>("Exterior");
		Aabb bounds = GetCombinedBounds(exterior.GetNode("Shell"));
		Require(bounds.Size.X >= 16.0f &&
			bounds.Size.Z >= 21.0f &&
			bounds.Size.Y >= 5.7f,
			"historic shell supplies the requested spacious sixteen by twenty-one metre footprint");
		Require(exterior.HasNode("Storefront/NorthWindow") &&
			exterior.HasNode("Storefront/SouthWindow") &&
			exterior.GetNode("Storefront/Frames").GetChildCount() >= 14,
			"street facade has broad glazed display bays with detailed framing");
		Require(exterior.GetNode<Label3D>("Identity/Name").Text ==
			"ASHWOOD GROCERY",
			"facade carries the final business identity");
		Require(exterior.GetNode("Awning").GetChildCount() >= 5,
			"period storefront awning has valance and structural ribs");
		Require(exterior.HasNode("LoadingDoor") &&
			exterior.HasNode("LoadingLabel") &&
			exterior.HasNode("LoadingLamp"),
			"rear elevation contains a dressed receiving entrance");
	}

	private static void ValidateArchitecture(Node3D grocery)
	{
		StaticBody3D interior = grocery.GetNode<StaticBody3D>("Interior");
		BoxShape3D floor = (BoxShape3D)interior
			.GetNode<CollisionShape3D>("FloorCollision").Shape;
		Require(floor.Size.X >= 15.5f && floor.Size.Z >= 20.5f,
			"interior retains the full spacious retail footprint");
		Require(interior.HasNode("Architecture/StorageWestWall") &&
			interior.HasNode("Architecture/StorageFrontWest") &&
			interior.HasNode("Architecture/StorageFrontEast") &&
			interior.HasNode("Architecture/StaffWestWall") &&
			interior.HasNode("Architecture/BathroomDividerWest") &&
			interior.HasNode("Architecture/BathroomDividerEast"),
			"receiving, office, and restroom use complete full-height partitions");
		Require(interior.HasNode("Architecture/BathroomFloor") &&
			interior.HasNode("Architecture/BathroomRearTile") &&
			interior.HasNode("Architecture/BathroomSouthTile"),
			"restroom receives continuous PBR tile surfaces");
		foreach (string path in new[]
		{
			"Architecture/StorageDoor",
			"Architecture/OfficeDoor",
			"Architecture/RestroomDoor",
		})
		{
			Require(interior.GetNode<Node3D>(path).SceneFilePath.EndsWith(
					"/white_door_1.glb", StringComparison.Ordinal),
				$"{path} uses an imported textured full-size door");
		}
		Require(interior.HasNode("Restroom/Toilet") &&
			interior.HasNode("Restroom/Sink") &&
			interior.HasNode("Architecture/BathroomMirror"),
			"enclosed restroom has sanitary fixtures and mirror");
	}

	private static void ValidateDepartments(Node3D grocery)
	{
		StaticBody3D interior = grocery.GetNode<StaticBody3D>("Interior");
		string[] requiredZones =
		{
			"SalesFloorFixtures/Aisles/Aisle01",
			"SalesFloorFixtures/Aisles/Aisle02",
			"SalesFloorFixtures/Aisles/Aisle03",
			"SalesFloorFixtures/Checkouts",
			"ProduceDepartment",
			"ColdStorageWall",
			"HouseholdZone",
			"Storage",
			"StaffOffice",
			"Restroom",
			"WindowDisplays",
			"Abandonment",
			"Signage",
			"Lighting",
		};
		Require(requiredZones.All(path => interior.HasNode(path)),
			"sales, produce, cold, household, receiving, staff, and atmospheric zones are complete");
		Require(interior.GetNode("SalesFloorFixtures/Aisles").GetChildCount() == 3 &&
			interior.GetNode("SalesFloorFixtures/Checkouts")
				.GetChildren().OfType<Node3D>()
				.Count(node => node.Name.ToString().StartsWith(
					"Lane", StringComparison.Ordinal)) == 2,
			"store has three long aisles and two checkout lanes");
		Require(interior.GetNode("ProduceDepartment/ProduceStock")
				.GetChildCount() >= 25 &&
			interior.GetNode("ColdStorageWall/BeverageAndColdStock")
				.GetChildCount() >= 12 &&
			interior.GetNode("HouseholdZone/HouseholdShelfStock")
				.GetChildCount() >= 8,
			"major departments are visibly stocked rather than represented by empty fixtures");
		Require(interior.GetNode("Storage").GetChildCount() >= 12 &&
			interior.GetNode("StaffOffice").GetChildCount() >= 8 &&
			interior.GetNode("Abandonment").GetChildCount() >= 7,
			"back rooms and hand-placed abandonment dressing are fully developed");
	}

	private static void ValidateProductionFixtures(Node3D grocery)
	{
		StaticBody3D interior = grocery.GetNode<StaticBody3D>("Interior");
		string[] fixturePaths =
		{
			"SalesFloorFixtures/Aisles/Aisle01/NorthSegment",
			"SalesFloorFixtures/Aisles/Aisle01/SouthSegment",
			"SalesFloorFixtures/Aisles/Aisle02/NorthSegment",
			"SalesFloorFixtures/Aisles/Aisle02/SouthSegment",
			"SalesFloorFixtures/Aisles/Aisle03/NorthSegment",
			"SalesFloorFixtures/Aisles/Aisle03/SouthSegment",
			"SalesFloorFixtures/Checkouts/Lane01",
			"SalesFloorFixtures/Checkouts/Lane02",
			"ProduceDepartment/NorthIsland",
			"ProduceDepartment/SouthIsland",
			"ProduceDepartment/WallBins",
			"ColdStorageWall/CaseNorth",
			"ColdStorageWall/CaseCentre",
			"ColdStorageWall/CaseSouth",
		};
		foreach (string path in fixturePaths)
		{
			Node3D fixture = interior.GetNode<Node3D>(path);
			Require(fixture.SceneFilePath.Contains(
					"/AshwoodGrocery/fixtures/grocery_",
					StringComparison.Ordinal),
				$"{path} uses the project-owned detailed PBR fixture pack");
			Require(fixture.FindChildren(
					"*", "MeshInstance3D", true, false).Count >= 3,
				$"{path} contains detailed multi-part geometry");
		}
	}

	private static void ValidateContentDensity(Node3D grocery)
	{
		StaticBody3D interior = grocery.GetNode<StaticBody3D>("Interior");
		Node[] downloadedExamples =
		{
			interior.GetNode("ProduceDepartment/ProduceStock/NorthBananasA"),
			interior.GetNode("ProduceDepartment/ProduceStock/NorthApples01"),
			interior.GetNode("ProduceDepartment/ProduceStock/NorthOnions01"),
			interior.GetNode("ProduceDepartment/ProduceStock/NorthPotatoes01"),
			interior.GetNode("WindowDisplays/NorthBasket"),
			interior.GetNode("ColdStorageWall/BeverageAndColdStock/WineNorthLow"),
			interior.GetNode("AisleStock/CannedAndDryGoods/Cans01"),
		};
		Require(downloadedExamples.All(node =>
				node.SceneFilePath.Contains(
					"/ashwood_grocery/poly_haven/",
					StringComparison.Ordinal)),
			"hero grocery merchandise uses the local licensed CC0 asset pack");

		int externalModelRoots = interior.FindChildren(
				"*", "Node3D", true, false)
			.OfType<Node3D>()
			.Count(node =>
				node.SceneFilePath.EndsWith(".gltf", StringComparison.Ordinal) ||
				node.SceneFilePath.EndsWith(".glb", StringComparison.Ordinal) ||
				node.SceneFilePath.EndsWith(".tscn", StringComparison.Ordinal));
		Require(externalModelRoots >= 115,
			"content density comes from instanced detailed assets instead of primitive stock proxies");
		Require(interior.FindChildren(
				"*", "MeshInstance3D", true, false).Count >= 170,
			"interior reaches a believable production prop and fixture density");
	}

	private static void ValidatePbrSurfaces(Node3D grocery)
	{
		StaticBody3D exterior = grocery.GetNode<StaticBody3D>("Exterior");
		StaticBody3D interior = grocery.GetNode<StaticBody3D>("Interior");
		StandardMaterial3D brick = GetMaterial(
			exterior.GetNode<MeshInstance3D>("Shell/NorthWall"));
		StandardMaterial3D floor = GetMaterial(
			interior.GetNode<MeshInstance3D>("Architecture/SalesFloor"));
		StandardMaterial3D wall = GetMaterial(
			interior.GetNode<MeshInstance3D>("Architecture/NorthWallLiner"));
		StandardMaterial3D ceiling = GetMaterial(
			interior.GetNode<MeshInstance3D>("Architecture/Ceiling"));
		StandardMaterial3D tile = GetMaterial(
			interior.GetNode<MeshInstance3D>("Architecture/BathroomFloor"));

		foreach ((StandardMaterial3D material, string label) in new[]
		{
			(brick, "exterior brick"),
			(floor, "linoleum sales floor"),
			(wall, "interior wall"),
			(ceiling, "interior ceiling"),
			(tile, "restroom tile"),
		})
		{
			Require(material.AlbedoTexture is not null &&
				material.NormalEnabled &&
				material.NormalTexture is not null,
				$"{label} uses authored albedo and normal PBR maps");
		}
	}

	private static void ValidateClearance(Node3D grocery)
	{
		StaticBody3D interior = grocery.GetNode<StaticBody3D>("Interior");
		Node3D[] aisles =
		{
			interior.GetNode<Node3D>("SalesFloorFixtures/Aisles/Aisle01/NorthSegment"),
			interior.GetNode<Node3D>("SalesFloorFixtures/Aisles/Aisle02/NorthSegment"),
			interior.GetNode<Node3D>("SalesFloorFixtures/Aisles/Aisle03/NorthSegment"),
		};
		float aisleCollisionWidth = ((BoxShape3D)interior
			.GetNode<CollisionShape3D>("Aisle01Collision").Shape).Size.X;
		Require(aisles.Zip(aisles.Skip(1), (first, second) =>
				Mathf.Abs(second.Position.X - first.Position.X) -
				aisleCollisionWidth)
			.All(clearance => clearance >= 1.85f),
			"three long aisles retain nearly two-metre clear walking lanes");

		float aisleHalfLength = ((BoxShape3D)interior
			.GetNode<CollisionShape3D>("Aisle01Collision").Shape).Size.Z * 0.5f;
		Node3D northProduce = interior.GetNode<Node3D>(
			"ProduceDepartment/NorthIsland");
		Node3D southProduce = interior.GetNode<Node3D>(
			"ProduceDepartment/SouthIsland");
		float produceHalfDepth = ((BoxShape3D)interior
			.GetNode<CollisionShape3D>("NorthProduceCollision").Shape).Size.Z * 0.5f;
		float northLoop = (-aisleHalfLength) -
			(northProduce.Position.Z + produceHalfDepth);
		float southLoop = (southProduce.Position.Z - produceHalfDepth) -
			aisleHalfLength;
		Require(northLoop >= 1.30f && southLoop >= 1.30f,
			"aisle ends retain walkable loops around both produce islands");

		Node3D laneNorth = interior.GetNode<Node3D>(
			"SalesFloorFixtures/Checkouts/Lane01");
		Node3D laneSouth = interior.GetNode<Node3D>(
			"SalesFloorFixtures/Checkouts/Lane02");
		float checkoutHalfWidth = ((BoxShape3D)interior
			.GetNode<CollisionShape3D>("Lane01Collision").Shape).Size.Z * 0.5f;
		float entryClearance = (laneSouth.Position.Z - checkoutHalfWidth) -
			(laneNorth.Position.Z + checkoutHalfWidth);
		Require(entryClearance >= 3.35f,
			"street entrance opens into a broad central checkout passage");

		float storageOpening = GetOpeningWidth(
			interior.GetNode<MeshInstance3D>("Architecture/StorageFrontWest"),
			interior.GetNode<MeshInstance3D>("Architecture/StorageFrontEast"));
		float officeOpening = GetOpeningWidth(
			interior.GetNode<MeshInstance3D>("Architecture/OfficeFrontWest"),
			interior.GetNode<MeshInstance3D>("Architecture/OfficeFrontEast"));
		float restroomOpening = GetOpeningWidth(
			interior.GetNode<MeshInstance3D>("Architecture/BathroomDividerWest"),
			interior.GetNode<MeshInstance3D>("Architecture/BathroomDividerEast"));
		Require(storageOpening >= 0.95f &&
			officeOpening >= 0.95f &&
			restroomOpening >= 0.95f,
			"storage, office, and restroom doorways preserve comfortable traversal");
	}

	private static void ValidateCollisionAndPerformance(Node3D grocery)
	{
		StaticBody3D interior = grocery.GetNode<StaticBody3D>("Interior");
		CollisionShape3D[] collisions = interior.FindChildren(
				"*", "CollisionShape3D", true, false)
			.OfType<CollisionShape3D>()
			.ToArray();
		Require(collisions.Length >= 21 && collisions.Length <= 24 &&
			collisions.All(collision =>
				!collision.Disabled && collision.Shape is BoxShape3D),
			"architecture and major fixtures use restrained active box collision");
		foreach (string path in new[]
		{
			"AisleStock",
			"ProduceDepartment/ProduceStock",
			"Abandonment",
			"Signage",
			"Lighting",
		})
		{
			Require(interior.GetNode(path).FindChildren(
					"*", "CollisionShape3D", true, false).Count == 0,
				$"{path} avoids per-prop collision overhead");
		}

		OmniLight3D[] lights = interior.FindChildren(
				"*", "OmniLight3D", true, false)
			.OfType<OmniLight3D>()
			.ToArray();
		Require(lights.Length == 6 &&
			lights.All(light => !light.ShadowEnabled),
			"daytime store uses six restrained shadowless fill lights");
		Require(interior.GetNode("Lighting").GetChildren()
				.OfType<Node3D>()
				.Count(node => node.SceneFilePath.EndsWith(
					".gltf", StringComparison.Ordinal)) == 12,
			"visible fluorescent fixtures cover every retail and staff zone");

		int totalNodes = CountDescendants(interior);
		int meshCount = interior.FindChildren(
			"*", "MeshInstance3D", true, false).Count;
		Require(totalNodes <= 4000 && meshCount <= 1800,
			"instancing and simplified collision keep the dense interior within performance budgets");
	}

	private async System.Threading.Tasks.Task ValidateEntrance(Node3D grocery)
	{
		DoorController door = grocery.GetNode<DoorController>("FrontDoor");
		Require(IsNear(door.Position.X, -8.13f) &&
			IsNear(door.Position.Z, -0.50f),
			"entrance sits on the local street-facing facade");
		Require(door.HasNode("Hinge/DoorBody/DoorModel") &&
			door.HasNode("Hinge/DoorBody/CollisionShape3D") &&
			door.HasNode("Interactable"),
			"entrance uses imported geometry, moving collision, and interaction");
		float closedRotation = door.GetNode<Node3D>("Hinge").Rotation.Y;
		door.AnimationDuration = 0.01f;
		door.ToggleDoor();
		await ToSignal(GetTree().CreateTimer(0.05),
			SceneTreeTimer.SignalName.Timeout);
		Require(door.IsOpen && !door.IsAnimating,
			"front door completes its opening state");
		Require(IsNear(
				Mathf.RadToDeg(
					door.GetNode<Node3D>("Hinge").Rotation.Y - closedRotation),
				-105.0f,
				1.0f),
			"front door swings clear of the entry passage");
	}

	private static float GetOpeningWidth(
		MeshInstance3D left,
		MeshInstance3D right)
	{
		Aabb leftBounds = left.Transform * left.GetAabb();
		Aabb rightBounds = right.Transform * right.GetAabb();
		return rightBounds.Position.X -
			(leftBounds.Position.X + leftBounds.Size.X);
	}

	private static StandardMaterial3D GetMaterial(MeshInstance3D mesh)
	{
		Material? material = mesh.MaterialOverride;
		if (material is null && mesh.Mesh is PrimitiveMesh primitive)
			material = primitive.Material;
		if (material is null && mesh.Mesh.GetSurfaceCount() > 0)
			material = mesh.Mesh.SurfaceGetMaterial(0);
		Require(material is StandardMaterial3D,
			$"{mesh.GetPath()} resolves to a StandardMaterial3D");
		return (StandardMaterial3D)material!;
	}

	private static Aabb GetCombinedBounds(Node root)
	{
		bool found = false;
		Aabb bounds = default;
		foreach (MeshInstance3D mesh in root.FindChildren(
			"*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
		{
			Aabb meshBounds = mesh.GlobalTransform * mesh.GetAabb();
			bounds = found ? bounds.Merge(meshBounds) : meshBounds;
			found = true;
		}
		Require(found, $"{root.GetPath()} contains visible mesh geometry");
		return bounds;
	}

	private static int CountDescendants(Node node)
	{
		int count = 0;
		foreach (Node child in node.GetChildren())
		{
			count += 1 + CountDescendants(child);
		}
		return count;
	}

	private static bool IsNear(
		float value,
		float expected,
		float tolerance = 0.025f)
	{
		return Mathf.Abs(value - expected) <= tolerance;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
			throw new InvalidOperationException(message);
	}
}
