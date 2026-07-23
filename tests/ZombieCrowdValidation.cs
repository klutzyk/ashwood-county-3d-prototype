#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ZombieCrowdValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Node zombies = world.GetNode("Zombies");
			Require(zombies.GetChildCount() == 15, "authored fifteen-zombie population is preserved");

			PrototypeZombie first = zombies.GetNode<PrototypeZombie>("PrototypeZombie1");
			PrototypeZombie second = zombies.GetNode<PrototypeZombie>("PrototypeZombie2");
			foreach (Node child in zombies.GetChildren())
			{
				if (child is not PrototypeZombie zombie)
				{
					continue;
				}

				Require(!zombie.GetNode<NavigationAgent3D>("NavigationAgent3D").AvoidanceEnabled,
					"full navigation avoidance stays disabled in favour of local separation");
				Require(zombie.DistantAwarenessUpdateInterval > zombie.AwarenessUpdateInterval,
					"distant awareness work is throttled");
				Require(zombie.DistantSeparationUpdateInterval > zombie.SeparationUpdateInterval,
					"distant separation work is throttled");
				if (zombie != first && zombie != second)
				{
					zombie.SetAlive(false);
				}
			}

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			player.SetPhysicsProcess(false);
			player.GlobalPosition = new Vector3(0.0f, 1.0f, 5.0f);
			ConfigureCloseChaser(first, new Vector3(-0.08f, 0.9f, 1.0f), player.GlobalPosition);
			ConfigureCloseChaser(second, new Vector3(0.08f, 0.9f, 1.0f), player.GlobalPosition);
			float startingSeparation = HorizontalDistance(first.GlobalPosition, second.GlobalPosition);

			for (int frame = 0; frame < 90; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}

			float endingSeparation = HorizontalDistance(first.GlobalPosition, second.GlobalPosition);
			Require(endingSeparation > startingSeparation + 0.25f,
				"nearby chasers steer apart instead of remaining overlapped");

			first.SetAlive(false);
			Require(!first.IsPhysicsProcessing(), "dead zombie stops all behaviour processing");
			Require(!first.GetNode<NavigationAgent3D>("NavigationAgent3D").AvoidanceEnabled,
				"dead zombie leaves navigation avoidance inactive");

			GD.Print("ZOMBIE_CROWD_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ZOMBIE_CROWD_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ConfigureCloseChaser(
		PrototypeZombie zombie,
		Vector3 position,
		Vector3 playerPosition)
	{
		zombie.GlobalPosition = position;
		zombie.DetectionRadius = 20.0f;
		zombie.FieldOfViewDegrees = 180.0f;
		zombie.AwarenessUpdateInterval = 0.02f;
		zombie.DistantAwarenessUpdateInterval = 0.08f;
		zombie.SeparationUpdateInterval = 0.02f;
		zombie.MoveSpeed = 1.2f;
		zombie.Acceleration = 12.0f;
		zombie.LookAt(playerPosition, Vector3.Up, true);
	}

	private static float HorizontalDistance(Vector3 first, Vector3 second)
	{
		return new Vector2(first.X, first.Z).DistanceTo(new Vector2(second.X, second.Z));
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
