#nullable enable

using System;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetEnterableBuildingsValidation : Node
{
	private const string MainStreetScenePath =
		"res://scenes/world/ashwood/main_street.tscn";

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(MainStreetScenePath)
				.Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Node3D presentation =
				world.GetNode<Node3D>("Environment/Presentation");
			Node3D storefronts =
				presentation.GetNode<Node3D>("Storefronts");

			ValidateProductionBuildings(storefronts);
			ValidateStreetFacingEntrances(storefronts);
			ValidateObsoleteFacadesRemoved(presentation);
			ValidateHandPlacedFrontageClearance(presentation, storefronts);
			ValidatePoliceBasementGroundCut(world);
			ValidateGameplayCompositionUnchanged(world);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GD.Print("MAIN_STREET_ENTERABLE_BUILDINGS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_ENTERABLE_BUILDINGS_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateProductionBuildings(Node3D storefronts)
	{
		RequireScene(
			storefronts,
			"NorthGrocery",
			"/AshwoodGrocery/ashwood_grocery.tscn");
		RequireScene(
			storefronts,
			"SouthSportingGoods",
			"/WillowOutfitters/willow_outfitters.tscn");
		RequireScene(
			storefronts,
			"SouthMillerHardware",
			"/MillerHardware/miller_hardware.tscn");
		RequireScene(
			storefronts,
			"SouthPoliceStation",
			"/AshwoodPoliceStation/ashwood_police_station.tscn");

		foreach (string path in new[]
		{
			"NorthGrocery",
			"NorthPharmacy",
			"SouthSportingGoods",
			"SouthMillerHardware",
			"SouthDiner",
			"SouthPoliceStation",
		})
		{
			Node3D building = storefronts.GetNode<Node3D>(path);
			Vector3 scale = building.GlobalBasis.Scale;
			Require(scale.IsEqualApprox(Vector3.One),
				$"{path} is placed at authored scale without distorting its assets");
		}
	}

	private static void RequireScene(
		Node3D storefronts,
		string nodeName,
		string sceneSuffix)
	{
		Node3D building = storefronts.GetNode<Node3D>(nodeName);
		Require(building.SceneFilePath.EndsWith(
				sceneSuffix,
				StringComparison.Ordinal),
			$"{nodeName} instantiates its complete production building");
	}

	private static void ValidateStreetFacingEntrances(Node3D storefronts)
	{
		AssertEntranceBand(
			storefronts.GetNode<Node3D>("NorthGrocery/FrontDoor"),
			-10.0f,
			-8.4f,
			"Ashwood Grocery");
		AssertEntranceBand(
			storefronts.GetNode<Node3D>("NorthPharmacy/FrontDoor"),
			-10.0f,
			-8.4f,
			"Greenleaf Pharmacy");
		AssertEntranceBand(
			storefronts.GetNode<Node3D>("SouthSportingGoods/FrontDoor"),
			8.4f,
			10.0f,
			"Willow Outfitters");
		AssertEntranceBand(
			storefronts.GetNode<Node3D>("SouthMillerHardware/FrontDoor"),
			8.4f,
			10.0f,
			"Miller Hardware");
		AssertEntranceBand(
			storefronts.GetNode<Node3D>("SouthDiner/FrontDoor"),
			8.4f,
			10.0f,
			"Silver Spoon Diner");
		AssertEntranceBand(
			storefronts.GetNode<Node3D>(
				"SouthPoliceStation/FrontEntrance/LeftDoor"),
			8.4f,
			10.0f,
			"Ashwood Police Station");

		AssertFacadeDirection(
			storefronts.GetNode<Node3D>("NorthGrocery"),
			Vector3.Left,
			Vector3.Back,
			"Ashwood Grocery");
		AssertFacadeDirection(
			storefronts.GetNode<Node3D>("NorthPharmacy"),
			Vector3.Right,
			Vector3.Back,
			"Greenleaf Pharmacy");
		foreach (string path in new[]
		{
			"SouthSportingGoods",
			"SouthMillerHardware",
			"SouthDiner",
			"SouthPoliceStation",
		})
		{
			AssertFacadeDirection(
				storefronts.GetNode<Node3D>(path),
				Vector3.Left,
				Vector3.Forward,
				path);
		}
	}

	private static void AssertEntranceBand(
		Node3D entrance,
		float minimumZ,
		float maximumZ,
		string buildingName)
	{
		Require(entrance.GlobalPosition.Z >= minimumZ &&
			entrance.GlobalPosition.Z <= maximumZ,
			$"{buildingName} entrance faces and meets the Main Street sidewalk");
	}

	private static void AssertFacadeDirection(
		Node3D building,
		Vector3 localOutward,
		Vector3 expectedWorldOutward,
		string buildingName)
	{
		Vector3 worldOutward = (building.GlobalBasis * localOutward).Normalized();
		Require(worldOutward.Dot(expectedWorldOutward) >= 0.99f,
			$"{buildingName} facade points toward the road, not the back lot");
	}

	private static void ValidateObsoleteFacadesRemoved(Node3D presentation)
	{
		Node storefronts = presentation.GetNode("Storefronts");
		Require(!storefronts.HasNode("SouthSportingAnnex") &&
			!storefronts.HasNode("SouthOffices"),
			"prototype sporting and office facades are replaced, not hidden underneath");
		Require(!presentation.GetNode("BackgroundBuildings")
				.HasNode("EastCarpetWarehouse"),
			"obsolete warehouse no longer intersects the police station");
		Require(!presentation.GetNode("BusinessSigns")
				.HasNode("SportingAnnex") &&
			!presentation.GetNode("BusinessSigns")
				.HasNode("OfficesSouth"),
			"obsolete floating business labels are removed");
	}

	private static void ValidateHandPlacedFrontageClearance(
		Node3D presentation,
		Node3D storefronts)
	{
		Node3D groceryDoor =
			storefronts.GetNode<Node3D>("NorthGrocery/FrontDoor");
		Node3D hardwareDoor =
			storefronts.GetNode<Node3D>("SouthMillerHardware/FrontDoor");
		Node3D policeDoor = storefronts.GetNode<Node3D>(
			"SouthPoliceStation/FrontEntrance/LeftDoor");

		foreach ((string path, Node3D door, float clearance) in new[]
		{
			("Trees/North05", groceryDoor, 2.0f),
			("Furniture/PlanterNorth04", groceryDoor, 2.0f),
			("Shrubs/SouthCentre03", hardwareDoor, 2.0f),
			("Furniture/BenchSouth04", policeDoor, 2.0f),
			("Furniture/PlanterSouth06", policeDoor, 2.0f),
		})
		{
			Node3D prop = presentation.GetNode<Node3D>(path);
			Vector2 separation = new(
				prop.GlobalPosition.X - door.GlobalPosition.X,
				prop.GlobalPosition.Z - door.GlobalPosition.Z);
			Require(separation.Length() >= clearance,
				$"{path} is deliberately recomposed away from the entrance route");
		}
	}

	private static void ValidatePoliceBasementGroundCut(Node3D world)
	{
		StaticBody3D ground =
			world.GetNode<StaticBody3D>("Environment/Ground");
		CollisionShape3D[] segments = ground.GetChildren()
			.OfType<CollisionShape3D>()
			.Where(shape => shape.Shape is BoxShape3D)
			.ToArray();
		Require(segments.Length == 4,
			"grass ground is split into four efficient collision segments");

		Vector3 basementCentre = new(88.0f, -0.1f, 18.0f);
		Require(segments.All(shape =>
				!ContainsHorizontalPoint(shape, basementCentre)),
			"ground collision has a real opening around the police basement");
		Require(world.HasNode("Environment/Sidewalks/SouthEast") &&
			world.HasNode("Environment/Curbs/SouthEast"),
			"the continuous public sidewalk and curb remain intact");
	}

	private static bool ContainsHorizontalPoint(
		CollisionShape3D collision,
		Vector3 worldPoint)
	{
		BoxShape3D box = (BoxShape3D)collision.Shape;
		Vector3 localPoint = collision.ToLocal(worldPoint);
		return Mathf.Abs(localPoint.X) <= box.Size.X * 0.5f &&
			Mathf.Abs(localPoint.Z) <= box.Size.Z * 0.5f;
	}

	private static void ValidateGameplayCompositionUnchanged(Node3D world)
	{
		Require(world.HasNode("Gameplay/Player") &&
			world.GetNode("Gameplay/Zombies").GetChildCount() == 5,
			"enterable buildings coexist with the five-zombie supply-run threat");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
