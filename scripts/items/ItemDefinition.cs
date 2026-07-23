#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Items;

public enum ItemCategory
{
	Medical,
	Food,
	Drink,
	CraftingMaterial,
	Objective,
}

[GlobalClass]
public partial class ItemDefinition : Resource
{
	[Export] public StringName ItemId { get; set; } = new();
	[Export] public string DisplayName { get; set; } = string.Empty;
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
	[Export] public int StackLimit { get; set; } = 1;
	[Export] public ItemCategory Category { get; set; }
	[Export] public Texture2D? Icon { get; set; }
	[Export(PropertyHint.MultilineText)] public string UseMessage { get; set; } = string.Empty;

	public virtual string UseFeedback => UseMessage;
	public virtual string EffectDescription =>
		string.IsNullOrWhiteSpace(UseMessage) ? "No direct use effect." : UseMessage;

	public virtual bool CanUse(Node user)
	{
		return !string.IsNullOrWhiteSpace(UseMessage);
	}

	public virtual bool Use(Node user)
	{
		return CanUse(user);
	}
}
