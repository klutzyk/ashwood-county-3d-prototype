#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Tools;

public enum BuildingInteriorElementType
{
	Wall = 0,
	Floor = 2,
	Ceiling = 3,
	Counter = 1,
	Fixture = 4,
}

[GlobalClass, Tool]
public partial class BuildingInteriorElement : Resource
{
	[Export] public StringName Name { get; set; } = new("Element");
	[Export] public BuildingInteriorElementType Type { get; set; }
	[Export] public Vector3 Position { get; set; }
	[Export] public Vector3 RotationDegrees { get; set; }
	[Export] public Vector3 Size { get; set; } = Vector3.One;
	[Export] public Material? Material { get; set; }
	[Export] public bool Enabled { get; set; } = true;
	[Export] public bool GenerateVisual { get; set; } = true;
	[Export] public bool GenerateCollision { get; set; } = true;
}
