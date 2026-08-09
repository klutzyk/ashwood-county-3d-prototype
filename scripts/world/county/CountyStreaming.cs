#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// The chunk grid every county subsystem agrees on.
///
/// Terrain, vegetation, roads and settlements all have to load and unload the
/// same regions at the same time, or the player walks into a patch of forest
/// standing on nothing. Putting the grid maths in one place means none of them
/// can quietly disagree about where chunk (3, -2) is.
/// </summary>
public static class CountyChunks
{
    /// <summary>
    /// Edge length of a chunk in metres.
    ///
    /// 256m is a deliberate compromise: large enough that an 8km county is only
    /// about 32x32 chunks so the resident set stays small, and small enough that
    /// a single chunk's terrain mesh, scatter and collision can be built inside
    /// one background task without a visible hitch when it lands.
    /// </summary>
    public const float Size = 256.0f;

    public static Vector2I ToChunk(float x, float z) => new(
        Mathf.FloorToInt(x / Size),
        Mathf.FloorToInt(z / Size));

    public static Vector2I ToChunk(Vector3 worldPosition) => ToChunk(worldPosition.X, worldPosition.Z);

    /// <summary>World-space position of the chunk's minimum corner.</summary>
    public static Vector2 Origin(Vector2I chunk) => new(chunk.X * Size, chunk.Y * Size);

    public static Vector2 Center(Vector2I chunk) => Origin(chunk) + new Vector2(Size * 0.5f, Size * 0.5f);

    public static Aabb Bounds(Vector2I chunk, float minY, float maxY)
    {
        Vector2 origin = Origin(chunk);
        return new Aabb(
            new Vector3(origin.X, minY, origin.Y),
            new Vector3(Size, Mathf.Max(maxY - minY, 0.01f), Size));
    }

    /// <summary>Chebyshev ring index: 0 is the chunk the player stands in.</summary>
    public static int Ring(Vector2I chunk, Vector2I center) =>
        Mathf.Max(Mathf.Abs(chunk.X - center.X), Mathf.Abs(chunk.Y - center.Y));

    /// <summary>True if any part of the chunk lies inside the playable county.</summary>
    public static bool Intersects(Vector2I chunk)
    {
        Vector2 origin = Origin(chunk);
        return origin.X + Size > CountyMap.WestX && origin.X < CountyMap.EastX &&
               origin.Y + Size > CountyMap.NorthZ && origin.Y < CountyMap.SouthZ;
    }

    /// <summary>
    /// Chunks within <paramref name="radius"/> rings of the centre, nearest first.
    ///
    /// Ordering matters: loading nearest-first means the ground under the player's
    /// feet always resolves before the far ridgeline, which is the difference
    /// between a world that streams invisibly and one that drops you through the
    /// floor while it works on the horizon.
    /// </summary>
    public static List<Vector2I> Around(Vector2I center, int radius)
    {
        var result = new List<Vector2I>();
        for (int dz = -radius; dz <= radius; dz++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                var chunk = new Vector2I(center.X + dx, center.Y + dz);
                if (Intersects(chunk))
                {
                    result.Add(chunk);
                }
            }
        }

        result.Sort((a, b) => Ring(a, center).CompareTo(Ring(b, center)));
        return result;
    }
}

/// <summary>
/// Implemented by anything that produces content per chunk. <see cref="CountyWorld"/>
/// drives every registered source through the same load/unload schedule so the
/// subsystems stay in lockstep without knowing about each other.
/// </summary>
public interface ICountyChunkSource
{
    /// <summary>How many rings out this source should populate. Cheap sources reach further.</summary>
    int ChunkRadius { get; }

    /// <summary>
    /// Build content for a chunk. Called on the main thread; implementations that
    /// do heavy work should hand it to a background task and add nodes deferred.
    /// </summary>
    void BuildChunk(Vector2I chunk, int ring);

    /// <summary>Drop content for a chunk that has left the resident set.</summary>
    void ReleaseChunk(Vector2I chunk);

    /// <summary>
    /// Called when a resident chunk changes ring, so a source can swap detail
    /// levels without a full rebuild. Sources with no LOD may ignore it.
    /// </summary>
    void UpdateChunkRing(Vector2I chunk, int ring)
    {
    }

    /// <summary>False while worker results are still outstanding or awaiting install.</summary>
    bool IsBuildComplete => true;
}
