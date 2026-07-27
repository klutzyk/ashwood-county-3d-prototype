#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Tools;

public enum BuildingSceneCollisionMode
{
	Auto,
	Box,
	Convex,
	Trimesh,
	None,
}

[GlobalClass, Tool]
public partial class BuildingSceneCollisionOverride : Resource
{
	[Export] public NodePath ObjectPath { get; set; } = new();
	[Export] public BuildingSceneCollisionMode Mode { get; set; } =
		BuildingSceneCollisionMode.Auto;
}
