#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Builds the county's inhabited places: Mill Creek, the Trailer Park, the Logging
/// Camp, Pine Ridge, the Fairgrounds, the farms and the Fire Lookout.
///
/// The terrain under every named site is already graded flat by
/// <see cref="CountyMap"/>, so this places structures on prepared ground and
/// concerns itself with what actually makes a building read as a building rather
/// than as a box: a pitched roof with overhanging eaves, a recessed door and
/// windows, and a plot that fronts onto the road serving the site.
///
/// Everything is deterministic - the RNG is seeded from the site position - so a
/// settlement streams out and back in exactly as it was.
/// </summary>
[Tool]
public partial class CountySettlements : Node3D, ICountyChunkSource
{
    /// <summary>
    /// Settlements are landmarks and need to be visible from the approach roads,
    /// so they reach further than vegetation.
    /// </summary>
    [Export] public int SettlementRadius { get; set; } = 5;

    public int ChunkRadius => SettlementRadius;

    private const string Palette = "res://assets/materials/county/";

    /// <summary>
    /// Nothing is built inside this radius of the origin. The hand-authored
    /// Ashwood Main Street slice lives there and must not be duplicated.
    /// </summary>
    private const float TownExclusion = 420.0f;

    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private readonly Dictionary<string, Material> _materials = new();

    /// <summary>
    /// Every structure in the county, resolved once and bucketed by chunk. Sites
    /// are small and few, so deciding what exists is cheap to do up front; only
    /// the geometry is deferred to streaming.
    /// </summary>
    private readonly Dictionary<Vector2I, List<Structure>> _byChunk = new();

    private enum Shape
    {
        /// <summary>Pitched-roof building: house, cabin, farmhouse.</summary>
        House,
        /// <summary>Wide low shed with a shallow roof: barn, sawmill, equipment shed.</summary>
        Shed,
        /// <summary>Box with a shallow roof and a long side: trailer, mobile home.</summary>
        Trailer,
        /// <summary>Cylinder: silo, water tank, propane.</summary>
        Silo,
        /// <summary>Lattice tower with a glazed cab. The Fire Lookout.</summary>
        Tower,
        /// <summary>Stacked timber.</summary>
        LogStack,
    }

    private readonly record struct Structure(
        Shape Shape,
        Vector3 Position,
        float Yaw,
        Vector3 Size,
        string WallMaterial,
        string RoofMaterial);

    public override void _Ready()
    {
        LoadPalette();
        PlanCounty();

        if (GetParent() is CountyWorld world)
        {
            world.RegisterSource(this);
        }
    }

    private void LoadPalette()
    {
        string[] wanted =
        {
            "county_siding_white", "county_siding_cream", "county_siding_green",
            "county_siding_blue", "county_siding_red", "county_siding_grey",
            "county_siding_yellow", "county_timber_grey", "county_timber_raw",
            "county_log_wall", "county_brick_red", "county_brick_pale",
            "county_corrugated", "county_galvanised", "county_rusted_steel",
            "county_roof_shingle", "county_roof_tin", "county_roof_rust",
            "county_concrete", "county_concrete_pad", "county_glass_dark",
            "county_trim_white", "county_paint_faded_red", "county_paint_faded_white",
        };

        foreach (string name in wanted)
        {
            string path = Palette + name + ".tres";
            if (ResourceLoader.Exists(path) && ResourceLoader.Load(path) is Material material)
            {
                _materials[name] = material;
            }
        }

        if (_materials.Count == 0)
        {
            GD.PushWarning("CountySettlements: county material palette not found.");
        }
    }

    private Material MaterialFor(string name)
    {
        if (_materials.TryGetValue(name, out Material? material))
        {
            return material;
        }

        return new StandardMaterial3D { AlbedoColor = new Color(0.6f, 0.58f, 0.55f), Roughness = 0.9f };
    }

    // ------------------------------------------------------------------ planning

