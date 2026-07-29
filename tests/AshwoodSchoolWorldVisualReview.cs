#nullable enable

using System;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Captures the completed school in its production Main Street context without
/// allowing player, zombie, objective, save, or HUD processing to affect the
/// review.
/// </summary>
public partial class AshwoodSchoolWorldVisualReview : Node3D
{
	private const string MainStreetScenePath =
		"res://scenes/world/ashwood/main_street.tscn";
	private const string SchoolNodePath =
		"Environment/AshwoodSchool";
	private const string OutputDirectoryPath =
		"res://.godot/ashwood_school_world_review";

	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target,
		float Fov);

	public override async void _Ready()
	{
		Node3D? world = null;
		try
		{
			PackedScene mainStreetScene =
				GD.Load<PackedScene>(MainStreetScenePath) ??
				throw new InvalidOperationException(
					$"Could not load {MainStreetScenePath}.");
			world = mainStreetScene.Instantiate<Node3D>();
			SuppressGameplay(world);
			AddChild(world);
			SuppressGameplay(world);

			Camera3D camera = new()
			{
				Name = "SchoolWorldReviewCamera",
				Current = true,
				Near = 0.05f,
				Far = 320.0f,
			};
			AddChild(camera);

			for (int warmupFrame = 0; warmupFrame < 12; warmupFrame++)
			{
				await ToSignal(
					GetTree(),
					SceneTree.SignalName.ProcessFrame);
			}

			Node3D school = world.GetNodeOrNull<Node3D>(SchoolNodePath) ??
				throw new InvalidOperationException(
					$"Main Street does not contain {SchoolNodePath}.");
			if (school.GetNodeOrNull<Node3D>("AuthoredSchool") is null)
			{
				throw new InvalidOperationException(
					"The school environment did not finish building.");
			}

			string outputDirectory =
				ProjectSettings.GlobalizePath(OutputDirectoryPath);
			Error directoryError =
				DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
			if (directoryError != Error.Ok)
			{
				throw new InvalidOperationException(
					$"Could not create capture directory: {directoryError}");
			}

			ReviewShot[] shots =
			{
				new(
					"01_down_street_context.png",
					new Vector3(34.0f, 5.8f, 2.4f),
					new Vector3(90.0f, 3.0f, -10.5f),
					61.0f),
				new(
					"02_facade_sidewalk_entrance.png",
					new Vector3(82.0f, 2.75f, 2.7f),
					new Vector3(88.5f, 2.6f, -9.15f),
					64.0f),
				new(
					"03_school_police_relationship.png",
					new Vector3(61.0f, 8.2f, 0.0f),
					new Vector3(88.0f, 2.5f, 0.0f),
					68.0f),
				new(
					"04_rear_activity_yard.png",
					new Vector3(120.0f, 6.4f, -18.0f),
					new Vector3(103.5f, 1.9f, -27.5f),
					69.0f),
			};

			foreach (ReviewShot shot in shots)
			{
				camera.Position = shot.Position;
				camera.Fov = shot.Fov;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int settleFrame = 0; settleFrame < 6; settleFrame++)
				{
					await ToSignal(
						GetTree(),
						SceneTree.SignalName.ProcessFrame);
				}

				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);
				Image image = GetViewport().GetTexture().GetImage();
				if (image.IsEmpty())
				{
					throw new InvalidOperationException(
						$"Captured an empty image for {shot.FileName}.");
				}

				Error saveError = image.SavePng(
					Path.Combine(outputDirectory, shot.FileName));
				if (saveError != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {saveError}");
				}
			}

			GD.Print(
				$"ASHWOOD_SCHOOL_WORLD_VISUAL_REVIEW: {outputDirectory}");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				"ASHWOOD_SCHOOL_WORLD_VISUAL_REVIEW: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
		}
	}

	private static void SuppressGameplay(Node3D world)
	{
		Node? gameplay = world.GetNodeOrNull("Gameplay");
		if (gameplay is not null)
		{
			gameplay.ProcessMode = ProcessModeEnum.Disabled;
		}

		Node3D? player =
			world.GetNodeOrNull<Node3D>("Gameplay/Player");
		if (player is not null)
		{
			player.Visible = false;
		}

		Node3D? zombies =
			world.GetNodeOrNull<Node3D>("Gameplay/Zombies");
		if (zombies is not null)
		{
			zombies.Visible = false;
		}

		CanvasLayer? hud =
			world.GetNodeOrNull<CanvasLayer>("Gameplay/GameplayHUD");
		if (hud is not null)
		{
			hud.Visible = false;
		}

		Camera3D? playerCamera = world.GetNodeOrNull<Camera3D>(
			"Gameplay/Player/CameraRig/SpringArm3D/Camera3D");
		if (playerCamera is not null)
		{
			playerCamera.Current = false;
		}
	}
}
