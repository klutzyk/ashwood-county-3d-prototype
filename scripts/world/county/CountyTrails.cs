#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Draws the authored walking routes between county roads and wilderness sites.
/// The tread follows the terrain instead of flattening a vehicle-width corridor;
/// CountyVegetation owns the corresponding clearance mask.
/// </summary>
[Tool]
public partial class CountyTrails : Node3D, ICountyChunkSource
{
    [Export] public int TrailRadius { get; set; } = 5;
    [Export(PropertyHint.Range, "2,10,0.5")] public float SampleSpacing { get; set; } = 4.0f;

    public int ChunkRadius => TrailRadius;

    private const float Edge = 1.85f;
    private const string MarkerScenePath =
        "res://assets/environment/roads/weathered_roadsign.tscn";

    private sealed record TrailPath(string Name, Vector2[] Points, float[] Along);

    private readonly List<TrailPath> _paths = new();
    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private PackedScene? _markerScene;

    public override void _Ready()
    {
        _markerScene = ResourceLoader.Exists(MarkerScenePath)
            ? ResourceLoader.Load<PackedScene>(MarkerScenePath)
            : null;
        BuildPaths();

        if (GetParent() is CountyWorld world) world.RegisterSource(this);
    }

    private void BuildPaths()
    {
        _paths.Clear();
        foreach (CountyMap.Trail trail in CountyMap.Trails)
        {
            Vector2[] points = Resample(trail.Points, SampleSpacing);
            var along = new float[points.Length];
            for (int i = 1; i < points.Length; i++)
            {
                along[i] = along[i - 1] + points[i - 1].DistanceTo(points[i]);
            }
            _paths.Add(new TrailPath(trail.Name, points, along));
        }
    }

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk)) return;

        var holder = new Node3D { Name = $"Trails_{chunk.X}_{chunk.Y}" };
        AddChild(holder);
        _chunks[chunk] = holder;

        Vector2 origin = CountyChunks.Origin(chunk);
        var bounds = new Rect2(origin - Vector2.One * Edge,
            Vector2.One * (CountyChunks.Size + Edge * 2.0f));

        foreach (TrailPath path in _paths)
        {
            if (bounds.HasPoint(path.Points[0])) AddTrailMarker(holder, path, true, ring);
            if (bounds.HasPoint(path.Points[^1])) AddTrailMarker(holder, path, false, ring);
        }
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        if (_chunks.Remove(chunk, out Node3D? holder)) holder.QueueFree();
    }

    public void UpdateChunkRing(Vector2I chunk, int ring) { }

    private void AddTrailMarker(Node3D holder, TrailPath path, bool start, int ring)
    {
        if (ring > 3 || _markerScene == null) return;
        int index = start ? 0 : path.Points.Length - 1;
        Vector2 point = path.Points[index];
        Vector2 direction = start
            ? (path.Points[1] - point).Normalized()
            : (point - path.Points[^2]).Normalized();
        var side = new Vector2(-direction.Y, direction.X);
        Vector2 position = point + side * 2.8f;

        Node3D marker = _markerScene.Instantiate<Node3D>();
        marker.Name = start
            ? $"{SafeName(path.Name)}Trailhead"
            : $"{SafeName(path.Name)}DestinationMarker";
        marker.Position = new Vector3(
            position.X, CountyMap.Height(position.X, position.Y), position.Y);
        marker.Rotation = new Vector3(0, Mathf.Atan2(direction.X, direction.Y), 0);
        holder.AddChild(marker);
        ConfigureMarker(marker);
    }

    private static void ConfigureMarker(Node node)
    {
        if (node is GeometryInstance3D geometry)
        {
            geometry.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
            geometry.VisibilityRangeEnd = 180.0f;
            geometry.VisibilityRangeEndMargin = 24.0f;
            geometry.VisibilityRangeFadeMode =
                GeometryInstance3D.VisibilityRangeFadeModeEnum.Self;
        }
        foreach (Node child in node.GetChildren()) ConfigureMarker(child);
    }

    private static Vector2[] Resample(Vector2[] control, float spacing)
    {
        var result = new List<Vector2>();
        for (int segment = 0; segment < control.Length - 1; segment++)
        {
            Vector2 a = control[segment];
            Vector2 b = control[segment + 1];
            int steps = Mathf.Max(1, Mathf.CeilToInt(a.DistanceTo(b) / spacing));
            for (int step = 0; step < steps; step++) result.Add(a.Lerp(b, step / (float)steps));
        }
        result.Add(control[^1]);
        return result.ToArray();
    }

    private static string SafeName(string value) =>
        value.Replace(" ", string.Empty).Replace("'", string.Empty);

    public bool IsBuildComplete => true;
}
