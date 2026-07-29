#nullable enable

using System;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetApocalypseDressingVisualReview : Node3D
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
			HideInteriorGeometry(world);
			AddChild(world);

			Node3D dressing = world.GetNode<Node3D>(
				"Environment/ApocalypseDressing");
			if (dressing.GetChildCount() == 0)
			{
				throw new InvalidOperationException(
					"The integrated apocalypse dressing is empty.");
			}

			Camera3D camera = new()
			{
				Current = true,
				Near = 0.05f,
				Far = 280.0f,
			};
			AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/main_street_apocalypse_dressing_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			ReviewShot[] shots =
			{
				new(
					"01_west_to_east_overview.png",
					new Vector3(-94.0f, 5.4f, 0.2f),
					new Vector3(18.0f, 1.25f, 0.0f),
					58.0f),
				new(
					"02_north_sidewalk_layers.png",
					new Vector3(-58.0f, 1.78f, -7.15f),
					new Vector3(28.0f, 1.0f, -7.0f),
					67.0f),
				new(
					"03_south_sidewalk_layers.png",
					new Vector3(78.0f, 1.8f, 7.1f),
					new Vector3(-8.0f, 1.05f, 7.0f),
					67.0f),
				new(
					"04_west_relief_story_cluster.png",
					new Vector3(-88.0f, 2.4f, 0.2f),
					new Vector3(-96.0f, 0.8f, -5.4f),
					61.0f),
			};

			await ToSignal(
				GetTree().CreateTimer(0.1),
				SceneTreeTimer.SignalName.Timeout);
			foreach (ReviewShot shot in shots)
			{
				camera.Position = shot.Position;
				camera.Fov = shot.Fov;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int frame = 0; frame < 5; frame++)
				{
					await ToSignal(
						GetTree(),
						SceneTree.SignalName.ProcessFrame);
				}

				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);
				Error error = GetViewport()
					.GetTexture()
					.GetImage()
					.SavePng(Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			GD.Print(
				$"MAIN_STREET_APOCALYPSE_DRESSING_VISUAL_REVIEW: " +
				outputDirectory);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_APOCALYPSE_DRESSING_VISUAL_REVIEW: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
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
