#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Player-height review of the county's travel hierarchy and trail approaches.</summary>
public partial class CountyRouteVisualReview : Node3D
{
    private readonly record struct Shot(string File, Vector2 From, Vector2 To, float Height, float Fov);

    public override async void _Ready()
    {
        try
        {
            var viewport = new SubViewport
            {
                Size = new Vector2I(1280, 720),
                OwnWorld3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            AddChild(viewport);

            var probe = new Node3D { Name = "RouteStreamProbe" };
            viewport.AddChild(probe);
            CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(probe);
            viewport.AddChild(built.Root);
            var camera = new Camera3D { Current = true, Near = 0.2f, Far = 5000.0f };
            viewport.AddChild(camera);

            string output = ProjectSettings.GlobalizePath("res://.godot/county_route_review");
            DirAccess.MakeDirRecursiveAbsolute(output);
            string requested = OS.GetEnvironment("COUNTY_ROUTE_SHOT");

            foreach (Shot shot in Shots())
            {
                if (requested.Length > 0 &&
                    !shot.File.StartsWith(requested, StringComparison.OrdinalIgnoreCase)) continue;

                probe.GlobalPosition = new Vector3(
                    shot.From.X, CountyMap.Height(shot.From.X, shot.From.Y) + 1.0f, shot.From.Y);
                camera.GlobalTransform = CountySceneBuilder.LookAt(shot.From, shot.To, shot.Height);
                camera.Fov = shot.Fov;

                bool settled = false;
                for (int frame = 0; frame < 900; frame++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (frame >= 90 && built.World.IsStreamingComplete)
                    {
                        settled = true;
                        break;
                    }
                }
                if (!settled) throw new InvalidOperationException($"Streaming did not settle for {shot.File}.");

                if (shot.File.StartsWith("08", StringComparison.Ordinal))
                {
                    Node? oldGrowth = built.World.FindChild("OldGrowthHollow", true, false);
                    GD.Print($"COUNTY_ROUTE_REVIEW: old-growth node present={oldGrowth != null}");
                }

                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Error error = viewport.GetTexture().GetImage().SavePng(Path.Combine(output, shot.File));
                if (error != Error.Ok) throw new InvalidOperationException($"Could not save {shot.File}: {error}.");
                GD.Print($"COUNTY_ROUTE_REVIEW: {shot.File}");
            }

            GD.Print("COUNTY_ROUTE_REVIEW: PASS");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("COUNTY_ROUTE_REVIEW: FAIL - " + error);
            GetTree().Quit(1);
        }
    }

    private static IEnumerable<Shot> Shots()
    {
        yield return new("01_highway_town_approach.png",
            new Vector2(1150, 505), new Vector2(200, 710), 2.35f, 62);
        yield return new("02_county_road_north.png",
            new Vector2(105, -1180), new Vector2(-260, -1760), 2.4f, 64);
        yield return new("03_fire_lookout_switchback.png",
            new Vector2(510, -3570), new Vector2(1030, -3260), 3.0f, 66);
        yield return new("04_logging_road_junction.png",
            new Vector2(-1040, -2500), new Vector2(-1600, -2460), 2.4f, 64);
        yield return new("05_dam_service_approach.png",
            new Vector2(380, -2350), new Vector2(-40, -1975), 2.5f, 62);
        yield return new("06_granite_narrows_trail.png",
            new Vector2(1540, -2443), new Vector2(1930, -2385), 2.05f, 68);
        yield return new("07_mill_creek_grotto_trail.png",
            new Vector2(-2730, 1090), new Vector2(-2685, 1045), 2.0f, 68);
        yield return new("08_old_growth_hollow_trail.png",
            new Vector2(2782, -280), new Vector2(2700, -270), 2.0f, 68);
    }
}
