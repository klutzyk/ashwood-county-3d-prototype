#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Reports what <see cref="CountyMap"/> actually says, with no rendering involved.
///
/// The first full review render showed a county with almost no forest, no lake
/// surface and roads riding long elevated decks. Each of those is either a map
/// bug or a consumer bug, and the two are indistinguishable from a screenshot -
/// so this prints the underlying numbers instead of guessing from pixels.
/// </summary>
public partial class CountyDataProbe : Node
{
    public override void _Ready()
    {
        ReportForest();
        ReportLake();
        ReportRoadDecks();
        ReportMillCreekProfile();
        GetTree().Quit(0);
    }

    private static void ReportForest()
    {
        GD.Print("--- FOREST DENSITY ---");

        int forested = 0;
        int total = 0;
        float sum = 0.0f;
        float peak = 0.0f;

        for (float z = CountyMap.NorthZ; z < CountyMap.SouthZ; z += 64.0f)
        {
            for (float x = CountyMap.WestX; x < CountyMap.EastX; x += 64.0f)
            {
                float density = CountyMap.ForestDensity(x, z);
                total++;
                sum += density;
                peak = Mathf.Max(peak, density);
                if (density > 0.35f)
                {
                    forested++;
                }
            }
        }

        GD.Print($"  mean={sum / total:F3} peak={peak:F3} " +
                 $"coverage>0.35 = {100.0f * forested / total:F1}% of {total} samples");

        // The deep northern forest is where the concept map is most emphatic, so
        // it is the strongest single signal that density is scaled wrongly.
        foreach ((string name, float x, float z) in new[]
                 {
                     ("north forest", -900.0f, -2000.0f),
                     ("pine ridge", -262.0f, -3402.0f),
                     ("logging camp", -1902.0f, -2448.0f),
                     ("east ridge", 1500.0f, -400.0f),
                     ("south canyon", -900.0f, 2600.0f),
                 })
        {
            GD.Print($"  {name,-14} density={CountyMap.ForestDensity(x, z):F3} " +
                     $"height={CountyMap.Height(x, z):F0} " +
                     $"slope={Mathf.RadToDeg(CountyMap.Slope(x, z)):F0}deg");
        }
    }

    private static void ReportLake()
    {
        GD.Print("--- LAKE ---");
        GD.Print($"  LakeSurfaceY={CountyMap.LakeSurfaceY:F1} " +
                 $"centre={CountyMap.LakeCenter}");

        int wet = 0;
        int dry = 0;
        float deepest = 0.0f;

        // A grid over the mapped lake basin. A reservoir that never reports a
        // surface above its bed is a lake that cannot be meshed.
        for (float z = CountyMap.LakeCenter.Y - 700.0f; z <= CountyMap.LakeCenter.Y + 700.0f; z += 25.0f)
        {
            for (float x = CountyMap.LakeCenter.X - 700.0f; x <= CountyMap.LakeCenter.X + 700.0f; x += 25.0f)
            {
                float surface = CountyMap.WaterSurfaceY(x, z);
                float ground = CountyMap.Height(x, z);
                if (surface > float.MinValue && ground < surface)
                {
                    wet++;
                    deepest = Mathf.Max(deepest, surface - ground);
                }
                else
                {
                    dry++;
                }
            }
        }

        GD.Print($"  basin samples wet={wet} dry={dry} deepest={deepest:F1}m");
        GD.Print($"  at centre: surface={CountyMap.WaterSurfaceY(CountyMap.LakeCenter.X, CountyMap.LakeCenter.Y):F1} " +
                 $"ground={CountyMap.Height(CountyMap.LakeCenter.X, CountyMap.LakeCenter.Y):F1}");
    }

    /// <summary>
    /// A cross-section through the Mill Creek valley.
    ///
    /// The village render showed a straight vertical cliff with the creek's water
    /// perched along its top edge and a grass plain far below - water cannot sit on
    /// a ridge, so either the channel carve or the water surface is wrong. Printing
    /// ground and water side by side across the valley says which.
    /// </summary>
    private static void ReportMillCreekProfile()
    {
        GD.Print("--- MILL CREEK CROSS-SECTION ---");
        GD.Print("  offset  ground   water   (metres, perpendicular to the creek)");

        // Through the village, stepping across the channel rather than along it.
        var centre = new Vector2(-2104.0f, 1702.0f);
        var across = new Vector2(0.82f, 0.57f).Normalized();

        for (float t = -180.0f; t <= 180.0f; t += 12.0f)
        {
            Vector2 p = centre + across * t;
            float ground = CountyMap.Height(p.X, p.Y);
            float water = CountyMap.WaterSurfaceY(p.X, p.Y);
            string waterText = water > float.MinValue ? $"{water,7:F1}" : "      -";
            string flag = water > float.MinValue && ground < water ? "  WET" : string.Empty;
            GD.Print($"  {t,6:F0} {ground,7:F1} {waterText}{flag}");
        }
    }

    private static void ReportRoadDecks()
    {
        GD.Print("--- ROAD WATER DECKS ---");

        // Roads lift onto a 3.2m deck wherever they read as over water. A bridge
        // is a handful of consecutive samples; anything longer is the road riding
        // a viaduct down a valley, which is what the panorama appeared to show.
        foreach (CountyMap.Road route in CountyMap.Roads)
        {
            int longest = 0;
            int run = 0;
            int wetSamples = 0;

            for (int i = 0; i < route.Points.Length - 1; i++)
            {
                Vector2 a = route.Points[i];
                Vector2 b = route.Points[i + 1];
                int steps = Mathf.Max(1, Mathf.RoundToInt(a.DistanceTo(b) / 7.0f));

                for (int s = 0; s < steps; s++)
                {
                    Vector2 p = a.Lerp(b, (float)s / steps);
                    float water = CountyMap.WaterSurfaceY(p.X, p.Y);
                    float ground = CountyMap.Height(p.X, p.Y);
                    if (water > float.MinValue && ground < water + 0.6f)
                    {
                        wetSamples++;
                        run++;
                        longest = Mathf.Max(longest, run);
                    }
                    else
                    {
                        run = 0;
                    }
                }
            }

            if (longest > 4)
            {
                GD.Print($"  {route.Name,-28} longest deck={longest} samples " +
                         $"(~{longest * 7}m) total wet={wetSamples}");
            }
        }
    }
}
