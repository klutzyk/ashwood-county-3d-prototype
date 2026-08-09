#nullable enable

using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Wires the full streamed county onto the shipped game scene so Main Street is
/// a walkable part of the open county rather than an isolated block.
///
/// Main Street sits at the county origin, and CountySettlements already keeps
/// procedural structures out of a radius around it, so the hand-authored town
/// and the generated terrain are meant to meet exactly here.
///
/// Marked [Tool] so the county can also be built inside the editor viewport for
/// inspection. Nothing it generates is ever given an Owner, so none of it is
/// written back into the scene file - the county stays procedural and the .tscn
/// stays small.
/// </summary>
[Tool]
public partial class CountyWorldIntegration : Node3D
{
    /// <summary>
    /// Builds the county in the editor viewport. Off by default because streaming
    /// an eight-kilometre world makes editing anything else in the scene slow.
    /// </summary>
    [Export]
    public bool PreviewInEditor
    {
        get => _previewInEditor;
        set
        {
            _previewInEditor = value;
            if (Engine.IsEditorHint() && IsInsideTree())
            {
                RefreshEditorPreview();
            }
        }
    }

    /// <summary>
    /// Where the editor preview streams around, in world XZ. The streamed radius
    /// is only about two kilometres, so this is what decides which part of the
    /// county is resident: (0,0) is Ashwood, (-2104, 1702) is Mill Creek.
    /// </summary>
    [Export]
    public Vector2 PreviewCentre
    {
        get => _previewCentre;
        set
        {
            _previewCentre = value;
            if (Engine.IsEditorHint() && IsInsideTree() && _previewInEditor)
            {
                RefreshEditorPreview();
            }
        }
    }

    /// <summary>
    /// Chunks around PreviewCentre kept resident in the editor. 2 is one built-up
    /// area's worth - enough to judge terrain, water, roads, trees and a
    /// settlement together. Raise it if you need to see further, but each step
    /// widens the resident set a lot faster than it sounds: radius 2 is a 5x5
    /// block of 256m chunks, radius 4 is 9x9 - more than triple the geometry.
    /// </summary>
    [Export(PropertyHint.Range, "1,8,1")]
    public int PreviewRadius
    {
        get => _previewRadius;
        set
        {
            _previewRadius = value;
            if (Engine.IsEditorHint() && IsInsideTree() && _previewInEditor)
            {
                RefreshEditorPreview();
            }
        }
    }

    private Vector2 _previewCentre = Vector2.Zero;
    private int _previewRadius = 2;

    private bool _previewInEditor;
    private Node3D? _built;

