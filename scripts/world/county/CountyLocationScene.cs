#nullable enable

using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Builds the first authored layout pass for one county location. Each location
/// has its own scene and selects a distinct plan here; later art passes can replace
/// individual generated buildings without changing its map placement.
/// </summary>
[Tool]
public partial class CountyLocationScene : Node3D
{
    public enum LocationKind
    {
        SheriffsOffice,
        Hospital,
        ServiceStation,
        PineRidge,
        FireLookout,
        BlackwaterDam,
        LoggingCamp,
        FarmDistrict,
        MillCreek,
        RailwayCrossing,
        CountyFairgrounds,
        TrailerPark,
        SouthFarmland,
    }

    [Export] public string LocationName { get; set; } = string.Empty;
    [Export] public LocationKind Kind { get; set; }

    private readonly Dictionary<string, StandardMaterial3D> _materials = new();

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            // Location scenes may carry finished project assets alongside this
            // generated layout. They are deliberately named Authored* so a tool
            // refresh never deletes them.
            if (child.Name.ToString().StartsWith("Authored"))
            {
                continue;
            }
            RemoveChild(child);
            child.QueueFree();
        }

        switch (Kind)
        {
            case LocationKind.SheriffsOffice: BuildSheriffsOffice(); break;
            case LocationKind.Hospital: BuildHospital(); break;
            case LocationKind.ServiceStation: BuildServiceStation(); break;
            case LocationKind.PineRidge: BuildPineRidge(); break;
            case LocationKind.FireLookout: BuildFireLookout(); break;
            case LocationKind.BlackwaterDam: BuildDam(); break;
            case LocationKind.LoggingCamp: BuildLoggingCamp(); break;
            case LocationKind.FarmDistrict: BuildFarmDistrict(false); break;
            case LocationKind.MillCreek: BuildMillCreek(); break;
            case LocationKind.RailwayCrossing: BuildRailwayCrossing(); break;
            case LocationKind.CountyFairgrounds: BuildFairgrounds(); break;
            case LocationKind.TrailerPark: BuildTrailerPark(); break;
            case LocationKind.SouthFarmland: BuildFarmDistrict(true); break;
        }
    }

    private void BuildSheriffsOffice()
    {
        if (!HasNode("AuthoredPoliceStation"))
        {
            AddBuilding("SheriffsOffice", new Vector3(0, 0, 0), new Vector3(30, 6.5f, 18), "brick", "roof");
        }
        AddBuilding("JailWing", new Vector3(-18, 0, 3), new Vector3(10, 5.2f, 12), "concrete", "roof");
        AddBox("EntranceCanopy", new Vector3(8, 3.1f, 11), new Vector3(11, 0.35f, 5), "metal");
        AddBox("FlagPole", new Vector3(17, 7, 11), new Vector3(0.25f, 14, 0.25f), "metal");
        AddParkingRows(4, 13, 26);
    }

    private void BuildHospital()
    {
        AddBuilding("HospitalMain", new Vector3(0, 0, 0), new Vector3(48, 12, 27), "hospital", "roof");
        AddBuilding("EmergencyWing", new Vector3(29, 0, 4), new Vector3(18, 7, 19), "concrete", "roof");
        AddBox("EmergencyCanopy", new Vector3(30, 4.2f, 17), new Vector3(19, 0.45f, 8), "metal");
        AddBox("RoofPlant", new Vector3(-8, 13.2f, 0), new Vector3(13, 2.2f, 8), "metal");
        AddBox("MedicalCrossVertical", new Vector3(0, 9, 13.65f), new Vector3(1.2f, 5, 0.25f), "red", false);
        AddBox("MedicalCrossHorizontal", new Vector3(0, 9, 13.8f), new Vector3(4, 1.2f, 0.25f), "red", false);
        AddParkingRows(6, 18, 38);
    }

    private void BuildServiceStation()
    {
        if (!HasNode("AuthoredServiceStation"))
        {
            AddBuilding("Workshop", new Vector3(-13, 0, -5), new Vector3(26, 6, 17), "cream", "roof");
        }
        AddBox("ForecourtCanopy", new Vector3(17, 4.8f, 5), new Vector3(29, 0.7f, 16), "red");
        for (int x = 8; x <= 26; x += 9)
        {
            AddBox($"Pump{x}", new Vector3(x, 1.1f, 5), new Vector3(1.2f, 2.2f, 1.2f), "red");
        }
        AddBox("PriceSign", new Vector3(34, 5, 17), new Vector3(2.4f, 10, 0.8f), "cream");
    }

    private void BuildPineRidge()
    {
        for (int i = 0; i < 16; i++)
        {
            float x = (i % 8 - 3.5f) * 32.0f;
            float z = (i / 8 == 0 ? -48.0f : 48.0f) + (i % 2) * 6.0f;
            AddBuilding($"Cabin{i + 1}", new Vector3(x, 0, z), new Vector3(12, 4.2f, 9), "timber", "rust");
            AddBox($"Porch{i + 1}", new Vector3(x, 0.35f, z + 6), new Vector3(8, 0.7f, 3), "timber");
        }
        AddBuilding("RidgeLodge", new Vector3(0, 0, 0), new Vector3(24, 6.5f, 14), "timber", "roof");
    }

    private void BuildFireLookout()
    {
        for (int sx = -1; sx <= 1; sx += 2)
        for (int sz = -1; sz <= 1; sz += 2)
        {
            AddBox($"TowerLeg{sx}_{sz}", new Vector3(sx * 5, 12, sz * 5), new Vector3(0.55f, 24, 0.55f), "metal");
        }
        for (int y = 4; y <= 20; y += 4)
        {
            AddBox($"BraceX{y}", new Vector3(0, y, 5), new Vector3(11, 0.35f, 0.35f), "metal");
            AddBox($"BraceZ{y}", new Vector3(5, y, 0), new Vector3(0.35f, 0.35f, 11), "metal");
        }
        AddBox("LookoutDeck", new Vector3(0, 24.3f, 0), new Vector3(15, 0.7f, 15), "timber");
        AddBuilding("LookoutCab", new Vector3(0, 24.6f, 0), new Vector3(10, 4.5f, 10), "cream", "roof");
    }

    private void BuildDam()
    {
        AddBox("DamWall", new Vector3(0, -9, 0), new Vector3(190, 26, 14), "concrete");
        AddBox("DamCrestRoad", new Vector3(0, 4.2f, 0), new Vector3(196, 0.5f, 12), "asphalt");
        AddBuilding("PowerHouse", new Vector3(62, -5, 22), new Vector3(42, 15, 26), "concrete", "metal");
        for (int x = -70; x <= 35; x += 35)
        {
            AddBox($"Spillway{x}", new Vector3(x, -7, 7.5f), new Vector3(22, 18, 2), "dark_concrete");
        }
    }

    private void BuildLoggingCamp()
    {
        AddBuilding("Sawmill", new Vector3(0, 0, 0), new Vector3(44, 8, 22), "timber", "rust");
        AddBuilding("EquipmentShed", new Vector3(36, 0, -18), new Vector3(27, 6, 15), "metal", "rust");
        AddBuilding("CampOffice", new Vector3(-35, 0, 18), new Vector3(18, 5, 12), "cream", "roof");
        for (int row = 0; row < 4; row++)
        for (int log = 0; log < 7; log++)
        {
            AddCylinder($"Log{row}_{log}", new Vector3(-25 + log * 7, 1.1f, -29 + row * 3), 0.8f, 6, "timber", new Vector3(0, 0, Mathf.Pi * 0.5f));
        }
    }

    private void BuildFarmDistrict(bool southern)
    {
        string prefix = southern ? "South" : "West";
        int halfColumns = southern ? 5 : 4;
        for (int row = -3; row <= 3; row++)
        for (int col = -halfColumns; col <= halfColumns; col++)
        {
            string material = (row + col) % 2 == 0 ? "field_green" : "field_gold";
            AddBox($"{prefix}Field{row}_{col}", new Vector3(col * 72, 0.02f, row * 70), new Vector3(64, 0.08f, 62), material, false);
        }
        AddFarmstead(prefix + "West", new Vector3(-220, 0, -155));
        AddFarmstead(prefix + "Central", new Vector3(0, 0, -170));
        AddFarmstead(prefix + "East", new Vector3(220, 0, -145));
        if (southern)
        {
            AddBuilding("LivestockBarn", new Vector3(250, 0, 165), new Vector3(42, 9, 23), "timber", "rust");
        }
    }

    private void AddFarmstead(string name, Vector3 position)
    {
        AddBuilding(name + "Barn", position, new Vector3(32, 9, 20), "red", "rust");
        AddBuilding(name + "House", position + new Vector3(32, 0, 3), new Vector3(18, 5, 13), "cream", "roof");
        AddCylinder(name + "Silo", position + new Vector3(-25, 8, 2), 6, 16, "metal");
    }

    private void BuildMillCreek()
    {
        AddBuilding("MillCreekStore", new Vector3(0, 0, 0), new Vector3(24, 6, 15), "brick", "roof");
        AddCylinder("WaterTower", new Vector3(-42, 15, -20), 5, 9, "metal");
        for (int i = 0; i < 20; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            float x = -145 + (i / 2) * 32;
            AddBuilding($"MillCreekHouse{i + 1}", new Vector3(x, 0, side * 52), new Vector3(14, 4.5f, 10), i % 3 == 0 ? "cream" : "timber", "roof");
        }
    }

    private void BuildRailwayCrossing()
    {
        AddBox("RailA", new Vector3(0, 0.2f, -2.1f), new Vector3(110, 0.25f, 0.22f), "metal");
        AddBox("RailB", new Vector3(0, 0.2f, 2.1f), new Vector3(110, 0.25f, 0.22f), "metal");
        for (int x = -50; x <= 50; x += 4)
        {
            AddBox($"Sleeper{x}", new Vector3(x, 0.08f, 0), new Vector3(0.35f, 0.2f, 7), "timber", false);
        }
        AddBuilding("FreightDepot", new Vector3(-28, 0, -18), new Vector3(30, 6, 13), "timber", "rust");
        for (int side = -1; side <= 1; side += 2)
        {
            AddBox($"SignalPost{side}", new Vector3(8, 4, side * 9), new Vector3(0.4f, 8, 0.4f), "metal");
            AddBox($"SignalCrossbar{side}", new Vector3(8, 7, side * 9), new Vector3(6, 0.5f, 0.5f), "cream");
        }
    }

    private void BuildFairgrounds()
    {
        for (int i = 0; i < 28; i++)
        {
            float angle = Mathf.Tau * i / 28.0f;
            AddBox($"ArenaFence{i}", new Vector3(Mathf.Cos(angle) * 58, 1, Mathf.Sin(angle) * 38), new Vector3(4.5f, 2, 0.35f), "timber", false, new Vector3(0, -angle, 0));
        }
        for (int row = 0; row < 4; row++)
        {
            AddBox($"Grandstand{row}", new Vector3(-70 - row * 1.6f, 1 + row * 1.1f, 0), new Vector3(2.6f, 0.45f, 60), "timber");
        }
        AddBuilding("ExhibitionHall", new Vector3(58, 0, -52), new Vector3(44, 8, 20), "red", "metal");
        AddBuilding("FairOffice", new Vector3(54, 0, 50), new Vector3(20, 5, 13), "cream", "roof");
    }

    private void BuildTrailerPark()
    {
        for (int row = -2; row <= 2; row++)
        for (int col = -5; col <= 5; col++)
        {
            float x = col * 19;
            float z = row * 22 + (col % 2) * 3;
            AddBuilding($"Trailer{row}_{col}", new Vector3(x, 0.7f, z), new Vector3(14, 3.2f, 6), col % 2 == 0 ? "cream" : "hospital", "metal", false);
            AddBox($"TrailerStep{row}_{col}", new Vector3(x, 0.25f, z + 4), new Vector3(3, 0.5f, 2), "timber");
        }
        AddBuilding("TrailerParkOffice", new Vector3(0, 0, 0), new Vector3(18, 5, 11), "brick", "roof");
    }

    private void AddParkingRows(int rows, float spacing, float width)
    {
        for (int row = 0; row < rows; row++)
        {
            AddBox($"ParkingStripe{row}", new Vector3(-width * 0.5f + row * spacing, 0.05f, 28), new Vector3(0.22f, 0.04f, 12), "cream", false);
        }
    }

    private void AddBuilding(string name, Vector3 basePosition, Vector3 size, string walls, string roof, bool pitched = true)
    {
        AddBox(name, basePosition + Vector3.Up * (size.Y * 0.5f), size, walls);
        if (pitched)
        {
            AddBox(name + "RoofA", basePosition + new Vector3(0, size.Y + 1.0f, -size.Z * 0.23f), new Vector3(size.X + 1.2f, 0.5f, size.Z * 0.58f), roof, false, new Vector3(-0.35f, 0, 0));
            AddBox(name + "RoofB", basePosition + new Vector3(0, size.Y + 1.0f, size.Z * 0.23f), new Vector3(size.X + 1.2f, 0.5f, size.Z * 0.58f), roof, false, new Vector3(0.35f, 0, 0));
        }
    }

    private void AddBox(string name, Vector3 position, Vector3 size, string material, bool collision = true, Vector3? rotation = null)
    {
        Vector3 euler = rotation ?? Vector3.Zero;
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new BoxMesh { Size = size },
            MaterialOverride = MaterialFor(material),
            Position = position,
            Rotation = euler,
            VisibilityRangeEnd = 1800.0f,
            VisibilityRangeEndMargin = 120.0f,
        };
        AddChild(mesh);
        if (!collision) return;

        var body = new StaticBody3D { Name = name + "Collision", Position = position, Rotation = euler };
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        AddChild(body);
    }

    private void AddCylinder(string name, Vector3 position, float radius, float height, string material, Vector3? rotation = null)
    {
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 12 },
            MaterialOverride = MaterialFor(material),
            Position = position,
            Rotation = rotation ?? Vector3.Zero,
            VisibilityRangeEnd = 1800.0f,
        };
        AddChild(mesh);
    }

    private StandardMaterial3D MaterialFor(string name)
    {
        if (_materials.TryGetValue(name, out StandardMaterial3D? material)) return material;
        Color color = name switch
        {
            "earth" => new Color(0.25f, 0.23f, 0.17f),
            "asphalt" => new Color(0.09f, 0.10f, 0.10f),
            "brick" => new Color(0.42f, 0.17f, 0.12f),
            "hospital" => new Color(0.66f, 0.72f, 0.68f),
            "cream" => new Color(0.72f, 0.67f, 0.53f),
            "concrete" => new Color(0.45f, 0.46f, 0.43f),
            "dark_concrete" => new Color(0.24f, 0.26f, 0.25f),
            "metal" => new Color(0.32f, 0.35f, 0.34f),
            "rust" => new Color(0.34f, 0.18f, 0.11f),
            "timber" => new Color(0.30f, 0.21f, 0.13f),
            "red" => new Color(0.52f, 0.08f, 0.055f),
            "field_green" => new Color(0.28f, 0.37f, 0.15f),
            "field_gold" => new Color(0.48f, 0.39f, 0.16f),
            "roof" => new Color(0.16f, 0.18f, 0.17f),
            _ => new Color(0.5f, 0.5f, 0.46f),
        };
        material = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.92f };
        _materials[name] = material;
        return material;
    }
}
