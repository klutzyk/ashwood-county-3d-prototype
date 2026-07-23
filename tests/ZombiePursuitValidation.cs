#nullable enable

using System;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Gameplay;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ZombiePursuitValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PrototypeZombie zombie = world.GetNode<PrototypeZombie>("Zombies/PrototypeZombie1");
			foreach (Node child in world.GetNode("Zombies").GetChildren())
			{
				if (child is PrototypeZombie other && other != zombie)
				{
					other.SetAlive(false);
				}
			}

			player.GlobalPosition = new Vector3(0.0f, 1.0f, 5.0f);
			zombie.GlobalPosition = new Vector3(0.0f, 0.9f, 1.0f);
			zombie.DetectionRadius = 20.0f;
			zombie.FieldOfViewDegrees = 180.0f;
			zombie.AwarenessUpdateInterval = 0.02f;
			zombie.DistantAwarenessUpdateInterval = 0.02f;
			zombie.LostSightGracePeriod = 0.08f;
			zombie.MoveSpeed = 8.0f;
			zombie.Acceleration = 30.0f;
			zombie.PlayerSearchDuration = 0.35f;
			zombie.PlayerSearchTargetInterval = 0.1f;
			zombie.LookAt(player.GlobalPosition, Vector3.Up, true);

			await WaitForState(zombie, "Chasing", 120);
			GameplayNoise.Emit(new Vector3(4.0f, 0.0f, 4.0f), 30.0f, GameplayNoiseCategory.Door);
			player.GlobalPosition = new Vector3(30.0f, 1.0f, 30.0f);

			await WaitForState(zombie, "SearchingPlayer", 180);
			await WaitForState(zombie, "Investigating", 360);

			zombie.SetAlive(false);
			Require(!zombie.IsPhysicsProcessing(), "dead zombie stops physics processing");
			Require(!zombie.IsAlive, "dead zombie remains inactive");

			GD.Print("ZOMBIE_PURSUIT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ZOMBIE_PURSUIT_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private async Task WaitForState(PrototypeZombie zombie, string expectedState, int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (zombie.CurrentStateName == expectedState)
			{
				return;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}

		throw new InvalidOperationException(
			$"zombie did not enter {expectedState}; current state is {zombie.CurrentStateName}");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
