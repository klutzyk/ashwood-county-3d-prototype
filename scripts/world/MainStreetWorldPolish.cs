#nullable enable

using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Builds non-interactive county context around Main Street. The generated
/// terrain, forest, leaf drift, and landmark details carry no collision or
/// processing after creation and keep repeated geometry in MultiMeshes.
/// </summary>
public partial class MainStreetWorldPolish : Node3D
{
	[Export] public PackedScene? TreeSceneA { get; set; }
	[Export] public PackedScene? TreeSceneB { get; set; }
	[Export(PropertyHint.Range, "0,80,1")]
	public int EndForestTreeCount { get; set; } = 17;
	[Export(PropertyHint.Range, "0,120,1")]
	public int SideForestTreeCount { get; set; } = 13;
	[Export(PropertyHint.Range, "0,400,1")]
	public int FallenLeafCount { get; set; } = 160;
	[Export] public uint LayoutSeed { get; set; } = 1952;

	private enum ForestRegion
	{
		WestEnd,
		EastEnd,
		NorthSide,
		SouthSide,
	}

	private readonly record struct ForestInstance(
		Transform3D Transform,
		Color Tint);

	private readonly record struct StaticMeshMergeKey(
		Rid MaterialRid,
		GeometryInstance3D.ShadowCastingSetting ShadowMode);

	private readonly record struct StaticMeshMergeSource(
		MeshInstance3D Instance,
		Material Material);

	private readonly Dictionary<ForestRegion, List<ForestInstance>>
		_treeInstancesA = new();
	private readonly Dictionary<ForestRegion, List<ForestInstance>>
		_treeInstancesB = new();

	private static readonly Color[] ForestPalette =
	{
		new(0.78f, 0.91f, 0.72f),
		new(0.9f, 0.9f, 0.61f),
		new(1.0f, 0.72f, 0.39f),
		new(0.92f, 0.55f, 0.31f),
		new(0.69f, 0.82f, 0.64f),
	};

	private static readonly Color[] LeafPalette =
	{
		new(0.48f, 0.25f, 0.075f),
		new(0.62f, 0.4f, 0.095f),
		new(0.37f, 0.16f, 0.055f),
		new(0.55f, 0.47f, 0.23f),
		new(0.27f, 0.2f, 0.13f),
	};

	public override void _Ready()
	{
		BuildRollingCountyTerrain();
		BuildDistantForest();
		BuildSeasonalLeafDrifts();
		BuildClockAndSignalDetails();
		MergeStaticMeshesByMaterial("RoadWetness", 105.0f);
		MergeStaticMeshesByMaterial("MainStreetClock", 150.0f);
		MergeStaticMeshesByMaterial("WestIntersectionSignal", 165.0f);
	}

	private void BuildRollingCountyTerrain()
	{
		MeshInstance3D? terrain = GetNodeOrNull<MeshInstance3D>("DistantTerrain");
		if (terrain is null)
		{
			GD.PushWarning($"{Name}: DistantTerrain is missing.");
			return;
		}

		SurfaceTool surfaceTool = new();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);

		const int columns = 65;
		const int rows = 41;
		const float width = 460.0f;
		const float depth = 250.0f;
		for (int row = 0; row < rows; row++)
		{
			float z = Mathf.Lerp(-depth * 0.5f, depth * 0.5f, row / (rows - 1.0f));
			for (int column = 0; column < columns; column++)
			{
				float x = Mathf.Lerp(-width * 0.5f, width * 0.5f,
					column / (columns - 1.0f));
				float height = SampleBackdropHeight(x, z);
				float edge = SampleEdgeStrength(x, z);
				float colorNoise = 0.5f + (0.5f * Mathf.Sin(
					(x * 0.071f) + (z * 0.047f)));
				float heightTint = Mathf.Clamp(height / 5.0f, 0.0f, 1.0f);
				Color tint = new Color(0.68f, 0.73f, 0.57f).Lerp(
					new Color(0.47f, 0.57f, 0.39f),
					Mathf.Clamp(
						(edge * 0.52f) + (heightTint * 0.26f) +
						(colorNoise * 0.09f),
						0.0f,
						1.0f));

				surfaceTool.SetColor(tint);
				surfaceTool.SetUV(new Vector2(x * 0.035f, z * 0.035f));
				surfaceTool.AddVertex(new Vector3(x, -0.26f + height, z));
			}
		}

