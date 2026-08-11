#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// The authoritative definition of Ashwood County as a place.
///
/// Everything else in the open world - terrain meshing, roads, water, vegetation
/// scatter, settlement placement, navigation - is derived from the pure functions
/// on this class. Nothing here touches the scene tree or allocates per call, so a
/// mesher can evaluate <see cref="Height"/> a few million times per chunk build and
/// a scatter pass can reject candidate points cheaply.
///
/// Coordinate frame (Godot: +X east, -Z north, +Y up):
///   - The origin is the middle of Ashwood's Main Street, so the existing town
///     slice and the Old Mill Bridge keep the coordinates they were authored at.
///   - Y is real county elevation minus <see cref="TownElevation"/>, which puts the
///     town at Y=0, the southern river mouth near Y=-110 and Fire Lookout near
///     Y=+950. Those come from the county planning map's 320m-1420m range.
///
/// The county sits on a plateau that falls away into cliffs at its rim, exactly as
/// the aerial concept shows. That is a deliberate design choice: it is the world
/// boundary, and it is a diegetic one, so the player meets a landscape rather than
/// an invisible wall.
/// </summary>
public static class CountyMap
{
    // ---------------------------------------------------------------- extents

    /// <summary>Real-world elevation of Ashwood town, in metres. Y=0 in game.</summary>
    public const float TownElevation = 430.0f;

    public const float WestX = -4400.0f;
    public const float EastX = 3800.0f;
    public const float NorthZ = -4200.0f;
    public const float SouthZ = 4000.0f;

    public const float CenterX = (WestX + EastX) * 0.5f;
    public const float CenterZ = (NorthZ + SouthZ) * 0.5f;
    public const float SpanX = EastX - WestX;
    public const float SpanZ = SouthZ - NorthZ;

    /// <summary>Everything below this drowns; it is the plateau's surrounding void.</summary>
    public const float AbyssY = -320.0f;

    /// <summary>Sea level for the river system south of the dam.</summary>
    public const float RiverMouthY = -104.0f;

    // ------------------------------------------------------------ water bodies

    /// <summary>Surface height of Blackwater Lake (the reservoir above the dam).</summary>
    public const float LakeSurfaceY = 214.0f;

    public static readonly Vector2 LakeCenter = new(-140.0f, -2400.0f);
    public const float LakeRadiusX = 620.0f;
    public const float LakeRadiusZ = 430.0f;

    /// <summary>Where the dam wall sits across the outflow throat.</summary>
    public static readonly Vector2 DamCenter = new(-40.0f, -1975.0f);
    public const float DamCrestY = LakeSurfaceY + 6.0f;
    public const float DamHalfWidth = 190.0f;

    // ----------------------------------------------------------- river network

    /// <summary>
    /// The Blackwater, north to south. The point at Z=0 is pinned to X=-176 so the
    /// channel arrives exactly under the Old Mill Bridge that is already built.
    /// </summary>
    public static readonly Vector2[] RiverSpine =
    {
        new(-250.0f, -3420.0f),
        new(-205.0f, -3130.0f),
        new(-168.0f, -2880.0f),
        new(-148.0f, -2660.0f),
        new(-140.0f, -2400.0f),
        new(-72.0f, -2110.0f),
        new(-40.0f, -1975.0f),
        new(-88.0f, -1700.0f),
        new(-58.0f, -1400.0f),
        new(-124.0f, -1090.0f),
        new(-102.0f, -790.0f),
        new(-152.0f, -498.0f),
        new(-176.0f, -210.0f),
        new(-176.0f, 0.0f),
        new(-178.0f, 210.0f),
        new(-212.0f, 505.0f),
        new(-298.0f, 852.0f),
        new(-422.0f, 1198.0f),
        new(-562.0f, 1548.0f),
        new(-702.0f, 1902.0f),
        new(-824.0f, 2252.0f),
        new(-902.0f, 2604.0f),
        new(-978.0f, 3002.0f),
        new(-1048.0f, 3402.0f),
        new(-1124.0f, 4060.0f),
    };

    /// <summary>Mill Creek's namesake tributary, joining the Blackwater from the west.</summary>
    public static readonly Vector2[] MillCreekSpine =
    {
        new(-2740.0f, 980.0f),
        new(-2480.0f, 1120.0f),
        new(-2210.0f, 1310.0f),
        new(-1940.0f, 1490.0f),
        new(-1640.0f, 1642.0f),
        new(-1330.0f, 1760.0f),
        new(-1020.0f, 1868.0f),
        new(-806.0f, 2126.0f),
    };

    // --------------------------------------------------------- points of interest

    public enum PoiKind
    {
        Town,
        Settlement,
        Farm,
        Industrial,
        Landmark,
        Infrastructure,
    }

    public readonly record struct Poi(
        string Name,
        Vector2 Position,
        PoiKind Kind,
        float Radius,
        /// <summary>How hard the terrain is levelled under the site, 0..1.</summary>
        float Flatten);

    /// <summary>
    /// Every named location on the county planning map, positioned to match the
    /// aerial concept. Terrain flattening, settlement dressing, navigation islands
    /// and the map screen all read from this one list.
    /// </summary>
    public static readonly Poi[] Places =
    {
        new("Ashwood", new Vector2(0.0f, 0.0f), PoiKind.Town, 640.0f, 1.0f),
        new("Sheriff's Office", new Vector2(268.0f, 74.0f), PoiKind.Infrastructure, 70.0f, 1.0f),
        new("Hospital", new Vector2(96.0f, 540.0f), PoiKind.Infrastructure, 140.0f, 0.95f),
        new("Service Station", new Vector2(30.0f, 742.0f), PoiKind.Infrastructure, 96.0f, 0.95f),
        new("Old Mill Bridge", new Vector2(-176.0f, 0.0f), PoiKind.Landmark, 150.0f, 0.0f),
        new("Blackwater Dam", DamCenter, PoiKind.Infrastructure, 240.0f, 0.0f),
        new("Pine Ridge", new Vector2(-262.0f, -3402.0f), PoiKind.Settlement, 330.0f, 0.82f),
        new("Fire Lookout", new Vector2(958.0f, -3048.0f), PoiKind.Landmark, 90.0f, 0.55f),
        new("Logging Camp", new Vector2(-1902.0f, -2448.0f), PoiKind.Industrial, 300.0f, 0.9f),
        new("Farm District", new Vector2(-1810.0f, -286.0f), PoiKind.Farm, 720.0f, 0.85f),
        new("Mill Creek", new Vector2(-2104.0f, 1702.0f), PoiKind.Settlement, 420.0f, 0.9f),
        new("Railway Crossing", new Vector2(-902.0f, 2210.0f), PoiKind.Infrastructure, 120.0f, 0.7f),
        new("County Fairgrounds", new Vector2(-96.0f, 2356.0f), PoiKind.Settlement, 330.0f, 0.95f),
        new("Trailer Park", new Vector2(1504.0f, 1598.0f), PoiKind.Settlement, 300.0f, 0.92f),
        new("South Farmland", new Vector2(-206.0f, 3204.0f), PoiKind.Farm, 900.0f, 0.85f),
    };

    public enum NaturalFeatureKind
    {
        Cave,
        Grotto,
        RockFormation,
        Overlook,
        Escarpment,
        OldGrowth,
    }

    public readonly record struct NaturalFeature(
        string Name,
        Vector2 Position,
        NaturalFeatureKind Kind,
        float Radius,
        float YawDegrees);

    /// <summary>
    /// Authored wilderness destinations between the mapped settlements. They are
    /// intentionally separate from POIs: these do not flatten terrain or clear a
    /// construction pad and are meant to feel discovered rather than signposted.
    /// </summary>
    public static readonly NaturalFeature[] NaturalFeatures =
    {
        new("Blackwater Cavern", new Vector2(2380.0f, -1310.0f), NaturalFeatureKind.Cave, 78.0f, 0.0f),
        new("Mill Creek Grotto", new Vector2(-2660.0f, 1020.0f), NaturalFeatureKind.Grotto, 64.0f, 62.0f),
        new("Granite Narrows", new Vector2(2390.0f, -2310.0f), NaturalFeatureKind.RockFormation, 110.0f, 46.0f),
        new("Pine Ridge Overlook", new Vector2(-1330.0f, -3600.0f), NaturalFeatureKind.Overlook, 92.0f, -98.0f),
        new("South Ridge Escarpment", new Vector2(1080.0f, 2890.0f), NaturalFeatureKind.Escarpment, 130.0f, -8.0f),
        new("Old Growth Hollow", new Vector2(2700.0f, -270.0f), NaturalFeatureKind.OldGrowth, 125.0f, -81.0f),
    };

    public readonly record struct Trail(string Name, Vector2[] Points);

    /// <summary>
    /// Walkable spurs from the maintained road network to every wilderness
    /// destination. These are authored routes, not straight lines between map
    /// icons: intermediate points keep them on shoulders and contour benches so
    /// the player gets a readable trailhead, a reveal sequence, and a practical
    /// return route.
    /// </summary>
    public static readonly Trail[] Trails =
    {
        new("Blackwater Cavern Trail", new[]
        {
            new Vector2(2100.0f, -1180.0f), new Vector2(2180.0f, -1215.0f),
            new Vector2(2270.0f, -1250.0f), new Vector2(2340.0f, -1280.0f),
            new Vector2(2380.0f, -1310.0f),
        }),
        new("Mill Creek Grotto Trail", new[]
        {
            new Vector2(-2740.0f, 1180.0f), new Vector2(-2750.0f, 1120.0f),
            new Vector2(-2720.0f, 1080.0f), new Vector2(-2685.0f, 1045.0f),
        }),
        new("Granite Narrows Trail", new[]
        {
            new Vector2(1080.0f, -2540.0f), new Vector2(1280.0f, -2480.0f),
            new Vector2(1560.0f, -2440.0f), new Vector2(1850.0f, -2400.0f),
            new Vector2(2140.0f, -2350.0f), new Vector2(2390.0f, -2310.0f),
        }),
        new("Pine Ridge Overlook Trail", new[]
        {
            new Vector2(-660.0f, -3260.0f), new Vector2(-830.0f, -3360.0f),
            new Vector2(-1010.0f, -3460.0f), new Vector2(-1180.0f, -3540.0f),
            new Vector2(-1330.0f, -3600.0f),
        }),
        new("South Ridge Escarpment Trail", new[]
        {
            new Vector2(1280.0f, 2880.0f), new Vector2(1205.0f, 2860.0f),
            new Vector2(1135.0f, 2875.0f), new Vector2(1080.0f, 2890.0f),
        }),
        new("Old Growth Hollow Trail", new[]
        {
            new Vector2(2940.0f, -300.0f), new Vector2(2865.0f, -285.0f),
            new Vector2(2785.0f, -278.0f), new Vector2(2700.0f, -270.0f),
        }),
    };

    // --------------------------------------------------------------- regions

    public enum RegionId
    {
        PineRidge,
        BlackwaterBasin,
        WesternFarms,
        Ashwood,
        EasternWoodlands,
        MillCreek,
        FairgroundsAndTrailerPark,
        SouthFarmland,
    }

    /// <summary>
    /// Stable authoring regions for building the county in passes. Scale is the
    /// approximate half-size of each region, not a hard gameplay boundary; the
    /// normalised nearest-anchor lookup below makes the regions meet without gaps.
    /// </summary>
    public readonly record struct Region(
        RegionId Id,
        string Name,
        Vector2 Center,
        Vector2 Scale,
        string PrimaryConnection);

