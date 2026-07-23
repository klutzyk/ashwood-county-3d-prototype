#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class TownRoadNetworkValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D roads = world.GetNode<Node3D>("TownRoadNetwork");
			Require(roads.HasNode("DinerAccessIntersection/Collision"),
				"diner access creates a second collidable intersection");
			Require(world.HasNode("ResidentialDistrict/Roads/ResidentialStreet/Collision"),
				"residential intersection remains collidable");

			Node sidewalks = roads.GetNode("Sidewalks");
			Require(sidewalks.GetChildCount() == 4, "four continuous sidewalk runs are present");
			foreach (Node sidewalk in sidewalks.GetChildren())
			{
				Require(sidewalk is StaticBody3D && sidewalk.HasNode("Collision"),
					$"{sidewalk.Name} is grounded and collidable");
			}

			Require(roads.GetNode("RoadMarkings").GetChildCount() >= 10,
				"road network includes centre markings and stop lines");
			Require(roads.GetNode("Signs").GetChildCount() == 4,
				"two stop signs and two street-name signs control junctions");
			Require(roads.HasNode("Signs/ResidentialStopSign/Text") &&
				roads.HasNode("Signs/DinerStopSign/Text"),
				"both intersections have readable stop signs");
			Require(roads.GetNode("Barriers").GetChildCount() == 2,
				"barriers define both unfinished road ends");

			NavigationMesh? navigationMesh =
				roads.GetNode<NavigationRegion3D>("NavigationRegion3D").NavigationMesh;
			Require(navigationMesh is not null && navigationMesh.GetPolygonCount() == 1,
				"diner access has connected navigation coverage");
			Require(world.GetNode<NavigationRegion3D>(
				"ResidentialDistrict/NavigationRegion3D").NavigationMesh is not null,
				"residential navigation remains active");

			GD.Print("TOWN_ROAD_NETWORK_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"TOWN_ROAD_NETWORK_VALIDATION: FAIL - {exception.Message}");
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
