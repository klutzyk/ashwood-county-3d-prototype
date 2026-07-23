#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class LootPlacementValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			SearchableContainer pharmacy = world.GetNode<SearchableContainer>(
				"Buildings/Pharmacy/Interior/MedicineCabinet/SearchableContainer");
			RequireTable(pharmacy, "medicine_cabinet.tres", "pharmacy medicine");

			SearchableContainer bathroom = world.GetNode<SearchableContainer>(
				"Buildings/House/BathroomMedicineCabinet/SearchableContainer");
			RequireTable(bathroom, "medicine_cabinet.tres", "bathroom medicine");

			SearchableContainer cupboard = world.GetNode<SearchableContainer>(
				"Props/PrototypeCupboard/SearchableContainer");
			RequireTable(cupboard, "cupboard.tres", "house kitchen cupboard");
			SearchableContainer houseFridge = world.GetNode<SearchableContainer>(
				"Buildings/House/KitchenFridge/SearchableContainer");
			RequireTable(houseFridge, "fridge.tres", "house kitchen fridge");

			SearchableContainer dinerPantry = world.GetNode<SearchableContainer>(
				"Buildings/Diner/Pantry");
			RequireTable(dinerPantry, "cupboard.tres", "diner pantry");
			SearchableContainer dinerFridge = world.GetNode<SearchableContainer>(
				"Buildings/Diner/Fridge");
			RequireTable(dinerFridge, "fridge.tres", "diner fridge");

			SearchableContainer serviceShelf = world.GetNode<SearchableContainer>(
				"Buildings/ServiceStation/SupplyShelf");
			RequireTable(serviceShelf, "service_station_shelf.tres", "service-station food");
			SearchableContainer toolbox = world.GetNode<SearchableContainer>(
				"Buildings/ServiceStation/Toolbox/SearchableContainer");
			RequireTable(toolbox, "toolbox.tres", "service-station tools");

			Require(pharmacy.GetNode("Inventory") != bathroom.GetNode("Inventory") &&
				cupboard.GetNode("Inventory") != houseFridge.GetNode("Inventory") &&
				serviceShelf.GetNode("Inventory") != toolbox.GetNode("Inventory"),
				"logical containers retain independent inventory state");

			GD.Print("LOOT_PLACEMENT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"LOOT_PLACEMENT_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void RequireTable(
		SearchableContainer container,
		string expectedFileName,
		string label)
	{
		Require(container.LootTable is not null &&
			container.LootTable.ResourcePath.EndsWith(expectedFileName, StringComparison.Ordinal),
			$"{label} uses the logical existing loot table");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
