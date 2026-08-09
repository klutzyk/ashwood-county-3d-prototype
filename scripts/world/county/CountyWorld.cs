#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Runs the open world: tracks the player, drives chunk streaming across every
/// registered subsystem, and owns the atmosphere.
///
/// The subsystems (terrain, water, roads, vegetation, settlements) know nothing
/// about each other. They register as <see cref="ICountyChunkSource"/> and this
/// node decides what is resident, so they can never drift out of step.
/// </summary>
[Tool]
public partial class CountyWorld : Node3D
{
    [Export] public NodePath TargetPath { get; set; } = new();

    /// <summary>
    /// Re-evaluates the resident set only when the target crosses a chunk border,
    /// which keeps a walking player from re-running the schedule every frame.
    /// </summary>
    [Export] public float UpdateInterval { get; set; } = 0.25f;

    /// <summary>Draws the streaming state to the console. Useful when tuning ring counts.</summary>
    [Export] public bool LogStreaming { get; set; }

    /// <summary>
    /// Allows streaming to run inside the editor so the county can be inspected
    /// without entering play mode. Off by default: an editor session that streams
    /// on every viewport move is far more disruptive than one that stays empty.
    /// </summary>
    public bool EditorPreview { get; set; }

    /// <summary>
    /// Caps every subsystem's streaming radius while in the editor. The editor
    /// viewport carries more overhead per triangle than the running game does -
    /// gizmos, selection outlines, no occlusion tuning - so streaming the same
    /// ~2km-across working set that gameplay uses made the preview laggier than
    /// just pressing Play. Two chunks is enough to see terrain, water, roads,
    /// trees and a settlement together without asking the editor to hold the
    /// whole county's worth of geometry live.
    /// </summary>
    public int EditorPreviewRadius { get; set; } = 2;

    private bool Dormant => Engine.IsEditorHint() && !EditorPreview;

    /// <summary>
    /// Chunk builds allowed to start per frame, across all sources.
    ///
    /// Previously every chunk inside the streaming radius was built in the frame
    /// the ring changed. On the first tick that is the entire resident set - 169
    /// chunks for vegetation alone, each running thousands of CountyMap samples
    /// and constructing its MultiMesh batches synchronously on the main thread.
    /// That is a ten to fifteen second freeze on load, and a smaller one every
    /// time the player crosses a chunk border.
    ///
    /// Spreading the work means the world fills in over a second or two instead of
    /// arriving all at once, which is both far better to play and the difference
    /// between a stutter and a steady frame.
    /// </summary>
    [Export(PropertyHint.Range, "1,64,1")]
    public int ChunkBuildsPerFrame { get; set; } = 2;

    private readonly List<ICountyChunkSource> _sources = new();
    private readonly Dictionary<ICountyChunkSource, Dictionary<Vector2I, int>> _resident = new();

    /// <summary>
    /// Chunks waiting to be built, nearest first. Kept per source so a cheap
    /// subsystem is not starved behind an expensive one.
    /// </summary>
    private readonly Dictionary<ICountyChunkSource, List<(Vector2I Chunk, int Ring)>> _queued = new();

    private Node3D? _target;
    private Vector2I _lastChunk = new(int.MinValue, int.MinValue);
    private float _sinceUpdate;
    private bool _primed;

    public Vector2I CurrentChunk => _lastChunk;

    /// <summary>
    /// The node streaming follows. Settable directly so a world assembled in code
    /// can point at its probe before either is in the tree, which a NodePath
    /// cannot express - GetPathTo needs a common ancestor that does not exist yet.
    /// </summary>
    public Node3D? Target
    {
        get => _target;
        set => _target = value;
    }

    public override void _Ready()
    {
        if (Dormant)
        {
            return;
        }

        ResolveTarget();
        CollectSources();
    }

    /// <summary>
    /// Registers a chunk source. Subsystems call this from their own _Ready, so
    /// the scene can be reordered or a subsystem removed entirely without this
    /// node needing to know.
    /// </summary>
    public void RegisterSource(ICountyChunkSource source)
    {
        if (_sources.Contains(source))
        {
            return;
        }

        _sources.Add(source);
        _resident[source] = new Dictionary<Vector2I, int>();
        _queued[source] = new List<(Vector2I, int)>();

        // A source that registers after the world has already primed still needs
        // its first load, otherwise it stays empty until the player walks.
        if (_primed && _target != null)
        {
            UpdateSource(source, CountyChunks.ToChunk(_target.GlobalPosition));
        }
    }

    public void UnregisterSource(ICountyChunkSource source)
    {
        if (!_resident.TryGetValue(source, out Dictionary<Vector2I, int>? resident))
        {
            return;
        }

        foreach (Vector2I chunk in resident.Keys)
        {
            source.ReleaseChunk(chunk);
        }

        _resident.Remove(source);
        _queued.Remove(source);
        _sources.Remove(source);
    }

    private void ResolveTarget()
    {
        if (_target != null && IsInstanceValid(_target))
        {
            return;
        }

        if (!TargetPath.IsEmpty)
        {
            _target = GetNodeOrNull<Node3D>(TargetPath);
        }

        // Fall back to whatever the game considers the player, so dropping this
        // node into a scene works without wiring an export every time.
        _target ??= GetTree().GetFirstNodeInGroup("player") as Node3D;
    }

