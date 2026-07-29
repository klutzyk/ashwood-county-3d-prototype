#nullable enable

using System;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class SilverSpoonVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target);

	public override async void _Ready()
	{
		try
		{
			Node3D diner = GD.Load<PackedScene>(
				"res://assets/environment/buildings/Diner/diner.tscn")
				.Instantiate<Node3D>();
			AddChild(diner);

			AddDaylight();

			Camera3D camera = new()
			{
				Current = true,
				Fov = 72.0f,
				Near = 0.05f,
				Far = 80.0f,
			};
			AddChild(camera);

			DoorController door = diner.GetNode<DoorController>("FrontDoor");
			door.AnimationDuration = 0.01f;
			door.ToggleDoor();

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/silver_spoon_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			ReviewShot[] shots =
			{
				new("01_exterior.png",
					new Vector3(-13.0f, 5.0f, 10.5f),
					new Vector3(-3.7f, 2.15f, 0.0f)),
				new("02_entry.png",
					new Vector3(-3.35f, 1.72f, -0.65f),
					new Vector3(1.25f, 1.25f, 0.25f)),
				new("03_dining.png",
					new Vector3(-1.72f, 1.78f, 3.2f),
					new Vector3(-2.35f, 1.05f, -1.5f)),
				new("04_counter_kitchen.png",
					new Vector3(-1.62f, 1.82f, -1.85f),
					new Vector3(5.6f, 1.22f, 0.8f)),
				new("05_cookline.png",
					new Vector3(4.65f, 1.72f, -2.85f),
					new Vector3(8.05f, 1.2f, 1.35f)),
				new("06_back_rooms.png",
					new Vector3(4.2f, 1.72f, 1.35f),
					new Vector3(6.35f, 1.25f, 4.85f)),
			};

			foreach (ReviewShot shot in shots)
			{
				camera.Position = shot.Position;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int frame = 0; frame < 3; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}

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

			GD.Print($"SILVER_SPOON_VISUAL_REVIEW: {outputDirectory}");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"SILVER_SPOON_VISUAL_REVIEW: FAIL - {exception.Message}");
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
			AmbientLightColor = new Color(0.78f, 0.83f, 0.86f),
			AmbientLightEnergy = 0.72f,
			ReflectedLightSource = Godot.Environment.ReflectionSource.Bg,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};
		AddChild(new WorldEnvironment { Environment = environment });

		DirectionalLight3D sun = new()
		{
			LightColor = new Color(1.0f, 0.9f, 0.76f),
			LightEnergy = 1.2f,
			ShadowEnabled = true,
			RotationDegrees = new Vector3(-42.0f, -128.0f, 0.0f),
		};
		AddChild(sun);
	}
}
