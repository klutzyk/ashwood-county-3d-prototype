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
    private readonly Dictionary<string, PackedScene> _packedScenes = new();

    private static readonly Dictionary<string, string> MaterialPaths = new()
    {
        ["earth"] = "res://assets/materials/county/county_dirt_yard.tres",
        ["asphalt"] = "res://assets/materials/county/county_asphalt_lot.tres",
        ["brick"] = "res://assets/materials/county/county_brick_red.tres",
        ["hospital"] = "res://assets/materials/county/county_siding_white.tres",
        ["cream"] = "res://assets/materials/county/county_siding_cream.tres",
        ["concrete"] = "res://assets/materials/county/county_concrete.tres",
        ["dark_concrete"] = "res://assets/materials/county/county_concrete_block.tres",
        ["metal"] = "res://assets/materials/county/county_galvanised.tres",
        ["rust"] = "res://assets/materials/county/county_roof_rust.tres",
        ["timber"] = "res://assets/materials/county/county_log_wall.tres",
        ["raw_timber"] = "res://assets/materials/county/county_timber_raw.tres",
        ["grey_timber"] = "res://assets/materials/county/county_timber_grey.tres",
        ["red"] = "res://assets/materials/county/county_siding_red.tres",
        ["blue"] = "res://assets/materials/county/county_siding_blue.tres",
        ["green"] = "res://assets/materials/county/county_siding_green.tres",
        ["roof"] = "res://assets/materials/county/county_roof_shingle.tres",
        ["tin"] = "res://assets/materials/county/county_roof_tin.tres",
        ["corrugated"] = "res://assets/materials/county/county_corrugated.tres",
        ["glass"] = "res://assets/materials/county/county_glass_dark.tres",
        ["broken_glass"] = "res://assets/materials/county/county_glass_broken.tres",
        ["door"] = "res://assets/materials/county/county_door_paint.tres",
        ["trim"] = "res://assets/materials/county/county_trim_white.tres",
        ["dark_trim"] = "res://assets/materials/county/county_trim_dark.tres",
        ["gravel"] = "res://assets/materials/county/county_gravel.tres",
        ["sign"] = "res://assets/materials/county/county_signage.tres",
        ["stone"] = "res://assets/materials/county/county_stone.tres",
        ["tyre"] = "res://assets/materials/county/county_tyre.tres",
        ["tarp"] = "res://assets/materials/county/county_tarp.tres",
        ["field_green"] = "res://assets/materials/county/county_field_meadow.tres",
        ["field_gold"] = "res://assets/materials/county/county_field_tilled.tres",
    };

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
        AddGroundPatch("PatrolLot", new Vector3(1, 0.03f, 29), new Vector3(58, 0.06f, 28), "asphalt");
        AddParkingRows(5, 11, 44);
        AddAsset("PatrolCar", "res://assets/environment/props/user_supplied/crown_victoria_police.tscn", new Vector3(-12, 0.05f, 27), 8);
        AddAsset("ImpoundedPickup", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(9, 0.05f, 31), -6);
        AddAsset("EvidenceBins", "res://assets/environment/props/barrel_crate.tscn", new Vector3(-22, 0, -5), 16);
        AddAsset("FrontLamp", "res://assets/environment/props/poly_haven_street_lamp.tscn", new Vector3(14, 0, 17), 0);
        AddFenceLine("SecureFence", new Vector3(-25, 0, -10), new Vector3(-25, 0, 16), 3.0f, "metal");
        AddShrubs("CivicPlanting", new Vector3(9, 0, 13), 5, 3.2f);
        AddSignLabel("SheriffSiteSign", "ASHWOOD COUNTY SHERIFF", new Vector3(0, 7.5f, 9.25f), 54, new Color(0.9f, 0.78f, 0.43f));
    }

    private void BuildHospital()
    {
        AddBuilding("HospitalMain", new Vector3(0, 0, 0), new Vector3(48, 12, 27), "hospital", "roof");
        AddBuilding("EmergencyWing", new Vector3(29, 0, 4), new Vector3(18, 7, 19), "concrete", "roof");
        AddBox("EmergencyCanopy", new Vector3(30, 4.2f, 17), new Vector3(19, 0.45f, 8), "metal");
        AddBox("RoofPlant", new Vector3(-8, 13.2f, 0), new Vector3(13, 2.2f, 8), "metal");
        AddBox("RoofPlantLouvers", new Vector3(-8, 13.3f, 4.05f), new Vector3(11, 1.2f, 0.12f), "dark_trim", false);
        AddBox("MedicalCrossVertical", new Vector3(0, 9, 13.65f), new Vector3(1.2f, 5, 0.25f), "red", false);
        AddBox("MedicalCrossHorizontal", new Vector3(0, 9, 13.8f), new Vector3(4, 1.2f, 0.25f), "red", false);
        AddGroundPatch("HospitalParking", new Vector3(0, 0.03f, 39), new Vector3(92, 0.06f, 44), "asphalt");
        AddParkingRows(8, 10, 72);
        AddAsset("AmbulanceStandIn", "res://assets/environment/vehicles/parked_american_panel_van.tscn", new Vector3(29, 0.05f, 22), -3);
        AddAsset("VisitorCarA", "res://assets/environment/vehicles/parked_1975_impala.tscn", new Vector3(-23, 0.05f, 37), 0);
        AddAsset("VisitorCarB", "res://assets/environment/vehicles/parked_rusted_alfa_visual.tscn", new Vector3(-8, 0.05f, 37), 3);
        AddAsset("EmergencyDoor", "res://assets/environment/props/user_supplied/hospital_door.tscn", new Vector3(30, 0, 13.7f), 0);
        AddAsset("WaitingBench", "res://assets/environment/props/cc0_park_bench.tscn", new Vector3(-17, 0, 17), 0);
        AddAsset("MedicalWaste", "res://assets/environment/props/user_supplied/trash_can.tscn", new Vector3(38, 0, 11), 0);
        AddShrubs("HospitalPlanting", new Vector3(-4, 0, 15.5f), 7, 4.0f);
        AddSignLabel("HospitalName", "ASHWOOD COUNTY MEDICAL CENTER", new Vector3(0, 10.6f, 13.72f), 62, new Color(0.92f, 0.94f, 0.9f));
        AddSignLabel("EmergencySign", "EMERGENCY", new Vector3(30, 5.05f, 13.76f), 48, new Color(0.92f, 0.15f, 0.11f));
    }

    private void BuildServiceStation()
    {
        if (!HasNode("AuthoredServiceStation"))
        {
            AddBuilding("Workshop", new Vector3(-13, 0, -5), new Vector3(26, 6, 17), "cream", "roof");
        }
        AddBox("ForecourtCanopy", new Vector3(17, 4.8f, 5), new Vector3(29, 0.7f, 16), "red");
        AddBox("CanopyFascia", new Vector3(17, 4.75f, 13.1f), new Vector3(29.4f, 1.0f, 0.25f), "sign", false);
        for (int x = 8; x <= 26; x += 9)
        {
            AddBox($"Pump{x}", new Vector3(x, 1.1f, 5), new Vector3(1.2f, 2.2f, 1.2f), "red");
            AddBox($"PumpFace{x}", new Vector3(x, 1.3f, 5.62f), new Vector3(0.72f, 0.72f, 0.08f), "glass", false);
            AddCylinder($"PumpHose{x}", new Vector3(x + 0.8f, 1.0f, 5), 0.07f, 1.7f, "tyre");
        }
        AddBox("PriceSign", new Vector3(34, 5, 17), new Vector3(2.4f, 10, 0.8f), "cream");
        AddBox("PriceBoard", new Vector3(34, 6.5f, 17.45f), new Vector3(3.8f, 4.8f, 0.16f), "sign", false);
        AddGroundPatch("Forecourt", new Vector3(10, 0.02f, 7), new Vector3(64, 0.05f, 44), "concrete");
        AddAsset("CustomerCar", "res://assets/environment/vehicles/parked_1975_impala.tscn", new Vector3(16, 0.05f, 5), 90);
        AddAsset("ServicePickup", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(-15, 0.05f, 13), -15);
        AddAsset("TyreStackA", "res://assets/environment/vehicles/old_tyre.tscn", new Vector3(-24, 0, 2), 0);
        AddAsset("TyreStackB", "res://assets/environment/vehicles/old_tyre.tscn", new Vector3(-23, 0.45f, 2), 21);
        AddAsset("WorkshopClutter", "res://assets/environment/props/barrel_crate.tscn", new Vector3(-24, 0, -9), -14);
        AddAsset("RoadSign", "res://assets/environment/roads/weathered_roadsign.tscn", new Vector3(36, 0, 14), -5);
        AddSignLabel("StationName", "ASHWOOD FUEL & SERVICE", new Vector3(17, 5.15f, 13.28f), 54, new Color(0.96f, 0.85f, 0.55f));
    }

    private void BuildPineRidge()
    {
        for (int i = 0; i < 12; i++)
        {
            float x = (i % 6 - 2.5f) * 34.0f;
            float side = i < 6 ? -1.0f : 1.0f;
            float z = side * (48.0f + (i % 3) * 4.0f);
            float yaw = side < 0 ? 180.0f : 0.0f;
            AddAsset($"Cabin{i + 1}", "res://assets/environment/buildings/old_russian_house.tscn", new Vector3(x, 0, z), yaw + ((i % 3) - 1) * 4, Vector3.One * 0.86f);
            AddAsset($"CabinDressing{i + 1}", "res://assets/environment/props/roadside_dressing.tscn", new Vector3(x + side * 3, 0, z + side * 7), yaw);
        }
        AddAsset("RidgeLodge", "res://assets/environment/buildings/House/house.tscn", new Vector3(0, 0, 0), 0, Vector3.One * 1.45f);
        AddBox("LodgePorch", new Vector3(0, 0.35f, 10.5f), new Vector3(19, 0.7f, 4), "raw_timber");
        AddBox("LodgeSign", new Vector3(0, 4.8f, 8.25f), new Vector3(11, 1.7f, 0.18f), "sign", false);
        AddAsset("LodgePickup", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(18, 0, 15), 16);
        AddAsset("TrailMapBench", "res://assets/environment/props/cc0_park_bench.tscn", new Vector3(-14, 0, 10), 0);
        AddForestScatter("PineRidgeForest", 42, 118, 72);
        AddSignLabel("LodgeName", "PINE RIDGE LODGE", new Vector3(0, 5.0f, 8.37f), 58, new Color(0.91f, 0.79f, 0.5f));
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
            AddBox($"BraceXBack{y}", new Vector3(0, y, -5), new Vector3(11, 0.22f, 0.22f), "rust", false, new Vector3(0, 0, y % 8 == 0 ? 0.62f : -0.62f));
            AddBox($"BraceZBack{y}", new Vector3(-5, y, 0), new Vector3(0.22f, 0.22f, 11), "rust", false, new Vector3(y % 8 == 0 ? 0.62f : -0.62f, 0, 0));
        }
        AddBox("LookoutDeck", new Vector3(0, 24.3f, 0), new Vector3(15, 0.7f, 15), "timber");
        AddBuilding("LookoutCab", new Vector3(0, 24.6f, 0), new Vector3(10, 4.5f, 10), "cream", "roof");
        AddRailingRect("DeckRail", new Vector3(0, 25.4f, 0), 15, 15, "metal");
        for (int y = 2; y <= 22; y += 2)
        {
            AddBox($"LadderRung{y}", new Vector3(-5.35f, y, 0), new Vector3(0.3f, 0.12f, 2.2f), "metal", false);
        }
        AddBox("RadioMast", new Vector3(0, 34, 0), new Vector3(0.18f, 10, 0.18f), "metal", false);
        AddAsset("LookoutTruck", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(18, 0, 8), -24);
        AddAsset("Firewood", "res://assets/environment/nature/polyhaven/ashwood_dead_log.tscn", new Vector3(-15, 0, 8), 32);
        AddForestScatter("LookoutForest", 24, 58, 38);
    }

    private void BuildDam()
    {
        AddBox("DamWall", new Vector3(0, -9, 0), new Vector3(190, 26, 14), "concrete");
        AddBox("DamCrestRoad", new Vector3(0, 4.2f, 0), new Vector3(196, 0.5f, 12), "asphalt");
        AddBuilding("PowerHouse", new Vector3(62, -5, 22), new Vector3(42, 15, 26), "concrete", "metal");
        for (int x = -70; x <= 35; x += 35)
        {
            AddBox($"Spillway{x}", new Vector3(x, -7, 7.5f), new Vector3(22, 18, 2), "dark_concrete");
            AddBox($"SpillwayGate{x}", new Vector3(x, 0.5f, 8.7f), new Vector3(18, 5.5f, 0.45f), "rust", false);
            AddCylinder($"GateWheel{x}", new Vector3(x, 4.2f, 9.2f), 1.4f, 0.25f, "metal", new Vector3(Mathf.Pi * 0.5f, 0, 0));
        }
        AddRailingLine("NorthCrestRail", new Vector3(-96, 5.2f, -5.5f), new Vector3(96, 5.2f, -5.5f), 4, "metal");
        AddRailingLine("SouthCrestRail", new Vector3(-96, 5.2f, 5.5f), new Vector3(96, 5.2f, 5.5f), 4, "metal");
        AddBox("ControlHouse", new Vector3(-70, 7.5f, 0), new Vector3(13, 6, 9), "brick");
        AddWindow("ControlWindowA", new Vector3(-70, 8.4f, 4.58f), new Vector2(4.8f, 2.0f));
        AddBox("WarningSign", new Vector3(-45, 6.6f, 6.05f), new Vector3(7, 2, 0.12f), "sign", false);
        AddAsset("MaintenanceTruck", "res://assets/environment/vehicles/parked_american_panel_van.tscn", new Vector3(72, 5, -1), 90);
        AddAsset("PowerUtility", "res://assets/environment/props/poly_haven_utility_box_02.tscn", new Vector3(79, -5, 37), 15);
        AddPipeBetween("PenstockA", new Vector3(28, 0, -5), new Vector3(52, -7, 20), 1.8f, "metal");
        AddPipeBetween("PenstockB", new Vector3(37, 0, -5), new Vector3(61, -7, 20), 1.8f, "metal");
        AddPipeBetween("PenstockC", new Vector3(46, 0, -5), new Vector3(70, -7, 20), 1.8f, "metal");
        AddAsset("DamUtilityPoleA", "res://assets/environment/roads/utility_pole.tscn", new Vector3(84, -5, 35), 18);
        AddAsset("DamUtilityPoleB", "res://assets/environment/roads/utility_pole.tscn", new Vector3(104, -5, 43), 18);
        AddAsset("DamCrates", "res://assets/third_party/props/crates/plastic_crate_01_1k.gltf/plastic_crate_01_1k.gltf", new Vector3(47, -5, 31), 12);
        AddAsset("DamWorkLight", "res://assets/third_party/interiors/pharmacy/poly_haven/industrial_wall_lamp/industrial_wall_lamp_1k.gltf", new Vector3(47, 3.8f, 35.2f), 0, Vector3.One * 1.8f);
        AddSignLabel("DamWarning", "BLACKWATER HYDROELECTRIC", new Vector3(62, 6.5f, 35.15f), 56, new Color(0.94f, 0.8f, 0.42f));
    }

    private void BuildLoggingCamp()
    {
        AddBuilding("Sawmill", new Vector3(0, 0, 0), new Vector3(44, 8, 22), "timber", "rust");
        AddBuilding("EquipmentShed", new Vector3(36, 0, -18), new Vector3(27, 6, 15), "metal", "rust");
        AddBuilding("CampOffice", new Vector3(-35, 0, 18), new Vector3(18, 5, 12), "cream", "roof");
        for (int row = 0; row < 4; row++)
        for (int log = 0; log < 7; log++)
        {
            AddCylinder($"Log{row}_{log}", new Vector3(-25 + log * 7, 1.1f + (row % 2) * 0.3f, -29 + row * 3), 0.8f, 6, "raw_timber", new Vector3(0, 0, Mathf.Pi * 0.5f));
        }
        AddGroundPatch("LoggingYard", new Vector3(0, 0.02f, -4), new Vector3(110, 0.05f, 82), "earth");
        AddAsset("LoggingTruck", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(27, 0.05f, 18), -18);
        AddAsset("AbandonedCar", "res://assets/environment/vehicles/parked_rusted_alfa_visual.tscn", new Vector3(43, 0.05f, -8), 70);
        AddAsset("CampClutter", "res://assets/environment/props/barrel_crate.tscn", new Vector3(-31, 0, 10), 24);
        AddAsset("LumberRackA", "res://assets/environment/buildings/MillerHardware/fixtures/miller_lumber_rack_production.tscn", new Vector3(15, 0, -18), 90);
        AddAsset("LumberRackB", "res://assets/environment/buildings/MillerHardware/fixtures/miller_lumber_rack_production.tscn", new Vector3(23, 0, -18), 90);
        AddAsset("IndustrialCart", "res://assets/third_party/environment/main_street_dressing/poly_haven/industrial_storage_cart/industrial_storage_cart_1k.gltf", new Vector3(19, 0, 14), -12);
        AddAsset("ToolWall", "res://assets/environment/buildings/MillerHardware/fixtures/miller_pegboard_tool_wall.glb", new Vector3(35, 1.1f, -10.2f), 180);
        AddAsset("StumpA", "res://assets/environment/nature/polyhaven/ashwood_tree_stump_01.tscn", new Vector3(-48, 0, -31), 0);
        AddAsset("StumpB", "res://assets/environment/nature/polyhaven/ashwood_tree_stump_02.tscn", new Vector3(49, 0, 31), 70);
        AddAsset("Deadfall", "res://assets/environment/nature/polyhaven/ashwood_dead_log.tscn", new Vector3(-51, 0, 28), 42);
        AddForestScatter("LoggingForestEdge", 30, 80, 53);
        AddSignLabel("SawmillName", "BLACKWATER TIMBER CO.", new Vector3(0, 6.1f, 11.15f), 58, new Color(0.9f, 0.72f, 0.4f));
    }

    private void BuildFarmDistrict(bool southern)
    {
        string prefix = southern ? "South" : "West";
        int columns = southern ? 6 : 5;
        for (int row = 0; row < 4; row++)
        for (int col = 0; col < columns; col++)
        {
            float x = (col - (columns - 1) * 0.5f) * 128.0f + Mathf.Sin(row * 1.7f + col) * 11.0f;
            float z = (row - 1.5f) * 142.0f + Mathf.Cos(col * 1.3f - row) * 13.0f;
            float width = 96.0f + ((row * 17 + col * 23) % 29);
            float depth = 108.0f + ((row * 31 + col * 11) % 35);
            bool tilled = (row * 2 + col) % 4 == 0 || (southern && (row + col) % 5 == 0);
            AddFieldPlot($"{prefix}Field{row}_{col}", new Vector3(x, 0.02f, z), new Vector2(width, depth), tilled);
        }
        AddGroundPatch(prefix + "FarmLane", new Vector3(0, 0.07f, -108), new Vector3(columns * 132, 0.08f, 9), "gravel");
        AddGroundPatch(prefix + "CrossLane", new Vector3(64, 0.075f, 0), new Vector3(8, 0.08f, 560), "gravel");
        AddFarmstead(prefix + "West", new Vector3(-220, 0, -155));
        AddFarmstead(prefix + "Central", new Vector3(0, 0, -170));
        AddFarmstead(prefix + "East", new Vector3(220, 0, -145));
        if (southern)
        {
            AddBuilding("LivestockBarn", new Vector3(250, 0, 165), new Vector3(42, 9, 23), "timber", "rust");
            AddFenceLine("LivestockFenceNorth", new Vector3(195, 0, 132), new Vector3(300, 0, 132), 7, "raw_timber");
            AddFenceLine("LivestockFenceSouth", new Vector3(195, 0, 205), new Vector3(300, 0, 205), 7, "raw_timber");
            AddAsset("SouthFarmTruck", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(224, 0, 175), -12);
            AddAsset("SouthFarmPicnic", "res://assets/environment/props/user_supplied/picnic_table.tscn", new Vector3(31, 0, -153), 8);
        }
        else
        {
            AddAsset("FarmDistrictTruck", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(-188, 0, -144), 15);
            AddAsset("RoadsideMailbox", "res://assets/environment/props/home_us_mailbox.tscn", new Vector3(18, 0, -136), 180);
        }
        for (int i = 0; i < 14; i++)
        {
            float x = -columns * 62 + i * (columns * 124 / 13.0f);
            string tree = i % 2 == 0
                ? "res://assets/environment/nature/polyhaven/ashwood_jacaranda_lod1.tscn"
                : "res://assets/environment/nature/polyhaven/ashwood_fir_b_lod1.tscn";
            AddAsset($"{prefix}Windbreak{i}", tree, new Vector3(x, 0, 226 + Mathf.Sin(i * 1.8f) * 8), i * 29, Vector3.One * (0.7f + (i % 4) * 0.08f));
        }
    }

    private void AddFieldPlot(string name, Vector3 center, Vector2 size, bool tilled)
    {
        string material = tilled ? "field_gold" : "field_green";
        AddBox(name, center, new Vector3(size.X, 0.07f, size.Y), material, false);
        if (!tilled) return;

        int rows = Mathf.Clamp(Mathf.FloorToInt(size.X / 8.0f), 8, 18);
        for (int i = 1; i < rows; i++)
        {
            float x = center.X - size.X * 0.5f + size.X * i / rows;
            AddBox($"{name}Furrow{i}", new Vector3(x, center.Y + 0.045f, center.Z), new Vector3(0.16f, 0.025f, size.Y - 2), "earth", false);
        }
    }

    private void AddFarmstead(string name, Vector3 position)
    {
        AddBuilding(name + "Barn", position, new Vector3(32, 9, 20), "red", "rust");
        AddAsset(name + "House", "res://assets/environment/buildings/House/house.tscn", position + new Vector3(34, 0, 3), 90);
        AddAsset(name + "Roadside", "res://assets/environment/props/roadside_dressing.tscn", position + new Vector3(36, 0, 12), 90);
        AddCylinder(name + "Silo", position + new Vector3(-25, 8, 2), 6, 16, "metal");
        AddCylinder(name + "SiloCap", position + new Vector3(-25, 16.7f, 2), 6.3f, 1.4f, "tin");
        AddAsset(name + "Tree", "res://assets/environment/nature/polyhaven/ashwood_jacaranda_lod0.tscn", position + new Vector3(48, 0, -9), 0, Vector3.One * 0.8f);
    }

    private void BuildMillCreek()
    {
        AddBuilding("MillCreekStore", new Vector3(0, 0, 0), new Vector3(24, 6, 15), "brick", "roof");
        AddCylinder("WaterTower", new Vector3(-42, 15, -20), 5, 9, "metal");
        for (int leg = -1; leg <= 1; leg += 2)
        for (int depth = -1; depth <= 1; depth += 2)
        {
            AddBox($"WaterTowerLeg{leg}_{depth}", new Vector3(-42 + leg * 3.5f, 6, -20 + depth * 3.5f), new Vector3(0.35f, 12, 0.35f), "rust");
        }
        for (int i = 0; i < 20; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            float x = -145 + (i / 2) * 32;
            string housePath = i % 3 == 0
                ? "res://assets/environment/buildings/House/house.tscn"
                : "res://assets/environment/buildings/old_russian_house.tscn";
            AddAsset($"MillCreekHouse{i + 1}", housePath, new Vector3(x, 0, side * 52), side < 0 ? 180 : 0, Vector3.One * (0.88f + (i % 3) * 0.04f));
            AddAsset($"MillCreekRoadside{i + 1}", "res://assets/environment/props/roadside_dressing.tscn", new Vector3(x + 5, 0, side * 43), side < 0 ? 180 : 0);
        }
        AddGroundPatch("MillCreekSquare", new Vector3(0, 0.02f, 12), new Vector3(58, 0.05f, 34), "gravel");
        AddAsset("StorePickup", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(15, 0, 14), 18);
        AddAsset("StoreBench", "res://assets/environment/props/cc0_park_bench.tscn", new Vector3(-7, 0, 10), 0);
        AddAsset("MillCreekUtilityPole", "res://assets/environment/roads/utility_pole.tscn", new Vector3(27, 0, 18), 0);
        AddSignLabel("StoreName", "MILL CREEK GENERAL STORE", new Vector3(0, 4.8f, 7.68f), 54, new Color(0.92f, 0.78f, 0.42f));
    }

    private void BuildRailwayCrossing()
    {
        AddGroundPatch("TrackBallast", new Vector3(0, 0.03f, 0), new Vector3(118, 0.08f, 11), "gravel");
        AddBox("RailA", new Vector3(0, 0.2f, -2.1f), new Vector3(110, 0.25f, 0.22f), "metal");
        AddBox("RailB", new Vector3(0, 0.2f, 2.1f), new Vector3(110, 0.25f, 0.22f), "metal");
        for (int x = -50; x <= 50; x += 4)
        {
            AddBox($"Sleeper{x}", new Vector3(x, 0.08f, 0), new Vector3(0.35f, 0.2f, 7), "timber", false);
        }
        AddBuilding("FreightDepot", new Vector3(-28, 0, -18), new Vector3(30, 6, 13), "timber", "rust");
        AddBox("DepotPlatform", new Vector3(-20, 0.55f, -8), new Vector3(48, 1.1f, 7), "raw_timber");
        AddBox("DepotSign", new Vector3(-28, 4.4f, -11.6f), new Vector3(10, 1.4f, 0.16f), "sign", false);
        for (int side = -1; side <= 1; side += 2)
        {
            AddBox($"SignalPost{side}", new Vector3(8, 4, side * 9), new Vector3(0.4f, 8, 0.4f), "metal");
            AddBox($"SignalCrossbar{side}", new Vector3(8, 7, side * 9), new Vector3(6, 0.5f, 0.5f), "cream");
            AddBox($"SignalLightA{side}", new Vector3(6.5f, 6.5f, side * 9.35f), new Vector3(0.7f, 0.7f, 0.25f), "red", false);
            AddBox($"SignalLightB{side}", new Vector3(9.5f, 6.5f, side * 9.35f), new Vector3(0.7f, 0.7f, 0.25f), "red", false);
            AddBox($"CrossingGate{side}", new Vector3(12, 4.3f, side * 9), new Vector3(10, 0.22f, 0.22f), "trim", false, new Vector3(0, 0, side * -0.12f));
        }
        AddAsset("DepotFreight", "res://assets/environment/props/barrel_crate.tscn", new Vector3(-36, 1.1f, -8), 12);
        AddAsset("DepotLumber", "res://assets/environment/buildings/MillerHardware/fixtures/miller_lumber_rack_production.tscn", new Vector3(-17, 1.1f, -8), 90);
        AddAsset("DepotCart", "res://assets/third_party/environment/main_street_dressing/poly_haven/industrial_storage_cart/industrial_storage_cart_1k.gltf", new Vector3(-5, 0, -13), -20);
        AddAsset("CrossingCar", "res://assets/environment/vehicles/parked_american_panel_van.tscn", new Vector3(25, 0, 15), 87);
        AddAsset("CrossingPickup", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(-42, 0, 17), 76);
        AddAsset("DepotLamp", "res://assets/environment/props/poly_haven_street_lamp.tscn", new Vector3(-8, 0, -12), 0);
        AddAsset("RailUtilityPoleA", "res://assets/environment/roads/utility_pole.tscn", new Vector3(-48, 0, 13), 90);
        AddAsset("RailUtilityPoleB", "res://assets/environment/roads/utility_pole.tscn", new Vector3(48, 0, 13), 90);
        AddAsset("TracksideDebris", "res://assets/environment/nature/polyhaven/ashwood_dry_branches_a.tscn", new Vector3(42, 0, -9), 20);
        AddShrubs("TracksideWeedsA", new Vector3(-37, 0, 7), 5, 3.4f);
        AddShrubs("TracksideWeedsB", new Vector3(35, 0, -7), 4, 3.6f);
        AddSignLabel("DepotName", "MILL CREEK FREIGHT DEPOT", new Vector3(-28, 4.45f, -11.72f), 48, new Color(0.9f, 0.78f, 0.48f));
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
        AddBox("EntryArchLeft", new Vector3(-2.8f, 4, 72), new Vector3(0.7f, 8, 0.7f), "trim");
        AddBox("EntryArchRight", new Vector3(2.8f, 4, 72), new Vector3(0.7f, 8, 0.7f), "trim");
        AddBox("EntryBanner", new Vector3(0, 7.3f, 72), new Vector3(8, 2, 0.18f), "sign", false);
        AddRailingRect("ArenaOuterRail", Vector3.Zero, 124, 84, "raw_timber");
        for (int i = 0; i < 5; i++)
        {
            AddAsset($"PicnicTable{i}", "res://assets/environment/props/user_supplied/picnic_table.tscn", new Vector3(22 + i * 7, 0, 42 + (i % 2) * 6), i * 13);
        }
        for (int i = 0; i < 6; i++)
        {
            float x = -45 + i * 15;
            AddBox($"VendorCounter{i}", new Vector3(x, 1.0f, 58), new Vector3(8, 2, 3.5f), i % 2 == 0 ? "red" : "cream");
            AddBox($"VendorCanopy{i}", new Vector3(x, 3.3f, 58), new Vector3(10, 0.2f, 6), "tarp", false, new Vector3(0.08f * (i % 2 == 0 ? 1 : -1), 0, 0));
            AddBox($"VendorPostL{i}", new Vector3(x - 4.2f, 1.8f, 55.5f), new Vector3(0.14f, 3.6f, 0.14f), "metal", false);
            AddBox($"VendorPostR{i}", new Vector3(x + 4.2f, 1.8f, 55.5f), new Vector3(0.14f, 3.6f, 0.14f), "metal", false);
        }
        AddBuilding("LivestockPavilion", new Vector3(-48, 0, -53), new Vector3(38, 6.5f, 17), "grey_timber", "tin");
        AddFenceLine("LivestockPensA", new Vector3(-56, 0, -25), new Vector3(-15, 0, -25), 3, "raw_timber");
        AddFenceLine("LivestockPensB", new Vector3(-56, 0, -35), new Vector3(-15, 0, -35), 3, "raw_timber");
        AddAsset("FairTruck", "res://assets/environment/vehicles/parked_american_panel_van.tscn", new Vector3(71, 0, -35), 12);
        AddAsset("FairPickup", "res://assets/environment/vehicles/parked_1963_c10.tscn", new Vector3(-63, 0, -43), -8);
        AddAsset("TicketBench", "res://assets/environment/props/cc0_park_bench.tscn", new Vector3(11, 0, 67), 0);
        AddAsset("FairgroundTrash", "res://assets/environment/props/poly_haven_metal_trash_can.tscn", new Vector3(8, 0, 66), 0);
        AddSignLabel("EntryName", "ASHWOOD COUNTY FAIRGROUNDS", new Vector3(0, 7.35f, 72.14f), 48, new Color(0.9f, 0.2f, 0.11f));
    }

    private void BuildTrailerPark()
    {
        for (int row = -2; row <= 1; row++)
        for (int col = -3; col <= 3; col++)
        {
            float x = col * 24 + (row % 2) * 5;
            float z = row * 25 + (col % 2) * 3;
            string siding = ((col + row) % 4) switch { 0 => "cream", 1 => "blue", 2 => "green", _ => "hospital" };
            AddBuilding($"Trailer{row}_{col}", new Vector3(x, 0.7f, z), new Vector3(15.5f, 3.35f, 6.4f), siding, "tin", false);
            AddBox($"TrailerStep{row}_{col}", new Vector3(x, 0.25f, z + 4), new Vector3(3, 0.5f, 2), "timber");
            AddBox($"TrailerSkirt{row}_{col}", new Vector3(x, 0.7f, z), new Vector3(15.7f, 0.8f, 6.6f), "dark_trim", false);
            if ((row + col) % 2 == 0)
            {
                AddBox($"TrailerAwning{row}_{col}", new Vector3(x - 3.4f, 3.25f, z + 4.2f), new Vector3(5.2f, 0.18f, 2.4f), "tarp", false, new Vector3(0.12f, 0, 0));
            }
            if ((row + col) % 3 == 0)
            {
                AddAsset($"TrailerClutter{row}_{col}", "res://assets/environment/props/roadside_dressing.tscn", new Vector3(x + 4, 0, z - 6), row * 7 + col * 3);
            }
        }
        AddBuilding("TrailerParkOffice", new Vector3(0, 0, 0), new Vector3(18, 5, 11), "brick", "roof");
        AddGroundPatch("TrailerParkLoop", new Vector3(0, 0.01f, 0), new Vector3(225, 0.04f, 12), "gravel");
        AddGroundPatch("TrailerParkSpine", new Vector3(0, 0.015f, 0), new Vector3(12, 0.04f, 112), "gravel");
        AddAsset("ParkedPanelVan", "res://assets/environment/vehicles/parked_american_panel_van.tscn", new Vector3(-11, 0, 12), -18);
        AddAsset("AbandonedTrailerCar", "res://assets/environment/vehicles/parked_rusted_alfa_visual.tscn", new Vector3(82, 0, -14), 30);
        AddAsset("OfficeBench", "res://assets/environment/props/cc0_park_bench.tscn", new Vector3(8, 0, 8), 0);
        AddAsset("Playground", "res://assets/environment/props/user_supplied/abandoned_slide.tscn", new Vector3(-36, 0, 5), 22);
        AddAsset("TrailerUtilityPoleA", "res://assets/environment/roads/utility_pole.tscn", new Vector3(-55, 0, 0), 0);
        AddAsset("TrailerUtilityPoleB", "res://assets/environment/roads/utility_pole.tscn", new Vector3(55, 0, 0), 0);
        AddAsset("TrailerShadeTreeA", "res://assets/environment/nature/polyhaven/ashwood_jacaranda_lod0.tscn", new Vector3(-86, 0, -32), 0, Vector3.One * 0.75f);
        AddAsset("TrailerShadeTreeB", "res://assets/environment/nature/polyhaven/ashwood_jacaranda_lod0.tscn", new Vector3(89, 0, 28), 64, Vector3.One * 0.82f);
        AddAsset("CommunityPicnic", "res://assets/environment/props/user_supplied/picnic_table.tscn", new Vector3(-31, 0, 10), 15);
        AddAsset("CommunityTrash", "res://assets/environment/props/poly_haven_metal_trash_can.tscn", new Vector3(-26, 0, 11), 0);
        AddSignLabel("ParkOfficeName", "CEDAR VIEW MOBILE HOME PARK", new Vector3(0, 4.15f, 5.68f), 46, new Color(0.92f, 0.77f, 0.43f));
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
        AddBox(name + "Foundation", basePosition + new Vector3(0, 0.22f, 0), new Vector3(size.X + 0.45f, 0.44f, size.Z + 0.45f), "stone");
        if (pitched)
        {
            AddBox(name + "RoofA", basePosition + new Vector3(0, size.Y + 1.0f, -size.Z * 0.23f), new Vector3(size.X + 1.2f, 0.5f, size.Z * 0.58f), roof, false, new Vector3(-0.35f, 0, 0));
            AddBox(name + "RoofB", basePosition + new Vector3(0, size.Y + 1.0f, size.Z * 0.23f), new Vector3(size.X + 1.2f, 0.5f, size.Z * 0.58f), roof, false, new Vector3(0.35f, 0, 0));
            AddBox(name + "FrontFascia", basePosition + new Vector3(0, size.Y + 0.35f, size.Z * 0.5f + 0.18f), new Vector3(size.X + 0.8f, 0.35f, 0.22f), "dark_trim", false);
            AddBox(name + "BackFascia", basePosition + new Vector3(0, size.Y + 0.35f, -size.Z * 0.5f - 0.18f), new Vector3(size.X + 0.8f, 0.35f, 0.22f), "dark_trim", false);
        }
        else
        {
            AddBox(name + "Roof", basePosition + new Vector3(0, size.Y + 0.18f, 0), new Vector3(size.X + 0.7f, 0.36f, size.Z + 0.7f), roof, false);
            AddBox(name + "FrontParapet", basePosition + new Vector3(0, size.Y + 0.7f, size.Z * 0.5f), new Vector3(size.X + 0.8f, 1.1f, 0.35f), walls, false);
        }

        DressFacade(name, basePosition, size);
    }

    private void DressFacade(string name, Vector3 basePosition, Vector3 size)
    {
        float frontZ = basePosition.Z + size.Z * 0.5f + 0.07f;
        float doorX = basePosition.X - Mathf.Min(size.X * 0.28f, 5.5f);
        AddBox(name + "Door", new Vector3(doorX, basePosition.Y + 1.35f, frontZ), new Vector3(1.55f, 2.7f, 0.16f), "door", false);
        AddBox(name + "DoorHead", new Vector3(doorX, basePosition.Y + 2.9f, frontZ + 0.02f), new Vector3(2.05f, 0.22f, 0.22f), "trim", false);

        int windows = Mathf.Clamp(Mathf.FloorToInt(size.X / 6.5f), 2, 7);
        float spacing = size.X / (windows + 1);
        for (int i = 1; i <= windows; i++)
        {
            float x = basePosition.X - size.X * 0.5f + spacing * i;
            if (Mathf.Abs(x - doorX) < 1.8f) continue;
            AddWindow($"{name}Window{i}", new Vector3(x, basePosition.Y + Mathf.Min(2.6f, size.Y * 0.5f), frontZ), new Vector2(Mathf.Min(2.2f, spacing * 0.52f), 1.65f));
        }

        AddBox(name + "SillBand", new Vector3(basePosition.X, basePosition.Y + 1.48f, frontZ - 0.01f), new Vector3(size.X, 0.16f, 0.15f), "trim", false);
    }

    private void AddWindow(string name, Vector3 position, Vector2 size)
    {
        AddBox(name + "Glass", position, new Vector3(size.X, size.Y, 0.12f), "glass", false);
        const float frame = 0.11f;
        AddBox(name + "Top", position + new Vector3(0, size.Y * 0.5f, 0.04f), new Vector3(size.X + frame * 2, frame, 0.18f), "trim", false);
        AddBox(name + "Bottom", position - new Vector3(0, size.Y * 0.5f, -0.04f), new Vector3(size.X + frame * 2, frame, 0.18f), "trim", false);
        AddBox(name + "Left", position - new Vector3(size.X * 0.5f, 0, -0.04f), new Vector3(frame, size.Y, 0.18f), "trim", false);
        AddBox(name + "Right", position + new Vector3(size.X * 0.5f, 0, 0.04f), new Vector3(frame, size.Y, 0.18f), "trim", false);
        AddBox(name + "Mullion", position + new Vector3(0, 0, 0.05f), new Vector3(frame * 0.7f, size.Y, 0.18f), "dark_trim", false);
    }

    private void AddSignLabel(string name, string text, Vector3 position, int fontSize, Color color)
    {
        var label = new Label3D
        {
            Name = name,
            Text = text,
            Position = position,
            FontSize = fontSize,
            PixelSize = 0.012f,
            OutlineSize = 9,
            Modulate = color,
            OutlineModulate = new Color(0.045f, 0.035f, 0.025f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VisibilityRangeEnd = 320.0f,
        };
        AddChild(label);
    }

    private void AddGroundPatch(string name, Vector3 position, Vector3 size, string material)
    {
        AddBox(name, position, size, material, false);
    }

    private void AddAsset(string name, string path, Vector3 position, float yawDegrees, Vector3? scale = null)
    {
        if (!_packedScenes.TryGetValue(path, out PackedScene? packed))
        {
            packed = ResourceLoader.Load<PackedScene>(path);
            if (packed is null)
            {
                GD.PushWarning($"{LocationName}: missing dressing asset {path}");
                return;
            }
            _packedScenes[path] = packed;
        }

        Node3D instance = packed.Instantiate<Node3D>();
        instance.Name = name;
        instance.Position = position;
        instance.RotationDegrees = new Vector3(0, yawDegrees, 0);
        instance.Scale = scale ?? Vector3.One;
        AddChild(instance);
    }

    private void AddFenceLine(string name, Vector3 start, Vector3 end, float segmentLength, string material)
    {
        Vector3 delta = end - start;
        float length = delta.Length();
        if (length < 0.1f) return;
        Vector3 direction = delta / length;
        int segments = Mathf.Max(1, Mathf.CeilToInt(length / segmentLength));
        float yaw = Mathf.Atan2(direction.X, direction.Z);
        for (int i = 0; i <= segments; i++)
        {
            Vector3 point = start.Lerp(end, i / (float)segments);
            AddBox($"{name}Post{i}", point + Vector3.Up * 1.05f, new Vector3(0.16f, 2.1f, 0.16f), material, false);
            if (i == segments) continue;
            Vector3 next = start.Lerp(end, (i + 1) / (float)segments);
            Vector3 middle = (point + next) * 0.5f;
            float railLength = point.DistanceTo(next);
            AddBox($"{name}RailTop{i}", middle + Vector3.Up * 1.45f, new Vector3(0.12f, 0.14f, railLength), material, false, new Vector3(0, yaw, 0));
            AddBox($"{name}RailLow{i}", middle + Vector3.Up * 0.7f, new Vector3(0.12f, 0.14f, railLength), material, false, new Vector3(0, yaw, 0));
        }
    }

    private void AddRailingLine(string name, Vector3 start, Vector3 end, float spacing, string material)
    {
        Vector3 delta = end - start;
        float length = delta.Length();
        int segments = Mathf.Max(1, Mathf.CeilToInt(length / spacing));
        Vector3 direction = delta.Normalized();
        float yaw = Mathf.Atan2(direction.X, direction.Z);
        AddBox(name + "TopRail", (start + end) * 0.5f + Vector3.Up * 1.1f, new Vector3(0.12f, 0.12f, length), material, false, new Vector3(0, yaw, 0));
        for (int i = 0; i <= segments; i++)
        {
            Vector3 point = start.Lerp(end, i / (float)segments);
            AddBox($"{name}Post{i}", point + Vector3.Up * 0.55f, new Vector3(0.12f, 1.1f, 0.12f), material, false);
        }
    }

    private void AddRailingRect(string name, Vector3 center, float width, float depth, string material)
    {
        Vector3 nw = center + new Vector3(-width * 0.5f, 0, -depth * 0.5f);
        Vector3 ne = center + new Vector3(width * 0.5f, 0, -depth * 0.5f);
        Vector3 sw = center + new Vector3(-width * 0.5f, 0, depth * 0.5f);
        Vector3 se = center + new Vector3(width * 0.5f, 0, depth * 0.5f);
        AddRailingLine(name + "North", nw, ne, 3, material);
        AddRailingLine(name + "South", sw, se, 3, material);
        AddRailingLine(name + "West", nw, sw, 3, material);
        AddRailingLine(name + "East", ne, se, 3, material);
    }

    private void AddShrubs(string name, Vector3 center, int count, float spacing)
    {
        string[] shrubs =
        {
            "res://assets/environment/nature/polyhaven/ashwood_shrub_02_a.tscn",
            "res://assets/environment/nature/polyhaven/ashwood_shrub_02_c.tscn",
            "res://assets/environment/nature/polyhaven/ashwood_shrub_03_b.tscn",
        };
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * spacing;
            AddAsset($"{name}{i}", shrubs[i % shrubs.Length], center + new Vector3(offset, 0, (i % 2) * 0.4f), i * 37, Vector3.One * (0.75f + (i % 3) * 0.12f));
        }
    }

    private void AddForestScatter(string name, int count, float radiusX, float radiusZ)
    {
        string[] trees =
        {
            "res://assets/environment/nature/polyhaven/ashwood_pine_a_lod1.tscn",
            "res://assets/environment/nature/polyhaven/ashwood_pine_b_lod1.tscn",
            "res://assets/environment/nature/polyhaven/ashwood_fir_c_lod1.tscn",
        };
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count + Mathf.Sin(i * 2.17f) * 0.13f;
            float ring = 0.78f + (i % 5) * 0.055f;
            Vector3 position = new(Mathf.Cos(angle) * radiusX * ring, 0, Mathf.Sin(angle) * radiusZ * ring);
            float scale = 0.76f + (i % 7) * 0.055f;
            AddAsset($"{name}{i}", trees[i % trees.Length], position, i * 53 % 360, Vector3.One * scale);
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
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 18 },
            MaterialOverride = MaterialFor(material),
            Position = position,
            Rotation = rotation ?? Vector3.Zero,
            VisibilityRangeEnd = 1800.0f,
        };
        AddChild(mesh);
    }

    private void AddPipeBetween(string name, Vector3 start, Vector3 end, float radius, string material)
    {
        Vector3 direction = end - start;
        float length = direction.Length();
        if (length < 0.01f) return;
        var mesh = new MeshInstance3D
        {
            Name = name,
            Mesh = new CylinderMesh
            {
                TopRadius = radius,
                BottomRadius = radius,
                Height = length,
                RadialSegments = 20,
            },
            MaterialOverride = MaterialFor(material),
            Position = (start + end) * 0.5f,
            Basis = new Basis(new Quaternion(Vector3.Up, direction.Normalized())),
            VisibilityRangeEnd = 1800.0f,
        };
        AddChild(mesh);
    }

    private StandardMaterial3D MaterialFor(string name)
    {
        if (_materials.TryGetValue(name, out StandardMaterial3D? material)) return material;
        if (MaterialPaths.TryGetValue(name, out string? path) && ResourceLoader.Load<StandardMaterial3D>(path) is StandardMaterial3D textured)
        {
            _materials[name] = textured;
            return textured;
        }

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