    public override void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            RefreshEditorPreview();
            return;
        }

        var player = GetNode<Node3D>("../Player");

        // Bisection guard: run the shipped scene with no county at all, to split a
        // frame cost between the open world and everything that was already here.
        if (OS.GetEnvironment("SKIP_COUNTY") == "1")
        {
            GD.Print("COUNTY: skipped by SKIP_COUNTY=1");
            if (OS.GetEnvironment("COUNTY_DIAG") == "1")
            {
                GetTree().CreateTimer(8.0).Timeout += BeginFrameSampling;
            }

            return;
        }

        CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(player, logStreaming: false);
        AddChild(built.Root);
        _built = built.Root;

        // WorldTime's own _Ready runs earlier in the scene's tree order, before
        // this builds the county's sun and sky, so it could not find them by
        // path. Hand them over directly now that both exist.
        var worldTime = GetNodeOrNull<WorldTime>("../WorldTime");
        var sun = built.Atmosphere.GetNodeOrNull<DirectionalLight3D>("Sun");
        var worldEnvironment = built.Atmosphere.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        if (worldTime != null && sun != null && worldEnvironment?.Environment != null)
        {
            worldTime.AttachToAtmosphere(sun, worldEnvironment.Environment);
        }

        if (OS.GetEnvironment("COUNTY_DIAG") == "1")
        {
            // Optionally teleport the player before sampling, so the diagnostic
            // shot can be taken out in the county rather than on Main Street.
            // Spawn is inside the town exclusion radius, where there is no forest
            // by design, so a shot from there says nothing about the open world.
            string spot = OS.GetEnvironment("DIAG_AT");
            if (spot.Length > 0)
            {
                string[] parts = spot.Split(',');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], out float dx) &&
                    float.TryParse(parts[1], out float dz))
                {
                    player.GlobalPosition = new Vector3(
                        dx, CountyMap.Height(dx, dz) + 2.0f, dz);

                    // Freeze the player where it is put.
                    //
                    // Without this, any test that removes the ground - notably
                    // skipping CountyTerrain - lets the player fall out of the
                    // world. The camera ends up kilometres below the terrain
                    // looking at nothing, renders 31 draw calls, and reports a
                    // spectacular frame rate that measures an empty screen. Two
                    // separate conclusions in this session were drawn from exactly
                    // that mistake before it was caught.
                    if (OS.GetEnvironment("DIAG_NOFREEZE") != "1")
                    {
                        player.ProcessMode = ProcessModeEnum.Disabled;
                    }
                    // Narrowing which part of the player costs the frame. Disabling
                    // the whole player subtree more than doubles the frame rate,
                    // and none of collision, shadows, draw calls, vegetation or
                    // resolution accounts for it - so the remaining candidates are
                    // its own per-frame work: skeletal animation, the spring arm's
                    // shape cast, and the gameplay scripts.
                    string off = OS.GetEnvironment("DIAG_DISABLE");
                    if (off.Length > 0)
                    {
                        foreach (string path in off.Split(','))
                        {
                            Node? node = player.GetNodeOrNull(path);
                            if (node != null)
                            {
                                node.ProcessMode = ProcessModeEnum.Disabled;
                                GD.Print($"DIAG disabled: {path}");
                            }
                            else
                            {
                                GD.Print($"DIAG disable MISS: {path}");
                            }
                        }
                    }

                    if (OS.GetEnvironment("DIAG_NOGRAVITY") == "1")
                    {
                        // Keeps a fully active player in place when the ground has
                        // been removed for a test. Without it, "disable terrain
                        // collision" and "disable terrain" both just drop the
                        // player out of the world and measure an empty screen.
                        player.Set("Gravity", 0.0f);
                    }
                }
            }

            // Long enough for streaming to finish. Sampling while chunks are still
            // building measures the loader, not the frame, and every number that
            // comes out of it is wrong in the same flattering direction.
            float settle = 24.0f;
            string settleOverride = OS.GetEnvironment("DIAG_SETTLE");
            if (settleOverride.Length > 0 && float.TryParse(settleOverride, out float parsed))
            {
                settle = parsed;
            }

            GetTree().CreateTimer(settle).Timeout += () =>
            {
                ReportAtmosphere(built);
                BeginFrameSampling();
            };
        }

        GD.Print($"COUNTY: subsystems [{string.Join(", ", built.Present)}]");
        if (built.Missing.Count > 0)
        {
            GD.Print($"COUNTY: MISSING [{string.Join(", ", built.Missing)}]");
        }
    }

    /// <summary>
    /// Dumps the live environment and vegetation state a few seconds in.
    ///
    /// The review harness builds the county on its own, but the shipped scene
    /// also runs WorldTime and the settings pass, both of which write to the same
    /// sun and environment. That means the game can look nothing like the review
    /// renders, and guessing at which system won is exactly the kind of blind
    /// tuning that wasted days earlier in this project. This reads the values back
    /// after everyone has had their turn.
    /// </summary>
    private void ReportAtmosphere(CountySceneBuilder.BuildResult built)
    {
        var sun = built.Atmosphere.GetNodeOrNull<DirectionalLight3D>("Sun");
        var worldEnvironment = built.Atmosphere.GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        Godot.Environment? environment = worldEnvironment?.Environment;

        if (sun != null)
        {
            GD.Print($"DIAG sun: energy={sun.LightEnergy:F2} " +
                     $"colour=({sun.LightColor.R:F2},{sun.LightColor.G:F2},{sun.LightColor.B:F2}) " +
                     $"rot={sun.RotationDegrees} shadow={sun.ShadowEnabled} " +
                     $"shadowMax={sun.DirectionalShadowMaxDistance:F0}m");
        }

        if (environment != null)
        {
            GD.Print($"DIAG fog: on={environment.FogEnabled} density={environment.FogDensity:F6} " +
                     $"colour=({environment.FogLightColor.R:F2},{environment.FogLightColor.G:F2},{environment.FogLightColor.B:F2}) " +
                     $"energy={environment.FogLightEnergy:F2} sunScatter={environment.FogSunScatter:F2} " +
                     $"aerial={environment.FogAerialPerspective:F2} skyAffect={environment.FogSkyAffect:F2} " +
                     $"heightDensity={environment.FogHeightDensity:F3} height={environment.FogHeight:F0}");
            GD.Print($"DIAG env: ambient={environment.AmbientLightEnergy:F2} " +
                     $"bgEnergy={environment.BackgroundEnergyMultiplier:F2} " +
                     $"exposure={environment.TonemapExposure:F2} white={environment.TonemapWhite:F2} " +
                     $"ssao={environment.SsaoEnabled} glow={environment.GlowEnabled}");
        }

        int multiMesh = 0;
        int instances = 0;
        CountInstances(built.Root, ref multiMesh, ref instances);
        GD.Print($"DIAG vegetation: multimeshes={multiMesh} instances={instances}");

        Camera3D? camera = GetViewport()?.GetCamera3D();
        if (camera != null)
        {
            GD.Print($"DIAG camera: far={camera.Far:F0} near={camera.Near:F2} " +
                     $"pos={camera.GlobalPosition}");
        }
    }

    /// <summary>
    /// Saves what the running game actually shows.
    ///
    /// The review harness renders the county without WorldTime or the settings
    /// pass, so for a long stretch it was reporting a world that looked nothing
    /// like the one the player got. A screenshot taken from inside the shipped
    /// scene is the only image that settles that.
    /// </summary>
    private void CaptureDiagnosticShot()
    {
        Viewport? viewport = GetViewport();
        if (viewport == null)
        {
            return;
        }

        Image image = viewport.GetTexture().GetImage();
        string directory = ProjectSettings.GlobalizePath("res://.godot/county_ingame");
        DirAccess.MakeDirRecursiveAbsolute(directory);

        string name = OS.GetEnvironment("DIAG_SHOT");
        if (name.Length == 0)
        {
            name = "ingame";
        }

        string path = System.IO.Path.Combine(directory, name + ".png");
        Error error = image.SavePng(path);
        GD.Print(error == Error.Ok
            ? $"DIAG shot: {path}"
            : $"DIAG shot FAILED: {error}");
    }

    private static void CountChildren(Node node, out int total)
    {
        total = 1;
        foreach (Node child in node.GetChildren())
        {
            CountChildren(child, out int nested);
            total += nested;
        }
    }

    private static void CountInstances(Node node, ref int multiMesh, ref int instances)
    {
        if (node is MultiMeshInstance3D mmi && mmi.Multimesh != null)
        {
            multiMesh++;
            instances += mmi.Multimesh.InstanceCount;
        }

        foreach (Node child in node.GetChildren())
        {
            CountInstances(child, ref multiMesh, ref instances);
        }
    }

    private bool _sampling;
    private double _sampleElapsed;
    private int _sampleFrames;
    private double _worstFrame;

    /// <summary>
    /// Measures steady-state cost, and enough of a breakdown to tell CPU from GPU.
    ///
    /// "It runs at five frames" is not actionable on its own - it could equally be
    /// draw call submission, triangle throughput, or the streamer building meshes
    /// on the main thread. Draw calls and primitives separate the first two, and
    /// process time against frame time separates CPU-bound from GPU-bound.
    /// </summary>
    private void BeginFrameSampling()
    {
        _sampling = true;
        _sampleElapsed = 0.0;
        _sampleFrames = 0;
        _worstFrame = 0.0;

        // A player sliding down a slope crosses chunk borders continuously, which
        // makes the streamer rebuild meshes on the main thread every few frames.
        // That looks exactly like a steady per-frame cost from the outside, so the
        // sampler records movement to tell the two apart.
        _samplerStartPosition = GetNodeOrNull<Node3D>("../Player")?.GlobalPosition
                                ?? Vector3.Zero;
        _chunkBuildsAtStart = CountyTerrain.TotalChunkBuilds;
        SetProcess(true);
    }

    private Vector3 _samplerStartPosition;
    private long _chunkBuildsAtStart;

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() || !_sampling)
        {
            return;
        }

        _sampleElapsed += delta;
        _sampleFrames++;
        _worstFrame = System.Math.Max(_worstFrame, delta);

        if (_sampleElapsed < 4.0)
        {
            return;
        }

        _sampling = false;

        double averageMs = (_sampleElapsed / System.Math.Max(_sampleFrames, 1)) * 1000.0;
        ulong drawCalls = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalDrawCallsInFrame);
        ulong primitives = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalPrimitivesInFrame);
        ulong textureMem = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TextureMemUsed);
        ulong bufferMem = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.BufferMemUsed);

        double processMs = Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0;
        double physicsMs = Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0;

        // Splits the frame into the parts a bisection cannot separate. TimeProcess
        // covers the whole main-loop iteration, which on a single-threaded renderer
        // includes waiting for the GPU - so a high process time alone does not
        // prove the CPU is busy, and this is what tells the two apart.
        double renderCpu = Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame);
        double videoMem = Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / (1024.0 * 1024.0);
        GD.Print($"PERF render: visibleObjects={renderCpu:N0} videoMem={videoMem:F0}MB");

        GD.Print($"PERF fps={1000.0 / System.Math.Max(averageMs, 0.001):F1} " +
                 $"frame={averageMs:F1}ms worst={_worstFrame * 1000.0:F1}ms");
        GD.Print($"PERF cpu: process={processMs:F2}ms physics={physicsMs:F2}ms " +
                 $"(rest of the frame is GPU or driver)");
        GD.Print($"PERF gpu: drawCalls={drawCalls} primitives={primitives:N0} " +
                 $"textureMem={textureMem / (1024 * 1024)}MB bufferMem={bufferMem / (1024 * 1024)}MB");

        // Node count matters on its own. Every node in the tree is visited each
        // frame regardless of whether its script does anything, and each one that
        // carries a collision shape also enters the physics broadphase - so a
        // subsystem that instantiates a scene per prop can cost far more than its
        // triangle count suggests.
        double nodes = Performance.GetMonitor(Performance.Monitor.ObjectNodeCount);
        double objects = Performance.GetMonitor(Performance.Monitor.ObjectCount);
        double bodies = Performance.GetMonitor(Performance.Monitor.Physics3DActiveObjects);
        double pairs = Performance.GetMonitor(Performance.Monitor.Physics3DCollisionPairs);
        GD.Print($"PERF scene: nodes={nodes:N0} objects={objects:N0} " +
                 $"activeBodies={bodies:N0} collisionPairs={pairs:N0}");

        if (_built != null)
        {
            CountChildren(_built, out int countyNodes);
            GD.Print($"PERF county subtree: nodes={countyNodes:N0}");
        }

        Vector3 now = GetNodeOrNull<Node3D>("../Player")?.GlobalPosition ?? Vector3.Zero;
        long builds = CountyTerrain.TotalChunkBuilds - _chunkBuildsAtStart;
        GD.Print($"PERF motion: drift={_samplerStartPosition.DistanceTo(now):F2}m " +
                 $"over {_sampleElapsed:F1}s, terrainChunkBuilds={builds}");

        CaptureDiagnosticShot();
        GetTree().Quit(0);
    }

    private void RefreshEditorPreview()
    {
        if (_built != null)
        {
            _built.QueueFree();
            _built = null;
        }

        if (!_previewInEditor)
        {
            return;
        }

        // Everything the preview creates hangs off one container, so toggling the
        // preview off frees the probe along with the world in a single QueueFree.
        var container = new Node3D { Name = "EditorPreview" };
        AddChild(container);
        _built = container;

        // A stationary stand-in for the player. Streaming follows this, so the
        // resident set is whatever surrounds PreviewCentre.
        var probe = new Node3D { Name = "EditorPreviewProbe" };
        container.AddChild(probe);
        probe.Position = new Vector3(
            PreviewCentre.X,
            CountyMap.Height(PreviewCentre.X, PreviewCentre.Y),
            PreviewCentre.Y);

        CountySceneBuilder.BuildResult built = CountySceneBuilder.Build(
            probe,
            logStreaming: false,
            editorPreview: true);
        built.World.EditorPreviewRadius = PreviewRadius;
        container.AddChild(built.Root);
    }
}
