#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.UI;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

internal sealed class PerformanceBenchmarkSampler
{
	private readonly List<double> _frameTimes = new();
	private double _processSeconds;
	private double _physicsSeconds;
	private double _navigationSeconds;
	private double _drawCalls;
	private double _visibleObjects;
	private double _primitives;

	public int FrameCount => _frameTimes.Count;

	public void AddFrame(double frameSeconds)
	{
		if (frameSeconds <= 0.0)
		{
			return;
		}

		_frameTimes.Add(frameSeconds);
		_processSeconds += Performance.GetMonitor(Performance.Monitor.TimeProcess);
		_physicsSeconds += Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess);
		_navigationSeconds += Performance.GetMonitor(
			Performance.Monitor.TimeNavigationProcess);
		_drawCalls += Performance.GetMonitor(
			Performance.Monitor.RenderTotalDrawCallsInFrame);
		_visibleObjects += Performance.GetMonitor(
			Performance.Monitor.RenderTotalObjectsInFrame);
		_primitives += Performance.GetMonitor(
			Performance.Monitor.RenderTotalPrimitivesInFrame);
	}

	public string CreateReport(string label)
	{
		if (_frameTimes.Count == 0)
		{
			throw new InvalidOperationException("Benchmark captured no rendered frames.");
		}

		List<double> sorted = new(_frameTimes);
		sorted.Sort();
		double elapsedSeconds = _frameTimes.Sum();
		double averageFps = _frameTimes.Count / elapsedSeconds;
		double minimumFps = 1.0 / sorted[^1];
		double sampleCount = _frameTimes.Count;

		return
			$"{label}: frames={_frameTimes.Count}, elapsed={elapsedSeconds:F2}s, " +
			$"average_fps={averageFps:F2}, median_ms={Percentile(sorted, 0.50):F2}, " +
			$"p95_ms={Percentile(sorted, 0.95):F2}, " +
			$"p99_ms={Percentile(sorted, 0.99):F2}, min_fps={minimumFps:F2}, " +
			$"process_ms={(_processSeconds * 1000.0 / sampleCount):F3}, " +
			$"physics_ms={(_physicsSeconds * 1000.0 / sampleCount):F3}, " +
			$"navigation_ms={(_navigationSeconds * 1000.0 / sampleCount):F3}, " +
			$"draw_calls={(_drawCalls / sampleCount):F1}, " +
			$"visible_objects={(_visibleObjects / sampleCount):F1}, " +
			$"primitives={(_primitives / sampleCount):F1}";
	}

	private static double Percentile(IReadOnlyList<double> sorted, double percentile)
	{
		int index = Mathf.Clamp(
			Mathf.CeilToInt(sorted.Count * percentile) - 1,
			0,
			sorted.Count - 1);
		return sorted[index] * 1000.0;
	}
}

internal static class PerformanceBenchmarkDiagnostics
{
	public static void ConfigureRuntime()
	{
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		DisplayServer.WindowSetSize(new Vector2I(1280, 720));
		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
		Engine.MaxFps = 0;
	}

	public static void PrintRuntimeConfiguration(string label)
	{
		GD.Print(
			$"{label}_CONFIG: window={DisplayServer.WindowGetSize().X}x" +
			$"{DisplayServer.WindowGetSize().Y}, " +
			$"vsync={DisplayServer.WindowGetVsyncMode()}, max_fps={Engine.MaxFps}, " +
			$"physics_ticks={Engine.PhysicsTicksPerSecond}, " +
			$"refresh_hz={DisplayServer.ScreenGetRefreshRate():F2}, " +
			$"renderer={RenderingServer.GetVideoAdapterName()}, " +
			$"debug_build={OS.IsDebugBuild()}");
	}

	public static void PrintWorldAudit(string label, Node world)
	{
		List<Node> nodes = Enumerate(world).ToList();
		List<PrototypeZombie> zombies = nodes.OfType<PrototypeZombie>().ToList();
		List<NavigationAgent3D> navigationAgents =
			nodes.OfType<NavigationAgent3D>().ToList();
		List<AnimationPlayer> animationPlayers = nodes.OfType<AnimationPlayer>().ToList();
		List<Light3D> lights = nodes.OfType<Light3D>().ToList();
		List<Camera3D> cameras = nodes.OfType<Camera3D>().ToList();
		List<AudioStreamPlayer> audioPlayers = nodes.OfType<AudioStreamPlayer>().ToList();
		int corpseNodes = zombies.Count(zombie => zombie.HasNode("CorpseLoot"));
		int enabledCorpseInteractions = zombies.Count(zombie =>
			zombie.GetNode<Interactable>("CorpseLoot/Interactable").Enabled);
		WorldTime worldTime = world.GetNode<WorldTime>("WorldTime");
		PlayerFlashlight flashlight =
			world.GetNode<PlayerFlashlight>(
				"Player/CameraRig/SpringArm3D/Camera3D/Flashlight");

		GD.Print(
			$"{label}_WORLD: root={world.SceneFilePath}, nodes={nodes.Count}, " +
			$"zombies={zombies.Count}, alive_zombies={zombies.Count(z => z.IsAlive)}, " +
			$"navigation_agents={navigationAgents.Count}, " +
			$"avoidance_agents={navigationAgents.Count(agent => agent.AvoidanceEnabled)}, " +
			$"animation_players={animationPlayers.Count}, " +
			$"playing_animations={animationPlayers.Count(player => player.IsPlaying())}, " +
			$"corpse_nodes={corpseNodes}, " +
			$"enabled_corpse_interactions={enabledCorpseInteractions}, " +
			$"lights={lights.Count}, shadowed_lights={lights.Count(light => light.ShadowEnabled)}, " +
			$"cameras={cameras.Count}, current_cameras={cameras.Count(camera => camera.Current)}, " +
			$"canvas_layers={nodes.Count(node => node is CanvasLayer)}, " +
			$"controls={nodes.Count(node => node is Control)}, " +
			$"audio_players={audioPlayers.Count}, " +
			$"playing_audio={audioPlayers.Count(player => player.Playing)}, " +
			$"settings_autoload={world.GetNodeOrNull<Node>("/root/SettingsManager") is not null}, " +
			$"world_time_processing={worldTime.IsProcessing()}, " +
			$"flashlight={flashlight.IsEnabled}, " +
			$"objectives={nodes.Count(node => node is AntibioticsObjective or ServiceStationSuppliesObjective)}, " +
			$"save_managers={nodes.Count(node => node is SaveGameManager)}, " +
			$"notifications={nodes.Count(node => node is GameplayNotificationDisplay)}");
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
