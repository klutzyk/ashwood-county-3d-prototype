#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// The county as seen from a distance: a permanently resident, low-resolution
/// shell of the entire landmass.
///
/// <see cref="CountyTerrain"/> streams roughly two kilometres around the player,
/// but the county is over eight across. Without this, standing on Fire Lookout or
/// any of the ridges shows an empty horizon where the rest of the world should be,
/// which is the single loudest way an open world announces that it is not one.
///
/// Tiles are aligned to whole blocks of the streaming chunk grid, so a tile is
/// either entirely covered by streamed chunks or entirely uncovered - never half
/// of each. That makes hiding them exact and removes any need to fight the
/// detailed mesh for depth.
/// </summary>
[Tool]
public partial class CountyFarTerrain : Node3D
{
    /// <summary>
    /// Tile size as a multiple of <see cref="CountyChunks.Size"/>. Four chunks a
    /// side is 1024m: large enough that the whole county is only 64 draw calls,
    /// small enough that hiding covered tiles actually reclaims something.
    /// </summary>
    [Export] public int ChunksPerTile { get; set; } = 4;

    /// <summary>Quads per tile edge. 32 over 1024m is 32m per vertex.</summary>
    [Export] public int QuadsPerTile { get; set; } = 32;

    /// <summary>
    /// How far the detailed terrain streams, in chunks. Tiles fully inside this
    /// are hidden. Read from the sibling CountyTerrain when one is present.
    /// </summary>
    [Export] public int StreamedRadius { get; set; } = 8;

    private const string FarMaterialPath = "res://assets/materials/county_far_terrain.tres";

    private readonly Dictionary<Vector2I, MeshInstance3D> _tiles = new();
    private readonly ConcurrentDictionary<Vector2I, TileData> _built = new();

    private Material? _material;
    private CountyTerrain? _streamed;
    private Vector2I _lastCenter = new(int.MinValue, int.MinValue);
    private int _pending;

    /// <summary>Mirrors <see cref="CountyWorld.EditorPreview"/>; set by the parent world.</summary>
    public bool EditorPreview { get; set; }

    private bool Dormant => Engine.IsEditorHint() && !EditorPreview;

    public override void _Ready()
    {
        if (Dormant)
        {
            return;
        }

        _material = LoadMaterial();

        foreach (Node sibling in GetParent()?.GetChildren() ?? new Godot.Collections.Array<Node>())
        {
            if (sibling is CountyTerrain terrain)
            {
                _streamed = terrain;
                StreamedRadius = terrain.TerrainRadius;
                break;
            }
        }

        BuildAllTiles();
    }

    private Material LoadMaterial()
    {
        if (ResourceLoader.Exists(FarMaterialPath) &&
            ResourceLoader.Load(FarMaterialPath) is Material loaded)
        {
            return loaded;
        }

        return new StandardMaterial3D
        {
            AlbedoColor = new Color(0.30f, 0.33f, 0.25f),
            Roughness = 1.0f,
        };
    }

    private void BuildAllTiles()
    {
        float tileSize = CountyChunks.Size * ChunksPerTile;
        int minTileX = Mathf.FloorToInt(CountyMap.WestX / tileSize);
        int maxTileX = Mathf.FloorToInt(CountyMap.EastX / tileSize);
        int minTileZ = Mathf.FloorToInt(CountyMap.NorthZ / tileSize);
        int maxTileZ = Mathf.FloorToInt(CountyMap.SouthZ / tileSize);

        for (int tz = minTileZ; tz <= maxTileZ; tz++)
        {
            for (int tx = minTileX; tx <= maxTileX; tx++)
            {
                var tile = new Vector2I(tx, tz);
                _pending++;
                Task.Run(() =>
                {
                    try
                    {
                        _built[tile] = BuildTileData(tile, ChunksPerTile, QuadsPerTile);
                        CallDeferred(nameof(InstallTile), tile);
                    }
                    catch (Exception error)
                    {
                        GD.PushError($"CountyFarTerrain: tile {tile} failed: {error}");
                        CallDeferred(nameof(DecrementPending));
                    }
                });
            }
        }
    }

