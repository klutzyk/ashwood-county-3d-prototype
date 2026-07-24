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
			target.GlobalPosition = new Vector3(0.0f, 0.9f, 1.5f);
			target.SetPhysicsProcess(false);
			combat.AttackDuration = 0.3f;
			combat.HitMoment = 0.45f;
			combat.ComboQueueOpenMoment = 0.38f;
			combat.QuickComboInputWindow = 0.2f;
			combat.MaximumComboAttacks = 3;
			weapon.Cooldown = 0.2f;
			weapon.Damage = 20.0f;
			combat.InputBufferDuration = 0.18f;
			combat.SetProcess(false);

			ZombieHealth targetHealth = target.GetNode<ZombieHealth>("Health");
			ZombieAudioFeedback targetAudio =
				target.GetNode<ZombieAudioFeedback>("AudioFeedback");
			int attacksStarted = 0;
			combat.AttackStarted += () => attacksStarted++;

			Require(combat.TryAttack(), "ready attack starts immediately");
			Require(combat.IsAttacking && attacksStarted == 1 && combat.ComboStep == 1,
				"attack start is visible in the input frame");
			combat._Process(0.1);
			Require(Mathf.IsEqualApprox(targetHealth.CurrentHealth, targetHealth.MaximumHealth),
				"damage waits for the configured impact moment");
			Require(!combat.RequestAttack(),
				"early mashing cannot queue before the anticipation completes");
			combat._Process(0.04);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - weapon.Damage),
				"one hit lands as the bat crosses the target");
			Require(target.IsHitStunned && target.CurrentAnimationName == "HitReaction",
				"impact starts the authored zombie reaction and brief hit stun");
			Require(targetAudio.LastCueName == nameof(ZombieAudioCue.Hurt),
				"impact triggers localized zombie hurt feedback at contact");
			Require(target.ActiveKnockbackVelocity.Z > 0.0f,
				"knockback follows the attack direction");
			Require(player.IsMeleeImpactFeedbackActive,
				"camera feedback starts only after a confirmed hit");
			Require(combat.RequestAttack(), "input during follow-through queues combo step two");
			Require(!combat.RequestAttack(), "only one attack can be queued at a time");
			combat._Process(0.16);
			Require(attacksStarted == 2 && combat.ComboStep == 2,
				"queued step two starts after the first follow-through");
			combat._Process(0.14);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - (weapon.Damage * 2.0f)),
				"combo step two applies one consistent hit");

			Require(combat.RequestAttack(), "input during recovery queues combo step three");
			combat._Process(0.16);
			Require(attacksStarted == 3 && combat.ComboStep == 3,
				"combo is capped at its authored third step");
			combat._Process(0.14);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - (weapon.Damage * 3.0f)),
				"third swing applies one consistent hit");
			Require(!combat.RequestAttack(), "a fourth attack cannot be chained");
			combat._Process(0.16);
			Require(!combat.CanAttack && !combat.RequestAttack(),
				"full recovery blocks immediate animation spam");
			combat._Process(0.03);
			Require(combat.RequestAttack(), "late recovery input uses the short buffer");
			combat._Process(0.17);
			Require(attacksStarted == 4 && combat.ComboStep == 1,
				"a recovered attack starts a fresh combo");

			GD.Print("MELEE_RESPONSIVENESS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"MELEE_RESPONSIVENESS_VALIDATION: FAIL - {exception.Message}");
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
