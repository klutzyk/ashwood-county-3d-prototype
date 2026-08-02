#nullable enable

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.Zombies;
using GodotFileAccess = Godot.FileAccess;

namespace AshwoodCounty3DPrototype.Tests;

public partial class SaveLoadValidation : Node
{
	private const string ValidationSavePath = "user://ashwood_county_save_validation.json";
	private static readonly Vector3 SavedPosition = new(2.5f, 1.2f, -4.5f);
	private static readonly Vector3 SavedRotation = new(0.0f, 1.1f, 0.0f);

	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			bool readAfterRestart = Array.Exists(OS.GetCmdlineUserArgs(), value => value == "read");
			if (readAfterRestart)
			{
				ValidateFreshProcessLoad(world);
			}
			else
			{
				ValidateSaveAndSameSessionLoad(world);
			}

			world.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GC.Collect();
			GC.WaitForPendingFinalizers();

			GD.Print(readAfterRestart
				? "SAVE_LOAD_RESTART_VALIDATION: PASS"
				: "SAVE_LOAD_SESSION_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"SAVE_LOAD_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void ValidateSaveAndSameSessionLoad(Node world)
	{
		SaveGameManager manager = GetManager(world);
		ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
		PlayerHealth health = player.GetNode<PlayerHealth>("Health");
		PlayerStamina stamina = player.GetNode<PlayerStamina>("Stamina");
		PlayerNeeds needs = player.GetNode<PlayerNeeds>("Needs");
		PlayerInventory inventory = player.GetNode<PlayerInventory>("Inventory");
		AntibioticsObjective objective = world.GetNode<AntibioticsObjective>("AntibioticsObjective");
		WorldTime worldTime = world.GetNode<WorldTime>("WorldTime");
		SearchableContainer cabinet = GetCabinet(world);
		SearchableContainer car = world.GetNode<SearchableContainer>("Vehicles/RustedAlfaRomeo/SearchableContainer");
		SearchableContainer crate = world.GetNode<SearchableContainer>("Props/BarrelCrate/SearchableContainer");
		SearchableContainer cupboard = world.GetNode<SearchableContainer>("Props/PrototypeCupboard/SearchableContainer");
		PrototypeZombie zombie = world.GetNode<PrototypeZombie>("Zombies/PrototypeZombie2");

		Require(InputHasKey("save_game", Key.F5), "F5 is mapped to save");
		Require(InputHasKey("load_game", Key.F9), "F9 is mapped to load");

		player.GlobalPosition = SavedPosition;
		player.Rotation = SavedRotation;
		health.RestoreState(73.0f);
		stamina.RestoreState(42.0f, false);
		needs.RestoreState(61.0f, 37.0f);
		inventory.ClearItems();
		inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/antibiotics.tres"), 1);
		inventory.AddSavedStack(GD.Load<ItemDefinition>("res://assets/items/bandage.tres"), 3);
		inventory.AddSavedStack(GD.Load<ItemDefinition>("res://assets/items/bandage.tres"), 2);
		Require(inventory.SwapStacks(2, 7),
			"validation state places a stack in a non-contiguous backpack slot");
		cabinet.Inventory.ClearItems();
		cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/water.tres"), 2);
		cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/medkit.tres"), 1);
		cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/painkillers.tres"), 2);
		cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/soda.tres"), 3);
		cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/canned_food.tres"), 4);
		cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/chocolate.tres"), 5);
		cabinet.RestoreSearchedState(true);
		SetContainerState(car, true, "res://assets/items/food.tres", 3);
		SetContainerState(crate, true, "res://assets/items/water.tres", 1);
		SetContainerState(cupboard, false, "res://assets/items/bandage.tres", 2);
		objective.RestoreState(AntibioticsObjectiveState.ReturnToSafePoint);
		worldTime.SetTimeOfDay(21.25f);
		zombie.SetAlive(false);

		string statusMessage = string.Empty;
		manager.StatusMessageRequested += message => statusMessage = message;
		Require(manager.SaveGame(), "versioned save file is written");
		Require(statusMessage == "Game Saved", "successful save requests brief feedback");
		Require(manager.SaveGame(), "existing save is atomically replaced");
		Require(!GodotFileAccess.FileExists($"{ValidationSavePath}.tmp"),
			"successful atomic save leaves no temporary file");

		player.GlobalPosition = Vector3.Zero;
		player.Rotation = Vector3.Zero;
		health.RestoreState(0.0f);
		stamina.RestoreState(100.0f, true);
		needs.RestoreState(100.0f, 100.0f);
		inventory.ClearItems();
		foreach (SearchableContainer container in new[] { cabinet, car, crate, cupboard })
		{
			container.Inventory.ClearItems();
			container.RestoreSearchedState(false);
		}
		objective.RestoreState(AntibioticsObjectiveState.SearchPharmacy);
		worldTime.SetTimeOfDay(8.0f);
		zombie.SetAlive(true);

		Require(manager.LoadGame(), "same-session save reload succeeds");
		Require(statusMessage == "Game Loaded", "successful load requests brief feedback");
		AssertSavedState(world);
		ValidatePreCapacityVersionOneLoad(manager, inventory, cabinet);
	}

	private static void ValidatePreCapacityVersionOneLoad(
		SaveGameManager manager,
		PlayerInventory inventory,
		SearchableContainer cabinet)
	{
		string originalJson = ReadText(ValidationSavePath);
		try
		{
			JsonObject root = JsonNode.Parse(originalJson)?.AsObject()
				?? throw new InvalidOperationException("saved version-1 JSON could not be parsed");
			root.Remove("WeatherKind");
			root.Remove("WeatherSecondsUntilChange");
			root.Remove("WeatherScheduleRandomState");
			root.Remove("WeatherSecondsUntilLightning");
			root.Remove("WeatherLightningRandomState");
			RemoveSlotIndices(root["PlayerInventory"]?.AsArray());

			JsonArray containers = root["Containers"]?.AsArray()
				?? throw new InvalidOperationException("saved containers were missing");
			JsonObject? cabinetRecord = null;
			foreach (JsonNode? containerNode in containers)
			{
				JsonObject container = containerNode?.AsObject()
					?? throw new InvalidOperationException("saved container record was malformed");
				RemoveSlotIndices(container["Items"]?.AsArray());
				string nodePath = container["NodePath"]?.GetValue<string>() ?? string.Empty;
				if (nodePath.EndsWith(
					"MedicineCabinet/SearchableContainer",
					StringComparison.Ordinal))
				{
					cabinetRecord = container;
				}
			}
			Require(cabinetRecord is not null, "legacy fixture resolves the medicine cabinet record");

			JsonArray overflowItems = new();
			for (int stack = 0; stack < cabinet.Inventory.Capacity + 1; stack++)
			{
				overflowItems.Add(new JsonObject
				{
					["ItemId"] = "medkit",
					["Quantity"] = 1,
				});
			}
			cabinetRecord!["Items"] = overflowItems;
			WriteText(
				ValidationSavePath,
				root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

			Require(SaveGameManager.HasValidSaveFile(ValidationSavePath),
				"slot-less pre-capacity version-1 JSON remains structurally valid");
			inventory.ClearItems();
			cabinet.Inventory.ClearItems();
			Require(manager.LoadGame(),
				"legacy container overflow does not reject the complete save");
			Require(inventory.StackCount == 3 &&
				inventory.GetQuantityAt(0) == 1 &&
				inventory.GetQuantityAt(1) == 3 &&
				inventory.GetQuantityAt(2) == 2 &&
				inventory.GetItemAt(7) is null,
				"missing SlotIndex records migrate deterministically to sequential slots");
			Require(cabinet.Inventory.HasCapacityOverflow &&
				cabinet.Inventory.StackCount == cabinet.Inventory.Capacity + 1 &&
				cabinet.Inventory.GetQuantity("medkit") == cabinet.Inventory.Capacity + 1,
				"all legacy over-capacity stacks restore without silent item loss");
		}
		finally
		{
			WriteText(ValidationSavePath, originalJson);
		}
	}

	private static void RemoveSlotIndices(JsonArray? stacks)
	{
		if (stacks is null)
		{
			throw new InvalidOperationException("saved item stack array was missing");
		}
		foreach (JsonNode? stackNode in stacks)
		{
			stackNode?.AsObject().Remove("SlotIndex");
		}
	}

	private static string ReadText(string path)
	{
		using GodotFileAccess file = GodotFileAccess.Open(
			path,
			GodotFileAccess.ModeFlags.Read)
			?? throw new InvalidOperationException("validation save could not be read");
		return file.GetAsText();
	}

	private static void WriteText(string path, string contents)
	{
		using GodotFileAccess file = GodotFileAccess.Open(
			path,
			GodotFileAccess.ModeFlags.Write)
			?? throw new InvalidOperationException("validation save could not be written");
		file.StoreString(contents);
		file.Flush();
	}

	private static void ValidateFreshProcessLoad(Node world)
	{
		SaveGameManager manager = GetManager(world);
		Require(GodotFileAccess.FileExists(ValidationSavePath), "validation save persists between game processes");
		Require(manager.LoadGame(), "fresh game process loads the local save");
		AssertSavedState(world);
		AntibioticsObjective objective = world.GetNode<AntibioticsObjective>("AntibioticsObjective");
		objective.RestoreState(AntibioticsObjectiveState.Completed);
		Require(manager.SaveGame(), "completed objective state saves");
		objective.RestoreState(AntibioticsObjectiveState.SearchPharmacy);
		Require(manager.LoadGame() &&
			objective.State == AntibioticsObjectiveState.Completed,
			"completed objective state persists through save and load");

		string absolutePath = ProjectSettings.GlobalizePath(ValidationSavePath);
		Require(DirAccess.RemoveAbsolute(absolutePath) == Error.Ok, "validation save cleanup succeeds");
		Require(!manager.LoadGame(), "missing save is handled safely");

		using (GodotFileAccess invalidFile = GodotFileAccess.Open(
			ValidationSavePath, GodotFileAccess.ModeFlags.Write)!)
		{
			invalidFile.StoreString("{\"Version\":1,\"invalid\":true}");
		}
		Vector3 positionBeforeInvalidLoad = world.GetNode<Node3D>("Player").GlobalPosition;
		Require(!manager.LoadGame(), "invalid save data is rejected without crashing");
		Require(world.GetNode<Node3D>("Player").GlobalPosition.IsEqualApprox(positionBeforeInvalidLoad),
			"invalid save does not partially mutate live state");

		using (GodotFileAccess futureFile = GodotFileAccess.Open(
			ValidationSavePath, GodotFileAccess.ModeFlags.Write)!)
		{
			futureFile.StoreString("{\"Version\":2}");
		}
		Require(!manager.LoadGame(), "unsupported future save version is rejected safely");
		Require(world.GetNode<Node3D>("Player").GlobalPosition.IsEqualApprox(positionBeforeInvalidLoad),
			"future save rejection does not mutate live state");
		DirAccess.RemoveAbsolute(absolutePath);
	}

	private static void AssertSavedState(Node world)
	{
		ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
		PlayerInventory inventory = player.GetNode<PlayerInventory>("Inventory");
		SearchableContainer cabinet = GetCabinet(world);
		Require(player.GlobalPosition.IsEqualApprox(SavedPosition), "player position restores");
		Require(player.Rotation.IsEqualApprox(SavedRotation), "player rotation restores");
		Require(Mathf.IsEqualApprox(player.GetNode<PlayerHealth>("Health").CurrentHealth, 73.0f),
			"player health restores");
		Require(Mathf.IsEqualApprox(player.GetNode<PlayerStamina>("Stamina").CurrentStamina, 42.0f) &&
			!player.GetNode<PlayerStamina>("Stamina").CanSprint, "player stamina state restores");
		Require(Mathf.Abs(player.GetNode<PlayerNeeds>("Needs").CurrentHunger - 61.0f) < 0.05f &&
			Mathf.Abs(player.GetNode<PlayerNeeds>("Needs").CurrentThirst - 37.0f) < 0.05f,
			"player hunger and thirst restore");
		Require(inventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 1,
			"player inventory contents restore");
		Require(inventory.StackCount == 3 && inventory.GetQuantity("bandage") == 5 &&
			inventory.GetQuantityAt(1) == 3 && inventory.GetItemAt(2) is null &&
			inventory.GetQuantityAt(7) == 2,
			"split stack boundaries, stable slot indices and quantities restore");
		Require(world.GetNode<AntibioticsObjective>("AntibioticsObjective").State ==
			AntibioticsObjectiveState.ReturnToSafePoint, "structured objective state restores");
		Require(Mathf.Abs(world.GetNode<WorldTime>("WorldTime").CurrentHour - 21.25f) < 0.05f,
			"world time restores");
		Require(cabinet.IsSearched && cabinet.Inventory.GetQuantity("water") == 2,
			"pharmacy container searched state and remaining items restore");
		Require(cabinet.Inventory.GetQuantity("medkit") == 1 &&
			cabinet.Inventory.GetQuantity("painkillers") == 2 &&
			cabinet.Inventory.GetQuantity("soda") == 3 &&
			cabinet.Inventory.GetQuantity("canned_food") == 4 &&
			cabinet.Inventory.GetQuantity("chocolate") == 5,
			"expanded item identifiers restore safely from version 1 saves");
		RequireContainer(world, "Vehicles/RustedAlfaRomeo/SearchableContainer", true, "food", 3);
		RequireContainer(world, "Props/BarrelCrate/SearchableContainer", true, "water", 1);
		RequireContainer(world, "Props/PrototypeCupboard/SearchableContainer", false, "bandage", 2);
		Require(!world.GetNode<Control>("PerformanceUI/DeathOverlay").Visible,
			"loading living health clears the death overlay");
		Require(!world.GetNode<PrototypeZombie>("Zombies/PrototypeZombie2").IsAlive,
			"placed zombie alive/dead state restores");
		Require(world.GetNode<PrototypeZombie>("Zombies/PrototypeZombie1").IsAlive,
			"other placed zombie states remain independent");
	}

	private static SaveGameManager GetManager(Node world)
	{
		SaveGameManager manager = world.GetNode<SaveGameManager>("SaveGameManager");
		manager.SaveFilePath = ValidationSavePath;
		return manager;
	}

	private static SearchableContainer GetCabinet(Node world)
	{
		return world.GetNode<SearchableContainer>(
			"Buildings/Pharmacy/Interior/MedicineCabinet/SearchableContainer");
	}

	private static void SetContainerState(
		SearchableContainer container,
		bool isSearched,
		string itemPath,
		int quantity)
	{
		container.Inventory.ClearItems();
		container.Inventory.AddItem(GD.Load<ItemDefinition>(itemPath), quantity);
		container.RestoreSearchedState(isSearched);
	}

	private static void RequireContainer(
		Node world,
		string path,
		bool isSearched,
		string itemId,
		int quantity)
	{
		SearchableContainer container = world.GetNode<SearchableContainer>(path);
		Require(container.IsSearched == isSearched && container.Inventory.GetQuantity(itemId) == quantity,
			$"container state restores for {path}");
	}

	private static bool InputHasKey(string action, Key key)
	{
		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey keyEvent && keyEvent.PhysicalKeycode == key)
			{
				return true;
			}
		}
		return false;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
