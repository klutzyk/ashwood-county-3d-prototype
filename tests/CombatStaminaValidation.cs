#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class CombatStaminaValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			ThirdPersonPlayer player = GD.Load<PackedScene>(
				"res://scenes/player/third_person_player.tscn")
				.Instantiate<ThirdPersonPlayer>();
			AddChild(player);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			player.SetPhysicsProcess(false);

			PlayerStamina stamina = player.GetNode<PlayerStamina>("Stamina");
			PlayerMeleeCombat combat = player.GetNode<PlayerMeleeCombat>("MeleeCombat");
			PlayerMeleeAudioFeedback audio =
				combat.GetNode<PlayerMeleeAudioFeedback>("AudioFeedback");
			combat.SetProcess(false);
			audio.MinimumCueInterval = 0.0f;

			Require(stamina.DrainRate > 0.0f,
				"sprint stamina has a real authored drain instead of the prototype zero value");
			float initialStamina = stamina.CurrentStamina;
			stamina.UpdateStamina(isSprinting: true, 1.0f);
			Require(Mathf.IsEqualApprox(
				stamina.CurrentStamina,
				initialStamina - stamina.DrainRate),
				"sprinting drains stamina at the configured rate");

			stamina.RestoreState(stamina.MaximumStamina, canSprint: true);
			float attackCost = combat.RequiredStaminaForNextAttack;
			Require(combat.TryAttack(), "a rested player can commit a melee swing");
			Require(Mathf.IsEqualApprox(
				stamina.CurrentStamina,
				stamina.MaximumStamina - attackCost),
				"melee commitment spends stamina atomically on the input frame");
			combat._Process(combat.AttackDuration);
			combat._Process(combat.WeaponDefinition?.Cooldown ?? 0.0f);

			float staminaAfterAttack = stamina.CurrentStamina;
			stamina.UpdateStamina(isSprinting: false, 0.5f);
			Require(Mathf.IsEqualApprox(stamina.CurrentStamina, staminaAfterAttack),
				"action stamina respects the shared regeneration delay");
			stamina.UpdateStamina(isSprinting: false, 0.5f);
			float expectedRecovery = stamina.RegenerationRate *
				Mathf.Max(1.0f - stamina.RegenerationDelay, 0.0f);
			Require(Mathf.IsEqualApprox(
				stamina.CurrentStamina,
				staminaAfterAttack + expectedRecovery),
				"only time beyond the delay contributes regeneration");

			int rejectedAttacks = 0;
			combat.AttackRejected += _ => rejectedAttacks++;
			stamina.RestoreState(Mathf.Max(attackCost - 0.5f, 0.0f), canSprint: true);
			float staminaBeforeRejectedAttack = stamina.CurrentStamina;
			Require(!combat.RequestAttack() && rejectedAttacks == 1,
				"an unaffordable swing is rejected with explicit feedback");
			Require(Mathf.IsEqualApprox(stamina.CurrentStamina, staminaBeforeRejectedAttack),
				"a rejected swing never consumes partial stamina");
			Require(audio.LastCueName == nameof(PlayerMeleeAudioCue.Exhausted),
				"exhaustion has local audible feedback");
			Require(audio.LightSwingStream is not null &&
				audio.HeavySwingStream is not null &&
				audio.FleshImpactStream is not null,
				"CC0 authored swing and impact cues load with procedural fallback available");

			int exhaustionEvents = 0;
			stamina.Exhausted += () => exhaustionEvents++;
			stamina.RestoreState(1.0f, canSprint: true);
			stamina.UpdateStamina(isSprinting: true, 1.0f);
			Require(stamina.IsExhausted && exhaustionEvents == 1,
				"stamina depletion produces one exhaustion transition");
			stamina.UpdateStamina(isSprinting: false, stamina.RegenerationDelay);
			stamina.UpdateStamina(
				isSprinting: false,
				(stamina.RecoveryThreshold / stamina.RegenerationRate) + 0.02f);
			Require(stamina.CanSprint && !stamina.IsExhausted,
				"sprint returns only after the configured recovery threshold");

			GD.Print("COMBAT_STAMINA_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"COMBAT_STAMINA_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
