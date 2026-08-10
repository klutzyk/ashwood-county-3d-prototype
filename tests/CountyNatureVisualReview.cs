#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Player-height review renders for every authored wilderness landmark.</summary>
public partial class CountyNatureVisualReview : Node
{
    private const int Width = 960;
    private const int Height = 540;

    public override async void _Ready()
    {
        try
        {
            var viewport = new SubViewport
            {
                Name = "NatureViewport",
                Size = new Vector2I(Width, Height),
                OwnWorld3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            AddChild(viewport);

            var probe = new Node3D { Name = "StreamProbe" };
            viewport.AddChild(probe);
            var excluded = new HashSet<string>(StringComparer.Ordinal)
            {
                "CountyFarTerrain",
                "CountyWater",
                "CountyRoads",
                "CountyLocations",
                "CountyPointsOfInterest",
            };
            CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(
                probe, excludedSubsystems: excluded);
            built.World.GetNode<CountyTerrain>("CountyTerrain").TerrainRadius = 2;
            built.World.GetNode<CountyVegetation>("CountyVegetation").VegetationRadius = 2;
            built.World.GetNode<CountyNaturalFeatures>("CountyNaturalFeatures").FeatureRadius = 2;
            viewport.AddChild(built.Root);

            Require(built.Present.Contains(nameof(CountyNaturalFeatures)),
                "CountyNaturalFeatures was not included in the assembled world.");

            var camera = new Camera3D
            {
                Name = "NatureCamera",
                Current = true,
                Near = 0.15f,
                Far = 2600.0f,
                Fov = 63.0f,
            };
            viewport.AddChild(camera);

            string directory = ProjectSettings.GlobalizePath("res://.godot/county_nature_review");
            DirAccess.MakeDirRecursiveAbsolute(directory);
            string requested = OS.GetEnvironment("COUNTY_NATURE_SHOT");

            for (int i = 0; i < CountyMap.NaturalFeatures.Length; i++)
            {
                CountyMap.NaturalFeature feature = CountyMap.NaturalFeatures[i];
                string file = $"{i + 1:D2}_{Slug(feature.Name)}.png";
                if (requested.Length > 0 && !file.StartsWith(requested, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                float yaw = Mathf.DegToRad(feature.YawDegrees);
                Vector2 forward = new(Mathf.Sin(yaw), Mathf.Cos(yaw));
                float distance = feature.Kind switch
                {
                    CountyMap.NaturalFeatureKind.RockFormation => feature.Radius + 55.0f,
                    CountyMap.NaturalFeatureKind.Overlook => feature.Radius + 38.0f,
                    CountyMap.NaturalFeatureKind.Escarpment => feature.Radius + 46.0f,
                    CountyMap.NaturalFeatureKind.OldGrowth => 62.0f,
                    _ => 52.0f,
                };
                Vector2 from = feature.Position + forward * distance;
                Vector2 to = feature.Position + forward * 3.0f;
                probe.GlobalPosition = CountyMap.GroundPoint(feature.Position.X, feature.Position.Y);
                camera.GlobalTransform = ReviewTransform(from, to);

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
                if (!settled)
                {
                    throw new InvalidOperationException(
                        $"Streaming did not settle at {feature.Name} within 900 frames.");
                }

                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Image image = viewport.GetTexture().GetImage();
                Error error = image.SavePng(Path.Combine(directory, file));
                Require(error == Error.Ok, $"Could not save {file}: {error}");
                GD.Print($"COUNTY_NATURE_REVIEW: {file} kind={feature.Kind} " +
                         $"height={CountyMap.Height(feature.Position.X, feature.Position.Y):F1}m");
            }

            GD.Print($"COUNTY_NATURE_REVIEW: PASS directory={directory}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_NATURE_REVIEW: FAIL - " + exception);
            GetTree().Quit(1);
        }
    }

    private static string Slug(string value) => value.ToLowerInvariant().Replace(" ", "_");

    private static Transform3D ReviewTransform(Vector2 from, Vector2 to)
    {
        // Height() contains detail finer than a streamed terrain cell. Sample the
        // immediate footprint so the review camera cannot land beneath a coarse
        // triangle on strongly folded ground.
        float eyeY = CountyMap.Height(from.X, from.Y);
        foreach (Vector2 offset in new[]
                 {
                     new Vector2(18, 0), new Vector2(-18, 0),
                     new Vector2(0, 18), new Vector2(0, -18),
                     new Vector2(13, 13), new Vector2(-13, 13),
                     new Vector2(13, -13), new Vector2(-13, -13),
                 })
        {
            eyeY = Mathf.Max(eyeY, CountyMap.Height(from.X + offset.X, from.Y + offset.Y));
        }

        Vector3 eye = new(from.X, eyeY + 3.2f, from.Y);
        Vector3 focus = new(to.X, CountyMap.Height(to.X, to.Y) + 4.0f, to.Y);
        return new Transform3D(Basis.Identity, eye).LookingAt(focus, Vector3.Up);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
