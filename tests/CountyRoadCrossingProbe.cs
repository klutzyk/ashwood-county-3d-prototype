using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Throwaway probe: measures terrain vs water at every route/water crossing.</summary>
public partial class CountyRoadCrossingProbe : Node
{
    public override void _Ready()
    {
        foreach (CountyMap.Road road in CountyMap.Roads)
        {
            var line = new CountyMap.Polyline(road.Points);
            float total = line.TotalLength;
            int steps = Mathf.Max(2, Mathf.RoundToInt(total / 4.0f));
            bool inside = false;
            float startAlong = 0.0f;

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = line.PointAt(t);
                float water = CountyMap.WaterSurfaceY(p.X, p.Y);
                bool wet = water > float.MinValue;

                if (wet && !inside)
                {
                    inside = true;
                    startAlong = t;
                }
                else if (!wet && inside)
                {
                    inside = false;
                    Report(road, line, startAlong, t);
                }
            }

            if (inside)
            {
                Report(road, line, startAlong, 1.0f);
            }
        }

        // Roughness of the graded corridor: how bumpy is a ribbon that samples
        // Height directly, across and along the carriageway?
        foreach (CountyMap.Road road in CountyMap.Roads)
        {
            var line = new CountyMap.Polyline(road.Points);
            float half = CountyMap.RoadHalfWidth(road.Class);
            float maxCross = 0.0f;
            float maxLong = 0.0f;
            int steps = Mathf.RoundToInt(line.TotalLength / 4.0f);
            float previous = 0.0f;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = line.PointAt(i / (float)steps);
                Vector2 d = line.DirectionNear(p);
                var n = new Vector2(-d.Y, d.X);
                float c = CountyMap.Height(p.X, p.Y);
                float l = CountyMap.Height(p.X - n.X * half, p.Y - n.Y * half);
                float r = CountyMap.Height(p.X + n.X * half, p.Y + n.Y * half);
                maxCross = Mathf.Max(maxCross, Mathf.Abs(c - (l + r) * 0.5f));
                if (i > 0)
                {
                    maxLong = Mathf.Max(maxLong, Mathf.Abs(c - previous));
                }
                previous = c;
            }

            GD.Print($"ROUGH {road.Name,-22} len={line.TotalLength,7:F0} crownErr={maxCross,6:F2} step4m={maxLong,6:F2}");
        }

        GetTree().Quit(0);
    }

    private static void Report(CountyMap.Road road, CountyMap.Polyline line, float a, float b)
    {
        Vector2 mid = line.PointAt((a + b) * 0.5f);
        float span = (b - a) * line.TotalLength;
        float water = CountyMap.WaterSurfaceY(mid.X, mid.Y);
        float terrain = CountyMap.Height(mid.X, mid.Y);
        float riverDistance = CountyMap.RiverLine.Distance(mid, out float along);
        float creek = CountyMap.MillCreekLine.Distance(mid);
        GD.Print(
            $"CROSS {road.Name,-22} {road.Class,-8} at ({mid.X,8:F0},{mid.Y,8:F0}) span={span,6:F0}m " +
            $"water={water,8:F1} terrain={terrain,8:F1} fill={terrain - water,7:F1} " +
            $"riverD={riverDistance,6:F0} halfW={CountyMap.RiverHalfWidth(along),5:F1} creekD={creek,7:F0}");
    }
}
