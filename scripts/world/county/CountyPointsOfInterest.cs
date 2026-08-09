#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// The evidence that Ashwood County was inhabited when the outbreak arrived, and
/// that people tried to do something about it.
///
/// Terrain, forest and roads make a landscape; they do not make a place anyone
/// lived. What turns a valley into a county someone fled is the wreckage of that
/// leaving - a roadblock the county sheriff threw across Highway 16, cars stalled
/// nose to tail where the evacuation jammed, a camp somebody abandoned in the
/// treeline. Every one of those is also a reason for the player to walk over and
/// look, which is what a point of interest is for.
///
/// Placement is deterministic and derived from <see cref="CountyMap"/>, so a POI
/// sits in the same spot every session and never lands in a river or on a cliff.
/// </summary>
[Tool]
public partial class CountyPointsOfInterest : Node3D, ICountyChunkSource
{
    /// <summary>
    /// Wreckage is small and read close up, but a roadblock silhouetted on a
    /// highway is legible a long way off and is one of the strongest signals that
    /// the road ahead is worth following.
    /// </summary>
    [Export] public int PoiRadius { get; set; } = 4;

    public int ChunkRadius => PoiRadius;

    private const string VehicleRoot = "res://assets/environment/vehicles/";
    private const string PropRoot = "res://assets/environment/props/";
    private const string RoadRoot = "res://assets/environment/roads/";

    private static readonly string[] Vehicles =
    {
        VehicleRoot + "parked_1963_c10.tscn",
        VehicleRoot + "parked_1975_impala.tscn",
        VehicleRoot + "parked_american_panel_van.tscn",
        VehicleRoot + "parked_rusted_alfa_visual.tscn",
    };

    private const string UtilityPole = RoadRoot + "utility_pole.tscn";
    private const string RoadSign = RoadRoot + "weathered_roadsign.tscn";
    private const string BarrelCrate = PropRoot + "barrel_crate.tscn";
    private const string Mailbox = PropRoot + "home_us_mailbox.tscn";
    private const string OldTyre = VehicleRoot + "old_tyre.tscn";

    /// <summary>
    /// A hand-placed set piece. These are the county's landmarks-of-catastrophe and
    /// are authored rather than scattered, because the whole point of a set piece
    /// is that it reads as deliberate.
    /// </summary>
    private readonly record struct SetPiece(
        string Name,
        Vector2 Position,
        SetPieceKind Kind,
        float Yaw);

    private enum SetPieceKind
    {
        /// <summary>Vehicles turned broadside across the carriageway, barrels, signage.</summary>
        Roadblock,

        /// <summary>A queue of stalled traffic - the evacuation that did not move.</summary>
        StalledConvoy,

        /// <summary>Somebody camped here, then did not come back for their things.</summary>
        AbandonedCamp,

        /// <summary>A single vehicle off the road, nose into the verge.</summary>
        CrashSite,
    }

    /// <summary>
    /// Positioned against the concept map. Highway 16 is the county's only way in
    /// or out, so it carries the heaviest evidence: the roadblock at the county
    /// line, and the jam behind it where the evacuation stopped moving.
    /// </summary>
    private static readonly SetPiece[] SetPieces =
    {
        new("Highway 16 west county line roadblock", new Vector2(-3760.0f, 830.0f),
            SetPieceKind.Roadblock, 1.51f),
        new("Highway 16 evacuation jam", new Vector2(-3450.0f, 806.0f),
            SetPieceKind.StalledConvoy, 1.51f),
        new("Highway 16 east approach roadblock", new Vector2(2540.0f, 300.0f),
            SetPieceKind.Roadblock, 1.72f),

        new("Old Mill Bridge checkpoint", new Vector2(-300.0f, -12.0f),
            SetPieceKind.Roadblock, 0.10f),
        new("Dam access crash", new Vector2(-70.0f, -1870.0f),
            SetPieceKind.CrashSite, 2.30f),

        new("Blackwater Lake shore camp", new Vector2(-540.0f, -2180.0f),
            SetPieceKind.AbandonedCamp, 0.62f),
        new("Logging camp muster point", new Vector2(-1860.0f, -2390.0f),
            SetPieceKind.AbandonedCamp, 1.95f),
        new("Fire Lookout supply drop", new Vector2(1010.0f, -3010.0f),
            SetPieceKind.AbandonedCamp, 0.35f),
        new("Pine Ridge road crash", new Vector2(-190.0f, -3300.0f),
            SetPieceKind.CrashSite, 1.10f),

        new("Mill Creek south roadblock", new Vector2(-2040.0f, 1560.0f),
            SetPieceKind.Roadblock, 0.85f),
        new("Fairgrounds staging area", new Vector2(-250.0f, 2380.0f),
            SetPieceKind.StalledConvoy, 2.05f),
        new("Trailer Park evacuation", new Vector2(1460.0f, 1540.0f),
            SetPieceKind.StalledConvoy, 0.48f),
        new("Farm District crash", new Vector2(-2150.0f, -430.0f),
            SetPieceKind.CrashSite, 2.70f),
        new("Railway crossing pile-up", new Vector2(690.0f, 1180.0f),
            SetPieceKind.StalledConvoy, 1.28f),
        new("Southern farmland camp", new Vector2(-820.0f, 3050.0f),
            SetPieceKind.AbandonedCamp, 1.40f),
        new("Service station forecourt", new Vector2(430.0f, 250.0f),
            SetPieceKind.StalledConvoy, 0.20f),
    };

    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private readonly Dictionary<string, PackedScene> _sceneCache = new();

