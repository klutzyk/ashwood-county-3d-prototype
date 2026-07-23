#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Zombies;

[GlobalClass]
public partial class ZombieVariantProfile : Resource
{
	[Export] public StringName Identifier { get; set; } = new();
	[Export] public string DisplayName { get; set; } = string.Empty;
	[Export] public float MovementSpeed { get; set; } = 0.36f;
	[Export] public float MaximumHealth { get; set; } = 100.0f;
	[Export] public float AttackDamage { get; set; } = 20.0f;
	[Export] public float DetectionRange { get; set; } = 12.0f;
	[Export(PropertyHint.Range, "0.25,2.0,0.05")] public float HearingSensitivity { get; set; } = 1.0f;
	[Export] public float SearchDuration { get; set; } = 3.0f;
	[Export] public Color MaterialTint { get; set; } = Colors.White;
}
