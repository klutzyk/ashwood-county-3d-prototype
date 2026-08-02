#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerStamina : Node
{
	[Signal]
	public delegate void StaminaChangedEventHandler(float currentStamina, float maximumStamina);

	[Signal]
	public delegate void ExhaustedEventHandler();

	[Export] public float MaximumStamina { get; set; } = 100.0f;
	[Export] public float DrainRate { get; set; } = 22.0f;
	[Export] public float RegenerationRate { get; set; } = 20.0f;
	[Export] public float RegenerationDelay { get; set; } = 1.0f;
	[Export] public float RecoveryThreshold { get; set; } = 20.0f;

	public float CurrentStamina { get; private set; }
	public bool CanSprint { get; private set; } = true;
	public bool IsExhausted => !CanSprint;
	public float NormalizedStamina => CurrentStamina / Mathf.Max(MaximumStamina, 1.0f);

	private float _regenerationDelayRemaining;

	public override void _Ready()
	{
		MaximumStamina = Mathf.Max(MaximumStamina, 1.0f);
		RecoveryThreshold = Mathf.Clamp(RecoveryThreshold, 0.0f, MaximumStamina);
		CurrentStamina = MaximumStamina;
		EmitStaminaChanged();
	}

	public void UpdateStamina(bool isSprinting, float delta)
	{
		float previousStamina = CurrentStamina;
		if (isSprinting && CanSprint)
		{
			_regenerationDelayRemaining = Mathf.Max(RegenerationDelay, 0.0f);
			CurrentStamina = Mathf.Max(CurrentStamina - (Mathf.Max(DrainRate, 0.0f) * delta), 0.0f);
			if (CurrentStamina <= 0.0f)
			{
				SetExhausted();
			}
		}
		else
		{
			Regenerate(delta);
		}

		if (!Mathf.IsEqualApprox(CurrentStamina, previousStamina))
		{
			EmitStaminaChanged();
		}
	}

	/// <summary>
	/// Atomically spends stamina for an action and restarts the shared recovery
	/// delay. Failed spends leave the value untouched so callers can safely use
	/// this as the final gate before committing an animation.
	/// </summary>
	public bool TrySpend(float amount)
	{
		float cost = Mathf.Max(amount, 0.0f);
		if (cost <= 0.0f)
		{
			return true;
		}
		if (CurrentStamina + 0.001f < cost)
		{
			return false;
		}

		CurrentStamina = Mathf.Max(CurrentStamina - cost, 0.0f);
		_regenerationDelayRemaining = Mathf.Max(RegenerationDelay, 0.0f);
		if (CurrentStamina <= 0.0f)
		{
			SetExhausted();
		}
		EmitStaminaChanged();
		return true;
	}

	public bool CanSpend(float amount)
	{
		return CurrentStamina + 0.001f >= Mathf.Max(amount, 0.0f);
	}

	public void RestoreState(float currentStamina, bool canSprint)
	{
		CurrentStamina = Mathf.Clamp(currentStamina, 0.0f, MaximumStamina);
		CanSprint = canSprint && CurrentStamina > 0.0f;
		_regenerationDelayRemaining = 0.0f;
		EmitStaminaChanged();
	}

	private void Regenerate(float delta)
	{
		float recoveryTime = Mathf.Max(delta, 0.0f);
		if (_regenerationDelayRemaining > 0.0f)
		{
			float delayTime = Mathf.Min(_regenerationDelayRemaining, recoveryTime);
			_regenerationDelayRemaining -= delayTime;
			recoveryTime -= delayTime;
			if (recoveryTime <= 0.0f)
			{
				return;
			}
		}

		CurrentStamina = Mathf.Min(
			CurrentStamina + (Mathf.Max(RegenerationRate, 0.0f) * recoveryTime),
			MaximumStamina);
		if (!CanSprint && CurrentStamina >= RecoveryThreshold)
		{
			CanSprint = true;
		}
	}

	private void SetExhausted()
	{
		if (!CanSprint)
		{
			return;
		}

		CanSprint = false;
		EmitSignal(SignalName.Exhausted);
	}

	private void EmitStaminaChanged()
	{
		EmitSignal(SignalName.StaminaChanged, CurrentStamina, MaximumStamina);
	}
}
