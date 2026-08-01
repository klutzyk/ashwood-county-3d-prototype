#nullable enable

using System;
using System.IO;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetGoldenHourSupplyRunVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 CameraPosition,
		Vector3 CameraTarget,
		float Fov,
		Vector3 PlayerPosition,
		float PlayerYaw,
		bool ShowPlayer,
		Vector3 ZombiePosition,
		bool ShowZombie,
		bool ShowHud);

	public override async void _Ready()
	{
		try
		{
			SubViewport captureViewport = new()
			{
				Name = "CaptureViewport",
				Size = new Vector2I(1920, 1080),
				OwnWorld3D = true,
				RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			};
			AddChild(captureViewport);

			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			captureViewport.AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			CanvasLayer gameplayHud =
				world.GetNode<CanvasLayer>("Gameplay/GameplayHUD");
			gameplayHud.Visible = false;
			Node3D player = world.GetNode<Node3D>("Gameplay/Player");
			player.ProcessMode = ProcessModeEnum.Disabled;
			world.GetNode<Camera3D>(
				"Gameplay/Player/CameraRig/SpringArm3D/Camera3D").Current = false;

			PrototypeZombie[] zombies = world
				.GetNode<Node3D>("Gameplay/Zombies")
				.GetChildren()
				.OfType<PrototypeZombie>()
				.ToArray();
			foreach (PrototypeZombie zombie in zombies)
			{
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				zombie.Visible = false;
			}
			PrototypeZombie heroZombie = zombies[0];

			WorldTime worldTime = world.GetNode<WorldTime>("Gameplay/WorldTime");
			worldTime.SetTimeOfDay(16.75f);
			worldTime.SetProcess(false);
			OpenDoor(world.GetNode<DoorController>("BakeryRoot"));

			Camera3D camera = new()
			{
				Name = "GoldenHourReviewCamera",
				Current = true,
				Near = 0.05f,
				Far = 280.0f,
			};
			captureViewport.AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/main_street_golden_hour_supply_run_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
			Node3D bakery = world.GetNode<Node3D>("BakeryRoot");
			Vector3 cacheCameraPosition = bakery.ToGlobal(
				new Vector3(0.45f, 1.52f, 2.66f));
			Vector3 cacheTarget = bakery.ToGlobal(
				new Vector3(-2.25f, 1.2f, 2.66f));

			ReviewShot[] shots =
			{
				new(
					"01_safe_point_departure.png",
					new Vector3(-100.8f, 2.65f, -1.35f),
					new Vector3(-57.0f, 1.42f, -6.6f),
					53.0f,
					new Vector3(-89.0f, 1.11f, -7.25f),
					90.0f,
					true,
					new Vector3(-74.5f, 1.0f, -3.4f),
					true,
					false),
				new(
					"02_bakery_approach.png",
					new Vector3(-79.0f, 2.0f, -1.0f),
					new Vector3(-59.7f, 2.0f, -9.0f),
					55.0f,
					new Vector3(-76.8f, 1.11f, -2.25f),
					90.0f,
					true,
					new Vector3(-64.8f, 1.0f, -1.5f),
					true,
					false),
				new(
					"03_bakery_threshold.png",
					new Vector3(-60.8f, 1.66f, -6.85f),
					new Vector3(-59.45f, 1.35f, -9.8f),
					51.0f,
					new Vector3(-59.6f, 1.11f, -8.35f),
					180.0f,
					true,
					Vector3.Zero,
					false,
					false),
				new(
					"04_bakery_supply_cache.png",
					cacheCameraPosition,
					cacheTarget,
					58.0f,
					Vector3.Zero,
					0.0f,
					false,
					Vector3.Zero,
					false,
					false),
				new(
					"05_gameplay_hud.png",
					new Vector3(-79.0f, 2.0f, -1.0f),
					new Vector3(-59.7f, 2.0f, -9.0f),
					55.0f,
					new Vector3(-76.8f, 1.11f, -2.25f),
					90.0f,
					true,
					new Vector3(-64.8f, 1.0f, -1.5f),
					true,
					true),
			};

			foreach (ReviewShot shot in shots)
			{
				gameplayHud.Visible = shot.ShowHud;
				player.Visible = shot.ShowPlayer;
				if (shot.ShowPlayer)
				{
					player.GlobalPosition = shot.PlayerPosition;
					player.RotationDegrees = new Vector3(0.0f, shot.PlayerYaw, 0.0f);
				}
				heroZombie.Visible = shot.ShowZombie;
				if (shot.ShowZombie)
				{
					heroZombie.GlobalPosition = shot.ZombiePosition;
					heroZombie.RotationDegrees = new Vector3(0.0f, -76.0f, 0.0f);
				}

				camera.GlobalPosition = shot.CameraPosition;
				camera.Fov = shot.Fov;
				camera.LookAt(shot.CameraTarget, Vector3.Up);
				for (int frame = 0; frame < 6; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);

				Image image = captureViewport.GetTexture().GetImage();
				Error error = image.SavePng(
					Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			GD.Print(
				$"MAIN_STREET_GOLDEN_HOUR_SUPPLY_RUN_VISUAL_REVIEW: " +
				$"{outputDirectory} ({captureViewport.Size.X}x{captureViewport.Size.Y})");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				"MAIN_STREET_GOLDEN_HOUR_SUPPLY_RUN_VISUAL_REVIEW: FAIL - " +
				exception.Message);
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
}
