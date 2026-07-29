#nullable enable

using System;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class WillowOutfittersVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target);

	public override async void _Ready()
	{
		try
		{
			Node3D willow = GD.Load<PackedScene>(
					"res://assets/environment/buildings/WillowOutfitters/willow_outfitters.tscn")
				.Instantiate<Node3D>();
			AddChild(willow);
			AddDaylight();

			Camera3D camera = new()
			{
				Current = true,
				Fov = 72.0f,
				Near = 0.05f,
				Far = 90.0f,
			};
			AddChild(camera);

			DoorController door = willow.GetNode<DoorController>("FrontDoor");
			door.AnimationDuration = 0.01f;
			door.ToggleDoor();

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/willow_outfitters_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			ReviewShot[] shots =
			{
				new("01_exterior.png",
					new Vector3(-15.5f, 5.1f, 9.2f),
					new Vector3(-5.8f, 2.25f, 0.0f)),
				new("02_entry.png",
					new Vector3(-5.45f, 1.72f, -4.05f),
					new Vector3(-0.35f, 1.25f, -0.2f)),
				new("03_boots_and_apparel.png",
					new Vector3(-2.2f, 1.72f, -2.55f),
					new Vector3(-3.1f, 1.3f, -5.25f)),
				new("04_sales_floor.png",
					new Vector3(-4.75f, 1.8f, 2.95f),
					new Vector3(-0.45f, 1.15f, -0.25f)),
				new("05_checkout.png",
					new Vector3(-1.35f, 1.72f, 2.3f),
					new Vector3(-4.05f, 1.05f, 4.65f)),
				new("06_fitting_rooms.png",
					new Vector3(0.45f, 1.72f, 1.85f),
					new Vector3(4.55f, 1.2f, 4.0f)),
				new("07_stock_room.png",
					new Vector3(2.8f, 1.72f, -0.75f),
					new Vector3(5.0f, 1.2f, -3.8f)),
			};

			foreach (ReviewShot shot in shots)
			{
				camera.Position = shot.Position;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int frame = 0; frame < 4; frame++)
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);
				Image image = GetViewport().GetTexture().GetImage();
				Error error = image.SavePng(
					System.IO.Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			willow.Visible = false;
			Node3D presentation = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/presentation/main_street_presentation.tscn")
				.Instantiate<Node3D>();
			AddChild(presentation);
			camera.Position = new Vector3(-24.0f, 4.4f, -0.5f);
			camera.LookAt(new Vector3(-13.0f, 2.25f, 9.2f), Vector3.Up);
			for (int frame = 0; frame < 5; frame++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(
				RenderingServer.Singleton,
				RenderingServer.SignalName.FramePostDraw);
			Image streetImage = GetViewport().GetTexture().GetImage();
			Error streetError = streetImage.SavePng(
				System.IO.Path.Combine(outputDirectory, "08_main_street.png"));
			if (streetError != Error.Ok)
			{
				throw new InvalidOperationException(
					$"Could not save 08_main_street.png: {streetError}");
			}

			GD.Print($"WILLOW_VISUAL_REVIEW: {outputDirectory}");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"WILLOW_VISUAL_REVIEW: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private void AddDaylight()
	{
		Godot.Environment environment = new()
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.45f, 0.57f, 0.66f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.76f, 0.81f, 0.84f),
			AmbientLightEnergy = 0.74f,
			ReflectedLightSource = Godot.Environment.ReflectionSource.Bg,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};
		AddChild(new WorldEnvironment { Environment = environment });

		AddChild(new DirectionalLight3D
		{
			LightColor = new Color(1.0f, 0.9f, 0.75f),
			LightEnergy = 1.22f,
			ShadowEnabled = true,
			RotationDegrees = new Vector3(-42.0f, -126.0f, 0.0f),
		});
	}
}
