#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.UI;

/// <summary>
/// Presents a restrained centre-screen confirmation only when a melee swing
/// actually connects. It stays quiet during misses so the animation and sound
/// remain the primary combat presentation.
/// </summary>
public partial class CombatFeedbackDisplay : Control
{
	[Export] public NodePath MeleeCombatPath { get; set; } =
		new("../../Player/MeleeCombat");

	[Export(PropertyHint.Range, "0.05,0.5,0.01")]
	public float MarkerDuration { get; set; } = 0.18f;

	private PlayerMeleeCombat _meleeCombat = null!;
	private Label _hitMarker = null!;
	private Label _weaponReadout = null!;
	private float _markerRemaining;

	public override void _Ready()
	{
		_meleeCombat = GetNode<PlayerMeleeCombat>(MeleeCombatPath);
		_hitMarker = GetNode<Label>("HitMarker");
		_weaponReadout = GetNode<Label>("WeaponReadout");
		_meleeCombat.HitConfirmed += ShowHitMarker;
		_meleeCombat.WeaponEquipped += UpdateWeaponReadout;
		UpdateWeaponReadout(
			_meleeCombat.EquippedWeaponSlot,
			_meleeCombat.WeaponDefinition?.DisplayName ?? "Unarmed");
		SetMarkerAlpha(0.0f);
		SetProcess(false);
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_meleeCombat))
		{
			_meleeCombat.HitConfirmed -= ShowHitMarker;
			_meleeCombat.WeaponEquipped -= UpdateWeaponReadout;
		}
	}

	public override void _Process(double delta)
	{
		_markerRemaining = Mathf.Max(_markerRemaining - (float)delta, 0.0f);
		float duration = Mathf.Max(MarkerDuration, 0.001f);
		float progress = 1.0f - (_markerRemaining / duration);
		float alpha = Mathf.Pow(1.0f - progress, 1.4f);
		SetMarkerAlpha(alpha);
		_hitMarker.Scale = Vector2.One * Mathf.Lerp(1.18f, 0.86f, progress);
		if (_markerRemaining <= 0.0f)
		{
			SetMarkerAlpha(0.0f);
			SetProcess(false);
		}
	}

	private void ShowHitMarker(int targetCount)
	{
		_markerRemaining = Mathf.Max(MarkerDuration, 0.0f);
		_hitMarker.Scale = Vector2.One * (targetCount > 1 ? 1.35f : 1.18f);
		SetMarkerAlpha(1.0f);
		SetProcess(_markerRemaining > 0.0f);
	}

	private void UpdateWeaponReadout(int slot, string displayName)
	{
		_weaponReadout.Text = $"MELEE  {slot + 1}  {displayName.ToUpperInvariant()}";
	}

	private void SetMarkerAlpha(float alpha)
	{
		_hitMarker.Modulate = new Color(1.0f, 0.88f, 0.72f, Mathf.Clamp(alpha, 0.0f, 1.0f));
	}
}
