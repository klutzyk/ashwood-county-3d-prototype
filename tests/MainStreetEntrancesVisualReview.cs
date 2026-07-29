#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetEntrancesVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target,
		float Fov = 66.0f);

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			world.GetNode("Gameplay").ProcessMode =
				ProcessModeEnum.Disabled;
			world.GetNode<CanvasLayer>("Gameplay/GameplayHUD").Visible = false;
			world.GetNode<Node3D>("Gameplay/Zombies").Visible = false;
			world.GetNode<Node3D>("Gameplay/Player").Visible = false;
			HideInteriorGeometry(world);
			AddChild(world);

			OpenDoor(world.GetNode<DoorController>("BakeryRoot"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/NorthGrocery/FrontDoor"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/NorthPharmacy/FrontDoor"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/SouthSportingGoods/FrontDoor"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/SouthMillerHardware/FrontDoor"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/SouthDiner/FrontDoor"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/SouthDiner/FrontDoorRight"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/SouthPoliceStation/FrontEntrance/LeftDoor"));
			OpenDoor(world.GetNode<DoorController>(
				"Environment/Presentation/Storefronts/SouthPoliceStation/FrontEntrance/RightDoor"));

			Camera3D camera = new()
			{
				Current = true,
				Near = 0.05f,
				Far = 260.0f,
			};
			AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/main_street_entrances_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			ReviewShot[] shots =
			{
				new(
					"01_bakery_north.png",
					new Vector3(-59.5f, 3.1f, 1.2f),
					new Vector3(-59.5f, 2.0f, -9.3f),
					62.0f),
				new(
					"02_grocery_pharmacy_north.png",
					new Vector3(35.0f, 4.2f, 1.8f),
					new Vector3(35.0f, 2.2f, -10.5f),
					72.0f),
				new(
					"03_willow_south.png",
					new Vector3(-13.0f, 3.4f, -0.5f),
					new Vector3(-13.0f, 2.0f, 9.6f),
					62.0f),
				new(
					"04_hardware_diner_south.png",
					new Vector3(42.0f, 4.3f, -1.8f),
					new Vector3(42.0f, 2.1f, 9.6f),
					73.0f),
				new(
					"05_police_south.png",
					new Vector3(88.0f, 4.4f, -1.2f),
					new Vector3(88.0f, 2.2f, 9.3f),
					66.0f),
				new(
					"06_eastward_street_overview.png",
					new Vector3(-48.0f, 5.5f, 0.0f),
					new Vector3(35.0f, 1.8f, 0.0f),
					61.0f),
			};

			await ToSignal(GetTree().CreateTimer(0.08),
				SceneTreeTimer.SignalName.Timeout);
			foreach (ReviewShot shot in shots)
			{
				camera.Position = shot.Position;
				camera.Fov = shot.Fov;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int frame = 0; frame < 4; frame++)
				{
					await ToSignal(GetTree(),
						SceneTree.SignalName.ProcessFrame);
				}

				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);
				Image image = GetViewport().GetTexture().GetImage();
				Error error = image.SavePng(
					Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			GD.Print(
				$"MAIN_STREET_ENTRANCES_VISUAL_REVIEW: {outputDirectory}");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_ENTRANCES_VISUAL_REVIEW: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void OpenDoor(DoorController door)
	{
		door.AnimationDuration = 0.01f;
		if (!door.IsOpen)
		{
			door.ToggleDoor();
		}
	}

	private static void HideInteriorGeometry(Node3D world)
	{
		string[] paths =
		{
			"BakeryRoot/ProductionInterior",
			"Environment/Presentation/Storefronts/NorthGrocery/Interior",
			"Environment/Presentation/Storefronts/NorthPharmacy/Interior",
			"Environment/Presentation/Storefronts/SouthSportingGoods/Interior",
			"Environment/Presentation/Storefronts/SouthMillerHardware/Interior",
			"Environment/Presentation/Storefronts/SouthDiner/Interior",
			"Environment/Presentation/Storefronts/SouthPoliceStation/AuthoredEnvironment/MainFloor",
			"Environment/Presentation/Storefronts/SouthPoliceStation/AuthoredEnvironment/Basement",
		};
		foreach (string path in paths)
		{
			world.GetNode<Node3D>(path).Visible = false;
		}
	}
}
