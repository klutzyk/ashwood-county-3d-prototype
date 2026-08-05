#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Assembles a complete county world at runtime.
///
/// The subsystems are built independently and land at different times, so this
/// discovers them by name in the loaded assembly rather than hard-wiring a scene
/// file against classes that may not exist yet. A missing subsystem degrades to
/// "that layer is absent" instead of failing the whole scene to load, which keeps
/// the review harness usable while the world is still being built.
/// </summary>
public static class CountySceneBuilder
{
    /// <summary>
    /// Subsystem type names in the order they should be added. Terrain first so
    /// anything that samples the ground finds it already present.
    /// </summary>
    private static readonly string[] SubsystemTypeNames =
    {
        "CountyTerrain",
        "CountyWater",
        "CountyRoads",
        "CountyVegetation",
        "CountySettlements",
    };

    public readonly record struct BuildResult(
        Node3D Root,
        CountyWorld World,
        CountyAtmosphere Atmosphere,
        IReadOnlyList<string> Present,
        IReadOnlyList<string> Missing);

    /// <summary>
    /// Builds the world under a fresh root node. The caller adds the root to the
    /// tree; nothing here touches the scene tree so it is safe to call before
    /// entering it.
    /// </summary>
    public static BuildResult Build(Node3D? target = null, bool logStreaming = false)
    {
        var root = new Node3D { Name = "AshwoodCounty" };

        var atmosphere = new CountyAtmosphere { Name = "Atmosphere" };
        root.AddChild(atmosphere);

        var world = new CountyWorld { Name = "World", LogStreaming = logStreaming };
        root.AddChild(world);

        var present = new List<string>();
        var missing = new List<string>();

        foreach (string typeName in SubsystemTypeNames)
        {
            Node3D? subsystem = Instantiate(typeName);
            if (subsystem == null)
            {
                missing.Add(typeName);
                continue;
            }

            subsystem.Name = typeName;
            world.AddChild(subsystem);
            present.Add(typeName);
        }

        // Assigned directly rather than as a NodePath: neither node is in the tree
        // yet, so there is no common ancestor for a path to be relative to.
        world.Target = target;

        return new BuildResult(root, world, atmosphere, present, missing);
    }

    private static Node3D? Instantiate(string typeName)
    {
        Type? type = Assembly.GetExecutingAssembly()
            .GetTypes()
            .FirstOrDefault(candidate =>
                candidate.Name == typeName &&
                typeof(Node3D).IsAssignableFrom(candidate) &&
                !candidate.IsAbstract);

        if (type == null)
        {
            return null;
        }

        try
        {
            return Activator.CreateInstance(type) as Node3D;
        }
        catch (Exception error)
        {
            GD.PushWarning($"CountySceneBuilder: could not construct {typeName}: {error.Message}");
            return null;
        }
    }

    /// <summary>
    /// A camera rig positioned to look at a county location, for review renders.
    /// Height is taken from the terrain so a viewpoint never ends up underground.
    /// </summary>
    public static Transform3D LookAt(Vector2 from, Vector2 to, float eyeHeight = 12.0f)
    {
        var eye = new Vector3(from.X, CountyMap.Height(from.X, from.Y) + eyeHeight, from.Y);
        var focus = new Vector3(to.X, CountyMap.Height(to.X, to.Y) + eyeHeight * 0.35f, to.Y);

        // Basis.LookingAt orients -Z along the given direction for models, which is
        // the opposite of what a camera wants; building the basis by hand here put
        // review cameras underneath the terrain. Transform3D.LookingAt uses the
        // camera convention and is the same one Node3D.LookAt goes through.
        if (eye.DistanceSquaredTo(focus) < 0.0001f)
        {
            focus = eye + Vector3.Forward;
        }

        return new Transform3D(Basis.Identity, eye).LookingAt(focus, Vector3.Up);
    }
}
