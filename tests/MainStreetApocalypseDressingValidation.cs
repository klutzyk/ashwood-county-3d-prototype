#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetApocalypseDressingValidation : Node
{
	private const string ScenePath =
		"res://scenes/world/ashwood/presentation/main_street_apocalypse_dressing.tscn";

	private static readonly string[] CategoryNames =
	{
		"VegetationReclaim",
		"RefuseDebris",
		"ServiceClutter",
		"DamagedStreetFurniture",
		"StoryClusters",
	};

	private static readonly (string Name, float X, bool North)[] Entrances =
	{
		("Glen's Bakery", -59.5f, true),
		("Ashwood Grocery", 26.0f, true),
		("Greenleaf Pharmacy", 44.0f, true),
		("Willow Outfitters", -13.0f, false),
		("Miller Hardware", 29.0f, false),
		("Silver Spoon Diner", 54.0f, false),
		("Ashwood Police Station", 88.0f, false),
	};

	public override async void _Ready()
	{
		try
		{
			Node3D dressing = GD.Load<PackedScene>(ScenePath)
				.Instantiate<Node3D>();
			AddChild(dressing);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D[] placements = CollectPlacements(dressing);
			ValidateDensityAndVariation(placements);
			ValidateFullStreetSpread(placements);
			ValidateCategories(dressing);
			ValidateIrregularComposition(dressing);
			ValidateEntranceRoutes(placements);
			ValidateRoadClearance(placements);
			ValidatePerformanceIntent(dressing);

			GD.Print(
				$"MAIN_STREET_APOCALYPSE_DRESSING_VALIDATION: PASS " +
				$"({placements.Length} hand-authored placements)");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_APOCALYPSE_DRESSING_VALIDATION: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
		}
	}

	private static Node3D[] CollectPlacements(Node3D dressing)
	{
		return CategoryNames
			.Select(name => dressing.GetNode<Node3D>(name))
			.SelectMany(category => category.GetChildren().OfType<Node3D>())
			.ToArray();
	}

	private static void ValidateDensityAndVariation(Node3D[] placements)
	{
		Require(placements.Length >= 170,
			"dressing contains at least 170 individually composed asset placements");

		HashSet<string> sourceScenes = placements
			.Select(node => node.SceneFilePath)
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.ToHashSet(StringComparer.Ordinal);
		Require(sourceScenes.Count >= 18,
			"dressing reuses at least 18 distinct imported or textured source assets");

		int rotated = placements.Count(node =>
			Mathf.Abs(node.Rotation.X) > 0.01f ||
			Mathf.Abs(node.Rotation.Y) > 0.01f ||
			Mathf.Abs(node.Rotation.Z) > 0.01f);
		int nonUniformScale = placements.Count(node =>
			!Mathf.IsEqualApprox(node.Scale.X, node.Scale.Y) ||
			!Mathf.IsEqualApprox(node.Scale.Y, node.Scale.Z));
		Require(rotated >= 150,
			"repeated assets have hand-authored orientation variation");
		Require(nonUniformScale >= 55,
			"vegetation and soft debris break repetition with authored silhouettes");
	}

	private static void ValidateFullStreetSpread(Node3D[] placements)
	{
		float minimumX = placements.Min(node => node.Position.X);
		float maximumX = placements.Max(node => node.Position.X);
		int northCount = placements.Count(node => node.Position.Z < -5.0f);
		int southCount = placements.Count(node => node.Position.Z > 5.0f);

		Require(minimumX <= -97.0f && maximumX >= 97.0f,
			"dressing spans the complete 200-metre Main Street");
		Require(northCount >= 80 && southCount >= 80,
			"both sidewalks and road edges receive comparable authored density");
	}

	private static void ValidateCategories(Node3D dressing)
	{
		Require(dressing.GetNode("VegetationReclaim").GetChildCount() >= 64,
			"vegetation reclaiming is distributed along both storefront rows");
		Require(dressing.GetNode("RefuseDebris").GetChildCount() >= 52,
			"bags, boxes, crates, and tyres form varied refuse clusters");
		Require(dressing.GetNode("ServiceClutter").GetChildCount() >= 30,
			"service clutter includes roadwork, bins, containers, and utilities");
		Require(dressing.GetNode("DamagedStreetFurniture").GetChildCount() >= 12,
			"street furniture adds human-scale sidewalk history");
		Require(dressing.GetNode("StoryClusters").GetChildCount() >= 16,
			"distinct evacuation and scavenging vignettes anchor the street ends");

		string[] requiredFragments =
		{
			"Bush",
			"TrashBag",
			"Cardboard",
			"Crate",
			"Tyre",
			"Jerrycan",
			"Toolbox",
			"Utility",
			"PublicBin",
			"RoadBarrier",
			"Bench",
			"Mailbox",
			"Hydrant",
			"HandTruck",
			"Barrel",
			"Broom",
			"RepairBag",
			"StorageCart",
			"Tent",
			"Fridge",
			"Shelf",
			"Firepit",
		};

		HashSet<string> names = CollectPlacements(dressing)
			.Select(node => node.Name.ToString())
			.ToHashSet(StringComparer.Ordinal);
		foreach (string fragment in requiredFragments)
		{
			Require(names.Any(name => name.Contains(
					fragment,
					StringComparison.Ordinal)),
				$"{fragment} asset category is represented");
		}
	}

	private static void ValidateEntranceRoutes(Node3D[] placements)
	{
		const float halfWidth = 2.4f;
		foreach ((string name, float x, bool north) in Entrances)
		{
			Node3D[] blockers = placements
				.Where(node => Mathf.Abs(node.Position.X - x) < halfWidth)
				.Where(node => north
					? node.Position.Z <= -5.4f
					: node.Position.Z >= 5.4f)
				.ToArray();
			Require(blockers.Length == 0,
				$"{name} keeps a 4.8-metre sidewalk-to-door approach clear");
		}
	}

	private static void ValidateIrregularComposition(Node3D dressing)
	{
		Node3D vegetation = dressing.GetNode<Node3D>("VegetationReclaim");
		foreach (bool north in new[] { true, false })
		{
			float[] xPositions = vegetation
				.GetChildren()
				.OfType<Node3D>()
				.Where(node => north
					? node.Position.Z < 0.0f
					: node.Position.Z > 0.0f)
				.Select(node => node.Position.X)
				.OrderBy(x => x)
				.ToArray();
			float[] gaps = xPositions
				.Skip(1)
				.Select((x, index) => x - xPositions[index])
				.ToArray();

			Require(gaps.Count(gap => gap <= 1.7f) >= 6,
				"vegetation includes tight two- and three-plant reclaim clusters");
			Require(gaps.Count(gap => gap >= 9.0f) >= 4,
				"vegetation leaves deliberate calm storefront stretches");
		}

		Node3D[] hardClutter = new[]
			{
				"RefuseDebris",
				"ServiceClutter",
				"DamagedStreetFurniture",
				"StoryClusters",
			}
			.Select(name => dressing.GetNode<Node3D>(name))
			.SelectMany(category => category.GetChildren().OfType<Node3D>())
			.ToArray();
		int clusteredPlacements = hardClutter.Count(node =>
			hardClutter.Any(other =>
				other != node &&
				Mathf.Sign(other.Position.Z) ==
					Mathf.Sign(node.Position.Z) &&
				Mathf.Abs(other.Position.X - node.Position.X) <= 2.0f));
		Require(clusteredPlacements >= 42,
			"refuse and service props form local stories rather than even seeding");

		int distinctDepths = hardClutter
			.Select(node => Mathf.RoundToInt(node.Position.Z * 10.0f))
			.Distinct()
			.Count();
		Require(distinctDepths >= 40,
			"hard clutter breaks repeated curb/facade depth bands");
	}

	private static void ValidateRoadClearance(Node3D[] placements)
	{
		Require(placements.All(node => Mathf.Abs(node.Position.Z) >= 5.0f),
			"all dressing stays out of both live travel lanes");

		Node3D[] substantialRoadEdgeProps = placements
			.Where(node => node.Name.ToString().Contains(
					"Barrier",
					StringComparison.Ordinal) ||
				node.Name.ToString().Contains(
					"Tent",
					StringComparison.Ordinal))
			.ToArray();
		Require(substantialRoadEdgeProps.All(node =>
				Mathf.Abs(node.Position.Z) >= 5.1f),
			"roadwork and relief clusters remain confined to curbside space");
	}

	private static void ValidatePerformanceIntent(Node3D dressing)
	{
		GeometryInstance3D[] geometry = dressing
			.FindChildren("*", string.Empty, true, false)
			.OfType<GeometryInstance3D>()
			.ToArray();
		Require(geometry.Length >= 170,
			"asset placements instantiate visible geometry");
		Require(geometry.All(instance => instance.VisibilityRangeEnd > 0.0f),
			"every imported geometry instance receives a distance-culling range");
		Require(geometry.All(instance =>
				instance.VisibilityRangeEndMargin >= 0.0f),
			"distance-culling margins remain valid");

		int collisionBodies = dressing
			.FindChildren("*", string.Empty, true, false)
			.Count(node => node is CollisionObject3D);
		Require(collisionBodies <= 24,
			"only sparse substantial sidewalk props retain simple collision");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
