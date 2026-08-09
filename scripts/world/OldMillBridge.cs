#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Procedurally builds the Old Mill Bridge landmark: the Blackwater River gorge,
/// the water surface, a green-painted steel Parker through-truss road bridge, the
/// derelict Old Mill on the east bank, and the abandoned checkpoint dressing.
///
/// Geometry is generated at runtime rather than authored as a .tscn because the
/// truss alone is ~450 members; emitting them as MultiMesh instances keeps the
/// scene file small and the draw-call count low enough for the GL Compatibility
/// renderer on integrated graphics.
///
/// Coordinates are in Main Street world space:
///   - Main Street road spans X [-110, +110], Z [-5.8, +5.8], top surface Y = 0.1
///   - Ground top surface Y = 0.0
/// The bridge extends WEST of Main Street toward the Farm District, matching the
/// county planning map where Old Mill Bridge crosses the Blackwater River.
/// </summary>
[Tool]
public partial class OldMillBridge : Node3D
{
	/// <summary>
	/// Uses the streamed county's terrain, river and vegetation instead of the
	/// self-contained backdrop used by the standalone bridge review scene.
	/// </summary>
	[Export] public bool CountyIntegrationMode { get; set; }

	// ---- Channel / gorge -------------------------------------------------
	public const float ChannelCenterX = -176.0f;
	public const float MeanderAmplitude = 6.0f;
	public const float MeanderPeriod = 45.0f;
	public const float RiverbedY = -10.5f;
	public const float WaterY = -8.5f;

	// Terrain footprint. The east edge stops exactly at Main Street's ground
	// plane edge (186.5 wide, centred on origin => -93.25) so the two surfaces
	// share an edge instead of z-fighting on an overlap.
	public const float TerrainMinX = -330.0f;
	public const float TerrainMaxX = -93.25f;
	public const float TerrainHalfZ = 150.0f;

	// ---- Bridge ----------------------------------------------------------
	public const float DeckTopY = 0.1f;          // flush with Main Street asphalt
	public const float SpanEastX = -140.0f;      // east abutment face
	public const float SpanWestX = -212.0f;      // west abutment face
	public const int PanelCount = 8;
	public const float TrussHalfZ = 7.0f;        // truss plane offset from centreline
	public const float RoadHalfZ = 5.8f;         // matches Main Street road width

	private const float BottomChordY = DeckTopY - 0.15f;

	private readonly List<Transform3D> _steel = new();
	private readonly List<Transform3D> _concrete = new();
	private readonly List<Transform3D> _stone = new();
	private readonly List<Transform3D> _timber = new();

	private Material _steelMaterial = null!;
	private Material _concreteMaterial = null!;
	private Material _stoneMaterial = null!;
	private Material _timberMaterial = null!;
	private Material _asphaltMaterial = null!;
	private Material _rockMaterial = null!;
	private Material _grassMaterial = null!;
	private Material _waterMaterial = null!;

	public override void _Ready()
	{
		// Marked [Tool] so the landmark is visible in the editor viewport as well as
		// at runtime. Every child below is generated, and none is given an Owner, so
		// nothing is ever serialised back into old_mill_bridge.tscn - the scene file
		// stays a single scripted node. Clearing first keeps repeated editor reloads
		// from stacking duplicate copies of the whole location on top of each other.
		ClearGenerated();

		LoadMaterials();

		if (!CountyIntegrationMode)
		{
			BuildTerrain();
			BuildWater();
		}
		BuildRoadway();
		BuildAbutments();
		BuildTruss();
		BuildGuardrails();
		BuildOldMill();
		BuildCheckpointDressing();

		// Everything accumulated by the builders above is emitted as four
		// MultiMesh batches - one draw call per material.
		EmitBatch("SteelMembers", _steel, _steelMaterial);
		EmitBatch("ConcreteMasses", _concrete, _concreteMaterial);
		EmitBatch("StoneMasses", _stone, _stoneMaterial);
		EmitBatch("TimberMembers", _timber, _timberMaterial);

		if (!CountyIntegrationMode)
		{
			ScatterVegetation();
		}
		BuildCollision();
	}

