#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Applies project-specific presentation and a strict visibility budget to a
/// retained third-party hero tree without modifying its imported source.
/// </summary>
public partial class HeroTreePresentation : Node3D
{
	[Export] public Color LeafTint { get; set; } = Colors.White;
	[Export] public Color BarkTint { get; set; } = Colors.White;
	[Export(PropertyHint.Range, "30,180,1")]
	public float VisibilityRange { get; set; } = 96.0f;
	[Export(PropertyHint.Range, "0,40,1")]
	public float VisibilityMargin { get; set; } = 12.0f;
	[Export] public bool CastShadows { get; set; } = true;

	public override void _Ready()
	{
		ConfigureBranch(this);
		SetMeta("environment_role", "seasonal_midground_hero_vegetation");
		SetMeta("triangle_budget", 120429);
	}

	private void ConfigureBranch(Node node)
	{
		if (node is MeshInstance3D meshInstance && meshInstance.Mesh is not null)
		{
			meshInstance.VisibilityRangeEnd = VisibilityRange;
			meshInstance.VisibilityRangeEndMargin = VisibilityMargin;
			meshInstance.CastShadow = CastShadows
				? GeometryInstance3D.ShadowCastingSetting.On
				: GeometryInstance3D.ShadowCastingSetting.Off;

			for (int surface = 0; surface < meshInstance.Mesh.GetSurfaceCount(); surface++)
			{
				if (meshInstance.Mesh.SurfaceGetMaterial(surface) is not StandardMaterial3D source)
				{
					continue;
				}

				string materialName = source.ResourceName.ToString().ToLowerInvariant();
				Color tint = materialName.Contains("leaves") ? LeafTint : BarkTint;
				if (tint.IsEqualApprox(Colors.White))
				{
					continue;
				}

				StandardMaterial3D localMaterial = (StandardMaterial3D)source.Duplicate();
				Color albedo = source.AlbedoColor;
				localMaterial.AlbedoColor = new Color(
					albedo.R * tint.R,
					albedo.G * tint.G,
					albedo.B * tint.B,
					albedo.A);
				localMaterial.ResourceLocalToScene = true;
				meshInstance.SetSurfaceOverrideMaterial(surface, localMaterial);
			}
		}

		foreach (Node child in node.GetChildren())
		{
			ConfigureBranch(child);
		}
	}
}