		for (int row = 0; row < rows - 1; row++)
		{
			for (int column = 0; column < columns - 1; column++)
			{
				float cellX = Mathf.Lerp(-width * 0.5f, width * 0.5f,
					(column + 0.5f) / (columns - 1.0f));
				float cellZ = Mathf.Lerp(-depth * 0.5f, depth * 0.5f,
					(row + 0.5f) / (rows - 1.0f));
				// The authored playable ground already covers this footprint. Leaving
				// a hole prevents invisible triplanar overdraw under the whole town.
				if (Mathf.Abs(cellX) < 110.0f && Mathf.Abs(cellZ) < 40.0f)
				{
					continue;
				}

				int a = (row * columns) + column;
				int b = a + 1;
				int c = ((row + 1) * columns) + column;
				int d = c + 1;
				surfaceTool.AddIndex(a);
				surfaceTool.AddIndex(c);
				surfaceTool.AddIndex(b);
				surfaceTool.AddIndex(b);
				surfaceTool.AddIndex(c);
				surfaceTool.AddIndex(d);
			}
		}

		// SurfaceTool's clockwise front-face convention gives this indexed grid
		// downward normals unless explicitly flipped.
		surfaceTool.GenerateNormals(flip: true);
		ArrayMesh mesh = surfaceTool.Commit();
		StandardMaterial3D terrainMaterial =
			terrain.GetActiveMaterial(0)?.Duplicate() as StandardMaterial3D ?? new();
		terrainMaterial.AlbedoColor = Colors.White;
		terrainMaterial.Roughness = 1.0f;
		terrainMaterial.VertexColorUseAsAlbedo = true;
		// Textured, vertex-tinted unshaded terrain retains atmospheric colour in
		// Compatibility without bringing back the near-black ridge faces removed
		// during the previous visual/performance pass.
		terrainMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
		mesh.SurfaceSetMaterial(0, terrainMaterial);