    public override void _Ready()
    {
        Settings.GraphicsPreset preset =
            Settings.SettingsManager.Instance?.Current.GraphicsPreset
            ?? Settings.GraphicsPreset.Low;
        PoiRadius = Mathf.Min(
            PoiRadius, Settings.GraphicsQuality.PoiRadius(preset));
        foreach (string scenePath in Vehicles)
        {
            LoadScene(scenePath);
        }
        foreach (string scenePath in new[]
                 {
                     UtilityPole, RoadSign, BarrelCrate, Mailbox, OldTyre,
                 })
        {
            LoadScene(scenePath);
        }
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

        var holder = new Node3D { Name = $"Poi_{chunk.X}_{chunk.Y}" };
        AddChild(holder);
        _chunks[chunk] = holder;

        Vector2 origin = CountyChunks.Origin(chunk);
        var bounds = new Rect2(origin, new Vector2(CountyChunks.Size, CountyChunks.Size));

        foreach (SetPiece piece in SetPieces)
        {
            if (bounds.HasPoint(piece.Position))
            {
                BuildSetPiece(holder, piece);
            }
        }

        // Poles and roadside clutter are procedural rather than authored: they run
        // the whole length of every route, and hand-placing thousands of them would
        // be all cost and no additional intent.
        BuildRoadFurniture(holder, chunk, bounds);
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
    }

    private void BuildSetPiece(Node3D holder, in SetPiece piece)
    {
        var rng = new RandomNumberGenerator
        {
            Seed = (ulong)piece.Name.GetHashCode(),
        };

        switch (piece.Kind)
        {
            case SetPieceKind.Roadblock:
                BuildRoadblock(holder, piece, rng);
                break;
            case SetPieceKind.StalledConvoy:
                BuildStalledConvoy(holder, piece, rng);
                break;
            case SetPieceKind.AbandonedCamp:
                BuildAbandonedCamp(holder, piece, rng);
                break;
            case SetPieceKind.CrashSite:
                BuildCrashSite(holder, piece, rng);
                break;
        }
    }

    /// <summary>
    /// Vehicles turned broadside to block the carriageway, with barrels and a sign.
    /// Turning them across the road rather than parking them along it is the whole
    /// read: a car facing the wrong way is instantly legible as a barricade.
    /// </summary>
    private void BuildRoadblock(Node3D holder, in SetPiece piece, RandomNumberGenerator rng)
    {
        Vector2 across = new Vector2(Mathf.Cos(piece.Yaw), Mathf.Sin(piece.Yaw));
        Vector2 along = new Vector2(-across.Y, across.X);

        for (int i = 0; i < 3; i++)
        {
            Vector2 spot = piece.Position
                + across * ((i - 1) * rng.RandfRange(4.2f, 5.4f))
                + along * rng.RandfRange(-1.4f, 1.4f);

            // Broadside, with a few degrees of slop so it does not read as parked.
            float yaw = piece.Yaw + Mathf.Pi * 0.5f + rng.RandfRange(-0.24f, 0.24f);
            Place(holder, Vehicles[rng.RandiRange(0, Vehicles.Length - 1)], spot, yaw);
        }

        for (int i = 0; i < 5; i++)
        {
            Vector2 spot = piece.Position
                + across * rng.RandfRange(-8.0f, 8.0f)
                + along * rng.RandfRange(3.0f, 7.0f);
            Place(holder, BarrelCrate, spot, rng.RandfRange(0.0f, Mathf.Tau));
        }

        Place(holder, RoadSign,
            piece.Position + along * 8.5f + across * 5.0f,
            piece.Yaw + Mathf.Pi * 0.5f);
    }

