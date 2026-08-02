#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using AshwoodCounty3DPrototype.Game;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.Zombies;
using GodotFileAccess = Godot.FileAccess;

namespace AshwoodCounty3DPrototype.Save;

public partial class SaveGameManager : Node
{
	public const string DefaultSaveFilePath = "user://ashwood_county_save_v1.json";
	private const int DefaultVersionOneMinimumContainerCount = 4;
	private const int DefaultVersionOneMinimumZombieCount = 5;

	[Signal]
	public delegate void StatusMessageRequestedEventHandler(string message);

	[Export] public string SaveFilePath { get; set; } = DefaultSaveFilePath;
	[Export(PropertyHint.Range, "0,100,1")]
	public int MinimumContainerCount { get; set; } =
		DefaultVersionOneMinimumContainerCount;
	[Export(PropertyHint.Range, "0,100,1")]
	public int MinimumZombieCount { get; set; } =
		DefaultVersionOneMinimumZombieCount;
	[Export] public NodePath PlayerPath { get; set; } = new("../Player");
	[Export] public NodePath ObjectivePath { get; set; } = new("../AntibioticsObjective");
	[Export] public NodePath SuppliesObjectivePath { get; set; } =
		new("../ServiceStationSuppliesObjective");
	[Export] public NodePath WorldTimePath { get; set; } = new("../WorldTime");
	[Export] public NodePath WeatherDirectorPath { get; set; } = new("../DynamicWeather");
	[Export] public NodePath PersistenceRootPath { get; set; } = new("..");

	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
	private static readonly IReadOnlyDictionary<string, string> ItemResourcePaths =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["antibiotics"] = "res://assets/items/antibiotics.tres",
			["bandage"] = "res://assets/items/bandage.tres",
			["canned_food"] = "res://assets/items/canned_food.tres",
			["chocolate"] = "res://assets/items/chocolate.tres",
			["food"] = "res://assets/items/food.tres",
			["medkit"] = "res://assets/items/medkit.tres",
			["painkillers"] = "res://assets/items/painkillers.tres",
			["scrap"] = "res://assets/items/scrap.tres",
			["soda"] = "res://assets/items/soda.tres",
			["water"] = "res://assets/items/water.tres",
		};

	private ThirdPersonPlayer _player = null!;
	private PlayerHealth _health = null!;
	private PlayerStamina _stamina = null!;
	private PlayerNeeds _needs = null!;
	private PlayerInventory _playerInventory = null!;
	private AntibioticsObjective _objective = null!;
	private ServiceStationSuppliesObjective _suppliesObjective = null!;
	private WorldTime _worldTime = null!;
	private WeatherDirector? _weatherDirector;

	public override void _Ready()
	{
		_player = GetNode<ThirdPersonPlayer>(PlayerPath);
		_health = _player.GetNode<PlayerHealth>("Health");
		_stamina = _player.GetNode<PlayerStamina>("Stamina");
		_needs = _player.GetNode<PlayerNeeds>("Needs");
		_playerInventory = _player.GetNode<PlayerInventory>("Inventory");
		_objective = GetNode<AntibioticsObjective>(ObjectivePath);
		_suppliesObjective = GetNode<ServiceStationSuppliesObjective>(SuppliesObjectivePath);
		_worldTime = GetNode<WorldTime>(WorldTimePath);
		_weatherDirector = GetNodeOrNull<WeatherDirector>(WeatherDirectorPath);
		if (GameLaunchContext.ConsumeContinueRequest())
		{
			CallDeferred(MethodName.LoadRequestedGame);
		}
	}

	private void LoadRequestedGame()
	{
		LoadGame();
	}

	public static bool HasValidSaveFile(
		string saveFilePath = DefaultSaveFilePath,
		int expectedContainerCount = -1,
		int expectedZombieCount = -1)
	{
		if (!GodotFileAccess.FileExists(saveFilePath))
		{
			return false;
		}

		try
		{
			using GodotFileAccess? file =
				GodotFileAccess.Open(saveFilePath, GodotFileAccess.ModeFlags.Read);
			SaveGameDataV1? data = file is null
				? null
				: JsonSerializer.Deserialize<SaveGameDataV1>(file.GetAsText(), JsonOptions);
			return IsStructurallyValid(
				data,
				expectedContainerCount,
				expectedZombieCount);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool DeleteSaveFile(string saveFilePath = DefaultSaveFilePath)
	{
		bool success = true;
		foreach (string path in new[] { saveFilePath, $"{saveFilePath}.tmp", $"{saveFilePath}.bak" })
		{
			string absolutePath = ProjectSettings.GlobalizePath(path);
			try
			{
				if (File.Exists(absolutePath))
				{
					File.Delete(absolutePath);
				}
			}
			catch (Exception)
			{
				success = false;
			}
		}
		return success;
	}

	private static bool IsStructurallyValid(
		SaveGameDataV1? data,
		int expectedContainerCount = -1,
		int expectedZombieCount = -1)
	{
		if (data is null || data.Version != SaveGameDataV1.CurrentVersion ||
			data.PlayerTransform?.Position is null || data.PlayerTransform.Rotation is null ||
			data.PlayerInventory is null || data.Containers is null || data.Zombies is null ||
			!Enum.IsDefined(typeof(AntibioticsObjectiveState), data.ObjectiveState) ||
			!Enum.IsDefined(
				typeof(ServiceStationSuppliesObjectiveState),
				data.ServiceStationObjectiveState) ||
			!IsFinite(data.PlayerHealth) || data.PlayerHealth < 0.0f || data.PlayerHealth > 100.0f ||
			!IsFinite(data.PlayerStamina) || data.PlayerStamina < 0.0f || data.PlayerStamina > 100.0f ||
			!IsFinite(data.PlayerHunger) || data.PlayerHunger < 0.0f || data.PlayerHunger > 100.0f ||
			!IsFinite(data.PlayerThirst) || data.PlayerThirst < 0.0f || data.PlayerThirst > 100.0f ||
			!IsFinite(data.WorldTimeHours) || data.WorldTimeHours < 0.0f ||
			data.WorldTimeHours >= 24.0f ||
			!HasValidOptionalWeatherState(data) ||
			!IsFinite(data.PlayerTransform.Position) ||
			!IsFinite(data.PlayerTransform.Rotation) ||
			data.Containers.Count < DefaultVersionOneMinimumContainerCount ||
			data.Zombies.Count < DefaultVersionOneMinimumZombieCount ||
			(expectedContainerCount >= 0 &&
				data.Containers.Count != expectedContainerCount) ||
			(expectedZombieCount >= 0 &&
				data.Zombies.Count != expectedZombieCount) ||
			!HasValidItemStacks(data.PlayerInventory))
		{
			return false;
		}

		HashSet<string> containerPaths = new(StringComparer.Ordinal);
		foreach (ContainerSaveData container in data.Containers)
		{
			if (container is null || string.IsNullOrWhiteSpace(container.NodePath) ||
				!containerPaths.Add(container.NodePath) || !HasValidItemStacks(container.Items))
			{
				return false;
			}
		}

		HashSet<string> zombiePaths = new(StringComparer.Ordinal);
		foreach (ZombieSaveData zombie in data.Zombies)
		{
			if (zombie is null || string.IsNullOrWhiteSpace(zombie.NodePath) ||
				!zombiePaths.Add(zombie.NodePath))
			{
				return false;
			}
		}
		return true;
	}

	private static bool HasValidOptionalWeatherState(SaveGameDataV1 data)
	{
		if (data.WeatherKind == -1)
		{
			return Mathf.IsEqualApprox(data.WeatherSecondsUntilChange, -1.0f) &&
				data.WeatherScheduleRandomState == 0 &&
				Mathf.IsEqualApprox(data.WeatherSecondsUntilLightning, -1.0f) &&
				data.WeatherLightningRandomState == 0;
		}

		return Enum.IsDefined(typeof(WeatherKind), data.WeatherKind) &&
			IsFinite(data.WeatherSecondsUntilChange) &&
			data.WeatherSecondsUntilChange >= 0.0f &&
			(data.WeatherSecondsUntilLightning < 0.0f
				? Mathf.IsEqualApprox(data.WeatherSecondsUntilLightning, -1.0f)
				: IsFinite(data.WeatherSecondsUntilLightning));
	}

	private static bool HasValidItemStacks(List<ItemStackSaveData>? stacks)
	{
		if (stacks is null)
		{
			return false;
		}
		foreach (ItemStackSaveData stack in stacks)
		{
			if (stack is null || stack.Quantity <= 0 || stack.SlotIndex < -1 ||
				!ItemResourcePaths.ContainsKey(stack.ItemId))
			{
				return false;
			}
		}
		return true;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Echo: true })
		{
			return;
		}

		if (@event.IsActionPressed("save_game"))
		{
			SaveGame();
			GetViewport().SetInputAsHandled();
		}
		else if (@event.IsActionPressed("load_game"))
		{
			LoadGame();
			GetViewport().SetInputAsHandled();
		}
	}

	public bool SaveGame()
	{
		string temporaryPath = GetTemporarySavePath();
		try
		{
			SaveGameDataV1 saveData = CaptureState();
			if (!TryValidate(saveData, out _))
			{
				throw new InvalidOperationException("Captured state did not pass version 1 validation.");
			}

			WriteSaveAtomically(JsonSerializer.Serialize(saveData, JsonOptions), temporaryPath);
			EmitSignal(SignalName.StatusMessageRequested, "Game Saved");
			return true;
		}
		catch (Exception exception)
		{
			TryDeleteFile(temporaryPath);
			GD.PushWarning($"Save failed: {exception.Message}");
			EmitSignal(SignalName.StatusMessageRequested, "Game Could Not Be Saved");
			return false;
		}
	}

	public bool LoadGame()
	{
		if (!GodotFileAccess.FileExists(SaveFilePath))
		{
			EmitSignal(SignalName.StatusMessageRequested, "No Save Found");
			return false;
		}

		try
		{
			using GodotFileAccess? file = GodotFileAccess.Open(SaveFilePath, GodotFileAccess.ModeFlags.Read);
			if (file is null)
			{
				throw new InvalidOperationException($"Could not open save file ({GodotFileAccess.GetOpenError()}).");
			}

			SaveGameDataV1? saveData = JsonSerializer.Deserialize<SaveGameDataV1>(file.GetAsText(), JsonOptions);
			if (saveData is null)
			{
				throw new InvalidOperationException("Save data was empty.");
			}
			if (saveData.Version > SaveGameDataV1.CurrentVersion)
			{
				throw new InvalidOperationException(
					$"Save version {saveData.Version} is newer than supported version " +
					$"{SaveGameDataV1.CurrentVersion}.");
			}
			if (saveData.Version < SaveGameDataV1.CurrentVersion)
			{
				throw new InvalidOperationException($"Save version {saveData.Version} is not supported.");
			}
			if (!TryValidate(saveData, out ValidatedSaveData validated))
			{
				throw new InvalidOperationException("Save version 1 data failed validation.");
			}

			ApplyState(saveData, validated);
			EmitSignal(SignalName.StatusMessageRequested, "Game Loaded");
			return true;
		}
		catch (Exception exception)
		{
			GD.PushWarning($"Load failed safely: {exception.Message}");
			EmitSignal(SignalName.StatusMessageRequested, "Save Could Not Be Loaded");
			return false;
		}
	}

	private SaveGameDataV1 CaptureState()
	{
		Node worldRoot = GetPersistenceRoot();
		SaveGameDataV1 data = new()
		{
			PlayerTransform = new TransformSaveData
			{
				Position = Vector3SaveData.FromVector(_player.GlobalPosition),
				Rotation = Vector3SaveData.FromVector(_player.Rotation),
			},
			PlayerHealth = _health.CurrentHealth,
			PlayerStamina = _stamina.CurrentStamina,
			PlayerCanSprint = _stamina.CanSprint,
			PlayerHunger = _needs.CurrentHunger,
			PlayerThirst = _needs.CurrentThirst,
			ObjectiveState = (int)_objective.State,
			ServiceStationObjectiveState = (int)_suppliesObjective.State,
			WorldTimeHours = _worldTime.CurrentHour,
		};
		if (_weatherDirector?.CurrentProfile is WeatherProfile weatherProfile)
		{
			data.WeatherKind = (int)weatherProfile.Kind;
			data.WeatherSecondsUntilChange =
				Mathf.Max(_weatherDirector.SecondsUntilWeatherChange, 0.0f);
			data.WeatherScheduleRandomState = _weatherDirector.ScheduleRandomState;
			data.WeatherSecondsUntilLightning = _weatherDirector.SecondsUntilLightning;
			data.WeatherLightningRandomState = _weatherDirector.LightningRandomState;
		}
		data.PlayerInventory = CaptureItems(_playerInventory, includeSlotIndices: true);

		foreach (SearchableContainer container in GetContainers())
		{
			data.Containers.Add(new ContainerSaveData
			{
				NodePath = worldRoot.GetPathTo(container).ToString(),
				IsSearched = container.IsSearched,
				Items = CaptureItems(container.Inventory),
			});
		}

		foreach (PrototypeZombie zombie in GetZombies())
		{
			data.Zombies.Add(new ZombieSaveData
			{
				NodePath = worldRoot.GetPathTo(zombie).ToString(),
				IsAlive = zombie.IsAlive,
			});
		}

		return data;
	}

	private bool TryValidate(SaveGameDataV1 data, out ValidatedSaveData validated)
	{
		validated = null!;
		if (data.PlayerTransform is null ||
			data.PlayerTransform.Position is null ||
			data.PlayerTransform.Rotation is null ||
			data.PlayerInventory is null ||
			data.Containers is null ||
			data.Zombies is null ||
			data.Version != SaveGameDataV1.CurrentVersion ||
			!Enum.IsDefined(typeof(AntibioticsObjectiveState), data.ObjectiveState) ||
			!Enum.IsDefined(
				typeof(ServiceStationSuppliesObjectiveState),
				data.ServiceStationObjectiveState) ||
			!IsFinite(data.PlayerHealth) || !IsFinite(data.PlayerStamina) ||
			!IsFinite(data.PlayerHunger) || !IsFinite(data.PlayerThirst) ||
			!IsFinite(data.WorldTimeHours) || !IsFinite(data.PlayerTransform.Position) ||
			!IsFinite(data.PlayerTransform.Rotation) ||
			!HasValidOptionalWeatherState(data) ||
			data.PlayerHealth < 0.0f || data.PlayerHealth > _health.MaximumHealth ||
			data.PlayerStamina < 0.0f || data.PlayerStamina > _stamina.MaximumStamina ||
			data.PlayerHunger < 0.0f || data.PlayerHunger > _needs.MaximumHunger ||
			data.PlayerThirst < 0.0f || data.PlayerThirst > _needs.MaximumThirst ||
			data.WorldTimeHours < 0.0f || data.WorldTimeHours >= 24.0f)
		{
			return false;
		}

		Node worldRoot = GetPersistenceRoot();
		List<SearchableContainer> existingContainers = GetContainers();
		List<PrototypeZombie> existingZombies = GetZombies();
		if (existingContainers.Count < MinimumContainerCount ||
			existingZombies.Count < MinimumZombieCount ||
			data.Containers.Count != existingContainers.Count ||
			data.Zombies.Count != existingZombies.Count)
		{
			return false;
		}

		ValidatedSaveData result = new();
		if (!TryResolveItems(data.PlayerInventory, out List<ResolvedItem> playerItems))
		{
			return false;
		}
		if (!_playerInventory.CanRestoreSavedStacks(ToRestoreData(playerItems)))
		{
			return false;
		}
		result.PlayerItems = playerItems;

		HashSet<SearchableContainer> seenContainers = new();
		foreach (ContainerSaveData containerData in data.Containers)
		{
			SearchableContainer? container = containerData is null
				? null
				: worldRoot.GetNodeOrNull<SearchableContainer>(containerData.NodePath);
			if (containerData is null || containerData.Items is null ||
				container is null || !seenContainers.Add(container) ||
				!TryResolveItems(containerData.Items, out List<ResolvedItem>? items) ||
				!container.Inventory.CanRestoreSavedStacks(ToRestoreData(items)))
			{
				return false;
			}
			result.Containers.Add((container, containerData, items));
		}

		HashSet<PrototypeZombie> seenZombies = new();
		foreach (ZombieSaveData zombieData in data.Zombies)
		{
			PrototypeZombie? zombie = zombieData is null
				? null
				: worldRoot.GetNodeOrNull<PrototypeZombie>(zombieData.NodePath);
			if (zombieData is null || zombie is null || !seenZombies.Add(zombie))
			{
				return false;
			}
			result.Zombies.Add((zombie, zombieData.IsAlive));
		}

		validated = result;
		return true;
	}

	private void ApplyState(SaveGameDataV1 data, ValidatedSaveData validated)
	{
		_player.GlobalPosition = data.PlayerTransform.Position.ToVector();
		_player.Rotation = data.PlayerTransform.Rotation.ToVector();
		_player.Velocity = Vector3.Zero;
		_health.RestoreState(data.PlayerHealth);
		_stamina.RestoreState(data.PlayerStamina, data.PlayerCanSprint);
		_needs.RestoreState(data.PlayerHunger, data.PlayerThirst);
		RestoreItems(_playerInventory, validated.PlayerItems);

		foreach ((SearchableContainer container, ContainerSaveData containerData, List<ResolvedItem> items) in validated.Containers)
		{
			RestoreItems(container.Inventory, items);
			container.RestoreSearchedState(containerData.IsSearched);
		}

		foreach ((PrototypeZombie zombie, bool isAlive) in validated.Zombies)
		{
			zombie.SetAlive(isAlive);
		}

		_objective.RestoreState((AntibioticsObjectiveState)data.ObjectiveState);
		_suppliesObjective.RestoreState(
			(ServiceStationSuppliesObjectiveState)data.ServiceStationObjectiveState);
		_worldTime.SetTimeOfDay(data.WorldTimeHours);
		if (_weatherDirector is not null && data.WeatherKind >= 0)
		{
			if (!_weatherDirector.RestoreWeatherState(
				(WeatherKind)data.WeatherKind,
				data.WeatherSecondsUntilChange,
				data.WeatherScheduleRandomState,
				data.WeatherSecondsUntilLightning,
				data.WeatherLightningRandomState))
			{
				// Weather is additive to version 1. A district that no longer offers a
				// saved profile keeps its authored condition without blocking core state.
				GD.PushWarning(
					$"Saved weather kind {(WeatherKind)data.WeatherKind} is unavailable; " +
					"the district's authored weather was retained.");
			}
		}
	}

	private static List<ItemStackSaveData> CaptureItems(
		ItemStorage inventory,
		bool includeSlotIndices = false)
	{
		List<ItemStackSaveData> items = new();
		foreach (int index in inventory.GetOccupiedSlotIndices())
		{
			items.Add(new ItemStackSaveData
			{
				ItemId = inventory.GetItemAt(index)!.ItemId.ToString(),
				Quantity = inventory.GetQuantityAt(index),
				SlotIndex = includeSlotIndices ? index : -1,
			});
		}
		return items;
	}

	private static bool TryResolveItems(List<ItemStackSaveData>? itemData, out List<ResolvedItem> items)
	{
		items = new List<ResolvedItem>();
		if (itemData is null)
		{
			return false;
		}
		foreach (ItemStackSaveData stack in itemData)
		{
			if (stack is null || stack.Quantity <= 0 || stack.SlotIndex < -1 ||
				string.IsNullOrWhiteSpace(stack.ItemId) ||
				!ItemResourcePaths.TryGetValue(stack.ItemId, out string? resourcePath))
			{
				return false;
			}

			ItemDefinition? item = GD.Load<ItemDefinition>(resourcePath);
			if (item is null || item.ItemId.ToString() != stack.ItemId)
			{
				return false;
			}
			items.Add(new ResolvedItem(item, stack.Quantity, stack.SlotIndex));
		}
		return true;
	}

	private static void RestoreItems(ItemStorage inventory, List<ResolvedItem> items)
	{
		if (!inventory.RestoreSavedStacks(ToRestoreData(items)))
		{
			throw new InvalidOperationException("Validated inventory state could not be restored.");
		}
	}

	private static List<ItemStackRestoreData> ToRestoreData(List<ResolvedItem> items)
	{
		List<ItemStackRestoreData> result = new(items.Count);
		foreach (ResolvedItem item in items)
		{
			result.Add(new ItemStackRestoreData(
				item.Definition,
				item.Quantity,
				item.SlotIndex));
		}
		return result;
	}

	private List<SearchableContainer> GetContainers()
	{
		Node persistenceRoot = GetPersistenceRoot();
		return GetTree().GetNodesInGroup(SearchableContainer.GroupName)
			.OfType<SearchableContainer>()
			.Where(node => persistenceRoot.IsAncestorOf(node))
			.OrderBy(node => node.GetPath().ToString(), StringComparer.Ordinal)
			.ToList();
	}

	private List<PrototypeZombie> GetZombies()
	{
		Node persistenceRoot = GetPersistenceRoot();
		return GetTree().GetNodesInGroup(PrototypeZombie.ZombieGroupName)
			.OfType<PrototypeZombie>()
			.Where(node => persistenceRoot.IsAncestorOf(node))
			.OrderBy(node => node.GetPath().ToString(), StringComparer.Ordinal)
			.ToList();
	}

	private Node GetPersistenceRoot()
	{
		return GetNode(PersistenceRootPath);
	}

	private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

	private static bool IsFinite(Vector3SaveData value)
	{
		return value is not null && IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
	}

	private void WriteSaveAtomically(string contents, string temporaryPath)
	{
		using (GodotFileAccess? file = GodotFileAccess.Open(
			temporaryPath,
			GodotFileAccess.ModeFlags.Write))
		{
			if (file is null)
			{
				throw new InvalidOperationException(
					$"Could not open temporary save file ({GodotFileAccess.GetOpenError()}).");
			}

			file.StoreString(contents);
			file.Flush();
		}

		string primaryAbsolutePath = ProjectSettings.GlobalizePath(SaveFilePath);
		string temporaryAbsolutePath = ProjectSettings.GlobalizePath(temporaryPath);
		string backupAbsolutePath = ProjectSettings.GlobalizePath(GetBackupSavePath());
		string? saveDirectory = Path.GetDirectoryName(primaryAbsolutePath);
		if (!string.IsNullOrEmpty(saveDirectory))
		{
			Directory.CreateDirectory(saveDirectory);
		}

		if (File.Exists(primaryAbsolutePath))
		{
			TryDeleteAbsoluteFile(backupAbsolutePath);
			File.Replace(
				temporaryAbsolutePath,
				primaryAbsolutePath,
				backupAbsolutePath,
				ignoreMetadataErrors: true);
			TryDeleteAbsoluteFile(backupAbsolutePath);
		}
		else
		{
			File.Move(temporaryAbsolutePath, primaryAbsolutePath);
		}
	}

	private string GetTemporarySavePath() => $"{SaveFilePath}.tmp";
	private string GetBackupSavePath() => $"{SaveFilePath}.bak";

	private static void TryDeleteFile(string path)
	{
		TryDeleteAbsoluteFile(ProjectSettings.GlobalizePath(path));
	}

	private static void TryDeleteAbsoluteFile(string absolutePath)
	{
		try
		{
			if (File.Exists(absolutePath))
			{
				File.Delete(absolutePath);
			}
		}
		catch (Exception)
		{
			// Cleanup failure must not replace the primary save failure or success.
		}
	}

	private sealed record ResolvedItem(
		ItemDefinition Definition,
		int Quantity,
		int SlotIndex);

	private sealed class ValidatedSaveData
	{
		public List<ResolvedItem> PlayerItems { get; set; } = new();
		public List<(SearchableContainer, ContainerSaveData, List<ResolvedItem>)> Containers { get; } = new();
		public List<(PrototypeZombie, bool)> Zombies { get; } = new();
	}
}
