#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class EnvironmentalStorytellingValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D storytelling =
				world.GetNode<Node3D>("Props/EnvironmentalStorytelling");
			string[] arrangementNames =
			{
				"RoadsideBreakdown",
				"LootedPharmacy",
				"BarricadedBackDoor",
				"ServiceStationCamp",
				"EmergencyRoadblock",
				"ResidentialEvacuation",
				"DinerCasualtyTrail",
			};
			Require(storytelling.GetChildCount() == arrangementNames.Length,
				"environmental storytelling contains seven authored arrangements");

			foreach (string arrangementName in arrangementNames)
			{
				Node3D arrangement = storytelling.GetNode<Node3D>(arrangementName);
				int geometryCount = 0;
				foreach (Node node in arrangement.FindChildren(
					"*", string.Empty, true, false))
				{
					Require(node is not CollisionObject3D &&
						node is not RigidBody3D,
						$"{arrangementName} remains static and navigation-safe");
					Require(node is not Interactable &&
						node is not SearchableContainer,
						$"{arrangementName} does not imply a new interaction");
					Require(node is not Light3D &&
						node is not GpuParticles3D &&
						node is not CpuParticles3D,
						$"{arrangementName} avoids expensive effects");
					if (node is GeometryInstance3D)
					{
						geometryCount++;
					}
				}
				Require(geometryCount >= 4 && geometryCount <= 9,
					$"{arrangementName} stays visually legible and lightweight");
			}

			foreach (Node interactableNode in GetTree().GetNodesInGroup(
				Interactable.GroupName))
			{
				if (interactableNode is not Node3D interactable ||
					!world.IsAncestorOf(interactable))
				{
					continue;
				}
				foreach (Node child in storytelling.GetChildren())
				{
					Node3D arrangement = (Node3D)child;
					Require(arrangement.GlobalPosition.DistanceTo(
							interactable.GlobalPosition) >= 1.5f,
						$"{arrangement.Name} preserves interaction approach space");
				}
			}

			GD.Print("ENVIRONMENTAL_STORYTELLING_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"ENVIRONMENTAL_STORYTELLING_VALIDATION: FAIL - {exception.Message}");
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
