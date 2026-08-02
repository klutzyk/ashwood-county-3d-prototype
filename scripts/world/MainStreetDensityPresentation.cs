#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Adds streetscape continuity outside the authored gameplay props. Repeated
/// visual primitives are merged or instanced; only the accessible facade masses
/// inside the safety boundary receive simple static box collision.
/// </summary>
public partial class MainStreetDensityPresentation : Node3D
{
	[Export] public Material? RoadMaterial { get; set; }
	[Export] public Material? BrickMaterialA { get; set; }
	[Export] public Material? BrickMaterialB { get; set; }
	[Export] public Material? RoofMaterial { get; set; }
	[Export] public PackedScene? StalledVehicleScene { get; set; }
	[Export] public uint LayoutSeed { get; set; } = 1952;

	private readonly record struct FacadeLot(
		string Name,
		float X,
		float Side,
		float Width,
		float Height,
		float Depth,
		int Style);

	private static readonly FacadeLot[] FacadeLots =
	{
		new("NorthWestGateway", -104.0f, -1.0f, 10.5f, 6.0f, 5.2f, 0),
		new("NorthCivicAnnex", 59.5f, -1.0f, 7.5f, 5.6f, 5.0f, 1),
		new("NorthMercantile", 83.0f, -1.0f, 9.5f, 7.1f, 5.8f, 2),
		new("NorthEastCorner", 102.0f, -1.0f, 11.5f, 6.0f, 5.2f, 3),
		new("SouthWestTailor", -80.0f, 1.0f, 8.5f, 5.8f, 5.0f, 1),
		new("SouthWestCafe", -68.5f, 1.0f, 7.5f, 6.6f, 5.3f, 0),
		new("SouthEastFeedStore", 68.0f, 1.0f, 8.5f, 5.8f, 5.2f, 3),
	};

	private static readonly Color[] PaperPalette =
	{
		new(0.62f, 0.57f, 0.46f),
		new(0.75f, 0.7f, 0.58f),
		new(0.48f, 0.43f, 0.35f),
	};

	private static readonly Color[] WeedPalette =
	{
		new(0.29f, 0.35f, 0.16f),
		new(0.4f, 0.39f, 0.17f),
		new(0.35f, 0.24f, 0.1f),
		new(0.24f, 0.3f, 0.13f),
	};

	public override void _Ready()
	{
		BuildRoadApproaches();
		BuildHistoricFrontage();
		BuildNarrativeDressing();
	}

