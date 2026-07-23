#nullable enable

using System;
using System.IO;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ServiceStationObjectiveValidation : Node
{
	private const string ValidationSavePath =
		"user://ashwood_county_service_objective_validation.json";

	public override async void _Ready()
	{
		try
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerInventory inventory = player.GetNode<PlayerInventory>("Inventory");
			AntibioticsObjective antibiotics =
				world.GetNode<AntibioticsObjective>("AntibioticsObjective");
			ServiceStationSuppliesObjective supplies =
				world.GetNode<ServiceStationSuppliesObjective>(
					"ServiceStationSuppliesObjective");
			SearchableContainer shelf = world.GetNode<SearchableContainer>(
				"Buildings/ServiceStation/SupplyShelf");
			ContainerInventoryDisplay inventoryUi =
				world.GetNode<ContainerInventoryDisplay>(
					"PerformanceUI/ContainerInventoryWindow");
			Interactable safePoint =
				world.GetNode<Interactable>("PrototypeSafePoint/Interactable");
			SaveGameManager saveManager = world.GetNode<SaveGameManager>("SaveGameManager");
			saveManager.SaveFilePath = ValidationSavePath;

			Require(supplies.State == ServiceStationSuppliesObjectiveState.Locked,
				"second objective starts locked behind antibiotics");
			Require(shelf.Inventory.GetQuantity(
					ServiceStationSuppliesObjective.CannedFoodItemId) >= 1 &&
				shelf.Inventory.GetQuantity(
					ServiceStationSuppliesObjective.WaterItemId) >= 1,
				"service-station shelf guarantees canned food and water");
			Require(inventory.GetQuantity(
					ServiceStationSuppliesObjective.CannedFoodItemId) == 0,
				"guaranteed supplies remain separate from player inventory");

			antibiotics.RestoreState(AntibioticsObjectiveState.Completed);
			Require(supplies.State ==
				ServiceStationSuppliesObjectiveState.SearchServiceStation,
				"second objective activates only after antibiotics completion");

			shelf.GetNode<Interactable>("Interactable").Interact(player);
			Require(shelf.IsSearched && inventoryUi.IsOpen,
				"searching the service-station shelf opens existing container UI");
			Require(inventory.GetQuantity(
					ServiceStationSuppliesObjective.CannedFoodItemId) == 0,
				"searching does not automatically transfer objective supplies");

			TakeItem(inventoryUi, shelf.Inventory, inventory,
				ServiceStationSuppliesObjective.CannedFoodItemId);
			TakeItem(inventoryUi, shelf.Inventory, inventory,
				ServiceStationSuppliesObjective.WaterItemId);
			Require(supplies.State ==
				ServiceStationSuppliesObjectiveState.ReturnToSafePoint,
				"explicit transfers advance the structured objective");

			inventory.AddItem(
				GD.Load<ItemDefinition>("res://assets/items/canned_food.tres"), 2);
			inventory.AddItem(
				GD.Load<ItemDefinition>("res://assets/items/water.tres"), 2);
			inventory.AddItem(
				GD.Load<ItemDefinition>("res://assets/items/scrap.tres"), 2);
			int cannedBefore = inventory.GetQuantity(
				ServiceStationSuppliesObjective.CannedFoodItemId);
			int waterBefore = inventory.GetQuantity(
				ServiceStationSuppliesObjective.WaterItemId);
			int scrapBefore = inventory.GetQuantity("scrap");

			inventoryUi.Close();
			safePoint.Interact(player);
			Require(supplies.State == ServiceStationSuppliesObjectiveState.Completed,
				"safe-point interaction completes the supplies objective");
			Require(inventory.GetQuantity(
					ServiceStationSuppliesObjective.CannedFoodItemId) ==
					cannedBefore - 1 &&
				inventory.GetQuantity(ServiceStationSuppliesObjective.WaterItemId) ==
					waterBefore - 1 &&
				inventory.GetQuantity("scrap") == scrapBefore,
				"submission consumes exact required quantities and preserves other items");

			Require(saveManager.SaveGame(), "completed second objective saves");
			supplies.RestoreState(
				ServiceStationSuppliesObjectiveState.SearchServiceStation);
			Require(saveManager.LoadGame() &&
				supplies.State == ServiceStationSuppliesObjectiveState.Completed,
				"second objective completion persists through save/load");

			string absoluteSavePath = ProjectSettings.GlobalizePath(ValidationSavePath);
			string priorFormat = File.ReadAllText(absoluteSavePath).Replace(
				"  \"ServiceStationObjectiveState\": 3,\r\n",
				string.Empty).Replace(
				"  \"ServiceStationObjectiveState\": 3,\n",
				string.Empty);
			Require(!priorFormat.Contains("ServiceStationObjectiveState"),
				"validation fixture represents a save created before the new field");
			File.WriteAllText(absoluteSavePath, priorFormat);
			Require(saveManager.LoadGame() &&
				supplies.State ==
					ServiceStationSuppliesObjectiveState.SearchServiceStation,
				"older version-1 saves without the added field activate the new objective safely");

			GD.Print("SERVICE_STATION_OBJECTIVE_VALIDATION: PASS");
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GD.PushError(
				$"SERVICE_STATION_OBJECTIVE_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void TakeItem(
		ContainerInventoryDisplay inventoryUi,
		ContainerInventory container,
		PlayerInventory playerInventory,
		StringName itemId)
	{
		int stack = container.FindItemStack(itemId);
		Require(stack >= 0, $"container has required {itemId} stack");
		int containerBefore = container.GetQuantity(itemId);
		int playerBefore = playerInventory.GetQuantity(itemId);
		inventoryUi.SelectContainerItem(stack);
		inventoryUi.TakeSelected();
		Require(container.GetQuantity(itemId) < containerBefore &&
			playerInventory.GetQuantity(itemId) > playerBefore,
			$"{itemId} moves only through the explicit transfer action");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
