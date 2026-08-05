#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Builds the county's water surfaces: Blackwater Lake, the Blackwater itself and
/// the Mill Creek tributary.
///
/// Water is meshed per chunk like everything else, but by marching the chunk's
/// cells and keeping only those that are actually wet. That matters because
/// Blackwater Lake has an irregular drowned-valley shoreline - a rectangle of
/// water clipped to the chunk would poke straight through the surrounding hills.
///
/// The shader (assets/materials/county_water.gdshader) reads its flow field from
/// vertex colour:
///   COLOR.rg  flow direction, packed dir * 0.5 + 0.5
///   COLOR.b   flow speed, 0 still to 1 rapids
///   COLOR.a   openness, 0 at the waterline to 1 in open water
/// Those are computed here from the river spine's tangent and gradient, so the
/// river visibly runs downhill and the lake sits still.
/// </summary>
[Tool]
public partial class CountyWater : Node3D, ICountyChunkSource
{
    /// <summary>
    /// Water reaches further than vegetation: the lake and the river are read as
    /// landscape features from the ridgelines, so they need to be there when you
    /// look down into the valley.
    /// </summary>
    [Export] public int WaterRadius { get; set; } = 7;

    /// <summary>Cells per chunk edge. 16 gives 16m cells, plenty for a water plane.</summary>
    [Export] public int CellsPerChunk { get; set; } = 16;

    public int ChunkRadius => WaterRadius;

    private const string WaterMaterialPath = "res://assets/materials/county_water.tres";

    /// <summary>
    /// The Old Mill Bridge builds its own local water plane across this reach.
    /// Generating county water on top of it would z-fight along the whole span,
    /// so the county skips the box the landmark already owns and lets the two
    /// meet at its edge - both sit at exactly OldMillBridge.WaterY there, because
    /// CountyMap.RiverSurfaceY is pinned to -8.5 at the bridge.
    /// </summary>
    private static readonly Rect2 BridgeReach = new(-232.0f, -46.0f, 116.0f, 92.0f);

    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private Material? _material;

    public override void _Ready()
    {
        _material = LoadMaterial();

        if (GetParent() is CountyWorld world)
        {
            world.RegisterSource(this);
        }
    }

