#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetSchoolPerformanceBenchmark : Node
{
	private const int WarmupFrames = 180;
	private const int SampleFrames = 600;

	public override async void _Ready()
	{
		try
		{
			PerformanceBenchmarkDiagnostics.ConfigureRuntime();

			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			AddChild(world);

			Node3D player = world.GetNode<Node3D>("Gameplay/Player");
			player.ProcessMode = ProcessModeEnum.Disabled;
			player.Visible = false;
			Node3D zombies = world.GetNode<Node3D>("Gameplay/Zombies");
			zombies.ProcessMode = ProcessModeEnum.Disabled;
			zombies.Visible = false;
			world.GetNode<CanvasLayer>("Gameplay/GameplayHUD").Visible = false;

			WorldTime worldTime =
				world.GetNode<WorldTime>("Gameplay/WorldTime");
			worldTime.SetTimeOfDay(16.0f);
			worldTime.SetProcess(false);

			Camera3D camera = new()
			{
				Name = "SchoolBenchmarkCamera",
				Current = true,
				Near = 0.05f,
				Far = 240.0f,
				Fov = 72.0f,
				Position = new Vector3(68.0f, 2.1f, 1.4f),
			};
			world.AddChild(camera);
			camera.LookAt(new Vector3(91.0f, 2.8f, -15.5f), Vector3.Up);

			PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration(
				"MAIN_STREET_SCHOOL_BENCHMARK");
			GD.Print(
				"MAIN_STREET_SCHOOL_BENCHMARK_VIEW: " +
				"1280x720 street-and-school approach, Compatibility renderer");

			for (int frame = 0; frame < WarmupFrames; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			PerformanceBenchmarkSampler sampler = new();
			ulong previousTicks = Time.GetTicksUsec();
			for (int frame = 0; frame < SampleFrames; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				ulong currentTicks = Time.GetTicksUsec();
				sampler.AddFrame(
					(currentTicks - previousTicks) / 1_000_000.0);
				previousTicks = currentTicks;
			}

			GD.Print(sampler.CreateReport(
				"MAIN_STREET_SCHOOL_BENCHMARK"));
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_SCHOOL_BENCHMARK: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}
}
