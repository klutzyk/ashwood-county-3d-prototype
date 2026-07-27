#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Tools;

[GlobalClass, Tool]
public partial class BuildingInteriorLayout : Resource
{
	[Export]
	public Godot.Collections.Array<BuildingInteriorElement> Elements { get; set; } =
		new();
}
