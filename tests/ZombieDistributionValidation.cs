#nullable enable

using System;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ZombieDistributionValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node zombiesRoot = world.GetNode("Zombies");
			PrototypeZombie[] zombies = zombiesRoot.GetChildren().Cast<PrototypeZombie>().ToArray();
			Require(zombies.Length == 15, "redistribution preserves the existing zombie count");
			for (int index = 0; index < zombies.Length; index++)
			{
				Require(zombies[index].Name == $"PrototypeZombie{index + 1}",
					"zombie node names and save paths remain unchanged");
			}

			Require(CountInArea(zombies, -20, -7, 5, 18) == 3,
				"pharmacy has a three-zombie hotspot");
			Require(CountInArea(zombies, -21, -7, -20, -10) == 3,
				"service station has a three-zombie hotspot");
			Require(CountInArea(zombies, 8, 46, 35, 45) == 4,
				"residential street has four distributed zombies");
			Require(CountInArea(zombies, -4, 4, 47, 54) == 4,
				"northern road end is the dangerous four-zombie hotspot");

			AssertQuiet(zombies, new Vector3(15, 0.9f, 16), 10.0f, "diner");
			AssertQuiet(zombies, new Vector3(5.1f, 0.9f, -8.7f), 10.0f, "safe point");

			GD.Print("ZOMBIE_DISTRIBUTION_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ZOMBIE_DISTRIBUTION_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static int CountInArea(
		PrototypeZombie[] zombies,
		float minimumX,
		float maximumX,
		float minimumZ,
		float maximumZ)
	{
		return zombies.Count(zombie =>
			zombie.Position.X >= minimumX &&
			zombie.Position.X <= maximumX &&
			zombie.Position.Z >= minimumZ &&
			zombie.Position.Z <= maximumZ);
	}

	private static void AssertQuiet(
		PrototypeZombie[] zombies,
		Vector3 centre,
		float radius,
		string areaName)
	{
		Require(zombies.All(zombie => zombie.Position.DistanceTo(centre) >= radius),
			$"{areaName} remains a quiet area");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