    private Material LoadMaterial()
    {
        if (ResourceLoader.Exists(WaterMaterialPath) &&
            ResourceLoader.Load(WaterMaterialPath) is Material loaded)
        {
            return loaded;
        }

        GD.PushWarning($"CountyWater: {WaterMaterialPath} missing, using a fallback material.");
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(0.10f, 0.22f, 0.28f, 0.82f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.1f,
            Metallic = 0.0f,
        };
    }

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk))
        {
            return;
        }

        ArrayMesh? mesh = BuildWaterMesh(chunk);
        if (mesh == null)
        {
            // Dry chunk. Recorded as resident anyway so the streamer does not retry
            // it every tick.
            _chunks[chunk] = new Node3D { Name = $"Water_{chunk.X}_{chunk.Y}_dry" };
            AddChild(_chunks[chunk]);
            return;
        }

        var holder = new Node3D { Name = $"Water_{chunk.X}_{chunk.Y}" };
        holder.AddChild(new MeshInstance3D
        {
            Name = "Surface",
            Mesh = mesh,
            MaterialOverride = _material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });

        AddChild(holder);
        _chunks[chunk] = holder;
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        if (_chunks.Remove(chunk, out Node3D? holder))
        {
            holder.QueueFree();
        }
    }

    /// <summary>
    /// Marches the chunk's cells and emits a quad for each one that is wet.
    /// Returns null when the chunk has no water at all, which is most of them.
    /// </summary>
    private ArrayMesh? BuildWaterMesh(Vector2I chunk)
    {
        int side = CellsPerChunk + 1;
        Vector2 origin = CountyChunks.Origin(chunk);
        float step = CountyChunks.Size / CellsPerChunk;

        var surfaceY = new float[side, side];
        var terrainY = new float[side, side];
        var wet = new bool[side, side];
        bool any = false;

        for (int row = 0; row < side; row++)
        {
            float z = origin.Y + row * step;
            for (int column = 0; column < side; column++)
            {
                float x = origin.X + column * step;
                float water = CountyMap.WaterSurfaceY(x, z);
                float ground = CountyMap.Height(x, z);

                surfaceY[column, row] = water;
                terrainY[column, row] = ground;

                // A vertex counts as wet if there is a water surface here at all.
                // Testing against the terrain as well would clip the surface back
                // to the exact waterline and leave a hard edge with no shallows.
                wet[column, row] = water > float.MinValue;
                any |= wet[column, row];
            }
        }

        if (!any)
        {
            return null;
        }

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color>();
        var uvs = new List<Vector2>();
        var indices = new List<int>();

        // Vertex indices into the emitted arrays, or -1 where the grid point is dry.
        var mapped = new int[side, side];
        for (int row = 0; row < side; row++)
        {
            for (int column = 0; column < side; column++)
            {
                mapped[column, row] = -1;
            }
        }

        for (int row = 0; row < side; row++)
        {
            float z = origin.Y + row * step;
            for (int column = 0; column < side; column++)
            {
                if (!wet[column, row])
                {
                    continue;
                }

                float x = origin.X + column * step;
                float water = surfaceY[column, row];

                mapped[column, row] = vertices.Count;
                vertices.Add(new Vector3(x, water, z));
                normals.Add(Vector3.Up);
                uvs.Add(new Vector2(x, z));
                colors.Add(FlowColor(x, z, water, terrainY[column, row]));
            }
        }

        for (int row = 0; row < CellsPerChunk; row++)
        {
            for (int column = 0; column < CellsPerChunk; column++)
            {
                int a = mapped[column, row];
                int b = mapped[column + 1, row];
                int c = mapped[column, row + 1];
                int d = mapped[column + 1, row + 1];

                // Only whole wet cells are emitted. A partially wet cell sits at the
                // shoreline where the terrain is above water anyway, and the
                // shader's depth fade covers the last metre far more convincingly
                // than a ragged triangulated edge would.
                if (a < 0 || b < 0 || c < 0 || d < 0)
                {
                    continue;
                }

                if (InBridgeReach(origin.X + column * step, origin.Y + row * step))
                {
                    continue;
                }

                // Wound front-face-up, matching the terrain mesher.
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);

                indices.Add(b);
                indices.Add(d);
                indices.Add(c);
            }
        }

        if (indices.Count == 0)
        {
            return null;
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
        arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
        arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
        arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    private static bool InBridgeReach(float x, float z) =>
        BridgeReach.HasPoint(new Vector2(x, z));

    /// <summary>
    /// Packs the flow field the water shader reads.
    ///
    /// Direction is the river spine's tangent; speed comes from the local gradient
    /// of the water surface, so the reaches that actually fall steeply are the ones
    /// that show rapids. Standing water on the reservoir gets zero speed and the
    /// shader falls back to its slow drift.
    /// </summary>
    private static Color FlowColor(float x, float z, float waterY, float terrainY)
    {
        var here = new Vector2(x, z);
        Vector2 direction = Vector2.Zero;
        float speed = 0.0f;

        if (!CountyMap.RiverLine.IsFarFrom(here, 90.0f))
        {
            float distance = CountyMap.RiverLine.Distance(here, out float along);
            if (distance < 90.0f)
            {
                direction = CountyMap.RiverLine.DirectionNear(here);

                // Gradient over a 60m window along the run. The Blackwater drops
                // about 320m from the dam to the mouth, but not evenly - this is
                // what puts the whitewater in the canyon and not in the town reach.
                float ahead = CountyMap.RiverSurfaceY(Mathf.Min(along + 0.006f, 1.0f));
                float behind = CountyMap.RiverSurfaceY(Mathf.Max(along - 0.006f, 0.0f));
                float drop = Mathf.Max(behind - ahead, 0.0f);
                speed = Mathf.Clamp(drop / 9.0f, 0.06f, 1.0f);
            }
        }

        if (!CountyMap.MillCreekLine.IsFarFrom(here, 30.0f))
        {
            float distance = CountyMap.MillCreekLine.Distance(here);
            if (distance < 30.0f && speed < 0.35f)
            {
                direction = CountyMap.MillCreekLine.DirectionNear(here);
                speed = 0.35f;
            }
        }

        // Openness: how far the water is from its own edge, judged by how far the
        // bed has dropped below the surface. The shader uses this to damp the swell
        // at the waterline so shoreline vertices do not bob out of their bed.
        float depth = waterY - terrainY;
        float openness = Mathf.Clamp(depth / 2.6f, 0.0f, 1.0f);

        return new Color(
            direction.X * 0.5f + 0.5f,
            direction.Y * 0.5f + 0.5f,
            speed,
            openness);
    }

    public void Rebuild()
    {
        foreach (Vector2I chunk in new List<Vector2I>(_chunks.Keys))
        {
            ReleaseChunk(chunk);
        }

        _material = LoadMaterial();
    }

    public int ResidentChunkCount => _chunks.Count;
}
