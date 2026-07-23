#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class RenderedPerformanceBenchmark : Node
{
	private const int WarmupFrames = 180;
	private const int SampleFrames = 600;
	private readonly List<double> _frameTimes = new(SampleFrames);

	public override async void _Ready()
	{
		try
		{
			DisplayServer.WindowSetSize(new Vector2I(1280, 720));
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
			Engine.MaxFps = 0;

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

			for (int frame = 0; frame < WarmupFrames; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			ulong previousTicks = Time.GetTicksUsec();
			for (int frame = 0; frame < SampleFrames; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				ulong currentTicks = Time.GetTicksUsec();
				_frameTimes.Add((currentTicks - previousTicks) / 1_000_000.0);
				previousTicks = currentTicks;
			}

			_frameTimes.Sort();
			double totalSeconds = 0.0;
			foreach (double frameTime in _frameTimes)
			{
				totalSeconds += frameTime;
			}

			double averageFps = SampleFrames / totalSeconds;
			int p95Index = Mathf.Clamp(Mathf.CeilToInt(SampleFrames * 0.95f) - 1, 0, SampleFrames - 1);
			double p95Milliseconds = _frameTimes[p95Index] * 1000.0;
			GD.Print(
				$"RENDERED_BENCHMARK: 1280x720, 17:00, 600 frames, " +
				$"average {averageFps:F2} FPS, p95 {p95Milliseconds:F2} ms");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"RENDERED_BENCHMARK: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}
}
