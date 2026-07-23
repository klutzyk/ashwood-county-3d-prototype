#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class TownCompositionValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D composition = world.GetNode<Node3D>("Props/TownCompositionPolish");
			string[] areaNames =
			{
				"PharmacyBackLot",
				"ServiceStationRear",
				"HouseSideYard",
				"RoadsideEdge",
				"SafePointEdge",
			};
			foreach (string areaName in areaNames)
			{
				Require(composition.HasNode(areaName),
					$"composition includes restrained dressing for {areaName}");
			}

			int geometryCount = 0;
			foreach (Node node in composition.FindChildren("*", string.Empty, true, false))
			{
				Require(node is not CollisionObject3D,
					"composition polish adds no collision that can alter navigation");
				Require(node is not Interactable && node is not SearchableContainer,
					"visual dressing does not imply new gameplay interactions");
				if (node is GeometryInstance3D)
				{
					geometryCount++;
				}
			}
			Require(geometryCount >= 18 && geometryCount <= 42,
				$"composition uses a restrained number of detailed visual pieces ({geometryCount})");

			AssertAreaClearance(
				composition.GetNode<Node3D>("PharmacyBackLot"),
				world.GetNode<Node3D>("Buildings/Pharmacy/FrontDoor"),
				4.0f,
				"pharmacy entrance");
			AssertAreaClearance(
				composition.GetNode<Node3D>("ServiceStationRear"),
				world.GetNode<Node3D>("Buildings/ServiceStation/FrontDoor"),
				4.0f,
				"service-station entrance");
			AssertAreaClearance(
				composition.GetNode<Node3D>("HouseSideYard"),
				world.GetNode<Node3D>("Buildings/House/FrontDoor"),
				5.0f,
				"house entrance");
			AssertAreaClearance(
				composition.GetNode<Node3D>("SafePointEdge"),
				world.GetNode<Node3D>("PrototypeSafePoint/Interactable"),
				1.5f,
				"safe-point interaction");

			GD.Print("TOWN_COMPOSITION_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"TOWN_COMPOSITION_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void AssertAreaClearance(
		Node3D area,
		Node3D accessPoint,
		float minimumDistance,
		string label)
	{
		Require(area.GlobalPosition.DistanceTo(accessPoint.GlobalPosition) >= minimumDistance,
			$"dressing preserves clear access to the {label}");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
