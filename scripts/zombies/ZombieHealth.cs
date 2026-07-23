#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Zombies;

public partial class ZombieHealth : Node
{
	[Signal]
	public delegate void DamagedEventHandler(float damageAmount);

	[Signal]
	public delegate void DiedEventHandler();

	[Export] public float MaximumHealth { get; set; } = 100.0f;

	public float CurrentHealth { get; private set; }
	public bool IsDead => CurrentHealth <= 0.0f;

	public override void _Ready()
	{
		MaximumHealth = Mathf.Max(MaximumHealth, 1.0f);
		CurrentHealth = MaximumHealth;
	}

	public bool ApplyDamage(float damageAmount)
	{
		if (damageAmount <= 0.0f || IsDead)
		{
			return false;
		}

		CurrentHealth = Mathf.Max(CurrentHealth - damageAmount, 0.0f);
		EmitSignal(SignalName.Damaged, damageAmount);
		if (IsDead)
		{
			EmitSignal(SignalName.Died);
		}
		return true;
	}

	public void RestoreAliveState(bool isAlive)
	{
		CurrentHealth = isAlive ? MaximumHealth : 0.0f;
	}
}
