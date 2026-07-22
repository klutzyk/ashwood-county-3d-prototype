#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class BandageItem : ItemDefinition
{
	[Export] public float HealthRestored { get; set; } = 40.0f;

	public override bool Use(Node user)
	{
		PlayerHealth? health = user.GetNodeOrNull<PlayerHealth>("Health");
		return health is not null && health.RestoreHealth(HealthRestored);
	}
}
