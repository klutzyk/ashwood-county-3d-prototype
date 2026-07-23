#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class DamageResistanceItem : ItemDefinition
{
	[Export(PropertyHint.Range, "0.0,0.9,0.05")] public float DamageReduction { get; set; } = 0.25f;
	[Export] public float DurationSeconds { get; set; } = 30.0f;

	public override string UseFeedback =>
		$"Damage resistance active for {Mathf.RoundToInt(DurationSeconds)} seconds.";

	public override string EffectDescription =>
		$"Reduces damage by {Mathf.RoundToInt(DamageReduction * 100.0f)}% for " +
		$"{Mathf.RoundToInt(DurationSeconds)} seconds.";

	public override bool CanUse(Node user)
	{
		PlayerHealth? health = user.GetNodeOrNull<PlayerHealth>("Health");
		return health is not null && !health.IsDead && !health.HasDamageResistance;
	}

	public override bool Use(Node user)
	{
		PlayerHealth? health = user.GetNodeOrNull<PlayerHealth>("Health");
		return health is not null &&
			health.ApplyDamageResistance(DamageReduction, DurationSeconds);
	}
}
