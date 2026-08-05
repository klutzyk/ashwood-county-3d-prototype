#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Scatters the county's plant life, chunk by chunk, as MultiMesh batches.
///
/// Placement is driven entirely by <see cref="CountyMap"/>, so the forest agrees
/// with the terrain material, the biome map and the roads without any of them
/// knowing about each other. Density comes from
/// <see cref="CountyMap.ForestDensity"/>, which already accounts for the treeline,
/// slope, worked fields and the clearings around settlements and roads - so the
/// scatter never has to re-implement any of those rules and cannot disagree with
/// the ground it is standing on.
///
/// Everything here is deterministic: the RNG is seeded from the chunk coordinate,
/// so streaming a chunk out and back in reproduces exactly the same forest rather
/// than reshuffling it behind the player.
/// </summary>
[Tool]
public partial class CountyVegetation : Node3D, ICountyChunkSource
{
    /// <summary>
    /// Vegetation streams less far than terrain. Past a few hundred metres a tree
    /// is a handful of pixels, and paying full canopy geometry for it is the
    /// fastest way to lose the frame budget on an integrated GPU.
    /// </summary>
    [Export] public int VegetationRadius { get; set; } = 3;

    /// <summary>Scales every layer's instance count. The single knob for the perf pass.</summary>
    [Export(PropertyHint.Range, "0.0,2.0,0.05")]
    public float DensityScale { get; set; } = 1.0f;

    public int ChunkRadius => VegetationRadius;

    private const string VegetationRoot = "res://assets/environment/nature/polyhaven/";

    /// <summary>
    /// One scatter layer. <paramref name="PerChunk"/> is the candidate count for a
    /// full-density chunk; the actual placed count is whatever survives the biome,
    /// slope and water rejections.
    /// </summary>
    private readonly record struct Layer(
        string Name,
        string[] Scenes,
        int PerChunk,
        float MinScale,
        float MaxScale,
        float VisibilityRange,
        bool CastShadow,
        /// <summary>Furthest ring this layer appears in. Undergrowth stays near.</summary>
        int MaxRing,
        /// <summary>Steepest ground this plant will grow on, in radians.</summary>
        float MaxSlope,
        LayerRule Rule,
        /// <summary>How strongly instances clump rather than spreading evenly.</summary>
        float Clustering = 0.8f);

    private enum LayerRule
    {
        /// <summary>Weighted by forest density: canopy and forest understorey.</summary>
        Forest,
        /// <summary>Open ground - meadow and the thinner edges of the forest.</summary>
        Open,
        /// <summary>Near water.</summary>
        Riverbank,
        /// <summary>Rocky and steep ground.</summary>
        Rock,
    }