    private void PlanCounty()
    {
        _byChunk.Clear();

        foreach (CountyMap.Poi place in CountyMap.Places)
        {
            if (place.Position.Length() < TownExclusion)
            {
                continue;
            }

            var rng = new RandomNumberGenerator
            {
                Seed = (ulong)(Mathf.RoundToInt(place.Position.X) * 73856093 ^
                               Mathf.RoundToInt(place.Position.Y) * 19349663),
            };

            switch (place.Kind)
            {
                case CountyMap.PoiKind.Settlement when place.Name == "Trailer Park":
                    PlanTrailerPark(place, rng);
                    break;
                case CountyMap.PoiKind.Settlement when place.Name == "County Fairgrounds":
                    PlanFairgrounds(place, rng);
                    break;
                case CountyMap.PoiKind.Settlement:
                    PlanVillage(place, rng);
                    break;
                case CountyMap.PoiKind.Industrial:
                    PlanLoggingCamp(place, rng);
                    break;
                case CountyMap.PoiKind.Farm:
                    PlanFarms(place, rng);
                    break;
                case CountyMap.PoiKind.Landmark when place.Name == "Fire Lookout":
                    PlanFireLookout(place);
                    break;
                case CountyMap.PoiKind.Infrastructure when place.Name != "Blackwater Dam":
                    PlanInfrastructure(place, rng);
                    break;
            }
        }
    }

    private void Add(Structure structure)
    {
        Vector2I chunk = CountyChunks.ToChunk(structure.Position.X, structure.Position.Z);
        if (!_byChunk.TryGetValue(chunk, out List<Structure>? list))
        {
            list = new List<Structure>();
            _byChunk[chunk] = list;
        }

        list.Add(structure);
    }

    /// <summary>
    /// Bearing of the road serving a point, so buildings can be squared to it.
    /// Buildings that ignore the road they sit on are the loudest tell that a
    /// settlement was scattered rather than built.
    /// </summary>
    private static float RoadYaw(Vector2 position, out float distanceToRoad)
    {
        float best = float.MaxValue;
        Vector2 direction = Vector2.Right;

        for (int i = 0; i < CountyMap.Roads.Length; i++)
        {
            if (CountyMap.RoadLines[i].IsFarFrom(position, 320.0f))
            {
                continue;
            }

            float distance = CountyMap.RoadLines[i].Distance(position);
            if (distance < best)
            {
                best = distance;
                direction = CountyMap.RoadLines[i].DirectionNear(position);
            }
        }

        distanceToRoad = best;
        return Mathf.Atan2(direction.X, direction.Y);
    }

    /// <summary>Ground position, or null when the spot is unusable.</summary>
    private static Vector3? Ground(Vector2 position, float maxSlope = 0.30f)
    {
        if (!CountyMap.IsPlayable(position.X, position.Y))
        {
            return null;
        }

        float height = CountyMap.Height(position.X, position.Y);
        float water = CountyMap.WaterSurfaceY(position.X, position.Y);
        if (water > float.MinValue && height < water + 1.0f)
        {
            return null;
        }

        if (CountyMap.Slope(position.X, position.Y, 4.0f) > maxSlope)
        {
            return null;
        }

        return new Vector3(position.X, height, position.Y);
    }

    private static readonly string[] SidingColours =
    {
        "county_siding_white", "county_siding_cream", "county_siding_green",
        "county_siding_blue", "county_siding_red", "county_siding_grey",
        "county_siding_yellow",
    };

    private static readonly string[] RoofColours =
    {
        "county_roof_shingle", "county_roof_tin", "county_roof_rust",
    };

