#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class WeightedLootValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			ValidateLootData();

			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			ContainerInventoryDisplay inventoryUi = world.GetNode<ContainerInventoryDisplay>(
				"PerformanceUI/ContainerInventoryWindow");
			SearchableContainer cabinet = world.GetNode<SearchableContainer>(
				"Buildings/Pharmacy/Interior/MedicineCabinet/SearchableContainer");
			Require(cabinet.Inventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 1,
				"objective antibiotics are authored before random loot generation");
			cabinet.SetLootSeed(17);
			cabinet.GetNode<Interactable>("Interactable").Interact(player);
			Require(cabinet.Inventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 1,
				"medicine-cabinet generation preserves deterministic objective antibiotics");
			inventoryUi.Close();

			SearchableContainer car = world.GetNode<SearchableContainer>(
				"Vehicles/RustedAlfaRomeo/SearchableContainer");
			ItemDefinition chocolate = GD.Load<ItemDefinition>("res://assets/items/chocolate.tres");
			car.Inventory.AddItem(chocolate, 2);
			car.SetLootSeed(31);
			car.GetNode<Interactable>("Interactable").Interact(player);
			int generatedStackCount = car.Inventory.StackCount;
			int generatedItemCount = CountItems(car.Inventory);
			car.GetNode<Interactable>("Interactable").Interact(player);
			Require(car.IsSearched && car.Inventory.StackCount == generatedStackCount &&
				CountItems(car.Inventory) == generatedItemCount,
				"searching a container repeatedly never generates loot twice");
			Require(car.Inventory.GetQuantity("chocolate") == 2,
				"items added before generation remain in the container");

			GD.Print("WEIGHTED_LOOT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"WEIGHTED_LOOT_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateLootData()
	{
		foreach (string profile in new[]
		{
			"pharmacy_shelf", "medicine_cabinet", "service_station_shelf", "fridge",
			"cupboard", "toolbox", "car_trunk", "zombie_corpse",
		})
		{
			LootTable table = GD.Load<LootTable>($"res://assets/items/loot_tables/{profile}.tres");
			bool hasEmptyResult = false;
			foreach (LootEntry entry in table.Entries)
			{
				hasEmptyResult |= entry.Item is null && entry.Weight > 0.0f;
				Require(entry.MaximumQuantity >= entry.MinimumQuantity,
					$"{profile} quantities are ordered");
			}
			Require(table.MaximumRolls >= table.MinimumRolls &&
				table.MaximumRolls > 0 && hasEmptyResult,
				$"{profile} has bounded rolls and an empty outcome");
		}

		ItemDefinition scrap = GD.Load<ItemDefinition>("res://assets/items/scrap.tres");
		LootTable fixedTable = new()
		{
			MinimumRolls = 3,
			MaximumRolls = 3,
			Entries = new Godot.Collections.Array<LootEntry>
			{
				new()
				{
					Item = scrap,
					Weight = 1.0f,
					MinimumQuantity = 2,
					MaximumQuantity = 2,
				},
			},
		};
		ContainerInventory inventory = new();
		fixedTable.GenerateInto(inventory, new RandomNumberGenerator { Seed = 5 });
		Require(inventory.GetQuantity("scrap") == 6,
			"maximum rolls and entry quantities are applied through reusable data");

		LootTable emptyTable = new()
		{
			MinimumRolls = 2,
			MaximumRolls = 2,
			Entries = new Godot.Collections.Array<LootEntry>
			{
				new() { Weight = 1.0f },
			},
		};
		emptyTable.GenerateInto(inventory, new RandomNumberGenerator { Seed = 9 });
		Require(inventory.GetQuantity("scrap") == 6,
			"empty weighted outcomes add no item");
		inventory.Free();
	}

	private static int CountItems(ItemStorage storage)
	{
		int count = 0;
		for (int index = 0; index < storage.StackCount; index++)
		{
			count += storage.GetQuantityAt(index);
		}
		return count;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
