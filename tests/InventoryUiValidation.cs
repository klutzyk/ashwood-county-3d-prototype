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
			Label status = inventoryUi.GetNode<Label>("Panel/Layout/Status");
			ItemList playerItems = inventoryUi.GetNode<ItemList>(
				"Panel/Layout/Columns/PlayerColumn/PlayerItems");
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

			containerItems.EmitSignal(ItemList.SignalName.ItemActivated, 0);
			Require(cabinet.Inventory.StackCount == 0 &&
				playerInventory.GetQuantity("antibiotics") == 1,
				"Enter or double-click Take transfers without duplicating container contents");
			Require(playerItems.HasFocus() && details.Text.Contains("Antibiotics") &&
				details.Text.Contains("Effect: No direct use effect."),
				"selection follows a moved item and clearly shows its effect");
			Require(take.Disabled && !store.Disabled && use.Disabled,
				"moved non-usable item enables Store but not Use");

			inventoryUi.StoreSelected();
			Require(cabinet.Inventory.GetQuantity("antibiotics") == 1 &&
				playerInventory.StackCount == 0, "Store returns the full stack to the container");
			Require(containerItems.HasFocus() && details.Text.Contains("Antibiotics"),
				"keyboard focus and selection follow the stored item");

			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/bandage.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/food.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/water.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/scrap.tres"));
			inventoryUi.SelectContainerItem(0);
			Require(take.Disabled, "Take is disabled when the player has no compatible free slot");
			inventoryUi.TakeSelected();
			Require(status.Text == "Player inventory is full.",
				"full-inventory feedback explains why Take is unavailable");
			Require(cabinet.Inventory.GetQuantity("antibiotics") == 1,
				"invalid Take leaves the item inside the container");

			inventoryUi.SelectPlayerItem(0);
			Require(details.Text.Contains("Effect: Restores 40 health."),
				"selected item details show quantity, description and numeric effect");
			inventoryUi.UseSelected();
			Require(status.Text == "Item cannot be used right now.",
				"invalid Use gives clear feedback without consuming the item");
			player.GetNode<PlayerHealth>("Health").ApplyDamage(20.0f);
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
			if (DisplayServer.GetName() != "headless")
			{
				Require(Input.MouseMode == Input.MouseModeEnum.Captured,
					"closing the inventory restores captured gameplay input");
			}

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
