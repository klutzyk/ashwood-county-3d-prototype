#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Meshes and streams the county's ground.
///
/// Chunks are built on background tasks and handed to the scene tree deferred,
/// because a single ring-0 chunk is a few thousand <see cref="CountyMap.Height"/>
/// evaluations and doing that inline drops a frame every time the player crosses
/// a chunk border.
///
/// Vertex resolution falls off by ring, which is what keeps an eight-kilometre
/// county inside an integrated GPU's budget. The cost of that is neighbouring
/// chunks meeting at different resolutions, so every chunk carries a skirt around
/// its edge - see <see cref="AppendSkirt"/> for why that is the fix rather than
/// stitching.
/// </summary>
[Tool]
public partial class CountyTerrain : Node3D, ICountyChunkSource
{
    /// <summary>
    /// How far terrain streams, in chunks. 10 rings of 256m is a 2.6km radius,
    /// which comfortably covers the aerial-perspective distance where fog takes
    /// over from geometry.
    /// </summary>
    [Export] public int TerrainRadius { get; set; } = 8;

    /// <summary>Rings that receive collision. Beyond this the player cannot reach anyway.</summary>
    [Export] public int CollisionRadius { get; set; } = 2;

    [Export] public bool BuildCollision { get; set; } = true;

    /// <summary>Diagnostic: turn skirts off to see the raw LOD seams they are hiding.</summary>
    [Export] public bool EnableSkirts { get; set; } = true;

    /// <summary>Emitted after a chunk's mesh lands, so scatter layers can populate it.</summary>
    [Signal]
    public delegate void ChunkReadyEventHandler(Vector2I chunk, int ring);

    [Signal]
    public delegate void ChunkReleasedEventHandler(Vector2I chunk);

    public int ChunkRadius => TerrainRadius;

    private const string TerrainMaterialPath = "res://assets/materials/county_terrain.tres";

    /// <summary>
    /// Quads per chunk edge by ring: 4m per vertex underfoot, easing to 16m at the
    /// horizon.
    ///
    /// The first attempt halved resolution every two rings down to 4 quads, which
    /// is 64m per sample. At that spacing whole hills vanish between vertices, so
    /// adjacent LODs disagreed by tens of metres and the skirts meant to hide the
    /// seam became visible walls across the landscape. A 16m floor keeps the
    /// silhouette intact for a few hundred extra triangles per chunk.
    /// </summary>
    private static int QuadsForRing(int ring) => ring switch
    {
        0 or 1 => 64,
        2 or 3 => 48,
        4 or 5 => 32,
        6 or 7 => 24,
        _ => 16,
    };

    /// <summary>Mirrors EnableSkirts for the static mesher, which has no instance.</summary>
    private static bool SkirtsEnabled = true;

    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private readonly HashSet<Vector2I> _pending = new();

    /// <summary>
    /// Finished vertex data waiting to be turned into resources on the main thread.
    ///
    /// Godot's RenderingServer will not accept resources created off the main
    /// thread - doing so produces "Attempting to initialize the wrong RID" and
    /// then garbage in the scene cull. So the background task produces only plain
    /// arrays, and <see cref="InstallChunk"/> builds the ArrayMesh and collision
    /// shape once it is back on the main thread.
    /// </summary>
    private readonly ConcurrentDictionary<Vector2I, ChunkData> _built = new();

    private Material? _material;

    public override void _Ready()
    {
        _material = LoadMaterial();
        SkirtsEnabled = EnableSkirts;

        // Registering with the parent world rather than being wired by hand means
        // the scene can be assembled in any order.
        if (GetParent() is CountyWorld world)
        {
            world.RegisterSource(this);
        }
    }

