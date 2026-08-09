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
    /// <summary>
    /// Raised from 3 to 6 once distant trees became imposters. The old radius put
    /// the last tree 768m away, which is nothing across a valley eight kilometres
    /// wide - every ridge view ended in bare ground. Real geometry could not have
    /// afforded the increase; two-triangle cards can.
    /// </summary>
    [Export] public int VegetationRadius { get; set; } = 6;

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
        float Clustering = 0.8f,
        /// <summary>Billboard imposters rather than instanced scene geometry.</summary>
        bool Imposter = false,
        /// <summary>Nearest range an imposter appears at, so it never overlaps the real tree.</summary>
        float VisibilityBegin = 0.0f);

    /// <summary>
    /// Imposter card geometry, taken from the bake report of
    /// tools/BakeTreeImposters.cs:
    ///   jacaranda width=24.25 height=19.42 base_offset=9.71
    /// The baker frames the tree with an orthogonal camera whose size is twice the
    /// bounding radius, so the card must be that same square for the tree to come
    /// back out at the size it went in at.
    /// </summary>
    private const float ImposterCardSize = 24.25f;

    /// <summary>Height of the baked frame's centre above the trunk base.</summary>
    private const float ImposterCentreHeight = 9.71f;

    private const string ImposterMaterialPath = "res://assets/materials/county_tree_imposter.tres";
    private const string ImposterAtlasRoot = "res://assets/environment/nature/imposters/";

    /// <summary>
    /// One baked tree, with the card geometry its atlas was rendered against.
    ///
    /// These numbers come straight from the bake report of BakeTreeImposters. The
    /// baker frames each tree with an orthogonal camera sized to twice its
    /// bounding radius, so a card must be that same square or the tree comes back
    /// out at the wrong size. They cannot be shared across species: a jacaranda is
    /// 24m wide and 19m tall while a fir is 6m wide and 19m tall, and using one
    /// set of numbers for both either squashes the conifers or floats them.
    /// </summary>
    private readonly record struct ImposterSpecies(
        string Atlas, float CardSize, float CentreHeight);

    private static readonly ImposterSpecies[] ConiferImposters =
    {
        new("fir_a_imposter.png", 18.89f, 9.44f),
        new("fir_b_imposter.png", 13.94f, 6.97f),
        new("fir_c_imposter.png", 12.59f, 6.30f),
        new("pine_a_imposter.png", 20.36f, 10.18f),
        new("pine_b_imposter.png", 14.98f, 7.49f),
        new("pine_c_imposter.png", 17.45f, 8.72f),
    };

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
        // Conifers, not the jacaranda.
        //
        // Ashwood is a cool-temperate county and the reference photography for the
        // genre is wall-to-wall spruce and fir; a tropical broadleaf was simply the
        // wrong tree, and it was only ever there because it happened to be the one
        // asset that had been through the Blender pipeline. Six mature variants now
        // exist - three fir, three pine, 12.5m to 20.4m - and mixing all six is what
        // stops a stand reading as one tree stamped repeatedly.
        //
        // Scales stay near 1.0 because these are photoscans at their real size,
        // unlike the jacaranda which had to be shrunk to pass for a temperate tree.
        new("canopy", new[]
            {
                VegetationRoot + "ashwood_fir_a_lod0.tscn",
                VegetationRoot + "ashwood_fir_b_lod0.tscn",
                VegetationRoot + "ashwood_pine_a_lod0.tscn",
                VegetationRoot + "ashwood_pine_c_lod0.tscn",
            },
            PerChunk: 46, MinScale: 0.78f, MaxScale: 1.18f,
            VisibilityRange: 120.0f, CastShadow: true, MaxRing: 1, MaxSlope: 0.66f,
            Rule: LayerRule.Forest, Clustering: 0.86f),

        new("canopy_mid", new[]
            {
                VegetationRoot + "ashwood_fir_a_lod1.tscn",
                VegetationRoot + "ashwood_fir_b_lod1.tscn",
                VegetationRoot + "ashwood_fir_c_lod1.tscn",
                VegetationRoot + "ashwood_pine_a_lod1.tscn",
                VegetationRoot + "ashwood_pine_b_lod1.tscn",
                VegetationRoot + "ashwood_pine_c_lod1.tscn",
            },
            PerChunk: 80, MinScale: 0.78f, MaxScale: 1.18f,
            VisibilityRange: 250.0f, CastShadow: false, MaxRing: 1, MaxSlope: 0.66f,
            Rule: LayerRule.Forest, Clustering: 0.86f),

        // Saplings and young growth under the canopy, so the forest floor is not
        // bare ground between mature trunks.
        new("understorey_conifer", new[]
            {
                VegetationRoot + "ashwood_fir_sapling_a.tscn",
                VegetationRoot + "ashwood_fir_sapling_b.tscn",
                VegetationRoot + "ashwood_pine_sapling_a.tscn",
                VegetationRoot + "ashwood_pine_sapling_c.tscn",
            },
            PerChunk: 110, MinScale: 0.7f, MaxScale: 1.5f,
            VisibilityRange: 90.0f, CastShadow: false, MaxRing: 1, MaxSlope: 0.7f,
            Rule: LayerRule.Forest, Clustering: 0.9f),

        // The forest proper. Everything above is detail applied to the nearest few
        // hundred metres; this is the layer that actually makes the county read as
        // wooded from a ridgeline, and at two triangles an instance it is by far
        // the cheapest thing in the scene. It starts where canopy_mid fades out so
        // a tree is never drawn as both card and geometry at once.
        new("canopy_imposter", new[]
            {
                ImposterMaterialPath,
            },
            // Dense enough that crowns overlap. Anything sparser leaves ground
            // visible between every tree, which reads as an orchard rather than
            // forest - the canopy closing over is the whole difference. At two
            // triangles an instance this is affordable where geometry never was:
            // a full ring-6 set is around 300k triangles, less than a single one
            // of the old LOD1 stands.
            PerChunk: 780, MinScale: 0.34f, MaxScale: 0.78f,
            VisibilityRange: 2400.0f, CastShadow: false, MaxRing: 6, MaxSlope: 0.62f,
            Rule: LayerRule.Forest, Clustering: 0.86f,
            Imposter: true, VisibilityBegin: 205.0f),

        new("shrubs", new[]
            {
                VegetationRoot + "ashwood_shrub_01.tscn",
                VegetationRoot + "ashwood_shrub_02_a.tscn",
                VegetationRoot + "ashwood_shrub_02_c.tscn",
                VegetationRoot + "ashwood_shrub_03_a.tscn",
                VegetationRoot + "ashwood_shrub_03_c.tscn",
            },
            PerChunk: 260, MinScale: 0.7f, MaxScale: 1.6f,
            VisibilityRange: 78.0f, CastShadow: false, MaxRing: 0, MaxSlope: 0.66f,
            Rule: LayerRule.Forest, Clustering: 0.7f),

        new("ferns", new[]
            {
                VegetationRoot + "ashwood_fern_02_a.tscn",
                VegetationRoot + "ashwood_fern_02_c.tscn",
                VegetationRoot + "ashwood_nettle_medium.tscn",
                VegetationRoot + "ashwood_nettle_tall.tscn",
            },
            // Broad-leaved ground plants carpeting the forest floor is the single
            // most characteristic thing in the reference photography, and the one
            // the county had least of. These are the closest match available.
            PerChunk: 520, MinScale: 0.75f, MaxScale: 1.6f,
            VisibilityRange: 72.0f, CastShadow: false, MaxRing: 1, MaxSlope: 0.62f,
            Rule: LayerRule.Forest, Clustering: 0.72f),

        new("grass", new[]
            {
                VegetationRoot + "ashwood_grass_bermuda_small.tscn",
                VegetationRoot + "ashwood_grass_bermuda_medium.tscn",
                VegetationRoot + "ashwood_grass_bermuda_dry.tscn",
            },
            // Waist-high grass across open ground, as in the reference shots where
            // vehicles and buildings sit half-swallowed by it. Short visibility
            // range keeps the cost to the near field where it actually reads.
            // MaxRing 1 rather than 0. At ring 0 the grass existed only in the
            // chunk the player stood in, so open country read as bare texture the
            // moment you looked more than a hundred metres ahead - which is most
            // of the time in a county this size.
            PerChunk: 900, MinScale: 0.9f, MaxScale: 2.2f,
            VisibilityRange: 74.0f, CastShadow: false, MaxRing: 1, MaxSlope: 0.55f,
            Rule: LayerRule.Open, Clustering: 0.5f),

        new("meadow_scrub", new[]
            {
                VegetationRoot + "ashwood_shrub_02_b.tscn",
                VegetationRoot + "ashwood_shrub_03_b.tscn",
                VegetationRoot + "ashwood_nettle_small.tscn",
            },
            PerChunk: 150, MinScale: 0.6f, MaxScale: 1.4f,
            VisibilityRange: 92.0f, CastShadow: false, MaxRing: 1, MaxSlope: 0.6f,
            Rule: LayerRule.Open, Clustering: 0.6f),

        new("deadwood", new[]
            {
                VegetationRoot + "ashwood_dead_tree_trunk.tscn",
                VegetationRoot + "ashwood_dead_log.tscn",
                VegetationRoot + "ashwood_bark_debris_a.tscn",
                VegetationRoot + "ashwood_bark_debris_c.tscn",
                VegetationRoot + "ashwood_tree_stump_01.tscn",
                VegetationRoot + "ashwood_pine_roots_a.tscn",
            },
            // Forests are messy. Fallen trunks, stumps, root plates and bark
            // litter do more for "this is real woodland" than another live tree.
            PerChunk: 70, MinScale: 0.8f, MaxScale: 1.6f,
            VisibilityRange: 104.0f, CastShadow: true, MaxRing: 0, MaxSlope: 0.5f,
            Rule: LayerRule.Forest, Clustering: 0.5f),

        new("rocks", new[]
            {
                VegetationRoot + "ashwood_rock_moss_01.tscn",
                VegetationRoot + "ashwood_rock_moss_03.tscn",
                VegetationRoot + "ashwood_rock_moss_04.tscn",
                VegetationRoot + "ashwood_rock_moss_06.tscn",
            },
            PerChunk: 110, MinScale: 0.7f, MaxScale: 3.1f,
            VisibilityRange: 168.0f, CastShadow: true, MaxRing: 1, MaxSlope: 1.1f,
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
        if (layer.Imposter)
        {
            EmitImposterLayer(holder, layer, placements);
            return;
        }

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

                var instance = new MultiMeshInstance3D
                {
                    Name = $"{layer.Name}_{variant:D2}_{part:D2}",
                    Multimesh = multiMesh,
                    CastShadow = layer.CastShadow
                        ? GeometryInstance3D.ShadowCastingSetting.On
                        : GeometryInstance3D.ShadowCastingSetting.Off,
                    VisibilityRangeEnd = layer.VisibilityRange,
                    VisibilityRangeEndMargin = layer.VisibilityRange * 0.14f,
                    VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
                };

                // Without this the node sits at the world origin while its
                // instances are placed in world space, so Godot measures both
                // frustum culling and VisibilityRangeEnd from world zero. Every
                // vegetation batch in every resident chunk then counts as being
                // right next to the camera and draws every frame, whatever the
                // range says - which put full-detail tree geometry from the whole
                // streamed radius into the frame at once and cost roughly an
                // order of magnitude of frame rate.
                instance.CustomAabb = InstanceBounds(
                    buckets[variant], parts[part].Mesh, parts[part].Local);
                holder.AddChild(instance);
            }
        }
    }

    /// <summary>
    /// Emits a chunk's worth of billboard trees as a single MultiMesh of quads.
    ///
    /// The scatter's rotation and hillside lean are deliberately thrown away: the
    /// shader rebuilds the card's basis every frame to face the camera, so any
    /// rotation baked in here would be overwritten, and a leaning card would only
    /// shrink the tree. Only position and a uniform scale survive.
    /// </summary>
    /// <summary>
    /// Emits a chunk's trees as a single MultiMesh of billboard cards.
    ///
    /// One batch, not one per species. Splitting by species cost six draw calls a
    /// chunk, and across a ring-6 imposter set that was over a thousand for the
    /// trees alone in a world that measures as draw-call bound - identical frame
    /// rate at 480x270 and at 1920x1080, so submission cost is what is being spent.
    ///
    /// It collapses cleanly because every conifer bake is a square of side equal
    /// to the tree's height, centred at half that height. So the card can be a
    /// unit quad scaled by the tree's height, and the only per-species difference
    /// left is which atlas row to sample - which rides along in the instance's
    /// custom data.
    /// </summary>
    private void EmitImposterLayer(Node3D holder, in Layer layer, List<Transform3D> placements)
    {
        Material? material = LoadConiferImposterMaterial();
        if (material == null)
        {
            return;
        }

        var quad = new QuadMesh
        {
            Size = new Vector2(1.0f, 1.0f),
            Material = material,
        };

        var multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = quad,

            // Colour carries per-tree tint, custom data carries the species row.
            UseColors = true,
            UseCustomData = true,
            InstanceCount = placements.Count,
        };

        int speciesCount = ConiferImposters.Length;
        var rng = new RandomNumberGenerator { Seed = (ulong)placements.Count * 2654435761UL };

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (int i = 0; i < placements.Count; i++)
        {
            Transform3D placement = placements[i];
            int species = i % speciesCount;
            ImposterSpecies chosen = ConiferImposters[species];

            // The scatter's own scale varies tree size within a species; the card
            // side is the species height, so the two multiply.
            float scatter = placement.Basis.X.Length();
            float side = chosen.CardSize * scatter;

            var origin = new Vector3(
                placement.Origin.X,
                placement.Origin.Y + (side * 0.5f),
                placement.Origin.Z);

            multiMesh.SetInstanceTransform(i,
                new Transform3D(Basis.Identity.Scaled(Vector3.One * side), origin));

            float value = rng.RandfRange(0.72f, 1.12f);
            float warmth = rng.RandfRange(-0.06f, 0.06f);
            multiMesh.SetInstanceColor(i, new Color(
                value * (1.0f + warmth),
                value,
                value * (1.0f - warmth * 0.65f)));

            // Normalised so the shader can recover the row by multiplying back up.
            multiMesh.SetInstanceCustomData(i,
                new Color(species / (float)speciesCount, 0.0f, 0.0f, 0.0f));

            float reach = side * 0.75f;
            min.X = Mathf.Min(min.X, origin.X - reach);
            min.Y = Mathf.Min(min.Y, origin.Y - reach);
            min.Z = Mathf.Min(min.Z, origin.Z - reach);
            max.X = Mathf.Max(max.X, origin.X + reach);
            max.Y = Mathf.Max(max.Y, origin.Y + reach);
            max.Z = Mathf.Max(max.Z, origin.Z + reach);
        }

        var instance = new MultiMeshInstance3D
        {
            Name = layer.Name,
            Multimesh = multiMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            VisibilityRangeBegin = layer.VisibilityBegin,
            VisibilityRangeBeginMargin = layer.VisibilityBegin * 0.16f,
            VisibilityRangeEnd = layer.VisibilityRange,
            VisibilityRangeEndMargin = layer.VisibilityRange * 0.1f,
            VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
        };

        // World-space instance transforms under a node at the origin: without an
        // explicit box Godot culls and range-tests from world zero, so nothing
        // culls and every chunk draws.
        instance.CustomAabb = new Aabb(min, max - min);
        holder.AddChild(instance);
    }

    /// <summary>
    /// World-space bounds of one batch of scattered scene geometry.
    ///
    /// Each instance contributes its mesh's own extent, scaled by that instance,
    /// so a batch of large trees gets a correspondingly larger box than a batch
    /// of grass tufts and neither is clipped early at a glancing angle.
    /// </summary>
    private static Aabb InstanceBounds(
        List<Transform3D> placements, Mesh mesh, Transform3D local)
    {
        Aabb meshBounds = mesh.GetAabb();
        float reach = Mathf.Max(
            meshBounds.Size.Length(),
            meshBounds.Position.Length() + meshBounds.Size.Length());

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Transform3D placement in placements)
        {
            Vector3 origin = (placement * local).Origin;
            float scaled = reach * Mathf.Max(placement.Basis.X.Length(), 0.001f);

            min.X = Mathf.Min(min.X, origin.X - scaled);
            min.Y = Mathf.Min(min.Y, origin.Y - scaled);
            min.Z = Mathf.Min(min.Z, origin.Z - scaled);
            max.X = Mathf.Max(max.X, origin.X + scaled);
            max.Y = Mathf.Max(max.Y, origin.Y + scaled);
            max.Z = Mathf.Max(max.Z, origin.Z + scaled);
        }

        return new Aabb(min, max - min);
    }

    /// <summary>
    /// World-space bounds of a chunk's imposter cards, padded for the card's own
    /// width and height. The shader billboards each card in vertex(), so the
    /// bounds have to allow for it swinging a full card-width either side of its
    /// instance origin as the camera moves around it.
    /// </summary>
    private static Aabb ChunkCardBounds(
        List<Transform3D> placements, float cardSize, float centreHeight)
    {
        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Transform3D placement in placements)
        {
            float scale = placement.Basis.X.Length();
            float reach = cardSize * scale * 0.5f;
            Vector3 origin = placement.Origin;

            min.X = Mathf.Min(min.X, origin.X - reach);
            min.Y = Mathf.Min(min.Y, origin.Y - reach);
            min.Z = Mathf.Min(min.Z, origin.Z - reach);
            max.X = Mathf.Max(max.X, origin.X + reach);
            max.Y = Mathf.Max(max.Y, origin.Y + (centreHeight * scale) + reach);
            max.Z = Mathf.Max(max.Z, origin.Z + reach);
        }

        return new Aabb(min, max - min);
    }

    private Material? _coniferMaterial;
    private bool _coniferMaterialTried;

    /// <summary>
    /// The single conifer imposter material, built from the combined atlas.
    /// </summary>
    private Material? LoadConiferImposterMaterial()
    {
        if (_coniferMaterialTried)
        {
            return _coniferMaterial;
        }

        _coniferMaterialTried = true;

        if (!ResourceLoader.Exists(ImposterMaterialPath) ||
            ResourceLoader.Load(ImposterMaterialPath) is not ShaderMaterial baseMaterial)
        {
            GD.PushWarning(
                $"CountyVegetation: imposter material missing at {ImposterMaterialPath}; " +
                "distant forest will not render. Run tools/bake_tree_imposters.tscn.");
            return null;
        }

        string atlasPath = ImposterAtlasRoot + "conifer_atlas.png";
        if (!ResourceLoader.Exists(atlasPath))
        {
            GD.PushWarning($"CountyVegetation: combined conifer atlas missing at {atlasPath}. " +
                           "Run tools/bake_tree_imposters.tscn.");
            return null;
        }

        var material = new ShaderMaterial { Shader = baseMaterial.Shader };
        foreach (string uniform in new[]
                 { "atlas_cells", "alpha_threshold", "canopy_tint", "roundness" })
        {
            Variant value = baseMaterial.GetShaderParameter(uniform);
            if (value.VariantType != Variant.Type.Nil)
            {
                material.SetShaderParameter(uniform, value);
            }
        }

        material.SetShaderParameter("imposter_atlas", ResourceLoader.Load<Texture2D>(atlasPath));
        material.SetShaderParameter("atlas_rows", ConiferImposters.Length);

        _coniferMaterial = material;
        return _coniferMaterial;
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
