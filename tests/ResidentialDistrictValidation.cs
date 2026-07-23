#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ResidentialDistrictValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D district = world.GetNode<Node3D>("ResidentialDistrict");
			Node lots = district.GetNode("ResidentialLots");
			Require(lots.GetChildCount() == 8, "district contains eight modular residential lots");
			foreach (Node lot in lots.GetChildren())
			{
				Require(lot.HasNode("HouseExterior"), $"{lot.Name} reuses the modular house exterior");
				Require(lot.HasNode("RoadsideDressing/Driveway"), $"{lot.Name} includes a driveway");
				Require(lot.HasNode("RoadsideDressing/FenceNorth"), $"{lot.Name} includes fencing");
				Require(lot.HasNode("RoadsideDressing/RubbishBin"), $"{lot.Name} includes a rubbish bin");
				Require(lot.HasNode("RoadsideDressing/BushFrontLeft"), $"{lot.Name} includes bushes");
				Require(lot.HasNode("FrontGarden"), $"{lot.Name} includes a garden");
			}

			Require(district.HasNode("Roads/MainRoadExtension/Collision"),
				"existing road is extended with collision");
			Require(district.HasNode("Roads/ResidentialStreet/Collision"),
				"one residential street is present with collision");
			Require(district.GetNode("Infrastructure").GetChildCount() == 4,
				"utility poles are spaced through the district");
			Require(district.GetNode("AbandonedVehicles").GetChildCount() == 2,
				"abandoned vehicles dress the district");

			NavigationRegion3D navigation = district.GetNode<NavigationRegion3D>("NavigationRegion3D");
			NavigationMesh? navigationMesh = navigation.NavigationMesh;
			Require(navigationMesh is not null, "district has a navigation mesh");
			Require(navigationMesh!.GetPolygonCount() == 3,
				"navigation covers the road extension and residential street");

			BoxMesh terrain = (BoxMesh)district.GetNode<MeshInstance3D>("Terrain/Mesh").Mesh;
			float existingGroundArea = 50.0f * 50.0f;
			float expansionRatio = terrain.Size.X * terrain.Size.Z / existingGroundArea;
			Require(expansionRatio >= 0.5f && expansionRatio <= 0.7f,
				"district ground expands the town footprint by approximately fifty percent");

			GD.Print("RESIDENTIAL_DISTRICT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"RESIDENTIAL_DISTRICT_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
