#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerNeeds : Node
{
	[Signal]
	public delegate void HungerChangedEventHandler(float currentHunger, float maximumHunger);

	[Signal]
	public delegate void ThirstChangedEventHandler(float currentThirst, float maximumThirst);

	[Export] public float MaximumHunger { get; set; } = 100.0f;
	[Export] public float MaximumThirst { get; set; } = 100.0f;
	[Export] public float HungerDecreasePerSecond { get; set; } = 0.08f;
	[Export] public float ThirstDecreasePerSecond { get; set; } = 0.12f;
	[Export] public float ZeroNeedHealthLossPerSecond { get; set; } = 2.0f;
	[Export] public float HealthLossTickInterval { get; set; } = 1.0f;

	public float CurrentHunger { get; private set; }
	public float CurrentThirst { get; private set; }

	private PlayerHealth _health = null!;
	private float _healthLossElapsed;

	public override void _Ready()
	{
		MaximumHunger = Mathf.Max(MaximumHunger, 1.0f);
		MaximumThirst = Mathf.Max(MaximumThirst, 1.0f);
		CurrentHunger = MaximumHunger;
		CurrentThirst = MaximumThirst;
		_health = GetParent<ThirdPersonPlayer>().GetNode<PlayerHealth>("Health");
		EmitNeedsChanged();
	}

	public override void _Process(double delta)
	{
		if (_health.IsDead)
		{
			return;
		}

		float deltaTime = (float)delta;
		SetNeeds(
			CurrentHunger - (Mathf.Max(HungerDecreasePerSecond, 0.0f) * deltaTime),
			CurrentThirst - (Mathf.Max(ThirstDecreasePerSecond, 0.0f) * deltaTime));
		UpdateHealthLoss(deltaTime);
	}

	public bool RestoreHunger(float amount)
	{
		if (amount <= 0.0f || CurrentHunger >= MaximumHunger)
		{
			return false;
		}

		SetNeeds(CurrentHunger + amount, CurrentThirst);
		return true;
	}

	public bool RestoreThirst(float amount)
	{
		if (amount <= 0.0f || CurrentThirst >= MaximumThirst)
		{
			return false;
		}

		SetNeeds(CurrentHunger, CurrentThirst + amount);
		return true;
	}

	public void RestoreState(float hunger, float thirst)
	{
		SetNeeds(hunger, thirst);
		_healthLossElapsed = 0.0f;
	}

	private void SetNeeds(float hunger, float thirst)
	{
		float nextHunger = Mathf.Clamp(hunger, 0.0f, MaximumHunger);
		float nextThirst = Mathf.Clamp(thirst, 0.0f, MaximumThirst);
		bool hungerChanged = !Mathf.IsEqualApprox(CurrentHunger, nextHunger);
		bool thirstChanged = !Mathf.IsEqualApprox(CurrentThirst, nextThirst);
		CurrentHunger = nextHunger;
		CurrentThirst = nextThirst;

		if (hungerChanged)
		{
			EmitSignal(SignalName.HungerChanged, CurrentHunger, MaximumHunger);
		}
		if (thirstChanged)
		{
			EmitSignal(SignalName.ThirstChanged, CurrentThirst, MaximumThirst);
		}
	}

	private void UpdateHealthLoss(float delta)
	{
		int emptyNeedCount = (CurrentHunger <= 0.0f ? 1 : 0) +
			(CurrentThirst <= 0.0f ? 1 : 0);
		if (emptyNeedCount == 0)
		{
			_healthLossElapsed = 0.0f;
			return;
		}

		_healthLossElapsed += delta;
		float interval = Mathf.Max(HealthLossTickInterval, 0.1f);
		if (_healthLossElapsed < interval)
		{
			return;
		}

		_healthLossElapsed -= interval;
		_health.ApplyDamage(
			Mathf.Max(ZeroNeedHealthLossPerSecond, 0.0f) * interval * emptyNeedCount);
	}

	private void EmitNeedsChanged()
	{
		EmitSignal(SignalName.HungerChanged, CurrentHunger, MaximumHunger);
		EmitSignal(SignalName.ThirstChanged, CurrentThirst, MaximumThirst);
	}
}
