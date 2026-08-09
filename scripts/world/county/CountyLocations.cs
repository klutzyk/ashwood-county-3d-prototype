#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>Places and streams the independently authored county location scenes.</summary>
[Tool]
public partial class CountyLocations : Node3D, ICountyChunkSource
{
    [Export] public int LocationRadius { get; set; } = 5;
    public int ChunkRadius => LocationRadius;

    private const string Root = "res://scenes/world/county/locations/";
    private readonly record struct Location(string MapName, string SceneFile, float YawDegrees);

    private static readonly Location[] Locations =
    {
        new("Sheriff's Office", "sheriffs_office.tscn", -8),
        new("Hospital", "hospital.tscn", 8),
        new("Service Station", "service_station.tscn", -12),
        new("Pine Ridge", "pine_ridge.tscn", 18),
        new("Fire Lookout", "fire_lookout.tscn", -24),
        new("Blackwater Dam", "blackwater_dam.tscn", 2),
        new("Logging Camp", "logging_camp.tscn", 14),
        new("Farm District", "farm_district.tscn", -8),
        new("Mill Creek", "mill_creek.tscn", 22),
        new("Railway Crossing", "railway_crossing.tscn", -37),
        new("County Fairgrounds", "county_fairgrounds.tscn", 5),
        new("Trailer Park", "trailer_park.tscn", -18),
        new("South Farmland", "south_farmland.tscn", 9),
    };

    private readonly Dictionary<Vector2I, Node3D> _chunks = new();
    private readonly Dictionary<string, PackedScene> _scenes = new();
    private Node3D? _editorLocations;

    public override void _Ready()
    {
        foreach (Location location in Locations)
        {
            string path = Root + location.SceneFile;
            if (ResourceLoader.Exists(path) && ResourceLoader.Load<PackedScene>(path) is PackedScene scene)
            {
                _scenes[location.MapName] = scene;
            }
            else
            {
                GD.PushWarning($"CountyLocations: missing location scene {path}");
            }
        }

        if (Engine.IsEditorHint() && GetParent() is CountyWorld { EditorPreview: true })
        {
            BuildEditorOverview();
            return;
        }

        if (GetParent() is CountyWorld world)
        {
            world.RegisterSource(this);
        }
    }

    private void BuildEditorOverview()
    {
        _editorLocations = new Node3D { Name = "AllMappedLocations" };
        AddChild(_editorLocations);
        foreach (Location location in Locations)
        {
            Place(_editorLocations, location);
        }
    }

    public void BuildChunk(Vector2I chunk, int ring)
    {
        if (_chunks.ContainsKey(chunk)) return;
        var holder = new Node3D { Name = $"Locations_{chunk.X}_{chunk.Y}" };
        AddChild(holder);
        _chunks[chunk] = holder;

        Vector2 origin = CountyChunks.Origin(chunk);
        var bounds = new Rect2(origin, Vector2.One * CountyChunks.Size);
        foreach (Location location in Locations)
        {
            CountyMap.Poi? place = CountyMap.FindPlace(location.MapName);
            if (place.HasValue && bounds.HasPoint(place.Value.Position))
            {
                Place(holder, location);
            }
        }
    }

    public void ReleaseChunk(Vector2I chunk)
    {
        if (_chunks.Remove(chunk, out Node3D? holder)) holder.QueueFree();
    }

    public void UpdateChunkRing(Vector2I chunk, int ring) { }

    private void Place(Node3D holder, in Location location)
    {
        CountyMap.Poi? mapped = CountyMap.FindPlace(location.MapName);
        if (!mapped.HasValue || !_scenes.TryGetValue(location.MapName, out PackedScene? scene)) return;

        CountyMap.Poi place = mapped.Value;
        Node3D instance = scene.Instantiate<Node3D>();
        instance.Name = location.MapName.Replace(" ", string.Empty).Replace("'", string.Empty);
        instance.Position = new Vector3(
            place.Position.X,
            CountyMap.Height(place.Position.X, place.Position.Y) + 0.04f,
            place.Position.Y);
        instance.RotationDegrees = new Vector3(0, location.YawDegrees, 0);
        holder.AddChild(instance);
    }

    public static int AuthoredLocationCount => Locations.Length;
}
