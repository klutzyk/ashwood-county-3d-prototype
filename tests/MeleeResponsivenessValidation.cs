#nullable enable

using System;
using Godot;
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
			combat.Cooldown = 0.4f;
			combat.InputBufferDuration = 0.18f;
			combat.SetProcess(false);

			ZombieHealth targetHealth = target.GetNode<ZombieHealth>("Health");
			int attacksStarted = 0;
			combat.AttackStarted += () => attacksStarted++;

			Require(combat.TryAttack(), "ready attack starts immediately");
			Require(combat.IsAttacking && attacksStarted == 1, "attack start is visible in the input frame");
			combat._Process(0.1);
			Require(Mathf.IsEqualApprox(targetHealth.CurrentHealth, targetHealth.MaximumHealth),
				"damage waits for the configured impact moment");
			combat._Process(0.04);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - combat.Damage),
				"one hit lands as the bat crosses the target");
			combat._Process(0.12);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - combat.Damage),
				"one swing cannot damage the same target twice");

			Require(combat.RequestAttack(), "late cooldown input is buffered");
			combat._Process(0.04);
			combat._Process(0.11);
			combat._Process(0.01);
			Require(attacksStarted == 2, "buffered input starts exactly when cooldown becomes ready");
			combat._Process(0.13);
			Require(Mathf.IsEqualApprox(
				targetHealth.CurrentHealth,
				targetHealth.MaximumHealth - (combat.Damage * 2.0f)),
				"second swing applies one consistent hit");
			combat._Process(0.2);
			combat._Process(0.1);
			Require(combat.CanAttack && combat.IsShowingReadyFeedback,
				"bat returns to its lightweight ready pose after cooldown");

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