    public static readonly Region[] Regions =
    {
        new(RegionId.PineRidge, "Pine Ridge Highlands",
            new Vector2(-360.0f, -3400.0f), new Vector2(2400.0f, 1050.0f), "County Road North"),
        new(RegionId.BlackwaterBasin, "Blackwater Lake and Dam",
            new Vector2(-120.0f, -2250.0f), new Vector2(1450.0f, 950.0f), "County Road North"),
        new(RegionId.WesternFarms, "Western Farm District",
            new Vector2(-1900.0f, -450.0f), new Vector2(1750.0f, 1450.0f), "Mill Road"),
        new(RegionId.Ashwood, "Ashwood and Old Mill Bridge",
            new Vector2(180.0f, 180.0f), new Vector2(1250.0f, 1200.0f), "State Highway 16"),
        new(RegionId.EasternWoodlands, "Eastern Woodlands",
            new Vector2(2200.0f, -900.0f), new Vector2(1750.0f, 2100.0f), "East County Road"),
        new(RegionId.MillCreek, "Mill Creek and Railway",
            new Vector2(-1900.0f, 1850.0f), new Vector2(1550.0f, 1300.0f), "Mill Creek Road"),
        new(RegionId.FairgroundsAndTrailerPark, "Fairgrounds and Trailer Park",
            new Vector2(700.0f, 1900.0f), new Vector2(1750.0f, 1250.0f), "Fairgrounds Road"),
        new(RegionId.SouthFarmland, "South Farmland",
            new Vector2(-100.0f, 3300.0f), new Vector2(2500.0f, 900.0f), "Southern Ridge Road"),
    };

    public static Region RegionAt(float x, float z)
    {
        var point = new Vector2(x, z);
        Region nearest = Regions[0];
        float nearestScore = float.MaxValue;

        foreach (Region region in Regions)
        {
            Vector2 offset = point - region.Center;
            float score = offset.X * offset.X / (region.Scale.X * region.Scale.X) +
                          offset.Y * offset.Y / (region.Scale.Y * region.Scale.Y);
            if (score < nearestScore)
            {
                nearest = region;
                nearestScore = score;
            }
        }

        return nearest;
    }

    // ------------------------------------------------------------ road network

    public enum RoadClass
    {
        /// <summary>State Highway 16 - the only real way in or out.</summary>
        Highway,
        Paved,
        Gravel,
        Dirt,
        Railway,
    }

    public readonly record struct Road(string Name, RoadClass Class, Vector2[] Points);

    public static float RoadHalfWidth(RoadClass roadClass) => roadClass switch
    {
        RoadClass.Highway => 7.4f,
        RoadClass.Paved => 5.2f,
        RoadClass.Gravel => 3.6f,
        RoadClass.Dirt => 2.6f,
        RoadClass.Railway => 3.1f,
        _ => 4.0f,
    };

    /// <summary>How far past the carriageway the graded shoulder reaches.</summary>
    public static float RoadShoulder(RoadClass roadClass) => roadClass switch
    {
        RoadClass.Highway => 9.0f,
        RoadClass.Paved => 6.5f,
        RoadClass.Gravel => 5.0f,
        RoadClass.Dirt => 4.0f,
        RoadClass.Railway => 7.0f,
        _ => 5.0f,
    };

