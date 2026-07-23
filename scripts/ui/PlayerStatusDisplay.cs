#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.UI;

public partial class PlayerStatusDisplay : CanvasLayer
{
	[Export] public NodePath HealthPath { get; set; } = new("../Player/Health");
	[Export] public NodePath StaminaPath { get; set; } = new("../Player/Stamina");
	[Export] public float DamageFlashDuration { get; set; } = 0.2f;
	[Export] public float DamageFlashOpacity { get; set; } = 0.24f;

	private PlayerHealth _health = null!;
	private PlayerStamina _stamina = null!;
	private Label _healthLabel = null!;
	private ProgressBar _staminaBar = null!;
	private ColorRect _damageFlash = null!;
	private Control _deathOverlay = null!;
	private float _damageFlashRemaining;

	public override void _Ready()
	{
		_health = GetNode<PlayerHealth>(HealthPath);
		_stamina = GetNode<PlayerStamina>(StaminaPath);
		_healthLabel = GetNode<Label>("HealthLabel");
		_staminaBar = GetNode<ProgressBar>("StaminaBar");
		_damageFlash = GetNode<ColorRect>("DamageFlash");
		_deathOverlay = GetNode<Control>("DeathOverlay");

		_health.HealthChanged += UpdateHealthLabel;
		_health.Damaged += ShowDamageFlash;
		_health.Died += ShowDeathScreen;
		_stamina.StaminaChanged += UpdateStaminaBar;

		UpdateHealthLabel(_health.CurrentHealth, _health.MaximumHealth);
		UpdateStaminaBar(_stamina.CurrentStamina, _stamina.MaximumStamina);
		SetFlashOpacity(0.0f);
		_deathOverlay.Visible = _health.IsDead;
	}

	public override void _Process(double delta)
	{
		if (_damageFlashRemaining <= 0.0f)
		{
			return;
		}

		_damageFlashRemaining = Mathf.Max(_damageFlashRemaining - (float)delta, 0.0f);
		float duration = Mathf.Max(DamageFlashDuration, 0.001f);
		SetFlashOpacity(DamageFlashOpacity * (_damageFlashRemaining / duration));
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_health.IsDead || @event is not InputEventKey keyEvent ||
			!keyEvent.Pressed || keyEvent.Echo ||
			(keyEvent.Keycode != Key.R && keyEvent.PhysicalKeycode != Key.R))
		{
			return;
		}

		GetViewport().SetInputAsHandled();
		GetTree().ReloadCurrentScene();
	}

	private void UpdateHealthLabel(float currentHealth, float maximumHealth)
	{
		_healthLabel.Text = $"Health: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maximumHealth)}";
		if (_deathOverlay is not null && currentHealth > 0.0f && _deathOverlay.Visible)
		{
			_deathOverlay.Visible = false;
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	private void UpdateStaminaBar(float currentStamina, float maximumStamina)
	{
		_staminaBar.MaxValue = maximumStamina;
		_staminaBar.Value = currentStamina;
	}

	private void ShowDamageFlash(float damageAmount)
	{
		_damageFlashRemaining = Mathf.Max(DamageFlashDuration, 0.0f);
		SetFlashOpacity(DamageFlashOpacity);
	}

	private void ShowDeathScreen()
	{
		_damageFlashRemaining = 0.0f;
		SetFlashOpacity(0.0f);
		_deathOverlay.Visible = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void SetFlashOpacity(float opacity)
	{
		_damageFlash.Modulate = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp(opacity, 0.0f, 1.0f));
	}
}
