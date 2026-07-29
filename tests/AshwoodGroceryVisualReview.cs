#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodGroceryVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target);

	public override async void _Ready()
	{
		try
		{
			Node3D grocery = GD.Load<PackedScene>(
				"res://assets/environment/buildings/AshwoodGrocery/ashwood_grocery.tscn")
				.Instantiate<Node3D>();
			AddChild(grocery);
			AddDaylight();

			Camera3D camera = new()
			{
				Current = true,
				Fov = 72.0f,
				Near = 0.05f,
				Far = 90.0f,
			};
			AddChild(camera);

			DoorController door =
				grocery.GetNode<DoorController>("FrontDoor");
			door.AnimationDuration = 0.01f;
			door.ToggleDoor();

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/ashwood_grocery_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			ReviewShot[] shots =
			{
				new("01_exterior.png",
					new Vector3(-18.0f, 5.7f, 13.8f),
					new Vector3(-7.8f, 2.25f, 0.0f)),
				new("02_entry.png",
					new Vector3(-7.25f, 1.72f, -0.2f),
					new Vector3(-1.6f, 1.25f, 0.0f)),
				new("03_sales_aisles.png",
					new Vector3(-4.8f, 1.78f, -8.4f),
					new Vector3(1.6f, 1.15f, -1.2f)),
				new("04_checkout_produce.png",
					new Vector3(-5.4f, 1.75f, 6.4f),
					new Vector3(-0.5f, 1.05f, 1.8f)),
				new("05_refrigeration.png",
					new Vector3(1.2f, 1.72f, 7.8f),
					new Vector3(6.7f, 1.25f, 2.2f)),
				new("06_service_rooms.png",
					new Vector3(3.1f, 1.72f, -1.8f),
					new Vector3(6.4f, 1.20f, -7.5f)),
			};

			foreach (ReviewShot shot in shots)
			{
				camera.Position = shot.Position;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int frame = 0; frame < 3; frame++)
				{
					await ToSignal(
						GetTree(),
						SceneTree.SignalName.ProcessFrame);
				}

				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);
				Image image = GetViewport().GetTexture().GetImage();
				Error error = image.SavePng(
					System.IO.Path.Combine(
						outputDirectory,
						shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			GD.Print($"ASHWOOD_GROCERY_VISUAL_REVIEW: {outputDirectory}");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"ASHWOOD_GROCERY_VISUAL_REVIEW: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private void AddDaylight()
	{
		Godot.Environment environment = new()
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.46f, 0.58f, 0.66f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.76f, 0.81f, 0.84f),
			AmbientLightEnergy = 0.68f,
			ReflectedLightSource =
				Godot.Environment.ReflectionSource.Bg,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};
		AddChild(new WorldEnvironment { Environment = environment });

		AddChild(new DirectionalLight3D
		{
			LightColor = new Color(1.0f, 0.9f, 0.76f),
			LightEnergy = 1.2f,
			ShadowEnabled = true,
			RotationDegrees = new Vector3(-42.0f, -128.0f, 0.0f),
		});
	}
}
