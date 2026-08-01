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
		Node dressing = world.GetNode("Environment/ApocalypseDressing");
		Require(abandonment.GetParent() == world.GetNode("Environment"),
			"legacy surface treatment remains isolated under the environment hierarchy");
		Require(dressing.GetParent() == world.GetNode("Environment"),
			"dense apocalypse dressing is integrated under the environment hierarchy");
		Require(world.HasNode("Gameplay/Player") &&
			world.GetNode("Gameplay/Zombies").GetChildCount() == 5,
			"player and five-zombie supply-run composition is present");
		Require(!abandonment.FindChildren("*", "Script", true, false).Any(),
			"art pass adds no gameplay scripts");
	}

	private static void ValidateStreetWear(Node3D world)
	{
		Node art = world.GetNode("Environment/Abandonment");
		Require(art.GetNode("RoadWear").GetChildCount() >= 17,
			"road has layered patches, cracks, and oil staining");
		Require(art.GetNode("SidewalkWear").GetChildCount() >= 6,
			"sidewalks retain subtle authored cracking without flat patch slabs");
		Require(art.GetNode("Drainage").GetChildCount() == 6,
			"both curbs have repeated drainage grates");

		Node dressing = world.GetNode("Environment/ApocalypseDressing");
		Require(dressing.GetNode("VegetationReclaim").GetChildCount() >= 64,
			"imported vegetation reclaims curb and storefront seams along the full walk");

		StandardMaterial3D asphalt = GD.Load<StandardMaterial3D>(
			"res://assets/materials/ashwood_main_street_asphalt.tres");
		Require(asphalt.AlbedoTexture?.ResourcePath.Contains("road_damaged") == true,
			"street uses the downloaded damaged-asphalt PBR material");
	}

	private static void ValidateStreetLife(Node3D world)
	{
		Node art = world.GetNode("Environment/Abandonment");
		Node utilities = art.GetNode("UtilityInfrastructure");
		Require(utilities.FindChildren("Pole*", string.Empty, false, false).Count == 6,
			"utility poles frame the full street");
		Require(utilities.FindChildren("Wire*", string.Empty, false, false).Count == 10,
			"two restrained power runs connect every pole");

		Node stories = art.GetNode("StreetStoryClusters");
		Require(stories.HasNode("DeliveryCrates") && stories.HasNode("BakeryTyre"),
			"retained imported roadside props remain distinct");

		Node dressing = world.GetNode("Environment/ApocalypseDressing");
		Require(dressing.GetNode("RefuseDebris").GetChildCount() >= 52,
			"imported refuse and debris form varied sidewalk clusters");
		Require(dressing.GetNode("ServiceClutter").GetChildCount() >= 30,
			"service clutter spans both storefront rows");
		Require(dressing.GetNode("DamagedStreetFurniture").GetChildCount() >= 12,
			"detailed street furniture adds human-scale history");
		Require(dressing.GetNode("StoryClusters").GetChildCount() >= 16,
			"hand-authored apocalypse vignettes anchor both street ends");
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
			"NorthGrocery",
			"NorthPharmacy",
			"SouthFlorist",
			"SouthBarber",
			"SouthMusicStore",
			"SouthSportingGoods",
			"SouthMillerHardware",
			"SouthDiner",
			"SouthPoliceStation",
		};
		foreach (string business in requiredBusinesses)
			Require(storefronts.HasNode(business), $"{business} has a unique storefront");

		Node vehicles = presentation.GetNode("Vehicles");
		Require(vehicles.GetChildCount() == 7,
			"parking stays restrained while the bakery delivery van supports the route story");
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
		Node dressing = world.GetNode("Environment/ApocalypseDressing");
		GeometryInstance3D[] geometry = dressing
			.FindChildren("*", string.Empty, true, false)
			.OfType<GeometryInstance3D>()
			.ToArray();
		Require(geometry.Length >= 170,
			"integrated apocalypse dressing instantiates dense visible geometry");
		Require(geometry.All(instance => instance.VisibilityRangeEnd > 0.0f),
			"all dense imported dressing uses distance culling");

		int collisionBodies = dressing.FindChildren("*", string.Empty, true, false)
			.Count(node => node is CollisionObject3D);
		Require(collisionBodies <= 24,
			"only substantial sidewalk props retain simple collision");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
			throw new InvalidOperationException(message);
	}
}
