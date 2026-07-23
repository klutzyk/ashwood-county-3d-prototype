#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class BandageItem : ItemDefinition
{
	[Export] public float HealthRestored { get; set; } = 40.0f;
	public override string UseFeedback => $"Restored {Mathf.RoundToInt(HealthRestored)} health.";
	public override string EffectDescription => $"Restores {Mathf.RoundToInt(HealthRestored)} health.";

	public override bool CanUse(Node user)
	{
		PlayerHealth? health = user.GetNodeOrNull<PlayerHealth>("Health");
		return health is not null && !health.IsDead && health.CurrentHealth < health.MaximumHealth;
	}

	public override bool Use(Node user)
	{
		PlayerHealth? health = user.GetNodeOrNull<PlayerHealth>("Health");
		return health is not null && health.RestoreHealth(HealthRestored);
	}
}
