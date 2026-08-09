#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Guards the concept-map facts that later regional work relies on.</summary>
public partial class CountyLayoutValidation : Node
{
    public override void _Ready()
    {
        try
        {
            Require(CountyMap.Regions.Length == 8, "The county must retain eight authoring regions.");
            Require(CountyMap.Roads.Length >= 28, "The county road hierarchy is incomplete.");

            var roadNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (CountyMap.Road road in CountyMap.Roads)
            {
                roadNames.Add(road.Name);
            }

            foreach (CountyMap.Region region in CountyMap.Regions)
            {
                Require(roadNames.Contains(region.PrimaryConnection),
                    $"{region.Name} has no primary road named {region.PrimaryConnection}.");
            }

            RequireRegion("Pine Ridge", CountyMap.RegionId.PineRidge);
            RequireRegion("Blackwater Dam", CountyMap.RegionId.BlackwaterBasin);
            RequireRegion("Farm District", CountyMap.RegionId.WesternFarms);
            RequireRegion("Ashwood", CountyMap.RegionId.Ashwood);
            RequireRegion("Mill Creek", CountyMap.RegionId.MillCreek);
            RequireRegion("County Fairgrounds", CountyMap.RegionId.FairgroundsAndTrailerPark);
            RequireRegion("Trailer Park", CountyMap.RegionId.FairgroundsAndTrailerPark);
            RequireRegion("South Farmland", CountyMap.RegionId.SouthFarmland);

            RequireElevation("Pine Ridge", 1210.0f, 1.0f);
            RequireElevation("Fire Lookout", 1380.0f, 1.0f);
            RequireNear(CountyMap.Height(CountyMap.DamCenter.X, CountyMap.DamCenter.Y),
                CountyMap.DamCrestY, 0.1f, "Blackwater Dam crest");

            float lakeBed = CountyMap.Height(CountyMap.LakeCenter.X, CountyMap.LakeCenter.Y);
            Require(lakeBed < CountyMap.LakeSurfaceY - 20.0f,
                "Blackwater Lake does not have a sufficiently deep basin.");

            float sampledMaximum = float.MinValue;
            for (float z = CountyMap.NorthZ; z <= CountyMap.SouthZ; z += 64.0f)
            {
                for (float x = CountyMap.WestX; x <= CountyMap.EastX; x += 64.0f)
                {
                    if (CountyMap.LandMask(x, z) > 0.5f)
                    {
                        sampledMaximum = Mathf.Max(sampledMaximum, CountyMap.Height(x, z));
                    }
                }
            }

            Require(sampledMaximum + CountyMap.TownElevation <= 1420.5f,
                $"County elevation exceeds the mapped 1420m maximum: " +
                $"{sampledMaximum + CountyMap.TownElevation:F1}m.");

            GD.Print($"COUNTY_LAYOUT: PASS regions={CountyMap.Regions.Length} " +
                     $"roads={CountyMap.Roads.Length} max={sampledMaximum + CountyMap.TownElevation:F1}m");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_LAYOUT: FAIL - " + exception.Message);
            GetTree().Quit(1);
        }
    }

    private static void RequireRegion(string placeName, CountyMap.RegionId expected)
    {
        CountyMap.Poi place = CountyMap.FindPlace(placeName)
            ?? throw new InvalidOperationException($"Missing mapped place: {placeName}");
        CountyMap.RegionId actual = CountyMap.RegionAt(place.Position.X, place.Position.Y).Id;
        Require(actual == expected, $"{placeName} belongs to {actual}, expected {expected}.");
    }

    private static void RequireElevation(string placeName, float realElevation, float tolerance)
    {
        CountyMap.Poi place = CountyMap.FindPlace(placeName)
            ?? throw new InvalidOperationException($"Missing mapped place: {placeName}");
        RequireNear(CountyMap.Height(place.Position.X, place.Position.Y) + CountyMap.TownElevation,
            realElevation, tolerance, placeName);
    }

    private static void RequireNear(float actual, float expected, float tolerance, string label) =>
        Require(Mathf.Abs(actual - expected) <= tolerance,
            $"{label} is {actual:F1}, expected {expected:F1} +/- {tolerance:F1}.");

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
