#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class GameplayNotificationValidation : Node
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
			GameplayNotificationDisplay notifications = world.GetNode<GameplayNotificationDisplay>(
				"PerformanceUI/GameplayNotifications");

			notifications.QueueNotification("Duplicate check");
			notifications.QueueNotification("Duplicate check");
			Require(notifications.PendingCount == 1, "identical pending notifications are suppressed");
			notifications._Process(notifications.FadeDuration * 0.5f);
			Require(notifications.Modulate.A > 0.0f && notifications.Modulate.A < 1.0f,
				"notifications fade in rather than appearing abruptly");

			cabinet.GetNode<Interactable>("Interactable").Interact(player);
			Require(notifications.ContainsMessage("Searched"),
				"completed searches report their loot result");
			inventoryUi.TakeSelected();
			Require(notifications.ContainsMessage("Item taken"),
				"successful Take queues item feedback");
			Require(notifications.ContainsMessage("Objective updated"),
				"taking antibiotics queues objective-update feedback");

			cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/scrap.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/bandage.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/food.tres"));
			playerInventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/water.tres"));
			inventoryUi.SelectContainerItem(cabinet.Inventory.FindItemStack("scrap"));
			inventoryUi.TakeSelected();
			Require(notifications.ContainsMessage("Inventory full"),
				"failed full-capacity Take queues inventory-full feedback");

			inventoryUi.SelectPlayerItem(3);
			inventoryUi.StoreSelected();
			Require(notifications.ContainsMessage("Item stored"),
				"successful Store queues item feedback");

			player.GetNode<PlayerHealth>("Health").ApplyDamage(20.0f);
			inventoryUi.SelectPlayerItem(1);
			inventoryUi.UseSelected();
			Require(notifications.ContainsMessage("Item used"),
				"successful Use queues item-use feedback");

			inventoryUi.Close();
			world.GetNode<Interactable>("PrototypeSafePoint/Interactable").Interact(player);
			Require(notifications.ContainsMessage("Objective completed"),
				"safe-point delivery queues completion feedback");

			SaveGameManager saveManager = world.GetNode<SaveGameManager>("SaveGameManager");
			saveManager.EmitSignal(SaveGameManager.SignalName.StatusMessageRequested, "Game Saved");
			saveManager.EmitSignal(SaveGameManager.SignalName.StatusMessageRequested, "Game Loaded");
			Require(notifications.ContainsMessage("Game Saved") &&
				notifications.ContainsMessage("Game Loaded"),
				"save and load status signals join the same notification queue");

			Node safePoint = world.GetNode("PrototypeSafePoint");
			Require(safePoint.HasNode("SupplyCrate") && safePoint.HasNode("SignBoard"),
				"safe point uses compact physical dressing");
			MeshInstance3D safePointCrate = safePoint.GetNode<MeshInstance3D>("SupplyCrate");
			StandardMaterial3D safePointMaterial =
				(StandardMaterial3D)safePointCrate.Mesh.SurfaceGetMaterial(0);
			Require(safePointMaterial.EmissionEnabled &&
				safePointMaterial.EmissionEnergyMultiplier >= 0.5f &&
				safePointMaterial.EmissionEnergyMultiplier < 1.0f,
				"safe point uses restrained emission for night readability");
			Require(!safePoint.GetNode<Label3D>("Label").NoDepthTest,
				"safe-point label remains occluded by world geometry");

			GD.Print("GAMEPLAY_NOTIFICATION_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"GAMEPLAY_NOTIFICATION_VALIDATION: FAIL - {exception.Message}");
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
