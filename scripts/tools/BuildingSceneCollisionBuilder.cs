#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.Tools;

public sealed class BuildingSceneCollisionBuildResult
{
	public required CollisionObject3D Body { get; init; }
	public required CollisionShape3D CollisionShape { get; init; }
	public required MeshInstance3D Preview { get; init; }
	public required BuildingSceneCollisionMode Mode { get; init; }
}

public static class BuildingSceneCollisionBuilder
{
	public static BuildingSceneCollisionBuildResult Build(
		MeshInstance3D source,
		Node3D generatedRoot,
		BuildingSceneCollisionMode requestedMode)
	{
		if (source.Mesh is null)
		{
			throw new InvalidOperationException("mesh resource is missing");
		}

		BuildingSceneCollisionMode mode = requestedMode ==
			BuildingSceneCollisionMode.Auto
			? ResolveAutoMode(source)
			: requestedMode;
		if (mode == BuildingSceneCollisionMode.None)
		{
			throw new InvalidOperationException("collision mode is None");
		}

		PhysicsBody3D? sourceBody = FindSourceBody(source, generatedRoot);
		if (mode == BuildingSceneCollisionMode.Trimesh &&
			sourceBody is not null &&
			sourceBody is not StaticBody3D)
		{
			throw new InvalidOperationException(
				"Trimesh collision is restricted to static objects");
		}

		Transform3D relativeTransform =
			generatedRoot.GlobalTransform.AffineInverse() *
			source.GlobalTransform;
		CollisionObject3D body = CreateBody(sourceBody);
		body.Name = new StringName($"{source.Name} {mode} Collision");
		body.Transform = relativeTransform;
		body.CollisionLayer =
			BuildingInteriorBuilder.EnvironmentCollisionLayer;
		body.CollisionMask =
			BuildingInteriorBuilder.EnvironmentCollisionMask;

		Aabb aabb = source.GetAabb();
		CollisionShape3D collisionShape = new()
		{
			Name = "CollisionShape3D",
		};
		MeshInstance3D preview = new()
		{
			Name = new StringName($"{source.Name} {mode} Preview"),
			Transform = relativeTransform,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};

		switch (mode)
		{
			case BuildingSceneCollisionMode.Box:
				collisionShape.Position = aabb.GetCenter();
				collisionShape.Shape = new BoxShape3D
				{
					Size = BuildingInteriorBuilder.SanitizeSize(aabb.Size),
				};
				preview.Transform =
					relativeTransform *
					new Transform3D(Basis.Identity, aabb.GetCenter());
				preview.Mesh = new BoxMesh
				{
					Size = BuildingInteriorBuilder.SanitizeSize(aabb.Size),
				};
				break;
			case BuildingSceneCollisionMode.Convex:
				collisionShape.Shape =
					source.Mesh.CreateConvexShape(true, true);
				preview.Mesh = source.Mesh;
				break;
			case BuildingSceneCollisionMode.Trimesh:
				collisionShape.Shape = source.Mesh.CreateTrimeshShape();
				preview.Mesh = source.Mesh;
				break;
			default:
				throw new InvalidOperationException(
					$"unsupported collision mode {mode}");
		}

		body.AddChild(collisionShape);
		return new BuildingSceneCollisionBuildResult
		{
			Body = body,
			CollisionShape = collisionShape,
			Preview = preview,
			Mode = mode,
		};
	}

	public static BuildingSceneCollisionMode ResolveAutoMode(
		MeshInstance3D source)
	{
		if (source.Mesh is null)
		{
			return BuildingSceneCollisionMode.None;
		}

		int triangleCount = source.Mesh.GetFaces().Length / 3;

		Vector3 effectiveSize =
			source.GetAabb().Size * source.GlobalBasis.Scale.Abs();
		float longestAxis = Mathf.Max(
			effectiveSize.X,
			Mathf.Max(effectiveSize.Y, effectiveSize.Z));
		if (triangleCount <= 24)
		{
			return BuildingSceneCollisionMode.Box;
		}
		if (longestAxis >= 6.0f && triangleCount >= 100)
		{
			return BuildingSceneCollisionMode.Trimesh;
		}
		return BuildingSceneCollisionMode.Convex;
	}

	public static bool HasExistingCollision(
		MeshInstance3D source,
		Node targetRoot)
	{
		if (source.FindChildren(
			"*", "CollisionShape3D", true, false).Count > 0)
		{
			return true;
		}

		Node? current = source.GetParent();
		while (current is not null && current != targetRoot)
		{
			if (current is CollisionObject3D &&
				current.FindChildren(
					"*", "CollisionShape3D", true, false).Count > 0)
			{
				return true;
			}
			current = current.GetParent();
		}
		return false;
	}

	private static PhysicsBody3D? FindSourceBody(
		Node source,
		Node stopAt)
	{
		Node? current = source.GetParent();
		while (current is not null && current != stopAt)
		{
			if (current is PhysicsBody3D body)
			{
				return body;
			}
			current = current.GetParent();
		}
		return null;
	}

	private static CollisionObject3D CreateBody(PhysicsBody3D? sourceBody)
	{
		return sourceBody switch
		{
			AnimatableBody3D => new AnimatableBody3D(),
			RigidBody3D => new RigidBody3D
			{
				Freeze = true,
			},
			CharacterBody3D => new CharacterBody3D(),
			_ => new StaticBody3D(),
		};
	}
}