    /// <summary>A queue of vehicles nose to tail, as the evacuation left it.</summary>
    private void BuildStalledConvoy(Node3D holder, in SetPiece piece, RandomNumberGenerator rng)
    {
        Vector2 along = new Vector2(Mathf.Cos(piece.Yaw), Mathf.Sin(piece.Yaw));
        Vector2 across = new Vector2(-along.Y, along.X);

        int count = rng.RandiRange(5, 8);
        float travelled = 0.0f;
        for (int i = 0; i < count; i++)
        {
            travelled += rng.RandfRange(6.5f, 11.0f);

            // Alternating lanes, with the odd one slewed as it tried to pull out.
            float lane = (i % 2 == 0 ? -1.0f : 1.0f) * rng.RandfRange(1.6f, 2.8f);
            Vector2 spot = piece.Position + along * travelled + across * lane;
            float yaw = piece.Yaw + rng.RandfRange(-0.13f, 0.13f);
            if (rng.Randf() < 0.22f)
            {
                yaw += rng.RandfRange(-0.7f, 0.7f);
            }

            Place(holder, Vehicles[rng.RandiRange(0, Vehicles.Length - 1)], spot, yaw);
        }

        Place(holder, OldTyre, piece.Position + across * 5.0f, rng.RandfRange(0.0f, Mathf.Tau));
    }

    /// <summary>Barrels and crates around a clearing somebody stopped in.</summary>
    private void BuildAbandonedCamp(Node3D holder, in SetPiece piece, RandomNumberGenerator rng)
    {
        int count = rng.RandiRange(5, 8);
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count + rng.RandfRange(-0.3f, 0.3f);
            float radius = rng.RandfRange(2.4f, 5.2f);
            Vector2 spot = piece.Position +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Place(holder, BarrelCrate, spot, rng.RandfRange(0.0f, Mathf.Tau));
        }

