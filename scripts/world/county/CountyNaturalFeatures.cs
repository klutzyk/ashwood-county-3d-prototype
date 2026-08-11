#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Streams authored wilderness landmarks built from project-owned wrappers over
/// the Poly Haven nature set. Ordinary vegetation stays batched in
/// <see cref="CountyVegetation"/>; this layer is reserved for collidable,
/// recognizable spaces worth exploring.
/// </summary>
[Tool]
public partial class CountyNaturalFeatures : Node3D, ICountyChunkSource
{
    [Export(PropertyHint.Range, "2,6,1")] public int FeatureRadius { get; set; } = 5;
    public int ChunkRadius => FeatureRadius;

    private const string NatureRoot = "res://assets/environment/nature/polyhaven/";

    private static readonly string[] BoulderScenes =
    {
        "ashwood_boulder_01.tscn",
        "ashwood_rock_moss_01.tscn",
        "ashwood_rock_moss_02.tscn",
        "ashwood_rock_moss_03.tscn",
        "ashwood_rock_moss_04.tscn",
        "ashwood_rock_moss_05.tscn",
        "ashwood_rock_moss_06.tscn",
    };

    private static readonly string[] ForestFloorScenes =
    {
        "ashwood_dead_log.tscn",
        "ashwood_dead_tree_trunk.tscn",
        "ashwood_pine_roots_a.tscn",
        "ashwood_pine_roots_b.tscn",
        "ashwood_tree_stump_01.tscn",
        "ashwood_tree_stump_02.tscn",
    };

    private static readonly string[] UnderstoryScenes =
    {
        "ashwood_fern_02_a.tscn",
        "ashwood_fern_02_b.tscn",
        "ashwood_fern_02_c.tscn",
        "ashwood_fern_02_d.tscn",
        "ashwood_nettle_medium.tscn",
        "ashwood_moss_clumped.tscn",
        "ashwood_moss_tall.tscn",
    };

    private static readonly string[] TreeScenes =
    {
        "ashwood_fir_a_lod0.tscn",
        "ashwood_fir_b_lod0.tscn",
        "ashwood_fir_c_lod0.tscn",
        "ashwood_pine_a_lod0.tscn",
        "ashwood_pine_b_lod0.tscn",
        "ashwood_pine_c_lod0.tscn",
    };

    private readonly Dictionary<string, PackedScene> _scenes = new();
    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private Node3D? _editorFeatures;

