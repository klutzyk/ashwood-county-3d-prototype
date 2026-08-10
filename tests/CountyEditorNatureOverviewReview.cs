#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Renders and validates the all-county editor vegetation representation.</summary>
public partial class CountyEditorNatureOverviewReview : Node
{
    public override async void _Ready()
    {
        try
        {
            var viewport = new SubViewport
            {
                Name = "OverviewViewport",
                Size = new Vector2I(1280, 720),
                OwnWorld3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            AddChild(viewport);

            var probe = new Node3D { Name = "EditorProbe" };
            viewport.AddChild(probe);
            var excluded = new HashSet<string>(StringComparer.Ordinal)
            {
                "CountyWater", "CountyRoads", "CountyLocations",
                "CountyNaturalFeatures", "CountyPointsOfInterest",
            };
            CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(
                probe, editorPreview: true, excludedSubsystems: excluded);
            built.World.EditorPreviewRadius = 2;
            viewport.AddChild(built.Root);

            var camera = new Camera3D
            {
                Name = "OverviewCamera",
                Current = true,
                Near = 1.0f,
                Far = 12000.0f,
                Fov = 52.0f,
            };
            viewport.AddChild(camera);
            camera.GlobalTransform = CountySceneBuilder.LookAt(
                new Vector2(CountyMap.CenterX, CountyMap.CenterZ + 1150.0f),
                new Vector2(CountyMap.CenterX, CountyMap.CenterZ - 450.0f),
                5100.0f);

            CountyVegetation vegetation = built.World.GetNode<CountyVegetation>("CountyVegetation");
            CountyFarTerrain farTerrain = built.World.GetNode<CountyFarTerrain>("CountyFarTerrain");
            farTerrain.ForceAllVisible = true;

            bool ready = false;
            for (int frame = 0; frame < 1200; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (frame >= 40 && vegetation.EditorOverviewReady && farTerrain.IsComplete)
                {
                    ready = true;
                    break;
                }
            }

            Require(ready, "County-wide editor canopy did not finish building.");
            Require(vegetation.EditorOverviewTreeCount >= 3000,
                $"Editor canopy contains only {vegetation.EditorOverviewTreeCount} trees.");

            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string directory = ProjectSettings.GlobalizePath("res://.godot/county_nature_review");
            DirAccess.MakeDirRecursiveAbsolute(directory);
            string path = Path.Combine(directory, "county_editor_nature_overview.png");
            Error error = viewport.GetTexture().GetImage().SavePng(path);
            Require(error == Error.Ok, $"Could not save editor overview: {error}");

            camera.GlobalTransform = CountySceneBuilder.LookAt(
                new Vector2(3000.0f, -250.0f),
                new Vector2(2200.0f, -1050.0f),
                280.0f);
            camera.Fov = 58.0f;
            for (int frame = 0; frame < 8; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            string obliquePath = Path.Combine(directory, "county_editor_eastern_woodlands.png");
            error = viewport.GetTexture().GetImage().SavePng(obliquePath);
            Require(error == Error.Ok, $"Could not save eastern overview: {error}");

            GD.Print($"COUNTY_EDITOR_NATURE: PASS trees={vegetation.EditorOverviewTreeCount} " +
                     $"paths={path},{obliquePath}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_EDITOR_NATURE: FAIL - " + exception);
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