		terrain.Mesh = mesh;
		terrain.SetMeta("environment_role", "rolling_non_playable_county_topography");
	}

	private static float SampleBackdropHeight(float x, float z)
	{
		float edge = SampleEdgeStrength(x, z);
		float broadRoll =
			(Mathf.Sin((x * 0.031f) + 0.8f) * 0.72f) +
			(Mathf.Sin((z * 0.054f) - 1.4f) * 0.46f) +
			(Mathf.Sin(((x + z) * 0.018f) + 2.2f) * 0.58f);
		float localRelief =
			(Mathf.Sin((x * 0.11f) - (z * 0.067f)) * 0.16f) +
			(Mathf.Sin(((x - z) * 0.073f) + 0.35f) * 0.11f);
		return Mathf.Max(0.0f,
			edge * (2.35f + (broadRoll * 0.68f) + (edge * 1.18f) + localRelief));
	}

	private static float SampleEdgeStrength(float x, float z)
	{
		float edgeX = SmoothStep(Mathf.InverseLerp(116.0f, 218.0f, Mathf.Abs(x)));
		float roadCorridor = SmoothStep(Mathf.InverseLerp(10.0f, 34.0f, Mathf.Abs(z)));
		// Maintain a shallow valley along the road while still rising gradually at
		// the county edge. A zero multiplier created the previous abrupt flat cut.
		edgeX *= Mathf.Lerp(0.26f, 1.0f, roadCorridor);
		float edgeZ = SmoothStep(Mathf.InverseLerp(40.0f, 122.0f, Mathf.Abs(z)));
		return Mathf.Max(edgeX, edgeZ);
	}

	private static float SmoothStep(float value)
	{
		float clamped = Mathf.Clamp(value, 0.0f, 1.0f);
		return clamped * clamped * (3.0f - (2.0f * clamped));
	}

	private void BuildDistantForest()
	{
		if (TreeSceneA is null || TreeSceneB is null)
		{
			GD.PushWarning($"{Name}: distant forest tree scenes are not assigned.");
			return;
		}

		_treeInstancesA.Clear();
		_treeInstancesB.Clear();
		RandomNumberGenerator random = new() { Seed = LayoutSeed };
		AddRoadEndForest(
			random, 1.0f, EndForestTreeCount, ForestRegion.EastEnd);
		AddRoadEndForest(
			random, -1.0f, EndForestTreeCount, ForestRegion.WestEnd);
		AddSideForest(
			random, 1.0f, SideForestTreeCount, ForestRegion.SouthSide);
		AddSideForest(
			random, -1.0f, SideForestTreeCount, ForestRegion.NorthSide);

		Mesh? detailedTreeA = CreateDetailedForestMesh(TreeSceneA);
		Mesh? detailedTreeB = CreateDetailedForestMesh(TreeSceneB);
		if (detailedTreeA is not null)
		{
			CreateForestRegionBatches(detailedTreeA, _treeInstancesA, "DeepGreen");
		}
		if (detailedTreeB is not null)
		{
			CreateForestRegionBatches(detailedTreeB, _treeInstancesB, "Olive");
		}
	}

	private void AddRoadEndForest(
		RandomNumberGenerator random,
		float direction,
		int count,
		ForestRegion region)
	{
		for (int index = 0; index < count; index++)
		{
			float depth = random.RandfRange(0.0f, 38.0f);
			float x = direction * (136.0f + depth);
			int negativeCount = (count + 1) / 2;
			bool negative = index < negativeCount;
			int zoneIndex = negative ? index : index - negativeCount;
			int zoneCount = negative ? negativeCount : count - negativeCount;
			float zSequence = (zoneIndex + random.RandfRange(-0.24f, 0.24f)) /
				Mathf.Max(1.0f, zoneCount - 1.0f);
			float zMagnitude = Mathf.Lerp(8.5f, 61.0f, zSequence);
			float z = negative ? -zMagnitude : zMagnitude;
			float scale = random.RandfRange(1.5f, 2.52f);
			AddTreeInstance(
				random,
				new Vector3(x, -0.18f + SampleBackdropHeight(x, z), z),
				new Vector3(scale, scale * random.RandfRange(1.02f, 1.22f), scale),
				region);
		}
	}

	private void AddSideForest(
		RandomNumberGenerator random,
		float direction,
		int count,
		ForestRegion region)
	{
		for (int index = 0; index < count; index++)
		{
			float xSequence = (index + random.RandfRange(-0.38f, 0.38f)) /
				Mathf.Max(1.0f, count - 1.0f);
			float x = Mathf.Lerp(-138.0f, 138.0f, xSequence);
			float depth = random.RandfRange(0.0f, 27.0f);
			float z = direction * (39.0f + depth);
			float scale = random.RandfRange(1.3f, 2.18f);
			AddTreeInstance(
				random,
				new Vector3(x, -0.18f + SampleBackdropHeight(x, z), z),
				new Vector3(scale, scale * random.RandfRange(1.03f, 1.22f), scale),
				region);
		}
	}

	private void AddTreeInstance(
		RandomNumberGenerator random,
		Vector3 position,
		Vector3 scale,
		ForestRegion region)
	{
		Basis basis = new Basis(Vector3.Up, random.RandfRange(0.0f, Mathf.Tau))
			.Scaled(scale);
		ForestInstance instance = new(
			new Transform3D(basis, position),
			ChooseForestTint(random));
		if (random.Randf() < 0.53f)
		{
			GetForestRegion(_treeInstancesA, region).Add(instance);
		}
		else
		{
			GetForestRegion(_treeInstancesB, region).Add(instance);
		}
	}

	private static List<ForestInstance> GetForestRegion(
		Dictionary<ForestRegion, List<ForestInstance>> regions,
		ForestRegion region)
	{
		if (!regions.TryGetValue(region, out List<ForestInstance>? instances))
		{
			instances = new List<ForestInstance>();
			regions.Add(region, instances);
		}

		return instances;
	}

	private static Color ChooseForestTint(RandomNumberGenerator random)
	{
		float selection = random.Randf();
		int index = selection switch
		{
			< 0.42f => 0,
			< 0.67f => 1,
			< 0.84f => 2,
			< 0.93f => 3,
			_ => 4,
		};
		float brightness = random.RandfRange(0.9f, 1.06f);
		return ForestPalette[index] * brightness;
	}

	private void CreateForestRegionBatches(
		Mesh sharedMesh,
		IReadOnlyDictionary<ForestRegion, List<ForestInstance>> regions,
		string treeTypeName)
	{
		CreateForestMultiMesh(
			$"{treeTypeName}WestEnd", sharedMesh,
			GetForestRegion(regions, ForestRegion.WestEnd));
		CreateForestMultiMesh(
			$"{treeTypeName}EastEnd", sharedMesh,
			GetForestRegion(regions, ForestRegion.EastEnd));
		CreateForestMultiMesh(
			$"{treeTypeName}NorthSide", sharedMesh,
			GetForestRegion(regions, ForestRegion.NorthSide));
		CreateForestMultiMesh(
			$"{treeTypeName}SouthSide", sharedMesh,
			GetForestRegion(regions, ForestRegion.SouthSide));
	}

	private static IReadOnlyList<ForestInstance> GetForestRegion(
		IReadOnlyDictionary<ForestRegion, List<ForestInstance>> regions,
		ForestRegion region)
	{
		return regions.TryGetValue(region, out List<ForestInstance>? instances)
			? instances
			: System.Array.Empty<ForestInstance>();
	}

	private void CreateForestMultiMesh(
		string nodeName,
		Mesh sharedMesh,
		IReadOnlyList<ForestInstance> instances)
	{
		if (instances.Count == 0)
		{
			return;
		}

		Vector3 batchOrigin = Vector3.Zero;
		for (int index = 0; index < instances.Count; index++)
		{
			batchOrigin += instances[index].Transform.Origin;
		}
		batchOrigin /= instances.Count;

		Aabb sharedAabb = sharedMesh.GetAabb();
		Aabb batchAabb = default;
		MultiMesh forest = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			Mesh = sharedMesh,
			InstanceCount = instances.Count,
			VisibleInstanceCount = instances.Count,
		};
		for (int index = 0; index < instances.Count; index++)
		{
			Transform3D worldTransform = instances[index].Transform;
			Transform3D localTransform = new(
				worldTransform.Basis,
				worldTransform.Origin - batchOrigin);
			forest.SetInstanceTransform(index, localTransform);
			forest.SetInstanceColor(index, instances[index].Tint);
			Aabb instanceAabb = localTransform * sharedAabb;
			batchAabb = index == 0
				? instanceAabb
				: batchAabb.Merge(instanceAabb);
		}
		forest.CustomAabb = batchAabb.Grow(0.75f);

		MultiMeshInstance3D forestInstance = new()
		{
			Name = nodeName,
			Position = batchOrigin,
			Multimesh = forest,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			VisibilityRangeEnd = 220.0f,
			VisibilityRangeEndMargin = 18.0f,
		};
		forestInstance.SetMeta("environment_role", "seasonal_distant_county_treeline");
		forestInstance.SetMeta("render_strategy", "region_culled_detailed_midground");
		GetOrCreateLayer("DistantForest").AddChild(forestInstance);
	}

	private Mesh? CreateDetailedForestMesh(PackedScene sourceScene)
	{
		Node source = sourceScene.Instantiate();
		MeshInstance3D? sourceMesh = FindFirstMesh(source);
		if (sourceMesh?.Mesh is null)
		{
			source.Free();
			GD.PushWarning($"{Name}: '{sourceScene.ResourcePath}' has no forest mesh.");
			return null;
		}

		Mesh sharedMesh = (Mesh)sourceMesh.Mesh.Duplicate();
		for (int surface = 0; surface < sharedMesh.GetSurfaceCount(); surface++)
		{
			Material? sourceOverride = sourceMesh.GetSurfaceOverrideMaterial(surface);
			Material? sourceSurface = sourceOverride ?? sharedMesh.SurfaceGetMaterial(surface);
			if (sourceSurface?.Duplicate() is Material material)
			{
				if (surface > 0 && material is BaseMaterial3D foliageMaterial)
				{
					foliageMaterial.VertexColorUseAsAlbedo = true;
				}
				sharedMesh.SurfaceSetMaterial(surface, material);
			}
		}

		source.Free();
		return sharedMesh;
	}

	private void BuildSeasonalLeafDrifts()
	{
		if (FallenLeafCount <= 0)
		{
			return;
		}

		StandardMaterial3D leafMaterial = new()
		{
			AlbedoColor = Colors.White,
			Roughness = 0.96f,
			VertexColorUseAsAlbedo = true,
		};
		ArrayMesh leafMesh = CreateLeafMesh(leafMaterial);
		MultiMesh leaves = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			Mesh = leafMesh,
			InstanceCount = FallenLeafCount,
			VisibleInstanceCount = FallenLeafCount,
		};

		float[] clusterAnchors = { -91.0f, -67.0f, -42.0f, -17.0f, 9.0f, 34.0f, 61.0f, 87.0f };
		RandomNumberGenerator random = new() { Seed = LayoutSeed + 781u };
		for (int index = 0; index < FallenLeafCount; index++)
		{
			float x = random.Randf() < 0.74f
				? clusterAnchors[random.RandiRange(0, clusterAnchors.Length - 1)] +
					random.RandfRange(-5.8f, 5.8f)
				: random.RandfRange(-104.0f, 104.0f);
			float side = random.Randf() < 0.5f ? -1.0f : 1.0f;
			float z = side * (random.Randf() < 0.76f
				? random.RandfRange(5.55f, 6.85f)
				: random.RandfRange(7.1f, 8.7f));
			float scale = random.RandfRange(0.65f, 1.45f);
			Basis basis = new Basis(Vector3.Up, random.RandfRange(0.0f, Mathf.Tau))
				.Scaled(new Vector3(scale, scale, random.RandfRange(0.72f, 1.22f)));
			leaves.SetInstanceTransform(
				index,
				new Transform3D(basis, new Vector3(x, 0.226f, z)));
			leaves.SetInstanceColor(
				index,
				LeafPalette[random.RandiRange(0, LeafPalette.Length - 1)] *
				random.RandfRange(0.82f, 1.08f));
		}

		MultiMeshInstance3D leafDrifts = new()
		{
			Name = "CurbLeafDrifts",
			Multimesh = leaves,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			VisibilityRangeEnd = 36.0f,
			VisibilityRangeEndMargin = 6.0f,
		};
		leafDrifts.SetMeta("environment_role", "batched_late_season_ground_detail");
		GetOrCreateLayer("SeasonalGroundDetail").AddChild(leafDrifts);
	}

	private void BuildClockAndSignalDetails()
	{
		Node3D? clock = GetNodeOrNull<Node3D>("MainStreetClock");
		if (clock is not null)
		{
			StandardMaterial3D iron = CreateMaterial(
				new Color(0.025f, 0.03f, 0.028f), 0.75f, 0.28f);
			BoxMesh tickMesh = new()
			{
				Size = new Vector3(0.026f, 0.13f, 0.025f),
				Material = iron,
			};
			MultiMesh ticks = new()
			{
				TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
				Mesh = tickMesh,
				InstanceCount = 24,
				VisibleInstanceCount = 24,
			};
			for (int face = 0; face < 2; face++)
			{
				float faceX = face == 0 ? -0.191f : 0.191f;
				for (int hour = 0; hour < 12; hour++)
				{
					float angle = hour * (Mathf.Tau / 12.0f);
					float cardinalScale = hour % 3 == 0 ? 1.34f : 1.0f;
					Basis basis = new Basis(Vector3.Right, -angle)
						.Scaled(new Vector3(1.0f, cardinalScale, 1.0f));
					Vector3 position = new(
						faceX,
						4.68f + (Mathf.Cos(angle) * 0.49f),
						Mathf.Sin(angle) * 0.49f);
					ticks.SetInstanceTransform((face * 12) + hour,
						new Transform3D(basis, position));
				}
			}
			clock.AddChild(new MultiMeshInstance3D
			{
				Name = "HourIndices",
				Multimesh = ticks,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			});

			CylinderMesh hubMesh = new()
			{
				TopRadius = 0.072f,
				BottomRadius = 0.072f,
				Height = 0.035f,
				RadialSegments = 16,
				Material = iron,
			};
			MultiMesh hubs = new()
			{
				TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
				Mesh = hubMesh,
				InstanceCount = 2,
				VisibleInstanceCount = 2,
			};
			Basis hubBasis = new Basis(Vector3.Forward, Mathf.Pi * 0.5f);
			hubs.SetInstanceTransform(0,
				new Transform3D(hubBasis, new Vector3(-0.207f, 4.68f, 0.0f)));
			hubs.SetInstanceTransform(1,
				new Transform3D(hubBasis, new Vector3(0.207f, 4.68f, 0.0f)));
			clock.AddChild(new MultiMeshInstance3D
			{
				Name = "FaceHubs",
				Multimesh = hubs,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			});

			AddCivicPlaque(clock);
		}
	}

	private static void AddCivicPlaque(Node3D clock)
	{
		StandardMaterial3D green = CreateMaterial(
			new Color(0.045f, 0.13f, 0.095f), 0.18f, 0.62f);
		BoxMesh plaqueMesh = new()
		{
			Size = new Vector3(0.045f, 0.66f, 1.05f),
			Material = green,
		};
		MultiMesh plaques = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			Mesh = plaqueMesh,
			InstanceCount = 2,
			VisibleInstanceCount = 2,
		};
		plaques.SetInstanceTransform(0,
			new Transform3D(Basis.Identity, new Vector3(-0.18f, 3.48f, 0.0f)));
		plaques.SetInstanceTransform(1,
			new Transform3D(Basis.Identity, new Vector3(0.18f, 3.48f, 0.0f)));
		clock.AddChild(new MultiMeshInstance3D
		{
			Name = "CivicPlaques",
			Multimesh = plaques,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});
		AddTwoSidedLabel(
			clock,
			"ASHWOOD\nEST. 1952",
			new Vector3(0.208f, 3.48f, 0.0f),
			42,
			0.0038f);
	}

	private static void AddTwoSidedLabel(
		Node3D parent,
		string text,
		Vector3 position,
		int fontSize,
		float pixelSize)
	{
		for (int side = 0; side < 2; side++)
		{
			Label3D label = new()
			{
				Name = side == 0 ? "EastLabel" : "WestLabel",
				Text = text,
				FontSize = fontSize,
				PixelSize = pixelSize,
				Modulate = new Color(0.82f, 0.74f, 0.56f),
				OutlineModulate = new Color(0.025f, 0.035f, 0.03f),
				OutlineSize = 7,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Shaded = true,
				DoubleSided = false,
				Position = new Vector3(
					side == 0 ? position.X : -position.X,
					position.Y,
					position.Z),
				RotationDegrees = new Vector3(0.0f, side == 0 ? 90.0f : -90.0f, 0.0f),
			};
			parent.AddChild(label);
		}
	}

	private void MergeStaticMeshesByMaterial(
		string parentPath,
		float visibilityRangeEnd)
	{
		Node3D? parent = GetNodeOrNull<Node3D>(parentPath);
		if (parent is null)
		{
			return;
		}

		Dictionary<StaticMeshMergeKey, List<StaticMeshMergeSource>> batches = new();
		foreach (Node child in parent.GetChildren())
		{
			if (child is not MeshInstance3D instance ||
				instance.Mesh is null ||
				instance.Mesh.GetSurfaceCount() != 1 ||
				instance.GetActiveMaterial(0) is not Material material)
			{
				continue;
			}

			StaticMeshMergeKey key = new(material.GetRid(), instance.CastShadow);
			if (!batches.TryGetValue(
				key,
				out List<StaticMeshMergeSource>? batch))
			{
				batch = new List<StaticMeshMergeSource>();
				batches.Add(key, batch);
			}
			batch.Add(new StaticMeshMergeSource(instance, material));
		}

		int mergedIndex = 0;
		foreach (KeyValuePair<StaticMeshMergeKey, List<StaticMeshMergeSource>> pair
			in batches)
		{
			List<StaticMeshMergeSource> batch = pair.Value;
			if (batch.Count < 2)
			{
				continue;
			}

			SurfaceTool surfaceTool = new();
			surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
			foreach (StaticMeshMergeSource source in batch)
			{
				surfaceTool.AppendFrom(source.Instance.Mesh!, 0, source.Instance.Transform);
			}
			ArrayMesh mergedMesh = surfaceTool.Commit();
			mergedMesh.SurfaceSetMaterial(0, batch[0].Material);

			MeshInstance3D mergedInstance = new()
			{
				Name = $"MergedStaticBatch{mergedIndex++}",
				Mesh = mergedMesh,
				CastShadow = pair.Key.ShadowMode,
				VisibilityRangeEnd = visibilityRangeEnd,
				VisibilityRangeEndMargin = 10.0f,
			};
			mergedInstance.SetMeta("merged_source_count", batch.Count);
			parent.AddChild(mergedInstance);

			foreach (StaticMeshMergeSource source in batch)
			{
				source.Instance.Visible = false;
			}
		}
	}

	private static ArrayMesh CreateLeafMesh(Material material)
	{
		SurfaceTool surfaceTool = new();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		Vector3[] vertices =
		{
			new(-0.06f, 0.0f, 0.0f),
			new(0.0f, 0.002f, 0.026f),
			new(0.06f, 0.0f, 0.0f),
			new(0.0f, 0.002f, -0.026f),
		};
		Vector2[] uvs =
		{
			new(0.0f, 0.5f),
			new(0.5f, 0.0f),
			new(1.0f, 0.5f),
			new(0.5f, 1.0f),
		};
		for (int index = 0; index < vertices.Length; index++)
		{
			surfaceTool.SetNormal(Vector3.Up);
			surfaceTool.SetUV(uvs[index]);
			surfaceTool.AddVertex(vertices[index]);
		}
		surfaceTool.AddIndex(0);
		surfaceTool.AddIndex(1);
		surfaceTool.AddIndex(2);
		surfaceTool.AddIndex(0);
		surfaceTool.AddIndex(2);
		surfaceTool.AddIndex(3);
		ArrayMesh mesh = surfaceTool.Commit();
		mesh.SurfaceSetMaterial(0, material);
		return mesh;
	}

	private static StandardMaterial3D CreateMaterial(
		Color albedo,
		float metallic,
		float roughness)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = albedo,
			Metallic = metallic,
			Roughness = roughness,
		};
	}

	private Node3D GetOrCreateLayer(string nodeName)
	{
		Node3D? existing = GetNodeOrNull<Node3D>(nodeName);
		if (existing is not null)
		{
			return existing;
		}

		Node3D layer = new() { Name = nodeName };
		AddChild(layer);
		return layer;
	}

	private static MeshInstance3D? FindFirstMesh(Node node)
	{
		if (node is MeshInstance3D meshInstance && meshInstance.Mesh is not null)
		{
			return meshInstance;
		}

		foreach (Node child in node.GetChildren())
		{
			MeshInstance3D? result = FindFirstMesh(child);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}

}