    private Material LoadMaterial()
    {
        if (ResourceLoader.Exists(TerrainMaterialPath) &&
            ResourceLoader.Load(TerrainMaterialPath) is Material loaded)
        {
            return loaded;
        }

        // A plain material still lets the world be flown and judged for shape while
        // the splat material is being authored.
        GD.PushWarning($"CountyTerrain: {TerrainMaterialPath} missing, using a fallback material.");
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(0.34f, 0.36f, 0.26f),
            Roughness = 0.95f,
            VertexColorUseAsAlbedo = false,
        };
    }

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk) || !_pending.Add(chunk))
        {
            return;
        }

        int quads = QuadsForRing(ring);
        bool wantsCollision = BuildCollision && ring <= CollisionRadius;

        Task.Run(() =>
        {
            try
            {
                _built[chunk] = BuildChunkData(chunk, quads, wantsCollision);
                CallDeferred(nameof(InstallChunk), chunk, ring);
            }
            catch (Exception error)
            {
                GD.PushError($"CountyTerrain: chunk {chunk} failed to build: {error}");
                CallDeferred(nameof(ClearPending), chunk);
            }
        });
    }

    private void ClearPending(Vector2I chunk) => _pending.Remove(chunk);

    private void InstallChunk(Vector2I chunk, int ring)
    {
        _pending.Remove(chunk);

        if (!_built.TryRemove(chunk, out ChunkData? data))
        {
            return;
        }

        // The chunk may have streamed back out while the task was in flight.
        if (!IsInsideTree() || _chunks.ContainsKey(chunk))
        {
            return;
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.Vertices;
        arrays[(int)Mesh.ArrayType.Normal] = data.Normals;
        arrays[(int)Mesh.ArrayType.Color] = data.Colors;
        arrays[(int)Mesh.ArrayType.TexUV] = data.Uvs;
        arrays[(int)Mesh.ArrayType.Index] = data.Indices;

        var surface = new ArrayMesh();
        surface.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        Shape3D? collision = null;
        if (data.CollisionFaces != null)
        {
            var shape = new ConcavePolygonShape3D();
            shape.SetFaces(data.CollisionFaces);
            collision = shape;
        }

        var holder = new Node3D { Name = $"Chunk_{chunk.X}_{chunk.Y}" };
        holder.SetMeta("quads", data.Quads);

        var mesh = new MeshInstance3D
        {
            Name = "Surface",
            Mesh = surface,
            MaterialOverride = _material,
            CastShadow = ring <= 4
                ? GeometryInstance3D.ShadowCastingSetting.On
                : GeometryInstance3D.ShadowCastingSetting.Off,
        };
        holder.AddChild(mesh);

        if (collision != null)
        {
            var body = new StaticBody3D { Name = "Body" };
            body.AddChild(new CollisionShape3D { Name = "Shape", Shape = collision });
            holder.AddChild(body);
        }

        AddChild(holder);
        _chunks[chunk] = holder;

        EmitSignal(SignalName.ChunkReady, chunk, ring);
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        _pending.Remove(chunk);
        _built.TryRemove(chunk, out _);
        if (!_chunks.Remove(chunk, out Node3D? holder))
        {
            return;
        }

        EmitSignal(SignalName.ChunkReleased, chunk);
        holder.QueueFree();
    }

    public void UpdateChunkRing(Vector2I chunk, int ring)
    {
        // Resolution is baked into the mesh, so a ring change means a rebuild. Only
        // do it when the detail level actually differs, or walking a border would
        // rebuild the same geometry every few steps.
        if (!_chunks.TryGetValue(chunk, out Node3D? holder))
        {
            return;
        }

        if (holder.GetMeta("quads", 0).AsInt32() == QuadsForRing(ring))
        {
            return;
        }

        ReleaseChunk(chunk);
        BuildChunk(chunk, ring);
    }

    /// <summary>Plain vertex data - no Godot resources, so it is safe to build off-thread.</summary>
    private sealed class ChunkData
    {
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public Color[] Colors = Array.Empty<Color>();
        public Vector2[] Uvs = Array.Empty<Vector2>();
        public int[] Indices = Array.Empty<int>();
        public Vector3[]? CollisionFaces;
        public int Quads;
    }

    /// <summary>
    /// Builds one chunk's vertex data. Runs off the main thread: it touches only
    /// <see cref="CountyMap"/>, which is pure and allocation-free.
    /// </summary>
    private static ChunkData BuildChunkData(Vector2I chunk, int quads, bool wantsCollision)
    {
        int side = quads + 1;
        Vector2 origin = CountyChunks.Origin(chunk);
        float step = CountyChunks.Size / quads;

        var heights = new float[side, side];
        for (int row = 0; row < side; row++)
        {
            float z = origin.Y + row * step;
            for (int column = 0; column < side; column++)
            {
                heights[column, row] = CountyMap.Height(origin.X + column * step, z);
            }
        }

        var vertices = new List<Vector3>(side * side + side * 4 * 2);
        var normals = new List<Vector3>(vertices.Capacity);
        var colors = new List<Color>(vertices.Capacity);
        var uvs = new List<Vector2>(vertices.Capacity);
        var indices = new List<int>(quads * quads * 6 + quads * 4 * 6);

        for (int row = 0; row < side; row++)
        {
            float z = origin.Y + row * step;
            for (int column = 0; column < side; column++)
            {
                float x = origin.X + column * step;
                float height = heights[column, row];

                // Normals come from the height grid rather than from
                // CountyMap.Normal, which would cost four more Height evaluations
                // per vertex. At chunk edges the grid is one sample short, so fall
                // back to sampling just those.
                float left = column > 0 ? heights[column - 1, row] : CountyMap.Height(x - step, z);
                float right = column < side - 1 ? heights[column + 1, row] : CountyMap.Height(x + step, z);
                float back = row > 0 ? heights[column, row - 1] : CountyMap.Height(x, z - step);
                float front = row < side - 1 ? heights[column, row + 1] : CountyMap.Height(x, z + step);

                var normal = new Vector3(left - right, 2.0f * step, back - front).Normalized();
                float slope = Mathf.Acos(Mathf.Clamp(normal.Y, -1.0f, 1.0f));

                vertices.Add(new Vector3(x, height, z));
                normals.Add(normal);
                colors.Add(SurfaceWeights(x, z, height, slope));

                // World-space UVs so the material tiles continuously across chunk
                // borders regardless of each chunk's resolution.
                uvs.Add(new Vector2(x, z));
            }
        }

        for (int row = 0; row < quads; row++)
        {
            for (int column = 0; column < quads; column++)
            {
                int topLeft = row * side + column;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + side;
                int bottomRight = bottomLeft + 1;

                // Wound so the front face points up. The first version had these
                // reversed, which back-face culled the entire ground when viewed
                // from above - the world looked like it was rendering only its
                // underside, and every camera appeared to be buried.
                indices.Add(topLeft);
                indices.Add(topRight);
                indices.Add(bottomLeft);

                indices.Add(topRight);
                indices.Add(bottomRight);
                indices.Add(bottomLeft);
            }
        }

        if (SkirtsEnabled)
        {
            AppendSkirt(vertices, normals, colors, uvs, indices, side, step);
        }

        Vector3[]? collisionFaces = null;
        if (wantsCollision)
        {
            // Collision uses the grid only, never the skirt: a skirt is a vertical
            // wall hanging under the chunk edge, and colliding with it would stop
            // the player dead at every chunk border.
            int surfaceIndexCount = quads * quads * 6;
            collisionFaces = new Vector3[surfaceIndexCount];
            for (int i = 0; i < surfaceIndexCount; i++)
            {
                collisionFaces[i] = vertices[indices[i]];
            }
        }

        return new ChunkData
        {
            Vertices = vertices.ToArray(),
            Normals = normals.ToArray(),
            Colors = colors.ToArray(),
            Uvs = uvs.ToArray(),
            Indices = indices.ToArray(),
            CollisionFaces = collisionFaces,
            Quads = quads,
        };
    }

    /// <summary>
    /// Hangs a vertical wall down from each chunk edge.
    ///
    /// Neighbouring chunks at different LODs sample the same edge at different
    /// rates, so their shared border does not line up and leaves hairline gaps that
    /// flicker sky. Stitching the two resolutions together needs each chunk to know
    /// its neighbours' LODs, which makes every chunk's mesh depend on four others
    /// and turns a single ring change into a cascade of rebuilds. A skirt is
    /// independent, costs a few hundred triangles, and hides the gap completely.
    /// </summary>
    private static void AppendSkirt(
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Color> colors,
        List<Vector2> uvs,
        List<int> indices,
        int side,
        float step)
    {
        // Only as deep as the LOD disagreement actually is.
        //
        // The first pass hung skirts step*2.2+6 metres deep, which at the outer
        // rings was seventy metres of vertical wall. A skirt's XZ projection is
        // degenerate, so every one of those walls rendered as a smear of stretched
        // texture, and together they drew a visible grid across the whole county.
        // With the resolution floor raised, neighbouring LODs differ by a couple of
        // metres at most, and a shallow skirt covers that while staying hidden.
        float depth = Mathf.Max(step * 0.30f, 1.5f);

        void Edge(Func<int, int> indexAt, int count)
        {
            int first = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                int source = indexAt(i);
                Vector3 top = vertices[source];
                vertices.Add(new Vector3(top.X, top.Y - depth, top.Z));
                normals.Add(normals[source]);
                colors.Add(colors[source]);
                uvs.Add(uvs[source]);
            }

            for (int i = 0; i < count - 1; i++)
            {
                int topA = indexAt(i);
                int topB = indexAt(i + 1);
                int lowA = first + i;
                int lowB = first + i + 1;

                indices.Add(topA);
                indices.Add(topB);
                indices.Add(lowA);

                indices.Add(topB);
                indices.Add(lowB);
                indices.Add(lowA);
            }
        }

        Edge(i => i, side);                                   // north
        Edge(i => (side - 1) * side + (side - 1 - i), side);  // south
        Edge(i => (side - 1 - i) * side, side);               // west
        Edge(i => i * side + (side - 1), side);               // east
    }

    /// <summary>
    /// Per-vertex surface weights, packed into vertex colour for the splat material.
    ///
    /// R = forest floor, G = rock, B = worked soil / dirt, A = riverbank wetness.
    /// Grass is whatever is left over, so the four channels never have to sum to
    /// one and the shader can treat grass as the base coat.
    /// </summary>
    private static Color SurfaceWeights(float x, float z, float height, float slope)
    {
        CountyMap.Biome biome = CountyMap.BiomeAt(x, z, height, slope);

        float forest = CountyMap.ForestDensity(x, z, height, slope);
        float field = CountyMap.FieldStrength(x, z);

        // Rock is driven by slope first and biome second: a cliff is rock whatever
        // the map says grows on the plateau above it.
        float rock = Mathf.Clamp((slope - 0.42f) / 0.34f, 0.0f, 1.0f);
        rock = Mathf.Max(rock, CountyMap.RimFalloff(x, z) * 1.4f);
        if (biome == CountyMap.Biome.Rock)
        {
            rock = Mathf.Max(rock, 0.72f);
        }

        float dirt = field;
        if (biome == CountyMap.Biome.Settled)
        {
            dirt = Mathf.Max(dirt, 0.45f);
        }

        float wet = 0.0f;
        if (biome == CountyMap.Biome.Riverbank)
        {
            wet = 1.0f;
            forest *= 0.15f;
        }

        // Ground right at the waterline is gravel and silt regardless of biome.
        float water = CountyMap.WaterSurfaceY(x, z);
        if (water > float.MinValue)
        {
            wet = Mathf.Max(wet, 1.0f - Mathf.Clamp((height - water) / 7.0f, 0.0f, 1.0f));
        }

        forest *= 1.0f - rock;
        dirt *= 1.0f - rock;

        return new Color(
            Mathf.Clamp(forest, 0.0f, 1.0f),
            Mathf.Clamp(rock, 0.0f, 1.0f),
            Mathf.Clamp(dirt, 0.0f, 1.0f),
            Mathf.Clamp(wet, 0.0f, 1.0f));
    }

    /// <summary>Drops every chunk so the next streaming tick rebuilds from scratch.</summary>
    public void Rebuild()
    {
        foreach (Vector2I chunk in new List<Vector2I>(_chunks.Keys))
        {
            ReleaseChunk(chunk);
        }

        _material = LoadMaterial();
    }

    /// <summary>Chunks currently resident, for tests and diagnostics.</summary>
    public int ResidentChunkCount => _chunks.Count;
}
