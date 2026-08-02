#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MeleeResponsivenessValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerMeleeCombat combat = player.GetNode<PlayerMeleeCombat>("MeleeCombat");
			MeleeWeaponDefinition weapon = combat.WeaponDefinition
				?? throw new InvalidOperationException("baseball bat definition is missing");
			Require(weapon.Identifier == "baseball_bat" &&
				weapon.DisplayName == "Baseball Bat" &&
				Mathf.IsEqualApprox(weapon.Damage, 40.0f) &&
				Mathf.IsEqualApprox(weapon.Range, 2.2f) &&
				Mathf.IsEqualApprox(weapon.AttackArcDegrees, 85.0f) &&
				Mathf.IsEqualApprox(weapon.Cooldown, 0.28f) &&
				Mathf.IsEqualApprox(weapon.Knockback, 5.0f) &&
				Mathf.IsEqualApprox(weapon.NoiseRadius, 12.0f),
				"baseball bat preserves all combat tuning in reusable weapon data");
			PrototypeZombie target = world.GetNode<PrototypeZombie>("Zombies/PrototypeZombie1");
			foreach (Node child in world.GetNode("Zombies").GetChildren())
			{
				if (child is PrototypeZombie zombie && zombie != target)
				{
					zombie.SetAlive(false);
				}
			}

			player.SetPhysicsProcess(false);
			player.GlobalPosition = new Vector3(0.0f, 1.0f, 0.0f);
			player.GlobalRotation = Vector3.Zero;
			target.GlobalPosition = new Vector3(0.0f, 0.9f, -0.9f);
			target.SetPhysicsProcess(false);
			combat.AttackDuration = 0.3f;
			combat.HitMoment = 0.45f;
			combat.MaximumComboAttacks = 1;
			weapon.Cooldown = 0.2f;
			weapon.Damage = 20.0f;
			combat.InputBufferDuration = 0.18f;
			combat.SetProcess(false);

			ZombieHealth targetHealth = target.GetNode<ZombieHealth>("Health");
			ZombieAudioFeedback targetAudio =
				target.GetNode<ZombieAudioFeedback>("AudioFeedback");
			PlayerStamina stamina = player.GetNode<PlayerStamina>("Stamina");
			PlayerMeleeAudioFeedback combatAudio =
				combat.GetNode<PlayerMeleeAudioFeedback>("AudioFeedback");
			ZombieImpactFeedback impactFeedback =
				target.GetNode<ZombieImpactFeedback>("ImpactFeedback");
			GpuParticles3D bloodSpray =
				impactFeedback.GetNode<GpuParticles3D>("BloodSpray");
			GpuParticles3D debrisSpray =
				impactFeedback.GetNode<GpuParticles3D>("DebrisSpray");
			int attacksStarted = 0;
			combat.AttackStarted += () => attacksStarted++;
			Require(targetAudio.Bus == "Effects",
				"zombie combat transients follow the Effects volume setting");
			Require(bloodSpray.DrawPass1 is CapsuleMesh &&
				debrisSpray.DrawPass1 is PrismMesh &&
				impactFeedback.ImpactLightEnergy <= 0.2f,
				"impact presentation uses rounded streaks, irregular flecks, and a subdued light");

			Require(combat.TryAttack(), "ready attack starts immediately");
			Require(combat.IsAttacking && attacksStarted == 1 && combat.ComboStep == 1,
				"attack start is visible in the input frame");
			Require(combat.HasAssistedTarget && combat.AssistedTarget == target,
				"camera-relative target assistance selects the visible frontal threat");
			Require(Mathf.IsEqualApprox(
				stamina.CurrentStamina,
				stamina.MaximumStamina - combat.AttackStaminaCost),
				"committing a swing spends its stamina exactly once");
			combat._Process(0.1);
			Require(Mathf.IsEqualApprox(targetHealth.CurrentHealth, targetHealth.MaximumHealth),
				"damage waits for the configured impact moment");
			Require(combat.RequestAttack() && combat.QueuedComboAttacks == 1,
				"a deliberate recovery input is buffered before the restart frame");
			combat._Process(0.04);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - weapon.Damage),
				"one hit lands as the bat crosses the target " +
				$"(gap={combat.AssistedTargetContactGap:0.000}, " +
				$"reach={combat.WeaponStrikeSegmentLength:0.000})");
			Require(target.IsHitStunned && target.CurrentAnimationName == "HitReaction",
				"impact starts the authored zombie reaction and brief hit stun");
			Require(target.CurrentStateName == "Staggered" &&
				target.HitReactionVisualStrength >= 0.95f &&
				target.IsImpactFrozen,
				"contact enters an explicit stagger state with a brief synchronized recoil freeze");
			Require(Mathf.Abs(target.HitReactionSide) >= 0.99f &&
				target.HitReactionDirection.Z < -0.5f,
				"stagger deformation preserves the resolved contact side and force direction");
			Vector3 contactOffset =
				target.LastMeleeContactWorldPosition - target.GlobalPosition;
			Vector3 horizontalContactOffset = contactOffset;
			horizontalContactOffset.Y = 0.0f;
			Require(target.ImpactPresentationCount == 1 &&
				contactOffset.Y is >= 0.22f and <= 0.8f &&
				horizontalContactOffset.Length() <= 0.41f &&
				horizontalContactOffset.Z > 0.08f &&
				combat.WeaponStrikeDistanceTo(
					target.LastMeleeContactWorldPosition) <= 0.22f,
				"impact presentation is clamped to the zombie's player-facing torso surface " +
				$"(offset={contactOffset}, gap=" +
				$"{combat.WeaponStrikeDistanceTo(target.LastMeleeContactWorldPosition):0.000}, " +
				$"reach={combat.WeaponStrikeSegmentLength:0.000})");
			Require(targetAudio.LastCueName == nameof(ZombieAudioCue.Hurt),
				"impact triggers localized zombie hurt feedback at contact");
			Require(target.ActiveKnockbackVelocity.Z < 0.0f,
				"knockback follows the attack direction");
			Require(player.IsMeleeImpactFeedbackActive,
				"camera feedback starts only after a confirmed hit");
			Require(combatAudio.LastCueName == nameof(PlayerMeleeAudioCue.FleshImpact),
				"confirmed contact replaces the swing transient with local flesh impact audio");
			Require(combat.IsConfirmedHitPaused,
				"confirmed contact freezes attack progression for a short authored hit-stop");
			Require(!combat.RequestAttack(),
				"a held buffer cannot be duplicated by input spam");
			AdvanceCombat(combat, combat.ConfirmedHitPauseDuration + 0.03f);
			Require(attacksStarted == 2 && combat.IsAttacking,
				"the buffered recovery click restarts after hit-stop at the authored cancel frame");
			AdvanceCombat(combat, 0.15f);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - (weapon.Damage * 2.0f)),
				"restarted attack applies its own impact");
			AdvanceUntilAttackFinished(combat, 1.0f, step: 0.005f);
			Require(!combat.CanAttack && !combat.RequestAttack(),
				"full recovery blocks immediate animation spam");
			AdvanceCombat(combat, 0.03f);
			Require(combat.RequestAttack(), "late recovery input uses the short buffer");
			AdvanceCombat(combat, 0.17f);
			Require(attacksStarted == 3 && combat.ComboStep == 1,
				"a recovered click starts another downward attack");

			world.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GD.Print("MELEE_RESPONSIVENESS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"MELEE_RESPONSIVENESS_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void AdvanceCombat(
		PlayerMeleeCombat combat,
		float duration,
		float step = 0.01f)
	{
		float remaining = Mathf.Max(duration, 0.0f);
		while (remaining > 0.0f)
		{
			float frame = Mathf.Min(remaining, Mathf.Max(step, 0.001f));
			combat._Process(frame);
			remaining -= frame;
		}
	}

	private static void AdvanceUntilAttackFinished(
		PlayerMeleeCombat combat,
		float maximumDuration,
		float step)
	{
		float elapsed = 0.0f;
		while (combat.IsAttacking && elapsed < maximumDuration)
		{
			combat._Process(step);
			elapsed += step;
		}
		if (combat.IsAttacking)
		{
			throw new InvalidOperationException(
				"attack did not finish within the validation time budget");
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