    private void PlanVillage(CountyMap.Poi place, RandomNumberGenerator rng)
    {
        // Plots are laid along the road serving the site rather than on a grid, so
        // the settlement grows the way a real one did.
        float yaw = RoadYaw(place.Position, out _);
        var along = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
        var across = new Vector2(along.Y, -along.X);

        int plots = place.Name == "Mill Creek" ? 34 : 18;
        float spacing = 26.0f;

        for (int i = 0; i < plots; i++)
        {
            int side = i % 2 == 0 ? 1 : -1;
            float step = (i / 2 - plots / 4.0f) * spacing + rng.RandfRange(-4.0f, 4.0f);
            float setback = rng.RandfRange(16.0f, 26.0f) * side;

            Vector2 spot = place.Position + along * step + across * setback;
            Vector3? ground = Ground(spot);
            if (ground == null)
            {
                continue;
            }

            // Face the road: rotate to the road bearing, then turn to look across it.
            float facing = yaw + (side > 0 ? Mathf.Pi * 0.5f : -Mathf.Pi * 0.5f);

            Add(new Structure(
                Shape.House,
                ground.Value,
                facing + rng.RandfRange(-0.05f, 0.05f),
                new Vector3(rng.RandfRange(7.0f, 10.5f), rng.RandfRange(3.2f, 4.4f),
                    rng.RandfRange(8.0f, 13.0f)),
                SidingColours[rng.RandiRange(0, SidingColours.Length - 1)],
                RoofColours[rng.RandiRange(0, RoofColours.Length - 1)]));

            // A shed or garage out the back on about half the plots.
            if (rng.Randf() < 0.45f)
            {
                Vector2 shedSpot = spot + across * (side * rng.RandfRange(11.0f, 15.0f));
                Vector3? shedGround = Ground(shedSpot);
                if (shedGround != null)
                {
                    Add(new Structure(
                        Shape.Shed, shedGround.Value, facing,
                        new Vector3(rng.RandfRange(4.0f, 6.0f), 2.6f, rng.RandfRange(4.0f, 7.0f)),
                        "county_timber_grey", "county_roof_tin"));
                }
            }
        }
    }

    private void PlanTrailerPark(CountyMap.Poi place, RandomNumberGenerator rng)
    {
        float yaw = RoadYaw(place.Position, out _);
        var along = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
        var across = new Vector2(along.Y, -along.X);

        // Trailers sit in tight regular rows - that regularity is the whole visual
        // signature of a trailer park, so unlike the village it is not broken up.
        for (int row = 0; row < 5; row++)
        {
            for (int slot = 0; slot < 7; slot++)
            {
                Vector2 spot = place.Position
                    + along * ((slot - 3) * 15.0f)
                    + across * ((row - 2) * 22.0f);

                Vector3? ground = Ground(spot, 0.22f);
                if (ground == null)
                {
                    continue;
                }

                Add(new Structure(
                    Shape.Trailer, ground.Value, yaw + rng.RandfRange(-0.03f, 0.03f),
                    new Vector3(3.4f, 2.7f, rng.RandfRange(9.0f, 12.5f)),
                    rng.Randf() < 0.4f ? "county_galvanised" : "county_siding_cream",
                    "county_roof_tin"));

                if (rng.Randf() < 0.3f)
                {
                    Add(new Structure(
                        Shape.Silo, ground.Value + new Vector3(2.6f, 0.0f, 0.0f), 0.0f,
                        new Vector3(0.5f, 1.3f, 0.5f),
                        "county_rusted_steel", "county_rusted_steel"));
                }
            }
        }
    }

    private void PlanLoggingCamp(CountyMap.Poi place, RandomNumberGenerator rng)
    {
        float yaw = RoadYaw(place.Position, out _);

        // The sawmill shed is the anchor; everything else is yard around it.
        Vector3? mill = Ground(place.Position);
        if (mill != null)
        {
            Add(new Structure(Shape.Shed, mill.Value, yaw,
                new Vector3(16.0f, 7.0f, 28.0f), "county_corrugated", "county_roof_rust"));
        }

        for (int i = 0; i < 22; i++)
        {
            float angle = rng.RandfRange(0.0f, Mathf.Tau);
            float radius = rng.RandfRange(40.0f, place.Radius * 0.75f);
            Vector2 spot = place.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            Vector3? ground = Ground(spot, 0.34f);
            if (ground == null)
            {
                continue;
            }

            if (rng.Randf() < 0.62f)
            {
                Add(new Structure(Shape.LogStack, ground.Value, rng.RandfRange(0.0f, Mathf.Tau),
                    new Vector3(rng.RandfRange(3.0f, 5.0f), rng.RandfRange(1.6f, 2.8f),
                        rng.RandfRange(7.0f, 12.0f)),
                    "county_timber_raw", "county_timber_raw"));
            }
            else
            {
                Add(new Structure(Shape.Shed, ground.Value, rng.RandfRange(0.0f, Mathf.Tau),
                    new Vector3(rng.RandfRange(4.0f, 8.0f), 3.0f, rng.RandfRange(5.0f, 9.0f)),
                    "county_corrugated", "county_roof_rust"));
            }
        }
    }

