#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class RenderedPerformanceBenchmark : Node
{
	private const int WarmupFrames = 180;
	private const int SampleFrames = 600;

	public override async void _Ready()
	{
		try
		{
			PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration(
				"SYNTHETIC_BENCHMARK_INHERITED");
			PerformanceBenchmarkDiagnostics.ConfigureRuntime();

			Node3D world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			WorldTime worldTime = world.GetNode<WorldTime>("WorldTime");
			worldTime.SetProcess(false);
			worldTime.SetTimeOfDay(17.0f);

			Node3D player = world.GetNode<Node3D>("Player");
			player.GlobalPosition = new Vector3(0.0f, 1.0f, 6.0f);
			player.GlobalRotation = Vector3.Zero;
			Node3D cameraRig = player.GetNode<Node3D>("CameraRig");
			cameraRig.Rotation = Vector3.Zero;
			PlayerFlashlight flashlight =
				player.GetNode<PlayerFlashlight>(
					"CameraRig/SpringArm3D/Camera3D/Flashlight");
			if (flashlight.IsEnabled)
			{
				flashlight.Toggle();
			}

			PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration("SYNTHETIC_BENCHMARK");
			PerformanceBenchmarkDiagnostics.PrintWorldAudit("SYNTHETIC_BENCHMARK", world);

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
				sampler.AddFrame((currentTicks - previousTicks) / 1_000_000.0);
				previousTicks = currentTicks;
			}

			GD.Print(sampler.CreateReport("SYNTHETIC_BENCHMARK"));
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"RENDERED_BENCHMARK: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}
}
