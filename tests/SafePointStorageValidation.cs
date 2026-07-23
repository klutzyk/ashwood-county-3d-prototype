#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class SafePointStorageValidation : Node
{
	private const string ValidationSavePath =
		"user://ashwood_county_safe_point_storage_validation.json";

	public override async void _Ready()
	{
		try
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerInventory playerInventory = player.GetNode<PlayerInventory>("Inventory");
			AntibioticsObjective antibiotics =
				world.GetNode<AntibioticsObjective>("AntibioticsObjective");
			ServiceStationSuppliesObjective supplies =
				world.GetNode<ServiceStationSuppliesObjective>(
					"ServiceStationSuppliesObjective");
			Interactable safePoint =
				world.GetNode<Interactable>("PrototypeSafePoint/Interactable");
			SearchableContainer storage =
				world.GetNode<SearchableContainer>("PrototypeSafePoint/Storage");
			ContainerInventoryDisplay inventoryUi =
				world.GetNode<ContainerInventoryDisplay>(
					"PerformanceUI/ContainerInventoryWindow");
			SaveGameManager saveManager = world.GetNode<SaveGameManager>("SaveGameManager");
			saveManager.SaveFilePath = ValidationSavePath;

			safePoint.Interact(player);
			Require(!inventoryUi.IsOpen,
				"storage remains unavailable before antibiotics completion");

			antibiotics.RestoreState(AntibioticsObjectiveState.Completed);
			safePoint.Interact(player);
			Require(inventoryUi.IsOpen && inventoryUi.CurrentContainer == storage,
				"safe-point interaction opens storage after the first objective");
			Require(storage.DisplayName == "Safe Point Storage" && storage.IsSearched,
				"storage has a clear name and never requires a search delay");
			Require(!storage.GetNode<Interactable>("Interactable").Enabled,
				"internal storage interaction stays disabled to avoid prompt conflicts");

			ItemDefinition scrap = GD.Load<ItemDefinition>("res://assets/items/scrap.tres");
			playerInventory.AddItem(scrap, 3);
			int playerScrapStack = playerInventory.FindItemStack("scrap");
			inventoryUi.SelectPlayerItem(playerScrapStack);
			inventoryUi.StoreSelected();
			Require(playerInventory.GetQuantity("scrap") == 0 &&
				storage.Inventory.GetQuantity("scrap") == 3,
				"Store explicitly moves the selected player stack into safe-point storage");

			int storageScrapStack = storage.Inventory.FindItemStack("scrap");
			inventoryUi.SelectContainerItem(storageScrapStack);
			inventoryUi.TakeSelected();
			Require(playerInventory.GetQuantity("scrap") == 3 &&
				storage.Inventory.GetQuantity("scrap") == 0,
				"Take explicitly returns the selected storage stack to the player");

			inventoryUi.SelectPlayerItem(playerInventory.FindItemStack("scrap"));
			inventoryUi.StoreSelected();
			Require(saveManager.SaveGame(), "safe-point storage saves through container state");
			storage.Inventory.ClearItems();
			Require(saveManager.LoadGame() &&
				storage.Inventory.GetQuantity("scrap") == 3,
				"safe-point storage contents persist through save/load");

			inventoryUi.Close();
			playerInventory.AddItem(
				GD.Load<ItemDefinition>("res://assets/items/canned_food.tres"), 2);
			playerInventory.AddItem(
				GD.Load<ItemDefinition>("res://assets/items/soda.tres"), 2);
			Require(supplies.State ==
				ServiceStationSuppliesObjectiveState.ReturnToSafePoint,
				"objective is ready when required player supplies are held");
			safePoint.Interact(player);
			Require(supplies.State == ServiceStationSuppliesObjectiveState.Completed &&
				playerInventory.GetQuantity("canned_food") == 1 &&
				playerInventory.GetQuantity("soda") == 1 &&
				storage.Inventory.GetQuantity("scrap") == 3 &&
				!inventoryUi.IsOpen,
				"objective delivery stays separate and cannot consume storage or unrelated items");

			safePoint.Interact(player);
			Require(inventoryUi.IsOpen && inventoryUi.CurrentContainer == storage,
				"completed objectives leave the safe point as reusable storage");

			GD.Print("SAFE_POINT_STORAGE_VALIDATION: PASS");
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GD.PushError($"SAFE_POINT_STORAGE_VALIDATION: FAIL - {exception.Message}");
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
