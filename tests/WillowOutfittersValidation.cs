#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class WillowOutfittersValidation : Node
{
	private const string WillowScenePath =
		"res://assets/environment/buildings/WillowOutfitters/willow_outfitters.tscn";
	private const string PresentationScenePath =
		"res://scenes/world/ashwood/presentation/main_street_presentation.tscn";

	public override async void _Ready()
	{
		try
		{
			Node3D willow = GD.Load<PackedScene>(WillowScenePath)
				.Instantiate<Node3D>();
			AddChild(willow);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ValidateAssembly(willow);
			ValidateExterior(willow);
			ValidateInterior(willow);
			ValidateFixtures(willow);
			ValidateMerchandise(willow);
			await ValidateDoor(willow);
			ValidateMainStreetPlacement();

			GD.Print("WILLOW_OUTFITTERS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"WILLOW_OUTFITTERS_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateAssembly(Node3D willow)
	{
		Require(willow.Name == "WillowOutfitters",
			"production building has the Willow Outfitters identity");
		Require(willow.GetNode("Exterior") is StaticBody3D &&
			willow.GetNode("Interior") is StaticBody3D &&
			willow.GetNode("FrontDoor") is DoorController,
			"exterior, interior, and interactive entrance are assembled");
		Require(willow.GetNode<Node3D>("Exterior").SceneFilePath.EndsWith(
				"/WillowOutfitters/exterior.tscn",
				StringComparison.Ordinal),
			"exterior is maintained as a focused production scene");
		Require(willow.GetNode<Node3D>("Interior").SceneFilePath.EndsWith(
				"/WillowOutfitters/interior.tscn",
				StringComparison.Ordinal),
			"interior is maintained as a focused production scene");
	}

	private static void ValidateExterior(Node3D willow)
	{
		StaticBody3D exterior = willow.GetNode<StaticBody3D>("Exterior");
		Node shell = exterior.GetNode("Shell");
		Aabb shellBounds = GetCombinedBounds(shell);
		Require(shellBounds.Size.X >= 12.7f &&
			shellBounds.Size.Z >= 12.7f &&
			shellBounds.Size.Y >= 5.1f,
			"large shell provides a substantial exploratory retail footprint");
		Require(exterior.HasNode("Storefront/LeftWindow") &&
			exterior.HasNode("Storefront/WindowA") &&
			exterior.HasNode("Storefront/WindowB") &&
			exterior.HasNode("Storefront/WindowC"),
			"street facade has a full multi-bay glazed storefront");
		Require(exterior.GetNode<Label3D>("Identity/Name").Text ==
			"WILLOW OUTFITTERS",
			"facade carries the final business name");
		Require(exterior.GetNode("Awning").GetChildCount() >= 4,
			"weathered awning has authored ribs and edge construction");
		Require(exterior.FindChildren("*Collision", "CollisionShape3D", true, false)
				.Count >= 12,
			"shell uses doorway-aware wall, glass, and roof collision");
	}

	private static void ValidateInterior(Node3D willow)
	{
		StaticBody3D interior = willow.GetNode<StaticBody3D>("Interior");
		BoxShape3D floor = (BoxShape3D)interior
			.GetNode<CollisionShape3D>("FloorCollision").Shape;
		Require(floor.Size.X >= 12.0f && floor.Size.Z >= 12.0f,
			"sales floor is large enough to explore comfortably");
		Require(interior.HasNode("SalesFloor/BootWall") &&
			interior.HasNode("SalesFloor/NorthApparel") &&
			interior.HasNode("SalesFloor/SouthApparel") &&
			interior.HasNode("SalesFloor/CentralClothingRack") &&
			interior.HasNode("SalesFloor/FoldedClothingTable") &&
			interior.HasNode("SalesFloor/BackpackWall") &&
			interior.HasNode("SalesFloor/Checkout"),
			"sales floor contains distinct workwear, apparel, camping, and checkout zones");
		Require(interior.HasNode("Architecture/FittingDoorA") &&
			interior.HasNode("Architecture/FittingDoorB") &&
			interior.HasNode("Architecture/StorageDoor"),
			"fitting and stock rooms use imported full-size doors");
		Require(interior.GetNode("FittingRooms").GetChildCount() >= 4 &&
			interior.GetNode("Storage").GetChildCount() >= 8,
			"both rear fitting rooms and the stock room are fully dressed");
		Require(interior.HasNode("Storage/StoredBinoculars") &&
			interior.HasNode("Storage/StoredLantern") &&
			interior.HasNode("Storage/StoredCompass") &&
			interior.HasNode("Storage/StoredAxe"),
			"stock room includes a restrained mix of imported outdoor-goods inventory");
		Node3D storedHat = interior.GetNode<Node3D>("Storage/StoredHat");
		Node3D supportBox = interior.GetNode<Node3D>("Storage/BoxC");
		Require(storedHat.Position.Y <= 0.90f &&
			new Vector2(storedHat.Position.X, storedHat.Position.Z)
				.DistanceTo(new Vector2(
					supportBox.Position.X,
					supportBox.Position.Z)) <= 0.05f,
			"stored hat is visibly supported by the delivery box");

		Vector3 entryCentre = new(-6.2f, 0.0f, -4.12f);
		Vector3 rackPosition = interior
			.GetNode<Node3D>("SalesFloor/CentralClothingRack").Position;
		Require(Mathf.Abs(entryCentre.Z - rackPosition.Z) >= 2.5f,
			"front door opens onto a broad unobstructed entry aisle");
		Vector3 tablePosition = interior
			.GetNode<Node3D>("SalesFloor/FoldedClothingTable").Position;
		Require(Mathf.Abs(tablePosition.Z - rackPosition.Z) >= 2.5f,
			"central fixtures retain a walkable loop instead of forming a cramped maze");

		int collisionCount = interior.FindChildren(
			"*", "CollisionShape3D", true, false).Count;
		Require(collisionCount >= 20 && collisionCount <= 28,
			"substantial architecture and fixtures have restrained simple collision");

		IEnumerable<OmniLight3D> lights = interior.FindChildren(
				"*", "OmniLight3D", true, false)
			.OfType<OmniLight3D>();
		Require(lights.Count() == 5 && lights.Count(light => light.ShadowEnabled) == 1,
			"warm daytime fill remains performant with one shadow-casting interior light");
	}

	private static void ValidateFixtures(Node3D willow)
	{
		StaticBody3D interior = willow.GetNode<StaticBody3D>("Interior");
		string[] fixturePaths =
		{
			"SalesFloor/BootWall",
			"SalesFloor/NorthApparel",
			"SalesFloor/SouthApparel",
			"SalesFloor/CentralClothingRack",
			"SalesFloor/FoldedClothingTable",
			"SalesFloor/BackpackWall",
			"SalesFloor/Checkout",
			"FittingRooms/RoomA",
			"FittingRooms/RoomB",
		};
		foreach (string path in fixturePaths)
		{
			Node3D fixture = interior.GetNode<Node3D>(path);
			Require(fixture.SceneFilePath.Contains(
					"/WillowOutfitters/fixtures/willow_",
					StringComparison.Ordinal),
				$"{path} uses the textured authored fixture pack");
			Require(fixture.FindChildren(
					"*", "MeshInstance3D", true, false).Count >= 3,
				$"{path} contains detailed multi-part geometry");
		}

		Aabb rack = GetCombinedBounds(
			interior.GetNode("SalesFloor/CentralClothingRack"));
		Require(rack.Size.X >= 2.8f && rack.Size.Y >= 2.0f &&
			rack.Size.Z >= 0.7f,
			"clothing rack is full-scale and includes hanging garment volume");
		Aabb table = GetCombinedBounds(
			interior.GetNode("SalesFloor/FoldedClothingTable"));
		Require(table.Size.X >= 2.5f && table.Size.Y >= 1.1f,
			"folded-clothing display is a substantial retail fixture");
	}

	private static void ValidateMerchandise(Node3D willow)
	{
		StaticBody3D interior = willow.GetNode<StaticBody3D>("Interior");
		Require(interior.GetNode("WindowDisplay").GetChildCount() >= 6,
			"street windows present a tent, workwear, travel, and lantern vignette");
		Require(interior.GetNode("BootMerchandise").GetChildCount() >= 7,
			"boot wall mixes footwear, hats, and gloves");
		Require(interior.GetNode("OutdoorGoods").GetChildCount() >= 7,
			"camping display includes optics, compass, thermos, lantern, axes, and safety gear");

		Node[] downloadedExamples =
		{
			interior.GetNode("BootMerchandise/BootsLowA"),
			interior.GetNode("WindowDisplay/LifeJacket"),
			interior.GetNode("OutdoorGoods/Binoculars"),
			interior.GetNode("OutdoorGoods/Compass"),
			interior.GetNode("OutdoorGoods/Thermos"),
		};
		Require(downloadedExamples.All(node =>
				node.SceneFilePath.Contains(
					"/willow_outfitters/poly_haven/",
					StringComparison.Ordinal)),
			"hero merchandise uses licensed external PBR models");
		Require(interior.FindChildren(
				"*", "MeshInstance3D", true, false).Count >= 80,
			"interior has production-detail density rather than placeholder geometry");
	}

	private async System.Threading.Tasks.Task ValidateDoor(Node3D willow)
	{
		DoorController door = willow.GetNode<DoorController>("FrontDoor");
		Require(door.Position.X < -6.0f,
			"entrance is placed on the street-facing front wall");
		Require(door.HasNode("Hinge/DoorBody/DoorModel") &&
			door.HasNode("Hinge/DoorBody/CollisionShape3D") &&
			door.HasNode("Interactable"),
			"entrance uses imported door geometry, moving collision, and interaction");
		float closedRotation = door.GetNode<Node3D>("Hinge").Rotation.Y;
		door.AnimationDuration = 0.01f;
		door.ToggleDoor();
		await ToSignal(
			GetTree().CreateTimer(0.05),
			SceneTreeTimer.SignalName.Timeout);
		Require(door.IsOpen && !door.IsAnimating,
			"front door completes its interactive opening state");
		Require(IsNear(
				Mathf.RadToDeg(
					door.GetNode<Node3D>("Hinge").Rotation.Y - closedRotation),
				-105.0f,
				1.0f),
			"front door swings clear of the entry aisle");
	}

	private void ValidateMainStreetPlacement()
	{
		Node3D presentation = GD.Load<PackedScene>(PresentationScenePath)
			.Instantiate<Node3D>();
		AddChild(presentation);
		Node3D storefront = presentation.GetNode<Node3D>(
			"Storefronts/SouthSportingGoods");
		Require(storefront.SceneFilePath.EndsWith(
				"/WillowOutfitters/willow_outfitters.tscn",
				StringComparison.Ordinal),
			"Main Street replaces the old sporting-goods placeholder with Willow");
		Require(IsNear(storefront.Position.X, -13.0f) &&
			IsNear(storefront.Position.Z, 15.0f),
			"enlarged shell remains centred on its established downtown lot");
		Vector3 worldFront = storefront.Basis * Vector3.Left;
		Require(worldFront.Dot(Vector3.Forward) >= 0.99f,
			"Willow's entrance and windows face Main Street");
		Require(!presentation.HasNode("BusinessSigns/SportingGoods"),
			"obsolete Trail & Field sign no longer overlaps the final facade");
		presentation.QueueFree();
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

	private static bool IsNear(
		float value,
		float expected,
		float tolerance = 0.02f)
	{
		return Mathf.Abs(value - expected) <= tolerance;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
			throw new InvalidOperationException(message);
	}
}
