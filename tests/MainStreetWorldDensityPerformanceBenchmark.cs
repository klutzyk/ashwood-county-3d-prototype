#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Retained fixed-view benchmark for the Main Street density presentation.
/// Run once normally and once with ASHWOOD_BENCH_DENSITY=0 to isolate the
/// end-to-end view cost of the infill layer without changing the camera.
/// The result includes secondary visibility/occlusion effects.
/// </summary>
public partial class MainStreetWorldDensityPerformanceBenchmark : Node
{
	private const int DefaultWarmupFrames = 180;
	private const int DefaultSampleFrames = 600;
	private const string DensityPath =
		"Environment/WorldPolish/DensityPresentation";

	public override async void _Ready()
	{
		try
		{
			int warmupFrames = ReadFrameCount(
				"ASHWOOD_BENCH_WARMUP_FRAMES", DefaultWarmupFrames);
			int sampleFrames = ReadFrameCount(
				"ASHWOOD_BENCH_SAMPLE_FRAMES", DefaultSampleFrames);
			bool densityEnabled = ReadDensityEnabled();
			string mode = densityEnabled ? "ON" : "OFF";
			string label = $"MAIN_STREET_DENSITY_BENCHMARK_{mode}";

			PerformanceBenchmarkDiagnostics.ConfigureRuntime();
			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D density = world.GetNode<Node3D>(DensityPath);
			if (!densityEnabled)
			{
				DisableDensityLayer(density);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}

			FreezePresentationConditions(world);
			InstallFixedCamera(world);
			PerformanceBenchmarkDiagnostics.ConfigureRuntime();
			PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration(label);
			GD.Print(
				$"{label}_VIEW: position=(55.0,3.3,0.8), " +
				"target=(136.0,5.0,0.0), fov=62, far=360, " +
				"time=16.75, weather=Clear, gameplay=active, density=" + mode);
			GD.Print(
				$"{label}_TIMING: warmup_frames={warmupFrames}, " +
				$"sample_frames={sampleFrames}");

			for (int frame = 0; frame < warmupFrames; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			PerformanceBenchmarkSampler sampler = new();
			ulong previousTicks = Time.GetTicksUsec();
			for (int frame = 0; frame < sampleFrames; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				ulong currentTicks = Time.GetTicksUsec();
				sampler.AddFrame(
					(currentTicks - previousTicks) / 1_000_000.0);
				previousTicks = currentTicks;
			}

			GD.Print(sampler.CreateReport(label));
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_DENSITY_BENCHMARK: FAIL - {exception}");
			GetTree().Quit(1);
		}
	}

	private static int ReadFrameCount(string variableName, int fallback) =>
		int.TryParse(OS.GetEnvironment(variableName), out int frameCount) &&
		frameCount > 0
			? frameCount
			: fallback;

	private static bool ReadDensityEnabled()
	{
		string value = OS.GetEnvironment("ASHWOOD_BENCH_DENSITY");
		return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) &&
			!string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);
	}

	private static void DisableDensityLayer(Node3D density)
	{
		density.Visible = false;
		density.ProcessMode = ProcessModeEnum.Disabled;
		foreach (Node node in Enumerate(density))
		{
			if (node is CollisionShape3D collision)
			{
				collision.SetDeferred(CollisionShape3D.PropertyName.Disabled, true);
			}
		}
	}

	private static void FreezePresentationConditions(Node3D world)
	{
		WorldTime worldTime = world.GetNode<WorldTime>("Gameplay/WorldTime");
		WeatherDirector weather =
			world.GetNode<WeatherDirector>("Gameplay/DynamicWeather");
		if (!weather.SetWeatherByKind(WeatherKind.Clear, immediate: true))
		{
			throw new InvalidOperationException(
				"Main Street benchmark could not select the Clear weather profile.");
		}
		weather.SetProcess(false);
		weather.SetPhysicsProcess(false);
		worldTime.SetTimeOfDay(16.75f);
		worldTime.SetProcess(false);
		worldTime.SetPhysicsProcess(false);
	}

	private static void InstallFixedCamera(Node3D world)
	{
		Camera3D playerCamera = world.GetNode<Camera3D>(
			"Gameplay/Player/CameraRig/SpringArm3D/Camera3D");
		playerCamera.Current = false;
		Camera3D camera = new()
		{
			Name = "MainStreetDensityBenchmarkCamera",
			Current = true,
			Near = 0.05f,
			Far = 360.0f,
			Fov = 62.0f,
			Position = new Vector3(55.0f, 3.3f, 0.8f),
		};
		world.AddChild(camera);
		camera.LookAt(new Vector3(136.0f, 5.0f, 0.0f), Vector3.Up);
	}

	private static System.Collections.Generic.IEnumerable<Node> Enumerate(Node root)
	{
		yield return root;
		foreach (Node child in root.GetChildren())
		{
			foreach (Node descendant in Enumerate(child))
			{
				yield return descendant;
			}
		}
	}
}
