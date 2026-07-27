#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Tools;

public static class BuildingInteriorBuilder
{
	public const uint EnvironmentCollisionLayer = 1;
	public const uint EnvironmentCollisionMask = 1;
	private const float MinimumElementSize = 0.01f;

	public static MeshInstance3D CreateVisual(
		BuildingInteriorElement element,
		Material? material = null)
	{
		Vector3 size = SanitizeSize(element.Size);
		MeshInstance3D visual = new()
		{
			Name = element.Name,
			Mesh = new BoxMesh
			{
				Size = Vector3.One,
				Material = material ?? CreateRuntimeMaterial(element.Type),
			},
			Position = element.Position,
			RotationDegrees = element.RotationDegrees,
			Scale = size,
			Visible = element.Enabled,
		};
		return visual;
	}

	public static StaticBody3D CreateCollision(BuildingInteriorElement element)
	{
		StaticBody3D body = new()
		{
			Name = element.Name,
			Position = element.Position,
			RotationDegrees = element.RotationDegrees,
			CollisionLayer = EnvironmentCollisionLayer,
			CollisionMask = EnvironmentCollisionMask,
		};
		body.AddChild(new CollisionShape3D
		{
			Name = "CollisionShape3D",
			Shape = new BoxShape3D
			{
				Size = SanitizeSize(element.Size),
			},
		});
		return body;
	}

	public static BuildingInteriorElement CreateElementFromVisual(
		MeshInstance3D visual,
		BuildingInteriorElementType type)
	{
		return new BuildingInteriorElement
		{
			Name = visual.Name,
			Type = type,
			Position = visual.Position,
			RotationDegrees = visual.RotationDegrees,
			Size = SanitizeSize(visual.Scale),
			Enabled = visual.Visible,
		};
	}

	public static void ClearGeneratedChildren(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			root.RemoveChild(child);
			child.Free();
		}
	}

	public static Vector3 SanitizeSize(Vector3 size)
	{
		return new Vector3(
			Mathf.Max(Mathf.Abs(size.X), MinimumElementSize),
			Mathf.Max(Mathf.Abs(size.Y), MinimumElementSize),
			Mathf.Max(Mathf.Abs(size.Z), MinimumElementSize));
	}

	private static StandardMaterial3D CreateRuntimeMaterial(
		BuildingInteriorElementType type)
	{
		return new StandardMaterial3D
		{
			AlbedoColor = type == BuildingInteriorElementType.Wall
				? new Color(0.72f, 0.69f, 0.62f)
				: new Color(0.36f, 0.2f, 0.1f),
			Roughness = 0.85f,
		};
	}
}
