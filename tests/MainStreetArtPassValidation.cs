#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetArtPassValidation : Node
{
	private const string ScenePath = "res://scenes/world/ashwood/main_street.tscn";

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ValidateEnvironmentSeparation(world);
			ValidateStreetWear(world);
			ValidateStreetLife(world);
			ValidateCommercialIdentity(world);
			ValidatePerformanceIntent(world);

			GD.Print("MAIN_STREET_ART_PASS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"MAIN_STREET_ART_PASS_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateEnvironmentSeparation(Node3D world)
	{
		Node abandonment = world.GetNode("Environment/Abandonment");
		Require(abandonment.GetParent() == world.GetNode("Environment"),
			"art pass remains isolated under the environment hierarchy");
		Require(world.HasNode("Gameplay/Player") &&
			world.GetNode("Gameplay/Zombies").GetChildCount() == 2,
			"player and zombie composition remains unchanged");
		Require(!abandonment.FindChildren("*", "Script", true, false).Any(),
			"art pass adds no gameplay scripts");
	}

	private static void ValidateStreetWear(Node3D world)
	{
		Node art = world.GetNode("Environment/Abandonment");
		Require(art.GetNode("RoadWear").GetChildCount() >= 17,
			"road has layered patches, cracks, and oil staining");
		Require(art.GetNode("SidewalkWear").GetChildCount() >= 12,
			"sidewalks have authored repairs and cracking");
		Require(art.GetNode("Drainage").GetChildCount() == 6,
			"both curbs have repeated drainage grates");
		Require(art.GetNode("ReclaimingVegetation").GetChildCount() >= 24,
			"weeds reclaim curb and storefront seams along the full walk");

		StandardMaterial3D asphalt = GD.Load<StandardMaterial3D>(
			"res://assets/materials/ashwood_main_street_asphalt.tres");
		Require(asphalt.AlbedoTexture?.ResourcePath.Contains("road_damaged") == true,
			"street uses the downloaded damaged-asphalt PBR material");
	}

	private static void ValidateStreetLife(Node3D world)
	{
		Node art = world.GetNode("Environment/Abandonment");
		Node furniture = art.GetNode("StreetFurniture");
		Require(furniture.FindChildren("Meter*", string.Empty, false, false).Count == 12,
			"parking meters cover both sides without filling every bay");
		Require(furniture.FindChildren("Gazette*", string.Empty, false, false).Count == 4,
			"newspaper boxes anchor distinct business clusters");
		Require(furniture.FindChildren("*Rack", string.Empty, false, false).Count == 2,
			"bike racks support the walkable downtown character");

		Node utilities = art.GetNode("UtilityInfrastructure");
		Require(utilities.FindChildren("Pole*", string.Empty, false, false).Count == 6,
			"utility poles frame the full street");
		Require(utilities.FindChildren("Wire*", string.Empty, false, false).Count == 10,
			"two restrained power runs connect every pole");
		Require(utilities.FindChildren("Transformer*", string.Empty, false, false).Count == 2,
			"pole-mounted transformers break repetition");

		Node stories = art.GetNode("StreetStoryClusters");
		Require(stories.HasNode("DeliveryCrates") && stories.HasNode("DinerDumpster") &&
			stories.HasNode("WestAlleyDumpster") && stories.HasNode("BakeryTyre"),
			"delivery, service-alley, and roadside abandonment clusters are distinct");
		Require(art.GetNode("Litter").GetChildCount() >= 11,
			"litter and leaf accumulation is authored in local clusters");
	}

	private static void ValidateCommercialIdentity(Node3D world)
	{
		Node presentation = world.GetNode("Environment/Presentation");
		Node storefronts = presentation.GetNode("Storefronts");
		string[] requiredBusinesses =
		{
			"NorthBookstore",
			"NorthAttorney",
			"NorthInsurance",
			"SouthFlorist",
			"SouthBarber",
			"SouthMusicStore",
			"SouthDiner",
		};
		foreach (string business in requiredBusinesses)
			Require(storefronts.HasNode(business), $"{business} has a unique storefront");

		Node vehicles = presentation.GetNode("Vehicles");
		Require(vehicles.GetChildCount() == 6,
			"parking remains restrained rather than filling every space");
		HashSet<string> vehicleScenes = vehicles.GetChildren()
			.OfType<Node>()
			.Select(vehicle => vehicle.SceneFilePath)
			.Where(path => path.Length > 0)
			.ToHashSet();
		Require(vehicleScenes.Count >= 4,
			"parked sedans, pickups, van, and rusted car avoid adjacent clones");

		Node bakeryDisplay = world.GetNode("BakeryRoot/WindowDisplay");
		Require(bakeryDisplay.HasNode("BreadWindow/Loaf05") &&
			bakeryDisplay.HasNode("CakeWindow/CoffeeUrn"),
			"bakery windows show bread, cake, and coffee props without a full interior");
	}

	private static void ValidatePerformanceIntent(Node3D world)
	{
		Node art = world.GetNode("Environment/Abandonment");
		Node firstWeed = art.GetNode("ReclaimingVegetation/North01");
		IEnumerable<GeometryInstance3D> weedGeometry =
			firstWeed.FindChildren("*", string.Empty, true, false)
				.OfType<GeometryInstance3D>();
		Require(weedGeometry.All(geometry => geometry.VisibilityRangeEnd > 0.0f),
			"small reclaiming vegetation uses distance culling");

		int collisionBodies = art.FindChildren("*", string.Empty, true, false)
			.Count(node => node is CollisionObject3D);
		Require(collisionBodies >= 20 && collisionBodies <= 30,
			"only substantial street props receive simple collision");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
			throw new InvalidOperationException(message);
	}
}
