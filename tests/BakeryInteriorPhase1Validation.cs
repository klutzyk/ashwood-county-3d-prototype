#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class BakeryInteriorPhase1Validation : Node
{
	private const string ScenePath = "res://scenes/world/ashwood/bakery.tscn";
	private const string InteriorPath = "ProductionInterior";

	public override async void _Ready()
	{
		try
		{
			Node3D bakery = GD.Load<PackedScene>(ScenePath).Instantiate<Node3D>();
			AddChild(bakery);
			Node3D door = bakery.GetNode<Node3D>("FrontDoorPivot");
			door.RotationDegrees = new Vector3(0.0f, -100.0f, 0.0f);

			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Node3D interior = bakery.GetNode<Node3D>(InteriorPath);
			ValidateProductionComposition(bakery, interior);
			ValidateLicensedAssets(interior);
			ValidateRetailAndKitchenDetail(interior);
			ValidateCollisionAndClearance(bakery, interior);
			ValidatePerformanceIntent(interior);

			GD.Print("GLENS_BAKERY_INTERIOR_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"GLENS_BAKERY_INTERIOR_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateProductionComposition(Node3D bakery, Node3D interior)
	{
		Require(!bakery.HasNode("BuildingInteriorInstance"),
			"the four-piece runtime prototype has been replaced");
		foreach (string zone in new[]
		{
			"Architecture",
			"Signage",
			"Retail",
			"Kitchen",
			"Storage",
			"Abandonment",
			"Lighting",
			"Collision",
		})
			Require(interior.HasNode(zone), $"{zone} has a dedicated authored group");

		BoxMesh floor = (BoxMesh)interior.GetNode<MeshInstance3D>(
			"Architecture/Floor").Mesh;
		Require(IsNear(floor.Size.X, 5.76f) && IsNear(floor.Size.Z, 7.22f),
			"production floor fits the measured imported shell");
		Require(interior.GetNode("Architecture/CeilingGrid").GetChildCount() == 5,
			"period ceiling is articulated with an authored support grid");
	}

	private static void ValidateLicensedAssets(Node3D interior)
	{
		foreach (string path in new[]
		{
			"Retail/RegisterStation/CashRegister",
			"Retail/BreadShelves/ShelfFront",
			"Kitchen/PrepTable",
			"Kitchen/StoveLeft",
			"Kitchen/StoveRight",
			"Storage/SteelShelf01",
			"Lighting/LampRetail01",
		})
			Require(interior.HasNode(path), $"{path} uses the curated high-detail pack");

		Require(interior.GetNode("Retail/BreadShelves")
				.FindChildren("ShelfBread*", string.Empty, false, false).Count >= 8,
			"real pastry assets dress both retail shelves");
		Require(interior.GetNode("Storage")
				.FindChildren("Crate*", string.Empty, false, false).Count == 3,
			"existing licensed crates are reused in the rear store");
	}

	private static void ValidateRetailAndKitchenDetail(Node3D interior)
	{
		Node display = interior.GetNode("Retail/DisplayCounter");
		Require(display.GetChildCount() >= 20 &&
			display.FindChildren("DisplayCroissant*", string.Empty, false, false).Count == 4,
			"the glazed sales counter has framed cabinetry, trays, cakes, and pastries");
		Require(interior.GetNode("Kitchen").GetChildCount() >= 20,
			"the open kitchen includes prep, baking, sink, storage, and small equipment");
		Require(interior.GetNode("Abandonment").GetChildCount() >= 6,
			"papers, a fallen tray, and discarded packaging add restrained decay");

		OmniLight3D[] lights = interior.GetNode("Lighting").GetChildren()
			.OfType<OmniLight3D>().ToArray();
		Require(lights.Length == 4 &&
			lights.All(light => light.LightEnergy >= 0.6f && light.LightEnergy <= 1.0f),
			"four restrained warm fixtures keep the daytime interior readable");
		Require(lights.All(light => !light.ShadowEnabled),
			"small interior fixtures avoid costly and distracting dynamic shadows");
	}

	private static void ValidateCollisionAndClearance(Node3D bakery, Node3D interior)
	{
		StaticBody3D collision = interior.GetNode<StaticBody3D>("Collision");
		CollisionShape3D[] shapes = collision.GetChildren()
			.OfType<CollisionShape3D>().ToArray();
		Require(collision.CollisionLayer == 1 && collision.CollisionMask == 1,
			"interior uses the established environment collision layer");
		Require(shapes.Length == 6 &&
			shapes.All(shape => !shape.Disabled && shape.Shape is BoxShape3D),
			"only substantial furniture receives simplified active collision");

		Vector3[] entranceRoute =
		{
			new(1.75f, 1.5f, 0.6f),
			new(1.35f, 1.5f, 1.45f),
			new(0.72f, 1.5f, 1.75f),
			new(0.15f, 1.5f, 2.15f),
			new(-0.55f, 1.5f, 2.65f),
		};
		foreach (Vector3 point in entranceRoute)
			Require(!OverlapsFurniture(bakery, point, shapes),
				$"authored entrance route clears a player capsule at {point}");
	}

	private static bool OverlapsFurniture(
		Node3D bakery,
		Vector3 bakeryLocalPoint,
		IEnumerable<CollisionShape3D> shapes)
	{
		const float playerRadius = 0.35f;
		Vector3 globalPoint = bakery.ToGlobal(bakeryLocalPoint);
		foreach (CollisionShape3D collisionShape in shapes)
		{
			if (collisionShape.Name == "Floor" ||
				collisionShape.Shape is not BoxShape3D box)
				continue;

			Vector3 local = collisionShape.ToLocal(globalPoint);
			if (Mathf.Abs(local.X) <= box.Size.X * 0.5f + playerRadius &&
				Mathf.Abs(local.Z) <= box.Size.Z * 0.5f + playerRadius)
				return true;
		}
		return false;
	}

	private static void ValidatePerformanceIntent(Node3D interior)
	{
		int geometryCount = interior.FindChildren("*", string.Empty, true, false)
			.Count(node => node is GeometryInstance3D);
		Require(geometryCount >= 90 && geometryCount <= 220,
			"bakery detail density remains bounded for the Compatibility renderer");
		Require(interior.FindChildren("*", "CollisionObject3D", true, false).Count == 1,
			"small clutter and repeated food props do not receive wasteful collision");
	}

	private static bool IsNear(float value, float expected, float tolerance = 0.01f)
	{
		return Mathf.Abs(value - expected) <= tolerance;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
