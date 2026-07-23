#nullable enable

using System;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.Tests;

public partial class TownLandmarkValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D diner = world.GetNode<Node3D>("Buildings/Diner");
			Require(diner.HasNode("Exterior"), "landmark has a separate modular exterior");
			Require(diner.HasNode("Interior"), "landmark has a separate modular interior");
			Require(diner.GetNode("Exterior") is StaticBody3D, "landmark exterior has static collision");
			Require(diner.GetNode("Interior") is StaticBody3D, "landmark interior has static collision");
			Require(diner.GetNode("FrontDoor") is DoorController,
				"landmark uses the existing hinged-door interaction");

			int lightCount = diner.GetNode("Interior").FindChildren("*", "OmniLight3D", true, false).Count;
			Require(lightCount == 2, "landmark uses two lightweight existing-style interior lights");
			foreach (OmniLight3D light in diner.GetNode("Interior")
				.FindChildren("*", "OmniLight3D", true, false).Cast<OmniLight3D>())
			{
				Require(!light.ShadowEnabled, "landmark interior lights remain lightweight");
			}

			SearchableContainer pantry = diner.GetNode<SearchableContainer>("Pantry");
			SearchableContainer fridge = diner.GetNode<SearchableContainer>("Fridge");
			Require(pantry.LootTable is not null && fridge.LootTable is not null,
				"both diner containers use existing loot tables");
			Require(pantry.GetNode<ContainerInventory>("Inventory") !=
				fridge.GetNode<ContainerInventory>("Inventory"),
				"landmark containers retain separate inventory state");
			Require(pantry.DisplayName == "Diner Pantry" && fridge.DisplayName == "Diner Fridge",
				"landmark containers have logical interaction names");

			GD.Print("TOWN_LANDMARK_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"TOWN_LANDMARK_VALIDATION: FAIL - {exception.Message}");
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
