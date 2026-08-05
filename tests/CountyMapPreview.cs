using System;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Renders Ashwood County top-down straight from <see cref="CountyMap"/> so the
/// world definition can be checked against the concept maps before any of it is
/// turned into geometry. Cheaper and far clearer than flying a camera around a
/// half-built world.
///
/// Writes .godot/county_preview/ashwood_county_map.png (shaded relief + biomes +
/// roads + water) and a companion elevation-only pass.
/// </summary>
public partial class CountyMapPreview : Node
{
    private const int Size = 900;

    public override void _Ready()
    {
        ulong started = Time.GetTicksMsec();
        string directory = "res://.godot/county_preview";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(directory));

        Image shaded = Image.CreateEmpty(Size, Size, false, Image.Format.Rgb8);
        Image elevation = Image.CreateEmpty(Size, Size, false, Image.Format.Rgb8);

        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;
        var heights = new float[Size, Size];

        // CountyMap is pure and allocation-free, so the sampling grid parallelises
        // across rows with no synchronisation at all.
        System.Threading.Tasks.Parallel.For(0, Size, py =>
        {
            float z = Mathf.Lerp(CountyMap.NorthZ, CountyMap.SouthZ, py / (float)(Size - 1));
            for (int px = 0; px < Size; px++)
            {
                float x = Mathf.Lerp(CountyMap.WestX, CountyMap.EastX, px / (float)(Size - 1));
                heights[px, py] = CountyMap.Height(x, z);
            }
        });

        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                float h = heights[px, py];
                if (h > CountyMap.AbyssY + 5.0f)
                {
                    minHeight = Mathf.Min(minHeight, h);
                    maxHeight = Mathf.Max(maxHeight, h);
                }
            }
        }

        GD.Print($"COUNTY_PREVIEW: sampled {Size}x{Size} in {Time.GetTicksMsec() - started}ms");

        GD.Print($"COUNTY_PREVIEW: elevation range {minHeight:F1} .. {maxHeight:F1} (game metres)");
        GD.Print($"COUNTY_PREVIEW: real-world {minHeight + CountyMap.TownElevation:F0}m .. " +
                 $"{maxHeight + CountyMap.TownElevation:F0}m");

        float metresPerPixelX = CountyMap.SpanX / (Size - 1);
        var shadedColours = new Color[Size, Size];

        // Biome and forest-density queries each re-derive slope, which costs another
        // handful of Height evaluations per pixel, so this pass also runs wide.
        System.Threading.Tasks.Parallel.For(0, Size, py =>
        {
            float z = Mathf.Lerp(CountyMap.NorthZ, CountyMap.SouthZ, py / (float)(Size - 1));
            for (int px = 0; px < Size; px++)
            {
                float x = Mathf.Lerp(CountyMap.WestX, CountyMap.EastX, px / (float)(Size - 1));
                float h = heights[px, py];

                // Hillshade from the neighbouring samples, lit from the northwest.
                float hL = px > 0 ? heights[px - 1, py] : h;
                float hR = px < Size - 1 ? heights[px + 1, py] : h;
                float hU = py > 0 ? heights[px, py - 1] : h;
                float hD = py < Size - 1 ? heights[px, py + 1] : h;
                var normal = new Vector3(hL - hR, 2.0f * metresPerPixelX, hU - hD).Normalized();
                float light = Mathf.Clamp(normal.Dot(new Vector3(-0.5f, 0.72f, -0.48f).Normalized()), 0.0f, 1.0f);
                light = 0.35f + 0.65f * light;

                Color colour = SurfaceColour(x, z, h);

                float water = CountyMap.WaterSurfaceY(x, z);
                if (water > float.MinValue && h < water)
                {
                    float depth = Mathf.Clamp((water - h) / 40.0f, 0.0f, 1.0f);
                    colour = new Color(0.16f, 0.34f, 0.44f).Lerp(new Color(0.04f, 0.11f, 0.20f), depth);
                    light = 0.85f + 0.15f * light;
                }

                shadedColours[px, py] = colour * light;
            }
        });

        for (int py = 0; py < Size; py++)
        {
            for (int px = 0; px < Size; px++)
            {
                shaded.SetPixel(px, py, shadedColours[px, py]);
                float t = Mathf.Clamp(
                    (heights[px, py] - minHeight) / Mathf.Max(maxHeight - minHeight, 1.0f), 0.0f, 1.0f);
                elevation.SetPixel(px, py, new Color(t, t, t));
            }
        }

        GD.Print($"COUNTY_PREVIEW: shaded in {Time.GetTicksMsec() - started}ms total");

        DrawRoads(shaded);
        DrawPlaces(shaded);

        string shadedPath = ProjectSettings.GlobalizePath($"{directory}/ashwood_county_map.png");
        string elevationPath = ProjectSettings.GlobalizePath($"{directory}/ashwood_county_elevation.png");
        shaded.SavePng(shadedPath);
        elevation.SavePng(elevationPath);

        GD.Print($"COUNTY_PREVIEW: wrote {shadedPath}");
        GD.Print($"COUNTY_PREVIEW: wrote {elevationPath}");

        ReportPlaces();

        GetTree().Quit(0);
    }

    private static Color SurfaceColour(float x, float z, float height)
    {
        if (CountyMap.RimFalloff(x, z) > 0.55f)
        {
            return new Color(0.06f, 0.07f, 0.09f);
        }

        return CountyMap.BiomeAt(x, z) switch
        {
            CountyMap.Biome.Forest => new Color(0.17f, 0.30f, 0.15f)
                .Lerp(new Color(0.10f, 0.20f, 0.10f), CountyMap.ForestDensity(x, z)),
            CountyMap.Biome.Meadow => new Color(0.42f, 0.46f, 0.25f),
            CountyMap.Biome.Farmland => new Color(0.62f, 0.57f, 0.32f),
            CountyMap.Biome.Rock => new Color(0.44f, 0.42f, 0.40f),
            CountyMap.Biome.Riverbank => new Color(0.54f, 0.51f, 0.44f),
            CountyMap.Biome.Settled => new Color(0.58f, 0.55f, 0.52f),
            _ => new Color(0.5f, 0.5f, 0.5f),
        };
    }

    private static Vector2I ToPixel(Vector2 world)
    {
        float u = (world.X - CountyMap.WestX) / CountyMap.SpanX;
        float v = (world.Y - CountyMap.NorthZ) / CountyMap.SpanZ;
        return new Vector2I(
            Mathf.Clamp(Mathf.RoundToInt(u * (Size - 1)), 0, Size - 1),
            Mathf.Clamp(Mathf.RoundToInt(v * (Size - 1)), 0, Size - 1));
    }

    private static void DrawRoads(Image image)
    {
        foreach (CountyMap.Road road in CountyMap.Roads)
        {
            Color colour = road.Class switch
            {
                CountyMap.RoadClass.Highway => new Color(0.75f, 0.16f, 0.14f),
                CountyMap.RoadClass.Paved => new Color(0.14f, 0.14f, 0.16f),
                CountyMap.RoadClass.Gravel => new Color(0.55f, 0.50f, 0.44f),
                CountyMap.RoadClass.Dirt => new Color(0.46f, 0.36f, 0.26f),
                CountyMap.RoadClass.Railway => new Color(0.30f, 0.22f, 0.30f),
                _ => Colors.Magenta,
            };
            int thickness = road.Class == CountyMap.RoadClass.Highway ? 2 : 1;

            for (int i = 0; i < road.Points.Length - 1; i++)
            {
                Vector2I a = ToPixel(road.Points[i]);
                Vector2I b = ToPixel(road.Points[i + 1]);
                DrawLine(image, a, b, colour, thickness);
            }
        }
    }

    private static void DrawPlaces(Image image)
    {
        foreach (CountyMap.Poi place in CountyMap.Places)
        {
            Vector2I p = ToPixel(place.Position);
            Color colour = place.Kind switch
            {
                CountyMap.PoiKind.Town => new Color(1.0f, 0.9f, 0.2f),
                CountyMap.PoiKind.Settlement => new Color(1.0f, 0.6f, 0.2f),
                CountyMap.PoiKind.Farm => new Color(0.9f, 0.85f, 0.5f),
                CountyMap.PoiKind.Industrial => new Color(0.8f, 0.4f, 0.9f),
                CountyMap.PoiKind.Landmark => new Color(0.3f, 0.9f, 1.0f),
                _ => new Color(1.0f, 1.0f, 1.0f),
            };

            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    if (dx * dx + dy * dy > 16)
                    {
                        continue;
                    }

                    int px = Mathf.Clamp(p.X + dx, 0, Size - 1);
                    int py = Mathf.Clamp(p.Y + dy, 0, Size - 1);
                    image.SetPixel(px, py, dx * dx + dy * dy > 9 ? Colors.Black : colour);
                }
            }
        }
    }

    private static void DrawLine(Image image, Vector2I a, Vector2I b, Color colour, int thickness)
    {
        int steps = Mathf.Max(Mathf.Abs(b.X - a.X), Mathf.Abs(b.Y - a.Y)) + 1;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int px = Mathf.RoundToInt(Mathf.Lerp(a.X, b.X, t));
            int py = Mathf.RoundToInt(Mathf.Lerp(a.Y, b.Y, t));
            for (int dy = -thickness; dy <= thickness; dy++)
            {
                for (int dx = -thickness; dx <= thickness; dx++)
                {
                    int qx = Mathf.Clamp(px + dx, 0, Size - 1);
                    int qy = Mathf.Clamp(py + dy, 0, Size - 1);
                    image.SetPixel(qx, qy, colour);
                }
            }
        }
    }

    private static void ReportPlaces()
    {
        GD.Print("COUNTY_PREVIEW: site elevations (real-world metres)");
        foreach (CountyMap.Poi place in CountyMap.Places)
        {
            float h = CountyMap.Height(place.Position.X, place.Position.Y);
            float slope = CountyMap.Slope(place.Position.X, place.Position.Y, 4.0f);
            GD.Print($"  {place.Name,-22} y={h,8:F1}  real={h + CountyMap.TownElevation,7:F0}m  " +
                     $"slope={Mathf.RadToDeg(slope),5:F1}deg  biome={CountyMap.BiomeAt(place.Position.X, place.Position.Y)}");
        }
    }
}