    private void CollectSources()
    {
        foreach (Node child in GetChildren())
        {
            if (child is ICountyChunkSource source)
            {
                RegisterSource(source);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (Dormant || _sources.Count == 0)
        {
            return;
        }

        if (_target == null || !IsInstanceValid(_target))
        {
            ResolveTarget();
            if (_target == null)
            {
                return;
            }
        }

        // Drained every frame, not on the streaming interval: the queue is what
        // keeps the frame smooth, so it must not wait a quarter second between
        // slices.
        DrainBuildQueue();

        _sinceUpdate += (float)delta;
        if (_sinceUpdate < UpdateInterval)
        {
            return;
        }

        _sinceUpdate = 0.0f;

        Vector2I chunk = CountyChunks.ToChunk(_target.GlobalPosition);
        if (chunk == _lastChunk && _primed)
        {
            return;
        }

        _lastChunk = chunk;
        _primed = true;

        foreach (ICountyChunkSource source in _sources)
        {
            UpdateSource(source, chunk);
        }

        if (LogStreaming)
        {
            LogState(chunk);
        }
    }

    private void UpdateSource(ICountyChunkSource source, Vector2I center)
    {
        int radius = Engine.IsEditorHint()
            ? Mathf.Min(source.ChunkRadius, EditorPreviewRadius)
            : source.ChunkRadius;

        Dictionary<Vector2I, int> resident = _resident[source];
        List<Vector2I> wanted = CountyChunks.Around(center, radius);

        // Release first. Freeing before allocating keeps the peak memory of a
        // streaming step down to roughly one ring rather than two full sets,
        // which matters on the integrated GPU this targets.
        var stale = new List<Vector2I>();
        foreach (Vector2I chunk in resident.Keys)
        {
            if (CountyChunks.Ring(chunk, center) > radius)
            {
                stale.Add(chunk);
            }
        }

        foreach (Vector2I chunk in stale)
        {
            source.ReleaseChunk(chunk);
            resident.Remove(chunk);
        }

        List<(Vector2I Chunk, int Ring)> queue = _queued[source];
        queue.Clear();

        foreach (Vector2I chunk in wanted)
        {
            int ring = CountyChunks.Ring(chunk, center);
            if (resident.TryGetValue(chunk, out int previousRing))
            {
                if (previousRing != ring)
                {
                    source.UpdateChunkRing(chunk, ring);
                    resident[chunk] = ring;
                }

                continue;
            }

            // Queued rather than built. CountyChunks.Around returns nearest first,
            // so draining in order fills the ground under the player before the
            // far ring, which is what makes the delay tolerable.
            queue.Add((chunk, ring));
        }
    }

    /// <summary>
    /// Builds a few queued chunks. Called every frame so the cost of streaming is
    /// paid a slice at a time rather than in one stall.
    /// </summary>
    private void DrainBuildQueue()
    {
        // The chunk the player is standing in is never deferred.
        //
        // Budgeting every build meant the ground under the spawn point did not
        // exist for the first second, and a CharacterBody3D with gravity does not
        // wait politely - it falls, accelerating, and by the time terrain arrives
        // it is hundreds of metres below the world. Ground first, detail after.
        foreach (ICountyChunkSource source in _sources)
        {
            List<(Vector2I Chunk, int Ring)> queue = _queued[source];
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i].Ring > 0)
                {
                    continue;
                }

                (Vector2I chunk, int ring) = queue[i];
                queue.RemoveAt(i);
                if (!_resident[source].ContainsKey(chunk))
                {
                    source.BuildChunk(chunk, ring);
                    _resident[source][chunk] = ring;
                }
            }
        }

        int budget = Mathf.Max(ChunkBuildsPerFrame, 1);

        // Round-robin across sources so terrain, water and vegetation for the same
        // area arrive together. Draining one source to exhaustion first would show
        // the player bare ground with trees standing on nothing.
        bool builtAny = true;
        while (budget > 0 && builtAny)
        {
            builtAny = false;
            foreach (ICountyChunkSource source in _sources)
            {
                if (budget <= 0)
                {
                    break;
                }

                List<(Vector2I Chunk, int Ring)> queue = _queued[source];
                if (queue.Count == 0)
                {
                    continue;
                }

                (Vector2I chunk, int ring) = queue[0];
                queue.RemoveAt(0);

                if (!_resident[source].ContainsKey(chunk))
                {
                    source.BuildChunk(chunk, ring);
                    _resident[source][chunk] = ring;
                    budget--;
                }

                builtAny = true;
            }
        }
    }

    /// <summary>True once nothing is waiting to be built. For tests and reviews.</summary>
    public bool IsStreamingComplete
    {
        get
        {
            foreach (List<(Vector2I, int)> queue in _queued.Values)
            {
                if (queue.Count > 0)
                {
                    return false;
                }
            }

            return _primed;
        }
    }

    private void LogState(Vector2I center)
    {
        GD.Print($"COUNTY_WORLD: chunk {center} " +
                 $"({CountyChunks.Center(center).X:F0}, {CountyChunks.Center(center).Y:F0})");
        foreach (ICountyChunkSource source in _sources)
        {
            GD.Print($"  {source.GetType().Name,-22} radius={source.ChunkRadius} " +
                     $"resident={_resident[source].Count}");
        }
    }

    /// <summary>Forces a full rebuild of every source. For editor use and tests.</summary>
    public void RebuildAll()
    {
        foreach (ICountyChunkSource source in _sources)
        {
            Dictionary<Vector2I, int> resident = _resident[source];
            foreach (Vector2I chunk in resident.Keys)
            {
                source.ReleaseChunk(chunk);
            }

            resident.Clear();
        }

        _primed = false;
        _lastChunk = new Vector2I(int.MinValue, int.MinValue);
    }
}
