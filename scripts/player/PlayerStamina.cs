#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerStamina : Node
{
	[Signal]
	public delegate void StaminaChangedEventHandler(float currentStamina, float maximumStamina);

	[Export] public float MaximumStamina { get; set; } = 100.0f;
	[Export] public float DrainRate { get; set; } = 25.0f;
	[Export] public float RegenerationRate { get; set; } = 20.0f;
	[Export] public float RegenerationDelay { get; set; } = 1.0f;
	[Export] public float RecoveryThreshold { get; set; } = 20.0f;

	public float CurrentStamina { get; private set; }
	public bool CanSprint { get; private set; } = true;

	private float _timeSinceSprinting;

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
			_timeSinceSprinting = 0.0f;
			CurrentStamina = Mathf.Max(CurrentStamina - (Mathf.Max(DrainRate, 0.0f) * delta), 0.0f);
			if (CurrentStamina <= 0.0f)
			{
				CanSprint = false;
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

	private void Regenerate(float delta)
	{
		_timeSinceSprinting += delta;
		if (_timeSinceSprinting < Mathf.Max(RegenerationDelay, 0.0f))
		{
			return;
		}

		CurrentStamina = Mathf.Min(
			CurrentStamina + (Mathf.Max(RegenerationRate, 0.0f) * delta),
			MaximumStamina);
		if (!CanSprint && CurrentStamina >= RecoveryThreshold)
		{
			CanSprint = true;
		}
	}

	private void EmitStaminaChanged()
	{
		EmitSignal(SignalName.StaminaChanged, CurrentStamina, MaximumStamina);
	}
}
