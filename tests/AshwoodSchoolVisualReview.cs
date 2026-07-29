#nullable enable

using System;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodSchoolVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target,
		float Fov = 70.0f);

	public override async void _Ready()
	{
		Node3D? school = null;
		try
		{
			school = GD.Load<PackedScene>(
					"res://assets/environment/buildings/AshwoodSchool/ashwood_school.tscn")
				.Instantiate<Node3D>();
			AddChild(school);
			AddDaylight();

			Camera3D camera = new()
			{
				Name = "ReviewCamera",
				Current = true,
				Near = 0.04f,
				Far = 120.0f,
			};
			AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/ashwood_school_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			ReviewShot[] shots =
			{
				new("01_street_exterior.png",
					new Vector3(0.0f, 6.4f, 30.0f),
					new Vector3(0.0f, 3.35f, 10.8f), 67.0f),
				new("02_entrance_approach.png",
					new Vector3(-5.6f, 2.1f, 19.5f),
					new Vector3(0.0f, 1.6f, 11.4f), 72.0f),
				new("03_ground_hall.png",
					new Vector3(0.0f, 1.68f, 11.1f),
					new Vector3(0.0f, 1.45f, -9.5f), 72.0f),
				new("04_library.png",
					new Vector3(-2.7f, 1.65f, 2.6f),
					new Vector3(-8.4f, 1.25f, -0.2f), 71.0f),
				new("05_cafeteria.png",
					new Vector3(-2.8f, 1.7f, -5.1f),
					new Vector3(-8.6f, 1.15f, -8.6f), 72.0f),
				new("06_athletics_room.png",
					new Vector3(2.8f, 1.75f, -4.6f),
					new Vector3(8.2f, 1.35f, -8.4f), 74.0f),
				new("06_gym_annex.png",
					new Vector3(15.0f, 2.05f, -3.7f),
					new Vector3(18.4f, 2.25f, -9.0f), 76.0f),
				new("07_stairwell.png",
					new Vector3(2.4f, 1.72f, 3.1f),
					new Vector3(5.4f, 2.0f, 1.6f), 70.0f),
				new("08_upper_hall.png",
					new Vector3(0.0f, 5.02f, 10.6f),
					new Vector3(0.0f, 4.78f, -9.6f), 72.0f),
				new("09_classroom.png",
					new Vector3(2.7f, 5.08f, -5.2f),
					new Vector3(8.1f, 4.72f, -8.1f), 71.0f),
				new("10_activity_yard.png",
					new Vector3(28.0f, 5.2f, 9.0f),
					new Vector3(15.0f, 1.2f, -1.0f), 69.0f),
				new("11_rear_exterior.png",
					new Vector3(-1.0f, 6.2f, -29.0f),
					new Vector3(0.0f, 3.2f, -10.8f), 67.0f),
			};

			for (int warmup = 0; warmup < 10; warmup++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			foreach (ReviewShot shot in shots)
			{
				camera.Fov = shot.Fov;
				camera.Position = shot.Position;
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

				Image image = GetViewport().GetTexture().GetImage();
				Error error = image.SavePng(
					Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			GD.Print($"ASHWOOD_SCHOOL_VISUAL_REVIEW: {outputDirectory}");
			school.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			if (school is not null && IsInstanceValid(school))
			{
				school.QueueFree();
			}
			GD.PushError(
				$"ASHWOOD_SCHOOL_VISUAL_REVIEW: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private void AddDaylight()
	{
		Godot.Environment environment = new()
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.42f, 0.55f, 0.68f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.76f, 0.8f, 0.82f),
			AmbientLightEnergy = 0.72f,
			ReflectedLightSource =
				Godot.Environment.ReflectionSource.Bg,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};
		AddChild(new WorldEnvironment
		{
			Name = "ReviewEnvironment",
			Environment = environment,
		});
		AddChild(new DirectionalLight3D
		{
			Name = "ReviewSun",
			LightColor = new Color(1.0f, 0.9f, 0.75f),
			LightEnergy = 1.16f,
			ShadowEnabled = true,
			RotationDegrees = new Vector3(-44.0f, -132.0f, 0.0f),
		});
	}
}
