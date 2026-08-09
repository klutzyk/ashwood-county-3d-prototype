#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

public partial class CountyLocationScenesValidation : Node
{
    private const string Root = "res://scenes/world/county/locations/";
    private static readonly (string MapName, string File, CountyLocationScene.LocationKind Kind)[] Expected =
    {
        ("Sheriff's Office", "sheriffs_office.tscn", CountyLocationScene.LocationKind.SheriffsOffice),
        ("Hospital", "hospital.tscn", CountyLocationScene.LocationKind.Hospital),
        ("Service Station", "service_station.tscn", CountyLocationScene.LocationKind.ServiceStation),
        ("Pine Ridge", "pine_ridge.tscn", CountyLocationScene.LocationKind.PineRidge),
        ("Fire Lookout", "fire_lookout.tscn", CountyLocationScene.LocationKind.FireLookout),
        ("Blackwater Dam", "blackwater_dam.tscn", CountyLocationScene.LocationKind.BlackwaterDam),
        ("Logging Camp", "logging_camp.tscn", CountyLocationScene.LocationKind.LoggingCamp),
        ("Farm District", "farm_district.tscn", CountyLocationScene.LocationKind.FarmDistrict),
        ("Mill Creek", "mill_creek.tscn", CountyLocationScene.LocationKind.MillCreek),
        ("Railway Crossing", "railway_crossing.tscn", CountyLocationScene.LocationKind.RailwayCrossing),
        ("County Fairgrounds", "county_fairgrounds.tscn", CountyLocationScene.LocationKind.CountyFairgrounds),
        ("Trailer Park", "trailer_park.tscn", CountyLocationScene.LocationKind.TrailerPark),
        ("South Farmland", "south_farmland.tscn", CountyLocationScene.LocationKind.SouthFarmland),
    };

    public override async void _Ready()
    {
        try
        {
            Require(CountyLocations.AuthoredLocationCount == Expected.Length,
                "CountyLocations does not register every location scene.");

            var seenKinds = new HashSet<CountyLocationScene.LocationKind>();
            foreach ((string mapName, string file, CountyLocationScene.LocationKind kind) in Expected)
            {
                CountyMap.Poi mapped = CountyMap.FindPlace(mapName)
                    ?? throw new InvalidOperationException($"CountyMap is missing {mapName}.");
                Require(CountyMap.IsPlayable(mapped.Position.X, mapped.Position.Y),
                    $"{mapName} lies outside the playable county.");

                PackedScene packed = GD.Load<PackedScene>(Root + file)
                    ?? throw new InvalidOperationException($"Could not load {file}.");
                CountyLocationScene location = packed.Instantiate<CountyLocationScene>();
                Require(location.LocationName == mapName && location.Kind == kind,
                    $"{file} has the wrong location configuration.");
                AddChild(location);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Require(CountMeshes(location) >= 4,
                    $"{mapName} did not generate a meaningful visible layout.");
                location.QueueFree();
                seenKinds.Add(kind);
            }

            Require(seenKinds.Count == Expected.Length,
                "Two county scenes share the same layout kind.");
            GD.Print($"COUNTY_LOCATION_SCENES: PASS scenes={Expected.Length}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_LOCATION_SCENES: FAIL - " + exception.Message);
            GetTree().Quit(1);
        }
    }

    private static int CountMeshes(Node node)
    {
        int count = node is MeshInstance3D ? 1 : 0;
        foreach (Node child in node.GetChildren()) count += CountMeshes(child);
        return count;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
