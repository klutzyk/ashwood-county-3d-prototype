#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// A single render aimed wherever you point it, for chasing LOD seam artefacts.
///
/// The Blackwater basin walls grew rows of dark triangular teeth. An earlier
/// NO_SKIRT test appeared to rule skirts out, but that test used the Mill Creek
/// smoke framing - which has no teeth in it - so it proved nothing about the
/// lake. This exists so the camera can be aimed at the actual defect from the
/// command line rather than by editing a hard-coded viewpoint.
///
/// EYE_X/EYE_Z/FOCUS_X/FOCUS_Z/EYE_H set the framing; NO_SKIRT=1 and
/// TERRAIN_DEBUG are honoured by the terrain itself.
/// </summary>
public partial class CountySeamProbe : Node3D
{
    public override async void _Ready()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var viewport = new SubViewport
            {
                Name = "SeamViewport",
                Size = new Vector2I(1600, 900),
                OwnWorld3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            AddChild(viewport);

            var eye = new Vector2(Env("EYE_X", -820.0f), Env("EYE_Z", -2400.0f));
            var focus = new Vector2(Env("FOCUS_X", 300.0f), Env("FOCUS_Z", -2400.0f));
            float eyeHeight = Env("EYE_H", 55.0f);

            var probe = new Node3D { Name = "StreamProbe" };
            viewport.AddChild(probe);
            probe.Position = new Vector3(eye.X, CountyMap.Height(eye.X, eye.Y), eye.Y);

            CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(probe);
            if (OS.GetEnvironment("NO_SKIRT") == "1")
            {
                foreach (Node child in built.World.GetChildren())
                {
                    if (child is CountyTerrain terrain)
                    {
                        terrain.EnableSkirts = false;
                    }
                }
            }

            viewport.AddChild(built.Root);

            var camera = new Camera3D
            {
                Name = "SeamCamera",
                Current = true,
                Near = 0.25f,
                Far = 9000.0f,
                Fov = 58.0f,
            };
            viewport.AddChild(camera);
            camera.Position = new Vector3(eye.X, CountyMap.Height(eye.X, eye.Y) + eyeHeight, eye.Y);
            camera.LookAt(
                new Vector3(focus.X, CountyMap.Height(focus.X, focus.Y) + 10.0f, focus.Y),
                Vector3.Up);

            for (int frame = 0; frame < 150; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            string directory = ProjectSettings.GlobalizePath("res://.godot/county_seam");
            DirAccess.MakeDirRecursiveAbsolute(directory);
            string name = OS.GetEnvironment("SHOT_NAME") is string s && s.Length > 0 ? s : "seam";

            Image image = viewport.GetTexture().GetImage();
            Error error = image.SavePng(Path.Combine(directory, name + ".png"));
            if (error != Error.Ok)
            {
                throw new InvalidOperationException($"Could not save {name}.png: {error}");
            }

            GD.Print($"SEAM: PASS - {directory}/{name}.png in {stopwatch.ElapsedMilliseconds}ms");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("SEAM: FAIL - " + exception);
            GetTree().Quit(1);
        }
    }

    private static float Env(string key, float fallback)
    {
        string raw = OS.GetEnvironment(key);
        return raw.Length > 0 && float.TryParse(raw, out float parsed) ? parsed : fallback;
    }
}
