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

        GD.Print($"COUNTY: subsystems [{string.Join(", ", built.Present)}]");
        if (built.Missing.Count > 0)
        {
            GD.Print($"COUNTY: MISSING [{string.Join(", ", built.Missing)}]");
        }
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
