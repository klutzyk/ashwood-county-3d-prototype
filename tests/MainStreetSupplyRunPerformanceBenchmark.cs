#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainStreetSupplyRunPerformanceBenchmark : Node
{
	private const int WarmupFrames = 180;
	private const int SampleFrames = 600;

	public override async void _Ready()
	{
		try
		{
			int warmupFrames = ReadFrameCount(
				"ASHWOOD_BENCH_WARMUP_FRAMES", WarmupFrames);
			int sampleFrames = ReadFrameCount(
				"ASHWOOD_BENCH_SAMPLE_FRAMES", SampleFrames);
			PerformanceBenchmarkDiagnostics.ConfigureRuntime();

			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			DisableRequestedLayers(world);

			PerformanceBenchmarkDiagnostics.ConfigureRuntime();
			WorldTime worldTime =
				world.GetNode<WorldTime>("Gameplay/WorldTime");
			worldTime.SetTimeOfDay(16.75f);
			worldTime.SetProcess(false);

			Camera3D playerCamera = world.GetNode<Camera3D>(
				"Gameplay/Player/CameraRig/SpringArm3D/Camera3D");
			playerCamera.Current = false;

			Camera3D camera = new()
			{
				Name = "SupplyRunBenchmarkCamera",
				Current = true,
				Near = 0.05f,
				Far = 260.0f,
				Fov = 55.0f,
				Position = new Vector3(-79.0f, 2.0f, -1.0f),
			};
			world.AddChild(camera);
			camera.LookAt(new Vector3(-59.7f, 2.0f, -9.0f), Vector3.Up);

			PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration(
				"MAIN_STREET_SUPPLY_RUN_BENCHMARK");
			GD.Print(
				"MAIN_STREET_SUPPLY_RUN_BENCHMARK_VIEW: " +
				"1280x720 live bakery approach with player, five zombies and HUD");
			PrintGeometryBreakdown(world);

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

			GD.Print(sampler.CreateReport(
				"MAIN_STREET_SUPPLY_RUN_BENCHMARK"));
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"MAIN_STREET_SUPPLY_RUN_BENCHMARK: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static int ReadFrameCount(string variableName, int fallback)
	{
		return int.TryParse(OS.GetEnvironment(variableName), out int frameCount) &&
			frameCount > 0
			? frameCount
			: fallback;
	}

	private static void DisableRequestedLayers(Node3D world)
	{
		string requestedLayers = OS.GetEnvironment("ASHWOOD_BENCH_DISABLE");
		if (string.IsNullOrWhiteSpace(requestedLayers))
		{
			return;
		}

		foreach (string layerPath in requestedLayers.Split(
				',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			Node3D? layer = world.GetNodeOrNull<Node3D>(layerPath);
			if (layer is null)
			{
				GD.PushWarning(
					$"MAIN_STREET_SUPPLY_RUN_BENCHMARK: unknown layer '{layerPath}'.");
				continue;
			}

			layer.Visible = false;
			layer.ProcessMode = ProcessModeEnum.Disabled;
			GD.Print(
				$"MAIN_STREET_SUPPLY_RUN_BENCHMARK_DISABLED: {layerPath}");
		}
	}

	private static void PrintGeometryBreakdown(Node3D world)
	{
		IEnumerable<Node> layers = world.GetChildren().Cast<Node>();
		Node? environment = world.GetNodeOrNull<Node>("Environment");
		if (environment is not null)
		{
			layers = layers.Concat(environment.GetChildren().Cast<Node>());
		}
		Node? presentation = world.GetNodeOrNull<Node>("Environment/Presentation");
		if (presentation is not null)
		{
			layers = layers.Concat(presentation.GetChildren().Cast<Node>());
		}
		Node? storefronts = world.GetNodeOrNull<Node>(
			"Environment/Presentation/Storefronts");
		if (storefronts is not null)
		{
			layers = layers.Concat(storefronts.GetChildren().Cast<Node>());
		}

		foreach (Node layer in layers.Distinct())
		{
			List<MeshInstance3D> meshes = Enumerate(layer)
				.OfType<MeshInstance3D>()
				.Where(mesh => mesh.IsVisibleInTree() && mesh.Mesh is not null)
				.ToList();
			int surfaces = meshes.Sum(mesh => mesh.Mesh?.GetSurfaceCount() ?? 0);
			int multiMeshes = Enumerate(layer)
				.Count(node => node is MultiMeshInstance3D instance && instance.IsVisibleInTree());
			GD.Print(
				$"MAIN_STREET_SUPPLY_RUN_GEOMETRY: layer={layer.GetPath()}, " +
				$"meshes={meshes.Count}, surfaces={surfaces}, multimeshes={multiMeshes}");
		}
	}

	private static IEnumerable<Node> Enumerate(Node root)
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