    private static readonly Layer[] Layers =
    {
        new("canopy", new[]
            {
                VegetationRoot + "ashwood_jacaranda_lod0.tscn",
            },
            PerChunk: 60, MinScale: 0.34f, MaxScale: 0.56f,
            VisibilityRange: 110.0f, CastShadow: true, MaxRing: 1, MaxSlope: 0.62f,
            Rule: LayerRule.Forest, Clustering: 0.86f),

        new("canopy_far", new[]
            {
                VegetationRoot + "ashwood_jacaranda_lod1.tscn",
            },
            PerChunk: 120, MinScale: 0.34f, MaxScale: 0.56f,
            VisibilityRange: 430.0f, CastShadow: false, MaxRing: 2, MaxSlope: 0.62f,
            Rule: LayerRule.Forest, Clustering: 0.86f),

        new("shrubs", new[]
            {
                VegetationRoot + "ashwood_shrub_01.tscn",
                VegetationRoot + "ashwood_shrub_02_a.tscn",
                VegetationRoot + "ashwood_shrub_02_c.tscn",
                VegetationRoot + "ashwood_shrub_03_a.tscn",
                VegetationRoot + "ashwood_shrub_03_c.tscn",
            },
            PerChunk: 95, MinScale: 0.7f, MaxScale: 1.5f,
            VisibilityRange: 72.0f, CastShadow: false, MaxRing: 0, MaxSlope: 0.66f,
            Rule: LayerRule.Forest, Clustering: 0.7f),

        new("ferns", new[]
            {
                VegetationRoot + "ashwood_fern_02_a.tscn",
                VegetationRoot + "ashwood_fern_02_c.tscn",
                VegetationRoot + "ashwood_nettle_medium.tscn",
                VegetationRoot + "ashwood_nettle_tall.tscn",
            },
            PerChunk: 130, MinScale: 0.7f, MaxScale: 1.4f,
            VisibilityRange: 52.0f, CastShadow: false, MaxRing: 0, MaxSlope: 0.62f,
            Rule: LayerRule.Forest, Clustering: 0.75f),

        new("grass", new[]
            {
                VegetationRoot + "ashwood_grass_bermuda_small.tscn",
                VegetationRoot + "ashwood_grass_bermuda_medium.tscn",
                VegetationRoot + "ashwood_grass_bermuda_dry.tscn",
            },
            PerChunk: 300, MinScale: 0.8f, MaxScale: 1.8f,
            VisibilityRange: 42.0f, CastShadow: false, MaxRing: 0, MaxSlope: 0.55f,
            Rule: LayerRule.Open, Clustering: 0.55f),

        new("meadow_scrub", new[]
            {
                VegetationRoot + "ashwood_shrub_02_b.tscn",
                VegetationRoot + "ashwood_shrub_03_b.tscn",
                VegetationRoot + "ashwood_nettle_small.tscn",
            },
            PerChunk: 62, MinScale: 0.6f, MaxScale: 1.3f,
            VisibilityRange: 88.0f, CastShadow: false, MaxRing: 1, MaxSlope: 0.6f,
            Rule: LayerRule.Open, Clustering: 0.6f),

        new("deadwood", new[]
            {
                VegetationRoot + "ashwood_dead_tree_trunk.tscn",
                VegetationRoot + "ashwood_dead_log.tscn",
                VegetationRoot + "ashwood_bark_debris_a.tscn",
                VegetationRoot + "ashwood_bark_debris_c.tscn",
            },
            PerChunk: 22, MinScale: 0.8f, MaxScale: 1.5f,
            VisibilityRange: 96.0f, CastShadow: true, MaxRing: 0, MaxSlope: 0.5f,
            Rule: LayerRule.Forest, Clustering: 0.5f),

        new("rocks", new[]
            {
                VegetationRoot + "ashwood_rock_moss_01.tscn",
                VegetationRoot + "ashwood_rock_moss_03.tscn",
                VegetationRoot + "ashwood_rock_moss_04.tscn",
                VegetationRoot + "ashwood_rock_moss_06.tscn",
            },
            PerChunk: 48, MinScale: 0.7f, MaxScale: 2.6f,
            VisibilityRange: 150.0f, CastShadow: true, MaxRing: 1, MaxSlope: 1.1f,
            Rule: LayerRule.Rock, Clustering: 0.65f),

        new("riverbank", new[]
            {
                VegetationRoot + "ashwood_rock_moss_02.tscn",
                VegetationRoot + "ashwood_rock_moss_05.tscn",
                VegetationRoot + "ashwood_nettle_tall.tscn",
                VegetationRoot + "ashwood_fern_02_b.tscn",
            },
            PerChunk: 72, MinScale: 0.7f, MaxScale: 1.6f,
            VisibilityRange: 80.0f, CastShadow: false, MaxRing: 0, MaxSlope: 0.7f,
            Rule: LayerRule.Riverbank, Clustering: 0.7f),
    };

    private readonly Dictionary<Vector2I, Node3D> _chunks = new();

    /// <summary>
    /// Meshes are loaded from their scenes once and shared by every chunk. Loading
    /// per chunk would re-parse the same PackedScenes thousands of times over a
    /// walk across the county.
    /// </summary>
    private readonly Dictionary<string, List<(Mesh Mesh, Transform3D Local)>> _meshCache = new();

