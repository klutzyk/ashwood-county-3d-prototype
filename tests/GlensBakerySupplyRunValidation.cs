#nullable enable

using System;
using System.Text.Json;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.UI;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.Zombies;
using GodotFileAccess = Godot.FileAccess;

namespace AshwoodCounty3DPrototype.Tests;

public partial class GlensBakerySupplyRunValidation : Node
{
	private const string MainMenuScenePath = "res://scenes/ui/main_menu.tscn";
	private const string MainStreetScenePath =
		"res://scenes/world/ashwood/main_street.tscn";
	private const string ProductionSavePath =
		"user://ashwood_main_street_vertical_slice_v1.json";
	private const string ValidationSavePath =
		"user://ashwood_glens_bakery_supply_run_validation.json";
	private const string BakeryCachePath =
		"BakeryRoot/ProductionInterior/Storage/BakerySupplyCache";
	private const int ProductionContainerCount = 12;
	private const int ProductionZombieCount = 5;

	public override async void _Ready()
	{
		try
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			ValidateBootConfiguration();

			Node3D world = GD.Load<PackedScene>(MainStreetScenePath)
				.Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			WorldTime worldTime = world.GetNode<WorldTime>("Gameplay/WorldTime");
			worldTime.SetProcess(false);
			ValidateObjectives(world);
			ValidateThreatAndPresentation(world, worldTime);
			ValidateSupplyRunAndPersistence(world, worldTime);

			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GD.Print("GLENS_BAKERY_SUPPLY_RUN_VALIDATION: PASS");
			QuitAfterManagedCleanup(0);
		}
		catch (Exception exception)
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GD.PushError(
				$"GLENS_BAKERY_SUPPLY_RUN_VALIDATION: FAIL - {exception.Message}");
			QuitAfterManagedCleanup(1);
		}
	}

	private void QuitAfterManagedCleanup(int exitCode)
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GetTree().Quit(exitCode);
	}

	private static void ValidateBootConfiguration()
	{
		Require(
			ProjectSettings.GetSetting("application/run/main_scene").AsString() ==
			MainMenuScenePath,
			"the project boots through the production main menu");

		MainMenuController menu = (MainMenuController)GD.Load<PackedScene>(
			MainMenuScenePath).Instantiate();
		Require(menu.GameplayScenePath == MainStreetScenePath,
			"New Game and Continue target production Main Street");
		Require(menu.SaveFilePath == ProductionSavePath,
			"the menu and production slice share one save slot");
		Require(menu.ExpectedContainerCount == ProductionContainerCount &&
			menu.ExpectedZombieCount == ProductionZombieCount,
			"Continue validates the exact production persistence set");
		menu.Free();
	}

	private static void ValidateObjectives(Node3D world)
	{
		AntibioticsObjective antibiotics = world.GetNode<AntibioticsObjective>(
			"Gameplay/AntibioticsObjective");
		ServiceStationSuppliesObjective supplies =
			world.GetNode<ServiceStationSuppliesObjective>(
				"Gameplay/ServiceStationSuppliesObjective");
		Label objectiveText = world.GetNode<Label>(
			"Gameplay/GameplayHUD/ObjectiveDisplay/ObjectiveText");

		Require(antibiotics.InitialState == AntibioticsObjectiveState.Completed &&
			antibiotics.State == AntibioticsObjectiveState.Completed,
			"the retired antibiotics step starts completed in this focused slice");
		Require(supplies.State ==
			ServiceStationSuppliesObjectiveState.SearchServiceStation,
			"the bakery supply objective activates on production scene startup");
		Require(supplies.RequiredSearchContainerPath.ToString() ==
			"../../BakeryRoot/ProductionInterior/Storage/BakerySupplyCache",
			"the focused objective requires Glen's Bakery cache to be searched");
		Require(supplies.DisplayText.Contains("Glen's Bakery", StringComparison.Ordinal) &&
			objectiveText.Text.Contains("Glen's Bakery", StringComparison.Ordinal),
			"the objective model and HUD identify Glen's Bakery");
	}

	private static void ValidateThreatAndPresentation(
		Node3D world,
		WorldTime worldTime)
	{
		Node zombies = world.GetNode("Gameplay/Zombies");
		int zombieCount = 0;
		for (int childIndex = 0; childIndex < zombies.GetChildCount(); childIndex++)
		{
			if (zombies.GetChild(childIndex) is PrototypeZombie)
			{
				zombieCount++;
			}
		}
		Require(zombieCount == 5,
			"the focused Main Street slice contains five placed zombies");

		Require(IsNear(worldTime.StartingHour, 16.75f) &&
			worldTime.FullDayDurationSeconds >= 14400.0f &&
			IsNear(worldTime.GoldenHourColorStrength, 1.0f),
			"Main Street starts in a deliberately sustained golden hour");
		DirectionalLight3D sunlight = world.GetNode<DirectionalLight3D>(
			"DirectionalLight3D");
		Require(sunlight.LightColor.R > sunlight.LightColor.G &&
			sunlight.LightColor.G > sunlight.LightColor.B,
			"runtime world-time lighting retains a warm golden-hour hierarchy");

		Godot.Environment environment = world.GetNode<WorldEnvironment>(
			"WorldEnvironment").Environment
			?? throw new InvalidOperationException("Main Street environment is missing");
		Require(environment.Get("fog_enabled").AsBool(),
			"the golden-hour vista uses bounded atmospheric fog");
		Sky sky = environment.Sky
			?? throw new InvalidOperationException("Main Street sky is missing");
		ProceduralSkyMaterial skyMaterial = sky.SkyMaterial as ProceduralSkyMaterial
			?? throw new InvalidOperationException(
				"Main Street must use its configured procedural sky");
		Texture2D? cloudCover =
			skyMaterial.Get("sky_cover").AsGodotObject() as Texture2D;
		Require(cloudCover is not null &&
			cloudCover.ResourcePath ==
				"res://assets/environment/sky/ashwood_golden_hour_cloud_cover.png",
			"the production sky uses the authored golden-hour cloud cover");
		Color cloudModulate = skyMaterial.Get("sky_cover_modulate").AsColor();
		Require(cloudModulate.A > 0.3f && cloudModulate.A < 0.5f,
			"cloud cover remains restrained enough to preserve the sunset gradient");

		Node vista = world.GetNode("Environment/Vista");
		Require(vista.HasNode("Ridges") &&
			vista.GetNode("Ridges").GetChildCount() >= 6 &&
			vista.HasNode("WaterTower/Tank"),
			"the production street has ridge silhouettes and its water-tower landmark");
	}

	private static void ValidateSupplyRunAndPersistence(
		Node3D world,
		WorldTime worldTime)
	{
		ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Gameplay/Player");
		PlayerInventory playerInventory = player.GetNode<PlayerInventory>("Inventory");
		ServiceStationSuppliesObjective supplies =
			world.GetNode<ServiceStationSuppliesObjective>(
				"Gameplay/ServiceStationSuppliesObjective");
		SearchableContainer cache = world.GetNode<SearchableContainer>(BakeryCachePath);
		ContainerInventoryDisplay inventoryUi =
			world.GetNode<ContainerInventoryDisplay>(
				"Gameplay/GameplayHUD/ContainerInventoryWindow");
		Interactable reliefSafePoint = world.GetNode<Interactable>(
			"Environment/ReliefSafePoint/Interactable");
		SaveGameManager saveManager = world.GetNode<SaveGameManager>(
			"Gameplay/SaveGameManager");

		Require(saveManager.SaveFilePath == ProductionSavePath,
			"the runtime save manager uses the menu's production save slot");
		Require(saveManager.PersistenceRootPath.ToString() == "../.." &&
			saveManager.GetNode(saveManager.PersistenceRootPath) == world,
			"production persistence is rooted above Gameplay at Main Street");
		saveManager.SaveFilePath = ValidationSavePath;

		Require(!cache.IsSearched &&
			cache.Inventory.GetQuantity(
				ServiceStationSuppliesObjective.CannedFoodItemId) >= 1 &&
			cache.Inventory.GetQuantity(
				ServiceStationSuppliesObjective.WaterItemId) >= 1,
			"the bakery cache owns guaranteed canned food and water before search");
		Require(playerInventory.GetQuantity(
				ServiceStationSuppliesObjective.CannedFoodItemId) == 0 &&
			playerInventory.GetQuantity(
				ServiceStationSuppliesObjective.WaterItemId) == 0,
			"bakery supplies begin separate from the player inventory");

		ItemDefinition cannedFood = GD.Load<ItemDefinition>(
			"res://assets/items/canned_food.tres");
		ItemDefinition water = GD.Load<ItemDefinition>(
			"res://assets/items/water.tres");
		Require(playerInventory.AddItem(cannedFood) && playerInventory.AddItem(water),
			"validation can simulate supplies scavenged outside the bakery");
		Require(supplies.State ==
			ServiceStationSuppliesObjectiveState.SearchServiceStation,
			"alternate food and drink cannot bypass the required bakery search");
		Require(playerInventory.RemoveItem(cannedFood.ItemId) &&
			playerInventory.RemoveItem(water.ItemId),
			"alternate-source validation items are removed before explicit transfer");

		cache.GetNode<Interactable>("Interactable").Interact(player);
		Require(cache.IsSearched && inventoryUi.IsOpen &&
			inventoryUi.CurrentContainer == cache,
			"searching the bakery cache opens the existing explicit-transfer UI");
		Require(playerInventory.GetQuantity(
				ServiceStationSuppliesObjective.CannedFoodItemId) == 0 &&
			playerInventory.GetQuantity(
				ServiceStationSuppliesObjective.WaterItemId) == 0,
			"search reveals supplies without automatically transferring them");

		TakeItem(
			inventoryUi,
			cache.Inventory,
			playerInventory,
			ServiceStationSuppliesObjective.CannedFoodItemId);
		Require(supplies.State ==
			ServiceStationSuppliesObjectiveState.SearchServiceStation,
			"one transferred requirement cannot advance the delivery objective");
		TakeItem(
			inventoryUi,
			cache.Inventory,
			playerInventory,
			ServiceStationSuppliesObjective.WaterItemId);
		Require(supplies.State ==
			ServiceStationSuppliesObjectiveState.ReturnToSafePoint,
			"explicitly taking both supplies advances the return objective");

		int cannedBeforeDelivery = playerInventory.GetQuantity(
			ServiceStationSuppliesObjective.CannedFoodItemId);
		int waterBeforeDelivery = playerInventory.GetQuantity(
			ServiceStationSuppliesObjective.WaterItemId);
		inventoryUi.Close();
		reliefSafePoint.Interact(player);
		Require(supplies.State == ServiceStationSuppliesObjectiveState.Completed,
			"the relief safe point explicitly completes the bakery delivery");
		Require(playerInventory.GetQuantity(
				ServiceStationSuppliesObjective.CannedFoodItemId) ==
				cannedBeforeDelivery - 1 &&
			playerInventory.GetQuantity(
				ServiceStationSuppliesObjective.WaterItemId) ==
				waterBeforeDelivery - 1,
			"delivery consumes exactly one food and one drink from the player");
		Require(world.GetNode<Label>(
				"Gameplay/GameplayHUD/ObjectiveDisplay/ObjectiveText")
			.Text.Contains("OBJECTIVE COMPLETE", StringComparison.Ordinal),
			"the focused HUD presents the completed delivery state");

		ItemDefinition scrap = GD.Load<ItemDefinition>("res://assets/items/scrap.tres");
		Require(cache.Inventory.AddItem(scrap, 2),
			"validation state can add player-authored contents to the bakery cache");
		float savedHour = worldTime.CurrentHour;
		Require(saveManager.SaveGame(),
			"the focused production slice saves through the isolated slot");

		string savedJson;
		using (GodotFileAccess file = GodotFileAccess.Open(
			ValidationSavePath,
			GodotFileAccess.ModeFlags.Read)!)
		{
			savedJson = file.GetAsText();
			Require(savedJson.Contains(BakeryCachePath, StringComparison.Ordinal),
				"saved container paths include the bakery hierarchy via PersistenceRootPath");
		}

		SaveGameDataV1 partialSave = JsonSerializer.Deserialize<SaveGameDataV1>(
			savedJson) ?? throw new InvalidOperationException(
				"validation could not deserialize the production save");
		Require(SaveGameManager.HasValidSaveFile(
			ValidationSavePath,
			ProductionContainerCount,
			ProductionZombieCount),
			$"the production save with {partialSave.Containers.Count} containers " +
			"passes the menu's exact persistence-set validation");
		partialSave.Containers.RemoveAt(partialSave.Containers.Count - 1);
		System.IO.File.WriteAllText(
			ProjectSettings.GlobalizePath(ValidationSavePath),
			JsonSerializer.Serialize(partialSave));
		Require(!SaveGameManager.HasValidSaveFile(
			ValidationSavePath,
			ProductionContainerCount,
			ProductionZombieCount),
			"the production menu rejects an incomplete persistence set");
		Require(!saveManager.LoadGame() && cache.IsSearched &&
			cache.Inventory.GetQuantity("scrap") == 2,
			"an incomplete container save is rejected without mutating live state");
		System.IO.File.WriteAllText(
			ProjectSettings.GlobalizePath(ValidationSavePath),
			savedJson);

		cache.Inventory.ClearItems();
		cache.RestoreSearchedState(false);
		supplies.RestoreState(
			ServiceStationSuppliesObjectiveState.SearchServiceStation);
		worldTime.SetTimeOfDay(8.0f);
		Require(saveManager.LoadGame(),
			"the focused production slice reloads its isolated save");
		Require(cache.IsSearched && cache.Inventory.GetQuantity("scrap") == 2,
			"save/load restores searched state and player-added bakery-cache contents");
		Require(supplies.State == ServiceStationSuppliesObjectiveState.Completed,
			"save/load restores the completed bakery delivery objective");
		Require(IsNear(worldTime.CurrentHour, savedHour, 0.02f),
			"save/load restores the golden-hour world time");
	}

	private static void TakeItem(
		ContainerInventoryDisplay inventoryUi,
		ContainerInventory container,
		PlayerInventory playerInventory,
		StringName itemId)
	{
		int stackIndex = container.FindItemStack(itemId);
		Require(stackIndex >= 0, $"bakery cache contains required {itemId}");
		int containerBefore = container.GetQuantity(itemId);
		int playerBefore = playerInventory.GetQuantity(itemId);
		inventoryUi.SelectContainerItem(stackIndex);
		inventoryUi.TakeSelected();
		Require(container.GetQuantity(itemId) < containerBefore &&
			playerInventory.GetQuantity(itemId) > playerBefore,
			$"{itemId} moves only through the explicit Take action");
	}

	private static bool IsNear(
		float value,
		float expected,
		float tolerance = 0.01f)
	{
		return Mathf.Abs(value - expected) <= tolerance;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