    private void DecrementPending() => _pending = Mathf.Max(_pending - 1, 0);

    private void InstallTile(Vector2I tile)
    {
        _pending = Mathf.Max(_pending - 1, 0);

        if (!_built.TryRemove(tile, out TileData? data) || !IsInsideTree())
        {
            return;
        }

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = data.Vertices;
        arrays[(int)Mesh.ArrayType.Normal] = data.Normals;
        arrays[(int)Mesh.ArrayType.Color] = data.Colors;
        arrays[(int)Mesh.ArrayType.TexUV] = data.Uvs;
        arrays[(int)Mesh.ArrayType.Index] = data.Indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

        var instance = new MeshInstance3D
        {
            Name = $"FarTile_{tile.X}_{tile.Y}",
            Mesh = mesh,
            MaterialOverride = _material,

            // The far field never casts or receives shadows. At these distances no
            // shadow is resolvable, and including 64 tiles in the cascade render
            // costs more than everything they contribute.
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            GIMode = GeometryInstance3D.GIModeEnum.Disabled,
        };

        AddChild(instance);
        _tiles[tile] = instance;

        // A tile that lands while the player is already standing on it must be
        // hidden immediately, or it pops through the detailed ground for a frame.
        UpdateVisibility(force: true);
    }

    public override void _Process(double delta)
    {
        if (Dormant)
        {
            return;
        }

        UpdateVisibility(force: false);
    }

    /// <summary>
    /// Hides tiles wholly covered by streamed chunks.
    ///
    /// The test is on whole tiles rather than on distance: a tile spans exactly
    /// ChunksPerTile chunks a side, so if its furthest corner chunk is inside the
    /// streaming radius then every chunk under it is too, and nothing of the tile
    /// can show through.
    /// </summary>
    private void UpdateVisibility(bool force)
    {
        if (_tiles.Count == 0)
        {
            return;
        }

        Vector2I center = _streamed != null && GetParent() is CountyWorld world
            ? world.CurrentChunk
            : new Vector2I(0, 0);

        // CountyWorld reports int.MinValue until its first streaming tick. Feeding
        // that into the ring arithmetic below overflows on the subtraction, so
        // until the streamer has primed there is nothing covered and every tile
        // stays visible.
        if (center.X == int.MinValue || center.Y == int.MinValue)
        {
            foreach (MeshInstance3D unprimed in _tiles.Values)
            {
                unprimed.Visible = true;
            }

            return;
        }

        if (!force && center == _lastCenter)
        {
            return;
        }

        _lastCenter = center;

        foreach ((Vector2I tile, MeshInstance3D instance) in _tiles)
        {
            int minChunkX = tile.X * ChunksPerTile;
            int minChunkZ = tile.Y * ChunksPerTile;
            int maxChunkX = minChunkX + ChunksPerTile - 1;
            int maxChunkZ = minChunkZ + ChunksPerTile - 1;

            int worstRing = Mathf.Max(
                Mathf.Max(Mathf.Abs(minChunkX - center.X), Mathf.Abs(maxChunkX - center.X)),
                Mathf.Max(Mathf.Abs(minChunkZ - center.Y), Mathf.Abs(maxChunkZ - center.Y)));

            instance.Visible = worstRing > StreamedRadius;
        }
    }

    private sealed class TileData
    {
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public Color[] Colors = Array.Empty<Color>();
        public Vector2[] Uvs = Array.Empty<Vector2>();
        public int[] Indices = Array.Empty<int>();
    }

