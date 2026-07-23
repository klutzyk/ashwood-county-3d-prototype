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
	public bool HasDamageResistance => _damageResistanceRemaining > 0.0f;
	public float DamageResistanceRemaining => _damageResistanceRemaining;

	private float _invulnerabilityRemaining;
	private float _damageResistanceRemaining;
	private float _damageTakenMultiplier = 1.0f;

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
		_damageResistanceRemaining = Mathf.Max(
			_damageResistanceRemaining - (float)delta,
			0.0f);
		if (_damageResistanceRemaining <= 0.0f)
		{
			_damageTakenMultiplier = 1.0f;
		}
	}

	public bool ApplyDamage(float damageAmount)
	{
		if (damageAmount <= 0.0f || IsDead || IsInvulnerable)
		{
			return false;
		}

		float appliedDamage = damageAmount * _damageTakenMultiplier;
		CurrentHealth = Mathf.Clamp(CurrentHealth - appliedDamage, 0.0f, MaximumHealth);
		_invulnerabilityRemaining = Mathf.Max(InvulnerabilityDuration, 0.0f);
		EmitSignal(SignalName.Damaged, appliedDamage);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaximumHealth);

		if (CurrentHealth <= 0.0f)
		{
			IsDead = true;
			EmitSignal(SignalName.Died);
		}

		return true;
	}

	public bool ApplyDamageResistance(float damageReduction, float duration)
	{
		if (IsDead || HasDamageResistance || damageReduction <= 0.0f || duration <= 0.0f)
		{
			return false;
		}

		_damageTakenMultiplier = 1.0f - Mathf.Clamp(damageReduction, 0.0f, 0.9f);
		_damageResistanceRemaining = duration;
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
		_damageResistanceRemaining = 0.0f;
		_damageTakenMultiplier = 1.0f;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaximumHealth);
		if (IsDead)
		{
			EmitSignal(SignalName.Died);
		}
	}
}
