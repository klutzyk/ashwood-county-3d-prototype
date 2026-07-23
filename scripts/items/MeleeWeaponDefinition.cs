#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class MeleeWeaponDefinition : Resource
{
	[Export] public StringName Identifier { get; set; } = new();
	[Export] public string DisplayName { get; set; } = string.Empty;
	[Export] public float Damage { get; set; } = 40.0f;
	[Export] public float Range { get; set; } = 2.2f;
	[Export(PropertyHint.Range, "1,180,1")] public float AttackArcDegrees { get; set; } = 85.0f;
	[Export] public float Cooldown { get; set; } = 0.65f;
	[Export] public float Knockback { get; set; } = 5.0f;
	[Export] public float NoiseRadius { get; set; } = 12.0f;
}
