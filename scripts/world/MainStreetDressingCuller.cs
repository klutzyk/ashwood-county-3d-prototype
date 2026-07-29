#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Applies conservative visibility ranges to the imported geometry in the
/// hand-authored Main Street dressing scene. Imported scenes do not all ship
/// with Godot visibility ranges, so the pass is centralized here rather than
/// modifying third-party assets.
/// </summary>
public partial class MainStreetDressingCuller : Node3D
{
	[Export(PropertyHint.Range, "20,120,1")]
	public float MicroDetailRange { get; set; } = 48.0f;

	[Export(PropertyHint.Range, "30,160,1")]
	public float VegetationRange { get; set; } = 72.0f;

	[Export(PropertyHint.Range, "40,180,1")]
	public float PropRange { get; set; } = 92.0f;

	[Export(PropertyHint.Range, "50,220,1")]
	public float StoryClusterRange { get; set; } = 118.0f;

	[Export(PropertyHint.Range, "0,20,0.5")]
	public float FadeMargin { get; set; } = 8.0f;

	public override void _Ready()
	{
		ApplyVisibilityRanges(this, PropRange);
	}

	private void ApplyVisibilityRanges(Node node, float inheritedRange)
	{
		float range = RangeForNode(node, inheritedRange);

		if (node is GeometryInstance3D geometry)
		{
			if (geometry.VisibilityRangeEnd <= 0.0f)
			{
				geometry.VisibilityRangeEnd = range;
			}

			if (geometry.VisibilityRangeEndMargin <= 0.0f)
			{
				geometry.VisibilityRangeEndMargin = FadeMargin;
			}
		}

		foreach (Node child in node.GetChildren())
		{
			ApplyVisibilityRanges(child, range);
		}
	}

	private float RangeForNode(Node node, float inheritedRange)
	{
		return node.Name.ToString() switch
		{
			"VegetationReclaim" => VegetationRange,
			"RefuseDebris" => MicroDetailRange,
			"ServiceClutter" => PropRange,
			"DamagedStreetFurniture" => PropRange,
			"StoryClusters" => StoryClusterRange,
			_ => inheritedRange,
		};
	}
}
