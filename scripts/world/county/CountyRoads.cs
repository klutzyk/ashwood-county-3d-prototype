#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Lays the county's roads, tracks and freight line onto the graded ground.
///
/// <see cref="CountyMap"/> already smooths the terrain toward a road-following
/// grade within a few metres of every centreline, so this does not re-flatten
/// anything - it sweeps a cross-section along each route and drapes it on the
/// ground that is already there.
///
/// The control points in CountyMap are hundreds of metres apart, which is fine as
/// a description of where a road goes and useless as geometry: swept directly they
/// produce a visible kink at every vertex. Each route is resampled through a
/// Catmull-Rom spline once at startup, and the dense result is what both the
/// ribbon mesher and the rail instancer consume.
/// </summary>
[Tool]
public partial class CountyRoads : Node3D, ICountyChunkSource
{
    /// <summary>
    /// Roads read as landscape features from a long way off - a highway cutting
    /// across a valley is one of the strongest cues that a place is inhabited - so
    /// they stream further than vegetation but not as far as terrain.
    /// </summary>
    [Export] public int RoadRadius { get; set; } = 6;

    /// <summary>Spacing of the resampled centreline, in metres.</summary>
    [Export] public float SampleSpacing { get; set; } = 7.0f;

    public int ChunkRadius => RoadRadius;

    private const string MaterialRoot = "res://assets/materials/";

    /// <summary>
    /// How far above the terrain the carriageway sits.
    ///
    /// Enough to win the depth test against ground that is only sampled every four
    /// metres, small enough that the kerb does not read as a step. Too little and
    /// the road strobes through the terrain; too much and it floats.
    /// </summary>
    private const float SurfaceLift = 0.11f;

    /// <summary>
    /// The Old Mill Bridge is a hand-built landmark that already carries the road
    /// across the Blackwater here. Any generated crossing in this box would sit
    /// inside it.
    /// </summary>
    private static readonly Rect2 OldMillBridgeReach = new(-236.0f, -50.0f, 124.0f, 100.0f);

    private sealed class RoadPath
    {
        public string Name = string.Empty;
        public CountyMap.RoadClass Class;
        public Vector2[] Points = Array.Empty<Vector2>();
        public float[] Along = Array.Empty<float>();
        public float MinX, MaxX, MinZ, MaxZ;

        public bool IsFarFrom(Vector2 min, Vector2 max, float margin) =>
            max.X < MinX - margin || min.X > MaxX + margin ||
            max.Y < MinZ - margin || min.Y > MaxZ + margin;
    }

    private readonly List<RoadPath> _paths = new();
    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private readonly Dictionary<CountyMap.RoadClass, Material> _materials = new();
    private Material? _railMaterial;
    private Material? _bridgeMaterial;

    public override void _Ready()
    {
        LoadMaterials();
        BuildPaths();

        if (GetParent() is CountyWorld world)
        {
            world.RegisterSource(this);
        }
    }

    private Material Load(string path, Color fallback, float roughness = 0.95f)
    {
        if (ResourceLoader.Exists(path) && ResourceLoader.Load(path) is Material loaded)
        {
            return loaded;
        }

        return new StandardMaterial3D { AlbedoColor = fallback, Roughness = roughness };
    }

    private void LoadMaterials()
    {
        // The paved classes reuse Main Street's own asphalt so the county road
        // network and the hand-authored town slice cannot drift apart in tone.
        Material asphalt = Load(MaterialRoot + "ashwood_main_street_asphalt.tres",
            new Color(0.19f, 0.19f, 0.20f));
        Material gravel = Load(MaterialRoot + "county/county_gravel.tres",
            new Color(0.44f, 0.42f, 0.38f));
        Material dirt = Load(MaterialRoot + "county/county_dirt_yard.tres",
            new Color(0.34f, 0.27f, 0.20f));

        _materials[CountyMap.RoadClass.Highway] = asphalt;
        _materials[CountyMap.RoadClass.Paved] = asphalt;
        _materials[CountyMap.RoadClass.Gravel] = gravel;
        _materials[CountyMap.RoadClass.Dirt] = dirt;
        _materials[CountyMap.RoadClass.Railway] = gravel;

        _railMaterial = Load(MaterialRoot + "county/county_rusted_steel.tres",
            new Color(0.26f, 0.21f, 0.18f), 0.6f);
        _bridgeMaterial = Load(MaterialRoot + "county/county_concrete.tres",
            new Color(0.52f, 0.51f, 0.49f));
    }

