#nullable enable

using System;
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
		inventory.ClearItems();
		inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/antibiotics.tres"), 1);
		cabinet.Inventory.ClearItems();
		cabinet.Inventory.AddItem(GD.Load<ItemDefinition>("res://assets/items/water.tres"), 2);
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

		player.GlobalPosition = Vector3.Zero;
		player.Rotation = Vector3.Zero;
		health.RestoreState(0.0f);
		stamina.RestoreState(100.0f, true);
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
	}

	private static void ValidateFreshProcessLoad(Node world)
	{
		SaveGameManager manager = GetManager(world);
		Require(GodotFileAccess.FileExists(ValidationSavePath), "validation save persists between game processes");
		Require(manager.LoadGame(), "fresh game process loads the local save");
		AssertSavedState(world);

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
		Require(inventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 1,
			"player inventory contents restore");
		Require(world.GetNode<AntibioticsObjective>("AntibioticsObjective").State ==
			AntibioticsObjectiveState.ReturnToSafePoint, "structured objective state restores");
		Require(Mathf.Abs(world.GetNode<WorldTime>("WorldTime").CurrentHour - 21.25f) < 0.05f,
			"world time restores");
		Require(cabinet.IsSearched && cabinet.Inventory.GetQuantity("water") == 2,
			"pharmacy container searched state and remaining items restore");
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