    public override void _Ready()
    {
        LoadSet(BoulderScenes);
        LoadSet(ForestFloorScenes);
        LoadSet(UnderstoryScenes);
        LoadSet(TreeScenes);

        if (Engine.IsEditorHint() && GetParent() is CountyWorld { EditorPreview: true })
        {
            BuildEditorOverview();
            return;
        }

        if (GetParent() is CountyWorld world) world.RegisterSource(this);
    }

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk)) return;
        var holder = new Node3D { Name = $"NaturalFeatures_{chunk.X}_{chunk.Y}" };
        AddChild(holder);
        _chunks[chunk] = holder;

        var bounds = new Rect2(CountyChunks.Origin(chunk), Vector2.One * CountyChunks.Size);
        for (int i = 0; i < CountyMap.NaturalFeatures.Length; i++)
        {
            CountyMap.NaturalFeature feature = CountyMap.NaturalFeatures[i];
            if (bounds.HasPoint(feature.Position)) BuildFeature(holder, feature, i);
        }
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        if (_chunks.Remove(chunk, out Node3D? holder)) holder.QueueFree();
    }

    public void UpdateChunkRing(Vector2I chunk, int ring) { }

    private void BuildEditorOverview()
    {
        _editorFeatures = new Node3D { Name = "AllNaturalFeatures" };
        AddChild(_editorFeatures);
        for (int i = 0; i < CountyMap.NaturalFeatures.Length; i++)
        {
            BuildFeature(_editorFeatures, CountyMap.NaturalFeatures[i], i);
        }
    }

    private void BuildFeature(Node3D holder, in CountyMap.NaturalFeature feature, int index)
    {
        var root = new Node3D { Name = SafeName(feature.Name) };
        root.SetMeta("feature_name", feature.Name);
        root.SetMeta("feature_kind", feature.Kind.ToString());
        holder.AddChild(root);

        var random = new RandomNumberGenerator { Seed = (ulong)(0xA57D0000 + index * 7919) };
        switch (feature.Kind)
        {
            case CountyMap.NaturalFeatureKind.Cave:
            case CountyMap.NaturalFeatureKind.Grotto:
                BuildCave(root, feature, random);
                break;
            case CountyMap.NaturalFeatureKind.Overlook:
                BuildOverlook(root, feature, random);
                break;
            case CountyMap.NaturalFeatureKind.Escarpment:
                BuildEscarpment(root, feature, random);
                break;
            case CountyMap.NaturalFeatureKind.OldGrowth:
                BuildOldGrowth(root, feature, random);
                break;
            default:
                BuildRockFormation(root, feature, random);
                break;
        }
    }

    private void BuildCave(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random)
    {
        // Horseshoe walls leave a broad entrance and a traversable chamber. The
        // lifted, tilted capstones create a true overhang without cutting a hole
        // into Godot's height-field terrain.
        AddCaveMouth(root, feature);
        Vector2[] frame =
        {
            new(-8.4f, 12.0f), new(8.4f, 12.0f),
            new(-6.2f, 13.5f), new(6.2f, 13.5f), new(0.0f, 14.5f),
        };
        for (int i = 0; i < frame.Length; i++)
        {
            float lift = i < 2 ? 0.25f : (i == 4 ? 1.6f : 1.0f);
            PlaceGrounded(root, Pick(BoulderScenes, random), ToWorld(feature, frame[i]),
                feature.YawDegrees + (i % 2 == 0 ? 78.0f : -78.0f),
                random.RandfRange(3.4f, 4.4f), random, yOffset: lift,
                tilt: i >= 2 ? new Vector3(0, 0, i % 2 == 0 ? 18 : -18) : default);
        }

        for (int i = 0; i < 11; i++)
        {
            float angle = Mathf.DegToRad(62.0f + i * 236.0f / 10.0f);
            Vector2 local = new(Mathf.Sin(angle) * 11.0f, Mathf.Cos(angle) * 13.5f + 7.0f);
            PlaceGrounded(root, Pick(BoulderScenes, random), ToWorld(feature, local),
                feature.YawDegrees + Mathf.RadToDeg(angle), random.RandfRange(3.4f, 5.2f), random,
                yOffset: 0.18f);
        }

        for (int i = -1; i <= 1; i++)
        {
            Vector2 local = new(i * 5.2f, 7.5f + Mathf.Abs(i));
            PlaceGrounded(root, Pick(BoulderScenes, random), ToWorld(feature, local),
                feature.YawDegrees + 90.0f, random.RandfRange(3.8f, 4.8f), random,
                yOffset: 1.2f + (1 - Mathf.Abs(i)) * 0.45f,
                tilt: new Vector3(random.RandfRange(-8, 8), 0, random.RandfRange(10, 20)));
        }

        PlaceForestFloor(root, feature, random, 14, 20.0f, 37.0f);
        PlaceUnderstory(root, feature, random, 26, 13.0f, 43.0f);
        PlaceTrees(root, feature, random, 10, 30.0f, 66.0f);
    }

    private static void AddCaveMouth(Node3D root, in CountyMap.NaturalFeature feature)
    {
        Vector2 world = ToWorld(feature, new Vector2(0, 13.0f));
        var shader = new Shader
        {
            Code = @"shader_type spatial;
render_mode unshaded, cull_disabled;
void fragment() {
    float x = abs((UV.x - 0.5) * 2.0);
    float from_bottom = 1.0 - UV.y;
    float cap_y = max((from_bottom - 0.53) / 0.47, 0.0);
    float half_width = from_bottom < 0.53 ? 1.0 : sqrt(max(1.0 - cap_y * cap_y, 0.0));
    float edge_noise = sin(UV.y * 49.0) * 0.025 + sin(UV.y * 113.0 + 1.7) * 0.012;
    if (x > half_width + edge_noise) { discard; }
    float depth = 0.008 + 0.012 * UV.y;
    ALBEDO = vec3(depth * 0.72, depth, depth * 0.78);
    ROUGHNESS = 1.0;
}",
        };
        var material = new ShaderMaterial { Shader = shader };
        var mouth = new MeshInstance3D
        {
            Name = "RecessedCaveMouth",
            Mesh = new QuadMesh { Size = new Vector2(15.5f, 10.5f), Material = material },
            Position = new Vector3(world.X, CountyMap.Height(world.X, world.Y) + 6.1f, world.Y),
            RotationDegrees = new Vector3(0, feature.YawDegrees, 0),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            VisibilityRangeEnd = 190.0f,
        };
        root.AddChild(mouth);
    }

    private void BuildRockFormation(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random)
    {
        for (int i = 0; i < 20; i++)
        {
            float angle = random.RandfRange(0, Mathf.Tau);
            float radius = Mathf.Sqrt(random.Randf()) * feature.Radius * 0.72f;
            Vector2 local = new(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius * 0.48f);
            PlaceGrounded(root, Pick(BoulderScenes, random), ToWorld(feature, local),
                random.RandfRange(0, 360), random.RandfRange(1.8f, 5.2f), random,
                tilt: new Vector3(random.RandfRange(-13, 13), 0, random.RandfRange(-13, 13)));
        }

        PlaceForestFloor(root, feature, random, 12, 20.0f, feature.Radius * 0.72f);
        PlaceUnderstory(root, feature, random, 30, 16.0f, feature.Radius * 0.82f);
    }

    private void BuildOverlook(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 local = new(-31.0f + i * 6.8f, 22.0f + random.RandfRange(-4, 4));
            PlaceGrounded(root, Pick(BoulderScenes, random), ToWorld(feature, local),
                feature.YawDegrees + 90, random.RandfRange(2.4f, 4.8f), random);
        }

        PlaceForestFloor(root, feature, random, 15, 18.0f, 62.0f);
        PlaceUnderstory(root, feature, random, 24, 22.0f, 72.0f);
        PlaceTrees(root, feature, random, 8, 35.0f, 82.0f);
    }

    private void BuildEscarpment(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random)
    {
        for (int row = 0; row < 3; row++)
        {
            int count = 12 - row * 2;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-48.0f, 48.0f, i / Mathf.Max(1.0f, count - 1.0f));
                Vector2 local = new(x + random.RandfRange(-3, 3), 13.0f + row * 7.5f);
                PlaceGrounded(root, Pick(BoulderScenes, random), ToWorld(feature, local),
                    feature.YawDegrees + random.RandfRange(-20, 20), random.RandfRange(2.5f, 5.4f), random,
                    yOffset: row * 2.8f);
            }
        }

        PlaceForestFloor(root, feature, random, 18, 25.0f, 88.0f);
        PlaceUnderstory(root, feature, random, 32, 28.0f, 102.0f);
    }

    private void BuildOldGrowth(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random)
    {
        // A deliberate inner grove guarantees a readable old-growth room from
        // every approach; the procedural outer ring then dissolves it naturally
        // into the county forest.
        Vector2[] innerGrove =
        {
            new(-22.0f, 10.0f), new(0.0f, 18.0f), new(23.0f, 8.0f),
            new(-30.0f, -18.0f), new(27.0f, -22.0f), new(4.0f, -34.0f),
        };
        for (int i = 0; i < innerGrove.Length; i++)
        {
            PlaceGrounded(root, TreeScenes[i % TreeScenes.Length], ToWorld(feature, innerGrove[i]),
                random.RandfRange(0, 360), random.RandfRange(2.15f, 2.85f), random,
                visibility: 720.0f);
        }

        PlaceTrees(root, feature, random, 18, 42.0f, 112.0f);
        PlaceForestFloor(root, feature, random, 38, 10.0f, 102.0f);
        PlaceUnderstory(root, feature, random, 78, 8.0f, 116.0f);
        for (int i = 0; i < 14; i++)
        {
            Vector2 position = RandomDisc(feature, random, 18.0f, 88.0f);
            if (CountyMap.DistanceToTrail(position) < 6.0f) continue;
            PlaceGrounded(root, Pick(BoulderScenes, random), position,
                random.RandfRange(0, 360), random.RandfRange(0.75f, 1.55f), random);
        }

        // Fallen trunks across the clearing create cover, traversal decisions and
        // the layered decay expected beneath genuinely old trees.
        for (int i = 0; i < 4; i++)
        {
            Vector2 local = new(-18.0f + i * 12.0f, -3.0f + (i % 2) * 15.0f);
            PlaceGrounded(root, i % 2 == 0 ? "ashwood_dead_log.tscn" : "ashwood_dead_tree_trunk.tscn",
                ToWorld(feature, local), feature.YawDegrees + 72.0f + i * 19.0f,
                random.RandfRange(1.5f, 2.25f), random, visibility: 300.0f);
        }
    }

    private void PlaceTrees(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random,
        int count, float inner, float outer)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 position = RandomDisc(feature, random, inner, outer);
            if (CountyMap.DistanceToTrail(position) < 5.5f) continue;
            PlaceGrounded(root, Pick(TreeScenes, random), position,
                random.RandfRange(0, 360), random.RandfRange(0.88f, 1.28f), random, visibility: 620.0f);
        }
    }

    private void PlaceForestFloor(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random,
        int count, float inner, float outer)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 position = RandomDisc(feature, random, inner, outer);
            if (CountyMap.DistanceToTrail(position) < 2.3f) continue;
            PlaceGrounded(root, Pick(ForestFloorScenes, random), position,
                random.RandfRange(0, 360), random.RandfRange(0.9f, 2.0f), random, visibility: 240.0f);
        }
    }

    private void PlaceUnderstory(Node3D root, in CountyMap.NaturalFeature feature, RandomNumberGenerator random,
        int count, float inner, float outer)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 position = RandomDisc(feature, random, inner, outer);
            if (CountyMap.DistanceToTrail(position) < 2.0f) continue;
            PlaceGrounded(root, Pick(UnderstoryScenes, random), position,
                random.RandfRange(0, 360), random.RandfRange(0.75f, 1.55f), random, visibility: 115.0f,
                shadows: false);
        }
    }

    private void PlaceGrounded(Node3D root, string sceneName, Vector2 world, float yaw, float scale,
        RandomNumberGenerator random, float yOffset = 0.0f, Vector3 tilt = default,
        float visibility = 460.0f, bool shadows = true)
    {
        if (!_scenes.TryGetValue(sceneName, out PackedScene? packed)) return;
        Node3D instance = packed.Instantiate<Node3D>();
        instance.Name = $"{SafeName(sceneName)}_{root.GetChildCount():D3}";
        instance.Position = new Vector3(world.X, CountyMap.Height(world.X, world.Y) + yOffset, world.Y);
        instance.RotationDegrees = new Vector3(tilt.X, yaw, tilt.Z);
        float variation = random.RandfRange(0.94f, 1.06f);
        instance.Scale = Vector3.One * scale * variation;
        root.AddChild(instance);
        ConfigureVisuals(instance, visibility, shadows);
    }

    private static Vector2 RandomDisc(in CountyMap.NaturalFeature feature, RandomNumberGenerator random,
        float inner, float outer)
    {
        float angle = random.RandfRange(0, Mathf.Tau);
        float radius = Mathf.Lerp(inner, outer, Mathf.Sqrt(random.Randf()));
        return feature.Position + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
    }

    private static Vector2 ToWorld(in CountyMap.NaturalFeature feature, Vector2 local)
    {
        float yaw = Mathf.DegToRad(feature.YawDegrees);
        Vector2 right = new(Mathf.Cos(yaw), -Mathf.Sin(yaw));
        Vector2 forward = new(Mathf.Sin(yaw), Mathf.Cos(yaw));
        return feature.Position + right * local.X + forward * local.Y;
    }

    private static void ConfigureVisuals(Node node, float visibility, bool shadows)
    {
        if (node is GeometryInstance3D geometry)
        {
            geometry.VisibilityRangeEnd = visibility;
            geometry.VisibilityRangeEndMargin = visibility * 0.12f;
            geometry.VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;
            geometry.CastShadow = shadows
                ? GeometryInstance3D.ShadowCastingSetting.On
                : GeometryInstance3D.ShadowCastingSetting.Off;
            geometry.GIMode = GeometryInstance3D.GIModeEnum.Disabled;
        }

        foreach (Node child in node.GetChildren()) ConfigureVisuals(child, visibility, shadows);
    }

    private void LoadSet(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            if (_scenes.ContainsKey(name)) continue;
            string path = NatureRoot + name;
            if (ResourceLoader.Exists(path) && ResourceLoader.Load<PackedScene>(path) is PackedScene scene)
            {
                _scenes[name] = scene;
            }
            else
            {
                GD.PushWarning($"CountyNaturalFeatures: missing nature scene {path}");
            }
        }
    }

    private static string Pick(string[] choices, RandomNumberGenerator random) =>
        choices[random.RandiRange(0, choices.Length - 1)];

    private static string SafeName(string value) =>
        value.Replace(".tscn", string.Empty).Replace(" ", string.Empty).Replace("'", string.Empty);

    public static int AuthoredFeatureCount => CountyMap.NaturalFeatures.Length;
}
