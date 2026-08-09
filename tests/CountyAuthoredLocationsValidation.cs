#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.UI;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Guards the authored town and bridge against drifting off the county map.</summary>
public partial class CountyAuthoredLocationsValidation : Node
{
    private const string MainStreetPath = "res://scenes/world/ashwood/main_street.tscn";

    public override void _Ready()
    {
        try
        {
            var menu = GD.Load<PackedScene>("res://scenes/ui/main_menu.tscn")
                .Instantiate<MainMenuController>();
            Require(menu.GameplayScenePath == MainStreetPath,
                "New Game does not launch the authored Ashwood county scene.");
            menu.Free();

            var ashwood = GD.Load<PackedScene>(MainStreetPath).Instantiate<Node3D>();
            Require(ashwood.Transform.IsEqualApprox(Transform3D.Identity),
                "Ashwood must remain anchored at the county origin.");

            var county = ashwood.GetNode<CountyWorldIntegration>("CountyWorld");
            Require(county.PlayerPath == new NodePath("../Gameplay/Player"),
                "County streaming is not following Ashwood's player.");
            Require(county.PreviewInEditor && county.PreviewRadius >= 2,
                "The unified Ashwood county is hidden in the editor viewport.");

            Node vista = ashwood.GetNode("Environment/Vista");
            Require(!vista.HasNode("Ridges"),
                "The obsolete low-poly ridge silhouettes are still present around Ashwood.");
            Require(vista.HasNode("WaterTower/Tank"),
                "Removing the ridge silhouettes also removed the Vista landmarks.");

            var bridge = ashwood.GetNode<OldMillBridge>("Environment/OldMillBridge");
            Require(bridge.Position.IsEqualApprox(Vector3.Zero),
                "Old Mill Bridge was offset even though it uses authored world coordinates.");
            Require(bridge.CountyIntegrationMode,
                "Old Mill Bridge would duplicate county terrain, water and vegetation.");

            CountyMap.Poi mapped = CountyMap.FindPlace("Old Mill Bridge")
                ?? throw new InvalidOperationException("Old Mill Bridge is absent from CountyMap.");
            Require(mapped.Position.IsEqualApprox(
                    new Vector2(OldMillBridge.ChannelCenterX, 0.0f)),
                $"Bridge scene centre {OldMillBridge.ChannelCenterX:F1} does not match map {mapped.Position}.");
            Require(Mathf.IsEqualApprox(
                    CountyMap.WaterSurfaceY(mapped.Position.X, mapped.Position.Y),
                    OldMillBridge.WaterY),
                "Blackwater River does not meet the authored bridge water level.");

            ashwood.Free();
            GD.Print("COUNTY_AUTHORED_LOCATIONS: PASS ashwood=(0,0) bridge=(-176,0)");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_AUTHORED_LOCATIONS: FAIL - " + exception.Message);
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
