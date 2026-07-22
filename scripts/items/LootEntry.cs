#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class LootEntry : Resource
{
	[Export] public ItemDefinition? Item { get; set; }
	[Export(PropertyHint.Range, "0,1,0.01")] public float Weight { get; set; } = 1.0f;
	[Export(PropertyHint.Range, "1,99,1")] public int MinimumQuantity { get; set; } = 1;
	[Export(PropertyHint.Range, "1,99,1")] public int MaximumQuantity { get; set; } = 1;
}