    public static readonly Road[] Roads =
    {
        new("State Highway 16", RoadClass.Highway, new[]
        {
            new Vector2(-4460.0f, 902.0f),
            new Vector2(-3900.0f, 858.0f),
            new Vector2(-3320.0f, 806.0f),
            new Vector2(-2740.0f, 792.0f),
            new Vector2(-2160.0f, 812.0f),
            new Vector2(-1600.0f, 806.0f),
            new Vector2(-1080.0f, 774.0f),
            new Vector2(-640.0f, 742.0f),
            new Vector2(-300.0f, 726.0f),
            new Vector2(30.0f, 742.0f),
            new Vector2(420.0f, 706.0f),
            new Vector2(860.0f, 610.0f),
            new Vector2(1320.0f, 452.0f),
            new Vector2(1820.0f, 288.0f),
            new Vector2(2360.0f, 142.0f),
            new Vector2(2940.0f, 32.0f),
            new Vector2(3860.0f, -46.0f),
        }),
        new("Main Street", RoadClass.Paved, new[]
        {
            new Vector2(-330.0f, 0.0f),
            new Vector2(-176.0f, 0.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(268.0f, 6.0f),
            new Vector2(470.0f, 22.0f),
        }),
        new("Mill Road", RoadClass.Paved, new[]
        {
            new Vector2(-330.0f, 0.0f),
            new Vector2(-620.0f, -32.0f),
            new Vector2(-960.0f, -104.0f),
            new Vector2(-1330.0f, -196.0f),
            new Vector2(-1690.0f, -262.0f),
            new Vector2(-1810.0f, -286.0f),
            new Vector2(-2140.0f, -330.0f),
            new Vector2(-2520.0f, -300.0f),
        }),
        new("County Road North", RoadClass.Paved, new[]
        {
            new Vector2(60.0f, 40.0f),
            new Vector2(96.0f, -320.0f),
            new Vector2(64.0f, -720.0f),
            new Vector2(120.0f, -1120.0f),
            new Vector2(96.0f, -1520.0f),
            new Vector2(-340.0f, -1640.0f),
            new Vector2(-650.0f, -1810.0f),
            new Vector2(-760.0f, -1950.0f),
            new Vector2(-930.0f, -2160.0f),
            new Vector2(-960.0f, -2500.0f),
            new Vector2(-800.0f, -2800.0f),
            new Vector2(-430.0f, -3070.0f),
            new Vector2(-262.0f, -3402.0f),
        }),
        new("Fire Lookout Road", RoadClass.Dirt, new[]
        {
            new Vector2(220.0f, -2910.0f),
            new Vector2(80.0f, -3150.0f),
            new Vector2(180.0f, -3450.0f),
            new Vector2(500.0f, -3600.0f),
            new Vector2(850.0f, -3500.0f),
            new Vector2(1120.0f, -3300.0f),
            new Vector2(1120.0f, -3050.0f),
            new Vector2(958.0f, -3048.0f),
        }),
        new("Logging Road", RoadClass.Gravel, new[]
        {
            new Vector2(-960.0f, -2500.0f),
            new Vector2(-1020.0f, -2500.0f),
            new Vector2(-1280.0f, -2480.0f),
            new Vector2(-1520.0f, -2470.0f),
            new Vector2(-1902.0f, -2448.0f),
            new Vector2(-2280.0f, -2360.0f),
        }),
        new("Farm District Loop", RoadClass.Gravel, new[]
        {
            new Vector2(-1810.0f, -286.0f),
            new Vector2(-2060.0f, -620.0f),
            new Vector2(-2180.0f, -1020.0f),
            new Vector2(-2020.0f, -1420.0f),
            new Vector2(-1700.0f, -1560.0f),
            new Vector2(-1380.0f, -1380.0f),
            new Vector2(-1290.0f, -980.0f),
            new Vector2(-1420.0f, -600.0f),
            new Vector2(-1690.0f, -262.0f),
        }),
        new("Mill Creek Road", RoadClass.Paved, new[]
        {
            new Vector2(-1600.0f, 806.0f),
            new Vector2(-1740.0f, 1050.0f),
            new Vector2(-1880.0f, 1310.0f),
            new Vector2(-2020.0f, 1540.0f),
            new Vector2(-2104.0f, 1702.0f),
            new Vector2(-2200.0f, 1980.0f),
            new Vector2(-2260.0f, 2320.0f),
        }),
        new("Fairgrounds Road", RoadClass.Paved, new[]
        {
            new Vector2(-300.0f, 726.0f),
            new Vector2(-320.0f, 1060.0f),
            new Vector2(-280.0f, 1420.0f),
            new Vector2(-220.0f, 1780.0f),
            new Vector2(-150.0f, 2090.0f),
            new Vector2(-96.0f, 2356.0f),
            new Vector2(-140.0f, 2700.0f),
            new Vector2(-206.0f, 3204.0f),
            new Vector2(-240.0f, 3620.0f),
        }),
        new("Trailer Park Road", RoadClass.Gravel, new[]
        {
            new Vector2(1320.0f, 452.0f),
            new Vector2(1420.0f, 780.0f),
            new Vector2(1470.0f, 1120.0f),
            new Vector2(1504.0f, 1598.0f),
            new Vector2(1560.0f, 1960.0f),
        }),
        new("South Farm Track", RoadClass.Dirt, new[]
        {
            new Vector2(-206.0f, 3204.0f),
            new Vector2(180.0f, 3120.0f),
            new Vector2(560.0f, 3180.0f),
            new Vector2(940.0f, 3060.0f),
            new Vector2(1280.0f, 2880.0f),
            new Vector2(1560.0f, 1960.0f),
        }),
        new("East County Road", RoadClass.Paved, new[]
        {
            new Vector2(268.0f, 74.0f),
            new Vector2(700.0f, -120.0f),
            new Vector2(1080.0f, -380.0f),
            new Vector2(1420.0f, -700.0f),
            new Vector2(1660.0f, -1080.0f),
            new Vector2(1780.0f, -1500.0f),
            new Vector2(1700.0f, -1940.0f),
            new Vector2(1420.0f, -2280.0f),
            new Vector2(1080.0f, -2540.0f),
            new Vector2(720.0f, -2760.0f),
            new Vector2(220.0f, -2910.0f),
        }),
        new("Eastern Forest Track", RoadClass.Dirt, new[]
        {
            new Vector2(1660.0f, -1080.0f),
            new Vector2(2100.0f, -1180.0f),
            new Vector2(2520.0f, -1020.0f),
            new Vector2(2820.0f, -700.0f),
            new Vector2(2940.0f, -300.0f),
            new Vector2(2940.0f, 32.0f),
        }),
        new("Ridge Track", RoadClass.Dirt, new[]
        {
            new Vector2(-262.0f, -3402.0f),
            new Vector2(-660.0f, -3260.0f),
            new Vector2(-1080.0f, -3060.0f),
            new Vector2(-1480.0f, -2820.0f),
            new Vector2(-1902.0f, -2448.0f),
        }),
        new("Dam Service Road", RoadClass.Gravel, new[]
        {
            new Vector2(-400.0f, -1880.0f),
            new Vector2(-210.0f, -1870.0f),
            new Vector2(-40.0f, -1975.0f),
            new Vector2(240.0f, -2060.0f),
            new Vector2(480.0f, -2280.0f),
            new Vector2(610.0f, -2540.0f),
            new Vector2(520.0f, -2780.0f),
            new Vector2(220.0f, -2910.0f),
        }),
        new("West Farm Lane", RoadClass.Dirt, new[]
        {
            new Vector2(-2180.0f, -1020.0f),
            new Vector2(-2560.0f, -940.0f),
            new Vector2(-2940.0f, -700.0f),
            new Vector2(-3200.0f, -360.0f),
            new Vector2(-3260.0f, 60.0f),
            new Vector2(-3080.0f, 460.0f),
            new Vector2(-2740.0f, 792.0f),
        }),
        new("Creek Lane", RoadClass.Dirt, new[]
        {
            new Vector2(-2104.0f, 1702.0f),
            new Vector2(-2480.0f, 1480.0f),
            new Vector2(-2740.0f, 1180.0f),
            new Vector2(-2860.0f, 820.0f),
            new Vector2(-3080.0f, 460.0f),
        }),
        new("Southern Ridge Road", RoadClass.Gravel, new[]
        {
            new Vector2(-2260.0f, 2320.0f),
            new Vector2(-2020.0f, 2680.0f),
            new Vector2(-1660.0f, 2980.0f),
            new Vector2(-1240.0f, 3220.0f),
            new Vector2(-780.0f, 3400.0f),
            new Vector2(-240.0f, 3620.0f),
            new Vector2(320.0f, 3560.0f),
            new Vector2(880.0f, 3340.0f),
        }),
        new("Hospital Approach", RoadClass.Paved, new[]
        {
            new Vector2(96.0f, 540.0f),
            new Vector2(160.0f, 300.0f),
            new Vector2(140.0f, 60.0f),
            new Vector2(60.0f, 40.0f),
        }),
        new("Fairground Back Lane", RoadClass.Dirt, new[]
        {
            new Vector2(-96.0f, 2356.0f),
            new Vector2(340.0f, 2280.0f),
            new Vector2(760.0f, 2160.0f),
            new Vector2(1180.0f, 2020.0f),
            new Vector2(1504.0f, 1598.0f),
        }),
        // Ashwood's street grid is a navigation structure, not town dressing. It
        // exists now so later building plots inherit real blocks and intersections.
        new("Ashwood Oak Avenue", RoadClass.Paved, new[]
        {
            new Vector2(-18.0f, -510.0f),
            new Vector2(-8.0f, -250.0f),
            new Vector2(0.0f, 0.0f),
            new Vector2(24.0f, 300.0f),
            new Vector2(30.0f, 742.0f),
        }),
        new("Ashwood Cedar Avenue", RoadClass.Paved, new[]
        {
            new Vector2(190.0f, -470.0f),
            new Vector2(184.0f, -220.0f),
            new Vector2(176.0f, 0.0f),
            new Vector2(168.0f, 260.0f),
            new Vector2(180.0f, 590.0f),
        }),
        new("Ashwood Franklin Avenue", RoadClass.Paved, new[]
        {
            new Vector2(390.0f, -390.0f),
            new Vector2(382.0f, -210.0f),
            new Vector2(372.0f, 14.0f),
            new Vector2(360.0f, 260.0f),
            new Vector2(410.0f, 520.0f),
        }),
        new("Ashwood North Street", RoadClass.Paved, new[]
        {
            new Vector2(-70.0f, -250.0f),
            new Vector2(190.0f, -220.0f),
            new Vector2(382.0f, -210.0f),
            new Vector2(620.0f, -165.0f),
        }),
        new("Ashwood Market Street", RoadClass.Paved, new[]
        {
            new Vector2(-78.0f, 245.0f),
            new Vector2(168.0f, 260.0f),
            new Vector2(360.0f, 260.0f),
            new Vector2(610.0f, 220.0f),
        }),
        new("Ashwood Hospital Street", RoadClass.Paved, new[]
        {
            new Vector2(-20.0f, 520.0f),
            new Vector2(96.0f, 540.0f),
            new Vector2(180.0f, 590.0f),
            new Vector2(410.0f, 520.0f),
            new Vector2(690.0f, 455.0f),
        }),
        new("Mill Creek Main Street", RoadClass.Paved, new[]
        {
            new Vector2(-2390.0f, 1560.0f),
            new Vector2(-2200.0f, 1630.0f),
            new Vector2(-2104.0f, 1702.0f),
            new Vector2(-1910.0f, 1790.0f),
            new Vector2(-1710.0f, 1870.0f),
        }),
        new("Mill Creek Depot Road", RoadClass.Gravel, new[]
        {
            new Vector2(-2200.0f, 1630.0f),
            new Vector2(-2020.0f, 1890.0f),
            new Vector2(-1710.0f, 2060.0f),
            new Vector2(-1180.0f, 2470.0f),
        }),
        new("Railway", RoadClass.Railway, new[]
        {
            new Vector2(-2700.0f, 3560.0f),
            new Vector2(-2280.0f, 3300.0f),
            new Vector2(-1880.0f, 3020.0f),
            new Vector2(-1500.0f, 2740.0f),
            new Vector2(-1180.0f, 2470.0f),
            new Vector2(-902.0f, 2210.0f),
            new Vector2(-620.0f, 1940.0f),
            new Vector2(-300.0f, 1660.0f),
            new Vector2(60.0f, 1400.0f),
            new Vector2(520.0f, 1160.0f),
            new Vector2(1060.0f, 980.0f),
            new Vector2(1700.0f, 806.0f),
            new Vector2(2400.0f, 620.0f),
            new Vector2(3200.0f, 402.0f),
        }),
    };

    // ------------------------------------------------------------------- noise

    private static float Hash(int x, int y)
    {
        unchecked
        {
            int n = x * 374761393 + y * 668265263;
            n = (n ^ (n >> 13)) * 1274126177;
            n ^= n >> 16;
            return (n & 0x7FFFFFF) / (float)0x7FFFFFF;
        }
    }

    private static float ValueNoise(float x, float y)
    {
        int xi = Mathf.FloorToInt(x);
        int yi = Mathf.FloorToInt(y);
        float xf = x - xi;
        float yf = y - yi;

        // Quintic fade: continuous second derivative, so lighting on the resulting
        // surface has no visible cell seams.
        float u = xf * xf * xf * (xf * (xf * 6.0f - 15.0f) + 10.0f);
        float v = yf * yf * yf * (yf * (yf * 6.0f - 15.0f) + 10.0f);

        float a = Hash(xi, yi);
        float b = Hash(xi + 1, yi);
        float c = Hash(xi, yi + 1);
        float d = Hash(xi + 1, yi + 1);

        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    /// <summary>Fractal Brownian motion in 0..1.</summary>
    public static float Fbm(float x, float y, int octaves, float lacunarity = 2.03f, float gain = 0.5f)
    {
        float sum = 0.0f;
        float amplitude = 1.0f;
        float total = 0.0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += ValueNoise(x, y) * amplitude;
            total += amplitude;
            x *= lacunarity;
            y *= lacunarity;
            amplitude *= gain;
        }
        return sum / Mathf.Max(total, 0.0001f);
    }

    /// <summary>
    /// Ridged multifractal in 0..1. Folding the noise about its midpoint turns
    /// smooth hills into creased ridgelines, which is what makes the northern
    /// mountains read as rock rather than as dunes.
    /// </summary>
    public static float Ridged(float x, float y, int octaves, float lacunarity = 2.07f, float gain = 0.5f)
    {
        float sum = 0.0f;
        float amplitude = 1.0f;
        float total = 0.0f;
        float weight = 1.0f;
        for (int i = 0; i < octaves; i++)
        {
            float n = 1.0f - Mathf.Abs(ValueNoise(x, y) * 2.0f - 1.0f);
            n *= n * weight;
            weight = Mathf.Clamp(n * 1.9f, 0.0f, 1.0f);
            sum += n * amplitude;
            total += amplitude;
            x *= lacunarity;
            y *= lacunarity;
            amplitude *= gain;
        }
        return sum / Mathf.Max(total, 0.0001f);
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        return t * t * (3.0f - 2.0f * t);
    }

    // ------------------------------------------------------------ polyline math

    /// <summary>
    /// A polyline with its per-segment maths precomputed.
    ///
    /// <see cref="Height"/> is evaluated a few million times per terrain chunk and
    /// once per scatter candidate, and it queries fourteen of these. Recomputing
    /// segment vectors, lengths and the total arc length on every query made the
    /// world definition the bottleneck for the whole pipeline, so all of it is
    /// cached once and every query gets a bounding-box reject first.
    /// </summary>
    public sealed class Polyline
    {
        private readonly Vector2[] _points;
        private readonly Vector2[] _delta;
        private readonly float[] _inverseLengthSquared;
        private readonly float[] _length;
        private readonly float[] _cumulative;
        private readonly float _total;
        private readonly float _minX;
        private readonly float _maxX;
        private readonly float _minY;
        private readonly float _maxY;

        public Polyline(Vector2[] points)
        {
            _points = points;
            int segments = Mathf.Max(points.Length - 1, 0);
            _delta = new Vector2[segments];
            _inverseLengthSquared = new float[segments];
            _length = new float[segments];
            _cumulative = new float[segments + 1];

            _minX = _minY = float.MaxValue;
            _maxX = _maxY = float.MinValue;

            for (int i = 0; i < segments; i++)
            {
                Vector2 d = points[i + 1] - points[i];
                _delta[i] = d;
                float lengthSquared = d.LengthSquared();
                _inverseLengthSquared[i] = lengthSquared > 0.0001f ? 1.0f / lengthSquared : 0.0f;
                _length[i] = Mathf.Sqrt(lengthSquared);
                _cumulative[i + 1] = _cumulative[i] + _length[i];
            }

            _total = _cumulative[segments];

            foreach (Vector2 p in points)
            {
                _minX = Mathf.Min(_minX, p.X);
                _maxX = Mathf.Max(_maxX, p.X);
                _minY = Mathf.Min(_minY, p.Y);
                _maxY = Mathf.Max(_maxY, p.Y);
            }
        }

        public Vector2[] Points => _points;

        /// <summary>
        /// True when the point is certainly further than <paramref name="margin"/>
        /// from the line. Lets a caller skip the segment loop entirely, which is the
        /// difference between the county evaluating in seconds and in minutes.
        /// </summary>
        public bool IsFarFrom(Vector2 p, float margin) =>
            p.X < _minX - margin || p.X > _maxX + margin ||
            p.Y < _minY - margin || p.Y > _maxY + margin;

        public float Distance(Vector2 p, out float alongNormalised)
        {
            float bestSquared = float.MaxValue;
            float bestAlong = 0.0f;

            for (int i = 0; i < _delta.Length; i++)
            {
                Vector2 a = _points[i];
                Vector2 ap = p - a;
                float t = Mathf.Clamp(ap.Dot(_delta[i]) * _inverseLengthSquared[i], 0.0f, 1.0f);
                float dx = ap.X - _delta[i].X * t;
                float dy = ap.Y - _delta[i].Y * t;
                float distanceSquared = dx * dx + dy * dy;
                if (distanceSquared < bestSquared)
                {
                    bestSquared = distanceSquared;
                    bestAlong = _cumulative[i] + _length[i] * t;
                }
            }

            alongNormalised = _total > 0.0001f ? bestAlong / _total : 0.0f;
            return Mathf.Sqrt(bestSquared);
        }

        public float Distance(Vector2 p) => Distance(p, out _);

        /// <summary>Unit direction of the segment nearest to p.</summary>
        public Vector2 DirectionNear(Vector2 p)
        {
            float bestSquared = float.MaxValue;
            Vector2 direction = Vector2.Right;

            for (int i = 0; i < _delta.Length; i++)
            {
                Vector2 a = _points[i];
                Vector2 ap = p - a;
                float t = Mathf.Clamp(ap.Dot(_delta[i]) * _inverseLengthSquared[i], 0.0f, 1.0f);
                float dx = ap.X - _delta[i].X * t;
                float dy = ap.Y - _delta[i].Y * t;
                float distanceSquared = dx * dx + dy * dy;
                if (distanceSquared < bestSquared)
                {
                    bestSquared = distanceSquared;
                    direction = _length[i] > 0.0001f ? _delta[i] / _length[i] : Vector2.Right;
                }
            }

            return direction;
        }

        public Vector2 PointAt(float alongNormalised)
        {
            float target = Mathf.Clamp(alongNormalised, 0.0f, 1.0f) * _total;
            for (int i = 0; i < _delta.Length; i++)
            {
                if (target <= _cumulative[i + 1] || i == _delta.Length - 1)
                {
                    float local = _length[i] > 0.0001f ? (target - _cumulative[i]) / _length[i] : 0.0f;
                    return _points[i] + _delta[i] * Mathf.Clamp(local, 0.0f, 1.0f);
                }
            }

            return _points[^1];
        }

        public float TotalLength => _total;
    }

    public static readonly Polyline RiverLine = new(RiverSpine);
    public static readonly Polyline MillCreekLine = new(MillCreekSpine);

    // Authored secondary drainage. These are dry cuts and seasonal gullies that
    // break up broad stretches of procedural terrain into natural corridors.
    private readonly record struct Ravine(Polyline Line, float ValleyWidth, float GorgeWidth, float Depth);

    private static readonly Ravine[] Ravines =
    {
        new(new Polyline(new[]
        {
            new Vector2(1710.0f, -2560.0f), new Vector2(1910.0f, -2180.0f),
            new Vector2(2200.0f, -1790.0f), new Vector2(2410.0f, -1300.0f),
        }), 170.0f, 42.0f, 38.0f),
        new(new Polyline(new[]
        {
            new Vector2(-3260.0f, -1970.0f), new Vector2(-3040.0f, -1550.0f),
            new Vector2(-2790.0f, -1160.0f), new Vector2(-2630.0f, -760.0f),
        }), 145.0f, 36.0f, 29.0f),
        new(new Polyline(new[]
        {
            new Vector2(1430.0f, 2370.0f), new Vector2(1600.0f, 2730.0f),
            new Vector2(1870.0f, 3060.0f), new Vector2(2110.0f, 3410.0f),
        }), 190.0f, 48.0f, 24.0f),
    };

    private readonly record struct Ridge(Vector2 Center, Vector2 Axis, float HalfLength, float HalfWidth, float Height);

    private static readonly Ridge[] SecondaryRidges =
    {
        new(new Vector2(2780.0f, -2360.0f), new Vector2(0.38f, 0.92f), 760.0f, 250.0f, 68.0f),
        new(new Vector2(-2590.0f, -2770.0f), new Vector2(0.93f, 0.37f), 650.0f, 220.0f, 82.0f),
        new(new Vector2(2570.0f, 560.0f), new Vector2(0.24f, 0.97f), 620.0f, 230.0f, 48.0f),
        new(new Vector2(1110.0f, 3030.0f), new Vector2(0.90f, 0.44f), 720.0f, 280.0f, 31.0f),
    };

    private static readonly float LakeCenterRiverAlong = AlongRiverAt(LakeCenter);
    private static readonly float DamRiverAlong = AlongRiverAt(DamCenter);
    private static readonly float BridgeRiverAlong = AlongRiverAt(new Vector2(-176.0f, 0.0f));

    private static float AlongRiverAt(Vector2 point)
    {
        RiverLine.Distance(point, out float along);
        return along;
    }

    /// <summary>Road centrelines, index-matched to <see cref="Roads"/>.</summary>
    public static readonly Polyline[] RoadLines = BuildRoadLines();
    public static readonly Polyline[] TrailLines = BuildTrailLines();

    private static Polyline[] BuildRoadLines()
    {
        var lines = new Polyline[Roads.Length];
        for (int i = 0; i < Roads.Length; i++)
        {
            lines[i] = new Polyline(Roads[i].Points);
        }

        return lines;
    }

    /// <summary>Convenience wrapper for callers that do not hold a cached line.</summary>
    public static float DistanceToPolyline(Vector2 p, Vector2[] points, out float alongNormalised) =>
        new Polyline(points).Distance(p, out alongNormalised);

    public static float DistanceToPolyline(Vector2 p, Vector2[] points) =>
        DistanceToPolyline(p, points, out _);

    // -------------------------------------------------------------- landmass

    /// <summary>
    /// 1 well inside the county, 0 out over the void, with a fast falloff at the
    /// rim. The boundary is a lobed radius rather than a circle so the silhouette
    /// matches the ragged plateau in the aerial concept.
    /// </summary>
    public static float LandMask(float x, float z)
    {
        float dx = (x - CenterX) / (SpanX * 0.5f);
        float dz = (z - CenterZ) / (SpanZ * 0.5f);
        float angle = Mathf.Atan2(dz, dx);
        float radius = Mathf.Sqrt(dx * dx + dz * dz);

        // Lobes plus fine noise: big bays and headlands, then a crumbled edge.
        float lobes =
            0.084f * Mathf.Sin(angle * 3.0f + 0.7f) +
            0.056f * Mathf.Sin(angle * 5.0f - 1.9f) +
            0.032f * Mathf.Sin(angle * 8.0f + 2.6f);
        float crumble = (Fbm(x * 0.00085f + 41.0f, z * 0.00085f - 17.0f, 4) - 0.5f) * 0.11f;

        float edge = 0.895f + lobes + crumble;
        return 1.0f - Smooth((radius - edge) / 0.075f);
    }

    /// <summary>How far outside the plateau rim a point is, 0 inside, 1 fully over the void.</summary>
    public static float RimFalloff(float x, float z) => 1.0f - LandMask(x, z);

    // -------------------------------------------------------------- elevation

    /// <summary>
    /// The regional trend before any noise, rivers or human works: a high, folded
    /// north that drops through the central river valley into southern farmland.
    /// Values are in game-space metres (town = 0).
    /// </summary>
    private static float RegionalTrend(float x, float z)
    {
        // North-south gradient. Fire Lookout country up top, floodplain at the bottom.
        float northness = Mathf.Clamp((-z - 300.0f) / 2900.0f, 0.0f, 1.0f);
        float trend = Mathf.Pow(northness, 1.42f) * 880.0f;

        // The county rises again on its eastern shoulder, which is what puts Fire
        // Lookout and the eastern forest wall above the town.
        float eastShoulder = Smooth((x - 900.0f) / 2400.0f) * 210.0f * (0.35f + 0.65f * northness);

        // Southern half sags gently toward the river mouth rather than being flat.
        float southSag = Smooth((z - 600.0f) / 2600.0f) * -86.0f;

        // Western farmland is a raised bench, not a plain, so fields read as terraced.
        float westBench = Smooth((-x - 900.0f) / 1500.0f) * 74.0f * (1.0f - Smooth((z - 900.0f) / 2200.0f));

        return trend + eastShoulder + southSag + westBench;
    }

    /// <summary>
    /// Named summits with the elevations printed on the county planning map.
    /// Noise alone will not put a peak where the map says one is, and these are
    /// the two the player is told to navigate by.
    /// </summary>
    private readonly record struct Peak(Vector2 Position, float Elevation, float Radius);

    private static readonly Peak[] Peaks =
    {
        // Fire Lookout, 1380m on the planning map.
        new(new Vector2(958.0f, -3048.0f), 1380.0f - TownElevation, 900.0f),
        // Pine Ridge, 1210m.
        new(new Vector2(-262.0f, -3402.0f), 1210.0f - TownElevation, 780.0f),
    };

    private static float ApplyPeaks(Vector2 p, float h)
    {
        foreach (Peak peak in Peaks)
        {
            float distance = p.DistanceTo(peak.Position);
            if (distance > peak.Radius)
            {
                continue;
            }

            // Pin the summit to its mapped elevation while feathering back into the
            // surrounding ridgeline. Noise may shape the shoulders, but it must not
            // make Pine Ridge higher than the elevation printed on the map.
            float t = 1.0f - distance / peak.Radius;
            float dome = t * t * (3.0f - 2.0f * t);
            h = Mathf.Lerp(h, peak.Elevation, dome);
        }

        return h;
    }

    private static Polyline[] BuildTrailLines()
    {
        var result = new Polyline[Trails.Length];
        for (int i = 0; i < Trails.Length; i++) result[i] = new Polyline(Trails[i].Points);
        return result;
    }

    public static float DistanceToTrail(Vector2 point)
    {
        float nearest = float.MaxValue;
        for (int i = 0; i < TrailLines.Length; i++)
        {
            if (!TrailLines[i].IsFarFrom(point, nearest))
            {
                nearest = Mathf.Min(nearest, TrailLines[i].Distance(point));
            }
        }
        return nearest;
    }

    private static float ApplySecondaryLandforms(Vector2 p, float h)
    {
        foreach (Ridge ridge in SecondaryRidges)
        {
            Vector2 axis = ridge.Axis.Normalized();
            Vector2 side = new(-axis.Y, axis.X);
            Vector2 offset = p - ridge.Center;
            float along = Mathf.Abs(offset.Dot(axis)) / ridge.HalfLength;
            float across = Mathf.Abs(offset.Dot(side)) / ridge.HalfWidth;
            if (along >= 1.0f || across >= 1.0f) continue;

            float endFalloff = 1.0f - Smooth(along);
            float sideFalloff = 1.0f - Smooth(across);
            float shoulderNoise = Mathf.Lerp(0.82f, 1.13f,
                Fbm(p.X * 0.0032f + 18.0f, p.Y * 0.0032f - 42.0f, 3));
            h += ridge.Height * Mathf.Pow(endFalloff * sideFalloff, 1.35f) * shoulderNoise;
        }

        foreach (Ravine ravine in Ravines)
        {
            if (ravine.Line.IsFarFrom(p, ravine.ValleyWidth)) continue;
            float distance = ravine.Line.Distance(p, out float along);
            float endFalloff = Mathf.Pow(Mathf.Max(0.0f, Mathf.Sin(along * Mathf.Pi)), 0.42f);
            float carve = CarveChannel(
                distance,
                ravine.ValleyWidth,
                ravine.Depth * 0.42f,
                ravine.GorgeWidth,
                ravine.Depth * 0.58f);
            h -= carve * endFalloff;
        }

        return h;
    }

    /// <summary>Mountain and hill relief, already scaled by where in the county we are.</summary>
    private static float Relief(float x, float z)
    {
        float northness = Mathf.Clamp((-z - 200.0f) / 3000.0f, 0.0f, 1.0f);

        // Ridged noise dominates the north; smooth fBm dominates the south.
        float ridge = Ridged(x * 0.00042f, z * 0.00042f, 6);
        float rolling = Fbm(x * 0.00055f + 7.3f, z * 0.00055f - 3.1f, 5) - 0.5f;

        float mountainAmplitude = Mathf.Lerp(28.0f, 430.0f, Mathf.Pow(northness, 1.25f));
        float rollingAmplitude = Mathf.Lerp(46.0f, 96.0f, northness);

        float h = (ridge - 0.34f) * mountainAmplitude + rolling * rollingAmplitude;

        // Mid-frequency breakup so slopes are never a clean ramp.
        h += (Fbm(x * 0.0021f - 12.0f, z * 0.0021f + 5.0f, 4) - 0.5f) * Mathf.Lerp(9.0f, 34.0f, northness);

        // The planning map calls the south "rolling hills, good soil". Without a
        // dedicated swell at field scale the whole southern half reads as a flat
        // green plate, which is the single fastest way to make an open world look
        // unfinished. These two octaves are what the farmland drapes over.
        float southness = 1.0f - northness;
        h += (Fbm(x * 0.00135f + 61.0f, z * 0.00135f - 29.0f, 4) - 0.5f) * 54.0f * southness;
        h += (Fbm(x * 0.0038f - 77.0f, z * 0.0038f + 13.0f, 3) - 0.5f) * 17.0f * southness;

        // Fine detail. Kept small so it survives being sampled at chunk LOD without
        // aliasing into shimmer at distance.
        h += (Fbm(x * 0.0094f + 88.0f, z * 0.0094f - 51.0f, 3) - 0.5f) * 4.4f;

        return h;
    }

    /// <summary>
    /// Carves a valley around a watercourse: a wide soft trough, a steeper inner
    /// gorge, then the channel itself. Returns the carve depth to subtract.
    /// </summary>
    private static float CarveChannel(
        float distance,
        float valleyWidth,
        float valleyDepth,
        float gorgeWidth,
        float gorgeDepth)
    {
        float valley = (1.0f - Smooth(distance / valleyWidth)) * valleyDepth;
        float gorge = (1.0f - Smooth(distance / gorgeWidth)) * gorgeDepth;
        return valley + gorge;
    }

    /// <summary>
    /// Half-width of the Blackwater's wetted channel at a point along its run.
    /// A county river is a few metres across at its head and broadens as it
    /// gathers; a constant width reads as a canal ruled across the landscape.
    /// </summary>
    public static float RiverHalfWidth(float alongNormalised)
    {
        float t = Mathf.Clamp(alongNormalised, 0.0f, 1.0f);
        // Headwaters -> the reservoir -> the town reach -> the southern canyon.
        if (t < 0.30f)
        {
            return Mathf.Lerp(5.5f, 9.0f, t / 0.30f);
        }

        if (t < 0.52f)
        {
            return Mathf.Lerp(9.0f, 13.5f, (t - 0.30f) / 0.22f);
        }

        return Mathf.Lerp(13.5f, 33.0f, (t - 0.52f) / 0.48f);
    }

    /// <summary>
    /// Displacement applied to the lake's radial coordinate. A reservoir drowns a
    /// river valley, so its shoreline follows drowned side-gullies and spurs; a
    /// plain ellipse reads as a stamped oval from any viewpoint above the water.
    /// </summary>
    private static float LakeShoreWarp(float x, float z) =>
        (Fbm(x * 0.0013f + 3.0f, z * 0.0013f - 9.0f, 4) - 0.5f) * 0.46f +
        (Fbm(x * 0.0041f - 27.0f, z * 0.0041f + 61.0f, 3) - 0.5f) * 0.17f;

    /// <summary>
    /// Height of the water surface of the Blackwater at a given point along its
    /// run, from the dam outflow down to the southern mouth. Used both to carve the
    /// bed and to place the water plane.
    /// </summary>
    public static float RiverSurfaceY(float alongNormalised)
    {
        // Mountain inflow descends into the level reservoir. The dam then creates
        // a discrete drop before the river continues through Ashwood and the
        // southern canyon.
        const float atHeadwater = 700.0f;
        const float atDam = 168.0f;
        const float atBridge = -8.5f; // matches OldMillBridge.WaterY exactly
        float lakeEntryAlong = Mathf.Max(LakeCenterRiverAlong - 0.02f, 0.01f);
        float damAlong = DamRiverAlong;
        float bridgeAlong = BridgeRiverAlong;

        if (alongNormalised < lakeEntryAlong)
        {
            return Mathf.Lerp(atHeadwater, LakeSurfaceY,
                Smooth(alongNormalised / lakeEntryAlong));
        }

        if (alongNormalised <= damAlong)
        {
            return LakeSurfaceY;
        }

        if (alongNormalised <= bridgeAlong)
        {
            float t = Smooth((alongNormalised - damAlong) / (bridgeAlong - damAlong));
            return Mathf.Lerp(atDam, atBridge, t);
        }

        float u = Smooth((alongNormalised - bridgeAlong) / (1.0f - bridgeAlong));
        return Mathf.Lerp(atBridge, RiverMouthY, u);
    }

    /// <summary>
    /// Natural and settled terrain before road grading, rim falloff, and the dam.
    /// Road grade sampling uses this directly so a road follows an already-carved
    /// valley instead of averaging toward the mountain that existed before it.
    /// </summary>
    private static float NaturalHeight(float x, float z)
    {
        var here = new Vector2(x, z);

        float h = RegionalTrend(x, z) + Relief(x, z);
        h = ApplyPeaks(here, h);
        h = ApplySecondaryLandforms(here, h);

        // ---- Blackwater valley -------------------------------------------------
        float riverDistance = RiverLine.Distance(here, out float along);
        float riverY = RiverSurfaceY(along);

        // The valley opens out as the river runs south, so the northern reach is a
        // notch in the mountains and the southern reach is a proper canyon.
        float southness = Smooth((along - 0.5f) / 0.45f);
        float valleyWidth = Mathf.Lerp(420.0f, 900.0f, southness);
        float valleyDepth = Mathf.Lerp(52.0f, 140.0f, southness);
        float gorgeWidth = Mathf.Lerp(64.0f, 170.0f, southness);
        float gorgeDepth = Mathf.Lerp(26.0f, 96.0f, southness);

        float carve = CarveChannel(riverDistance, valleyWidth, valleyDepth, gorgeWidth, gorgeDepth);
        h -= carve;

        // Force the wetted channel under the surface, then ease through a broad
        // low bank before returning to the valley. The previous blend completed
        // the full elevation change in roughly 40m downstream, producing a near
        // vertical texture wall and leaving no traversable riparian habitat.
        float halfWidth = RiverHalfWidth(along);
        float bankReach = Mathf.Lerp(38.0f, 96.0f, southness);
        float bankT = Mathf.Clamp((riverDistance - halfWidth) / bankReach, 0.0f, 1.0f);
        // Hold the inner bank at its intended shelf height. Only the outer 45%
        // transitions back into the regional terrain; blending over the complete
        // width still inherited too much height halfway across the bank.
        float bedBlend = 1.0f - Smooth((bankT - 0.55f) / 0.45f);
        if (bedBlend > 0.0f)
        {
            float bedY = riverY - Mathf.Lerp(2.4f, 9.5f, southness)
                         - (Fbm(x * 0.02f, z * 0.02f, 3) - 0.5f) * 2.2f;
            float bankY = riverY + Mathf.Lerp(2.2f, 11.0f, southness) * Smooth(bankT);
            float channelT = Smooth(
                (riverDistance - halfWidth * 0.72f) / Mathf.Max(halfWidth * 0.55f, 1.0f));
            float channelAndBankY = Mathf.Lerp(bedY, bankY, channelT);
            h = Mathf.Lerp(h, channelAndBankY, bedBlend);
        }

        // ---- Mill Creek tributary ---------------------------------------------
        if (!MillCreekLine.IsFarFrom(here, 240.0f))
        {
            float creekDistance = MillCreekLine.Distance(here, out float creekAlong);
            // The former 15m gorge was narrower than two vertices in the medium
            // terrain rings, so its banks rendered as repeating triangular teeth.
            h -= CarveChannel(creekDistance, 210.0f, 34.0f, 52.0f, 17.0f);
            float creekBed = 1.0f - Smooth(
                (creekDistance - MillCreekHalfWidth) / 30.0f);
            if (creekBed > 0.0f)
            {
                // Blends fully to the bed rather than to 85 percent of it. Stopping
                // short meant that wherever the surrounding land stood well above
                // the channel the bed never reached below the water surface, so the
                // creek simply had no water for that stretch - the gaps between the
                // floating rectangles were exactly these places.
                float creekY = MillCreekSurfaceY(creekAlong);
                h = Mathf.Lerp(h, creekY - 1.6f, creekBed);
            }
        }

        // ---- Blackwater Lake basin --------------------------------------------
        float lakeDx = (x - LakeCenter.X) / LakeRadiusX;
        float lakeDz = (z - LakeCenter.Y) / LakeRadiusZ;
        float lakeRadial = Mathf.Sqrt(lakeDx * lakeDx + lakeDz * lakeDz);
        float lakeShape = lakeRadial + LakeShoreWarp(x, z);
        // A broad transition shelf is essential here: the surrounding mountains
        // can stand hundreds of metres above the reservoir. Blending that change
        // over only 180m produced a quarry wall around an otherwise natural lake.
        float inLake = 1.0f - Smooth((lakeShape - 0.86f) / 0.80f);
        bool separateRiverChannel = lakeShape > 1.04f && riverDistance < halfWidth * 2.4f;
        if (inLake > 0.0f && !separateRiverChannel)
        {
            // A dished basin, deepest in the middle, so the shoreline reads as a
            // beach rather than as a wall.
            float basin = LakeSurfaceY - Mathf.Lerp(1.0f, 46.0f, 1.0f - Mathf.Clamp(lakeShape / 0.86f, 0.0f, 1.0f));
            h = Mathf.Lerp(h, basin, inLake);
        }

        // ---- Human levelling ---------------------------------------------------
        h = ApplyPlaces(here, h);

        return h;
    }

    private static float PinSummitCores(Vector2 p, float h)
    {
        foreach (Peak peak in Peaks)
        {
            float distance = p.DistanceTo(peak.Position);
            const float coreRadius = 120.0f;
            if (distance >= coreRadius)
            {
                continue;
            }

            float weight = 1.0f - Smooth(distance / coreRadius);
            h = Mathf.Lerp(h, peak.Elevation, weight);
        }

        return h;
    }

    private static float ApplyDam(Vector2 p, float h)
    {
        float damDistance = Mathf.Abs(
            (p.X - DamCenter.X) * 0.94f + (p.Y - DamCenter.Y) * 0.34f);
        float damLateral = Mathf.Abs(
            (p.X - DamCenter.X) * -0.34f + (p.Y - DamCenter.Y) * 0.94f);
        if (damDistance >= 34.0f || damLateral >= DamHalfWidth)
        {
            return h;
        }

        float wall = (1.0f - Smooth((damDistance - 16.0f) / 18.0f))
                     * (1.0f - Smooth((damLateral - DamHalfWidth * 0.72f) / (DamHalfWidth * 0.28f)));
        return Mathf.Lerp(h, DamCrestY, wall);
    }

    private static float LimitHighElevation(float h)
    {
        // The planning map gives 1420m as the county maximum. Preserve the mapped
        // 1380m lookout exactly, then smoothly compress only the final 40m so noise
        // cannot create a taller unnamed mountain or a visibly flat hard clamp.
        const float start = 1380.0f - TownElevation;
        const float ceiling = 1420.0f - TownElevation;
        if (h <= start)
        {
            return h;
        }

        return start + (ceiling - start) *
            (1.0f - Mathf.Exp(-(h - start) / (ceiling - start)));
    }

    /// <summary>Terrain elevation in game-space metres. The single source of truth.</summary>
    public static float Height(float x, float z)
    {
        var here = new Vector2(x, z);
        float h = NaturalHeight(x, z);

        h = ApplyPlaces(here, h);
        h = PinSummitCores(here, h);
        // Roads are the final human grading operation. Applying settlement pads
        // after them buckled Highway 16 at the service station and side-road
        // junctions even though its own profile was valid.
        h = ApplyRoads(here, h);
        h = ApplyTrails(here, h);

        // The dam is infrastructure, not a natural ridge. Apply it after road
        // grading so its own service roads cannot plane the crest out of existence.
        h = ApplyDam(here, h);
        h = LimitHighElevation(h);

        // ---- Plateau rim -------------------------------------------------------
        // Outside the county the land falls into cliff and then void. The transition
        // is sharpened so it reads as a rock face, not a slope you could walk off.
        float land = LandMask(x, z);
        // Highway 16 is the county's mapped east-west entrance. The irregular
        // plateau rim used to win after road grading and cut a near-vertical cliff
        // straight through the asphalt at both county limits. Open a broad,
        // readable mountain pass only where the highway reaches those limits.
        if ((x < WestX + 420.0f || x > EastX - 420.0f) && RoadLines.Length > 0)
        {
            float highwayDistance = RoadLines[0].Distance(here);
            float pass = 1.0f - Smooth((highwayDistance - 16.0f) / 52.0f);
            land = Mathf.Max(land, pass);
        }
        // The lobed silhouette may form bays, but it may not sever authored
        // infrastructure inside the playable county. Preserve a narrower shelf
        // around every mapped road; only Highway 16 receives the broad exit pass.
        for (int roadIndex = 0; roadIndex < Roads.Length; roadIndex++)
        {
            if (Roads[roadIndex].Class == RoadClass.Railway) continue;
            // Mapped infrastructure must sit on land, not on a narrow finger over
            // the abyss. A broad pass also gives roads room for drainage, trees,
            // and believable approach slopes at the irregular county rim.
            float outer = Mathf.Max(RoadShoulder(Roads[roadIndex].Class) * 3.2f, 260.0f);
            if (RoadLines[roadIndex].IsFarFrom(here, outer)) continue;
            float distance = RoadLines[roadIndex].Distance(here);
            if (distance >= outer) continue;
            float inner = Mathf.Max(RoadShoulder(Roads[roadIndex].Class) * 1.25f, 42.0f);
            float corridor = 1.0f - Smooth((distance - inner) / (outer - inner));
            land = Mathf.Max(land, corridor);
        }
        for (int trailIndex = 0; trailIndex < TrailLines.Length; trailIndex++)
        {
            const float outer = 150.0f;
            if (TrailLines[trailIndex].IsFarFrom(here, outer)) continue;
            float distance = TrailLines[trailIndex].Distance(here);
            if (distance >= outer) continue;
            float corridor = 1.0f - Smooth((distance - 18.0f) / 132.0f);
            land = Mathf.Max(land, corridor);
        }
        if (land < 1.0f)
        {
            float cliff = Mathf.Pow(land, 0.42f);
            h = Mathf.Lerp(AbyssY, h, cliff);
        }

        return h;
    }

    /// <summary>
    /// A settlement's effective radius in a given direction. Towns grew along the
    /// land they were built on, so a perfect circle of levelled ground is an
    /// instant tell that terrain was stamped rather than settled. Warping the
    /// radius per-direction costs one noise sample and removes the tell entirely.
    /// </summary>
    private static float WarpedRadius(Vector2 p, in Poi place)
    {
        // Low-harmonic sinusoids produce a clover, which is just as obviously
        // stamped as the circle they replaced. Noise sampled in world space gives an
        // outline with no periodicity for the eye to lock onto.
        float warp = (Fbm(p.X * 0.0022f + place.Position.Y * 0.01f,
                          p.Y * 0.0022f - place.Position.X * 0.01f, 3) - 0.5f) * 0.62f;
        return place.Radius * (1.0f + warp);
    }

    private static float ApplyPlaces(Vector2 p, float h)
    {
        float water = WaterSurfaceY(p.X, p.Y);
        if (water > float.MinValue && h < water + 0.5f)
        {
            return h;
        }

        foreach (Poi place in Places)
        {
            if (place.Flatten <= 0.0f)
            {
                continue;
            }

            float distance = p.DistanceTo(place.Position);
            if (distance > place.Radius * 2.2f)
            {
                continue;
            }

            float radius = WarpedRadius(p, place);

            // Sample the untouched trend at the site centre so settlements sit on a
            // believable bench cut into the hillside rather than on a floating disc.
            float trendAtCentre = RegionalTrend(place.Position.X, place.Position.Y);
            float reliefAtCentre = Relief(place.Position.X, place.Position.Y);
            float target = trendAtCentre + reliefAtCentre * 0.35f;

            // A site placed on a named summit keeps the summit height, otherwise
            // levelling Fire Lookout would shave the mountain it exists to look from.
            float peaked = ApplyPeaks(place.Position, trendAtCentre + reliefAtCentre);
            if (peaked > trendAtCentre + reliefAtCentre + 1.0f)
            {
                target = peaked;
            }

            if (place.Name == "Ashwood")
            {
                target = 0.0f; // the existing Main Street slice is authored at Y=0
            }

            // Level hard over the built core, then release over a long tail so the
            // site sits in the landscape instead of on a podium.
            float core = radius * 0.34f;
            float weight = (1.0f - Smooth((distance - core) / (radius * 1.25f))) * place.Flatten;

            // Keep a little of the original ground so even the core is not a plane.
            h = Mathf.Lerp(h, target, weight * 0.94f);
        }

        return h;
    }

    private static float ApplyRoads(Vector2 p, float h)
    {
        const float maximumEarthworkReach = 280.0f;
        int selected = -1;
        float selectedDistance = float.MaxValue;
        float selectedAlong = 0.0f;
        float selectedShoulder = 0.0f;
        float selectedOuter = 0.0f;

        for (int i = 0; i < Roads.Length; i++)
        {
            float shoulder = RoadShoulder(Roads[i].Class);

            Polyline line = RoadLines[i];
            if (line.IsFarFrom(p, maximumEarthworkReach))
            {
                continue;
            }

            float distance = line.Distance(p, out float along);
            if (distance > maximumEarthworkReach)
            {
                continue;
            }

            // Water crossings are bridged by CountyRoads. Grading the terrain at
            // those pixels fills the channel first, turning a bridge into an
            // embankment and, on the lake, an accidental causeway.
            float water = WaterSurfaceY(p.X, p.Y);
            if (water > float.MinValue && h < water + 0.5f)
            {
                continue;
            }

            // The nearest centreline owns the ground. Class only breaks a true
            // junction tie: prioritising class across the whole shoulder made the
            // paved lookout approach overwrite a parallel dirt spur 24m away.
            bool farther = distance > selectedDistance + 0.25f;
            bool tiedButLowerPriority = Mathf.Abs(distance - selectedDistance) <= 0.25f &&
                                        selected >= 0 &&
                                        (int)Roads[i].Class >= (int)Roads[selected].Class;
            if (selected >= 0 && (farther || tiedButLowerPriority))
            {
                continue;
            }

            float target = RoadHeightAt(i, along);
            float correction = Mathf.Abs(target - h);
            // Real cut and fill slopes consume space. A fixed 25-40m feather made
            // a grade-correct centreline into a vertical slot canyon whenever a
            // route crossed substantial relief. Scale the earthwork reach with the
            // actual correction, while retaining a compact footprint on plains.
            float outer = Mathf.Clamp(
                shoulder + 24.0f + correction * 2.4f,
                shoulder * 4.2f,
                maximumEarthworkReach);
            if (distance >= outer)
            {
                continue;
            }

            selected = i;
            selectedDistance = distance;
            selectedAlong = along;
            selectedShoulder = shoulder;
            selectedOuter = outer;
        }

        if (selected >= 0)
        {
            // Roads cannot be levelled to a constant height without either flying
            // or tunnelling, so terrain follows a class-limited elevation profile.
            float smoothed = RoadHeightAt(selected, selectedAlong);
            float weight = 1.0f - Smooth(
                (selectedDistance - selectedShoulder) /
                (selectedOuter - selectedShoulder));
            h = Mathf.Lerp(h, smoothed, weight);
        }

        return h;
    }

    private static float ApplyTrails(Vector2 point, float height)
    {
        float trailAuthority = 1.0f;
        for (int roadIndex = 0; roadIndex < RoadLines.Length; roadIndex++)
        {
            float shoulder = RoadShoulder(Roads[roadIndex].Class);
            float releaseEnd = shoulder * 1.2f + 12.0f;
            if (RoadLines[roadIndex].IsFarFrom(point, releaseEnd)) continue;
            float distance = RoadLines[roadIndex].Distance(point);
            if (distance >= releaseEnd) continue;
            float release = Smooth((distance - shoulder) / (releaseEnd - shoulder));
            trailAuthority = Mathf.Min(trailAuthority, release);
        }

        for (int i = 0; i < TrailLines.Length; i++)
        {
            const float maximumOuter = 72.0f;
            const float tread = 1.9f;
            Polyline line = TrailLines[i];
            if (line.IsFarFrom(point, maximumOuter)) continue;
            float distance = line.Distance(point, out float along);

            float water = WaterSurfaceY(point.X, point.Y);
            if (water > float.MinValue && height < water + 0.5f) continue;

            float target = TrailHeightAt(i, along);
            float correction = Mathf.Abs(target - height);
            float outer = Mathf.Clamp(7.0f + correction * 2.2f, 7.0f, maximumOuter);
            if (distance >= outer) continue;
            float edgeWeight = 1.0f - Smooth((distance - tread) / (outer - tread));
            float weight = edgeWeight * (distance <= tread ? 1.0f : trailAuthority);
            height = Mathf.Lerp(height, target, weight);
        }
        return height;
    }

    // ---------------------------------------------------------------- surface

    /// <summary>Analytic-ish surface normal from central differences.</summary>
    public static Vector3 Normal(float x, float z, float epsilon = 1.6f)
    {
        float hL = Height(x - epsilon, z);
        float hR = Height(x + epsilon, z);
        float hD = Height(x, z - epsilon);
        float hU = Height(x, z + epsilon);
        return new Vector3(hL - hR, 2.0f * epsilon, hD - hU).Normalized();
    }

    /// <summary>Slope in radians, 0 flat.</summary>
    public static float Slope(float x, float z, float epsilon = 1.6f) =>
        Mathf.Acos(Mathf.Clamp(Normal(x, z, epsilon).Y, -1.0f, 1.0f));

    // ------------------------------------------------------------------ water

    /// <summary>
    /// Mill Creek's water surface, sampled along its own spine.
    ///
    /// This used to be a straight elevation ramp - Lerp(56, -46, along) - chosen
    /// without reference to the land the creek runs through. Terrain does not
    /// oblige a straight line: where the ground sat above the ramp the channel
    /// never cut and the creek was dry; where it sat below, the ramp floated above
    /// the surrounding plain and meshed as slabs of water lying on dry grass. At
    /// Mill Creek village it did both within a few hundred metres, which is why
    /// the creek rendered as a row of disconnected rectangles beside a cliff.
    ///
    /// Sampling the land and then forcing the result to run downhill gives a
    /// profile that is always below its own banks and never flows uphill, which is
    /// the only pair of properties the channel actually needs.
    /// </summary>
    private static readonly float[] MillCreekProfile = BuildMillCreekProfile();

    /// <summary>How far the creek's surface sits below the surrounding ground.</summary>
    private const float MillCreekIncision = 3.2f;
    public const float MillCreekHalfWidth = 12.0f;

    private static float[] BuildMillCreekProfile()
    {
        Vector2[] spine = MillCreekSpine;
        var profile = new float[spine.Length];

        for (int i = 0; i < spine.Length; i++)
        {
            Vector2 p = spine[i];

            // The base landform only - not Height(), which would recurse straight
            // back into the creek carve that depends on this profile.
            profile[i] = ApplyPeaks(p, RegionalTrend(p.X, p.Y) + Relief(p.X, p.Y))
                         - MillCreekIncision;
        }

        // Water does not flow uphill. A single downstream pass clamping each point
        // to its predecessor turns a noisy terrain sample into a valid channel.
        for (int i = 1; i < profile.Length; i++)
        {
            profile[i] = Mathf.Min(profile[i], profile[i - 1] - 0.35f);
        }

        return profile;
    }

    // Road elevations are derived from the carved natural terrain once. Sampling
    // five complete terrain evaluations per road vertex made streaming workers
    // contend with the render thread for the entire traversal benchmark.
    private const float RoadProfileSpacing = 8.0f;
    private static readonly float[][] RoadHeightProfiles = BuildRoadHeightProfiles();
    private readonly record struct RoadProfileAnchor(int Index, float Height);

    private static float[][] BuildRoadHeightProfiles()
    {
        var profiles = new float[RoadLines.Length][];
        for (int roadIndex = 0; roadIndex < RoadLines.Length; roadIndex++)
        {
            Polyline line = RoadLines[roadIndex];
            int count = Mathf.Max(2, Mathf.CeilToInt(line.TotalLength / RoadProfileSpacing) + 1);
            var raw = new float[count];
            for (int sample = 0; sample < count; sample++)
            {
                Vector2 point = line.PointAt(sample / (float)(count - 1));
                raw[sample] = RoadProfileBaseHeight(point);
            }

            var smoothed = new float[count];
            for (int sample = 0; sample < count; sample++)
            {
                float sum = 0.0f;
                int samples = 0;
                for (int offset = -2; offset <= 2; offset++)
                {
                    int source = Mathf.Clamp(sample + offset, 0, count - 1);
                    sum += raw[source];
                    samples++;
                }

                smoothed[sample] = sum / samples;
            }

            // A smoothed profile can still contain a sharp regional transition.
            // Constrain each pass by the operating class, then run the constraint
            // backwards so a steep drop cannot simply be displaced to the other
            // side of the segment.
            float maxGrade = RoadProfileGrade(Roads[roadIndex].Class);
            float spacing = line.TotalLength / Mathf.Max(count - 1, 1);
            float maximumStep = spacing * maxGrade;
            float startHeight = raw[0];
            float endHeight = raw[^1];
            for (int sample = 0; sample < count; sample++)
            {
                float fromStart = maximumStep * sample;
                float fromEnd = maximumStep * (count - 1 - sample);
                float lower = Mathf.Max(startHeight - fromStart, endHeight - fromEnd);
                float upper = Mathf.Min(startHeight + fromStart, endHeight + fromEnd);
                smoothed[sample] = lower <= upper
                    ? Mathf.Clamp(smoothed[sample], lower, upper)
                    : Mathf.Lerp(lower, upper, 0.5f);
            }

            smoothed[0] = startHeight;
            smoothed[^1] = endHeight;
            // Alternating projections converge the whole profile while retaining
            // both fixed endpoints. Corrections can travel only so far through a
            // sampled road per pass, so iteration count follows profile length.
            for (int pass = 0; pass < count + 2; pass++)
            {
                for (int sample = 1; sample < count - 1; sample++)
                {
                    smoothed[sample] = Mathf.Clamp(smoothed[sample],
                        smoothed[sample - 1] - maximumStep,
                        smoothed[sample - 1] + maximumStep);
                }
                for (int sample = count - 2; sample > 0; sample--)
                {
                    smoothed[sample] = Mathf.Clamp(smoothed[sample],
                        smoothed[sample + 1] - maximumStep,
                        smoothed[sample + 1] + maximumStep);
                }
            }

            profiles[roadIndex] = smoothed;
        }

        BakeRoadJunctions(profiles);
        return profiles;
    }

    private static float RoadProfileGrade(RoadClass roadClass) => roadClass switch
    {
        RoadClass.Highway => 0.12f,
        RoadClass.Paved => 0.20f,
        RoadClass.Gravel => 0.26f,
        RoadClass.Dirt => 0.34f,
        RoadClass.Railway => 0.10f,
        _ => 0.18f,
    };

    private static void BakeRoadJunctions(float[][] profiles)
    {
        var anchors = new System.Collections.Generic.List<RoadProfileAnchor>[Roads.Length];
        for (int i = 0; i < anchors.Length; i++) anchors[i] = new();

        for (int a = 0; a < Roads.Length; a++)
        {
            for (int b = a + 1; b < Roads.Length; b++)
            {
                foreach (Vector2 pointA in Roads[a].Points)
                {
                    foreach (Vector2 pointB in Roads[b].Points)
                    {
                        if (pointA.DistanceSquaredTo(pointB) > 0.25f) continue;
                        RoadLines[a].Distance(pointA, out float alongA);
                        RoadLines[b].Distance(pointA, out float alongB);
                        int owner = (int)Roads[a].Class < (int)Roads[b].Class ? a :
                                    (int)Roads[b].Class < (int)Roads[a].Class ? b : a;
                        int subordinate = owner == a ? b : a;
                        float ownerAlong = owner == a ? alongA : alongB;
                        if (ownerAlong <= 0.001f || ownerAlong >= 0.999f) continue;

                        float subordinateAlong = subordinate == a ? alongA : alongB;
                        float ownerPosition = ownerAlong * (profiles[owner].Length - 1);
                        int ownerLow = Mathf.FloorToInt(ownerPosition);
                        int ownerHigh = Mathf.Min(ownerLow + 1, profiles[owner].Length - 1);
                        float junctionHeight = Mathf.Lerp(
                            profiles[owner][ownerLow], profiles[owner][ownerHigh],
                            ownerPosition - ownerLow);
                        int subordinateIndex = Mathf.RoundToInt(
                            subordinateAlong * (profiles[subordinate].Length - 1));
                        AddRoadProfileAnchor(anchors[subordinate],
                            new RoadProfileAnchor(subordinateIndex, junctionHeight));
                    }
                }
            }
        }

        for (int roadIndex = 0; roadIndex < profiles.Length; roadIndex++)
        {
            float[] profile = profiles[roadIndex];
            if (!HasRoadProfileAnchor(anchors[roadIndex], 0))
            {
                AddRoadProfileAnchor(anchors[roadIndex], new RoadProfileAnchor(0, profile[0]));
            }
            if (!HasRoadProfileAnchor(anchors[roadIndex], profile.Length - 1))
            {
                AddRoadProfileAnchor(anchors[roadIndex],
                    new RoadProfileAnchor(profile.Length - 1, profile[^1]));
            }
            anchors[roadIndex].Sort((left, right) => left.Index.CompareTo(right.Index));

            float spacing = RoadLines[roadIndex].TotalLength / Mathf.Max(profile.Length - 1, 1);
            float maximumStep = spacing * RoadProfileGrade(Roads[roadIndex].Class);
            for (int anchor = 0; anchor < anchors[roadIndex].Count - 1; anchor++)
            {
                ConstrainRoadProfileSegment(profile,
                    anchors[roadIndex][anchor], anchors[roadIndex][anchor + 1], maximumStep);
            }
        }
    }

    private static void ConstrainRoadProfileSegment(
        float[] profile, RoadProfileAnchor start, RoadProfileAnchor end, float maximumStep)
    {
        int span = end.Index - start.Index;
        if (span <= 0) return;
        float requiredStep = Mathf.Abs(end.Height - start.Height) / span;
        float step = Mathf.Max(maximumStep, requiredStep);
        profile[start.Index] = start.Height;
        profile[end.Index] = end.Height;
        for (int pass = 0; pass < span + 2; pass++)
        {
            for (int i = start.Index + 1; i < end.Index; i++)
            {
                profile[i] = Mathf.Clamp(profile[i], profile[i - 1] - step, profile[i - 1] + step);
            }
            for (int i = end.Index - 1; i > start.Index; i--)
            {
                profile[i] = Mathf.Clamp(profile[i], profile[i + 1] - step, profile[i + 1] + step);
            }
        }
    }

    private static void AddRoadProfileAnchor(
        System.Collections.Generic.List<RoadProfileAnchor> anchors, RoadProfileAnchor candidate)
    {
        for (int i = 0; i < anchors.Count; i++)
        {
            if (anchors[i].Index != candidate.Index) continue;
            anchors[i] = candidate;
            return;
        }
        anchors.Add(candidate);
    }

    private static bool HasRoadProfileAnchor(
        System.Collections.Generic.List<RoadProfileAnchor> anchors, int index)
    {
        foreach (RoadProfileAnchor anchor in anchors)
        {
            if (anchor.Index == index) return true;
        }
        return false;
    }

    private static float RoadProfileBaseHeight(Vector2 point)
    {
        float height = NaturalHeight(point.X, point.Y);
        height = ApplyPlaces(point, height);
        height = PinSummitCores(point, height);
        height = ApplyDam(point, height);
        return height;
    }

    private static readonly float[][] TrailHeightProfiles = BuildTrailHeightProfiles();

    private static float[][] BuildTrailHeightProfiles()
    {
        var profiles = new float[TrailLines.Length][];
        for (int trailIndex = 0; trailIndex < TrailLines.Length; trailIndex++)
        {
            Polyline line = TrailLines[trailIndex];
            int count = Mathf.Max(2, Mathf.CeilToInt(line.TotalLength / 3.0f) + 1);
            var raw = new float[count];
            for (int sample = 0; sample < count; sample++)
            {
                Vector2 point = line.PointAt(sample / (float)(count - 1));
                float height = RoadProfileBaseHeight(point);
                raw[sample] = ApplyRoads(point, height);
            }

            var profile = new float[count];
            for (int sample = 0; sample < count; sample++)
            {
                float sum = 0.0f;
                int samples = 0;
                for (int offset = -2; offset <= 2; offset++)
                {
                    sum += raw[Mathf.Clamp(sample + offset, 0, count - 1)];
                    samples++;
                }
                profile[sample] = sum / samples;
            }

            float spacing = line.TotalLength / Mathf.Max(count - 1, 1);
            ConstrainRoadProfileSegment(profile,
                new RoadProfileAnchor(0, raw[0]),
                new RoadProfileAnchor(count - 1, raw[^1]),
                spacing * 0.38f);
            profiles[trailIndex] = profile;
        }
        return profiles;
    }

    private static float TrailHeightAt(int trailIndex, float alongNormalised)
    {
        float[] profile = TrailHeightProfiles[trailIndex];
        float position = Mathf.Clamp(alongNormalised, 0.0f, 1.0f) * (profile.Length - 1);
        int lower = Mathf.FloorToInt(position);
        int upper = Mathf.Min(lower + 1, profile.Length - 1);
        return Mathf.Lerp(profile[lower], profile[upper], position - lower);
    }

    private static float RoadHeightAt(int roadIndex, float alongNormalised)
    {
        return RawRoadProfileHeight(roadIndex, alongNormalised);
    }

    private static float RawRoadProfileHeight(int roadIndex, float alongNormalised)
    {
        float[] profile = RoadHeightProfiles[roadIndex];
        float position = Mathf.Clamp(alongNormalised, 0.0f, 1.0f) * (profile.Length - 1);
        int lower = Mathf.FloorToInt(position);
        int upper = Mathf.Min(lower + 1, profile.Length - 1);
        return Mathf.Lerp(profile[lower], profile[upper], position - lower);
    }

    /// <summary>Creek surface at a normalised distance along the spine.</summary>
    private static float MillCreekSurfaceY(float alongNormalised)
    {
        if (MillCreekProfile.Length == 1)
        {
            return MillCreekProfile[0];
        }

        float scaled = Mathf.Clamp(alongNormalised, 0.0f, 1.0f) * (MillCreekProfile.Length - 1);
        int index = Mathf.Clamp((int)scaled, 0, MillCreekProfile.Length - 2);
        return Mathf.Lerp(MillCreekProfile[index], MillCreekProfile[index + 1], scaled - index);
    }

    /// <summary>
    /// Water surface height at a point, or float.MinValue if the point is dry.
    /// Covers the reservoir, the river below the dam and the creek.
    /// </summary>
    public static float WaterSurfaceY(float x, float z)
    {
        var p = new Vector2(x, z);

        float lakeDx = (x - LakeCenter.X) / LakeRadiusX;
        float lakeDz = (z - LakeCenter.Y) / LakeRadiusZ;
        float lakeShape = Mathf.Sqrt(lakeDx * lakeDx + lakeDz * lakeDz) + LakeShoreWarp(x, z);
        if (lakeShape < 0.92f)
        {
            return LakeSurfaceY;
        }

        if (!RiverLine.IsFarFrom(p, 40.0f))
        {
            float riverDistance = RiverLine.Distance(p, out float along);
            if (riverDistance < RiverHalfWidth(along))
            {
                return RiverSurfaceY(along);
            }
        }

        if (!MillCreekLine.IsFarFrom(p, MillCreekHalfWidth))
        {
            float creekDistance = MillCreekLine.Distance(p, out float creekAlong);
            if (creekDistance < MillCreekHalfWidth)
            {
                return MillCreekSurfaceY(creekAlong);
            }
        }

        return float.MinValue;
    }

    public static bool IsUnderwater(float x, float z)
    {
        float water = WaterSurfaceY(x, z);
        return water > float.MinValue && Height(x, z) < water;
    }

    // ----------------------------------------------------------------- biomes

    public enum Biome
    {
        /// <summary>Dense conifer forest - the northern half and the eastern wall.</summary>
        Forest,
        /// <summary>Open meadow and scrub.</summary>
        Meadow,
        /// <summary>Worked fields, hedgerows, crop rows.</summary>
        Farmland,
        /// <summary>Exposed rock: cliffs, high ridges, the plateau rim.</summary>
        Rock,
        /// <summary>Gravel and silt along the water.</summary>
        Riverbank,
        /// <summary>Mown, tracked, built on.</summary>
        Settled,
    }

    public enum Habitat
    {
        Settled,
        Field,
        Meadow,
        UplandScrub,
        ForestEdge,
        MixedWoodland,
        ConiferInterior,
        Riparian,
        Scree,
        Alpine,
    }

    /// <summary>
    /// Which surface a point belongs to. Terrain splatting, vegetation scatter and
    /// footstep audio all key off this, so they cannot disagree with each other.
    /// </summary>
    public static Biome BiomeAt(float x, float z) =>
        BiomeAt(x, z, Height(x, z), Slope(x, z, 3.0f));

    /// <summary>
    /// Biome lookup for callers that already hold the height and slope.
    ///
    /// A terrain mesher derives both from its own vertex grid for free, and a
    /// scatter pass has just sampled them to place the instance. Re-deriving them
    /// here would cost five more <see cref="Height"/> evaluations per query - on a
    /// 65x65 chunk grid that is twenty thousand redundant evaluations per chunk,
    /// which was the whole cost of meshing in the first place.
    /// </summary>
    public static Biome BiomeAt(float x, float z, float height, float slope) =>
        BiomeAtCore(x, z, height, slope, null);

    public static Biome BiomeAt(
        float x, float z, float height, float slope, float forestDensity) =>
        BiomeAtCore(x, z, height, slope, forestDensity);

    private static Biome BiomeAtCore(
        float x, float z, float height, float slope, float? knownForestDensity)
    {
        if (slope > 0.86f || RimFalloff(x, z) > 0.12f)
        {
            return Biome.Rock;
        }

        var p = new Vector2(x, z);

        foreach (Poi place in Places)
        {
            if (place.Kind is PoiKind.Farm)
            {
                continue;
            }

            // Only the built core counts as settled ground. The wider Poi radius is
            // the area whose terrain gets graded, which is a much bigger footprint
            // than the area that is actually paved and built on.
            float built = WarpedRadius(p, place) * (place.Kind == PoiKind.Town ? 0.72f : 0.40f);
            if (p.DistanceTo(place.Position) < built)
            {
                return Biome.Settled;
            }
        }

        if (!RiverLine.IsFarFrom(p, 70.0f))
        {
            float riverDistance = RiverLine.Distance(p, out float along);
            if (riverDistance < RiverHalfWidth(along) * 2.6f && height < RiverSurfaceY(along) + 5.0f)
            {
                return Biome.Riverbank;
            }
        }

        float lakeDx = (x - LakeCenter.X) / LakeRadiusX;
        float lakeDz = (z - LakeCenter.Y) / LakeRadiusZ;
        if (Mathf.Sqrt(lakeDx * lakeDx + lakeDz * lakeDz) < 1.06f && height < LakeSurfaceY + 6.0f)
        {
            return Biome.Riverbank;
        }

        if (FieldStrength(x, z) > 0.5f && slope < 0.32f)
        {
            return Biome.Farmland;
        }

        // Above the treeline the mountains go bare. Ashwood's peaks top out around
        // 1420m real, so a treeline near 1250m leaves bare rock only on the true
        // summits rather than scalping the whole northern third of the county.
        if (height > 820.0f)
        {
            return Biome.Rock;
        }

        float forest = knownForestDensity ?? ForestDensity(x, z, height, slope);
        return forest > 0.34f ? Biome.Forest : Biome.Meadow;
    }

    /// <summary>
    /// More specific ecological identity used for prop and vegetation variety.
    /// Biome controls the terrain material; habitat controls what grows on top of
    /// it, allowing two green forest pixels to become visibly different places.
    /// </summary>
    public static Habitat HabitatAt(float x, float z, float height, float slope)
    {
        Biome biome = BiomeAt(x, z, height, slope);
        float forest = ForestDensity(x, z, height, slope);
        return HabitatAt(x, z, height, slope, biome, forest);
    }

    public static Habitat HabitatAt(
        float x, float z, float height, float slope, Biome biome, float forest)
    {
        if (biome == Biome.Settled) return Habitat.Settled;
        if (biome == Biome.Farmland) return Habitat.Field;
        if (biome == Biome.Riverbank) return Habitat.Riparian;

        if (biome == Biome.Rock)
        {
            return height > 700.0f ? Habitat.Alpine : Habitat.Scree;
        }

        if (forest > 0.68f)
        {
            RegionId region = RegionAt(x, z).Id;
            return region is RegionId.PineRidge or RegionId.BlackwaterBasin
                ? Habitat.ConiferInterior
                : Habitat.MixedWoodland;
        }

        if (forest > 0.25f) return Habitat.ForestEdge;
        return slope > 0.26f ? Habitat.UplandScrub : Habitat.Meadow;
    }

    public static Habitat HabitatAt(float x, float z) =>
        HabitatAt(x, z, Height(x, z), Slope(x, z, 3.0f));

    /// <summary>
    /// Worked-field strength 0..1 inside the farming districts.
    ///
    /// Fields are laid out as a rotated grid of parcels separated by hedgerow
    /// gaps, because that is what farmland actually looks like from the air and
    /// what both concept maps show. A blob of tan noise would not read as
    /// agriculture from any altitude.
    /// </summary>
    public static float FieldStrength(float x, float z)
    {
        var p = new Vector2(x, z);
        float best = 0.0f;

        foreach (Poi place in Places)
        {
            if (place.Kind != PoiKind.Farm)
            {
                continue;
            }

            float radius = WarpedRadius(p, place);
            float distance = p.DistanceTo(place.Position);
            if (distance > radius)
            {
                continue;
            }

            // Each district has its own field bearing, set from its position so it
            // is stable but not shared.
            float bearing = place.Position.X * 0.0009f + place.Position.Y * 0.0013f;
            float cos = Mathf.Cos(bearing);
            float sin = Mathf.Sin(bearing);
            Vector2 local = p - place.Position;
            float u = local.X * cos - local.Y * sin;
            float v = local.X * sin + local.Y * cos;

            // Irregular parcel sizes: real fields are not a uniform grid.
            const float cellU = 168.0f;
            const float cellV = 122.0f;
            float cellX = Mathf.Floor(u / cellU);
            float cellY = Mathf.Floor(v / cellV);

            // Some parcels lie fallow or were never cleared.
            float parcelSeed = Hash(Mathf.RoundToInt(cellX) + 7919, Mathf.RoundToInt(cellY) - 104729);
            if (parcelSeed < 0.22f)
            {
                continue;
            }

            float fu = u / cellU - cellX;
            float fv = v / cellV - cellY;

            // Hedgerow gap around each parcel edge.
            const float hedgeU = 0.055f;
            const float hedgeV = 0.075f;
            float inside =
                Smooth((fu - hedgeU) / hedgeU) * Smooth((1.0f - fu - hedgeU) / hedgeU) *
                Smooth((fv - hedgeV) / hedgeV) * Smooth((1.0f - fv - hedgeV) / hedgeV);

            float falloff = 1.0f - Smooth((distance - radius * 0.62f) / (radius * 0.38f));
            best = Mathf.Max(best, inside * falloff);
        }

        return best;
    }

    /// <summary>
    /// Strength of hedgerow and verge habitat along worked parcel boundaries.
    /// This evaluates the parcel grid directly instead of sampling FieldStrength
    /// in several directions, keeping dense hedge scatter cheap to generate.
    /// </summary>
    public static float FieldMarginStrength(float x, float z)
    {
        var p = new Vector2(x, z);
        float best = 0.0f;
        foreach (Poi place in Places)
        {
            if (place.Kind != PoiKind.Farm) continue;

            float radius = WarpedRadius(p, place);
            float distance = p.DistanceTo(place.Position);
            if (distance > radius) continue;

            float bearing = place.Position.X * 0.0009f + place.Position.Y * 0.0013f;
            float cos = Mathf.Cos(bearing);
            float sin = Mathf.Sin(bearing);
            Vector2 local = p - place.Position;
            float u = local.X * cos - local.Y * sin;
            float v = local.X * sin + local.Y * cos;

            const float cellU = 168.0f;
            const float cellV = 122.0f;
            float fu = u / cellU - Mathf.Floor(u / cellU);
            float fv = v / cellV - Mathf.Floor(v / cellV);
            float edgeMetres = Mathf.Min(
                Mathf.Min(fu, 1.0f - fu) * cellU,
                Mathf.Min(fv, 1.0f - fv) * cellV);

            float hedge = 1.0f - Smooth((edgeMetres - 3.0f) / 10.0f);
            float districtFalloff = 1.0f - Smooth(
                (distance - radius * 0.72f) / Mathf.Max(radius * 0.28f, 1.0f));
            best = Mathf.Max(best, hedge * districtFalloff);
        }

        return best;
    }

    /// <summary>
    /// Continuous forest density 0..1, for scatter weighting. Kept separate from
    /// <see cref="BiomeAt"/> so tree placement can feather at the treeline instead
    /// of stopping on a hard edge.
    /// </summary>
    public static float ForestDensity(float x, float z) =>
        ForestDensity(x, z, Height(x, z), Slope(x, z, 3.0f));

    /// <summary>Forest density for callers that already hold height and slope.</summary>
    public static float ForestDensity(float x, float z, float height, float slope)
    {
        float northness = Mathf.Clamp((-z - 200.0f) / 3000.0f, 0.0f, 1.0f);

        // Two scales of clearing: broad stands, then gaps chewed into them. One
        // octave alone gives the tell-tale "paint splodge" forest edge.
        float stands = Fbm(x * 0.00072f - 21.0f, z * 0.00072f + 33.0f, 4);
        float gaps = Fbm(x * 0.0026f + 55.0f, z * 0.0026f - 12.0f, 3);
        float chance = Mathf.Lerp(0.62f, 0.95f, northness);

        float density = Smooth((chance - stands) / 0.26f);
        density *= 0.62f + 0.38f * Smooth((gaps - 0.24f) / 0.34f);

        // Thin out on steep ground and fade out toward the treeline.
        density *= 1.0f - Smooth((slope - 0.52f) / 0.30f);
        // Town is the 320m datum, so a 1250m treeline is roughly +930m in game
        // space. The former +700m cutoff stripped the entire lookout approach and
        // upper logging country into barren rock well below the mapped treeline.
        density *= 1.0f - Smooth((height - 930.0f) / 170.0f);

        // Cleared and worked ground carries no trees.
        density *= 1.0f - FieldStrength(x, z);

        // Clear only the occupied core, then feather quickly into unmanaged land.
        // The old 0.6..1.25 radius blanket turned Ashwood and every farm district
        // into kilometre-wide lawns even where no building or field existed.
        var p = new Vector2(x, z);
        foreach (Poi place in Places)
        {
            float distance = p.DistanceTo(place.Position);
            (float innerFactor, float outerFactor) = place.Kind switch
            {
                PoiKind.Town => (0.32f, 0.78f),
                PoiKind.Farm => (0.18f, 0.86f),
                PoiKind.Settlement => (0.30f, 0.88f),
                PoiKind.Infrastructure => (0.42f, 0.92f),
                PoiKind.Industrial => (0.34f, 0.92f),
                _ => (0.24f, 0.76f),
            };
            float inner = place.Radius * innerFactor;
            float outer = place.Radius * outerFactor;
            if (distance < outer)
            {
                density *= Smooth((distance - inner) / Mathf.Max(outer - inner, 1.0f));
            }
        }

        for (int i = 0; i < Roads.Length; i++)
        {
            float clear = RoadShoulder(Roads[i].Class) * 1.6f;
            if (RoadLines[i].IsFarFrom(p, clear))
            {
                continue;
            }

            float distance = RoadLines[i].Distance(p);
            if (distance < clear)
            {
                density *= Smooth(distance / clear);
            }
        }

        return Mathf.Clamp(density, 0.0f, 1.0f);
    }

    // -------------------------------------------------------------- utilities

    /// <summary>True if the point is inside the playable county, not out over the void.</summary>
    public static bool IsPlayable(float x, float z) => LandMask(x, z) > 0.55f;

    /// <summary>A safe standing position at or near the requested spot.</summary>
    public static Vector3 GroundPoint(float x, float z) => new(x, Height(x, z), z);

    public static Poi? FindPlace(string name)
    {
        foreach (Poi place in Places)
        {
            if (place.Name == name)
            {
                return place;
            }
        }

        return null;
    }
}
