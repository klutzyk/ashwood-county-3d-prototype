#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class GreenleafPharmacyInteriorValidation : Node
{
	private const string ScenePath =
		"res://assets/environment/buildings/Pharmacy/pharmacy.tscn";
	private const float PlayerRadius = 0.45f;
	private const float PlayerHalfHeight = 0.9f;

	public override async void _Ready()
	{
		try
		{
			Node3D pharmacy = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
			AddChild(pharmacy);

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			StaticBody3D exterior = pharmacy.GetNode<StaticBody3D>("Exterior");
			StaticBody3D interior = pharmacy.GetNode<StaticBody3D>("Interior");
			DoorController frontDoor = pharmacy.GetNode<DoorController>("FrontDoor");

			ValidateAssembly(exterior, interior, frontDoor);
			ValidateConceptZones(interior);
			ValidateImportedFocalAssets(interior);
			ValidateMedicineCabinet(interior);
			ValidateMajorOnlyCollision(interior);
			ValidatePerformanceIntent(interior);
			await ValidateDoorAndClearance(pharmacy, frontDoor);

			GD.Print("GREENLEAF_PHARMACY_INTERIOR_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"GREENLEAF_PHARMACY_INTERIOR_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateAssembly(
		StaticBody3D exterior,
		StaticBody3D interior,
		DoorController frontDoor)
	{
		Require(exterior.SceneFilePath.EndsWith(
				"/Pharmacy/exterior.tscn",
				StringComparison.Ordinal),
			"assembled root uses the dedicated production exterior");
		Require(interior.SceneFilePath.EndsWith(
				"/Pharmacy/interior.tscn",
				StringComparison.Ordinal),
			"assembled root uses the dedicated production interior");
		Require(frontDoor.SceneFilePath.EndsWith(
				"/Pharmacy/front_door.tscn",
				StringComparison.Ordinal),
			"assembled root uses the pharmacy-specific functional front door");
		Require(exterior.HasNode("StorefrontGlass/NorthWindow") &&
			exterior.HasNode("StorefrontGlass/SouthWindow") &&
			exterior.GetNode<Label3D>("Name").Text == "GREENLEAF PHARMACY",
			"exterior has the Greenleaf identity and two dressed shop windows");
		Require(exterior.CollisionLayer == 1 && interior.CollisionLayer == 1,
			"assembled shell and interior use the environment collision layer");
	}

	private static void ValidateConceptZones(StaticBody3D interior)
	{
		foreach (string path in new[]
		{
			"RetailFloor",
			"RetailFloor/NorthAisle",
			"RetailFloor/SouthAisle",
			"RetailFloor/Checkout",
			"RetailFloor/WaitingArea",
			"PrescriptionCounter",
			"RxWorkspace",
			"Storage",
			"StaffOffice",
			"Restroom",
			"Abandonment",
			"CeilingFixtures",
		})
		{
			Require(interior.HasNode(path),
				$"{path} preserves a required concept-art zone");
		}

		Require(interior.GetNode("RetailFloor/NorthAisle").GetChildCount() == 3 &&
			interior.GetNode("RetailFloor/SouthAisle").GetChildCount() == 3,
			"sales floor has two complete, deliberately spaced shelf aisles");
		Require(interior.GetNode("RxWorkspace").GetChildCount() >= 12,
			"prescription workspace includes preparation and stocked back-counter detail");
		Require(interior.GetNode("StaffOffice").GetChildCount() >= 6 &&
			interior.GetNode("Storage").GetChildCount() >= 6,
			"staff office and stock room are distinct, furnished destinations");
		Require(interior.HasNode("Restroom/Toilet") &&
			interior.HasNode("Restroom/Sink"),
			"staff bathroom contains the expected sanitary fixtures");
	}

	private static void ValidateImportedFocalAssets(StaticBody3D interior)
	{
		foreach (string path in new[]
		{
			"Architecture/StorageDoor",
			"RetailFloor/NorthAisle/ShelfFront",
			"RetailFloor/SouthAisle/ShelfFront",
			"RetailFloor/Checkout/Register",
			"RetailFloor/WaitingArea/Wheelchair",
			"RetailFloor/WaitingArea/Crutches",
			"RxWorkspace/ChemistrySet",
			"StaffOffice/Desk",
			"StaffOffice/FileCabinet",
			"Storage/ShelfA",
			"Restroom/Toilet",
			"Restroom/Sink",
			"CeilingFixtures/RetailNorthFront",
		})
		{
			Node focalAsset = interior.GetNode(path);
			Require(focalAsset.SceneFilePath.StartsWith(
					"res://assets/third_party/",
					StringComparison.Ordinal),
				$"{path} is a curated external asset rather than placeholder geometry");
		}

		int importedAssetRoots = interior
			.FindChildren("*", string.Empty, true, false)
			.Count(node => node.SceneFilePath.StartsWith(
				"res://assets/third_party/",
				StringComparison.Ordinal));
		Require(importedAssetRoots >= 60 && importedAssetRoots <= 120,
			"curated imported prop density is substantial but bounded");
	}

	private static void ValidateMedicineCabinet(StaticBody3D interior)
	{
		const string cabinetPath = "MedicineCabinet/SearchableContainer";
		SearchableContainer cabinet =
			interior.GetNode<SearchableContainer>(cabinetPath);
		Require(cabinet.DisplayName == "Pharmacy Medicine Cabinet",
			"preserved medicine cabinet keeps its established interaction identity");
		Require(cabinet.LootTable?.ResourcePath.EndsWith(
				"/loot_tables/medicine_cabinet.tres",
				StringComparison.Ordinal) == true,
			"preserved medicine cabinet keeps the medical loot table");
		Require(cabinet.HasNode("Interactable") && cabinet.HasNode("Inventory"),
			"MedicineCabinet/SearchableContainer retains interaction and inventory state");
	}

	private static void ValidateMajorOnlyCollision(StaticBody3D interior)
	{
		CollisionShape3D[] authoredShapes = interior.GetChildren()
			.OfType<CollisionShape3D>()
			.ToArray();
		Require(interior.CollisionLayer == 1 && interior.CollisionMask == 1,
			"interior collision uses the established world layer and mask");
		Require(authoredShapes.Length >= 18 && authoredShapes.Length <= 24,
			"collision remains limited to architecture and substantial furniture");
		Require(authoredShapes.All(shape =>
				!shape.Disabled && shape.Shape is BoxShape3D),
			"authored collision uses active simplified boxes only");

		foreach (string path in new[]
		{
			"NorthShelfCollision",
			"SouthShelfCollision",
			"CheckoutCollision",
			"CounterCollision",
			"OfficeDeskCollision",
			"ShelfCollision",
			"BathroomFixtureCollision",
		})
		{
			Require(interior.HasNode(path),
				$"{path} covers a substantial traversal obstacle");
		}

		foreach (string path in new[]
		{
			"RetailFloor/NorthStock",
			"RetailFloor/SouthStock",
			"RxWorkspace",
			"Abandonment",
			"CeilingFixtures",
		})
		{
			Require(interior.GetNode(path)
					.FindChildren("*", "CollisionShape3D", true, false).Count == 0,
				$"{path} small detail does not carry wasteful per-prop collision");
		}
	}

	private static void ValidatePerformanceIntent(StaticBody3D interior)
	{
		Node[] descendants = interior.FindChildren(
				"*", string.Empty, true, false)
			.ToArray();
		int geometryCount = descendants.Count(node => node is GeometryInstance3D);
		Require(geometryCount >= 120 && geometryCount <= 600,
			"pharmacy detail density remains bounded for the Compatibility renderer");
		Require(descendants.Length <= 1400,
			"production interior keeps a bounded runtime node count");

		OmniLight3D[] lights = descendants.OfType<OmniLight3D>().ToArray();
		Require(lights.Length == 5 &&
			lights.All(light => !light.ShadowEnabled),
			"five lightweight shadowless fills support the daytime interior");
		Require(lights.All(light =>
				light.LightEnergy >= 0.15f && light.LightEnergy <= 0.7f),
			"interior light energy remains restrained");
	}

	private async System.Threading.Tasks.Task ValidateDoorAndClearance(
		Node3D pharmacy,
		DoorController frontDoor)
	{
		Node3D hinge = frontDoor.GetNode<Node3D>("Hinge");
		float closedAngle = hinge.Rotation.Y;
		Require(frontDoor.HasNode("Hinge/DoorBody/DoorModel") &&
			frontDoor.GetNode("Hinge/DoorBody/DoorModel")
				.SceneFilePath.StartsWith(
					"res://assets/third_party/",
					StringComparison.Ordinal),
			"functional front door uses the imported shop-door model");
		Require(frontDoor.GetNode<CollisionShape3D>(
				"Hinge/DoorBody/CollisionShape3D").Shape is BoxShape3D,
			"functional front door has a simplified moving collision box");

		frontDoor.AnimationDuration = 0.01f;
		frontDoor.ToggleDoor();
		for (int frame = 0; frame < 4; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		float expectedOpenAngle =
			closedAngle + Mathf.DegToRad(frontDoor.OpenAngleDegrees);
		Require(frontDoor.IsOpen && !frontDoor.IsAnimating &&
			Mathf.Abs(Mathf.AngleDifference(
				hinge.Rotation.Y, expectedOpenAngle)) <= 0.02f,
			"front door interaction opens the visible model and collision together");

		CollisionShape3D[] authoredBoxes = pharmacy
			.FindChildren("*", "CollisionShape3D", true, false)
			.OfType<CollisionShape3D>()
			.Where(shape => !shape.Disabled && shape.Shape is BoxShape3D)
			.ToArray();

		ValidateClearRoute(pharmacy, authoredBoxes, "front entrance", new[]
		{
			new Vector3(4.35f, 0.9f, 0.0f),
			new Vector3(3.9f, 0.9f, 0.0f),
			new Vector3(3.5f, 0.9f, 0.0f),
			new Vector3(3.15f, 0.9f, 0.0f),
			new Vector3(2.75f, 0.9f, 0.0f),
		});
		ValidateClearRoute(pharmacy, authoredBoxes, "central sales aisle", new[]
		{
			new Vector3(2.4f, 0.9f, 0.0f),
			new Vector3(1.8f, 0.9f, 0.0f),
			new Vector3(1.2f, 0.9f, 0.0f),
			new Vector3(0.6f, 0.9f, 0.0f),
			new Vector3(0.25f, 0.9f, 0.0f),
		});
		ValidateClearRoute(pharmacy, authoredBoxes, "storage-room doorway", new[]
		{
			new Vector3(-2.05f, 0.9f, -2.2f),
			new Vector3(-2.05f, 0.9f, -2.4f),
			new Vector3(-2.05f, 0.9f, -2.55f),
			new Vector3(-2.05f, 0.9f, -2.72f),
			new Vector3(-2.05f, 0.9f, -2.88f),
		});
		ValidateClearRoute(pharmacy, authoredBoxes, "restroom doorway", new[]
		{
			new Vector3(-1.94f, 0.9f, 2.75f),
			new Vector3(-1.94f, 0.9f, 2.9f),
			new Vector3(-1.94f, 0.9f, 3.05f),
			new Vector3(-1.94f, 0.9f, 3.18f),
		});
	}

	private static void ValidateClearRoute(
		Node3D pharmacy,
		IEnumerable<CollisionShape3D> authoredBoxes,
		string routeName,
		IEnumerable<Vector3> points)
	{
		foreach (Vector3 point in points)
		{
			Require(!OverlapsAuthoredBox(pharmacy, point, authoredBoxes),
				$"{routeName} clears the 0.45 m player radius at {point}");
		}
	}

	private static bool OverlapsAuthoredBox(
		Node3D pharmacy,
		Vector3 pharmacyLocalPoint,
		IEnumerable<CollisionShape3D> authoredBoxes)
	{
		Vector3 globalPoint = pharmacy.ToGlobal(pharmacyLocalPoint);
		foreach (CollisionShape3D collisionShape in authoredBoxes)
		{
			if (collisionShape.Name.ToString().Contains(
				"Floor", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			BoxShape3D box = (BoxShape3D)collisionShape.Shape;
			Vector3 localPoint = collisionShape.ToLocal(globalPoint);
			if (Mathf.Abs(localPoint.Y) >
				box.Size.Y * 0.5f + PlayerHalfHeight)
			{
				continue;
			}

			if (Mathf.Abs(localPoint.X) <=
					box.Size.X * 0.5f + PlayerRadius &&
				Mathf.Abs(localPoint.Z) <=
					box.Size.Z * 0.5f + PlayerRadius)
			{
				return true;
			}
		}

		return false;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
