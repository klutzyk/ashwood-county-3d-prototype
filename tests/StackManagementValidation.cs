#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class StackManagementValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			ValidateStorageRules();

			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerInventory playerInventory = player.GetNode<PlayerInventory>("Inventory");
			SearchableContainer cupboard = world.GetNode<SearchableContainer>(
				"Props/PrototypeCupboard/SearchableContainer");
			ContainerInventoryDisplay inventoryUi = world.GetNode<ContainerInventoryDisplay>(
				"PerformanceUI/ContainerInventoryWindow");
			ItemList playerItems = inventoryUi.GetNode<ItemList>(
				"Panel/Layout/Columns/PlayerColumn/PlayerItems");
			ItemList containerItems = inventoryUi.GetNode<ItemList>(
				"Panel/Layout/Columns/ContainerColumn/ContainerItems");

			ItemDefinition cannedFood = GD.Load<ItemDefinition>("res://assets/items/canned_food.tres");
			playerInventory.ClearItems();
			playerInventory.AddItem(cannedFood, 3);
			cupboard.Inventory.ClearItems();
			cupboard.Inventory.AddItem(cannedFood, 4);
			cupboard.RestoreSearchedState(true);
			cupboard.GetNode<Interactable>("Interactable").Interact(player);

			inventoryUi.SelectContainerItem(0);
			Require(inventoryUi.TakeSelectedQuantity(2),
				"a chosen quantity transfers from container to player");
			Require(cupboard.Inventory.GetQuantity("canned_food") == 2 &&
				playerInventory.StackCount == 2 &&
				playerInventory.GetQuantityAt(0) == 4 &&
				playerInventory.GetQuantityAt(1) == 1,
				"partial transfer fills a compatible stack then creates bounded overflow");
			Require(playerItems.GetSelectedItems().Length == 1 &&
				playerItems.GetSelectedItems()[0] == 1,
				"selection follows the destination stack after a partial transfer");

			inventoryUi.SelectPlayerItem(0);
			Require(inventoryUi.SplitSelectedStack(2),
				"selected player stack splits by a chosen quantity");
			Require(playerInventory.StackCount == 3 &&
				playerInventory.GetQuantityAt(0) == 2 &&
				playerInventory.GetQuantityAt(1) == 2 &&
				playerInventory.GetQuantityAt(2) == 1,
				"splitting preserves total quantity and stack limits");
			Require(playerItems.GetSelectedItems()[0] == 1,
				"selection follows the new split stack");

			inventoryUi.SelectPlayerItem(1);
			Require(inventoryUi.StoreSelectedQuantity(1),
				"a chosen quantity transfers from player to container");
			Require(playerInventory.GetQuantity("canned_food") == 4 &&
				cupboard.Inventory.GetQuantity("canned_food") == 3,
				"chosen-quantity storage preserves totals on both sides");
			Require(containerItems.GetSelectedItems().Length == 1,
				"selection follows the stored destination");
			Require(playerItems.GetItemText(0).Contains("x2") &&
				containerItems.GetItemText(0).Contains("x3"),
				"stack quantities remain clearly displayed");

			GD.Print("STACK_MANAGEMENT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"STACK_MANAGEMENT_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private void ValidateStorageRules()
	{
		PlayerInventory player = new();
		ContainerInventory container = new();
		AddChild(player);
		AddChild(container);
		ItemDefinition bandage = GD.Load<ItemDefinition>("res://assets/items/bandage.tres");
		ItemDefinition food = GD.Load<ItemDefinition>("res://assets/items/food.tres");
		ItemDefinition water = GD.Load<ItemDefinition>("res://assets/items/water.tres");
		ItemDefinition medkit = GD.Load<ItemDefinition>("res://assets/items/medkit.tres");

		Require(player.AddItem(bandage, 4) && player.AddItem(bandage, 3),
			"compatible additions merge automatically");
		Require(player.StackCount == 2 && player.GetQuantityAt(0) == 5 &&
			player.GetQuantityAt(1) == 2 && player.GetQuantity("bandage") == 7,
			"automatic merging never exceeds the bandage stack limit");
		Require(player.AddItem(food) && player.AddItem(water) && player.IsFull,
			"player capacity counts bounded stacks");
		Require(!player.AddItem(medkit, 3) && player.GetQuantity("medkit") == 0,
			"an invalid addition is atomic and never exceeds capacity");

		Require(player.TransferQuantityTo(1, 2, container, out int destination) &&
			destination == 0 && player.GetQuantity("bandage") == 5 &&
			container.GetQuantity("bandage") == 2,
			"quantity transfer removes only the chosen amount");
		Require(container.AddItem(bandage, 4) &&
			container.GetQuantityAt(0) == 5 && container.GetQuantityAt(1) == 1,
			"container additions merge before creating overflow stacks");
		Require(container.SplitStack(0, 2) == 1 &&
			container.GetQuantityAt(0) == 3 && container.GetQuantityAt(1) == 2,
			"storage split inserts a predictable selected stack");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