    private void PlanFarms(CountyMap.Poi place, RandomNumberGenerator rng)
    {
        // Farmsteads sit at the corners of the worked fields, not in the middle of
        // them - a farmhouse standing in a crop is an instant tell.
        int steads = place.Name == "South Farmland" ? 7 : 5;

        for (int i = 0; i < steads; i++)
        {
            float angle = rng.RandfRange(0.0f, Mathf.Tau);
            float radius = rng.RandfRange(place.Radius * 0.35f, place.Radius * 0.92f);
            Vector2 centre = place.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            // Nudge off any worked parcel onto its hedgerow margin.
            for (int attempt = 0; attempt < 6 && CountyMap.FieldStrength(centre.X, centre.Y) > 0.35f; attempt++)
            {
                centre += new Vector2(rng.RandfRange(-40.0f, 40.0f), rng.RandfRange(-40.0f, 40.0f));
            }

            Vector3? ground = Ground(centre);
            if (ground == null)
            {
                continue;
            }

            float yaw = RoadYaw(centre, out _);

            Add(new Structure(Shape.House, ground.Value, yaw,
                new Vector3(9.0f, 4.2f, 12.0f),
                SidingColours[rng.RandiRange(0, SidingColours.Length - 1)],
                "county_roof_shingle"));

            Vector2 barnSpot = centre + new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw)) * 26.0f;
            Vector3? barnGround = Ground(barnSpot);
            if (barnGround != null)
            {
                Add(new Structure(Shape.Shed, barnGround.Value, yaw,
                    new Vector3(13.0f, 6.5f, 20.0f), "county_paint_faded_red", "county_roof_rust"));
            }

