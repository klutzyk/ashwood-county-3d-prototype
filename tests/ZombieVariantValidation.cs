#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ZombieVariantValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Godot.Collections.Array<Node> nodes =
				GetTree().GetNodesInGroup(PrototypeZombie.ZombieGroupName);
			Require(nodes.Count == 15, "variant assignment preserves the fifteen-zombie population");
			Dictionary<string, int> counts = new(StringComparer.Ordinal);
			foreach (Node node in nodes)
			{
				PrototypeZombie zombie = (PrototypeZombie)node;
				ZombieVariantProfile profile = zombie.VariantProfile
					?? throw new InvalidOperationException("zombie is missing a variant profile");
				string identifier = profile.Identifier.ToString();
				counts[identifier] = counts.GetValueOrDefault(identifier) + 1;

				ZombieHealth health = zombie.GetNode<ZombieHealth>("Health");
				Require(Mathf.IsEqualApprox(zombie.MoveSpeed, profile.MovementSpeed) &&
					Mathf.IsEqualApprox(zombie.AttackDamage, profile.AttackDamage) &&
					Mathf.IsEqualApprox(zombie.DetectionRadius, profile.DetectionRange) &&
					Mathf.IsEqualApprox(zombie.HearingSensitivity, profile.HearingSensitivity) &&
					Mathf.IsEqualApprox(zombie.PlayerSearchDuration, profile.SearchDuration) &&
					Mathf.IsEqualApprox(health.MaximumHealth, profile.MaximumHealth),
					$"{profile.DisplayName} applies all reusable behaviour tuning");
				Require(profile.MaterialTint.R >= 0.8f && profile.MaterialTint.G >= 0.8f &&
					profile.MaterialTint.B >= 0.8f,
					$"{profile.DisplayName} tint remains subtle and believable");
				Require(HasTintedMaterial(zombie.GetNode<Node3D>("Visual")),
					$"{profile.DisplayName} uses project-owned material overrides");
				Require(zombie.GetNode<SearchableContainer>("CorpseLoot") is not null,
					$"{profile.DisplayName} preserves corpse loot");
			}

			Require(counts.GetValueOrDefault("slow_walker") == 7 &&
				counts.GetValueOrDefault("walker") == 6 &&
				counts.GetValueOrDefault("runner") == 2,
				"all three profiles are assigned and runners remain uncommon");

			ZombieVariantProfile slow =
				GD.Load<ZombieVariantProfile>("res://assets/zombies/slow_walker.tres");
			ZombieVariantProfile walker =
				GD.Load<ZombieVariantProfile>("res://assets/zombies/walker.tres");
			ZombieVariantProfile runner =
				GD.Load<ZombieVariantProfile>("res://assets/zombies/runner.tres");
			Require(slow.MovementSpeed < walker.MovementSpeed &&
				walker.MovementSpeed < runner.MovementSpeed,
				"movement speeds clearly order Slow Walker, Walker and Runner");

			GD.Print("ZOMBIE_VARIANT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ZOMBIE_VARIANT_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static bool HasTintedMaterial(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D mesh && mesh.Mesh is not null)
			{
				for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
				{
					if (mesh.GetSurfaceOverrideMaterial(surface) is not null)
					{
						return true;
					}
				}
			}
			if (HasTintedMaterial(child))
			{
				return true;
			}
		}
		return false;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
