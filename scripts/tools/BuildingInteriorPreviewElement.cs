#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Tools;

[Tool]
public partial class BuildingInteriorPreviewElement : MeshInstance3D
{
	private Material? _elementMaterial;
	private bool _elementEnabled = true;

	[Export] public BuildingInteriorElementType ElementType { get; set; }
	[Export]
	public Material? ElementMaterial
	{
		get => _elementMaterial;
		set
		{
			_elementMaterial = value;
			RefreshMaterial();
		}
	}
	[Export]
	public bool ElementEnabled
	{
		get => _elementEnabled;
		set
		{
			_elementEnabled = value;
			Visible = value;
		}
	}
	[Export] public bool GenerateVisual { get; set; } = true;
	[Export] public bool GenerateCollision { get; set; } = true;

	public override void _Ready()
	{
		RefreshMaterial();
		Visible = _elementEnabled;
	}

	private void RefreshMaterial()
	{
		if (Mesh is BoxMesh boxMesh)
		{
			boxMesh.Material =
				_elementMaterial ??
				BuildingInteriorBuilder.CreateFallbackMaterial();
		}
	}
}
