#nullable enable

using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.Objectives;

namespace AshwoodCounty3DPrototype.Save;

public sealed class SaveGameDataV1
{
	public const int CurrentVersion = 1;

	public int Version { get; set; } = CurrentVersion;
	public TransformSaveData PlayerTransform { get; set; } = new();
	public float PlayerHealth { get; set; }
	public float PlayerStamina { get; set; }
	public bool PlayerCanSprint { get; set; }
	public float PlayerHunger { get; set; } = 100.0f;
	public float PlayerThirst { get; set; } = 100.0f;
	public List<ItemStackSaveData> PlayerInventory { get; set; } = new();
	public int ObjectiveState { get; set; }
	public int ServiceStationObjectiveState { get; set; } =
		(int)ServiceStationSuppliesObjectiveState.Locked;
	public float WorldTimeHours { get; set; }
	public List<ContainerSaveData> Containers { get; set; } = new();
	public List<ZombieSaveData> Zombies { get; set; } = new();
}

public sealed class TransformSaveData
{
	public Vector3SaveData Position { get; set; } = new();
	public Vector3SaveData Rotation { get; set; } = new();
}

public sealed class Vector3SaveData
{
	public float X { get; set; }
	public float Y { get; set; }
	public float Z { get; set; }

	public static Vector3SaveData FromVector(Vector3 value)
	{
		return new Vector3SaveData { X = value.X, Y = value.Y, Z = value.Z };
	}

	public Vector3 ToVector()
	{
		return new Vector3(X, Y, Z);
	}
}

public sealed class ItemStackSaveData
{
	public string ItemId { get; set; } = string.Empty;
	public int Quantity { get; set; }
}

public sealed class ContainerSaveData
{
	public string NodePath { get; set; } = string.Empty;
	public bool IsSearched { get; set; }
	public List<ItemStackSaveData> Items { get; set; } = new();
}

public sealed class ZombieSaveData
{
	public string NodePath { get; set; } = string.Empty;
	public bool IsAlive { get; set; }
}
