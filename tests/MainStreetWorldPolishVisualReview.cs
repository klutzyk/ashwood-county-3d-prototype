#nullable enable

using System;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetWorldPolishVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target,
		float Fov);

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			world.GetNode("Gameplay").ProcessMode = ProcessModeEnum.Disabled;
			world.GetNode<CanvasLayer>("Gameplay/GameplayHUD").Visible = false;
			world.GetNode<Node3D>("Gameplay/Zombies").Visible = false;
			world.GetNode<Node3D>("Gameplay/Player").Visible = false;
			world.GetNode<Camera3D>(
				"Gameplay/Player/CameraRig/SpringArm3D/Camera3D").Current = false;
			AddChild(world);

			Camera3D camera = new()
			{
				Name = "WorldPolishReviewCamera",
				Current = true,
				Near = 0.05f,
				Far = 360.0f,
			};
			AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/main_street_world_polish_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
			ReviewShot[] shots =
			{
				new(
					"01_west_gateway_and_county_context.png",
					new Vector3(-101.0f, 4.6f, 0.2f),
					new Vector3(36.0f, 1.75f, 0.0f),
					58.0f),
				new(
					"02_main_street_clock_landmark.png",
					new Vector3(25.0f, 2.15f, -2.8f),
					new Vector3(69.0f, 3.15f, -7.2f),
					54.0f),
				new(
					"03_wet_road_and_storefront_layers.png",
					new Vector3(-45.0f, 1.72f, 2.9f),
					new Vector3(42.0f, 1.05f, -1.1f),
					63.0f),
				new(
					"04_east_vista_and_world_edge.png",
					new Vector3(55.0f, 3.3f, 0.8f),
					new Vector3(136.0f, 5.0f, 0.0f),
					62.0f),
				new(
					"05_civic_clock_memorial_detail.png",
					new Vector3(74.0f, 1.62f, -4.3f),
					new Vector3(68.0f, 2.65f, -8.28f),
					52.0f),
				new(
					"06_rolling_county_edge.png",
					new Vector3(94.0f, 2.05f, 4.6f),
					new Vector3(148.0f, 4.2f, 39.0f),
					58.0f),
			};

			for (int warmup = 0; warmup < 8; warmup++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			foreach (ReviewShot shot in shots)
			{
				camera.GlobalPosition = shot.Position;
				camera.Fov = shot.Fov;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int frame = 0; frame < 6; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);

				Error error = GetViewport().GetTexture().GetImage().SavePng(
					Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			GD.Print(
				$"MAIN_STREET_WORLD_POLISH_VISUAL_REVIEW: {outputDirectory} " +
				$"({GetViewport().GetVisibleRect().Size})");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				"MAIN_STREET_WORLD_POLISH_VISUAL_REVIEW: FAIL - " + exception);
			GetTree().Quit(1);
		}
	}
}
