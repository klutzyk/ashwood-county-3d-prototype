#nullable enable

using System;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetWorldPolishValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node3D polish = GD.Load<PackedScene>(
				"res://scenes/world/ashwood/presentation/main_street_world_polish.tscn")
				.Instantiate<Node3D>();
			AddChild(polish);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			MeshInstance3D terrain = polish.GetNode<MeshInstance3D>("DistantTerrain");
			Require(terrain.Mesh is ArrayMesh && terrain.Mesh.GetSurfaceCount() == 1,
				"county backdrop is generated as one batched terrain surface");
			Require(terrain.Mesh.GetAabb().Size.Y > 2.0f,
				"county backdrop has real rolling relief instead of a flat world edge");

			Godot.Collections.Array arrays = terrain.Mesh.SurfaceGetArrays(0);
			Vector3[] normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
			float averageNormalY = normals.Length > 0
				? normals.Average(normal => normal.Y)
				: -1.0f;
			Require(averageNormalY > 0.75f,
				"generated terrain normals face upward for stable lighting");
			Require(terrain.GetActiveMaterial(0) is StandardMaterial3D terrainMaterial &&
				terrainMaterial.AlbedoTexture is not null &&
				terrainMaterial.VertexColorUseAsAlbedo,
				"county terrain combines licensed ground texture with authored relief tinting");

			Node3D density = polish.GetNode<Node3D>("DensityPresentation");
			MeshInstance3D roadApproach = density.GetNode<MeshInstance3D>(
				"RoadApproaches/ApproachSurface");
			MultiMeshInstance3D approachDashes = density.GetNode<MultiMeshInstance3D>(
				"RoadApproaches/FadedCentreDashes");
			Require(roadApproach.Mesh is ArrayMesh &&
				roadApproach.Mesh.GetAabb().Size.X > 420.0f &&
				roadApproach.Mesh.GetAabb().Size.Y > 0.35f &&
				approachDashes.Multimesh?.InstanceCount == 16,
				"both road ends continue over graded terrain with batched depth-cue markings");

			Node3D frontage = density.GetNode<Node3D>("HistoricFrontage");
			MeshInstance3D[] frontageBatches = frontage.GetChildren()
				.OfType<MeshInstance3D>()
				.ToArray();
			Require(frontage.GetMeta("facade_lot_count").AsInt32() == 7 &&
				frontageBatches.Length == 7 &&
				frontageBatches.All(batch => batch.Mesh is ArrayMesh &&
					batch.GetMeta("batched_primitive_count").AsInt32() > 0),
				"seven historic gap lots collapse into seven material batches");
			Node3D facadeCollision = frontage.GetNode<Node3D>("FacadeCollision");
			Require(facadeCollision.GetChildCount() == 7 &&
				facadeCollision.GetChildren().All(child =>
					child is StaticBody3D body &&
					(body.CollisionLayer & 1u) != 0 &&
					body.GetNode<CollisionShape3D>("Collision").Shape is BoxShape3D),
				"accessible infill lots use one cheap solid box each");

			Node3D narrative = density.GetNode<Node3D>("NarrativeDressing");
			Require(narrative.GetNode<MultiMeshInstance3D>(
					"WindblownDeliveryPapers").Multimesh?.InstanceCount == 28 &&
				narrative.GetNode<MultiMeshInstance3D>(
					"FacadeAndShoulderWeeds").Multimesh?.InstanceCount == 36 &&
				narrative.HasNode("EastEvacuationSedan"),
				"narrative debris, overgrowth, and stalled vehicle create low-cost story groupings");

			Node3D forest = polish.GetNode<Node3D>("DistantForest");
			MultiMeshInstance3D[] forestBatches = forest.GetChildren()
				.OfType<MultiMeshInstance3D>()
				.ToArray();
			Require(forestBatches.Length is >= 4 and <= 8 &&
				forestBatches.Sum(batch => batch.Multimesh?.InstanceCount ?? 0) == 60,
				"seasonal county forest uses spatial batches containing 60 detailed trees");
			Require(forestBatches.All(batch => batch.Multimesh?.UseColors == true),
				"forest batches carry per-instance seasonal tint variation");
			Require(forestBatches.All(batch =>
				batch.Multimesh?.Mesh?.GetSurfaceCount() == 2 &&
				batch.GetMeta("render_strategy").AsString() ==
					"region_culled_detailed_midground"),
				"forest preserves detailed alpha-scissored trees only as regional midground breakup");
			Require(!forest.GetChildren().OfType<MeshInstance3D>().Any(),
				"forest edge avoids opaque ribbon walls that can read as black voids");
			Require(forestBatches.All(batch =>
				batch.Multimesh?.CustomAabb.HasVolume() == true &&
				batch.Position.Length() > 25.0f &&
				batch.VisibilityRangeEnd <= 220.0f),
				"forest batches have tight local AABBs and region-centred distance culling");

			MultiMeshInstance3D leaves = polish.GetNode<MultiMeshInstance3D>(
				"SeasonalGroundDetail/CurbLeafDrifts");
			Require(leaves.Multimesh?.InstanceCount == 160 &&
				leaves.VisibilityRangeEnd <= 36.0f,
				"curb leaf detail stays in one bounded MultiMesh");
			Require(polish.GetNode("MainStreetClock").HasNode("HourIndices") &&
				polish.GetNode("MainStreetClock").HasNode("CivicPlaques") &&
				!polish.GetNode("MainStreetClock").HasNode("TownMemorial"),
				"the civic clock preserves readable period detail without micro-prop draws");
			RequireMergedLayer(polish, "RoadWetness", 1);
			RequireMergedLayer(polish, "MainStreetClock", 3);
			RequireMergedLayer(polish, "WestIntersectionSignal", 4);
			Require(!ContainsCollision(density.GetNode("RoadApproaches")) &&
				!ContainsCollision(narrative),
				"road-end and story dressing behind the safety wall remain visual-only");

			GD.Print(
				$"MAIN_STREET_WORLD_POLISH_VALIDATION: PASS " +
				$"(average_normal_y={averageNormalY:F3})");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_WORLD_POLISH_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void RequireMergedLayer(
		Node3D polish,
		string nodePath,
		int expectedBatchCount)
	{
		Node3D layer = polish.GetNode<Node3D>(nodePath);
		MeshInstance3D[] directMeshes = layer.GetChildren()
			.OfType<MeshInstance3D>()
			.ToArray();
		MeshInstance3D[] merged = directMeshes
			.Where(instance => instance.Name.ToString().StartsWith("MergedStaticBatch"))
			.ToArray();
		MeshInstance3D[] sources = directMeshes
			.Where(instance => !instance.Name.ToString().StartsWith("MergedStaticBatch"))
			.ToArray();
		Require(merged.Length == expectedBatchCount &&
			merged.All(instance => instance.Mesh is ArrayMesh) &&
			sources.All(instance => !instance.Visible),
			$"{nodePath} static primitives collapse into {expectedBatchCount} material batches");
	}

	private static bool ContainsCollision(Node node)
	{
		if (node is CollisionObject3D or CollisionShape3D or CollisionPolygon3D)
		{
			return true;
		}

		return node.GetChildren().Any(ContainsCollision);
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
