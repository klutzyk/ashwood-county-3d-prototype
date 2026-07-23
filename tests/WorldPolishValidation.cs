#nullable enable

using System;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class WorldPolishValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn")
				.Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ValidateResidentialClearance(world);
			ValidateDinerRoadClearance(world);
			ValidateLighting(world);
			ValidateNavigation(world);
			ValidateRenderer();

			GD.Print("WORLD_POLISH_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"WORLD_POLISH_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateResidentialClearance(Node world)
	{
		Node lots = world.GetNode("ResidentialDistrict/ResidentialLots");
		foreach (Node3D lot in lots.GetChildren().Cast<Node3D>())
		{
			MeshInstance3D driveway =
				lot.GetNode<MeshInstance3D>("RoadsideDressing/Driveway");
			Require(Mathf.IsEqualApprox(driveway.Scale.X, 0.44f),
				$"{lot.Name} driveway no longer projects deep into Birch Street");
			MeshInstance3D garden = lot.GetNode<MeshInstance3D>("FrontGarden");
			Require(garden.Position.X >= -3.01f,
				$"{lot.Name} front garden remains behind the road edge");
		}
	}

	private static void ValidateDinerRoadClearance(Node world)
	{
		StaticBody3D access =
			world.GetNode<StaticBody3D>("TownRoadNetwork/DinerAccessIntersection");
		BoxMesh roadMesh = (BoxMesh)access.GetNode<MeshInstance3D>("Mesh").Mesh;
		float roadEnd = access.Position.X + roadMesh.Size.X * 0.5f;
		float dinerFront = world.GetNode<Node3D>("Buildings/Diner").Position.X - 4.0f;
		Require(roadEnd <= dinerFront + 0.01f,
			"Diner Lane stops at the diner frontage instead of clipping beneath it");

		Node3D toolbox = world.GetNode<Node3D>("Buildings/ServiceStation/Toolbox");
		Node3D storageShelf =
			world.GetNode<Node3D>("Buildings/ServiceStation/Interior/StorageShelf");
		Require(toolbox.Position.DistanceTo(storageShelf.Position) >= 0.8f,
			"service-station toolbox clears the storage shelf");
	}

	private static void ValidateLighting(Node world)
	{
		foreach (Light3D light in world.FindChildren("*", "Light3D", true, false)
			.Cast<Light3D>())
		{
			if (light is DirectionalLight3D)
			{
				continue;
			}

			Require(!light.ShadowEnabled,
				$"{light.GetPath()} remains a lightweight non-shadowed local light");
		}
	}

	private static void ValidateNavigation(Node3D world)
	{
		NavigationMesh mainMesh =
			world.GetNode<NavigationRegion3D>("NavigationRegion3D").NavigationMesh!;
		Require(mainMesh.GetPolygonCount() >= 180,
			"rebuilt main navigation retains detailed town coverage");

		Rid map = world.GetWorld3D().NavigationMap;
		Vector3 south = new(15, 0.2f, 8.5f);
		Vector3 north = new(15, 0.2f, 23.5f);
		Vector3[] path = NavigationServer3D.MapGetPath(map, south, north, true);
		Require(path.Length >= 3, "navigation finds a route around the diner");

		float pathLength = 0.0f;
		for (int index = 1; index < path.Length; index++)
		{
			pathLength += path[index - 1].DistanceTo(path[index]);
		}
		Require(pathLength > south.DistanceTo(north) + 1.0f,
			"navigation routes around the diner shell instead of through it");
	}

	private static void ValidateRenderer()
	{
		string renderer =
			ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString();
		Require(renderer == "gl_compatibility",
			"Compatibility renderer remains configured");
		Require(Mathf.IsEqualApprox(
				ProjectSettings.GetSetting("navigation/3d/default_cell_height").AsSingle(),
				0.1f),
			"navigation map cell height matches the baked meshes");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
