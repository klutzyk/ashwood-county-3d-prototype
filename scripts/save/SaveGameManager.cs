#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
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
	private const int VersionOneMinimumContainerCount = 4;
	private const int VersionOneMinimumZombieCount = 5;

	[Signal]
	public delegate void StatusMessageRequestedEventHandler(string message);

	[Export] public string SaveFilePath { get; set; } = "user://ashwood_county_save_v1.json";
	[Export] public NodePath PlayerPath { get; set; } = new("../Player");
	[Export] public NodePath ObjectivePath { get; set; } = new("../AntibioticsObjective");
	[Export] public NodePath WorldTimePath { get; set; } = new("../WorldTime");

	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
	private static readonly IReadOnlyDictionary<string, string> ItemResourcePaths =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["antibiotics"] = "res://assets/items/antibiotics.tres",
			["bandage"] = "res://assets/items/bandage.tres",
			["food"] = "res://assets/items/food.tres",
			["scrap"] = "res://assets/items/scrap.tres",
			["water"] = "res://assets/items/water.tres",
		};

	private ThirdPersonPlayer _player = null!;
	private PlayerHealth _health = null!;
	private PlayerStamina _stamina = null!;
	private PlayerNeeds _needs = null!;
	private PlayerInventory _playerInventory = null!;
	private AntibioticsObjective _objective = null!;
	private WorldTime _worldTime = null!;

	public override void _Ready()
	{
		_player = GetNode<ThirdPersonPlayer>(PlayerPath);
		_health = _player.GetNode<PlayerHealth>("Health");
		_stamina = _player.GetNode<PlayerStamina>("Stamina");
		_needs = _player.GetNode<PlayerNeeds>("Needs");
		_playerInventory = _player.GetNode<PlayerInventory>("Inventory");
		_objective = GetNode<AntibioticsObjective>(ObjectivePath);
		_worldTime = GetNode<WorldTime>(WorldTimePath);
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
		Node worldRoot = GetParent();
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
			WorldTimeHours = _worldTime.CurrentHour,
		};
		data.PlayerInventory = CaptureItems(_playerInventory);

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
			!IsFinite(data.PlayerHealth) || !IsFinite(data.PlayerStamina) ||
			!IsFinite(data.PlayerHunger) || !IsFinite(data.PlayerThirst) ||
			!IsFinite(data.WorldTimeHours) || !IsFinite(data.PlayerTransform.Position) ||
			!IsFinite(data.PlayerTransform.Rotation) ||
			data.PlayerHealth < 0.0f || data.PlayerHealth > _health.MaximumHealth ||
			data.PlayerStamina < 0.0f || data.PlayerStamina > _stamina.MaximumStamina ||
			data.PlayerHunger < 0.0f || data.PlayerHunger > _needs.MaximumHunger ||
			data.PlayerThirst < 0.0f || data.PlayerThirst > _needs.MaximumThirst ||
			data.WorldTimeHours < 0.0f || data.WorldTimeHours >= 24.0f)
		{
			return false;
		}

		Node worldRoot = GetParent();
		List<SearchableContainer> existingContainers = GetContainers();
		List<PrototypeZombie> existingZombies = GetZombies();
		if (data.Containers.Count < VersionOneMinimumContainerCount ||
			data.Containers.Count > existingContainers.Count ||
			data.Zombies.Count < VersionOneMinimumZombieCount ||
			data.Zombies.Count > existingZombies.Count)
		{
			return false;
		}

		ValidatedSaveData result = new();
		if (!TryResolveItems(data.PlayerInventory, out List<ResolvedItem> playerItems))
		{
			return false;
		}
		if (playerItems.Count > _playerInventory.Capacity)
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
				!TryResolveItems(containerData.Items, out List<ResolvedItem>? items))
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
		_worldTime.SetTimeOfDay(data.WorldTimeHours);
	}

	private static List<ItemStackSaveData> CaptureItems(ItemStorage inventory)
	{
		List<ItemStackSaveData> items = new();
		for (int index = 0; index < inventory.StackCount; index++)
		{
			items.Add(new ItemStackSaveData
			{
				ItemId = inventory.GetItemAt(index)!.ItemId.ToString(),
				Quantity = inventory.GetQuantityAt(index),
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
		HashSet<string> seenItemIds = new(StringComparer.Ordinal);
		foreach (ItemStackSaveData stack in itemData)
		{
			if (stack is null || stack.Quantity <= 0 || string.IsNullOrWhiteSpace(stack.ItemId) ||
				!seenItemIds.Add(stack.ItemId) ||
				!ItemResourcePaths.TryGetValue(stack.ItemId, out string? resourcePath))
			{
				return false;
			}

			ItemDefinition? item = GD.Load<ItemDefinition>(resourcePath);
			if (item is null || item.ItemId.ToString() != stack.ItemId)
			{
				return false;
			}
			items.Add(new ResolvedItem(item, stack.Quantity));
		}
		return true;
	}

	private static void RestoreItems(ItemStorage inventory, List<ResolvedItem> items)
	{
		inventory.ClearItems();
		foreach (ResolvedItem item in items)
		{
			inventory.AddItem(item.Definition, item.Quantity);
		}
	}

	private List<SearchableContainer> GetContainers()
	{
		return GetTree().GetNodesInGroup(SearchableContainer.GroupName)
			.OfType<SearchableContainer>()
			.Where(node => GetParent().IsAncestorOf(node))
			.OrderBy(node => node.GetPath().ToString(), StringComparer.Ordinal)
			.ToList();
	}

	private List<PrototypeZombie> GetZombies()
	{
		return GetTree().GetNodesInGroup(PrototypeZombie.ZombieGroupName)
			.OfType<PrototypeZombie>()
			.Where(node => GetParent().IsAncestorOf(node))
			.OrderBy(node => node.GetPath().ToString(), StringComparer.Ordinal)
			.ToList();
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

	private sealed record ResolvedItem(ItemDefinition Definition, int Quantity);

	private sealed class ValidatedSaveData
	{
		public List<ResolvedItem> PlayerItems { get; set; } = new();
		public List<(SearchableContainer, ContainerSaveData, List<ResolvedItem>)> Containers { get; } = new();
		public List<(PrototypeZombie, bool)> Zombies { get; } = new();
	}
}
