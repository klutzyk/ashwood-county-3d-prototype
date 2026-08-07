#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Renders hero views of the whole open county so the world can be judged as
/// images rather than by claim.
///
/// The camera is teleported to each viewpoint and the streamer is given time to
/// resolve that region before the shutter opens, because a screenshot taken mid
/// stream shows holes that are not really there and hides problems that are.
/// </summary>
public partial class CountyVisualReview : Node3D
{
    private readonly record struct Shot(
        string FileName,
        string Description,
        Vector2 From,
        Vector2 To,
        float EyeHeight,
        float Fov);

    private const int Width = 1920;
    private const int Height = 1080;

    /// <summary>
    /// Frames to wait after moving the camera. Chunk builds are handed to
    /// background tasks, so the world needs several frames to catch up before a
    /// capture is representative.
    /// </summary>
    private const int SettleFrames = 90;

    public override async void _Ready()
    {
        try
        {
            var viewport = new SubViewport
            {
                Name = "CaptureViewport",
                Size = new Vector2I(Width, Height),
                OwnWorld3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            AddChild(viewport);

            var probe = new Node3D { Name = "StreamProbe" };
            viewport.AddChild(probe);

            CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(probe);
            viewport.AddChild(built.Root);

            GD.Print($"COUNTY_REVIEW: subsystems present [{string.Join(", ", built.Present)}]");
            if (built.Missing.Count > 0)
            {
                GD.Print($"COUNTY_REVIEW: subsystems MISSING [{string.Join(", ", built.Missing)}]");
            }

            var camera = new Camera3D
            {
                Name = "ReviewCamera",
                Current = true,
                Near = 0.25f,
                Far = 9000.0f,
            };
            viewport.AddChild(camera);

            string outputDirectory = ProjectSettings.GlobalizePath("res://.godot/county_review");
            DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

            foreach (Shot shot in BuildShots())
            {
                // Move the streaming probe first so the world starts resolving the
                // region while the camera is still being placed.
                probe.GlobalPosition = new Vector3(
                    shot.From.X, CountyMap.Height(shot.From.X, shot.From.Y), shot.From.Y);

                Transform3D view = CountySceneBuilder.LookAt(shot.From, shot.To, shot.EyeHeight);
                camera.GlobalTransform = view;
                camera.Fov = shot.Fov;

                for (int frame = 0; frame < SettleFrames; frame++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }

                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

                Image image = viewport.GetTexture().GetImage();
                Error error = image.SavePng(Path.Combine(outputDirectory, shot.FileName));
                if (error != Error.Ok)
                {
                    throw new InvalidOperationException($"Could not save {shot.FileName}: {error}");
                }

                GD.Print($"COUNTY_REVIEW: {shot.FileName} - {shot.Description}");
            }

            GD.Print($"COUNTY_REVIEW: PASS - renders in {outputDirectory}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_REVIEW: FAIL - " + exception);
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Viewpoints chosen to cover every claim the concept maps make: the scale of
    /// the valley, each named landmark, and the transitions between biomes that
    /// are the usual places a procedural world falls apart.
    /// </summary>
    private static IEnumerable<Shot> BuildShots()
    {
        yield return new Shot(
            "01_town_west_to_bridge.png",
            "Ashwood looking west down Main Street toward the Old Mill Bridge",
            new Vector2(120.0f, 0.0f), new Vector2(-600.0f, -20.0f), 2.4f, 62.0f);

        yield return new Shot(
            "02_valley_from_east_ridge.png",
            "The Blackwater valley and town from the eastern shoulder",
            new Vector2(1500.0f, -400.0f), new Vector2(-400.0f, 200.0f), 60.0f, 52.0f);

        // From the western shore looking across the reservoir. The previous framing
        // stood north of the dam looking south, which put the dam's own ridge
        // between the camera and the water - the lake was never actually in shot.
        yield return new Shot(
            "03_lake_and_dam.png",
            "Blackwater Lake from the western shore",
            new Vector2(-820.0f, -2400.0f), new Vector2(300.0f, -2400.0f), 55.0f, 58.0f);

        yield return new Shot(
            "04_fire_lookout_summit.png",
            "From Fire Lookout at 1380m, the highest point in the county",
            new Vector2(958.0f, -3048.0f), new Vector2(-200.0f, -1200.0f), 30.0f, 60.0f);

        yield return new Shot(
            "05_northern_forest.png",
            "Inside the dense northern conifer forest",
            new Vector2(-900.0f, -2000.0f), new Vector2(-500.0f, -2400.0f), 2.2f, 65.0f);

        yield return new Shot(
            "06_farm_district.png",
            "Farm District field parcels and hedgerows",
            new Vector2(-1500.0f, -100.0f), new Vector2(-2200.0f, -500.0f), 14.0f, 58.0f);

        yield return new Shot(
            "07_highway_16.png",
            "State Highway 16, the only road in or out",
            new Vector2(1200.0f, 470.0f), new Vector2(-1600.0f, 800.0f), 3.0f, 55.0f);

        yield return new Shot(
            "08_mill_creek.png",
            "Mill Creek settlement in the southwest",
            new Vector2(-1750.0f, 1450.0f), new Vector2(-2104.0f, 1702.0f), 18.0f, 55.0f);

        yield return new Shot(
            "09_southern_canyon.png",
            "The Blackwater in its southern canyon",
            new Vector2(-600.0f, 1900.0f), new Vector2(-900.0f, 2600.0f), 40.0f, 58.0f);

        yield return new Shot(
            "10_fairgrounds_and_rail.png",
            "County Fairgrounds and the freight line",
            new Vector2(300.0f, 2100.0f), new Vector2(-300.0f, 2450.0f), 20.0f, 55.0f);

        yield return new Shot(
            "11_trailer_park.png",
            "Trailer Park in the southeast",
            new Vector2(1250.0f, 1350.0f), new Vector2(1504.0f, 1598.0f), 12.0f, 55.0f);

        yield return new Shot(
            "12_county_panorama.png",
            "The county from altitude, matching the aerial concept",
            new Vector2(-300.0f, 3200.0f), new Vector2(-300.0f, -2200.0f), 900.0f, 62.0f);

        yield return new Shot(
            "13_logging_camp.png",
            "Logging Camp in the northwest forest",
            new Vector2(-1600.0f, -2300.0f), new Vector2(-1902.0f, -2448.0f), 16.0f, 55.0f);

        yield return new Shot(
            "14_pine_ridge.png",
            "Pine Ridge mountain village at 1210m",
            new Vector2(0.0f, -3300.0f), new Vector2(-262.0f, -3402.0f), 20.0f, 55.0f);
    }
}
