#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Validates and reports the terrain suitability of wilderness landmarks.</summary>
public partial class CountyNatureValidation : Node
{
    public override void _Ready()
    {
        try
        {
            Require(CountyMap.NaturalFeatures.Length >= 6, "Too few authored natural landmarks.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (CountyMap.NaturalFeature feature in CountyMap.NaturalFeatures)
            {
                Require(names.Add(feature.Name), $"Duplicate natural landmark: {feature.Name}");
                Vector2 candidate = FindCandidate(feature);
                float slope = CountyMap.Slope(feature.Position.X, feature.Position.Y, 4.0f);
                Vector3 normal = CountyMap.Normal(candidate.X, candidate.Y, 4.0f);
                float downhillYaw = Mathf.RadToDeg(Mathf.Atan2(normal.X, normal.Z));
                GD.Print($"COUNTY_NATURE_SITE: {feature.Name} current=({feature.Position.X:F0}," +
                         $"{feature.Position.Y:F0}) height={CountyMap.Height(feature.Position.X, feature.Position.Y):F1} " +
                         $"slope={Mathf.RadToDeg(slope):F1}deg candidate=({candidate.X:F0},{candidate.Y:F0}) " +
                         $"candidate_slope={Mathf.RadToDeg(CountyMap.Slope(candidate.X, candidate.Y, 4.0f)):F1}deg " +
                         $"downhill_yaw={downhillYaw:F0}");
            }

            GD.Print("COUNTY_NATURE_VALIDATION: PASS");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_NATURE_VALIDATION: FAIL - " + exception.Message);
            GetTree().Quit(1);
        }
    }

    private static Vector2 FindCandidate(in CountyMap.NaturalFeature feature)
    {
        Vector2 best = feature.Position;
        float bestScore = float.MaxValue;
        float desiredSlope = feature.Kind is CountyMap.NaturalFeatureKind.Escarpment
            ? Mathf.DegToRad(12.0f)
            : Mathf.DegToRad(5.0f);

        for (float dz = -360; dz <= 360; dz += 40)
        {
            for (float dx = -360; dx <= 360; dx += 40)
            {
                Vector2 p = feature.Position + new Vector2(dx, dz);
                if (CountyMap.LandMask(p.X, p.Y) < 0.78f || NearInfrastructure(p)) continue;
                float slope = CountyMap.Slope(p.X, p.Y, 4.0f);
                if (slope > Mathf.DegToRad(24.0f)) continue;

                float score = p.DistanceTo(feature.Position) * 0.001f +
                              Mathf.Abs(slope - desiredSlope) * 8.0f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
        }

        return best;
    }

    private static bool NearInfrastructure(Vector2 p)
    {
        for (int i = 0; i < CountyMap.Roads.Length; i++)
        {
            if (CountyMap.RoadLines[i].Distance(p) < 72.0f) return true;
        }

        foreach (CountyMap.Poi place in CountyMap.Places)
        {
            if (p.DistanceTo(place.Position) < place.Radius * 0.72f) return true;
        }

        return false;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