            if (rng.Randf() < 0.7f)
            {
                Vector2 siloSpot = centre + new Vector2(Mathf.Cos(yaw), -Mathf.Sin(yaw)) * 20.0f;
                Vector3? siloGround = Ground(siloSpot);
                if (siloGround != null)
                {
                    Add(new Structure(Shape.Silo, siloGround.Value, 0.0f,
                        new Vector3(2.6f, 9.0f, 2.6f), "county_galvanised", "county_galvanised"));
                }
            }
        }
    }

    private void PlanFairgrounds(CountyMap.Poi place, RandomNumberGenerator rng)
    {
        float yaw = RoadYaw(place.Position, out _);

        Vector3? grandstand = Ground(place.Position);
        if (grandstand != null)
        {
            Add(new Structure(Shape.Shed, grandstand.Value, yaw,
                new Vector3(11.0f, 8.0f, 30.0f), "county_timber_grey", "county_roof_tin"));
        }

        // Barns and stalls ringing the show ground.
        for (int i = 0; i < 12; i++)
        {
            float angle = i / 12.0f * Mathf.Tau;
            Vector2 spot = place.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 95.0f;
            Vector3? ground = Ground(spot);
            if (ground == null)
            {
                continue;
            }

            Add(new Structure(Shape.Shed, ground.Value, angle,
                new Vector3(rng.RandfRange(5.0f, 9.0f), 3.4f, rng.RandfRange(8.0f, 14.0f)),
                "county_paint_faded_white", "county_roof_tin"));
        }
    }

    private void PlanInfrastructure(CountyMap.Poi place, RandomNumberGenerator rng)
    {
        Vector3? ground = Ground(place.Position);
        if (ground == null)
        {
            return;
        }

        float yaw = RoadYaw(place.Position, out _);

        Vector3 size = place.Name switch
        {
            "Hospital" => new Vector3(26.0f, 9.0f, 40.0f),
            "Service Station" => new Vector3(10.0f, 4.0f, 14.0f),
            "Railway Crossing" => new Vector3(4.0f, 3.0f, 5.0f),
            _ => new Vector3(12.0f, 5.0f, 18.0f),
        };

        Add(new Structure(
            place.Name == "Hospital" ? Shape.Shed : Shape.House,
            ground.Value, yaw, size,
            place.Name == "Hospital" ? "county_brick_pale" : "county_siding_white",
            "county_roof_tin"));
    }

    /// <summary>
    /// The Fire Lookout: a lattice tower on the county's highest point. It is meant
    /// to be recognisable as a silhouette from kilometres away, so it is tall and
    /// deliberately skinny rather than a shed on stilts.
    /// </summary>
    private void PlanFireLookout(CountyMap.Poi place)
    {
        float height = CountyMap.Height(place.Position.X, place.Position.Y);
        Add(new Structure(
            Shape.Tower,
            new Vector3(place.Position.X, height, place.Position.Y),
            0.35f,
            new Vector3(6.0f, 22.0f, 6.0f),
            "county_timber_grey",
            "county_roof_tin"));
    }

    // ----------------------------------------------------------------- streaming

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk))
        {
            return;
        }

        var holder = new Node3D { Name = $"Site_{chunk.X}_{chunk.Y}" };
        AddChild(holder);
        _chunks[chunk] = holder;

        if (!_byChunk.TryGetValue(chunk, out List<Structure>? structures))
        {
            return;
        }

        // Collision only where the player can reach; a settlement three rings out
        // is scenery.
        bool wantsCollision = ring <= 2;

        foreach (Structure structure in structures)
        {
            Node3D? built = Build(structure, wantsCollision);
            if (built != null)
            {
                holder.AddChild(built);
            }
        }
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        if (_chunks.Remove(chunk, out Node3D? holder))
        {
            holder.QueueFree();
        }
    }

    private Node3D? Build(Structure structure, bool wantsCollision)
    {
        var root = new Node3D
        {
            Name = structure.Shape.ToString(),
            Position = structure.Position,
            Rotation = new Vector3(0.0f, structure.Yaw, 0.0f),
        };

        switch (structure.Shape)
        {
            case Shape.House:
                BuildPitched(root, structure, roofPitch: 0.62f, eaves: 0.55f);
                break;
            case Shape.Shed:
                BuildPitched(root, structure, roofPitch: 0.28f, eaves: 0.4f);
                break;
            case Shape.Trailer:
                BuildPitched(root, structure, roofPitch: 0.12f, eaves: 0.25f);
                break;
            case Shape.Silo:
                BuildSilo(root, structure);
                break;
            case Shape.LogStack:
                BuildLogStack(root, structure);
                break;
            case Shape.Tower:
                BuildTower(root, structure);
                break;
        }

        if (wantsCollision)
        {
            var body = new StaticBody3D { Name = "Body" };
            body.AddChild(new CollisionShape3D
            {
                Name = "Shape",
                Shape = new BoxShape3D { Size = structure.Size },
                Position = new Vector3(0.0f, structure.Size.Y * 0.5f, 0.0f),
            });
            root.AddChild(body);
        }

        return root;
    }

    /// <summary>
    /// A walled box with a pitched roof and overhanging eaves.
    ///
    /// The eaves are the detail that does the work: a roof flush with the walls
    /// reads as a box with a triangle on top, while even half a metre of overhang
    /// casts the shadow line that makes it read as a building.
    /// </summary>
    private void BuildPitched(Node3D root, in Structure structure, float roofPitch, float eaves)
    {
        Vector3 size = structure.Size;

        root.AddChild(new MeshInstance3D
        {
            Name = "Walls",
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = MaterialFor(structure.WallMaterial),
            Position = new Vector3(0.0f, size.Y * 0.5f, 0.0f),
        });

        // Roof as two slabs leaning against each other along the long axis.
        float ridge = size.Z * 0.5f * roofPitch + 0.6f;
        float slope = Mathf.Atan2(ridge, size.Z * 0.5f);
        float slabLength = Mathf.Sqrt(ridge * ridge + size.Z * size.Z * 0.25f) + eaves;
        Material roof = MaterialFor(structure.RoofMaterial);

        for (int side = -1; side <= 1; side += 2)
        {
            root.AddChild(new MeshInstance3D
            {
                Name = side < 0 ? "RoofWest" : "RoofEast",
                Mesh = new BoxMesh { Size = new Vector3(size.X + eaves * 2.0f, 0.18f, slabLength) },
                MaterialOverride = roof,
                Position = new Vector3(
                    0.0f,
                    size.Y + ridge * 0.5f,
                    side * (size.Z * 0.25f + eaves * 0.25f)),
                Rotation = new Vector3(side * slope, 0.0f, 0.0f),
            });
        }

        // Gable infill so the roof does not float above open triangles.
        for (int side = -1; side <= 1; side += 2)
        {
            root.AddChild(new MeshInstance3D
            {
                Name = side < 0 ? "GableSouth" : "GableNorth",
                Mesh = new BoxMesh { Size = new Vector3(size.X, ridge, 0.2f) },
                MaterialOverride = MaterialFor(structure.WallMaterial),
                Position = new Vector3(0.0f, size.Y + ridge * 0.5f, side * size.Z * 0.5f),
            });
        }

        // Recessed openings. Flat painted-on windows look wrong in raking light, so
        // these are inset panels that catch a real shadow at their reveal.
        Material glass = MaterialFor("county_glass_dark");
        int windows = Mathf.Max(Mathf.FloorToInt(size.Z / 3.2f), 1);
        for (int i = 0; i < windows; i++)
        {
            float z = (i + 0.5f) / windows * size.Z - size.Z * 0.5f;
            for (int side = -1; side <= 1; side += 2)
            {
                root.AddChild(new MeshInstance3D
                {
                    Name = $"Window{i}{(side < 0 ? "W" : "E")}",
                    Mesh = new BoxMesh { Size = new Vector3(0.12f, 1.15f, 0.95f) },
                    MaterialOverride = glass,
                    Position = new Vector3(side * (size.X * 0.5f - 0.06f), size.Y * 0.58f, z),
                });
            }
        }

        root.AddChild(new MeshInstance3D
        {
            Name = "Door",
            Mesh = new BoxMesh { Size = new Vector3(1.0f, 2.05f, 0.14f) },
            MaterialOverride = MaterialFor("county_trim_white"),
            Position = new Vector3(0.0f, 1.02f, -size.Z * 0.5f + 0.05f),
        });
    }

    private void BuildSilo(Node3D root, in Structure structure)
    {
        root.AddChild(new MeshInstance3D
        {
            Name = "Shell",
            Mesh = new CylinderMesh
            {
                TopRadius = structure.Size.X,
                BottomRadius = structure.Size.X,
                Height = structure.Size.Y,
                RadialSegments = 12,
                Rings = 1,
            },
            MaterialOverride = MaterialFor(structure.WallMaterial),
            Position = new Vector3(0.0f, structure.Size.Y * 0.5f, 0.0f),
        });

        root.AddChild(new MeshInstance3D
        {
            Name = "Cap",
            Mesh = new CylinderMesh
            {
                TopRadius = 0.05f,
                BottomRadius = structure.Size.X * 1.05f,
                Height = structure.Size.X * 0.8f,
                RadialSegments = 12,
                Rings = 1,
            },
            MaterialOverride = MaterialFor(structure.RoofMaterial),
            Position = new Vector3(0.0f, structure.Size.Y + structure.Size.X * 0.4f, 0.0f),
        });
    }

    private void BuildLogStack(Node3D root, in Structure structure)
    {
        Material timber = MaterialFor(structure.WallMaterial);
        int layers = Mathf.Max(Mathf.FloorToInt(structure.Size.Y / 0.55f), 2);
        var rng = new RandomNumberGenerator { Seed = (ulong)Mathf.RoundToInt(structure.Position.X * 31.0f) };

        for (int layer = 0; layer < layers; layer++)
        {
            int count = Mathf.Max(Mathf.FloorToInt(structure.Size.X / 0.55f) - layer / 2, 1);
            for (int i = 0; i < count; i++)
            {
                root.AddChild(new MeshInstance3D
                {
                    Name = $"Log{layer}_{i}",
                    Mesh = new CylinderMesh
                    {
                        TopRadius = 0.26f,
                        BottomRadius = 0.28f,
                        Height = structure.Size.Z,
                        RadialSegments = 6,
                        Rings = 1,
                    },
                    MaterialOverride = timber,
                    Position = new Vector3(
                        (i - count * 0.5f + 0.5f) * 0.56f + rng.RandfRange(-0.03f, 0.03f),
                        0.28f + layer * 0.52f,
                        0.0f),
                    Rotation = new Vector3(Mathf.Pi * 0.5f, 0.0f, 0.0f),
                });
            }
        }
    }

    /// <summary>Lattice tower with a glazed cab and catwalk.</summary>
    private void BuildTower(Node3D root, in Structure structure)
    {
        Material timber = MaterialFor(structure.WallMaterial);
        float half = structure.Size.X * 0.5f;
        float height = structure.Size.Y;

        // Four legs, splayed so the tower reads as a truss rather than a post.
        for (int corner = 0; corner < 4; corner++)
        {
            float sx = (corner & 1) == 0 ? -1.0f : 1.0f;
            float sz = (corner & 2) == 0 ? -1.0f : 1.0f;

            root.AddChild(new MeshInstance3D
            {
                Name = $"Leg{corner}",
                Mesh = new BoxMesh { Size = new Vector3(0.3f, height, 0.3f) },
                MaterialOverride = timber,
                Position = new Vector3(sx * half * 0.62f, height * 0.5f, sz * half * 0.62f),
                Rotation = new Vector3(sz * 0.055f, 0.0f, -sx * 0.055f),
            });
        }

        // Cross-bracing every few metres, which is what actually reads as a lattice
        // at distance rather than four sticks.
        int bands = Mathf.Max(Mathf.FloorToInt(height / 3.2f), 2);
        for (int band = 1; band <= bands; band++)
        {
            float y = band / (float)(bands + 1) * height;
            float ring = half * 0.62f * (1.0f + (1.0f - y / height) * 0.11f);

            for (int side = 0; side < 4; side++)
            {
                float angle = side * Mathf.Pi * 0.5f;
                root.AddChild(new MeshInstance3D
                {
                    Name = $"Brace{band}_{side}",
                    Mesh = new BoxMesh { Size = new Vector3(ring * 2.1f, 0.14f, 0.14f) },
                    MaterialOverride = timber,
                    Position = new Vector3(Mathf.Sin(angle) * ring, y, Mathf.Cos(angle) * ring),
                    Rotation = new Vector3(0.0f, angle, band % 2 == 0 ? 0.42f : -0.42f),
                });
            }
        }

        root.AddChild(new MeshInstance3D
        {
            Name = "Catwalk",
            Mesh = new BoxMesh { Size = new Vector3(structure.Size.X * 1.35f, 0.2f, structure.Size.X * 1.35f) },
            MaterialOverride = timber,
            Position = new Vector3(0.0f, height, 0.0f),
        });

        root.AddChild(new MeshInstance3D
        {
            Name = "Cab",
            Mesh = new BoxMesh { Size = new Vector3(structure.Size.X * 0.9f, 2.6f, structure.Size.X * 0.9f) },
            MaterialOverride = MaterialFor("county_glass_dark"),
            Position = new Vector3(0.0f, height + 1.4f, 0.0f),
        });

        root.AddChild(new MeshInstance3D
        {
            Name = "CabRoof",
            Mesh = new BoxMesh { Size = new Vector3(structure.Size.X * 1.15f, 0.22f, structure.Size.X * 1.15f) },
            MaterialOverride = MaterialFor(structure.RoofMaterial),
            Position = new Vector3(0.0f, height + 2.8f, 0.0f),
        });
    }

    public void Rebuild()
    {
        foreach (Vector2I chunk in new List<Vector2I>(_chunks.Keys))
        {
            ReleaseChunk(chunk);
        }

        LoadPalette();
        PlanCounty();
    }

    /// <summary>Total structures planned across the county. For tests.</summary>
    public int StructureCount
    {
        get
        {
            int total = 0;
            foreach (List<Structure> list in _byChunk.Values)
            {
                total += list.Count;
            }

            return total;
        }
    }
}
