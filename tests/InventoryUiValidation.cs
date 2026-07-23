#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class InventoryUiValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerInventory playerInventory = player.GetNode<PlayerInventory>("Inventory");
			SearchableContainer cabinet = world.GetNode<SearchableContainer>(
				"Buildings/Pharmacy/Interior/MedicineCabinet/SearchableContainer");
			ContainerInventoryDisplay inventoryUi = world.GetNode<ContainerInventoryDisplay>(
				"PerformanceUI/ContainerInventoryWindow");
			ItemList containerItems = inventoryUi.GetNode<ItemList>(
				"Panel/Layout/Columns/ContainerColumn/ContainerItems");
			Label details = inventoryUi.GetNode<Label>("Panel/Layout/Details");
			Button take = inventoryUi.GetNode<Button>("Panel/Layout/Columns/Actions/Take");
			Button store = inventoryUi.GetNode<Button>("Panel/Layout/Columns/Actions/Store");
			Button use = inventoryUi.GetNode<Button>("Panel/Layout/Columns/Actions/Use");

			cabinet.GetNode<Interactable>("Interactable").Interact(player);
			Require(inventoryUi.IsOpen, "search opens the inventory interface");
			Require(containerItems.HasFocus(), "container items receive initial keyboard focus");
			Require(details.Text.Contains("Antibiotics") &&
				details.Text.Contains("sealed course"), "selected item details include name and description");
			Require(!take.Disabled && store.Disabled && use.Disabled,
				"only Take is valid for selected container antibiotics");

			inventoryUi.TakeSelected();
			Require(cabinet.Inventory.StackCount == 0 &&
				playerInventory.GetQuantity("antibiotics") == 1,
				"Take transfers without duplicating container contents");
			Require(take.Disabled && !store.Disabled && use.Disabled,
				"moved non-usable item enables Store but not Use");

			inventoryUi.StoreSelected();
			Require(cabinet.Inventory.GetQuantity("antibiotics") == 1 &&
				playerInventory.StackCount == 0, "Store returns the full stack to the container");

			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/bandage.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/food.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/water.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/scrap.tres"));
			inventoryUi.SelectContainerItem(0);
			Require(take.Disabled, "Take is disabled when the player has no compatible free slot");
			Require(cabinet.Inventory.GetQuantity("antibiotics") == 1,
				"invalid Take leaves the item inside the container");

			player.GetNode<PlayerHealth>("Health").ApplyDamage(20.0f);
			inventoryUi.SelectPlayerItem(0);
			Require(!use.Disabled, "Use is enabled when the selected bandage can restore health");
			inventoryUi.UseSelected();
			Require(playerInventory.GetQuantity("bandage") == 0,
				"Use consumes one valid selected item");

			inventoryUi._UnhandledInput(new InputEventKey
			{
				Keycode = Key.Escape,
				Pressed = true,
			});
			Require(!inventoryUi.IsOpen && !player.IsInventoryUiOpen,
				"Escape closes cleanly and restores player control");

			GD.Print("INVENTORY_UI_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"INVENTORY_UI_VALIDATION: FAIL - {exception.Message}");
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
