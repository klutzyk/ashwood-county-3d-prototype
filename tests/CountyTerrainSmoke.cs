#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// A fast, single-viewpoint check that the county actually meshes.
///
/// The full CountyVisualReview teleports across fourteen viewpoints and rebuilds
/// the entire resident chunk set at each one, which is far too slow a loop to
/// iterate a shader on. This builds one region, reports what it produced, and
/// renders one frame - so a broken material or an empty world shows up in
/// seconds rather than minutes.
/// </summary>
public partial class CountyTerrainSmoke : Node3D
{
    public override async void _Ready()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            var viewport = new SubViewport
            {
                Name = "CaptureViewport",
                Size = new Vector2I(1600, 900),
                OwnWorld3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            };
            AddChild(viewport);

            // A ridge east of town looking back across the Blackwater valley: it
            // takes in mountains, forest, the river and the town flat in one frame,
            // so a single image covers most of what can go wrong.
            var eye = new Vector2(-980.0f, -1980.0f);
            var focus = new Vector2(-420.0f, -2280.0f);

            var probe = new Node3D { Name = "StreamProbe" };
            viewport.AddChild(probe);
            probe.Position = new Vector3(eye.X, CountyMap.Height(eye.X, eye.Y), eye.Y);

            CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(probe);
            foreach (Node n in built.World.GetChildren())
            {
                if (n is CountyTerrain t && OS.GetEnvironment("NO_SKIRT") == "1")
                {
                    t.EnableSkirts = false;
                }
            }
            viewport.AddChild(built.Root);

            GD.Print($"SMOKE: subsystems [{string.Join(", ", built.Present)}]");
            if (built.Missing.Count > 0)
            {
                GD.Print($"SMOKE: MISSING [{string.Join(", ", built.Missing)}]");
            }

            var camera = new Camera3D
            {
                Name = "SmokeCamera",
                Current = true,
                Near = 0.25f,
                Far = 9000.0f,
                Fov = 55.0f,
            };
            viewport.AddChild(camera);
            // Node3D.LookAt rather than a hand-built basis: Basis.LookingAt's
            // forward convention is the opposite of a camera's, which put the eye
            // under the terrain looking up at the undersides of trees.
            float eyeY = CountyMap.Height(eye.X, eye.Y) + 30.0f;
            camera.Position = new Vector3(eye.X, eyeY, eye.Y);
            camera.LookAt(new Vector3(focus.X, CountyMap.Height(focus.X, focus.Y) + 4.0f, focus.Y),
                Vector3.Up);
            GD.Print($"SMOKE: eye=({eye.X:F0}, {eyeY:F1}, {eye.Y:F0}) " +
                     $"ground={CountyMap.Height(eye.X, eye.Y):F1}");

            // Let the streamer resolve. Reported so a slow build is visible as a
            // number rather than as an unexplained wait.
            for (int frame = 0; frame < 240; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            GD.Print($"SMOKE: settled in {stopwatch.ElapsedMilliseconds}ms");
            ReportCounts(built.Root);
            VerifyMeshMatchesHeightfield(built.Root);

            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            string directory = ProjectSettings.GlobalizePath("res://.godot/county_smoke");
            DirAccess.MakeDirRecursiveAbsolute(directory);
            Image image = viewport.GetTexture().GetImage();
            Error error = image.SavePng(Path.Combine(directory, "smoke.png"));
            if (error != Error.Ok)
            {
                throw new InvalidOperationException($"Could not save smoke.png: {error}");
            }

            GD.Print($"SMOKE: PASS - {directory}/smoke.png in {stopwatch.ElapsedMilliseconds}ms");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("SMOKE: FAIL - " + exception);
            GetTree().Quit(1);
        }
    }

    /// <summary>
    /// Compares actual mesh vertices against CountyMap.Height. If these disagree,
    /// every camera placed from the height function ends up in the wrong place and
    /// no amount of reframing will help.
    /// </summary>
    private static void VerifyMeshMatchesHeightfield(Node root)
    {
        int checkedVertices = 0;
        float worstError = 0.0f;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        void Walk(Node node)
        {
            if (node is MeshInstance3D instance && instance.Mesh is ArrayMesh mesh &&
                node.GetParent() is Node3D holder && holder.Name.ToString().StartsWith("Chunk_") &&
                checkedVertices < 400)
            {
                Godot.Collections.Array arrays = mesh.SurfaceGetArrays(0);
                if (arrays[(int)Mesh.ArrayType.Vertex].Obj is Vector3[] vertices)
                {
                    for (int i = 0; i < vertices.Length && checkedVertices < 400; i += 37)
                    {
                        Vector3 v = vertices[i];
                        minY = Mathf.Min(minY, v.Y);
                        maxY = Mathf.Max(maxY, v.Y);

                        // Skirt vertices hang below the surface by design, so only
                        // compare ones that should sit exactly on it.
                        float expected = CountyMap.Height(v.X, v.Z);
                        float error = v.Y - expected;
                        if (error > -0.01f)
                        {
                            worstError = Mathf.Max(worstError, Mathf.Abs(error));
                            checkedVertices++;
                        }
                    }
                }
            }

            foreach (Node child in node.GetChildren())
            {
                Walk(child);
            }
        }

        Walk(root);
        GD.Print($"SMOKE: mesh_vs_heightfield checked={checkedVertices} " +
                 $"worst_error={worstError:F3}m vertexY=[{minY:F1} .. {maxY:F1}]");
    }

    /// <summary>Walks the built world and totals what actually reached the tree.</summary>
    private static void ReportCounts(Node root)
    {
        int meshes = 0;
        int multiMeshes = 0;
        int multiMeshInstances = 0;
        long triangles = 0;
        int bodies = 0;

        void Walk(Node node)
        {
            switch (node)
            {
                case MultiMeshInstance3D multi when multi.Multimesh?.Mesh != null:
                    multiMeshes++;
                    multiMeshInstances += multi.Multimesh.InstanceCount;
                    triangles += TriangleCount(multi.Multimesh.Mesh) * multi.Multimesh.InstanceCount;
                    break;

                case MeshInstance3D mesh when mesh.Mesh != null:
                    meshes++;
                    triangles += TriangleCount(mesh.Mesh);
                    break;

                case StaticBody3D:
                    bodies++;
                    break;
            }

            foreach (Node child in node.GetChildren())
            {
                Walk(child);
            }
        }

        Walk(root);

        GD.Print($"SMOKE: meshes={meshes} multimesh_batches={multiMeshes} " +
                 $"instances={multiMeshInstances} collision_bodies={bodies}");
        GD.Print($"SMOKE: triangles={triangles:N0} draw_calls~{meshes + multiMeshes}");
    }

    private static long TriangleCount(Mesh mesh)
    {
        long total = 0;
        for (int surface = 0; surface < mesh.GetSurfaceCount(); surface++)
        {
            Godot.Collections.Array arrays = mesh.SurfaceGetArrays(surface);
            if (arrays.Count <= (int)Mesh.ArrayType.Index)
            {
                continue;
            }

            if (arrays[(int)Mesh.ArrayType.Index].Obj is int[] indices && indices.Length > 0)
            {
                total += indices.Length / 3;
            }
            else if (arrays[(int)Mesh.ArrayType.Vertex].Obj is Vector3[] vertices)
            {
                total += vertices.Length / 3;
            }
        }

        return total;
    }
}