    /// <summary>Resamples every route into a dense, smooth centreline.</summary>
    private void BuildPaths()
    {
        _paths.Clear();

        for (int i = 0; i < CountyMap.Roads.Length; i++)
        {
            CountyMap.Road road = CountyMap.Roads[i];
            Vector2[] dense = Resample(road.Points, SampleSpacing);
            if (dense.Length < 2)
            {
                continue;
            }

            var along = new float[dense.Length];
            for (int p = 1; p < dense.Length; p++)
            {
                along[p] = along[p - 1] + dense[p].DistanceTo(dense[p - 1]);
            }

            var path = new RoadPath
            {
                Name = road.Name,
                Class = road.Class,
                Points = dense,
                Along = along,
                MinX = float.MaxValue,
                MaxX = float.MinValue,
                MinZ = float.MaxValue,
                MaxZ = float.MinValue,
            };

            foreach (Vector2 p in dense)
            {
                path.MinX = Mathf.Min(path.MinX, p.X);
                path.MaxX = Mathf.Max(path.MaxX, p.X);
                path.MinZ = Mathf.Min(path.MinZ, p.Y);
                path.MaxZ = Mathf.Max(path.MaxZ, p.Y);
            }

            _paths.Add(path);
        }
    }

    /// <summary>
    /// Catmull-Rom through the control points at a fixed arc-length spacing.
    ///
    /// Catmull-Rom rather than Bezier because it passes through its control points:
    /// the route data says the highway goes through the service station, and a
    /// curve that merely approximates that would drift the road off its own
    /// junctions.
    /// </summary>
    private static Vector2[] Resample(Vector2[] control, float spacing)
    {
        if (control.Length < 2)
        {
            return control;
        }

        var result = new List<Vector2>();

        for (int i = 0; i < control.Length - 1; i++)
        {
            Vector2 p0 = control[Mathf.Max(i - 1, 0)];
            Vector2 p1 = control[i];
            Vector2 p2 = control[i + 1];
            Vector2 p3 = control[Mathf.Min(i + 2, control.Length - 1)];

            // Steps chosen from the straight-line length; the spline is never far
            // enough from its chord for the difference to matter at this spacing.
            int steps = Mathf.Max(Mathf.CeilToInt(p1.DistanceTo(p2) / spacing), 1);
            for (int step = 0; step < steps; step++)
            {
                float t = step / (float)steps;
                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        result.Add(control[^1]);
        return result.ToArray();
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2.0f * p1 +
            (-p0 + p2) * t +
            (2.0f * p0 - 5.0f * p1 + 4.0f * p2 - p3) * t2 +
            (-p0 + 3.0f * p1 - 3.0f * p2 + p3) * t3);
    }

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk))
        {
            return;
        }

        var holder = new Node3D { Name = $"Roads_{chunk.X}_{chunk.Y}" };
        Vector2 min = CountyChunks.Origin(chunk);
        Vector2 max = min + new Vector2(CountyChunks.Size, CountyChunks.Size);

        bool anything = false;

        foreach (RoadPath path in _paths)
        {
            if (path.IsFarFrom(min, max, 40.0f))
            {
                continue;
            }

            anything |= EmitRoad(holder, path, min, max, ring);
        }

        if (anything)
        {
            AddChild(holder);
            _chunks[chunk] = holder;
        }
        else
        {
            holder.QueueFree();
            _chunks[chunk] = new Node3D { Name = $"Roads_{chunk.X}_{chunk.Y}_empty" };
            AddChild(_chunks[chunk]);
        }
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        if (_chunks.Remove(chunk, out Node3D? holder))
        {
            holder.QueueFree();
        }
    }

    /// <summary>
    /// Emits the runs of a route that pass through this chunk.
    ///
    /// A route is clipped by index rather than geometrically: every sample inside
    /// the chunk, plus one either side so adjacent chunks' ribbons overlap by a
    /// segment and leave no gap at the border.
    /// </summary>
    private bool EmitRoad(Node3D holder, RoadPath path, Vector2 min, Vector2 max, int ring)
    {
        float halfWidth = CountyMap.RoadHalfWidth(path.Class);
        float shoulder = CountyMap.RoadShoulder(path.Class);

        var runs = new List<(int Start, int End)>();
        int runStart = -1;

        for (int i = 0; i < path.Points.Length; i++)
        {
            Vector2 p = path.Points[i];
            bool inside = p.X >= min.X - shoulder && p.X <= max.X + shoulder &&
                          p.Y >= min.Y - shoulder && p.Y <= max.Y + shoulder;

            if (inside && runStart < 0)
            {
                runStart = Mathf.Max(i - 1, 0);
            }
            else if (!inside && runStart >= 0)
            {
                runs.Add((runStart, Mathf.Min(i + 1, path.Points.Length - 1)));
                runStart = -1;
            }
        }

        if (runStart >= 0)
        {
            runs.Add((runStart, path.Points.Length - 1));
        }

        bool emitted = false;
        int index = 0;

        foreach ((int start, int end) in runs)
        {
            if (end - start < 1)
            {
                continue;
            }

            ArrayMesh? mesh = BuildRibbon(path, start, end, halfWidth, shoulder,
                out List<Transform3D> sleeperFrames, out List<(Vector3 Deck, float Drop)> piers);
            if (mesh == null)
            {
                continue;
            }

            holder.AddChild(new MeshInstance3D
            {
                Name = $"{Sanitise(path.Name)}_{index}",
                Mesh = mesh,
                MaterialOverride = _materials.GetValueOrDefault(path.Class),
                CastShadow = ring <= 2
                    ? GeometryInstance3D.ShadowCastingSetting.On
                    : GeometryInstance3D.ShadowCastingSetting.Off,
            });

            if (path.Class == CountyMap.RoadClass.Railway && sleeperFrames.Count > 0)
            {
                EmitRail(holder, $"{Sanitise(path.Name)}_{index}", sleeperFrames);
            }

            if (piers.Count > 0)
            {
                EmitPiers(holder, $"{Sanitise(path.Name)}_{index}", piers);
            }

            emitted = true;
            index++;
        }

        return emitted;
    }

    private static string Sanitise(string name) => name.Replace(" ", "").Replace("'", "");

    /// <summary>
    /// Sweeps the cross-section along a run of the centreline.
    ///
    /// The section is five points wide: two shoulders, two carriageway edges and a
    /// crowned centre. The crown matters more than it looks - a perfectly flat
    /// ribbon reads as a decal laid on the ground, where a cambered one catches the
    /// light along its length the way a real road does.
    /// </summary>
    private ArrayMesh? BuildRibbon(
        RoadPath path,
        int start,
        int end,
        float halfWidth,
        float shoulder,
        out List<Transform3D> sleeperFrames,
        out List<(Vector3 Deck, float Drop)> piers)
    {
        sleeperFrames = new List<Transform3D>();
        piers = new List<(Vector3, float)>();

        int count = end - start + 1;
        if (count < 2)
        {
            return null;
        }

        // -shoulder, -carriageway, crown, +carriageway, +shoulder
        float[] offsets = { -shoulder, -halfWidth, 0.0f, halfWidth, shoulder };
        float[] lift =
        {
            -0.06f,             // shoulder falls away into the verge
            SurfaceLift,
            SurfaceLift + 0.09f, // crown
            SurfaceLift,
            -0.06f,
        };

        var vertices = new List<Vector3>(count * offsets.Length);
        var normals = new List<Vector3>(vertices.Capacity);
        var uvs = new List<Vector2>(vertices.Capacity);
        var indices = new List<int>();

        float tileMeters = path.Class switch
        {
            CountyMap.RoadClass.Highway => 8.0f,
            CountyMap.RoadClass.Railway => 6.0f,
            _ => 6.5f,
        };

        float sinceSleeper = 0.0f;

        for (int i = 0; i < count; i++)
        {
            int p = start + i;
            Vector2 here = path.Points[p];
            Vector2 next = path.Points[Mathf.Min(p + 1, path.Points.Length - 1)];
            Vector2 previous = path.Points[Mathf.Max(p - 1, 0)];

            Vector2 tangent = (next - previous);
            if (tangent.LengthSquared() < 0.0001f)
            {
                tangent = Vector2.Right;
            }

            tangent = tangent.Normalized();
            var side = new Vector2(-tangent.Y, tangent.X);

            float centreGround = CountyMap.Height(here.X, here.Y);

            // A crossing is anywhere the road would otherwise be in the water.
            float water = CountyMap.WaterSurfaceY(here.X, here.Y);
            bool overWater = water > float.MinValue && centreGround < water + 0.6f &&
                             !OldMillBridgeReach.HasPoint(here);

            float deckY = centreGround;
            if (overWater)
            {
                // Carry the deck across at a constant height above the water rather
                // than dipping into it. Piers are dropped from here to the bed.
                deckY = water + 3.2f;
                piers.Add((new Vector3(here.X, deckY, here.Y), deckY - centreGround));
            }

            for (int o = 0; o < offsets.Length; o++)
            {
                Vector2 world = here + side * offsets[o];
                float ground = overWater ? deckY : CountyMap.Height(world.X, world.Y);

                // Blend the outer shoulder back toward real terrain so the road
                // does not end in a cliff edge against the verge.
                if (!overWater && (o == 0 || o == offsets.Length - 1))
                {
                    ground = Mathf.Lerp(ground, CountyMap.Height(world.X, world.Y), 1.0f);
                }

                vertices.Add(new Vector3(world.X, ground + lift[o], world.Y));
                normals.Add(overWater
                    ? Vector3.Up
                    : CountyMap.Normal(world.X, world.Y, 2.5f));

                float u = (offsets[o] / shoulder) * 0.5f + 0.5f;
                uvs.Add(new Vector2(u * (shoulder * 2.0f / tileMeters),
                    path.Along[p] / tileMeters));
            }

            if (path.Class == CountyMap.RoadClass.Railway)
            {
                float advance = i == 0 ? 0.0f : path.Along[p] - path.Along[p - 1];
                sinceSleeper += advance;
                if (i == 0 || sinceSleeper >= 0.65f)
                {
                    sinceSleeper = 0.0f;
                    var basis = new Basis(Vector3.Up, Mathf.Atan2(tangent.X, tangent.Y));
                    sleeperFrames.Add(new Transform3D(basis,
                        new Vector3(here.X, deckY + SurfaceLift + 0.10f, here.Y)));
                }
            }
        }

        int columns = offsets.Length;
        for (int i = 0; i < count - 1; i++)
        {
            for (int o = 0; o < columns - 1; o++)
            {
                int a = i * columns + o;
                int b = a + 1;
                int c = a + columns;
                int d = c + 1;

                // Wound to match the terrain mesher's convention, which Godot
                // treats as front-facing when viewed from above. The first version
                // had these reversed, so the whole network was back-face culled and
                // the only sign a road existed was the shadow it cast.
                indices.Add(a);
                indices.Add(c);
                indices.Add(b);

                indices.Add(b);
                indices.Add(c);
                indices.Add(d);
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
        arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
        arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        return mesh;
    }

    /// <summary>
    /// Sleepers and rails as MultiMesh batches.
    ///
    /// A kilometre of track is a few thousand sleepers; as nodes that would be
    /// thousands of draw calls for something the player mostly walks past.
    /// </summary>
    private void EmitRail(Node3D holder, string name, List<Transform3D> frames)
    {
        var sleeper = new BoxMesh { Size = new Vector3(2.5f, 0.16f, 0.26f) };
        var sleeperMulti = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = sleeper,
            InstanceCount = frames.Count,
        };

        for (int i = 0; i < frames.Count; i++)
        {
            sleeperMulti.SetInstanceTransform(i, frames[i]);
        }

        var sleeperInstance = new MultiMeshInstance3D
        {
            Name = $"{name}_sleepers",
            Multimesh = sleeperMulti,
            MaterialOverride = _materials.GetValueOrDefault(CountyMap.RoadClass.Dirt),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            VisibilityRangeEnd = 260.0f,
            VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
        };
        sleeperInstance.CustomAabb = FrameBounds(frames, 3.0f);
        holder.AddChild(sleeperInstance);

        // Two rails, offset to standard gauge either side of the centreline.
        var rail = new BoxMesh { Size = new Vector3(0.09f, 0.14f, 1.0f) };
        var railMulti = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = rail,
            InstanceCount = Mathf.Max(frames.Count - 1, 0) * 2,
        };

        int cursor = 0;
        for (int i = 0; i < frames.Count - 1; i++)
        {
            Vector3 a = frames[i].Origin;
            Vector3 b = frames[i + 1].Origin;
            float span = a.DistanceTo(b);
            if (span < 0.001f)
            {
                continue;
            }

            Basis basis = frames[i].Basis;
            Vector3 across = basis.X.Normalized();

            for (int rail_side = -1; rail_side <= 1; rail_side += 2)
            {
                Vector3 origin = (a + b) * 0.5f + across * (0.7175f * rail_side) + Vector3.Up * 0.14f;
                Basis scaled = basis.Scaled(new Vector3(1.0f, 1.0f, span * 1.05f));
                if (cursor < railMulti.InstanceCount)
                {
                    railMulti.SetInstanceTransform(cursor++, new Transform3D(scaled, origin));
                }
            }
        }

        railMulti.VisibleInstanceCount = cursor;

        var railInstance = new MultiMeshInstance3D
        {
            Name = $"{name}_rails",
            Multimesh = railMulti,
            MaterialOverride = _railMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            VisibilityRangeEnd = 300.0f,
            VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
        };
        railInstance.CustomAabb = FrameBounds(frames, 3.0f);
        holder.AddChild(railInstance);
    }

    /// <summary>
    /// World-space bounds of a set of instance frames.
    ///
    /// Instance transforms here are world-space while the holder sits at the
    /// origin, so without an explicit box Godot measures culling and visibility
    /// range from world zero - every batch then counts as adjacent to the camera
    /// and draws regardless of the range set on it.
    /// </summary>
    private static Aabb FrameBounds(List<Transform3D> frames, float padding)
    {
        if (frames.Count == 0)
        {
            return new Aabb();
        }

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (Transform3D frame in frames)
        {
            Vector3 o = frame.Origin;
            min.X = Mathf.Min(min.X, o.X - padding);
            min.Y = Mathf.Min(min.Y, o.Y - padding);
            min.Z = Mathf.Min(min.Z, o.Z - padding);
            max.X = Mathf.Max(max.X, o.X + padding);
            max.Y = Mathf.Max(max.Y, o.Y + padding);
            max.Z = Mathf.Max(max.Z, o.Z + padding);
        }

        return new Aabb(min, max - min);
    }

    /// <summary>Drops support piers from a bridge deck to the bed beneath it.</summary>
    private void EmitPiers(Node3D holder, string name, List<(Vector3 Deck, float Drop)> piers)
    {
        var pierMesh = new BoxMesh { Size = new Vector3(1.6f, 1.0f, 1.6f) };
        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = pierMesh,
            InstanceCount = piers.Count,
        };

        int cursor = 0;
        float sinceLast = float.MaxValue;
        Vector3 previous = Vector3.Zero;

        foreach ((Vector3 deck, float drop) in piers)
        {
            // One pier every dozen metres or so; a pier under every sample would be
            // a wall, not a bridge.
            sinceLast = cursor == 0 ? float.MaxValue : previous.DistanceTo(deck) + sinceLast;
            if (sinceLast < 14.0f)
            {
                continue;
            }

            sinceLast = 0.0f;
            previous = deck;

            float height = Mathf.Max(drop, 1.0f);
            var basis = Basis.Identity.Scaled(new Vector3(1.0f, height, 1.0f));
            multi.SetInstanceTransform(cursor++,
                new Transform3D(basis, deck - Vector3.Up * (height * 0.5f)));
        }

        if (cursor == 0)
        {
            return;
        }

        multi.VisibleInstanceCount = cursor;

        var pierFrames = new List<Transform3D>(cursor);
        for (int i = 0; i < cursor; i++)
        {
            pierFrames.Add(multi.GetInstanceTransform(i));
        }

        var pierInstance = new MultiMeshInstance3D
        {
            Name = $"{name}_piers",
            Multimesh = multi,
            MaterialOverride = _bridgeMaterial,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
            VisibilityRangeEnd = 400.0f,
            VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
        };

        // Piers are scaled tall, so the box has to allow for their full drop
        // rather than the mesh's unscaled height.
        pierInstance.CustomAabb = FrameBounds(pierFrames, 40.0f);
        holder.AddChild(pierInstance);
    }

    public void Rebuild()
    {
        foreach (Vector2I chunk in new List<Vector2I>(_chunks.Keys))
        {
            ReleaseChunk(chunk);
        }

        LoadMaterials();
        BuildPaths();
    }

    public int ResidentChunkCount => _chunks.Count;
}
