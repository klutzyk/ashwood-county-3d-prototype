#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MillerHardwareValidation : Node
{
	private const string MillerScenePath =
		"res://assets/environment/buildings/MillerHardware/miller_hardware.tscn";

	public override async void _Ready()
	{
		try
		{
			Node3D miller = GD.Load<PackedScene>(MillerScenePath)
				.Instantiate<Node3D>();
			AddChild(miller);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ValidateAssembly(miller);
			ValidateExterior(miller);
			ValidateInteriorArchitecture(miller);
			ValidateRetailLayout(miller);
			ValidateProductionFixtures(miller);
			ValidateMerchandise(miller);
			ValidatePbrSurfaces(miller);
			ValidateCollisionAndLighting(miller);
			await ValidateEntrance(miller);

			GD.Print("MILLER_HARDWARE_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"MILLER_HARDWARE_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateAssembly(Node3D miller)
	{
		Require(miller.Name == "MillerHardware",
			"production wrapper carries the Miller Hardware identity");
		Require(miller.GetNode("Exterior") is StaticBody3D &&
			miller.GetNode("Interior") is StaticBody3D &&
			miller.GetNode("FrontDoor") is DoorController,
			"focused exterior, interior, and interactive entrance are assembled");
		Require(miller.GetNode<Node3D>("Exterior").SceneFilePath.EndsWith(
				"/MillerHardware/exterior.tscn", StringComparison.Ordinal) &&
			miller.GetNode<Node3D>("Interior").SceneFilePath.EndsWith(
				"/MillerHardware/interior.tscn", StringComparison.Ordinal),
			"production scenes remain independently authorable");
	}

	private static void ValidateExterior(Node3D miller)
	{
		StaticBody3D exterior = miller.GetNode<StaticBody3D>("Exterior");
		Aabb bounds = GetCombinedBounds(exterior.GetNode("Shell"));
		Require(bounds.Size.X >= 14.4f &&
			bounds.Size.Z >= 18.4f &&
			bounds.Size.Y >= 6.0f,
			"historic shell supplies the requested large 14 by 18 metre footprint");
		Require(exterior.GetNode<Label3D>("Identity/Name").Text ==
			"MILLER HARDWARE",
			"facade carries the final business identity");
		Require(exterior.GetNode("Storefront").GetChildren()
			.OfType<MeshInstance3D>().Count() >= 7,
			"street facade has a broad multi-bay glazed storefront");
		Require(exterior.HasNode("Canopy/WeatheredMetal") &&
			exterior.GetNode("Canopy").GetChildCount() >= 6,
			"period metal canopy has edge construction and authored ribs");
		Require(exterior.HasNode("RearLoading/LoadingDoor") &&
			exterior.HasNode("RearLoading/LoadingDoorSlatE") &&
			exterior.HasNode("RearLoading/RearLamp"),
			"rear elevation includes a dressed warehouse loading door");

		IEnumerable<MeshInstance3D> glazing = exterior.GetNode("Storefront")
			.GetChildren()
			.OfType<MeshInstance3D>()
			.Where(mesh => mesh.Name.ToString().Contains(
				"Window", StringComparison.Ordinal));
		Require(glazing.Count() >= 6 &&
			glazing.All(window => window.Position.X <= -7.10f),
			"windows remain aligned to the required local front plane at X equals -7");

		int collisionCount = exterior.FindChildren(
			"*", "CollisionShape3D", true, false).Count;
		Require(collisionCount >= 15 && collisionCount <= 18,
			"exterior uses doorway-aware major collision without per-trim shapes");
	}

	private static void ValidateInteriorArchitecture(Node3D miller)
	{
		StaticBody3D interior = miller.GetNode<StaticBody3D>("Interior");
		BoxShape3D floor = (BoxShape3D)interior
			.GetNode<CollisionShape3D>("FloorCollision").Shape;
		Require(floor.Size.X >= 13.7f && floor.Size.Z >= 17.7f,
			"interior retains the full spacious sales footprint");
		Require(interior.HasNode("Architecture/RearPartitionNorth") &&
			interior.HasNode("Architecture/RearPartitionMiddle") &&
			interior.HasNode("Architecture/RearPartitionSouth") &&
			interior.HasNode("Architecture/BathroomDivider"),
			"rear service band is constructed from complete full-height walls");
		Require(interior.HasNode("Architecture/BathroomRearTile") &&
			interior.HasNode("Architecture/BathroomSouthTile") &&
			interior.HasNode("Architecture/BathroomFrontTile"),
			"bathroom receives continuous authored tile wall liners");
		Node3D bathroomDoor = interior.GetNode<Node3D>(
			"Architecture/BathroomDoor");
		Require(bathroomDoor.SceneFilePath.EndsWith(
				"/white_door_1.glb", StringComparison.Ordinal),
			"bathroom uses a full imported textured door");
		Require(bathroomDoor.Scale.X >= 0.22f &&
			bathroomDoor.Position.Y <= 0.1f &&
			interior.HasNode("Architecture/BathroomDoorHeader") &&
			interior.HasNode("Architecture/WarehouseDoorHeader"),
			"warehouse and bathroom doorways have grounded full-size leaves and wall headers");
		Require(interior.HasNode("Bathroom/Toilet") &&
			interior.HasNode("Bathroom/Sink") &&
			interior.HasNode("Bathroom/Mirror"),
			"enclosed bathroom is furnished with authored sanitary fixtures");
	}

	private static void ValidateRetailLayout(Node3D miller)
	{
		StaticBody3D interior = miller.GetNode<StaticBody3D>("Interior");
		Node3D[] aisles =
		{
			interior.GetNode<Node3D>("SalesFloorZones/AisleTools"),
			interior.GetNode<Node3D>("SalesFloorZones/AisleFasteners"),
			interior.GetNode<Node3D>("SalesFloorZones/AisleGeneral"),
		};
		string[] expectedAisleFixtures =
		{
			"/fixtures/miller_gondola_aisle.glb",
			"/fixtures/miller_gondola_fasteners.glb",
			"/fixtures/miller_gondola_general.glb",
		};
		Require(aisles.Select(aisle => expectedAisleFixtures.Any(path =>
				aisle.SceneFilePath.EndsWith(path, StringComparison.Ordinal)))
			.All(matched => matched) &&
			aisles.Select(aisle => aisle.SceneFilePath).Distinct().Count() == 3,
			"three complete sales aisles use distinct, department-stocked double-sided fixtures");
		Require(aisles.All(aisle => aisle.FindChildren(
				"*", "MeshInstance3D", true, false).Count >= 7),
			"each gondola consolidates dense packaged stock into a performant multi-material fixture");
		Require(aisles.Zip(aisles.Skip(1), (first, second) =>
				Mathf.Abs(second.Position.Z - first.Position.Z))
			.All(spacing => spacing >= 3.25f),
			"aisles preserve broad 1.8 metre clear passages");

		float aisleHalfLength = 3.82f * 0.5f;
		float frontLoop = (aisles[0].Position.X - aisleHalfLength) - (-3.05f);
		float rearLoop = 3.55f - (aisles[0].Position.X + aisleHalfLength);
		Require(frontLoop >= 1.20f && rearLoop >= 1.45f,
			"gondolas keep walkable loops around both ends");

		Vector3 entrance = new(-7.0f, 0, -6.65f);
		Node3D counter = interior.GetNode<Node3D>("CheckoutZone/Counter");
		Require(Mathf.Abs(entrance.Z - counter.Position.Z) >= 1.25f,
			"checkout remains convenient without obstructing the entry lane");
		Require(interior.HasNode("ToolWall/Pegboard") &&
			interior.HasNode("ToolWall/FastenerBins") &&
			interior.HasNode("PaintZone/Display") &&
			interior.HasNode("PlumbingZone/Rack") &&
			interior.HasNode("Warehouse/LumberRack"),
			"sales floor and warehouse contain distinct tools, fasteners, paint, plumbing, and lumber zones");
		Require(interior.GetNode("Warehouse").GetChildCount() >= 12,
			"rear warehouse is fully dressed rather than an empty room");
	}

	private static void ValidateProductionFixtures(Node3D miller)
	{
		StaticBody3D interior = miller.GetNode<StaticBody3D>("Interior");
		string[] fixturePaths =
		{
			"SalesFloorZones/AisleTools",
			"SalesFloorZones/AisleFasteners",
			"SalesFloorZones/AisleGeneral",
			"CheckoutZone/Counter",
			"ToolWall/Pegboard",
			"ToolWall/FastenerBins",
			"PaintZone/Display",
			"PlumbingZone/Rack",
			"Warehouse/LumberRack",
		};
		foreach (string path in fixturePaths)
		{
			Node3D fixture = interior.GetNode<Node3D>(path);
			Require(fixture.SceneFilePath.Contains(
					"/MillerHardware/fixtures/miller_",
					StringComparison.Ordinal),
				$"{path} uses the project-owned PBR fixture pack");
			Require(fixture.FindChildren(
					"*", "MeshInstance3D", true, false).Count >= 4,
				$"{path} has detailed multi-material geometry");
		}

		Aabb pegboard = GetCombinedBounds(interior.GetNode("ToolWall/Pegboard"));
		Require(pegboard.Size.X >= 4.2f &&
			pegboard.Size.Y >= 2.7f &&
			pegboard.Size.Z >= 0.55f,
			"pegboard wall is full scale with actual holes, hooks, and cabinetry");
		Aabb paint = GetCombinedBounds(interior.GetNode("PaintZone/Display"));
		Require(paint.Size.X >= 3.5f && paint.Size.Y >= 2.5f,
			"paint display is a substantial stocked retail fixture");
	}

	private static void ValidateMerchandise(Node3D miller)
	{
		StaticBody3D interior = miller.GetNode<StaticBody3D>("Interior");
		Require(interior.GetNode("AisleMerchandise").GetChildCount() >= 15,
			"three aisles carry varied, deliberately placed tool merchandise");
		Require(interior.GetNode("ToolWall").GetChildCount() >= 7,
			"hero tool wall mixes the authored fixture and imported tools");
		Require(interior.GetNode("WindowDisplay").GetChildCount() >= 5,
			"street windows carry a composed hardware vignette");
		Require(interior.GetNode("Abandonment").GetChildCount() >= 5,
			"store contains restrained hand-placed abandonment clutter");

		Node[] heroTools =
		{
			interior.GetNode("ToolWall/WallDrill"),
			interior.GetNode("ToolWall/WallWrench"),
			interior.GetNode("ToolWall/WallHacksaw"),
			interior.GetNode("ToolWall/WallCrowbar"),
			interior.GetNode("ToolWall/WallScrewdrivers"),
			interior.GetNode("Warehouse/Ladder"),
			interior.GetNode("Warehouse/JerrycanA"),
		};
		Require(heroTools.All(tool =>
				tool.SceneFilePath.Contains(
					"/miller_hardware/poly_haven/",
					StringComparison.Ordinal)),
			"hero merchandise uses the verified local CC0 Poly Haven pack");

		int externalModelRoots = interior.FindChildren(
				"*", "Node3D", true, false)
			.OfType<Node3D>()
			.Count(node =>
				node.SceneFilePath.EndsWith(".gltf", StringComparison.Ordinal) ||
				node.SceneFilePath.EndsWith(".glb", StringComparison.Ordinal));
		Require(externalModelRoots >= 45,
			"content density comes from instanced detailed assets rather than primitive placeholders");
		Require(interior.FindChildren(
				"*", "MeshInstance3D", true, false).Count >= 90,
			"interior reaches production prop and fixture density");
	}

	private static void ValidatePbrSurfaces(Node3D miller)
	{
		StaticBody3D exterior = miller.GetNode<StaticBody3D>("Exterior");
		StaticBody3D interior = miller.GetNode<StaticBody3D>("Interior");
		StandardMaterial3D brick = GetMaterial(
			exterior.GetNode<MeshInstance3D>("Shell/RearWall"));
		StandardMaterial3D floor = GetMaterial(
			interior.GetNode<MeshInstance3D>("Architecture/SalesFloor"));
		StandardMaterial3D wall = GetMaterial(
			interior.GetNode<MeshInstance3D>("Architecture/NorthWallLiner"));
		StandardMaterial3D ceiling = GetMaterial(
			interior.GetNode<MeshInstance3D>("Architecture/Ceiling"));

		foreach ((StandardMaterial3D material, string label) in new[]
		{
			(brick, "exterior brick"),
			(floor, "concrete floor"),
			(wall, "interior wall"),
			(ceiling, "interior ceiling"),
		})
		{
			Require(material.AlbedoTexture is not null &&
				material.NormalEnabled &&
				material.NormalTexture is not null,
				$"{label} uses authored albedo and normal PBR maps");
		}
		Require(
			floor.Get("ao_texture").AsGodotObject() is Texture2D
			&& brick.Get("ao_texture").AsGodotObject() is Texture2D
			&& wall.Get("ao_texture").AsGodotObject() is Texture2D,
			"major surfaces retain ambient-occlusion material detail");
	}

	private static void ValidateCollisionAndLighting(Node3D miller)
	{
		StaticBody3D interior = miller.GetNode<StaticBody3D>("Interior");
		int collisionCount = interior.FindChildren(
			"*", "CollisionShape3D", true, false).Count;
		Require(collisionCount >= 17 && collisionCount <= 21,
			"only architecture and substantial fixtures receive simple collision");

		OmniLight3D[] lights = interior.FindChildren(
				"*", "OmniLight3D", true, false)
			.OfType<OmniLight3D>()
			.ToArray();
		Require(lights.Length == 6 &&
			lights.Count(light => light.ShadowEnabled) == 1,
			"daytime interior uses six restrained fills with one shadow caster");
		Require(interior.GetNode("CeilingFixtures").GetChildCount() == 7,
			"visible fluorescent fixtures cover retail, warehouse, and bathroom zones");
	}

	private async System.Threading.Tasks.Task ValidateEntrance(Node3D miller)
	{
		DoorController door = miller.GetNode<DoorController>("FrontDoor");
		Require(IsNear(door.Position.X, -7.11f) &&
			IsNear(door.Position.Z, -7.19f),
			"entrance sits on the required local street facade");
		CollisionShape3D doorLeaf = door.GetNode<CollisionShape3D>(
			"Hinge/DoorBody/CollisionShape3D");
		Require(IsNear(
				door.Position.Z + doorLeaf.Position.Z,
				-6.69f,
				0.04f),
			"door leaf is centred in the authored storefront opening");
		Require(door.HasNode("Hinge/DoorBody/DoorModel") &&
			door.HasNode("Hinge/DoorBody/CollisionShape3D") &&
			door.HasNode("Interactable"),
			"entrance uses imported geometry, moving collision, and interaction");
		float closedRotation = door.GetNode<Node3D>("Hinge").Rotation.Y;
		door.AnimationDuration = 0.01f;
		door.ToggleDoor();
		await ToSignal(GetTree().CreateTimer(0.05),
			SceneTreeTimer.SignalName.Timeout);
		Require(door.IsOpen && !door.IsAnimating,
			"front door completes its opening state");
		Require(IsNear(
				Mathf.RadToDeg(
					door.GetNode<Node3D>("Hinge").Rotation.Y - closedRotation),
				-105.0f,
				1.0f),
			"front door swings clear of the entry aisle");
	}

	private static StandardMaterial3D GetMaterial(MeshInstance3D mesh)
	{
		Material? material = mesh.MaterialOverride;
		if (material is null && mesh.Mesh is PrimitiveMesh primitive)
			material = primitive.Material;
		if (material is null && mesh.Mesh.GetSurfaceCount() > 0)
			material = mesh.Mesh.SurfaceGetMaterial(0);
		Require(material is StandardMaterial3D,
			$"{mesh.GetPath()} resolves to a StandardMaterial3D");
		return (StandardMaterial3D)material!;
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
		float tolerance = 0.025f)
	{
		return Mathf.Abs(value - expected) <= tolerance;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
			throw new InvalidOperationException(message);
	}
}
