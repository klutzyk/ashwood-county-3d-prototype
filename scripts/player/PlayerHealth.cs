#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerHealth : Node
{
	[Signal]
	public delegate void HealthChangedEventHandler(float currentHealth, float maximumHealth);

	[Signal]
	public delegate void DamagedEventHandler(float damageAmount);

	[Signal]
	public delegate void DiedEventHandler();

	[Export] public float MaximumHealth { get; set; } = 100.0f;
	[Export] public float InvulnerabilityDuration { get; set; } = 0.5f;

	public float CurrentHealth { get; private set; }
	public bool IsDead { get; private set; }
	public bool IsInvulnerable => _invulnerabilityRemaining > 0.0f;

	private float _invulnerabilityRemaining;

	public override void _Ready()
	{
		MaximumHealth = Mathf.Max(MaximumHealth, 1.0f);
		CurrentHealth = MaximumHealth;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaximumHealth);
	}

	public override void _Process(double delta)
	{
		_invulnerabilityRemaining = Mathf.Max(
			_invulnerabilityRemaining - (float)delta,
			0.0f);
	}

	public bool ApplyDamage(float damageAmount)
	{
		if (damageAmount <= 0.0f || IsDead || IsInvulnerable)
		{
			return false;
		}

		CurrentHealth = Mathf.Clamp(CurrentHealth - damageAmount, 0.0f, MaximumHealth);
		_invulnerabilityRemaining = Mathf.Max(InvulnerabilityDuration, 0.0f);
		EmitSignal(SignalName.Damaged, damageAmount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaximumHealth);

		if (CurrentHealth <= 0.0f)
		{
			IsDead = true;
			EmitSignal(SignalName.Died);
		}

		return true;
	}

	public bool RestoreHealth(float amount)
	{
		if (amount <= 0.0f || IsDead || CurrentHealth >= MaximumHealth)
		{
			return false;
		}

		CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0.0f, MaximumHealth);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaximumHealth);
		return true;
	}

	public void RestoreState(float currentHealth)
	{
		CurrentHealth = Mathf.Clamp(currentHealth, 0.0f, MaximumHealth);
		IsDead = CurrentHealth <= 0.0f;
		_invulnerabilityRemaining = 0.0f;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaximumHealth);
		if (IsDead)
		{
			EmitSignal(SignalName.Died);
		}
	}
}
