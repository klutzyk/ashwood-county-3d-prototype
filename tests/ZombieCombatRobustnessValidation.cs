#nullable enable

using System;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ZombieCombatRobustnessValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerHealth playerHealth = player.GetNode<PlayerHealth>("Health");
			PrototypeZombie zombie =
				world.GetNode<PrototypeZombie>("Zombies/PrototypeZombie1");
			foreach (Node child in world.GetNode("Zombies").GetChildren())
			{
				if (child is PrototypeZombie other && other != zombie)
				{
					other.SetAlive(false);
				}
			}

			player.SetPhysicsProcess(false);
			player.GlobalPosition = new Vector3(0.0f, 1.0f, 1.3f);
			zombie.GlobalPosition = new Vector3(0.0f, 0.9f, 0.0f);
			zombie.DetectionRadius = 20.0f;
			zombie.FieldOfViewDegrees = 180.0f;
			zombie.CloseAwarenessRadius = 2.0f;
			zombie.AwarenessUpdateInterval = 0.01f;
			zombie.DistantAwarenessUpdateInterval = 0.01f;
			zombie.AttackDistance = 1.6f;
			zombie.AttackDisengageDistance = 2.0f;
			zombie.AttackHitMoment = 0.18f;
			zombie.AttackRecoveryDuration = 0.12f;
			zombie.AttackCooldown = 0.34f;
			zombie.AttackLungeSpeed = 0.0f;
			zombie.MoveSpeed = 0.0f;
			zombie.TurnSpeed = 0.0f;
			zombie.LookAt(player.GlobalPosition, Vector3.Up, true);

			await WaitFor(() => zombie.CurrentStateName == "Attacking", 120,
				"zombie did not enter its attack windup");
			AnimationPlayer zombieAnimation = FindDescendant<AnimationPlayer>(zombie)
				?? throw new InvalidOperationException("zombie animation player is missing");
			zombieAnimation.Active = false;

			int attemptBeforeDodge = zombie.AttackAttemptCount;
			player.GlobalPosition = new Vector3(0.0f, 1.0f, -1.25f);
			await WaitFor(
				() => zombie.AttackAttemptCount > attemptBeforeDodge,
				120,
				"attack phase did not advance while animation processing was disabled");
			Require(Mathf.IsEqualApprox(
				playerHealth.CurrentHealth,
				playerHealth.MaximumHealth),
				"a player who dodges behind the committed arc is not hit by a stale range check");
			Require(!zombie.LastAttackConnected,
				"the authoritative attack phase records the dodged swing as a miss");

			player.GlobalPosition = new Vector3(0.0f, 1.0f, 1.3f);
			int successBefore = zombie.SuccessfulAttackCount;
			await WaitFor(
				() => zombie.SuccessfulAttackCount > successBefore,
				180,
				"valid frontal attack never connected");
			Require(Mathf.IsEqualApprox(
				playerHealth.CurrentHealth,
				playerHealth.MaximumHealth - zombie.AttackDamage),
				"a visible in-range frontal target receives exactly one attack payload");
			Require(player.IsDamageFeedbackActive && player.IsDamageStaggered,
				"confirmed zombie contact starts directional player feedback and a short stagger");

			playerHealth.RestoreState(playerHealth.MaximumHealth);
			StaticBody3D blocker = CreateAttackBlocker();
			world.AddChild(blocker);
			blocker.GlobalPosition = new Vector3(0.0f, 1.0f, 0.65f);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			int attemptsBeforeOcclusion = zombie.AttackAttemptCount;
			await WaitFor(() => zombie.CurrentStateName != "Attacking", 120,
				"live line-of-sight obstruction did not interrupt the attack state");
			for (int frame = 0; frame < 30; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			Require(Mathf.IsEqualApprox(
				playerHealth.CurrentHealth,
				playerHealth.MaximumHealth),
				"a wall blocks zombie damage rather than allowing attacks through geometry");
			Require(zombie.AttackAttemptCount == attemptsBeforeOcclusion,
				"occluded pursuit does not continue an invisible attack cycle");

			blocker.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await WaitFor(() => zombie.CurrentStateName == "Attacking", 180,
				"zombie did not reacquire after the obstruction was removed");
			playerHealth.ApplyDamage(playerHealth.MaximumHealth * 2.0f);
			await WaitFor(
				() => zombie.CurrentStateName is not "Attacking" and not "Chasing" and not "SearchingPlayer",
				60,
				"zombie kept attacking a dead player");

			world.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GD.Print("ZOMBIE_COMBAT_ROBUSTNESS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ZOMBIE_COMBAT_ROBUSTNESS_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static StaticBody3D CreateAttackBlocker()
	{
		StaticBody3D blocker = new()
		{
			Name = "AttackLineOfSightBlocker",
			CollisionLayer = 1,
			CollisionMask = 1,
		};
		CollisionShape3D collision = new()
		{
			Shape = new BoxShape3D
			{
				Size = new Vector3(2.0f, 3.0f, 0.4f),
			},
		};
		blocker.AddChild(collision);
		return blocker;
	}

	private async Task WaitFor(Func<bool> predicate, int maximumFrames, string failureMessage)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (predicate())
			{
				return;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}

		throw new InvalidOperationException(failureMessage);
	}

	private static T? FindDescendant<T>(Node node) where T : Node
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is T match)
			{
				return match;
			}
			T? descendant = FindDescendant<T>(child);
			if (descendant is not null)
			{
				return descendant;
			}
		}
		return null;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
