#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.Tests;

public partial class SilverSpoonDinerValidation : Node
{
	private const string DinerScenePath =
		"res://assets/environment/buildings/Diner/diner.tscn";
	private const string MainStreetPresentationPath =
		"res://scenes/world/ashwood/presentation/main_street_presentation.tscn";
	private const float PlayerRadius = 0.45f;
	private const float PlayerHalfHeight = 0.9f;

	public override async void _Ready()
	{
		try
		{
			Node3D diner = GD.Load<PackedScene>(DinerScenePath)
				.Instantiate<Node3D>();
			AddChild(diner);

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			StaticBody3D exterior = diner.GetNode<StaticBody3D>("Exterior");
			StaticBody3D interior = diner.GetNode<StaticBody3D>("Interior");
			DoorController frontDoor =
				diner.GetNode<DoorController>("FrontDoor");
			SearchableContainer pantry =
				diner.GetNode<SearchableContainer>("Pantry");
			SearchableContainer fridge =
				diner.GetNode<SearchableContainer>("Fridge");

			ValidateProtectedAssembly(
				diner,
				exterior,
				interior,
				frontDoor,
				pantry,
				fridge);
			ValidateConceptZones(interior);
			ValidateDinerFixtures(interior);
			ValidateImportedPropDensity(interior);
			ValidateMajorOnlyCollision(interior);
			ValidatePerformanceIntent(interior);
			RequireNoModernDiner(diner, "standalone diner");
			await ValidateDoorAndClearance(diner, frontDoor);

			diner.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ValidateMainStreetAssembly();

			GD.Print("SILVER_SPOON_DINER_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"SILVER_SPOON_DINER_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateProtectedAssembly(
		Node3D diner,
		StaticBody3D exterior,
		StaticBody3D interior,
		DoorController frontDoor,
		SearchableContainer pantry,
		SearchableContainer fridge)
	{
		Require(exterior.SceneFilePath.EndsWith(
				"/Diner/exterior.tscn",
				StringComparison.Ordinal),
			"Exterior remains the dedicated Silver Spoon production exterior");
		Require(interior.SceneFilePath.EndsWith(
				"/Diner/interior.tscn",
				StringComparison.Ordinal),
			"Interior remains the dedicated Silver Spoon production interior");
		Require(frontDoor.SceneFilePath.EndsWith(
				"/Diner/front_door.tscn",
				StringComparison.Ordinal),
			"FrontDoor remains the diner-specific functional production door");
		Require(pantry.SceneFilePath.EndsWith(
				"/containers/diner_pantry.tscn",
				StringComparison.Ordinal),
			"Pantry preserves the established diner pantry scene");
		Require(fridge.SceneFilePath.EndsWith(
				"/containers/diner_fridge.tscn",
				StringComparison.Ordinal),
			"Fridge preserves the established diner fridge scene");

		Require(diner.GetNode("Exterior") is StaticBody3D &&
			diner.GetNode("Interior") is StaticBody3D &&
			diner.GetNode("FrontDoor") is DoorController &&
			diner.GetNode("Pantry") is SearchableContainer &&
			diner.GetNode("Fridge") is SearchableContainer,
			"protected diner paths retain their established runtime types");
		Require(exterior.CollisionLayer == 1 && interior.CollisionLayer == 1,
			"assembled shell and interior use the environment collision layer");

		Node3D storefrontGlass =
			exterior.GetNode<Node3D>("StorefrontGlass");
		Require(storefrontGlass.GetNode("NorthWindow") is MeshInstance3D &&
			storefrontGlass.GetNode("SouthWindow") is MeshInstance3D &&
			storefrontGlass.GetChildren().OfType<MeshInstance3D>().Count() == 2,
			"exterior has exactly two dedicated storefront window panes");
		Require(exterior.GetNode<Label3D>("Identity/Name").Text ==
			"SILVER SPOON DINER",
			"exterior presents the Silver Spoon Diner identity");

		Require(frontDoor.HasNode("Hinge/DoorBody/DoorModel") &&
			frontDoor.GetNode("Hinge/DoorBody/DoorModel")
				.SceneFilePath.StartsWith(
					"res://assets/third_party/",
					StringComparison.Ordinal),
			"production front door uses the curated shop-door asset");
		Require(frontDoor.GetNode<CollisionShape3D>(
				"Hinge/DoorBody/CollisionShape3D").Shape is BoxShape3D,
			"production front door uses one simplified moving collision box");

		ValidateContainer(
			pantry,
			"Diner Pantry",
			"/loot_tables/cupboard.tres");
		ValidateContainer(
			fridge,
			"Diner Fridge",
			"/loot_tables/fridge.tres");
	}

	private static void ValidateContainer(
		SearchableContainer container,
		string displayName,
		string lootTableSuffix)
	{
		Require(container.DisplayName == displayName,
			$"{displayName} preserves its interaction display name");
		Require(container.LootTable?.ResourcePath.EndsWith(
				lootTableSuffix,
				StringComparison.Ordinal) == true,
			$"{displayName} preserves its configured loot table");
		Require(container.HasNode("Interactable") &&
			container.GetNode("Interactable") is Interactable &&
			container.HasNode("Inventory") &&
			container.GetNode("Inventory") is ContainerInventory,
			$"{displayName} retains interaction and independent inventory state");
		Require(container.Inventory ==
			container.GetNode<ContainerInventory>("Inventory"),
			$"{displayName} exposes its own initialized container inventory");
	}

	private static void ValidateConceptZones(StaticBody3D interior)
	{
		foreach (string path in new[]
		{
			"Architecture",
			"DiningRoom/Booths",
			"DiningRoom/CounterStools",
			"ServiceCounter",
			"Kitchen/CookLine",
			"Kitchen/Washing",
			"Kitchen/Prep",
			"Office",
			"Storage",
			"Restroom",
			"ColdStorage",
			"Abandonment",
			"Signage",
			"Lighting",
		})
		{
			Require(interior.HasNode(path),
				$"{path} preserves a required diner concept-art zone");
		}

		Require(interior.GetNode("Office").GetChildCount() >= 7 &&
			interior.GetNode("Storage").GetChildCount() >= 5,
			"office and dry storage are distinct, furnished destinations");
		Require(interior.HasNode("Restroom/Toilet") &&
			interior.HasNode("Restroom/Sink"),
			"restroom contains the expected sanitary fixtures");
		Require(interior.GetNode("Abandonment").GetChildCount() >= 5,
			"restrained abandoned clutter gives the diner a lived-in history");
	}

	private static void ValidateDinerFixtures(StaticBody3D interior)
	{
		ValidateNamedFixture(
			interior,
			"DiningRoom/Booths/NorthRear",
			"diner_booth_bay.glb");
		ValidateNamedFixture(
			interior,
			"DiningRoom/Tables/NorthRear",
			"diner_pedestal_table.glb");
		ValidateNamedFixture(
			interior,
			"ServiceCounter/Counter",
			"diner_service_counter.glb");
		ValidateNamedFixture(
			interior,
			"Kitchen/Washing/TripleSink",
			"diner_commercial_sink.glb");
		ValidateNamedFixture(
			interior,
			"Kitchen/CookLine/GriddleRange",
			"diner_griddle_range.glb");
		ValidateNamedFixture(
			interior,
			"Kitchen/CookLine/TwinDeepFryer",
			"diner_deep_fryer.glb");
		ValidateNamedFixture(
			interior,
			"Kitchen/CookLine/ExtractorHood",
			"diner_extractor_hood.glb");

		Node booths = interior.GetNode("DiningRoom/Booths");
		Node tables = interior.GetNode("DiningRoom/Tables");
		Node counterStools = interior.GetNode("DiningRoom/CounterStools");
		Require(booths.GetChildren().Count(node =>
				node.SceneFilePath.EndsWith(
					"/fixtures/diner_booth_bay.glb",
					StringComparison.Ordinal)) == 4,
			"dining room has four complete authored booth instances");
		Require(tables.GetChildren().Count(node =>
				node.SceneFilePath.EndsWith(
					"/fixtures/diner_pedestal_table.glb",
					StringComparison.Ordinal)) == 4,
			"each booth bay has a dedicated pedestal table instance");
		Require(counterStools.GetChildCount() == 5 &&
			counterStools.GetChildren().All(node =>
				node.SceneFilePath.EndsWith(
					"/metal_stool_01_1k.gltf",
					StringComparison.Ordinal)),
			"service counter has five curated metal stool instances");
	}

	private static void ValidateNamedFixture(
		StaticBody3D interior,
		string nodePath,
		string fileName)
	{
		Node fixture = interior.GetNode(nodePath);
		Require(fixture.SceneFilePath.EndsWith(
				$"/Diner/fixtures/{fileName}",
				StringComparison.Ordinal),
			$"{nodePath} uses the authored {fileName} fixture");
	}

	private static void ValidateImportedPropDensity(StaticBody3D interior)
	{
		int importedAssetRoots = interior
			.FindChildren("*", string.Empty, true, false)
			.Count(node => node.SceneFilePath.StartsWith(
				"res://assets/third_party/",
				StringComparison.Ordinal));
		Require(importedAssetRoots >= 50 && importedAssetRoots <= 90,
			"curated imported prop density is substantial but bounded");
	}

	private static void ValidateMajorOnlyCollision(StaticBody3D interior)
	{
		CollisionShape3D[] authoredShapes = interior.GetChildren()
			.OfType<CollisionShape3D>()
			.ToArray();
		Require(interior.CollisionLayer == 1 &&
			interior.CollisionMask == 1,
			"interior collision uses the established world layer and mask");
		Require(authoredShapes.Length >= 18 && authoredShapes.Length <= 28,
			"direct collision remains limited to architecture and major fixtures");
		Require(authoredShapes.All(shape =>
				!shape.Disabled && shape.Shape is BoxShape3D),
			"direct authored collision uses active simplified boxes only");

		foreach (string path in new[]
		{
			"BoothNorthRearCollision",
			"BoothSouthRearCollision",
			"CounterCollision",
			"CoffeeCartCollision",
			"GriddleCollision",
			"FryerCollision",
			"SinkCollision",
			"OfficeDeskCollision",
			"ToiletCollision",
		})
		{
			Require(interior.HasNode(path),
				$"{path} covers a substantial traversal obstacle");
		}

		foreach (string path in new[]
		{
			"DiningRoom/TableSettings",
			"ServiceCounter",
			"Kitchen/Prep",
			"Abandonment",
			"Signage",
			"Lighting",
		})
		{
			Require(interior.GetNode(path)
					.FindChildren("*", "CollisionShape3D", true, false).Count == 0,
				$"{path} detail avoids wasteful per-prop collision");
		}
	}

	private static void ValidatePerformanceIntent(StaticBody3D interior)
	{
		Node[] descendants = interior
			.FindChildren("*", string.Empty, true, false)
			.ToArray();
		int geometryCount =
			descendants.Count(node => node is GeometryInstance3D);
		Require(geometryCount >= 100 && geometryCount <= 700,
			"diner geometry density remains bounded for the Compatibility renderer");
		Require(descendants.Length <= 1600,
			"production diner keeps a bounded runtime node count");

		OmniLight3D[] lights =
			descendants.OfType<OmniLight3D>().ToArray();
		Require(lights.Length == 2 &&
			lights.All(light => !light.ShadowEnabled),
			"exactly two lightweight shadowless Omni lights support the daytime interior");
		Require(lights.All(light =>
				light.LightEnergy >= 0.2f &&
				light.LightEnergy <= 0.7f),
			"diner fill-light energy remains restrained");
	}

	private async System.Threading.Tasks.Task ValidateDoorAndClearance(
		Node3D diner,
		DoorController frontDoor)
	{
		Node3D hinge = frontDoor.GetNode<Node3D>("Hinge");
		float closedAngle = hinge.Rotation.Y;

		frontDoor.AnimationDuration = 0.01f;
		frontDoor.ToggleDoor();
		await WaitForDoor(frontDoor);

		float expectedOpenAngle =
			closedAngle + Mathf.DegToRad(frontDoor.OpenAngleDegrees);
		Require(frontDoor.IsOpen && !frontDoor.IsAnimating &&
			Mathf.Abs(Mathf.AngleDifference(
				hinge.Rotation.Y,
				expectedOpenAngle)) <= 0.02f,
			"front door interaction opens the visible model and collision together");

		CollisionShape3D[] authoredBoxes = diner
			.FindChildren("*", "CollisionShape3D", true, false)
			.OfType<CollisionShape3D>()
			.Where(shape =>
				!shape.Disabled &&
				shape.Shape is BoxShape3D)
			.ToArray();

		ValidateClearRoute(diner, authoredBoxes, "front entrance", new[]
		{
			new Vector3(-4.45f, 0.9f, -0.55f),
			new Vector3(-4.05f, 0.9f, -0.55f),
			new Vector3(-3.65f, 0.9f, -0.55f),
			new Vector3(-3.25f, 0.9f, -0.55f),
			new Vector3(-2.7f, 0.9f, -0.55f),
		});
		ValidateClearRoute(diner, authoredBoxes, "main dining aisle", new[]
		{
			new Vector3(-2.3f, 0.9f, -0.55f),
			new Vector3(-1.9f, 0.9f, -0.55f),
			new Vector3(-1.55f, 0.9f, -0.55f),
		});
		ValidateClearRoute(diner, authoredBoxes, "behind-counter access", new[]
		{
			new Vector3(-1.55f, 0.9f, 2.85f),
			new Vector3(-1.1f, 0.9f, 2.85f),
			new Vector3(-0.6f, 0.9f, 2.85f),
			new Vector3(-0.15f, 0.9f, 2.85f),
			new Vector3(0.4f, 0.9f, 2.85f),
			new Vector3(0.9f, 0.9f, 2.85f),
		});
		ValidateClearRoute(diner, authoredBoxes, "kitchen working aisle", new[]
		{
			new Vector3(1.15f, 0.9f, 2.45f),
			new Vector3(1.15f, 0.9f, 1.5f),
			new Vector3(1.15f, 0.9f, 0.5f),
			new Vector3(1.15f, 0.9f, -0.8f),
			new Vector3(1.15f, 0.9f, -2.2f),
		});
		ValidateClearRoute(diner, authoredBoxes, "office doorway", new[]
		{
			new Vector3(1.55f, 0.9f, -3.25f),
			new Vector3(1.55f, 0.9f, -3.55f),
			new Vector3(1.55f, 0.9f, -3.82f),
			new Vector3(1.55f, 0.9f, -4.05f),
		});
		ValidateClearRoute(diner, authoredBoxes, "pantry doorway", new[]
		{
			new Vector3(3.15f, 0.9f, -3.25f),
			new Vector3(3.15f, 0.9f, -3.55f),
			new Vector3(3.15f, 0.9f, -3.82f),
			new Vector3(3.15f, 0.9f, -4.05f),
		});
		ValidateClearRoute(diner, authoredBoxes, "restroom doorway", new[]
		{
			new Vector3(1.52f, 0.9f, 3.25f),
			new Vector3(1.52f, 0.9f, 3.55f),
			new Vector3(1.52f, 0.9f, 3.82f),
			new Vector3(1.52f, 0.9f, 4.1f),
		});
		ValidateClearRoute(diner, authoredBoxes, "cold-storage doorway", new[]
		{
			new Vector3(3.15f, 0.9f, 3.25f),
			new Vector3(3.15f, 0.9f, 3.55f),
			new Vector3(3.15f, 0.9f, 3.82f),
			new Vector3(3.15f, 0.9f, 4.1f),
		});

		frontDoor.ToggleDoor();
		await WaitForDoor(frontDoor);
		Require(!frontDoor.IsOpen && !frontDoor.IsAnimating &&
			Mathf.Abs(Mathf.AngleDifference(
				hinge.Rotation.Y,
				closedAngle)) <= 0.02f,
			"front door can toggle cleanly back to its closed state");
	}

	private async System.Threading.Tasks.Task WaitForDoor(
		DoorController frontDoor)
	{
		for (int frame = 0;
			frame < 8 && frontDoor.IsAnimating;
			frame++)
		{
			await ToSignal(
				GetTree(),
				SceneTree.SignalName.ProcessFrame);
		}
	}

	private static void ValidateClearRoute(
		Node3D diner,
		IEnumerable<CollisionShape3D> authoredBoxes,
		string routeName,
		IEnumerable<Vector3> points)
	{
		foreach (Vector3 point in points)
		{
			Require(!OverlapsAuthoredBox(
					diner,
					point,
					authoredBoxes),
				$"{routeName} clears the 0.45 m player radius at {point}");
		}
	}

	private static bool OverlapsAuthoredBox(
		Node3D diner,
		Vector3 dinerLocalPoint,
		IEnumerable<CollisionShape3D> authoredBoxes)
	{
		Vector3 globalPoint = diner.ToGlobal(dinerLocalPoint);
		foreach (CollisionShape3D collisionShape in authoredBoxes)
		{
			string shapeName = collisionShape.Name.ToString();
			if (shapeName.Contains(
					"Floor",
					StringComparison.OrdinalIgnoreCase) ||
				shapeName.Contains(
					"Roof",
					StringComparison.OrdinalIgnoreCase) ||
				shapeName.Contains(
					"Ceiling",
					StringComparison.OrdinalIgnoreCase))
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

	private async System.Threading.Tasks.Task ValidateMainStreetAssembly()
	{
		Node3D presentation = GD.Load<PackedScene>(
				MainStreetPresentationPath)
			.Instantiate<Node3D>();
		AddChild(presentation);

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		Node3D southDiner =
			presentation.GetNode<Node3D>("Storefronts/SouthDiner");
		Require(southDiner.SceneFilePath.EndsWith(
				"/Diner/diner.tscn",
				StringComparison.Ordinal),
			"Main Street SouthDiner instantiates the complete production diner");
		Require(southDiner.GetNode("Exterior") is StaticBody3D &&
			southDiner.GetNode("Interior") is StaticBody3D &&
			southDiner.GetNode("FrontDoor") is DoorController &&
			southDiner.GetNode("Pantry") is SearchableContainer &&
			southDiner.GetNode("Fridge") is SearchableContainer,
			"Main Street SouthDiner is fully assembled, not an exterior-only facade");

		ValidateContainer(
			southDiner.GetNode<SearchableContainer>("Pantry"),
			"Diner Pantry",
			"/loot_tables/cupboard.tres");
		ValidateContainer(
			southDiner.GetNode<SearchableContainer>("Fridge"),
			"Diner Fridge",
			"/loot_tables/fridge.tres");
		RequireNoModernDiner(presentation, "Main Street presentation");

		presentation.QueueFree();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private static void RequireNoModernDiner(Node root, string context)
	{
		IEnumerable<Node> nodes = new[] { root }.Concat(
			root.FindChildren("*", string.Empty, true, false));
		string[] offenders = nodes
			.Where(node => node.SceneFilePath.Contains(
				"modern_diner",
				StringComparison.OrdinalIgnoreCase))
			.Select(node => $"{node.GetPath()} ({node.SceneFilePath})")
			.ToArray();
		Require(offenders.Length == 0,
			$"{context} rejects modern_diner descendants: " +
			string.Join(", ", offenders));
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