        // One vehicle they arrived in and did not leave in.
        Place(holder, Vehicles[rng.RandiRange(0, Vehicles.Length - 1)],
            piece.Position + new Vector2(Mathf.Cos(piece.Yaw), Mathf.Sin(piece.Yaw)) * 8.0f,
            piece.Yaw + rng.RandfRange(-0.4f, 0.4f));
    }

    private void BuildCrashSite(Node3D holder, in SetPiece piece, RandomNumberGenerator rng)
    {
        // Well off the carriageway and turned hard, which is what separates a crash
        // from a parked car.
        Place(holder, Vehicles[rng.RandiRange(0, Vehicles.Length - 1)],
            piece.Position, piece.Yaw + rng.RandfRange(0.9f, 1.7f));

        for (int i = 0; i < 3; i++)
        {
            Vector2 spot = piece.Position + new Vector2(
                rng.RandfRange(-4.5f, 4.5f), rng.RandfRange(-4.5f, 4.5f));
            Place(holder, i == 0 ? OldTyre : BarrelCrate, spot, rng.RandfRange(0.0f, Mathf.Tau));
        }
    }

    /// <summary>
    /// Utility poles down every road, plus the occasional mailbox and abandoned
    /// vehicle on the verge.
    ///
    /// Poles do more for "someone lived here" per triangle than almost anything
    /// else: they give the roads a rhythm and a vertical scale reference, and they
    /// are visible from far enough away to draw the eye along a route.
    /// </summary>
    private void BuildRoadFurniture(Node3D holder, Vector2I chunk, Rect2 bounds)
    {
        var rng = new RandomNumberGenerator
        {
            Seed = (ulong)(chunk.X * 6971 ^ chunk.Y * 40639 ^ 0x5EED),
        };

        Vector2 centre = bounds.Position + (bounds.Size * 0.5f);
        float chunkReach = CountyChunks.Size * 0.75f;

        for (int r = 0; r < CountyMap.Roads.Length; r++)
        {
            CountyMap.Road road = CountyMap.Roads[r];

            // Railways carry no poles or mailboxes, and dirt tracks were never
            // wired in the first place.
            if (road.Class == CountyMap.RoadClass.Railway ||
                road.Class == CountyMap.RoadClass.Dirt)
            {
                continue;
            }

            CountyMap.Polyline line = CountyMap.RoadLines[r];
            if (line.IsFarFrom(centre, chunkReach))
            {
                continue;
            }

            float spacing = road.Class == CountyMap.RoadClass.Highway ? 62.0f : 88.0f;
            float shoulder = CountyMap.RoadShoulder(road.Class);

            // Walk the route in world space and keep whatever lands in this chunk,
            // so poles stay evenly spaced across chunk borders instead of restarting
            // their rhythm at every seam.
            int steps = Mathf.CeilToInt(line.TotalLength / spacing);
            for (int step = 0; step <= steps; step++)
            {
                float along = (step * spacing) / Mathf.Max(line.TotalLength, 0.001f);
                if (along > 1.0f)
                {
                    break;
                }

                Vector2 point = line.PointAt(along);
                if (!bounds.HasPoint(point))
                {
                    continue;
                }

                Vector2 direction = line.DirectionNear(point);
                var side = new Vector2(-direction.Y, direction.X);

                // Alternate which verge, as real lines do where they cross a road.
                float offset = (step % 7 == 0 ? -1.0f : 1.0f) * (shoulder + 2.6f);
                Vector2 spot = point + side * offset;

                if (!CountyMap.IsPlayable(spot.X, spot.Y))
                {
                    continue;
                }

                // A pole standing in the river is worse than a gap in the line.
                float water = CountyMap.WaterSurfaceY(spot.X, spot.Y);
                if (water > float.MinValue && CountyMap.Height(spot.X, spot.Y) < water + 0.4f)
                {
                    continue;
                }

                float yaw = Mathf.Atan2(direction.X, direction.Y);
                Place(holder, UtilityPole, spot, yaw);

                if (rng.Randf() < 0.14f)
                {
                    Place(holder, Mailbox, point + side * -offset, yaw + Mathf.Pi * 0.5f);
                }

                // The occasional vehicle simply left on the verge, away from the
                // authored set pieces, so the roads never feel swept clean.
                if (rng.Randf() < 0.08f)
                {
                    Vector2 wreck = point + side * (offset * 0.55f) +
                        direction * rng.RandfRange(6.0f, 18.0f);
                    if (CountyMap.IsPlayable(wreck.X, wreck.Y))
                    {
                        Place(holder, Vehicles[rng.RandiRange(0, Vehicles.Length - 1)],
                            wreck, yaw + rng.RandfRange(-0.5f, 0.5f));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Drops one instance on the ground at a world XZ, aligned to the surface.
    /// </summary>
    private void Place(Node3D holder, string scenePath, Vector2 point, float yaw)
    {
        PackedScene? scene = LoadScene(scenePath);
        if (scene == null)
        {
            return;
        }

        var instance = scene.Instantiate<Node3D>();
        holder.AddChild(instance);

        float height = CountyMap.Height(point.X, point.Y);
        Vector3 normal = CountyMap.Normal(point.X, point.Y, 1.5f);

        var basis = new Basis(Vector3.Up, yaw);

        // Wheeled things sit on the ground plane, so they lean with it. Leaning
        // only part of the way keeps a car on a camber from looking tipped over.
        Vector3 up = Vector3.Up.Lerp(normal, 0.6f).Normalized();
        Vector3 axis = Vector3.Up.Cross(up);
        if (axis.LengthSquared() > 0.000001f)
        {
            basis = new Basis(axis.Normalized(), Vector3.Up.AngleTo(up)) * basis;
        }

        instance.Transform = new Transform3D(basis, new Vector3(point.X, height, point.Y));
    }

    private PackedScene? LoadScene(string path)
    {
        if (_sceneCache.TryGetValue(path, out PackedScene? cached))
        {
            return cached;
        }

        PackedScene? loaded = ResourceLoader.Exists(path)
            ? ResourceLoader.Load<PackedScene>(path)
            : null;

        if (loaded == null)
        {
            GD.PushWarning($"CountyPointsOfInterest: missing scene {path}");
        }

        _sceneCache[path] = loaded!;
        return loaded;
    }
}
