#nullable enable

using System;
using System.Reflection;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Geometry-level audit for continuous road and wilderness access.</summary>
public partial class CountyTraversalAudit : Node
{
    public override void _Ready()
    {
        try
        {
            AuditRoads();
            AuditTrails();
            GD.Print("COUNTY_TRAVERSAL: PASS");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("COUNTY_TRAVERSAL: FAIL - " + error.Message);
            GetTree().Quit(1);
        }
    }

    private static void AuditRoads()
    {
        foreach (CountyMap.Road road in CountyMap.Roads)
        {
            if (road.Class == CountyMap.RoadClass.Railway) continue;
            var line = new CountyMap.Polyline(road.Points);
            int steps = Mathf.Max(2, Mathf.CeilToInt(line.TotalLength / 4.0f));
            float maxDryGrade = 0.0f;
            Vector2 worst = default;
            Vector2 worstPrevious = default;
            float worstPreviousHeight = 0.0f;
            float worstHeight = 0.0f;

            Vector2 previous = line.PointAt(0.0f);
            float previousHeight = CountyMap.Height(previous.X, previous.Y);
            bool previousWet = IsWaterCrossing(previous, previousHeight);
            for (int i = 1; i <= steps; i++)
            {
                Vector2 point = line.PointAt(i / (float)steps);
                float height = CountyMap.Height(point.X, point.Y);
                bool wet = IsWaterCrossing(point, height);
                float span = Mathf.Max(point.DistanceTo(previous), 0.01f);
                if (!wet && !previousWet)
                {
                    float grade = Mathf.Abs(height - previousHeight) / span;
                    if (grade > maxDryGrade)
                    {
                        maxDryGrade = grade;
                        worst = point;
                        worstPrevious = previous;
                        worstPreviousHeight = previousHeight;
                        worstHeight = height;
                    }
                }
                previous = point;
                previousHeight = height;
                previousWet = wet;
            }

            float limit = road.Class switch
            {
                CountyMap.RoadClass.Highway => 0.18f,
                CountyMap.RoadClass.Paved => 0.24f,
                CountyMap.RoadClass.Gravel => 0.30f,
                _ => 0.40f,
            };
            GD.Print($"ROAD_GRADE {road.Name,-27} max={maxDryGrade * 100.0f,5:F1}% " +
                     $"from {worstPrevious} y={worstPreviousHeight:F2} to {worst} y={worstHeight:F2}");
            if (maxDryGrade > limit) ReportRoadCandidates(worstPrevious, worst);
            Require(maxDryGrade <= limit,
                $"{road.Name} reaches {maxDryGrade * 100.0f:F1}% dry grade (limit {limit * 100.0f:F0}%).");
        }
    }

    private static void ReportRoadCandidates(Vector2 before, Vector2 after)
    {
        MethodInfo? sample = typeof(CountyMap).GetMethod(
            "RoadHeightAt", BindingFlags.NonPublic | BindingFlags.Static);
        if (sample == null) return;
        for (int i = 0; i < CountyMap.Roads.Length; i++)
        {
            float distance = CountyMap.RoadLines[i].Distance(after, out float along);
            float gradeRadius = CountyMap.RoadShoulder(CountyMap.Roads[i].Class) * 4.2f;
            if (distance > gradeRadius) continue;
            float profile = (float)(sample.Invoke(null, new object[] { i, along }) ?? 0.0f);
            GD.Print($"  CANDIDATE {CountyMap.Roads[i].Name,-27} d={distance:F2} along={along:F4} profile={profile:F2} " +
                     $"beforeY={CountyMap.Height(before.X, before.Y):F2} afterY={CountyMap.Height(after.X, after.Y):F2}");

            FieldInfo? anchorsField = typeof(CountyMap).GetField(
                "RoadJunctionAnchors", BindingFlags.NonPublic | BindingFlags.Static);
            if (anchorsField?.GetValue(null) is Array all && all.GetValue(i) is Array anchors)
            {
                foreach (object anchor in anchors)
                {
                    Type type = anchor.GetType();
                    GD.Print($"    ANCHOR along={type.GetProperty("Along")?.GetValue(anchor)} " +
                             $"height={type.GetProperty("Height")?.GetValue(anchor)}");
                }
            }
        }
    }

    private static void AuditTrails()
    {
        Require(CountyMap.Trails.Length == CountyMap.NaturalFeatures.Length,
            "Every wilderness landmark needs one authored access trail.");

        for (int index = 0; index < CountyMap.Trails.Length; index++)
        {
            CountyMap.Trail trail = CountyMap.Trails[index];
            CountyMap.NaturalFeature feature = CountyMap.NaturalFeatures[index];
            var line = new CountyMap.Polyline(trail.Points);

            float roadDistance = float.MaxValue;
            foreach (CountyMap.Polyline road in CountyMap.RoadLines)
            {
                roadDistance = Mathf.Min(roadDistance, road.Distance(trail.Points[0]));
            }
            Require(roadDistance <= 8.0f, $"{trail.Name} has no readable road trailhead ({roadDistance:F1}m). ");
            Require(trail.Points[^1].DistanceTo(feature.Position) <= feature.Radius * 0.65f,
                $"{trail.Name} does not arrive at {feature.Name}.");

            int steps = Mathf.Max(2, Mathf.CeilToInt(line.TotalLength / 3.0f));
            float maxGrade = 0.0f;
            Vector2 worst = default;
            Vector2 worstPrevious = default;
            float worstHeight = 0.0f;
            float worstPreviousHeight = 0.0f;
            Vector2 previous = line.PointAt(0.0f);
            float previousHeight = CountyMap.Height(previous.X, previous.Y);
            for (int i = 1; i <= steps; i++)
            {
                Vector2 point = line.PointAt(i / (float)steps);
                float height = CountyMap.Height(point.X, point.Y);
                float grade = Mathf.Abs(height - previousHeight) /
                              Mathf.Max(point.DistanceTo(previous), 0.01f);
                if (grade > maxGrade)
                {
                    maxGrade = grade;
                    worst = point;
                    worstPrevious = previous;
                    worstHeight = height;
                    worstPreviousHeight = previousHeight;
                }
                Require(!IsWaterCrossing(point, height), $"{trail.Name} enters water at {point}.");
                previous = point;
                previousHeight = height;
            }
            GD.Print($"TRAIL_GRADE {trail.Name,-30} len={line.TotalLength,6:F0}m max={maxGrade * 100.0f,5:F1}% " +
                     $"from {worstPrevious} y={worstPreviousHeight:F2} to {worst} y={worstHeight:F2}");
            Require(maxGrade <= 0.48f, $"{trail.Name} reaches {maxGrade * 100.0f:F1}% grade.");
        }
    }

    private static bool IsWaterCrossing(Vector2 point, float ground)
    {
        float water = CountyMap.WaterSurfaceY(point.X, point.Y);
        return water > float.MinValue && ground < water + 0.6f;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