    public override void _Ready()
    {
        if (GetParent() is CountyWorld world)
        {
            world.RegisterSource(this);
        }
    }

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk))
        {
            return;
        }

        var holder = new Node3D { Name = $"Veg_{chunk.X}_{chunk.Y}" };
        AddChild(holder);
        _chunks[chunk] = holder;

        foreach (Layer layer in Layers)
        {
            if (ring > layer.MaxRing)
            {
                continue;
            }

            List<Transform3D> placements = ScatterLayer(chunk, layer);
            if (placements.Count == 0)
            {
                continue;
            }

            EmitLayer(holder, layer, placements);
        }
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        if (_chunks.Remove(chunk, out Node3D? holder))
        {
            holder.QueueFree();
        }
    }

    public void UpdateChunkRing(Vector2I chunk, int ring)
    {
        // Layers are selected by ring at build time, so a ring change means the
        // chunk needs different layers entirely.
        ReleaseChunk(chunk);
        BuildChunk(chunk, ring);
    }

    /// <summary>
    /// Places one layer inside one chunk.
    ///
    /// Candidates are drawn around cluster seeds rather than uniformly, because
    /// real vegetation grows in stands and thickets - a uniform Poisson scatter
    /// reads as wallpaper the moment you look across a valley.
    /// </summary>
    private List<Transform3D> ScatterLayer(Vector2I chunk, in Layer layer)
    {
        var placements = new List<Transform3D>();

        // Seeding from the chunk coordinate and the layer name makes the result
        // stable across streaming and independent between layers.
        var rng = new RandomNumberGenerator
        {
            Seed = (ulong)(chunk.X * 73856093 ^ chunk.Y * 19349663 ^ layer.Name.GetHashCode()),
        };

        Vector2 origin = CountyChunks.Origin(chunk);
        int candidates = Mathf.RoundToInt(layer.PerChunk * DensityScale);
        if (candidates <= 0)
        {
            return placements;
        }

        int clusterCount = Mathf.Max(1, Mathf.RoundToInt(candidates / 14.0f));
        var clusters = new Vector2[clusterCount];
        for (int i = 0; i < clusterCount; i++)
        {
            clusters[i] = origin + new Vector2(
                rng.RandfRange(0.0f, CountyChunks.Size),
                rng.RandfRange(0.0f, CountyChunks.Size));
        }

        float clusterRadius = CountyChunks.Size / Mathf.Sqrt(clusterCount) * 0.55f;

        for (int i = 0; i < candidates; i++)
        {
            Vector2 point;
            if (rng.Randf() < layer.Clustering)
            {
                Vector2 seed = clusters[rng.RandiRange(0, clusterCount - 1)];
                // Square-rooting the radius fills the disc evenly instead of
                // piling every instance onto the seed point.
                float radius = Mathf.Sqrt(rng.Randf()) * clusterRadius;
                float angle = rng.RandfRange(0.0f, Mathf.Tau);
                point = seed + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            else
            {
                point = origin + new Vector2(
                    rng.RandfRange(0.0f, CountyChunks.Size),
                    rng.RandfRange(0.0f, CountyChunks.Size));
            }

            // Clusters can spill past the chunk edge; clamping rather than
            // rejecting keeps the density even right up to the border.
            point.X = Mathf.Clamp(point.X, origin.X, origin.X + CountyChunks.Size);
            point.Y = Mathf.Clamp(point.Y, origin.Y, origin.Y + CountyChunks.Size);

            if (TryPlace(point, layer, rng, out Transform3D transform))
            {
                placements.Add(transform);
            }
        }

        return placements;
    }

    private static bool TryPlace(
        Vector2 point, in Layer layer, RandomNumberGenerator rng, out Transform3D transform)
    {
        transform = Transform3D.Identity;

        if (!CountyMap.IsPlayable(point.X, point.Y))
        {
            return false;
        }

        float height = CountyMap.Height(point.X, point.Y);

        // Nothing grows under water, and the waterline itself is gravel.
        float water = CountyMap.WaterSurfaceY(point.X, point.Y);
        bool nearWater = water > float.MinValue;
        if (nearWater && height < water + 0.35f)
        {
            return false;
        }

        Vector3 normal = CountyMap.Normal(point.X, point.Y, 2.0f);
        float slope = Mathf.Acos(Mathf.Clamp(normal.Y, -1.0f, 1.0f));
        if (slope > layer.MaxSlope)
        {
            return false;
        }

        CountyMap.Biome biome = CountyMap.BiomeAt(point.X, point.Y, height, slope);

        // Roads and their shoulders stay clear, or the county grows a forest
        // through its own carriageways.
        for (int i = 0; i < CountyMap.Roads.Length; i++)
        {
            float clear = CountyMap.RoadShoulder(CountyMap.Roads[i].Class) * 1.15f;
            if (CountyMap.RoadLines[i].IsFarFrom(point, clear))
            {
                continue;
            }

            if (CountyMap.RoadLines[i].Distance(point) < clear)
            {
                return false;
            }
        }

        float chance = layer.Rule switch
        {
            LayerRule.Forest => CountyMap.ForestDensity(point.X, point.Y, height, slope),

            // Open ground thins out where the canopy closes over, so meadow layers
            // are the complement of forest rather than a separate mask that could
            // overlap it.
            LayerRule.Open => biome switch
            {
                CountyMap.Biome.Meadow => 1.0f,
                CountyMap.Biome.Farmland => 0.28f,
                CountyMap.Biome.Forest => 0.30f,
                CountyMap.Biome.Settled => 0.35f,
                CountyMap.Biome.Riverbank => 0.4f,
                _ => 0.05f,
            } * (1.0f - CountyMap.ForestDensity(point.X, point.Y, height, slope) * 0.6f),

            LayerRule.Riverbank => biome == CountyMap.Biome.Riverbank ? 1.0f : 0.0f,

            LayerRule.Rock => biome == CountyMap.Biome.Rock
                ? 1.0f
                : Mathf.Clamp((slope - 0.35f) / 0.4f, 0.0f, 1.0f) * 0.7f,

            _ => 0.0f,
        };

        if (chance <= 0.001f || rng.Randf() > chance)
        {
            return false;
        }

        float scale = rng.RandfRange(layer.MinScale, layer.MaxScale);
        var basis = new Basis(Vector3.Up, rng.RandfRange(0.0f, Mathf.Tau));

        // Plants grow toward vertical, not perpendicular to the hillside. Leaning
        // only part of the way to the surface normal is what keeps a wooded slope
        // from looking like a hairbrush.
        Vector3 up = Vector3.Up.Lerp(normal, 0.3f).Normalized();
        if (up.DistanceTo(Vector3.Up) > 0.0001f)
        {
            Vector3 axis = Vector3.Up.Cross(up);
            if (axis.LengthSquared() > 0.000001f)
            {
                basis = new Basis(axis.Normalized(), Vector3.Up.AngleTo(up)) * basis;
            }
        }

        // Sink very slightly so nothing hovers on a hairline of ground.
        transform = new Transform3D(basis.Scaled(Vector3.One * scale),
            new Vector3(point.X, height - 0.08f * scale, point.Y));
        return true;
    }

    private void EmitLayer(Node3D holder, in Layer layer, List<Transform3D> placements)
    {
        // Split placements across the layer's scene variants so a stand is mixed
        // species rather than one cloned plant.
        int variants = layer.Scenes.Length;
        var buckets = new List<Transform3D>[variants];
        for (int i = 0; i < variants; i++)
        {
            buckets[i] = new List<Transform3D>();
        }

        for (int i = 0; i < placements.Count; i++)
        {
            buckets[i % variants].Add(placements[i]);
        }

        for (int variant = 0; variant < variants; variant++)
        {
            if (buckets[variant].Count == 0)
            {
                continue;
            }

            List<(Mesh Mesh, Transform3D Local)> parts = GetParts(layer.Scenes[variant]);
            for (int part = 0; part < parts.Count; part++)
            {
                var multiMesh = new MultiMesh
                {
                    TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                    Mesh = parts[part].Mesh,
                    InstanceCount = buckets[variant].Count,
                };

                for (int i = 0; i < buckets[variant].Count; i++)
                {
                    multiMesh.SetInstanceTransform(i, buckets[variant][i] * parts[part].Local);
                }

                holder.AddChild(new MultiMeshInstance3D
                {
                    Name = $"{layer.Name}_{variant:D2}_{part:D2}",
                    Multimesh = multiMesh,
                    CastShadow = layer.CastShadow
                        ? GeometryInstance3D.ShadowCastingSetting.On
                        : GeometryInstance3D.ShadowCastingSetting.Off,
                    VisibilityRangeEnd = layer.VisibilityRange,
                    VisibilityRangeEndMargin = layer.VisibilityRange * 0.14f,
                    VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
                });
            }
        }
    }

    private List<(Mesh Mesh, Transform3D Local)> GetParts(string scenePath)
    {
        if (_meshCache.TryGetValue(scenePath, out List<(Mesh, Transform3D)>? cached))
        {
            return cached;
        }

        var parts = new List<(Mesh, Transform3D)>();
        var packed = ResourceLoader.Load<PackedScene>(scenePath);
        if (packed == null)
        {
            GD.PushWarning($"CountyVegetation: could not load {scenePath}");
            _meshCache[scenePath] = parts;
            return parts;
        }

        Node source = packed.Instantiate();
        CollectMeshes(source, Transform3D.Identity, parts);
        source.Free();

        _meshCache[scenePath] = parts;
        return parts;
    }

    private static void CollectMeshes(
        Node node, Transform3D parent, List<(Mesh, Transform3D)> results)
    {
        Transform3D local = parent;
        if (node is Node3D spatial)
        {
            local = parent * spatial.Transform;
        }

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
    /// The vegetation scenes assign their materials as surface_material_override
    /// entries on the MeshInstance3D (bark on surface 0, leaves on surface 1)
    /// rather than on the mesh resource. MultiMeshInstance3D has no per-surface
    /// override and a single MaterialOverride would flatten trunk and canopy into
    /// one material, so the overrides are baked into a duplicated mesh here.
    /// Without this every plant renders untextured white.
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

    public void Rebuild()
    {
        foreach (Vector2I chunk in new List<Vector2I>(_chunks.Keys))
        {
            ReleaseChunk(chunk);
        }
    }

    public int ResidentChunkCount => _chunks.Count;
}
