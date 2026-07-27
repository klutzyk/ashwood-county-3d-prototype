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
				Material = material ?? element.Material ?? CreateFallbackMaterial(),
			},
			Position = element.Position,
			RotationDegrees = element.RotationDegrees,
			Scale = size,
			Visible = element.Enabled,
		};
		return visual;
	}

	public static BuildingInteriorPreviewElement CreatePreview(
		BuildingInteriorElement element)
	{
		BuildingInteriorPreviewElement preview = new()
		{
			Name = element.Name,
			ElementType = element.Type,
			ElementMaterial = element.Material,
			ElementEnabled = element.Enabled,
			GenerateVisual = element.GenerateVisual,
			GenerateCollision = element.GenerateCollision,
			Mesh = new BoxMesh
			{
				Size = Vector3.One,
				Material = element.Material ?? CreateFallbackMaterial(),
			},
			Position = element.Position,
			RotationDegrees = element.RotationDegrees,
			Scale = SanitizeSize(element.Size),
			Visible = element.Enabled,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		return preview;
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

	public static BuildingInteriorElement CreateElementFromPreview(
		BuildingInteriorPreviewElement preview)
	{
		return new BuildingInteriorElement
		{
			Name = preview.Name,
			Type = preview.ElementType,
			Position = preview.Position,
			RotationDegrees = preview.RotationDegrees,
			Size = SanitizeSize(preview.Scale),
			Material = preview.ElementMaterial,
			Enabled = preview.ElementEnabled,
			GenerateVisual = preview.GenerateVisual,
			GenerateCollision = preview.GenerateCollision,
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

	public static StandardMaterial3D CreateFallbackMaterial()
	{
		return new StandardMaterial3D
		{
			AlbedoColor = new Color(0.65f, 0.67f, 0.7f),
			Roughness = 0.85f,
		};
	}
}