    private static TileData BuildTileData(Vector2I tile, int chunksPerTile, int quads)
    {
        float tileSize = CountyChunks.Size * chunksPerTile;
        var origin = new Vector2(tile.X * tileSize, tile.Y * tileSize);
        float step = tileSize / quads;
        int side = quads + 1;

        // Point-sampling a heightfield every 32m aliases badly: the Blackwater's
        // canyon is narrower than the sample spacing, so single samples fell into
        // it at random and the far mesh grew a row of spikes down the river.
        // Averaging a 2x2 kernel is a proper box filter and removes them.
        var heights = new float[side, side];
        float quarter = step * 0.25f;
        for (int row = 0; row < side; row++)
        {
            float z = origin.Y + row * step;
            for (int column = 0; column < side; column++)
            {
                float x = origin.X + column * step;
                heights[column, row] = 0.25f * (
                    CountyMap.Height(x - quarter, z - quarter) +
                    CountyMap.Height(x + quarter, z - quarter) +
                    CountyMap.Height(x - quarter, z + quarter) +
                    CountyMap.Height(x + quarter, z + quarter));
            }
        }

        var vertices = new Vector3[side * side];
        var normals = new Vector3[side * side];
        var colors = new Color[side * side];
        var uvs = new Vector2[side * side];

        for (int row = 0; row < side; row++)
        {
            float z = origin.Y + row * step;
            for (int column = 0; column < side; column++)
            {
                float x = origin.X + column * step;
                float height = heights[column, row];

                float left = column > 0 ? heights[column - 1, row] : height;
                float right = column < side - 1 ? heights[column + 1, row] : height;
                float back = row > 0 ? heights[column, row - 1] : height;
                float front = row < side - 1 ? heights[column, row + 1] : height;

                var normal = new Vector3(left - right, 2.0f * step, back - front).Normalized();
                float slope = Mathf.Acos(Mathf.Clamp(normal.Y, -1.0f, 1.0f));

                int index = row * side + column;
                vertices[index] = new Vector3(x, height, z);
                normals[index] = normal;
                uvs[index] = new Vector2(x, z);
                colors[index] = FarSurfaceWeights(x, z, height, slope);
            }
        }

        var indices = new int[quads * quads * 6];
        int cursor = 0;
        for (int row = 0; row < quads; row++)
        {
            for (int column = 0; column < quads; column++)
            {
                int topLeft = row * side + column;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + side;
                int bottomRight = bottomLeft + 1;

                indices[cursor++] = topLeft;
                indices[cursor++] = topRight;
                indices[cursor++] = bottomLeft;

                indices[cursor++] = topRight;
                indices[cursor++] = bottomRight;
                indices[cursor++] = bottomLeft;
            }
        }

        return new TileData
        {
            Vertices = vertices,
            Normals = normals,
            Colors = colors,
            Uvs = uvs,
            Indices = indices,
        };
    }

    /// <summary>
    /// Surface weights for the far field.
    ///
    /// Deliberately cheaper than the streamed mesher's version: at 32m per vertex
    /// the field parcels and riverbank gravel are far below a pixel, and querying
    /// them for every vertex of 64 tiles would dominate startup for detail nobody
    /// can see. Forest and rock are what actually read at this distance.
    /// </summary>
    private static Color FarSurfaceWeights(float x, float z, float height, float slope)
    {
        float forest = CountyMap.ForestDensity(x, z, height, slope);

        float rock = Mathf.Clamp((slope - 0.42f) / 0.34f, 0.0f, 1.0f);
        rock = Mathf.Max(rock, CountyMap.RimFalloff(x, z) * 1.4f);
        if (height > 820.0f)
        {
            rock = Mathf.Max(rock, 0.72f);
        }

        forest *= 1.0f - rock;

        return new Color(
            Mathf.Clamp(forest, 0.0f, 1.0f),
            Mathf.Clamp(rock, 0.0f, 1.0f),
            0.0f,
            0.0f);
    }

    /// <summary>True once every tile has been meshed. For tests and review renders.</summary>
    public bool IsComplete => _pending == 0 && _tiles.Count > 0;

    public int TileCount => _tiles.Count;

    public int VisibleTileCount
    {
        get
        {
            int visible = 0;
            foreach (MeshInstance3D instance in _tiles.Values)
            {
                if (instance.Visible)
                {
                    visible++;
                }
            }

            return visible;
        }
    }
}
