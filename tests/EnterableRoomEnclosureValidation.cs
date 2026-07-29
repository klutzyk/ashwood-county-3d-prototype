#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class EnterableRoomEnclosureValidation : Node
{
	private const string DoorSuffix = "/white_door_1.glb";
	private const string DoorLeafPath =
		"Sketchfab_model/root/GLTF_SceneRootNode/Empty_13";

	public override async void _Ready()
	{
		try
		{
			await ValidatePharmacy();
			await ValidateDiner();
			await ValidateWillow();
			await ValidateGrocery();

			GD.Print("ENTERABLE_ROOM_ENCLOSURE_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"ENTERABLE_ROOM_ENCLOSURE_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private async System.Threading.Tasks.Task ValidatePharmacy()
	{
		StaticBody3D interior = await LoadInterior(
			"res://assets/environment/buildings/Pharmacy/interior.tscn");
		ValidateFullHeightWalls(interior, 3.35f, new[]
		{
			"Architecture/StoragePartitionRear",
			"Architecture/StorageCrossWallWest",
			"Architecture/StorageCrossWallEast",
			"Architecture/StaffOfficeFrontNorth",
			"Architecture/StaffOfficeFrontSouth",
			"Architecture/BathroomPartitionRear",
			"Architecture/BathroomCrossWallWest",
			"Architecture/BathroomCrossWallEast",
		});
		ValidateDoors(interior, new[]
		{
			"Architecture/StorageDoor",
			"Architecture/StaffOfficeDoor",
			"Architecture/RestroomDoor",
		});
		ValidateHeaders(interior, new[]
		{
			"Architecture/StorageDoorHeader",
			"Architecture/StaffOfficeDoorHeader",
			"Architecture/RestroomDoorHeader",
		});
		ValidateDoorwayClear(
			interior,
			new Vector3(-6.4f, 1.0f, -3.1f),
			"StorageCrossWestCollision",
			"StorageCrossEastCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(-4.25f, 1.0f, 0),
			"StaffOfficeFrontNorthCollision",
			"StaffOfficeFrontSouthCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(-6.4f, 1.0f, 3.1f),
			"BathroomCrossWestCollision",
			"BathroomCrossEastCollision");
		interior.QueueFree();
	}

	private async System.Threading.Tasks.Task ValidateDiner()
	{
		StaticBody3D interior = await LoadInterior(
			"res://assets/environment/buildings/Diner/interior.tscn");
		ValidateFullHeightWalls(interior, 3.5f, new[]
		{
			"Architecture/NorthRoomWestWall",
			"Architecture/NorthPartitionWestStub",
			"Architecture/NorthPartitionMiddle",
			"Architecture/NorthPartitionEastEnd",
			"Architecture/NorthRoomDivider",
			"Architecture/SouthRoomWestWall",
			"Architecture/SouthPartitionWestStub",
			"Architecture/SouthPartitionMiddle",
			"Architecture/SouthPartitionEastEnd",
			"Architecture/SouthRoomDivider",
		});
		ValidateDoors(interior, new[]
		{
			"Architecture/OfficeDoor",
			"Architecture/PantryDoor",
			"Architecture/RestroomDoor",
			"Architecture/ColdStorageDoor",
		});
		ValidateHeaders(interior, new[]
		{
			"Architecture/OfficeDoorHeader",
			"Architecture/PantryDoorHeader",
			"Architecture/RestroomDoorHeader",
			"Architecture/ColdStorageDoorHeader",
		});
		ValidateDoorwayClear(
			interior,
			new Vector3(1.9f, 1.0f, -4.2f),
			"NorthPartitionWestCollision",
			"NorthPartitionMiddleCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(6.7f, 1.0f, -4.2f),
			"NorthPartitionMiddleCollision",
			"NorthPartitionEastCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(1.9f, 1.0f, 4.2f),
			"SouthPartitionWestCollision",
			"SouthPartitionMiddleCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(6.7f, 1.0f, 4.2f),
			"SouthPartitionMiddleCollision",
			"SouthPartitionEastCollision");
		interior.QueueFree();
	}

	private async System.Threading.Tasks.Task ValidateWillow()
	{
		StaticBody3D interior = await LoadInterior(
			"res://assets/environment/buildings/WillowOutfitters/interior.tscn");
		ValidateFullHeightWalls(interior, 3.7f, new[]
		{
			"Architecture/StoragePartitionNorth",
			"Architecture/StoragePartitionSouth",
			"Architecture/FittingPartitionStub",
			"Architecture/FittingPartitionMiddle",
			"Architecture/FittingPartitionEnd",
			"Architecture/StorageFittingDivider",
			"Architecture/FittingRoomDivider",
		});
		ValidateDoors(interior, new[]
		{
			"Architecture/StorageDoor",
			"Architecture/FittingDoorA",
			"Architecture/FittingDoorB",
		});
		ValidateHeaders(interior, new[]
		{
			"Architecture/StorageDoorHeader",
			"Architecture/FittingDoorAHeader",
			"Architecture/FittingDoorBHeader",
		});
		ValidateDoorwayClear(
			interior,
			new Vector3(2.4f, 1.0f, -1.315f),
			"StorageNorthCollision",
			"StorageSouthCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(2.4f, 1.0f, 2.775f),
			"FittingStubCollision",
			"FittingMiddleCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(2.4f, 1.0f, 4.57f),
			"FittingMiddleCollision",
			"FittingEndCollision");
		interior.QueueFree();
	}

	private async System.Threading.Tasks.Task ValidateGrocery()
	{
		StaticBody3D interior = await LoadInterior(
			"res://assets/environment/buildings/AshwoodGrocery/interior.tscn");
		ValidateFullHeightWalls(interior, 4.3f, new[]
		{
			"Architecture/StorageWestWall",
			"Architecture/StorageFrontWest",
			"Architecture/StorageFrontEast",
			"Architecture/StaffWestWall",
			"Architecture/OfficeFrontWest",
			"Architecture/OfficeFrontEast",
			"Architecture/BathroomDividerWest",
			"Architecture/BathroomDividerEast",
		});
		ValidateDoors(interior, new[]
		{
			"Architecture/StorageDoor",
			"Architecture/OfficeDoor",
			"Architecture/RestroomDoor",
		});
		ValidateHeaders(interior, new[]
		{
			"Architecture/StorageDoorHeader",
			"Architecture/OfficeDoorHeader",
			"Architecture/RestroomDoorHeader",
		});
		ValidateDoorwayClear(
			interior,
			new Vector3(3.1f, 1.0f, -6.05f),
			"StorageFrontWestCollision",
			"StorageFrontEastCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(5.1f, 1.0f, 6.05f),
			"OfficeFrontWestCollision",
			"OfficeFrontEastCollision");
		ValidateDoorwayClear(
			interior,
			new Vector3(6.1f, 1.0f, 8.2f),
			"BathroomDividerWestCollision",
			"BathroomDividerEastCollision");
		interior.QueueFree();
	}

	private async System.Threading.Tasks.Task<StaticBody3D> LoadInterior(
		string scenePath)
	{
		StaticBody3D interior = GD.Load<PackedScene>(scenePath)
			.Instantiate<StaticBody3D>();
		AddChild(interior);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		return interior;
	}

	private static void ValidateFullHeightWalls(
		StaticBody3D interior,
		float minimumHeight,
		IEnumerable<string> paths)
	{
		foreach (string path in paths)
		{
			MeshInstance3D wall = interior.GetNode<MeshInstance3D>(path);
			Require(wall.Scale.Y >= minimumHeight,
				$"{interior.Name}/{path} is a full-height privacy wall");
			Require(HasTexturedMaterial(wall),
				$"{interior.Name}/{path} uses the building's textured PBR finish");
		}
	}

	private static void ValidateDoors(
		StaticBody3D interior,
		IEnumerable<string> paths)
	{
		foreach (string path in paths)
		{
			Node3D door = interior.GetNode<Node3D>(path);
			Require(door.SceneFilePath.EndsWith(
					DoorSuffix,
					StringComparison.Ordinal),
				$"{interior.Name}/{path} is the imported textured door");
			Require(door.Scale.X >= 0.22f &&
				door.Scale.Y >= 0.22f &&
				door.Position.Y <= 0.1f,
				$"{interior.Name}/{path} is visibly full-size and grounded");
			Require(Mathf.Abs(Mathf.Sin(door.Rotation.Y * 2.0f)) <= 0.02f,
				$"{interior.Name}/{path} keeps its imported frame square to the wall");
			Node3D leaf = door.GetNode<Node3D>(DoorLeafPath);
			Require(Mathf.Abs(leaf.Rotation.Y) >= 1.2f,
				$"{interior.Name}/{path} opens its hinged leaf clear of the doorway");
		}
	}

	private static void ValidateHeaders(
		StaticBody3D interior,
		IEnumerable<string> paths)
	{
		foreach (string path in paths)
		{
			MeshInstance3D header = interior.GetNode<MeshInstance3D>(path);
			Require(header.Scale.Y >= 1.15f &&
				HasTexturedMaterial(header),
				$"{interior.Name}/{path} closes the wall above its doorway");
		}
	}

	private static void ValidateDoorwayClear(
		StaticBody3D interior,
		Vector3 doorwayCentre,
		params string[] collisionPaths)
	{
		foreach (string path in collisionPaths)
		{
			CollisionShape3D collision =
				interior.GetNode<CollisionShape3D>(path);
			BoxShape3D box = (BoxShape3D)collision.Shape;
			Vector3 offset = doorwayCentre - collision.Position;
			bool inside = Mathf.Abs(offset.X) < box.Size.X * 0.5f &&
				Mathf.Abs(offset.Y) < box.Size.Y * 0.5f &&
				Mathf.Abs(offset.Z) < box.Size.Z * 0.5f;
			Require(!inside,
				$"{interior.Name}/{path} leaves its doorway collision-free");
		}
	}

	private static bool HasTexturedMaterial(MeshInstance3D instance)
	{
		Material? material = instance.GetSurfaceOverrideMaterial(0) ??
			instance.Mesh?.SurfaceGetMaterial(0);
		return material is StandardMaterial3D standard &&
			standard.AlbedoTexture is not null &&
			standard.RoughnessTexture is not null;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
