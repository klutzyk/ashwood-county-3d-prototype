#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class ItemDefinition : Resource
{
	[Export] public StringName ItemId { get; set; } = new();
	[Export] public string DisplayName { get; set; } = string.Empty;
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;

	public virtual bool Use(Node user)
	{
		return false;
	}
}
