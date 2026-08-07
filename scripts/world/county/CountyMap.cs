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
            new Vector2(40.0f, -1830.0f),
            new Vector2(-40.0f, -1975.0f),
            new Vector2(-160.0f, -2130.0f),
            new Vector2(-330.0f, -2360.0f),
            new Vector2(-402.0f, -2680.0f),
            new Vector2(-350.0f, -3040.0f),
            new Vector2(-262.0f, -3402.0f),
        }),
        new("Fire Lookout Road", RoadClass.Dirt, new[]
        {
            new Vector2(-330.0f, -2382.0f),
            new Vector2(-40.0f, -2520.0f),
            new Vector2(320.0f, -2600.0f),
            new Vector2(640.0f, -2740.0f),
            new Vector2(852.0f, -2920.0f),
            new Vector2(958.0f, -3048.0f),
        }),
        new("Logging Road", RoadClass.Gravel, new[]
        {
            new Vector2(-402.0f, -2680.0f),
            new Vector2(-760.0f, -2620.0f),
            new Vector2(-1140.0f, -2530.0f),
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
            new Vector2(958.0f, -3048.0f),
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
            new Vector2(-40.0f, -1975.0f),
            new Vector2(240.0f, -2060.0f),
            new Vector2(480.0f, -2280.0f),
            new Vector2(560.0f, -2560.0f),
            new Vector2(320.0f, -2600.0f),
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

    /// <summary>Road centrelines, index-matched to <see cref="Roads"/>.</summary>
    public static readonly Polyline[] RoadLines = BuildRoadLines();

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

            // A smooth dome that only ever raises the land, so the peak sits on top
            // of the existing ridgeline rather than replacing it.
            float t = 1.0f - distance / peak.Radius;
            float dome = t * t * (3.0f - 2.0f * t);
            h = Mathf.Max(h, Mathf.Lerp(h, peak.Elevation, dome));
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
        // Above the bridge the river is a shallow town-level stream; below it the
        // land falls away and the river drops into the southern canyon.
        const float atDam = 168.0f;
        const float atBridge = -8.5f; // matches OldMillBridge.WaterY exactly
        float bridgeAlong = 0.52f;

        if (alongNormalised <= bridgeAlong)
        {
            float t = Smooth(alongNormalised / bridgeAlong);
            return Mathf.Lerp(atDam, atBridge, t);
        }

        float u = Smooth((alongNormalised - bridgeAlong) / (1.0f - bridgeAlong));
        return Mathf.Lerp(atBridge, RiverMouthY, u);
    }

    /// <summary>Terrain elevation in game-space metres. The single source of truth.</summary>
    public static float Height(float x, float z)
    {
        var here = new Vector2(x, z);

        float h = RegionalTrend(x, z) + Relief(x, z);
        h = ApplyPeaks(here, h);

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

        // Force the immediate channel to sit just under its water surface so the
        // river is always actually in its bed rather than perched on a shelf.
        float halfWidth = RiverHalfWidth(along);
        float bedBlend = 1.0f - Smooth((riverDistance - halfWidth) / (halfWidth * 2.4f));
        if (bedBlend > 0.0f)
        {
            float bedY = riverY - Mathf.Lerp(2.4f, 9.5f, southness)
                         - (Fbm(x * 0.02f, z * 0.02f, 3) - 0.5f) * 2.2f;
            h = Mathf.Lerp(h, bedY, bedBlend);
        }

        // ---- Mill Creek tributary ---------------------------------------------
        if (!MillCreekLine.IsFarFrom(here, 240.0f))
        {
            float creekDistance = MillCreekLine.Distance(here, out float creekAlong);
            h -= CarveChannel(creekDistance, 210.0f, 34.0f, 15.0f, 21.0f);
            float creekBed = 1.0f - Smooth((creekDistance - 7.0f) / 11.0f);
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
        float inLake = 1.0f - Smooth((lakeShape - 0.86f) / 0.30f);
        if (inLake > 0.0f)
        {
            // A dished basin, deepest in the middle, so the shoreline reads as a
            // beach rather than as a wall.
            float basin = LakeSurfaceY - Mathf.Lerp(1.0f, 46.0f, 1.0f - Mathf.Clamp(lakeShape / 0.86f, 0.0f, 1.0f));
            h = Mathf.Lerp(h, basin, inLake);
        }

        // ---- Dam wall ----------------------------------------------------------
        // The dam plugs the outflow throat. Without it the reservoir would simply
        // drain down the carved channel and there would be no lake at all.
        float damDistance = Mathf.Abs(
            (x - DamCenter.X) * 0.94f + (z - DamCenter.Y) * 0.34f);
        float damLateral = Mathf.Abs(
            (x - DamCenter.X) * -0.34f + (z - DamCenter.Y) * 0.94f);
        if (damDistance < 34.0f && damLateral < DamHalfWidth)
        {
            float wall = (1.0f - Smooth((damDistance - 16.0f) / 18.0f))
                         * (1.0f - Smooth((damLateral - DamHalfWidth * 0.72f) / (DamHalfWidth * 0.28f)));
            h = Mathf.Lerp(h, DamCrestY, wall);
        }

        // ---- Human levelling ---------------------------------------------------
        h = ApplyPlaces(here, h);
        h = ApplyRoads(here, h);

        // ---- Plateau rim -------------------------------------------------------
        // Outside the county the land falls into cliff and then void. The transition
        // is sharpened so it reads as a rock face, not a slope you could walk off.
        float land = LandMask(x, z);
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
        for (int i = 0; i < Roads.Length; i++)
        {
            float shoulder = RoadShoulder(Roads[i].Class);
            float grade = shoulder * 4.2f;

            Polyline line = RoadLines[i];
            if (line.IsFarFrom(p, grade))
            {
                continue;
            }

            float distance = line.Distance(p);
            if (distance > grade)
            {
                continue;
            }

            // Roads cannot be levelled to a constant height without either flying or
            // tunnelling, so instead the terrain is smoothed toward a local average
            // sampled along the carriageway. That produces a graded cutting that
            // still climbs with the land.
            float smoothed = LocalAverageHeight(p, line, shoulder * 3.0f);
            float weight = 1.0f - Smooth((distance - shoulder) / (grade - shoulder));
            h = Mathf.Lerp(h, smoothed, weight * 0.92f);
        }

        return h;
    }

    /// <summary>
    /// Average of the raw terrain sampled along the road direction, which gives a
    /// road-following grade without recursing back into <see cref="Height"/>.
    /// </summary>
    private static float LocalAverageHeight(Vector2 p, Polyline line, float span)
    {
        Vector2 direction = line.DirectionNear(p);

        float sum = 0.0f;
        const int samples = 5;
        for (int i = 0; i < samples; i++)
        {
            float t = (i / (float)(samples - 1) - 0.5f) * 2.0f * span;
            Vector2 s = p + direction * t;

            // Peaks have to be included here. Without them the road grading averages
            // toward the un-peaked trend and quietly planes the named summits back
            // off - Fire Lookout lost 120m of mountain to its own access track.
            sum += ApplyPeaks(s, RegionalTrend(s.X, s.Y) + Relief(s.X, s.Y));
        }

        return sum / samples;
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

        if (!MillCreekLine.IsFarFrom(p, 7.0f))
        {
            float creekDistance = MillCreekLine.Distance(p, out float creekAlong);
            if (creekDistance < 7.0f)
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
    public static Biome BiomeAt(float x, float z, float height, float slope)
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

        return ForestDensity(x, z, height, slope) > 0.34f ? Biome.Forest : Biome.Meadow;
    }

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
        density *= 1.0f - Smooth((height - 700.0f) / 180.0f);

        // Cleared and worked ground carries no trees.
        density *= 1.0f - FieldStrength(x, z);

        // Clearings: fields, roads, settlements and water all push trees back.
        var p = new Vector2(x, z);
        foreach (Poi place in Places)
        {
            float distance = p.DistanceTo(place.Position);
            if (distance < place.Radius * 1.25f)
            {
                density *= Smooth((distance - place.Radius * 0.6f) / (place.Radius * 0.65f));
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
