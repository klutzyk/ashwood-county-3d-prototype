#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tools;

/// <summary>
/// Bakes billboard imposters for the county's trees.
///
/// A forest is the one thing an open world cannot fake with ground texture. The
/// terrain material can colour a hillside like canopy, but colour has no
/// silhouette - no trees break the skyline, nothing occludes anything, and the
/// result reads as green paint however carefully it is tuned. Two full shader
/// tuning passes were spent confirming that before this was written.
///
/// Real geometry cannot cover the distance either: the jacaranda is 19k triangles
/// at LOD0 and 10.6k at LOD1, so the couple of hundred trees a dense stand needs
/// would cost more than the entire rest of the frame on the integrated GPU this
/// targets. An imposter is two triangles.
///
/// The bake renders each tree from eight angles evenly spaced around the compass
/// into one horizontal atlas strip, with alpha. At runtime a camera-facing quad
/// picks the cell nearest the current view angle, so a tree still turns correctly
/// as you walk around it rather than spinning to follow you.
///
/// Run with a display context - a headless Godot has no renderer and silently
/// bakes eight transparent cells.
/// </summary>
public partial class BakeTreeImposters : Node3D
{
    /// <summary>
    /// Angles around the compass. Eight is the usual sweet spot: four is visibly
    /// steppy as you circle a stand, sixteen doubles atlas width for a difference
    /// nobody sees at the distances imposters are used.
    /// </summary>
    private const int AngleCount = 8;

    /// <summary>Pixels per atlas cell. Trees are rarely more than ~80px tall on screen at imposter range.</summary>
    private const int CellSize = 256;

    private readonly record struct Subject(string Name, string ScenePath);

    /// <summary>
    /// Baked from the LOD1 mesh, not LOD0: the atlas cell is 256px and the extra
    /// detail in LOD0 is below a texel at that size, so it would cost bake time
    /// and memory for a result no different.
    /// </summary>
    private static readonly Subject[] Subjects =
    {
        new("jacaranda", "res://assets/environment/nature/polyhaven/ashwood_jacaranda_lod1.tscn"),
        new("fir_a", "res://assets/environment/nature/polyhaven/ashwood_fir_a_lod1.tscn"),
        new("fir_b", "res://assets/environment/nature/polyhaven/ashwood_fir_b_lod1.tscn"),
        new("fir_c", "res://assets/environment/nature/polyhaven/ashwood_fir_c_lod1.tscn"),
        new("pine_a", "res://assets/environment/nature/polyhaven/ashwood_pine_a_lod1.tscn"),
        new("pine_b", "res://assets/environment/nature/polyhaven/ashwood_pine_b_lod1.tscn"),
        new("pine_c", "res://assets/environment/nature/polyhaven/ashwood_pine_c_lod1.tscn"),
    };

    public override async void _Ready()
    {
        try
        {
            string outputDirectory = ProjectSettings.GlobalizePath("res://assets/environment/nature/imposters");
            DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

            var baked = new System.Collections.Generic.List<string>();
            foreach (Subject subject in Subjects)
            {
                await BakeSubject(subject, outputDirectory);
                if (subject.Name != "jacaranda")
                {
                    baked.Add(subject.Name);
                }
            }

            ComposeConiferAtlas(baked, outputDirectory);

            GD.Print("IMPOSTER_BAKE: PASS");
            GetTree().Quit(0);
        }
        catch (Exception error)
        {
            GD.PushError("IMPOSTER_BAKE: FAIL - " + error);
            GetTree().Quit(1);
        }
    }

