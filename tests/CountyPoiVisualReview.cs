#nullable enable

using System;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Renders representative county locations for direct art review.</summary>
public partial class CountyPoiVisualReview : Node
{
    private readonly record struct Shot(string Scene, string File, Vector3 Camera, Vector3 Target, float Fov);

    public override async void _Ready()
    {
        try
        {
            var viewport = new SubViewport
            {
                Name = "CountyPoiViewport",
                Size = new Vector2I(1600, 900),
                OwnWorld3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            AddChild(viewport);
            viewport.AddChild(BuildSun());
            viewport.AddChild(BuildEnvironment());
            viewport.AddChild(BuildGround());

            var camera = new Camera3D { Current = true, Near = 0.08f, Far = 1200, Fov = 55 };
            viewport.AddChild(camera);

            string output = ProjectSettings.GlobalizePath("res://.godot/county_poi_review");
            DirAccess.MakeDirRecursiveAbsolute(output);
            Shot[] shots =
            {
                new("hospital.tscn", "01_hospital.png", new Vector3(61, 9, 59), new Vector3(3, 4.5f, 7), 55),
                new("service_station.tscn", "02_service_station.png", new Vector3(49, 7, 45), new Vector3(5, 3.2f, 5), 57),
                new("pine_ridge.tscn", "03_pine_ridge.png", new Vector3(62, 8, 45), new Vector3(0, 3.8f, 0), 58),
                new("logging_camp.tscn", "04_logging_camp.png", new Vector3(76, 10, 66), new Vector3(0, 3.8f, -2), 58),
                new("mill_creek.tscn", "05_mill_creek.png", new Vector3(72, 11, 69), new Vector3(0, 4.5f, 4), 58),
                new("blackwater_dam.tscn", "06_blackwater_dam.png", new Vector3(120, 23, 81), new Vector3(4, -1, 3), 54),
                new("trailer_park.tscn", "07_trailer_park.png", new Vector3(81, 10, 82), new Vector3(0, 3, 0), 58),
                new("county_fairgrounds.tscn", "08_fairgrounds.png", new Vector3(95, 14, 104), new Vector3(0, 3, 4), 57),
                new("sheriffs_office.tscn", "09_sheriffs_office.png", new Vector3(49, 7, 48), new Vector3(0, 3.5f, 5), 55),
                new("fire_lookout.tscn", "10_fire_lookout.png", new Vector3(54, 25, 59), new Vector3(0, 15, 0), 54),
                new("farm_district.tscn", "11_farm_district.png", new Vector3(330, 105, 350), new Vector3(0, 3, 0), 49),
                new("railway_crossing.tscn", "12_railway_crossing.png", new Vector3(70, 10, 61), new Vector3(0, 3, 0), 56),
                new("south_farmland.tscn", "13_south_farmland.png", new Vector3(385, 120, 400), new Vector3(0, 3, 0), 48),
            };

            Node3D? location = null;
            foreach (Shot shot in shots)
            {
                location?.QueueFree();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                location = GD.Load<PackedScene>("res://scenes/world/county/locations/" + shot.Scene).Instantiate<Node3D>();
                viewport.AddChild(location);
                camera.Position = shot.Camera;
                camera.Fov = shot.Fov;
                camera.LookAt(shot.Target, Vector3.Up);
                for (int frame = 0; frame < 8; frame++)
                {
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Error error = viewport.GetTexture().GetImage().SavePng(Path.Combine(output, shot.File));
                if (error != Error.Ok) throw new InvalidOperationException($"Could not save {shot.File}: {error}");
            }

            GD.Print($"COUNTY_POI_VISUAL_REVIEW: PASS renders={shots.Length} output={output}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_POI_VISUAL_REVIEW: FAIL - " + exception);
            GetTree().Quit(1);
        }
    }

    private static DirectionalLight3D BuildSun() => new()
    {
        RotationDegrees = new Vector3(-52, -38, 0),
        LightColor = new Color(1.0f, 0.83f, 0.66f),
        LightEnergy = 1.45f,
        ShadowEnabled = true,
        ShadowBlur = 1.3f,
        DirectionalShadowMaxDistance = 450,
    };

    private static WorldEnvironment BuildEnvironment()
    {
        var skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.15f, 0.255f, 0.405f),
            SkyHorizonColor = new Color(0.86f, 0.58f, 0.36f),
            GroundBottomColor = new Color(0.05f, 0.065f, 0.052f),
            GroundHorizonColor = new Color(0.32f, 0.3f, 0.25f),
            UseDebanding = true,
        };
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = skyMaterial },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 1.05f,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            TonemapExposure = 1.05f,
            TonemapWhite = 6.0f,
        };
        return new WorldEnvironment { Environment = environment };
    }

    private static MeshInstance3D BuildGround() => new()
    {
        Name = "ReviewGround",
        Position = new Vector3(0, -0.08f, 0),
        Mesh = new BoxMesh { Size = new Vector3(900, 0.1f, 900) },
        MaterialOverride = GD.Load<Material>("res://assets/materials/grass_ground.tres"),
    };
}
