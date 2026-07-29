#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodPoliceStationValidation : Node
{
	private readonly List<string> _failures = new();

	public override void _Ready()
	{
		CallDeferred(MethodName.RunValidation);
	}

	private void RunValidation()
	{
		try
		{
			PackedScene? packed = GD.Load<PackedScene>(
				"res://assets/environment/buildings/AshwoodPoliceStation/ashwood_police_station.tscn");
			Require(packed != null, "Police station PackedScene loads.");
			if (packed == null)
			{
				Finish();
				return;
			}

			Node3D station = packed.Instantiate<Node3D>();
			AddChild(station);

			ValidateStaticProductionScene(station);
			ValidateFootprintAndAreas(station);
			ValidateTexturedArchitecture(station);
			ValidateImportedDetail(station);
			ValidateCells(station);
			ValidateStairs(station);
			ValidateCollision(station);

			station.QueueFree();
		}
		catch (Exception exception)
		{
			_failures.Add($"Unhandled validation exception: {exception}");
		}

		Finish();
	}

	private void ValidateStaticProductionScene(Node3D station)
	{
		Variant script = station.GetScript();
		Require(script.VariantType == Variant.Type.Nil,
			"Production station is a baked static scene, not runtime-generated.");
		Require(station.HasMeta("_environment_art"), "Environment-art authorship metadata exists.");
		Require(station.HasMeta("_facade_local_x"), "Facade placement metadata exists.");
		Require(Mathf.IsEqualApprox(station.GetMeta("_facade_local_x").AsSingle(), -9.0f),
			"Public facade is authored at local X = -9.");
		Require(Mathf.IsEqualApprox(station.GetMeta("_basement_floor_y").AsSingle(), -3.6f),
			"True basement floor is authored at Y = -3.6.");
	}

	private void ValidateFootprintAndAreas(Node3D station)
	{
		Node? authored = station.GetNodeOrNull("AuthoredEnvironment");
		Require(authored != null, "Static AuthoredEnvironment hierarchy exists.");
		if (authored == null)
		{
			return;
		}

		foreach (string path in new[]
		{
			"Exterior",
			"MainFloor/LobbyReceptionWaiting",
			"MainFloor/LobbyReceptionWaiting/Reception",
			"MainFloor/LobbyReceptionWaiting/WaitingArea",
			"MainFloor/OpenOffices",
			"MainFloor/ChiefOffice",
			"MainFloor/InterviewRoom",
			"MainFloor/EvidenceRoom",
			"MainFloor/Armory",
			"MainFloor/Garage",
			"MainFloor/Bathroom",
			"MainFloor/BasementStairwell",
			"Basement/SecureCorridor",
			"Basement/BookingArea",
			"Basement/BasementEvidenceStorage",
			"Basement/PrisonCells",
			"Lighting",
		})
		{
			Require(authored.GetNodeOrNull(path) != null, $"Required authored area exists: {path}");
		}

		Node3D? southWall = authored.GetNodeOrNull<Node3D>("Exterior/SouthSideWall");
		Node3D? northWall = authored.GetNodeOrNull<Node3D>("Exterior/NorthSideWall");
		Node3D? rearWall = authored.GetNodeOrNull<Node3D>("Exterior/RearWall");
		Require(southWall != null && northWall != null &&
			Mathf.Abs(northWall.Position.Z - southWall.Position.Z) >= 20.9f,
			"Station frontage is approximately 21 metres.");
		Require(rearWall != null && Mathf.IsEqualApprox(rearWall.Position.X, 9.0f),
			"Station depth runs from facade X=-9 to rear X=9.");

		Node? frontEntrance = station.GetNodeOrNull("FrontEntrance");
		Require(frontEntrance != null && frontEntrance.GetChildCount() == 2,
			"Station has a civic double-door front entrance.");
		Require(FindByName(station, "BathroomDoor") != null,
			"Bathroom has a dedicated imported door.");
		Require(FindByName(station, "ArmorySecurityDoor") != null,
			"Armory has a dedicated security door.");
	}

	private void ValidateTexturedArchitecture(Node3D station)
	{
		int checkedWalls = 0;
		foreach (Node node in Descendants(station))
		{
			if (node is not StaticBody3D body)
			{
				continue;
			}

			string name = body.Name.ToString();
			if (!name.Contains("Wall", StringComparison.OrdinalIgnoreCase) &&
				!name.Contains("Partition", StringComparison.OrdinalIgnoreCase) &&
				!name.Contains("Liner", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			MeshInstance3D? visual = body.GetNodeOrNull<MeshInstance3D>("TexturedVisual");
			Require(visual?.Mesh != null, $"{name} has a textured visual mesh.");
			if (visual?.Mesh == null)
			{
				continue;
			}

			Material? surfaceMaterial = visual.Mesh.SurfaceGetMaterial(0);
			Require(surfaceMaterial is StandardMaterial3D,
				$"{name} uses a StandardMaterial3D PBR material.");
			if (surfaceMaterial is StandardMaterial3D pbr)
			{
				Require(pbr.AlbedoTexture != null, $"{name} has an albedo texture.");
				Require(pbr.NormalEnabled && pbr.NormalTexture != null,
					$"{name} has normal-mapped surface detail.");
			}
			checkedWalls++;
		}

		Require(checkedWalls >= 22,
			$"At least 22 individually authored textured wall/partition pieces exist (found {checkedWalls}).");

		string[] materialPaths =
		{
			"res://assets/materials/ashwood_police_exterior_brick.tres",
			"res://assets/materials/ashwood_police_wall_plaster.tres",
			"res://assets/materials/ashwood_police_floor_linoleum.tres",
			"res://assets/materials/ashwood_police_basement_concrete.tres",
			"res://assets/materials/ashwood_police_rusty_metal.tres",
			"res://assets/materials/ashwood_police_bathroom_tile.tres",
		};
		foreach (string materialPath in materialPaths)
		{
			StandardMaterial3D? material = GD.Load<StandardMaterial3D>(materialPath);
			Require(material?.AlbedoTexture != null && material.NormalEnabled &&
				material.NormalTexture != null,
				$"PBR material has albedo and normal textures: {materialPath.GetFile()}");
		}
	}

	private void ValidateImportedDetail(Node3D station)
	{
		int importedCount = Descendants(station).Count(node => node.HasMeta("source_path"));
		Require(importedCount >= 85,
			$"Dense set dressing instances at least 85 imported assets (found {importedCount}).");

		foreach (string name in new[]
		{
			"DeskRadio",
			"PublicMegaphone",
			"ChiefCaseFile",
			"InterviewRecorder",
			"EvidenceGasMask",
			"EvidenceFlashlight",
			"AmmoBox1",
			"RiotGasMask",
			"CorridorCamera",
			"BookingRadio",
			"GarageStorageShelf",
			"BathroomToilet",
			"BathroomSink",
		})
		{
			Node? prop = FindByName(station, name);
			Require(prop != null && prop.HasMeta("source_path"),
				$"Relevant imported detail prop exists: {name}");
			if (prop != null && prop.HasMeta("source_path"))
			{
				string source = prop.GetMeta("source_path").AsString();
				Require(ResourceLoader.Exists(source), $"{name} source resource exists.");
			}
		}

		int fluorescentCount = Descendants(station)
			.Count(node => node.Name.ToString().Contains("Fluorescent", StringComparison.Ordinal));
		Require(fluorescentCount >= 16,
			$"Station has extensive physical fluorescent fixtures (found {fluorescentCount}).");
	}

	private void ValidateCells(Node3D station)
	{
		Node? prisonCells = station.GetNodeOrNull(
			"AuthoredEnvironment/Basement/PrisonCells");
		Require(prisonCells != null, "PrisonCells hierarchy exists.");
		if (prisonCells == null)
		{
			return;
		}

		List<Node> cells = prisonCells.GetChildren()
			.Where(child => child.Name.ToString().StartsWith("Cell", StringComparison.Ordinal))
			.ToList();
		Require(cells.Count == 2, $"Exactly two prison cells exist (found {cells.Count}).");
		Require(cells.Select(cell => cell.Name.ToString()).OrderBy(name => name)
			.SequenceEqual(new[] { "Cell01", "Cell02" }),
			"Cells are explicitly named Cell01 and Cell02.");

		foreach (Node cell in cells)
		{
			foreach (string path in new[]
			{
				"BarredFront",
				"OpenBarredCellDoor",
				"Bunk",
				"Bunk/LowerMattress",
				"Bunk/UpperMattress",
				"CellToilet",
				"CellSink",
				"CellCamera",
				"SanitaryFixtureCollision",
			})
			{
				Require(cell.GetNodeOrNull(path) != null,
					$"{cell.Name} is fully furnished: {path}");
			}

			Node? barredFront = cell.GetNodeOrNull("BarredFront");
			int remainingBars = barredFront?.GetChildren()
				.Count(child => child.Name.ToString().StartsWith("VerticalBar", StringComparison.Ordinal)) ?? 0;
			Require(remainingBars == 9,
				$"{cell.Name} has nine structural bars plus a traversable door opening (found {remainingBars}).");
			Require(cell.GetNodeOrNull("OpenBarredCellDoor")?.GetChildCount() >= 7,
				$"{cell.Name} has a detailed visibly open barred door leaf.");
		}
	}

	private void ValidateStairs(Node3D station)
	{
		Node? stairwell = station.GetNodeOrNull(
			"AuthoredEnvironment/MainFloor/BasementStairwell");
		Require(stairwell != null, "Basement stairwell exists.");
		if (stairwell == null)
		{
			return;
		}

		List<StaticBody3D> steps = stairwell.GetChildren()
			.OfType<StaticBody3D>()
			.Where(node => node.Name.ToString().StartsWith("VisibleStep_", StringComparison.Ordinal))
			.OrderBy(node => node.Name.ToString())
			.ToList();
		Require(steps.Count == 19, $"Stair has 19 visible normal-human steps (found {steps.Count}).");

		float? previousTop = null;
		foreach (StaticBody3D step in steps)
		{
			MeshInstance3D? visual = step.GetNodeOrNull<MeshInstance3D>("TexturedVisual");
			BoxMesh? mesh = visual?.Mesh as BoxMesh;
			Require(mesh != null, $"{step.Name} is a materially detailed stair tread.");
			if (mesh == null)
			{
				continue;
			}

			float top = step.Position.Y + (mesh.Size.Y * 0.5f);
			if (previousTop.HasValue)
			{
				Require(Mathf.IsEqualApprox(previousTop.Value - top, 0.18f),
					$"{step.Name} uses a 0.18m riser.");
			}
			Require(mesh.Size.X >= 0.28f && mesh.Size.X <= 0.32f,
				$"{step.Name} uses a 0.28-0.32m tread.");
			previousTop = top;
		}

		StaticBody3D? ramp = stairwell.GetNodeOrNull<StaticBody3D>("InvisibleWalkableStairRamp");
		CollisionShape3D? rampCollision = ramp?.GetNodeOrNull<CollisionShape3D>("Collision");
		Require(rampCollision?.Shape is BoxShape3D, "Invisible walkable stair ramp has collision.");
		if (ramp != null && rampCollision?.Shape is BoxShape3D rampShape)
		{
			float angleDegrees = Mathf.Abs(Mathf.RadToDeg(ramp.Rotation.Z));
			Require(angleDegrees > 29.0f && angleDegrees < 35.0f,
				$"Invisible ramp slope is walkable ({angleDegrees:0.0} degrees).");
			Require(rampShape.Size.X > 6.5f && rampShape.Size.Z > 2.0f,
				"Invisible ramp spans the full stair run and player width.");
		}

		int railPosts = stairwell.GetChildren()
			.Count(node => node.Name.ToString().StartsWith("RailPost_", StringComparison.Ordinal));
		Require(railPosts == 10, "Stair has detailed handrails on both sides.");
	}

	private void ValidateCollision(Node3D station)
	{
		int staticBodyCount = Descendants(station).OfType<StaticBody3D>().Count();
		int collisionCount = Descendants(station).OfType<CollisionShape3D>()
			.Count(shape => shape.Shape != null);
		Require(staticBodyCount >= 120,
			$"Detailed station has extensive static structure/collision bodies (found {staticBodyCount}).");
		Require(collisionCount >= 115,
			$"Detailed station has extensive valid collision shapes (found {collisionCount}).");

		foreach (string floorPath in new[]
		{
			"AuthoredEnvironment/MainFloor/Architecture/MainFloorCenter/Collision",
			"AuthoredEnvironment/MainFloor/Architecture/MainFloorStairFront/Collision",
			"AuthoredEnvironment/MainFloor/Architecture/MainFloorStairRear/Collision",
			"AuthoredEnvironment/Basement/Architecture/BasementFloor/Collision",
		})
		{
			CollisionShape3D? floor = station.GetNodeOrNull<CollisionShape3D>(floorPath);
			Require(floor?.Shape is BoxShape3D, $"Navigable floor collision exists: {floorPath.GetFile()}");
		}
	}

	private static Node? FindByName(Node root, string name)
	{
		return Descendants(root).FirstOrDefault(node => node.Name == name);
	}

	private static IEnumerable<Node> Descendants(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			yield return child;
			foreach (Node descendant in Descendants(child))
			{
				yield return descendant;
			}
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		if (_failures.Count == 0)
		{
			GD.Print("ASHWOOD_POLICE_STATION_VALIDATION:PASS");
			GetTree().Quit(0);
			return;
		}

		foreach (string failure in _failures)
		{
			GD.PushError($"ASHWOOD_POLICE_STATION_VALIDATION:{failure}");
		}
		GD.Print($"ASHWOOD_POLICE_STATION_VALIDATION:FAIL:{_failures.Count}");
		GetTree().Quit(1);
	}
}