	/// <summary>
	/// Removes every previously generated child. All children of this node are
	/// produced by the builders below, so this is a full reset.
	/// </summary>
	private void ClearGenerated()
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}

		_steel.Clear();
		_concrete.Clear();
		_stone.Clear();
		_timber.Clear();
	}

	private void LoadMaterials()
	{
		_steelMaterial = GD.Load<Material>("res://assets/materials/ashwood_police_rusty_metal.tres");
		_concreteMaterial = GD.Load<Material>("res://assets/materials/ashwood_main_street_concrete.tres");
		_stoneMaterial = GD.Load<Material>("res://assets/materials/miller_hardware_brick.tres");
		_timberMaterial = GD.Load<Material>("res://assets/materials/ashwood_police_dark_wood.tres");
		_asphaltMaterial = GD.Load<Material>("res://assets/materials/ashwood_main_street_asphalt.tres");
		_rockMaterial = GD.Load<Material>("res://assets/materials/blackwater_gorge_rock.tres");
		_grassMaterial = GD.Load<Material>("res://assets/materials/ashwood_main_street_grass.tres");
		_waterMaterial = GD.Load<Material>("res://assets/materials/blackwater_river.tres");
	}

	// ======================================================================
	// Terrain
	// ======================================================================

	/// <summary>
	/// Centre line of the river channel at a given Z. The two sine terms give the
	/// gorge a meander so it never reads as a straight trench.
	/// The water shader mirrors this function so shore foam tracks the real waterline.
	/// </summary>
	public static float ChannelCenterAt(float z)
	{
		return ChannelCenterX
			+ MeanderAmplitude * Mathf.Sin(z / MeanderPeriod)
			+ 2.5f * Mathf.Sin(z / 17.0f);
	}

	/// <summary>
	/// Gorge cross-section profile. Flat town ground beyond d=36, then a rim roll-off,
	/// a steep rocky bank (~42 degrees, deliberately under the character controller's
	/// 45 degree floor limit so a player who slides in can climb back out), a shore
	/// shelf, and the riverbed.
	/// </summary>
	public static float GorgeHeight(float x, float z)
	{
		float d = Mathf.Abs(x - ChannelCenterAt(z));

		float h;
		if (d <= 20.0f)
		{
			h = RiverbedY;
		}
		else if (d <= 26.0f)
		{
			h = Mathf.Lerp(RiverbedY, -8.0f, Smooth((d - 20.0f) / 6.0f));
		}
		else if (d <= 32.0f)
		{
			h = Mathf.Lerp(-8.0f, -2.5f, Smooth((d - 26.0f) / 6.0f));
		}
		else if (d <= 36.0f)
		{
			h = Mathf.Lerp(-2.5f, 0.0f, Smooth((d - 32.0f) / 4.0f));
		}
		else
		{
			return RollingGround(x, z);
		}

		// Blend the surrounding landform in as the profile reaches the rim so the
		// gorge lip meets the open ground without a step.
		h += RollingGround(x, z) * Smooth((d - 30.0f) / 6.0f);

		// Rock character on the banks only. The falloff is forced to zero by d=36
		// and at the riverbed so the profile still meets the town ground exactly
		// and the water plane never pokes through the bed.
		float bankBlend = Mathf.Sin(Mathf.Pi * Mathf.Clamp((d - 18.0f) / 18.0f, 0.0f, 1.0f));

		// Bedding planes. Quantising the bank toward horizontal ledges with steep
		// risers makes the cliff cast its own shadows; without this the bank is a
		// smooth ramp that catches flat sky ambient and reads as snow, not rock.
		const float ledge = 1.45f;
		float ledgeBase = Mathf.Floor(h / ledge) * ledge;
		float ledgeFraction = (h - ledgeBase) / ledge;
		float terraced = ledgeBase
			+ ledge * Smooth(Mathf.Clamp(ledgeFraction * 1.85f - 0.42f, 0.0f, 1.0f));
		h = Mathf.Lerp(h, terraced, 0.72f * bankBlend);

		float detail =
			0.85f * Mathf.Sin(x * 0.31f + z * 0.17f)
			+ 0.55f * Mathf.Sin(x * 0.73f - z * 0.41f)
			+ 0.30f * Mathf.Sin(x * 1.47f + z * 1.13f);
		h += detail * bankBlend * 0.75f;

		return h;
	}

	private static float Smooth(float t)
	{
		t = Mathf.Clamp(t, 0.0f, 1.0f);
		return t * t * (3.0f - 2.0f * t);
	}

	/// <summary>
	/// Gentle rolling landform for the open ground either side of the gorge.
	/// Without this the whole county outside the channel is a mathematically flat
	/// plane, which is the single most artificial thing in a wide shot.
	///
	/// The field is forced flat along the road corridor (so the carriageway never
	/// undulates) and at the eastern edge (so it meets Main Street's ground plane
	/// at exactly Y = 0).
	/// </summary>
	private static float RollingGround(float x, float z)
	{
		// Flat under the road and shoulders, rising to full amplitude out in the fields.
		float corridor = Smooth((Mathf.Abs(z) - 14.0f) / 22.0f);

		// Flat where the terrain hands over to Main Street.
		float junction = Smooth((-x - 118.0f) / 42.0f);

		float field =
			1.45f * Mathf.Sin(x * 0.021f + z * 0.017f)
			+ 0.85f * Mathf.Sin(x * 0.041f - z * 0.033f)
			+ 0.45f * Mathf.Sin(x * 0.087f + z * 0.062f)
			+ 0.22f * Mathf.Sin(x * 0.163f - z * 0.131f);

		return field * corridor * junction;
	}

	private void BuildTerrain()
	{
		// ~2m cells over the whole footprint: fine enough to hold the ledge detail
		// on the banks, coarse enough to stay around 31k triangles.
		const int stepsX = 120;
		const int stepsZ = 130;
		float dx = (TerrainMaxX - TerrainMinX) / stepsX;
		float dz = (TerrainHalfZ * 2.0f) / stepsZ;

		var surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		for (int ix = 0; ix < stepsX; ix++)
		{
			float x0 = TerrainMinX + ix * dx;
			float x1 = x0 + dx;
			for (int iz = 0; iz < stepsZ; iz++)
			{
				float z0 = -TerrainHalfZ + iz * dz;
				float z1 = z0 + dz;

				Vector3 v00 = new(x0, GorgeHeight(x0, z0), z0);
				Vector3 v01 = new(x0, GorgeHeight(x0, z1), z1);
				Vector3 v10 = new(x1, GorgeHeight(x1, z0), z0);
				Vector3 v11 = new(x1, GorgeHeight(x1, z1), z1);

				// Winding chosen so face normals point up (+Y).
				AddTerrainVertex(surface, v00);
				AddTerrainVertex(surface, v11);
				AddTerrainVertex(surface, v10);

				AddTerrainVertex(surface, v00);
				AddTerrainVertex(surface, v01);
				AddTerrainVertex(surface, v11);
			}
		}

		surface.GenerateNormals();
		surface.GenerateTangents();
		ArrayMesh mesh = surface.Commit();

		// Two passes: rock everywhere, then grass drawn only on the near-flat rim.
		// Splitting by slope in geometry avoids needing a custom terrain shader,
		// which the Compatibility renderer would make awkward.
		var rock = new MeshInstance3D
		{
			Name = "GorgeRock",
			Mesh = mesh,
			MaterialOverride = _rockMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
		};
		AddChild(rock);

		BuildGroundCover();
	}

	private static void AddTerrainVertex(SurfaceTool surface, Vector3 v)
	{
		surface.SetUV(new Vector2(v.X * 0.06f, v.Z * 0.06f));
		surface.AddVertex(v);
	}

	/// <summary>
	/// Grass cover laid over the rock shell wherever the ground is shallow enough
	/// to hold soil. Selection is by local slope rather than by distance from the
	/// channel, so grass also settles on the flat ledges and shore shelves inside
	/// the gorge, and the boundary is broken up by a patchiness field so it never
	/// reads as a rectangle.
	/// </summary>
	private void BuildGroundCover()
	{
		const int stepsX = 150;
		const int stepsZ = 160;
		float dx = (TerrainMaxX - TerrainMinX) / stepsX;
		float dz = (TerrainHalfZ * 2.0f) / stepsZ;

		var surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);
		bool any = false;

		for (int ix = 0; ix < stepsX; ix++)
		{
			float x0 = TerrainMinX + ix * dx;
			float x1 = x0 + dx;
			for (int iz = 0; iz < stepsZ; iz++)
			{
				float z0 = -TerrainHalfZ + iz * dz;
				float z1 = z0 + dz;

				// The road corridor stays bare.
				if (Mathf.Abs(z0) < 10.5f || Mathf.Abs(z1) < 10.5f)
				{
					continue;
				}

				float h00 = GorgeHeight(x0, z0);
				float h01 = GorgeHeight(x0, z1);
				float h10 = GorgeHeight(x1, z0);
				float h11 = GorgeHeight(x1, z1);

				float lo = Mathf.Min(Mathf.Min(h00, h01), Mathf.Min(h10, h11));
				float hi = Mathf.Max(Mathf.Max(h00, h01), Mathf.Max(h10, h11));

				// Too steep to hold soil, or below the waterline.
				if (hi - lo > 0.85f || lo < WaterY + 0.35f)
				{
					continue;
				}

				// Organic coverage boundary.
				float cx = (x0 + x1) * 0.5f;
				float cz = (z0 + z1) * 0.5f;
				if (Patchiness(cx, cz) < 0.34f)
				{
					continue;
				}

				const float lift = 0.04f;
				Vector3 v00 = new(x0, h00 + lift, z0);
				Vector3 v01 = new(x0, h01 + lift, z1);
				Vector3 v10 = new(x1, h10 + lift, z0);
				Vector3 v11 = new(x1, h11 + lift, z1);

				AddTerrainVertex(surface, v00);
				AddTerrainVertex(surface, v11);
				AddTerrainVertex(surface, v10);

				AddTerrainVertex(surface, v00);
				AddTerrainVertex(surface, v01);
				AddTerrainVertex(surface, v11);
				any = true;
			}
		}

		if (!any)
		{
			return;
		}

		surface.GenerateNormals();
		surface.GenerateTangents();
		AddChild(new MeshInstance3D
		{
			Name = "GroundCover",
			Mesh = surface.Commit(),
			MaterialOverride = _grassMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});
	}

	/// <summary>Smooth 0..1 field used to give cover and scatter an organic boundary.</summary>
	private static float Patchiness(float x, float z)
	{
		float v =
			0.55f * Mathf.Sin(x * 0.055f + z * 0.031f)
			+ 0.30f * Mathf.Sin(x * 0.11f - z * 0.09f)
			+ 0.15f * Mathf.Sin(x * 0.23f + z * 0.19f);
		return 0.5f + 0.5f * v;
	}

	private void BuildWater()
	{
		var plane = new PlaneMesh
		{
			Size = new Vector2(70.0f, 210.0f),
			SubdivideWidth = 8,
			SubdivideDepth = 24,
		};

		AddChild(new MeshInstance3D
		{
			Name = "BlackwaterSurface",
			Mesh = plane,
			MaterialOverride = _waterMaterial,
			Position = new Vector3(ChannelCenterX, WaterY, 0.0f),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});
	}

	// ======================================================================
	// Roadway
	// ======================================================================

	private void BuildRoadway()
	{
		// Approach roads continue Main Street's asphalt to each abutment.
		AddSlab("ApproachEast", new Vector3(-125.0f, 0.05f, 0.0f),
			new Vector3(30.0f, 0.1f, 11.6f), _asphaltMaterial);
		AddSlab("ApproachWest", new Vector3(-231.0f, 0.05f, 0.0f),
			new Vector3(38.0f, 0.1f, 11.6f), _asphaltMaterial);

		// Bridge deck: structural slab, then the wearing course on top so the
		// finished surface lands exactly on Main Street's Y = 0.1.
		float spanLength = SpanEastX - SpanWestX;
		float spanMidX = (SpanEastX + SpanWestX) * 0.5f;

		AddSlab("DeckSlab", new Vector3(spanMidX, DeckTopY - 0.25f, 0.0f),
			new Vector3(spanLength, 0.4f, 14.4f), _concreteMaterial);
		AddSlab("DeckWearingCourse", new Vector3(spanMidX, DeckTopY - 0.03f, 0.0f),
			new Vector3(spanLength, 0.06f, 11.6f), _asphaltMaterial);

		// Raised walkways between the road edge and each truss line.
		for (int side = -1; side <= 1; side += 2)
		{
			AddSlab($"DeckWalkway{(side < 0 ? "North" : "South")}",
				new Vector3(spanMidX, DeckTopY + 0.06f, side * 6.35f),
				new Vector3(spanLength, 0.24f, 1.1f), _concreteMaterial);
		}

		BuildRoadMarkings();
	}

	/// <summary>
	/// Faded centre and edge lines. Beyond realism these do real readability work:
	/// they tell the player at a glance that the crossing is the road continuing,
	/// and they give the eye a leading line toward the truss portal.
	/// </summary>
	private void BuildRoadMarkings()
	{
		var paint = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.62f, 0.58f, 0.42f),
			Roughness = 0.92f,
			Metallic = 0.0f,
		};

		var markings = new Node3D { Name = "RoadMarkings" };
		AddChild(markings);

		var centreLine = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			Mesh = new BoxMesh { Size = Vector3.One },
		};

		var dashes = new List<Transform3D>();
		for (float x = -248.0f; x < -111.0f; x += 9.0f)
		{
			// Skip the dashes that would sit inside an abutment joint.
			if ((x > SpanEastX - 2.0f && x < SpanEastX + 2.0f) ||
				(x > SpanWestX - 2.0f && x < SpanWestX + 2.0f))
			{
				continue;
			}
			AddBox(dashes, new Vector3(x, DeckTopY + 0.011f, 0.0f),
				new Vector3(3.2f, 0.02f, 0.16f));
		}

		centreLine.InstanceCount = dashes.Count;
		for (int i = 0; i < dashes.Count; i++)
		{
			centreLine.SetInstanceTransform(i, dashes[i]);
		}

		markings.AddChild(new MultiMeshInstance3D
		{
			Name = "CentreLine",
			Multimesh = centreLine,
			MaterialOverride = paint,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		});

		// Continuous edge lines.
		foreach (float z in new[] { -5.05f, 5.05f })
		{
			markings.AddChild(new MeshInstance3D
			{
				Name = $"EdgeLine{(z < 0 ? "North" : "South")}",
				Mesh = new BoxMesh { Size = new Vector3(138.0f, 0.02f, 0.14f) },
				MaterialOverride = paint,
				Position = new Vector3(-179.0f, DeckTopY + 0.011f, z),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			});
		}
	}

	private void AddSlab(string name, Vector3 center, Vector3 size, Material material)
	{
		AddChild(new MeshInstance3D
		{
			Name = name,
			Mesh = new BoxMesh { Size = size },
			MaterialOverride = material,
			Position = center,
		});
	}

	// ======================================================================
	// Abutments and piers
	// ======================================================================

	private void BuildAbutments()
	{
		foreach (float faceX in new[] { SpanEastX, SpanWestX })
		{
			float outward = faceX == SpanEastX ? 1.0f : -1.0f;

			// Main abutment mass, buried into the bank.
			AddBox(_concrete,
				new Vector3(faceX + outward * 3.5f, -5.0f, 0.0f),
				new Vector3(7.0f, 11.0f, 16.0f));

			// Bearing seat the truss sits on.
			AddBox(_concrete,
				new Vector3(faceX + outward * 1.2f, DeckTopY - 0.75f, 0.0f),
				new Vector3(3.0f, 1.4f, 15.4f));

			// Flared wing walls retaining the approach embankment.
			for (int side = -1; side <= 1; side += 2)
			{
				AddBox(_concrete,
					new Vector3(faceX + outward * 6.5f, -2.4f, side * 7.6f),
					new Vector3(9.0f, 6.0f, 1.0f));
			}
		}

		// Remnant stone piers from the original 19th-century crossing, left standing
		// in the river. Environmental storytelling: the mill and the first bridge
		// predate the steel span.
		AddBox(_stone, new Vector3(ChannelCenterX - 9.0f, -9.4f, 6.0f),
			new Vector3(3.4f, 3.6f, 6.0f));
		AddBox(_stone, new Vector3(ChannelCenterX + 11.0f, -9.8f, -4.5f),
			new Vector3(3.0f, 2.6f, 5.4f));
	}

	// ======================================================================
	// Steel truss
	// ======================================================================

	private void BuildTruss()
	{
		float panel = (SpanEastX - SpanWestX) / PanelCount;

		// Panel point X positions, east to west.
		var px = new float[PanelCount + 1];
		for (int k = 0; k <= PanelCount; k++)
		{
			px[k] = SpanEastX - panel * k;
		}

		// Parker truss: polygonal top chord, deepest at midspan.
		var topY = new float[PanelCount + 1];
		for (int k = 0; k <= PanelCount; k++)
		{
			topY[k] = BottomChordY + 5.0f + 4.2f * Mathf.Sin(Mathf.Pi * k / PanelCount);
		}

		foreach (int sideIndex in new[] { -1, 1 })
		{
			float z = sideIndex * TrussHalfZ;

			// Bottom chord, one member per panel.
			for (int k = 0; k < PanelCount; k++)
			{
				AddBeam(_steel,
					new Vector3(px[k], BottomChordY, z),
					new Vector3(px[k + 1], BottomChordY, z),
					0.50f, 0.60f);
			}

			// Inclined end posts.
			AddBeam(_steel,
				new Vector3(px[0], BottomChordY, z),
				new Vector3(px[1], topY[1], z), 0.52f, 0.58f);
			AddBeam(_steel,
				new Vector3(px[PanelCount], BottomChordY, z),
				new Vector3(px[PanelCount - 1], topY[PanelCount - 1], z), 0.52f, 0.58f);

			// Top chord between interior panel points.
			for (int k = 1; k < PanelCount - 1; k++)
			{
				AddBeam(_steel,
					new Vector3(px[k], topY[k], z),
					new Vector3(px[k + 1], topY[k + 1], z),
					0.46f, 0.52f);
			}

			// Verticals and web diagonals.
			for (int k = 1; k <= PanelCount - 1; k++)
			{
				AddBeam(_steel,
					new Vector3(px[k], BottomChordY, z),
					new Vector3(px[k], topY[k], z),
					0.30f, 0.30f);

				// Gusset plate at each top and bottom joint.
				AddBox(_steel, new Vector3(px[k], topY[k], z), new Vector3(1.5f, 1.2f, 0.10f));
				AddBox(_steel, new Vector3(px[k], BottomChordY, z), new Vector3(1.6f, 1.3f, 0.10f));
			}

			// Pratt web: diagonals fall toward midspan from both ends.
			for (int p = 1; p <= PanelCount - 2; p++)
			{
				bool eastHalf = p < PanelCount / 2;
				Vector3 a = eastHalf
					? new Vector3(px[p], topY[p], z)
					: new Vector3(px[p], BottomChordY, z);
				Vector3 b = eastHalf
					? new Vector3(px[p + 1], BottomChordY, z)
					: new Vector3(px[p + 1], topY[p + 1], z);
				AddBeam(_steel, a, b, 0.26f, 0.26f);
			}
		}

		BuildTrussBracing(px, topY);
		BuildFloorSystem(px);
	}

	private void BuildTrussBracing(float[] px, float[] topY)
	{
		// Transverse struts joining the two top chords at each interior panel point.
		for (int k = 1; k <= PanelCount - 1; k++)
		{
			AddBeam(_steel,
				new Vector3(px[k], topY[k], -TrussHalfZ),
				new Vector3(px[k], topY[k], TrussHalfZ),
				0.24f, 0.24f);
		}

		// Top lateral X-bracing between panel points.
		for (int p = 1; p <= PanelCount - 2; p++)
		{
			AddBeam(_steel,
				new Vector3(px[p], topY[p] - 0.15f, -TrussHalfZ),
				new Vector3(px[p + 1], topY[p + 1] - 0.15f, TrussHalfZ),
				0.16f, 0.16f);
			AddBeam(_steel,
				new Vector3(px[p], topY[p] - 0.15f, TrussHalfZ),
				new Vector3(px[p + 1], topY[p + 1] - 0.15f, -TrussHalfZ),
				0.16f, 0.16f);
		}

		// Portal frames at both entrances: the visual "gateway" read of a through truss.
		foreach (int end in new[] { 1, PanelCount - 1 })
		{
			float y = topY[end];
			AddBeam(_steel,
				new Vector3(px[end], y - 0.55f, -TrussHalfZ),
				new Vector3(px[end], y - 0.55f, TrussHalfZ),
				0.55f, 0.80f);

			// Knee braces angling in from each truss line.
			for (int side = -1; side <= 1; side += 2)
			{
				AddBeam(_steel,
					new Vector3(px[end], y - 1.1f, side * TrussHalfZ),
					new Vector3(px[end], y - 2.6f, side * (TrussHalfZ - 1.9f)),
					0.22f, 0.22f);
			}
		}
	}

	private void BuildFloorSystem(float[] px)
	{
		// Transverse floor beams at every panel point.
		for (int k = 0; k <= PanelCount; k++)
		{
			AddBeam(_steel,
				new Vector3(px[k], BottomChordY - 0.55f, -TrussHalfZ),
				new Vector3(px[k], BottomChordY - 0.55f, TrussHalfZ),
				0.40f, 0.62f);
		}

		// Longitudinal stringers carrying the deck between floor beams.
		foreach (float z in new[] { -4.6f, -1.6f, 1.6f, 4.6f })
		{
			AddBeam(_steel,
				new Vector3(SpanEastX, BottomChordY - 0.32f, z),
				new Vector3(SpanWestX, BottomChordY - 0.32f, z),
				0.24f, 0.34f);
		}
	}

	// ======================================================================
	// Guardrails on the approaches
	// ======================================================================

	private void BuildGuardrails()
	{
		BuildGuardrailRun(-110.0f, SpanEastX);
		BuildGuardrailRun(SpanWestX, -248.0f);
	}

	private void BuildGuardrailRun(float fromX, float toX)
	{
		float step = fromX < toX ? 4.0f : -4.0f;
		int count = Mathf.Abs(Mathf.RoundToInt((toX - fromX) / 4.0f));

		for (int side = -1; side <= 1; side += 2)
		{
			float z = side * 6.6f;

			for (int i = 0; i <= count; i++)
			{
				float x = fromX + step * i;
				AddBox(_timber, new Vector3(x, 0.45f, z), new Vector3(0.18f, 1.1f, 0.18f));
			}

			// W-beam rail run.
			AddBeam(_steel,
				new Vector3(fromX, 0.78f, z),
				new Vector3(toX, 0.78f, z),
				0.10f, 0.34f);
		}
	}

	// ======================================================================
	// The Old Mill
	// ======================================================================

	private void BuildOldMill()
	{
		// Sited on the east rim overlooking the river, north of the road, on a
		// stone plinth that steps down the bank - this is what the crossing is
		// named for and it anchors the view from the bridge deck.
		const float millZ = -38.0f;

		// Retaining plinth carrying the building out over the slope.
		AddBox(_stone, new Vector3(-143.0f, -3.4f, millZ), new Vector3(19.0f, 7.6f, 21.0f));

		// Ground-floor stone shell, built as wall segments so door and window
		// openings read as real gaps rather than decals.
		const float wallBaseY = 0.4f;
		const float wallTopY = 4.9f;
		float wallH = wallTopY - wallBaseY;
		float wallMidY = (wallBaseY + wallTopY) * 0.5f;

		// North and south long walls (running along X).
		foreach (float z in new[] { millZ - 8.0f, millZ + 8.0f })
		{
			AddBox(_stone, new Vector3(-149.0f, wallMidY, z), new Vector3(5.0f, wallH, 0.7f));
			AddBox(_stone, new Vector3(-143.5f, wallTopY - 0.6f, z), new Vector3(6.0f, 1.2f, 0.7f));
			AddBox(_stone, new Vector3(-143.5f, wallBaseY + 0.6f, z), new Vector3(6.0f, 1.2f, 0.7f));
			AddBox(_stone, new Vector3(-138.0f, wallMidY, z), new Vector3(5.0f, wallH, 0.7f));
		}

		// East gable wall (facing town) with the cart doorway.
		AddBox(_stone, new Vector3(-135.5f, wallMidY, millZ - 5.4f), new Vector3(0.7f, wallH, 5.2f));
		AddBox(_stone, new Vector3(-135.5f, wallMidY, millZ + 5.4f), new Vector3(0.7f, wallH, 5.2f));
		AddBox(_stone, new Vector3(-135.5f, wallTopY - 0.7f, millZ), new Vector3(0.7f, 1.4f, 5.6f));

		// West wall over the river, partly collapsed - the gap is the ruin's story.
		AddBox(_stone, new Vector3(-151.5f, wallMidY, millZ - 5.6f), new Vector3(0.7f, wallH, 4.8f));
		AddBox(_stone, new Vector3(-151.5f, wallBaseY + 1.1f, millZ + 3.0f), new Vector3(0.7f, 2.2f, 10.0f));

		// Rubble spilling from the collapse.
		AddBox(_stone, new Vector3(-153.4f, -1.2f, millZ + 4.5f), new Vector3(3.0f, 1.1f, 4.0f));
		AddBox(_stone, new Vector3(-154.8f, -2.6f, millZ + 2.0f), new Vector3(2.2f, 0.9f, 2.6f));

		BuildMillUpperStorey(millZ, wallTopY);
		BuildWaterWheel(millZ);
	}

	private void BuildMillUpperStorey(float millZ, float wallTopY)
	{
		// Timber-framed upper floor, half gone. Surviving posts and a sagging
		// ridge beam give the silhouette its broken profile.
		float postTop = wallTopY + 3.4f;

		for (int i = 0; i < 5; i++)
		{
			float x = -149.0f + i * 3.2f;
			// The two westernmost posts are snapped short.
			float top = i < 2 ? wallTopY + 1.3f : postTop;
			foreach (float z in new[] { millZ - 7.6f, millZ + 7.6f })
			{
				AddBox(_timber, new Vector3(x, (wallTopY + top) * 0.5f, z),
					new Vector3(0.32f, top - wallTopY, 0.32f));
			}
		}

		// Surviving roof structure over the eastern half only.
		AddBeam(_timber,
			new Vector3(-141.0f, postTop + 1.5f, millZ),
			new Vector3(-135.5f, postTop + 1.5f, millZ),
			0.34f, 0.42f);

		for (int i = 0; i < 5; i++)
		{
			float x = -141.0f + i * 1.4f;
			foreach (int side in new[] { -1, 1 })
			{
				AddBeam(_timber,
					new Vector3(x, postTop + 1.5f, millZ),
					new Vector3(x, postTop - 0.2f, millZ + side * 7.6f),
					0.16f, 0.22f);
			}
		}

		// Collapsed rafters lying at an angle across the open western bay.
		AddBeam(_timber,
			new Vector3(-150.5f, wallTopY + 0.4f, millZ - 6.0f),
			new Vector3(-145.0f, wallTopY - 3.2f, millZ + 1.5f),
			0.22f, 0.28f);
		AddBeam(_timber,
			new Vector3(-149.0f, wallTopY + 1.0f, millZ + 5.0f),
			new Vector3(-144.0f, wallTopY - 3.6f, millZ - 2.0f),
			0.20f, 0.26f);
	}

	private void BuildWaterWheel(float millZ)
	{
		// Overshot wheel hung on the river face, fed by a timber flume from upstream.
		var hub = new Vector3(-153.6f, -2.2f, millZ - 1.0f);
		const float radius = 3.4f;
		const int paddles = 16;

		// Axle.
		AddBeam(_timber,
			hub + new Vector3(0.0f, 0.0f, -1.6f),
			hub + new Vector3(0.0f, 0.0f, 1.6f),
			0.34f, 0.34f);

		for (int i = 0; i < paddles; i++)
		{
			float a = Mathf.Tau * i / paddles;
			var outer = new Vector3(
				hub.X + Mathf.Cos(a) * radius,
				hub.Y + Mathf.Sin(a) * radius,
				hub.Z);
			float aNext = Mathf.Tau * (i + 1) / paddles;
			var outerNext = new Vector3(
				hub.X + Mathf.Cos(aNext) * radius,
				hub.Y + Mathf.Sin(aNext) * radius,
				hub.Z);

			// Two rims, fore and aft.
			foreach (float zOff in new[] { -1.3f, 1.3f })
			{
				AddBeam(_timber,
					outer + new Vector3(0.0f, 0.0f, zOff),
					outerNext + new Vector3(0.0f, 0.0f, zOff),
					0.16f, 0.20f);
			}

			// Spokes on every second position, and paddle boards between rims.
			if (i % 2 == 0)
			{
				AddBeam(_timber, hub, outer, 0.14f, 0.14f);
			}

			var mid = (outer + outerNext) * 0.5f;
			AddBox(_timber, mid, new Vector3(0.7f, 0.10f, 2.6f));
		}

		// Head race flume on trestles, running in from upstream.
		AddBeam(_timber,
			new Vector3(-150.0f, 1.4f, millZ - 14.0f),
			new Vector3(-153.6f, 1.2f, millZ - 2.6f),
			1.5f, 0.5f);
		for (int i = 0; i < 4; i++)
		{
			float t = i / 3.0f;
			var p = new Vector3(
				Mathf.Lerp(-150.0f, -153.6f, t),
				0.0f,
				Mathf.Lerp(millZ - 14.0f, millZ - 2.6f, t));
			float groundY = GorgeHeight(p.X, p.Z);
			AddBox(_timber, new Vector3(p.X, (groundY + 1.0f) * 0.5f, p.Z),
				new Vector3(0.26f, 1.0f - groundY, 0.26f));
		}
	}

	// ======================================================================
	// Abandoned checkpoint
	// ======================================================================

	private void BuildCheckpointDressing()
	{
		// A county roadblock that was set up at the east portal and then abandoned.
		// Kept deliberately sparse: design canon calls for "life interrupted",
		// not a war zone, and the crossing has to stay walkable for the player
		// and navigable for zombies.
		const float portalX = -146.0f;

		// Jersey barriers staggered across the eastbound lane, leaving a gap.
		for (int i = 0; i < 4; i++)
		{
			float z = -5.0f + i * 2.4f;
			if (i == 2)
			{
				continue; // the gap survivors squeezed through
			}
			AddBox(_concrete, new Vector3(portalX + (i % 2) * 1.1f, 0.55f, z),
				new Vector3(0.8f, 0.9f, 2.0f));
		}

		// A second, looser line further out on the approach.
		for (int i = 0; i < 3; i++)
		{
			AddBox(_concrete, new Vector3(-131.0f - i * 0.6f, 0.5f, -3.4f + i * 3.2f),
				new Vector3(0.75f, 0.85f, 1.9f));
		}

		// One barrier shoved aside and toppled.
		AddBox(_concrete, new Vector3(-134.0f, 0.42f, 4.8f), new Vector3(1.9f, 0.8f, 0.75f));

		// Timber sawhorse barricades on the walkway.
		AddBox(_timber, new Vector3(-142.0f, 0.95f, 6.3f), new Vector3(2.4f, 0.12f, 0.16f));
		AddBox(_timber, new Vector3(-142.0f, 0.55f, 6.3f), new Vector3(2.4f, 0.12f, 0.16f));

		// Sandbag emplacement at the portal foot.
		for (int row = 0; row < 3; row++)
		{
			for (int i = 0; i < 5 - row; i++)
			{
				AddBox(_concrete,
					new Vector3(-143.5f + i * 0.55f + row * 0.25f, 0.18f + row * 0.28f, -6.4f),
					new Vector3(0.5f, 0.26f, 0.9f));
			}
		}
	}

	// ======================================================================
	// Collision
	// ======================================================================

	private void BuildCollision()
	{
		// Terrain: trimesh over the generated gorge shell.
		var terrainMesh = GetNodeOrNull<MeshInstance3D>("GorgeRock")?.Mesh as ArrayMesh;
		if (!CountyIntegrationMode && terrainMesh != null)
		{
			var terrainBody = new StaticBody3D { Name = "GorgeCollision" };
			terrainBody.AddChild(new CollisionShape3D
			{
				Name = "Shape",
				Shape = terrainMesh.CreateTrimeshShape(),
			});
			AddChild(terrainBody);
		}

		var road = new StaticBody3D { Name = "RoadwayCollision" };
		AddChild(road);

		AddCollisionBox(road, new Vector3(-125.0f, 0.05f, 0.0f), new Vector3(30.0f, 0.1f, 13.4f));
		AddCollisionBox(road, new Vector3(-231.0f, 0.05f, 0.0f), new Vector3(38.0f, 0.1f, 13.4f));

		float spanLength = SpanEastX - SpanWestX;
		float spanMidX = (SpanEastX + SpanWestX) * 0.5f;
		AddCollisionBox(road, new Vector3(spanMidX, DeckTopY - 0.2f, 0.0f),
			new Vector3(spanLength, 0.5f, 14.4f));

		// Parapet walls along the full crossing so the player cannot walk off the
		// deck into the gorge. Height is set above the character controller's step
		// and no jump exists, so this is a hard boundary.
		var parapet = new StaticBody3D { Name = "ParapetCollision" };
		AddChild(parapet);
		for (int side = -1; side <= 1; side += 2)
		{
			AddCollisionBox(parapet,
				new Vector3(spanMidX, DeckTopY + 0.9f, side * 7.0f),
				new Vector3(spanLength, 1.8f, 0.4f));

			// Guardrail collision on both approaches.
			AddCollisionBox(parapet,
				new Vector3(-125.0f, 0.7f, side * 6.6f), new Vector3(30.0f, 1.4f, 0.3f));
			AddCollisionBox(parapet,
				new Vector3(-230.0f, 0.7f, side * 6.6f), new Vector3(36.0f, 1.4f, 0.3f));
		}

		// Mill shell collision, simplified to the outer box plus the plinth.
		var mill = new StaticBody3D { Name = "OldMillCollision" };
		AddChild(mill);
		AddCollisionBox(mill, new Vector3(-143.0f, -3.4f, -38.0f), new Vector3(19.0f, 7.6f, 21.0f));
		AddCollisionBox(mill, new Vector3(-135.5f, 2.65f, -43.4f), new Vector3(0.7f, 4.5f, 5.2f));
		AddCollisionBox(mill, new Vector3(-135.5f, 2.65f, -32.6f), new Vector3(0.7f, 4.5f, 5.2f));
		AddCollisionBox(mill, new Vector3(-149.0f, 2.65f, -46.0f), new Vector3(5.0f, 4.5f, 0.7f));
		AddCollisionBox(mill, new Vector3(-138.0f, 2.65f, -46.0f), new Vector3(5.0f, 4.5f, 0.7f));
		AddCollisionBox(mill, new Vector3(-149.0f, 2.65f, -30.0f), new Vector3(5.0f, 4.5f, 0.7f));
		AddCollisionBox(mill, new Vector3(-138.0f, 2.65f, -30.0f), new Vector3(5.0f, 4.5f, 0.7f));
	}

	private static void AddCollisionBox(StaticBody3D body, Vector3 center, Vector3 size)
	{
		body.AddChild(new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = size },
			Position = center,
		});
	}

	// ======================================================================
	// Vegetation
	// ======================================================================

	/// <summary>
	/// One scattered vegetation layer: a set of interchangeable source scenes plus
	/// the placement rules that decide where its members are allowed to grow.
	/// </summary>
	private readonly record struct ScatterLayer(
		string Name,
		string[] Scenes,
		int Count,
		float MinScale,
		float MaxScale,
		float MaxSlopeDrop,
		float MinY,
		float MaxY,
		float VisibilityRange,
		bool CastShadow,
		float ClusterRadius,
		float SinkDepth);

	/// <summary>
	/// Scatters the decimated Poly Haven photoscans across the gorge rim and banks.
	///
	/// The stylised lowpoly trees this replaced had salmon-pink trunks and flat
	/// cartoon canopies, which destroyed the realism of every shot. The new set is
	/// photoscanned, so the budget is spent very differently: a jacaranda is ~20k
	/// triangles and 19 m tall, while a fern is 300 and a grass tuft 113. That
	/// forces the composition real forests actually have - a modest number of hero
	/// trees over a dense, cheap understorey - rather than a uniform field of
	/// mid-sized props.
	///
	/// Each species becomes one MultiMesh, so the whole forest is a handful of draw
	/// calls. Placement is rejection-sampled against slope, water level, the road
	/// corridor and the built structures, then clustered: uniformly sprinkled
	/// vegetation is one of the most reliable tells of amateur environment art.
	/// </summary>
	private void ScatterVegetation()
	{
		const string Root = "res://assets/environment/nature/polyhaven/";

		ScatterLayer[] layers =
		{
			// Hero canopy. Deliberately sparse: these are 19 m trees at ~20k
			// triangles each, so they are placed for silhouette, not for density.
			new("Canopy",
				new[] { Root + "ashwood_jacaranda_lod0.tscn" },
				10, 0.9f, 1.4f, 1.15f, -5.0f, 999.0f, 165.0f, true, 30.0f, 0.35f),

			// Second canopy rank at reduced detail, pushed further out so it fills
			// the middle distance without the near-field triangle cost.
			new("CanopyFar",
				new[] { Root + "ashwood_jacaranda_lod1.tscn" },
				16, 0.7f, 1.2f, 1.4f, -5.0f, 999.0f, 260.0f, false, 38.0f, 0.35f),

			// Dead standing timber and fallen logs - the storytelling layer that
			// makes a wood read as neglected rather than landscaped.
			new("Deadwood",
				new[]
				{
					Root + "ashwood_dead_tree_trunk.tscn",
					Root + "ashwood_dead_log.tscn",
				},
				38, 0.8f, 1.6f, 1.6f, -6.5f, 999.0f, 190.0f, true, 18.0f, 0.18f),

			// Mid understorey.
			new("Shrubs",
				new[]
				{
					Root + "ashwood_shrub_01.tscn",
					Root + "ashwood_shrub_02_a.tscn",
					Root + "ashwood_shrub_02_b.tscn",
					Root + "ashwood_shrub_02_c.tscn",
					Root + "ashwood_shrub_02_d.tscn",
				},
				360, 0.7f, 1.8f, 2.4f, WaterY + 0.4f, 999.0f, 170.0f, false, 12.0f, 0.12f),

			// Ferns and nettles gather in damp ground, so they are allowed much
			// further down the bank than anything else.
			new("Ferns",
				new[]
				{
					Root + "ashwood_fern_02_a.tscn",
					Root + "ashwood_fern_02_b.tscn",
					Root + "ashwood_fern_02_c.tscn",
					Root + "ashwood_fern_02_d.tscn",
					Root + "ashwood_nettle_tall.tscn",
					Root + "ashwood_nettle_medium.tscn",
				},
				420, 0.8f, 2.4f, 3.0f, WaterY - 0.2f, 2.5f, 95.0f, false, 9.0f, 0.10f),

			// Grass tufts are the cheapest way to kill the "bare terrain" read.
			new("GrassTufts",
				new[]
				{
					Root + "ashwood_grass_bermuda_medium.tscn",
					Root + "ashwood_grass_bermuda_small.tscn",
					Root + "ashwood_grass_bermuda_dry.tscn",
					Root + "ashwood_shrub_03_a.tscn",
					Root + "ashwood_shrub_03_b.tscn",
					Root + "ashwood_shrub_03_c.tscn",
				},
				700, 1.2f, 3.4f, 2.8f, WaterY + 0.2f, 999.0f, 62.0f, false, 7.0f, 0.06f),

			// Mossy rock. These sell the gorge as rock rather than as brown ground,
			// so they are pushed hard onto the steep bank and down to the waterline.
			// boulder_01 is excluded on purpose: it decimated to 26k triangles.
			new("Rocks",
				new[]
				{
					Root + "ashwood_rock_moss_01.tscn",
					Root + "ashwood_rock_moss_02.tscn",
					Root + "ashwood_rock_moss_03.tscn",
					Root + "ashwood_rock_moss_04.tscn",
					Root + "ashwood_rock_moss_05.tscn",
					Root + "ashwood_rock_moss_06.tscn",
				},
				150, 0.5f, 1.9f, 4.5f, WaterY - 1.2f, 999.0f, 230.0f, true, 15.0f, 0.55f),

			// Bark litter on the forest floor.
			new("Litter",
				new[]
				{
					Root + "ashwood_bark_debris_a.tscn",
					Root + "ashwood_bark_debris_b.tscn",
					Root + "ashwood_bark_debris_c.tscn",
					Root + "ashwood_bark_debris_d.tscn",
				},
				200, 0.9f, 2.2f, 2.6f, WaterY + 0.3f, 999.0f, 70.0f, false, 8.0f, 0.05f),
		};

		var rng = new RandomNumberGenerator { Seed = 20260803 };

		foreach (ScatterLayer layer in layers)
		{
			ScatterOneLayer(layer, rng);
		}
	}

	private void ScatterOneLayer(ScatterLayer layer, RandomNumberGenerator rng)
	{
		var batches = new List<Transform3D>[layer.Scenes.Length];
		for (int i = 0; i < batches.Length; i++)
		{
			batches[i] = new List<Transform3D>();
		}

		// Cluster seeds. Members are drawn around a seed rather than uniformly over
		// the map, which is what makes a scatter read as growth instead of noise.
		int seedCount = Mathf.Max(4, layer.Count / 9);
		var seeds = new List<Vector2>(seedCount);
		for (int attempt = 0; attempt < seedCount * 40 && seeds.Count < seedCount; attempt++)
		{
			float sx = rng.RandfRange(TerrainMinX + 8.0f, TerrainMaxX - 8.0f);
			float sz = rng.RandfRange(-TerrainHalfZ + 8.0f, TerrainHalfZ - 8.0f);
			if (IsPlantable(sx, sz, layer.MaxSlopeDrop, layer.MinY, out _))
			{
				seeds.Add(new Vector2(sx, sz));
			}
		}
		if (seeds.Count == 0)
		{
			return;
		}

		int placed = 0;
		int budget = layer.Count * 60;
		for (int attempt = 0; attempt < budget && placed < layer.Count; attempt++)
		{
			// Most members hug a seed; a minority are scattered loose so the edges
			// of each clump stay soft.
			float x;
			float z;
			if (rng.Randf() < 0.82f)
			{
				Vector2 seed = seeds[rng.RandiRange(0, seeds.Count - 1)];
				float angle = rng.RandfRange(0.0f, Mathf.Tau);
				// sqrt keeps the disc evenly filled instead of bunching at the centre.
				float radius = layer.ClusterRadius * Mathf.Sqrt(rng.Randf());
				x = seed.X + Mathf.Cos(angle) * radius;
				z = seed.Y + Mathf.Sin(angle) * radius;
			}
			else
			{
				x = rng.RandfRange(TerrainMinX + 4.0f, TerrainMaxX - 4.0f);
				z = rng.RandfRange(-TerrainHalfZ + 4.0f, TerrainHalfZ - 4.0f);
			}

			if (x < TerrainMinX + 3.0f || x > TerrainMaxX - 3.0f ||
				Mathf.Abs(z) > TerrainHalfZ - 3.0f)
			{
				continue;
			}
			if (!IsPlantable(x, z, layer.MaxSlopeDrop, layer.MinY, out float y))
			{
				continue;
			}
			if (y > layer.MaxY)
			{
				continue;
			}

			int species = rng.RandiRange(0, layer.Scenes.Length - 1);
			float scale = rng.RandfRange(layer.MinScale, layer.MaxScale);

			// Yaw plus a small random lean. Perfectly upright props read as stamped
			// on; a couple of degrees of tilt is enough to break that.
			var basis = new Basis(Vector3.Up, rng.RandfRange(0.0f, Mathf.Tau));
			basis = new Basis(Vector3.Right, rng.RandfRange(-0.05f, 0.05f)) * basis;
			basis = new Basis(Vector3.Forward, rng.RandfRange(-0.05f, 0.05f)) * basis;

			batches[species].Add(new Transform3D(
				basis.Scaled(Vector3.One * scale),
				new Vector3(x, y - layer.SinkDepth * scale, z)));
			placed++;
		}

		for (int i = 0; i < layer.Scenes.Length; i++)
		{
			ScatterFromScene(
				layer.Scenes[i],
				$"{layer.Name}{i:D2}",
				batches[i],
				layer.VisibilityRange,
				layer.CastShadow);
		}
	}

	private static int CountAll(List<Transform3D>[] batches)
	{
		int total = 0;
		foreach (List<Transform3D> batch in batches)
		{
			total += batch.Count;
		}
		return total;
	}

	/// <summary>
	/// Rejection test for a scatter position: off the road, clear of the bridge and
	/// the mill, above <paramref name="minY"/>, and flat enough over a 3m footprint.
	/// </summary>
	private static bool IsPlantable(
		float x, float z, float maxDrop, float minY, out float y)
	{
		y = GorgeHeight(x, z);

		if (y < minY)
		{
			return false;
		}

		// Road and shoulder corridor.
		if (Mathf.Abs(z) < 13.0f)
		{
			return false;
		}

		// Bridge structure, abutments and wing walls.
		if (x > -226.0f && x < -128.0f && Mathf.Abs(z) < 19.0f)
		{
			return false;
		}

		// Mill site and its plinth.
		if (x > -158.0f && x < -130.0f && z > -54.0f && z < -22.0f)
		{
			return false;
		}

		float h1 = GorgeHeight(x + 1.5f, z);
		float h2 = GorgeHeight(x - 1.5f, z);
		float h3 = GorgeHeight(x, z + 1.5f);
		float h4 = GorgeHeight(x, z - 1.5f);
		float lo = Mathf.Min(Mathf.Min(h1, h2), Mathf.Min(h3, h4));
		float hi = Mathf.Max(Mathf.Max(h1, h2), Mathf.Max(h3, h4));

		return hi - lo <= maxDrop;
	}

	/// <summary>
	/// Instances every MeshInstance3D found inside a source scene as a MultiMesh
	/// using the supplied placements, preserving each mesh's local offset within
	/// the source scene so multi-part props (trunk plus canopy) stay aligned.
	/// </summary>
	private void ScatterFromScene(
		string scenePath,
		string namePrefix,
		List<Transform3D> placements,
		float visibilityRange,
		bool castShadow)
	{
		if (placements.Count == 0)
		{
			return;
		}

		var packed = ResourceLoader.Load<PackedScene>(scenePath);
		if (packed == null)
		{
			GD.PushWarning($"OldMillBridge: could not load scatter source {scenePath}");
			return;
		}

		Node source = packed.Instantiate();
		var parts = new List<(Mesh Mesh, Transform3D Local)>();
		CollectMeshes(source, Transform3D.Identity, parts);

		int partIndex = 0;
		foreach ((Mesh mesh, Transform3D local) in parts)
		{
			var multiMesh = new MultiMesh
			{
				TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
				Mesh = mesh,
				InstanceCount = placements.Count,
			};
			for (int i = 0; i < placements.Count; i++)
			{
				multiMesh.SetInstanceTransform(i, placements[i] * local);
			}

			var instance = new MultiMeshInstance3D
			{
				Name = $"{namePrefix}_{partIndex:D2}",
				Multimesh = multiMesh,
				CastShadow = castShadow
					? GeometryInstance3D.ShadowCastingSetting.On
					: GeometryInstance3D.ShadowCastingSetting.Off,
				VisibilityRangeEnd = visibilityRange,
				VisibilityRangeEndMargin = visibilityRange * 0.12f,
				VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
			};

			AddChild(instance);
			partIndex++;
		}

		source.Free();
	}

	private static void CollectMeshes(
		Node node,
		Transform3D accumulated,
		List<(Mesh Mesh, Transform3D Local)> results)
	{
		Transform3D local = node is Node3D spatial
			? accumulated * spatial.Transform
			: accumulated;

		if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
		{
			results.Add((BakeSurfaceMaterials(meshInstance), local));
		}

		foreach (Node child in node.GetChildren())
		{
			CollectMeshes(child, local, results);
		}
	}

	/// <summary>
	/// The project's tree and bush scenes assign their materials as
	/// surface_material_override entries on the MeshInstance3D (bark on surface 0,
	/// leaves on surface 1) rather than on the mesh resource itself.
	/// MultiMeshInstance3D has no per-surface override and a single MaterialOverride
	/// would flatten trunk and canopy to one material, so the overrides are baked
	/// into a duplicated mesh here. Without this every plant renders with the
	/// default white material.
	/// </summary>
	private static Mesh BakeSurfaceMaterials(MeshInstance3D meshInstance)
	{
		Mesh mesh = meshInstance.Mesh!;

		bool hasOverride = false;
		for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
		{
			if (meshInstance.GetSurfaceOverrideMaterial(surface) != null)
			{
				hasOverride = true;
				break;
			}
		}

		if (!hasOverride || mesh.Duplicate() is not ArrayMesh baked)
		{
			return mesh;
		}

		for (int surface = 0; surface < baked.GetSurfaceCount(); surface++)
		{
			Material? material = meshInstance.GetSurfaceOverrideMaterial(surface);
			if (material != null)
			{
				baked.SurfaceSetMaterial(surface, material);
			}
		}

		return baked;
	}

	// ======================================================================
	// Batch helpers
	// ======================================================================

	/// <summary>Adds an axis-aligned box to a batch.</summary>
	private static void AddBox(List<Transform3D> batch, Vector3 center, Vector3 size)
	{
		batch.Add(new Transform3D(
			new Basis(
				new Vector3(size.X, 0.0f, 0.0f),
				new Vector3(0.0f, size.Y, 0.0f),
				new Vector3(0.0f, 0.0f, size.Z)),
			center));
	}

	/// <summary>
	/// Adds a structural member running from <paramref name="a"/> to <paramref name="b"/>
	/// with the given cross-section, oriented along its own axis.
	/// </summary>
	private static void AddBeam(
		List<Transform3D> batch, Vector3 a, Vector3 b, float width, float height)
	{
		Vector3 delta = b - a;
		float length = delta.Length();
		if (length <= 0.0001f)
		{
			return;
		}

		Vector3 forward = delta / length;
		Vector3 reference = Mathf.Abs(forward.Dot(Vector3.Up)) > 0.98f
			? Vector3.Forward
			: Vector3.Up;
		Vector3 right = forward.Cross(reference).Normalized();
		Vector3 up = right.Cross(forward).Normalized();

		batch.Add(new Transform3D(
			new Basis(right * width, up * height, forward * length),
			(a + b) * 0.5f));
	}

	private void EmitBatch(string name, List<Transform3D> transforms, Material material)
	{
		if (transforms.Count == 0)
		{
			return;
		}

		var multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			Mesh = new BoxMesh { Size = Vector3.One },
			InstanceCount = transforms.Count,
		};

		for (int i = 0; i < transforms.Count; i++)
		{
			multiMesh.SetInstanceTransform(i, transforms[i]);
		}

		AddChild(new MultiMeshInstance3D
		{
			Name = name,
			Multimesh = multiMesh,
			MaterialOverride = material,
		});
	}
}
