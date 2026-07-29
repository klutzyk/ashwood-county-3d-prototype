#nullable enable

using System;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodPoliceStationValidationVisual : Node3D
{
	private readonly record struct Shot(string FileName, Vector3 Position, Vector3 Target, float Fov = 72.0f);

	public override async void _Ready()
	{
		try
		{
			Node3D station = GD.Load<PackedScene>(
					"res://assets/environment/buildings/AshwoodPoliceStation/ashwood_police_station.tscn")
				.Instantiate<Node3D>();
			AddChild(station);
			AddDaylight();

			Camera3D camera = new()
			{
				Current = true,
				Near = 0.04f,
				Far = 100.0f,
			};
			AddChild(camera);

			string output = ProjectSettings.GlobalizePath(
				"res://.godot/ashwood_police_station_review");
			DirAccess.MakeDirRecursiveAbsolute(output);
			Shot[] shots =
			{
				new("01_exterior.png", new Vector3(-22.5f, 6.0f, 15.0f), new Vector3(-7.2f, 2.3f, 0), 66),
				new("02_lobby.png", new Vector3(-8.0f, 1.72f, -0.2f), new Vector3(-2.3f, 1.25f, 0), 76),
				new("03_waiting_reception.png", new Vector3(-5.7f, 1.75f, -6.8f), new Vector3(-4.6f, 1.2f, 0), 74),
				new("04_offices.png", new Vector3(-2.9f, 1.72f, -3.0f), new Vector3(3.2f, 1.15f, 0.4f), 76),
				new("05_rear_suite.png", new Vector3(5.7f, 1.72f, 6.25f), new Vector3(8.15f, 1.2f, 8.6f), 76),
				new("06_stairs.png", new Vector3(6.25f, -2.65f, -8.15f), new Vector3(0.15f, -0.1f, -8.15f), 72),
				new("07_booking.png", new Vector3(-7.6f, -1.85f, -7.7f), new Vector3(-4.9f, -2.1f, -1.7f), 76),
				new("08_cells_corridor.png", new Vector3(-4.8f, -1.8f, 0), new Vector3(5.1f, -2.0f, 0), 78),
				new("09_cell_interior.png", new Vector3(2.2f, -1.8f, -5.0f), new Vector3(7.0f, -2.0f, -3.5f), 76),
			};

			foreach (Shot shot in shots)
			{
				camera.Fov = shot.Fov;
				camera.Position = shot.Position;
				camera.LookAt(shot.Target, Vector3.Up);
				for (int frame = 0; frame < 4; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				Image image = GetViewport().GetTexture().GetImage();
				Error error = image.SavePng(Path.Combine(output, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException($"Could not save {shot.FileName}: {error}");
				}
			}

			GD.Print($"ASHWOOD_POLICE_STATION_VISUAL_REVIEW:{output}");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ASHWOOD_POLICE_STATION_VISUAL_REVIEW:FAIL:{exception}");
			GetTree().Quit(1);
		}
	}

	private void AddDaylight()
	{
		Godot.Environment environment = new()
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.43f, 0.54f, 0.62f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.73f, 0.78f, 0.78f),
			AmbientLightEnergy = 0.69f,
			ReflectedLightSource = Godot.Environment.ReflectionSource.Bg,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};
		AddChild(new WorldEnvironment { Environment = environment });
		AddChild(new DirectionalLight3D
		{
			LightColor = new Color(1.0f, 0.90f, 0.74f),
			LightEnergy = 1.18f,
			ShadowEnabled = true,
			RotationDegrees = new Vector3(-42, -125, 0),
		});
	}
}