    private async System.Threading.Tasks.Task BakeSubject(Subject subject, string outputDirectory)
    {
        if (!ResourceLoader.Exists(subject.ScenePath))
        {
            throw new InvalidOperationException($"Missing tree scene {subject.ScenePath}");
        }

        var packed = ResourceLoader.Load<PackedScene>(subject.ScenePath);

        var viewport = new SubViewport
        {
            Name = $"Bake_{subject.Name}",
            Size = new Vector2I(CellSize, CellSize),
            OwnWorld3D = true,

            // Alpha is the whole point: the atlas must carry the tree's silhouette,
            // so the background has to be genuinely empty rather than sky-coloured.
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        AddChild(viewport);

        Node3D instance = packed.Instantiate<Node3D>();
        viewport.AddChild(instance);

        // Collision bodies would not render anyway, but a StaticBody3D root also
        // drags physics into the bake for no reason.
        DisablePhysics(instance);

        Aabb bounds = ComputeBounds(instance);
        if (bounds.Size.Y <= 0.001f)
        {
            throw new InvalidOperationException($"{subject.Name} has no visible geometry to bake");
        }

        // Flat white ambient and no directional light, so what lands in the atlas is
        // the tree's albedo rather than the tree under one particular sun. The
        // county runs a day/night cycle; a bake with directional shading welded in
        // would keep its noon highlights at midnight, and would contradict the
        // real trees standing next to it. The imposter shader relights this.
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.ClearColor,
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(1.0f, 1.0f, 1.0f),
            AmbientLightEnergy = 1.0f,

            // Linear, not ACES: tonemapping here would bake a curve into the albedo
            // that the scene then applies a second time.
            TonemapMode = Godot.Environment.ToneMapper.Linear,
            TonemapExposure = 1.0f,
        };
        viewport.AddChild(new WorldEnvironment { Environment = environment });

        // Orthogonal, because an imposter is used at distances where perspective
        // across a single tree is negligible - and a perspective bake would lock in
        // a foreshortening that is wrong everywhere except the baked distance.
        float radius = Mathf.Max(
            Mathf.Max(bounds.Size.X, bounds.Size.Z) * 0.5f,
            bounds.Size.Y * 0.5f);
        var camera = new Camera3D
        {
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = radius * 2.0f,
            Near = 0.05f,
            Far = radius * 8.0f,
            Current = true,
        };
        viewport.AddChild(camera);

        Vector3 centre = bounds.Position + (bounds.Size * 0.5f);
        float distance = radius * 3.0f;

        var atlas = Image.CreateEmpty(CellSize * AngleCount, CellSize, false, Image.Format.Rgba8);
        atlas.Fill(new Color(0, 0, 0, 0));

        for (int angle = 0; angle < AngleCount; angle++)
        {
            float yaw = Mathf.Tau * angle / AngleCount;
            var offset = new Vector3(Mathf.Sin(yaw), 0.0f, Mathf.Cos(yaw)) * distance;
            camera.Position = centre + offset;
            camera.LookAt(centre, Vector3.Up);

            // Several frames: the first is often produced before the instance's
            // materials and the light have both settled.
            for (int frame = 0; frame < 4; frame++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            Image cell = viewport.GetTexture().GetImage();
            cell.Convert(Image.Format.Rgba8);
            atlas.BlitRect(
                cell,
                new Rect2I(0, 0, CellSize, CellSize),
                new Vector2I(angle * CellSize, 0));
        }

        string atlasPath = Path.Combine(outputDirectory, $"{subject.Name}_imposter.png");
        Error saved = atlas.SavePng(atlasPath);
        if (saved != Error.Ok)
        {
            throw new InvalidOperationException($"Could not save {atlasPath}: {saved}");
        }

        // The quad must be the size of the tree it replaces, so the dimensions are
        // part of the bake output rather than something the caller guesses.
        GD.Print($"IMPOSTER_BAKE: {subject.Name} " +
                 $"width={bounds.Size.X:F2} height={bounds.Size.Y:F2} depth={bounds.Size.Z:F2} " +
                 $"base_offset={centre.Y - bounds.Position.Y:F2} cells={AngleCount} -> {atlasPath}");

        viewport.QueueFree();
    }

    /// <summary>
    /// Stacks every conifer atlas into one texture, one species per row.
    ///
    /// Six species meant six materials, six meshes and therefore six MultiMesh
    /// batches per chunk. Across a ring-6 imposter set that is over a thousand
    /// draw calls for the trees alone, and this world is draw-call bound - it runs
    /// at the same speed at 480x270 as at 1920x1080, so submission cost, not
    /// shading, is what is spending the frame.
    ///
    /// One atlas collapses that back to a single batch per chunk. It works because
    /// every conifer bake is a square of side equal to the tree's height, centred
    /// at half its height, so one normalised card fits all of them and the species
    /// difference is purely which row to sample.
    /// </summary>
    private static void ComposeConiferAtlas(
        System.Collections.Generic.List<string> names, string outputDirectory)
    {
        if (names.Count == 0)
        {
            return;
        }

        var combined = Image.CreateEmpty(
            CellSize * AngleCount, CellSize * names.Count, false, Image.Format.Rgba8);
        combined.Fill(new Color(0, 0, 0, 0));

        for (int row = 0; row < names.Count; row++)
        {
            string path = Path.Combine(outputDirectory, names[row] + "_imposter.png");
            Image? strip = Image.LoadFromFile(path);
            if (strip == null)
            {
                throw new InvalidOperationException($"Missing baked strip {path}");
            }

            strip.Convert(Image.Format.Rgba8);
            combined.BlitRect(
                strip,
                new Rect2I(0, 0, CellSize * AngleCount, CellSize),
                new Vector2I(0, row * CellSize));
        }

        string combinedPath = Path.Combine(outputDirectory, "conifer_atlas.png");
        Error saved = combined.SavePng(combinedPath);
        if (saved != Error.Ok)
        {
            throw new InvalidOperationException($"Could not save {combinedPath}: {saved}");
        }

        GD.Print($"IMPOSTER_BAKE: conifer_atlas rows=[{string.Join(", ", names)}] " +
                 $"cells={AngleCount} -> {combinedPath}");
    }

    private static void DisablePhysics(Node node)
    {
        if (node is CollisionObject3D body)
        {
            body.ProcessMode = ProcessModeEnum.Disabled;
        }

        foreach (Node child in node.GetChildren())
        {
            DisablePhysics(child);
        }
    }

    /// <summary>World-space bounds of every visual mesh under the instance.</summary>
    private static Aabb ComputeBounds(Node3D root)
    {
        var bounds = new Aabb();
        bool first = true;
        Gather(root);
        return bounds;

        void Gather(Node node)
        {
            if (node is VisualInstance3D visual)
            {
                Aabb local = visual.GetAabb();
                Transform3D relative = root.GlobalTransform.AffineInverse() * visual.GlobalTransform;
                Aabb world = relative * local;
                bounds = first ? world : bounds.Merge(world);
                first = false;
            }

            foreach (Node child in node.GetChildren())
            {
                Gather(child);
            }
        }
    }
}