	private void BuildRoadApproaches()
	{
		Node3D layer = GetOrCreateLayer("RoadApproaches");
		SurfaceTool surfaceTool = new();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		const int segments = 22;
		for (int directionIndex = 0; directionIndex < 2; directionIndex++)
		{
			float direction = directionIndex == 0 ? -1.0f : 1.0f;
			int baseVertex = directionIndex * ((segments + 1) * 2);
			for (int segment = 0; segment <= segments; segment++)
			{
				float t = segment / (float)segments;
				float absoluteX = Mathf.Lerp(106.0f, 218.0f, t);
				float centreZ = SampleRoadCentre(direction, t);
				float halfWidth = Mathf.Lerp(5.82f, 4.3f, SmoothStep(t));
				float height = SampleRoadGrade(absoluteX) + 0.105f;
				for (int edge = 0; edge < 2; edge++)
				{
					float z = centreZ + (edge == 0 ? -halfWidth : halfWidth);
					surfaceTool.SetNormal(Vector3.Up);
					surfaceTool.SetUV(new Vector2(absoluteX * 0.18f, z * 0.18f));
					surfaceTool.AddVertex(new Vector3(direction * absoluteX, height, z));
				}
			}

			for (int segment = 0; segment < segments; segment++)
			{
				int a = baseVertex + (segment * 2);
				int b = a + 1;
				int c = a + 2;
				int d = a + 3;
				surfaceTool.AddIndex(a);
				surfaceTool.AddIndex(c);
				surfaceTool.AddIndex(b);
				surfaceTool.AddIndex(b);
				surfaceTool.AddIndex(c);
				surfaceTool.AddIndex(d);
			}
		}

		ArrayMesh approachMesh = surfaceTool.Commit();
		Material asphalt = DuplicateOrFallback(
			RoadMaterial,
			new Color(0.13f, 0.125f, 0.115f),
			0.98f);
		if (asphalt is BaseMaterial3D asphaltBase)
		{
			asphaltBase.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		}
		approachMesh.SurfaceSetMaterial(0, asphalt);

		MeshInstance3D surface = new()
		{
			Name = "ApproachSurface",
			Mesh = approachMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			VisibilityRangeEnd = 330.0f,
			VisibilityRangeEndMargin = 24.0f,
		};
		surface.SetMeta("environment_role", "tapered_visual_main_street_continuation");
		surface.SetMeta("visual_length_metres", 224.0f);
		layer.AddChild(surface);

		StandardMaterial3D markingMaterial = new()
		{
			AlbedoColor = new Color(0.47f, 0.35f, 0.09f),
			Roughness = 0.96f,
		};
		BoxMesh dashMesh = new()
		{
			Size = new Vector3(5.0f, 0.024f, 0.13f),
			Material = markingMaterial,
		};
		const int dashesPerEnd = 8;
		MultiMesh dashes = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			Mesh = dashMesh,
			InstanceCount = dashesPerEnd * 2,
			VisibleInstanceCount = dashesPerEnd * 2,
		};
		for (int directionIndex = 0; directionIndex < 2; directionIndex++)
		{
			float direction = directionIndex == 0 ? -1.0f : 1.0f;
			for (int index = 0; index < dashesPerEnd; index++)
			{
				float absoluteX = 116.0f + (index * 13.0f);
				float t = Mathf.InverseLerp(106.0f, 218.0f, absoluteX);
				float centreZ = SampleRoadCentre(direction, t);
				float aheadZ = SampleRoadCentre(direction, Mathf.Min(1.0f, t + 0.02f));
				float yaw = Mathf.Atan2(aheadZ - centreZ, direction * 2.24f);
				dashes.SetInstanceTransform(
					(directionIndex * dashesPerEnd) + index,
					new Transform3D(
						new Basis(Vector3.Up, -yaw),
						new Vector3(
							direction * absoluteX,
							SampleRoadGrade(absoluteX) + 0.124f,
							centreZ)));
			}
		}
		MultiMeshInstance3D markings = new()
		{
			Name = "FadedCentreDashes",
			Multimesh = dashes,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			VisibilityRangeEnd = 280.0f,
			VisibilityRangeEndMargin = 20.0f,
		};
		markings.SetMeta("environment_role", "road_end_depth_cue");
		layer.AddChild(markings);
	}

	private void BuildHistoricFrontage()
	{
		Node3D layer = GetOrCreateLayer("HistoricFrontage");
		List<Transform3D> brickA = new();
		List<Transform3D> brickB = new();
		List<Transform3D> trim = new();
		List<Transform3D> glass = new();
		List<Transform3D> painted = new();
		List<Transform3D> awnings = new();
		List<Transform3D> boards = new();
		Node3D collisionLayer = new() { Name = "FacadeCollision" };
		collisionLayer.SetMeta("environment_role", "accessible_infill_building_collision");
		layer.AddChild(collisionLayer);

		foreach (FacadeLot lot in FacadeLots)
		{
			float frontZ = lot.Side * 9.02f;
			float volumeZ = frontZ + (lot.Side * lot.Depth * 0.5f);
			List<Transform3D> brick = lot.Style % 2 == 0 ? brickA : brickB;
			brick.Add(BoxTransform(
				new Vector3(lot.X, 0.2f + (lot.Height * 0.5f), volumeZ),
				new Vector3(lot.Width, lot.Height, lot.Depth)));
			AddFacadeCollision(collisionLayer, lot, volumeZ);

			float parapetHeight = lot.Style == 2 ? 0.85f : 0.58f;
			brick.Add(BoxTransform(
				new Vector3(lot.X, 0.2f + lot.Height + (parapetHeight * 0.5f), volumeZ),
				new Vector3(lot.Width + 0.12f, parapetHeight, lot.Depth + 0.08f)));
			trim.Add(BoxTransform(
				new Vector3(lot.X, lot.Height + 0.45f, frontZ - (lot.Side * 0.07f)),
				new Vector3(lot.Width + 0.42f, 0.3f, 0.19f)));
			trim.Add(BoxTransform(
				new Vector3(lot.X, 3.15f, frontZ - (lot.Side * 0.08f)),
				new Vector3(lot.Width * 0.9f, 0.22f, 0.18f)));
			trim.Add(BoxTransform(
				new Vector3(lot.X - (lot.Width * 0.5f) + 0.16f, 2.0f,
					frontZ - (lot.Side * 0.09f)),
				new Vector3(0.25f, 3.55f, 0.2f)));
			trim.Add(BoxTransform(
				new Vector3(lot.X + (lot.Width * 0.5f) - 0.16f, 2.0f,
					frontZ - (lot.Side * 0.09f)),
				new Vector3(0.25f, 3.55f, 0.2f)));

			// Side-wall windows and lintels prevent oblique approaches from exposing
			// the large blank brick slabs common to placeholder storefront massing.
			for (int wall = 0; wall < 2; wall++)
			{
				float wallDirection = wall == 0 ? -1.0f : 1.0f;
				float wallX = lot.X + (wallDirection * ((lot.Width * 0.5f) + 0.055f));
				for (int sideWindow = 0; sideWindow < 2; sideWindow++)
				{
					float sideWindowZ = frontZ +
						(lot.Side * (1.25f +
						(sideWindow * Mathf.Min(1.65f, lot.Depth * 0.24f))));
					glass.Add(BoxTransform(
						new Vector3(wallX, Mathf.Max(3.85f, lot.Height - 1.72f), sideWindowZ),
						new Vector3(0.075f, 1.22f, 1.05f)));
					trim.Add(BoxTransform(
						new Vector3(
							wallX + (wallDirection * 0.02f),
							Mathf.Max(4.55f, lot.Height - 1.02f),
							sideWindowZ),
						new Vector3(0.13f, 0.13f, 1.28f)));
				}
			}

			float glassZ = frontZ - (lot.Side * 0.115f);
			float groundWindowWidth = Mathf.Max(1.45f, lot.Width * 0.27f);
			glass.Add(BoxTransform(
				new Vector3(lot.X - (lot.Width * 0.23f), 1.67f, glassZ),
				new Vector3(groundWindowWidth, 2.05f, 0.075f)));
			glass.Add(BoxTransform(
				new Vector3(lot.X + (lot.Width * 0.08f), 1.67f, glassZ),
				new Vector3(groundWindowWidth, 2.05f, 0.075f)));
			painted.Add(BoxTransform(
				new Vector3(lot.X + (lot.Width * 0.36f), 1.55f, glassZ),
				new Vector3(0.92f, 2.65f, 0.09f)));
			painted.Add(BoxTransform(
				new Vector3(lot.X, 2.88f, glassZ),
				new Vector3(lot.Width * 0.84f, 0.42f, 0.11f)));
			painted.Add(BoxTransform(
				new Vector3(lot.X - (lot.Width * 0.075f), 1.67f, glassZ),
				new Vector3(0.1f, 2.1f, 0.13f)));
			painted.Add(BoxTransform(
				new Vector3(lot.X - (lot.Width * 0.23f), 0.61f, glassZ),
				new Vector3(groundWindowWidth + 0.15f, 0.14f, 0.13f)));
			painted.Add(BoxTransform(
				new Vector3(lot.X + (lot.Width * 0.08f), 0.61f, glassZ),
				new Vector3(groundWindowWidth + 0.15f, 0.14f, 0.13f)));

			int upperWindowCount = Mathf.Clamp(Mathf.FloorToInt(lot.Width / 2.8f), 2, 4);
			float upperY = Mathf.Max(4.3f, lot.Height - 1.72f);
			for (int window = 0; window < upperWindowCount; window++)
			{
				float normalized = upperWindowCount == 1
					? 0.5f
					: window / (upperWindowCount - 1.0f);
				float windowX = lot.X + Mathf.Lerp(
					-lot.Width * 0.36f,
					lot.Width * 0.36f,
					normalized);
				glass.Add(BoxTransform(
					new Vector3(windowX, upperY, glassZ),
					new Vector3(1.15f, 1.35f, 0.07f)));
				trim.Add(BoxTransform(
					new Vector3(windowX, upperY + 0.77f, glassZ),
					new Vector3(1.42f, 0.14f, 0.12f)));
				trim.Add(BoxTransform(
					new Vector3(windowX, upperY - 0.77f, glassZ),
					new Vector3(1.42f, 0.14f, 0.12f)));
				trim.Add(BoxTransform(
					new Vector3(windowX - 0.65f, upperY, glassZ),
					new Vector3(0.13f, 1.5f, 0.12f)));
				trim.Add(BoxTransform(
					new Vector3(windowX + 0.65f, upperY, glassZ),
					new Vector3(0.13f, 1.5f, 0.12f)));
			}

			if (lot.Style != 2)
			{
				Basis awningBasis = new Basis(Vector3.Right, lot.Side * 0.12f)
					.Scaled(new Vector3(lot.Width * 0.72f, 0.12f, 1.05f));
				awnings.Add(new Transform3D(
					awningBasis,
					new Vector3(lot.X, 2.65f, frontZ - (lot.Side * 0.55f))));
				awnings.Add(BoxTransform(
					new Vector3(lot.X, 2.46f, frontZ - (lot.Side * 1.04f)),
					new Vector3(lot.Width * 0.72f, 0.28f, 0.08f)));
			}
			else
			{
				for (int board = -1; board <= 1; board++)
				{
					Basis boardBasis = new Basis(Vector3.Forward, board * 0.12f)
						.Scaled(new Vector3(groundWindowWidth * 0.95f, 0.16f, 0.09f));
					boards.Add(new Transform3D(
						boardBasis,
						new Vector3(
							lot.X - (lot.Width * 0.23f),
							1.67f + (board * 0.55f),
							glassZ - (lot.Side * 0.05f))));
				}
			}
		}

		CreateBoxBatch(layer, "BrickBatchA", BrickMaterialA, brickA,
			new Color(0.64f, 0.46f, 0.36f), true, tintSource: true);
		CreateBoxBatch(layer, "BrickBatchB", BrickMaterialB, brickB,
			new Color(0.56f, 0.51f, 0.44f), true, tintSource: true);
		CreateBoxBatch(layer, "CorniceAndTrim", RoofMaterial, trim,
			new Color(0.07f, 0.075f, 0.065f), false);
		CreateBoxBatch(layer, "ShopfrontGlass", null, glass,
			new Color(0.12f, 0.16f, 0.165f), false, 0.3f, 0.18f);
		CreateBoxBatch(layer, "PaintedJoinery", null, painted,
			new Color(0.11f, 0.19f, 0.14f), false);
		CreateBoxBatch(layer, "CanvasAwnings", null, awnings,
			new Color(0.34f, 0.19f, 0.085f), false);
		CreateBoxBatch(layer, "BoardedWindow", null, boards,
			new Color(0.33f, 0.23f, 0.135f), false);
		layer.SetMeta("environment_role", "continuous_historic_storefront_silhouette");
		layer.SetMeta("facade_lot_count", FacadeLots.Length);
		AddFacadeSigns(layer);
	}

	private static void AddFacadeSigns(Node3D parent)
	{
		AddFrontSign(parent, "CivicAnnexSign", "COUNTY CLERK", 59.5f, -1.0f, 3.02f, 36);
		AddFrontSign(parent, "MercantileSign", "ASHWOOD MERCANTILE", 83.0f, -1.0f, 3.02f, 33);
		AddFrontSign(parent, "TailorSign", "HARRIS DRY GOODS", -80.0f, 1.0f, 3.02f, 34);
		AddFrontSign(parent, "FeedStoreSign", "COUNTY FEED & SEED", 68.0f, 1.0f, 3.02f, 32);

		Label3D ghostSign = CreateSignLabel("ASHWOOD\nMERCANTILE", 46, 0.0052f);
		ghostSign.Name = "MercantileWestWallGhostSign";
		ghostSign.Position = new Vector3(78.18f, 4.15f, -11.45f);
		ghostSign.RotationDegrees = new Vector3(0.0f, -90.0f, 0.0f);
		ghostSign.Modulate = new Color(0.58f, 0.48f, 0.32f, 0.78f);
		parent.AddChild(ghostSign);

		Label3D civicWallSign = CreateSignLabel("COUNTY\nRECORDS", 42, 0.005f);
		civicWallSign.Name = "CivicAnnexEastWallGhostSign";
		civicWallSign.Position = new Vector3(63.32f, 3.62f, -11.25f);
		civicWallSign.RotationDegrees = new Vector3(0.0f, 90.0f, 0.0f);
		civicWallSign.Modulate = new Color(0.57f, 0.47f, 0.31f, 0.76f);
		parent.AddChild(civicWallSign);
	}

	private static void AddFacadeCollision(
		Node3D collisionLayer,
		FacadeLot lot,
		float volumeZ)
	{
		// Extend the simple shell a few centimetres toward the pavement. Some
		// infill silhouettes deliberately sit directly in front of the authored
		// school shell; the shallow lip makes the visible shopfront the first
		// contact surface instead of leaving physics order ambiguous where the
		// two building masses nearly share a plane.
		float pavementContactLip = lot.Name is "NorthMercantile" or "NorthEastCorner"
			? 0.2f
			: 0.0f;
		StaticBody3D body = new()
		{
			Name = lot.Name,
			Position = new Vector3(
				lot.X,
				0.2f + (lot.Height * 0.5f),
				volumeZ - (lot.Side * pavementContactLip * 0.5f)),
			CollisionLayer = 1u,
			CollisionMask = 1u,
		};
		body.SetMeta("environment_role", "historic_facade_solid_volume");
		body.AddChild(new CollisionShape3D
		{
			Name = "Collision",
			Shape = new BoxShape3D
			{
				Size = new Vector3(
					lot.Width,
					lot.Height,
					lot.Depth + pavementContactLip),
			},
		});
		collisionLayer.AddChild(body);
	}

	private static void AddFrontSign(
		Node3D parent,
		string name,
		string text,
		float x,
		float side,
		float y,
		int fontSize)
	{
		Label3D label = CreateSignLabel(text, fontSize, 0.0037f);
		label.Name = name;
		label.Position = new Vector3(x, y, (side * 9.02f) - (side * 0.19f));
		label.RotationDegrees = new Vector3(0.0f, side < 0.0f ? 0.0f : 180.0f, 0.0f);
		parent.AddChild(label);
	}

	private static Label3D CreateSignLabel(string text, int fontSize, float pixelSize) =>
		new()
		{
			Text = text,
			FontSize = fontSize,
			PixelSize = pixelSize,
			Modulate = new Color(0.76f, 0.66f, 0.45f),
			OutlineModulate = new Color(0.045f, 0.035f, 0.025f),
			OutlineSize = 6,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Shaded = true,
			DoubleSided = false,
		};

	private void BuildNarrativeDressing()
	{
		Node3D layer = GetOrCreateLayer("NarrativeDressing");
		List<Transform3D> crates = new()
		{
			BoxTransform(new Vector3(84.2f, 0.52f, 5.55f), new Vector3(0.92f, 0.72f, 0.76f), 0.18f),
			BoxTransform(new Vector3(85.1f, 0.43f, 6.18f), new Vector3(0.72f, 0.52f, 0.66f), -0.32f),
			BoxTransform(new Vector3(85.0f, 1.08f, 5.52f), new Vector3(0.64f, 0.52f, 0.58f), 0.08f),
			BoxTransform(new Vector3(86.0f, 0.38f, 5.92f), new Vector3(0.78f, 0.42f, 0.62f), 0.58f),
			BoxTransform(new Vector3(-57.2f, 0.42f, -8.43f), new Vector3(0.68f, 0.45f, 0.56f), -0.2f),
		};
		CreateBoxBatch(layer, "InterruptedDeliveryCrates", null, crates,
			new Color(0.32f, 0.205f, 0.105f), false);
		BuildPaperDrifts(layer);
		BuildWeedTufts(layer);
		AddStalledVehicle(layer);
		layer.SetMeta("environment_role", "low_cost_abandonment_story_clusters");
	}

	private void BuildPaperDrifts(Node3D parent)
	{
		StandardMaterial3D paperMaterial = new()
		{
			AlbedoColor = Colors.White,
			Roughness = 0.95f,
			VertexColorUseAsAlbedo = true,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};
		ArrayMesh paperMesh = CreateGroundDiamondMesh(paperMaterial, 0.11f, 0.055f);
		const int paperCount = 28;
		MultiMesh papers = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			Mesh = paperMesh,
			InstanceCount = paperCount,
			VisibleInstanceCount = paperCount,
		};
		RandomNumberGenerator random = new() { Seed = LayoutSeed + 2401u };
		for (int index = 0; index < paperCount; index++)
		{
			bool delivery = index < 19;
			Vector3 anchor = delivery
				? new Vector3(85.4f, 0.208f, 5.6f)
				: new Vector3(-57.2f, 0.208f, -7.05f);
			Vector3 position = anchor + new Vector3(
				random.RandfRange(-3.2f, 3.2f),
				random.RandfRange(0.0f, 0.008f),
				random.RandfRange(-1.35f, 1.35f));
			float scale = random.RandfRange(0.72f, 1.3f);
			papers.SetInstanceTransform(index, new Transform3D(
				new Basis(Vector3.Up, random.RandfRange(0.0f, Mathf.Tau))
					.Scaled(new Vector3(scale, scale, random.RandfRange(0.72f, 1.15f))),
				position));
			papers.SetInstanceColor(index,
				PaperPalette[random.RandiRange(0, PaperPalette.Length - 1)] *
				random.RandfRange(0.9f, 1.07f));
		}

		MultiMeshInstance3D drift = new()
		{
			Name = "WindblownDeliveryPapers",
			Multimesh = papers,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			VisibilityRangeEnd = 54.0f,
			VisibilityRangeEndMargin = 7.0f,
		};
		drift.SetMeta("environment_role", "interrupted_delivery_story_detail");
		parent.AddChild(drift);
	}

	private void BuildWeedTufts(Node3D parent)
	{
		StandardMaterial3D weedMaterial = new()
		{
			AlbedoColor = Colors.White,
			Roughness = 1.0f,
			VertexColorUseAsAlbedo = true,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};
		ArrayMesh weedMesh = CreateWeedMesh(weedMaterial);
		const int weedCount = 36;
		MultiMesh weeds = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true,
			Mesh = weedMesh,
			InstanceCount = weedCount,
			VisibleInstanceCount = weedCount,
		};
		RandomNumberGenerator random = new() { Seed = LayoutSeed + 3307u };
		for (int index = 0; index < weedCount; index++)
		{
			bool approach = index >= 26;
			float side = random.Randf() < 0.5f ? -1.0f : 1.0f;
			float x;
			float z;
			float y;
			if (approach)
			{
				float direction = random.Randf() < 0.5f ? -1.0f : 1.0f;
				float absoluteX = random.RandfRange(111.5f, 145.0f);
				x = direction * absoluteX;
				z = SampleRoadCentre(direction,
					Mathf.InverseLerp(106.0f, 218.0f, absoluteX)) +
					(side * random.RandfRange(6.0f, 8.2f));
				y = SampleRoadGrade(absoluteX) + 0.03f;
			}
			else
			{
				FacadeLot lot = FacadeLots[random.RandiRange(0, FacadeLots.Length - 1)];
				x = lot.X + random.RandfRange(-lot.Width * 0.46f, lot.Width * 0.46f);
				z = lot.Side * random.RandfRange(8.7f, 9.15f);
				y = 0.205f;
			}
			float scale = random.RandfRange(0.65f, 1.45f);
			weeds.SetInstanceTransform(index, new Transform3D(
				new Basis(Vector3.Up, random.RandfRange(0.0f, Mathf.Tau))
					.Scaled(new Vector3(scale, scale * random.RandfRange(0.78f, 1.25f), scale)),
				new Vector3(x, y, z)));
			weeds.SetInstanceColor(index,
				WeedPalette[random.RandiRange(0, WeedPalette.Length - 1)] *
				random.RandfRange(0.87f, 1.08f));
		}

		MultiMeshInstance3D tuftLayer = new()
		{
			Name = "FacadeAndShoulderWeeds",
			Multimesh = weeds,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			VisibilityRangeEnd = 72.0f,
			VisibilityRangeEndMargin = 8.0f,
		};
		tuftLayer.SetMeta("environment_role", "batched_overgrowth_transition_detail");
		parent.AddChild(tuftLayer);
	}

	private void AddStalledVehicle(Node3D parent)
	{
		if (StalledVehicleScene is null)
		{
			GD.PushWarning($"{Name}: stalled vehicle scene is not assigned.");
			return;
		}

		Node3D vehicle = StalledVehicleScene.Instantiate<Node3D>();
		vehicle.Name = "EastEvacuationSedan";
		vehicle.Position = new Vector3(132.0f, SampleRoadGrade(132.0f) + 0.1f, 2.65f);
		vehicle.RotationDegrees = new Vector3(0.0f, 87.0f, -1.3f);
		vehicle.Scale = Vector3.One * 0.008f;
		vehicle.SetMeta("environment_role", "stalled_vehicle_at_town_limit");
		ConfigureVisualDescendants(vehicle);
		parent.AddChild(vehicle);
	}

	private static void ConfigureVisualDescendants(Node node)
	{
		if (node is GeometryInstance3D geometry)
		{
			geometry.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			geometry.VisibilityRangeEnd = 230.0f;
			geometry.VisibilityRangeEndMargin = 16.0f;
		}

		foreach (Node child in node.GetChildren())
		{
			ConfigureVisualDescendants(child);
		}
	}

	private static void CreateBoxBatch(
		Node3D parent,
		string nodeName,
		Material? sourceMaterial,
		IReadOnlyList<Transform3D> transforms,
		Color fallbackColor,
		bool castShadow,
		float roughness = 0.88f,
		float metallic = 0.0f,
		bool tintSource = false)
	{
		if (transforms.Count == 0)
		{
			return;
		}

		BoxMesh unitBox = new() { Size = Vector3.One };
		SurfaceTool surfaceTool = new();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		foreach (Transform3D transform in transforms)
		{
			surfaceTool.AppendFrom(unitBox, 0, transform);
		}
		ArrayMesh mesh = surfaceTool.Commit();
		Material material = DuplicateOrFallback(
			sourceMaterial,
			fallbackColor,
			roughness,
			metallic);
		if (tintSource && material is BaseMaterial3D baseMaterial)
		{
			baseMaterial.AlbedoColor = fallbackColor;
		}
		mesh.SurfaceSetMaterial(0, material);

		MeshInstance3D instance = new()
		{
			Name = nodeName,
			Mesh = mesh,
			CastShadow = castShadow
				? GeometryInstance3D.ShadowCastingSetting.On
				: GeometryInstance3D.ShadowCastingSetting.Off,
			VisibilityRangeEnd = 175.0f,
			VisibilityRangeEndMargin = 14.0f,
		};
		instance.SetMeta("batched_primitive_count", transforms.Count);
		parent.AddChild(instance);
	}

	private static Transform3D BoxTransform(
		Vector3 position,
		Vector3 size,
		float yaw = 0.0f) =>
		new(new Basis(Vector3.Up, yaw).Scaled(size), position);

	private static Material DuplicateOrFallback(
		Material? source,
		Color fallbackColor,
		float roughness,
		float metallic = 0.0f)
	{
		if (source?.Duplicate() is Material duplicate)
		{
			return duplicate;
		}

		return new StandardMaterial3D
		{
			AlbedoColor = fallbackColor,
			Roughness = roughness,
			Metallic = metallic,
		};
	}

	private static ArrayMesh CreateGroundDiamondMesh(
		Material material,
		float halfLength,
		float halfWidth)
	{
		SurfaceTool surfaceTool = new();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		Vector3[] vertices =
		{
			new(-halfLength, 0.0f, 0.0f),
			new(0.0f, 0.002f, halfWidth),
			new(halfLength, 0.0f, 0.0f),
			new(0.0f, 0.002f, -halfWidth),
		};
		for (int index = 0; index < vertices.Length; index++)
		{
			surfaceTool.SetNormal(Vector3.Up);
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

	private static ArrayMesh CreateWeedMesh(Material material)
	{
		SurfaceTool surfaceTool = new();
		surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
		for (int plane = 0; plane < 3; plane++)
		{
			float angle = plane * (Mathf.Pi / 3.0f);
			Vector3 right = new(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
			Vector3 forward = new(-right.Z, 0.0f, right.X);
			Vector3[] vertices =
			{
				-right * 0.18f,
				right * 0.18f,
				(forward * 0.035f) + (Vector3.Up * 0.56f),
			};
			foreach (Vector3 vertex in vertices)
			{
				surfaceTool.SetNormal(forward);
				surfaceTool.AddVertex(vertex);
			}
		}
		ArrayMesh mesh = surfaceTool.Commit();
		mesh.SurfaceSetMaterial(0, material);
		return mesh;
	}

	private static float SampleRoadCentre(float direction, float t)
	{
		float eased = SmoothStep(t);
		float bend = Mathf.Sin((t * 1.45f) + (direction > 0.0f ? 0.25f : 1.1f));
		return direction * eased * bend * 0.78f;
	}

	private static float SampleRoadGrade(float absoluteX)
	{
		float t = SmoothStep(Mathf.InverseLerp(110.0f, 218.0f, absoluteX));
		return t * (0.68f + (Mathf.Sin(absoluteX * 0.041f) * 0.14f));
	}

	private static float SmoothStep(float value)
	{
		float clamped = Mathf.Clamp(value, 0.0f, 1.0f);
		return clamped * clamped * (3.0f - (2.0f * clamped));
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
}
