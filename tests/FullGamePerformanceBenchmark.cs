#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class FullGamePerformanceBenchmark : Node
{
	private const double WarmupSeconds = 5.0;
	private const double SampleSeconds = 20.0;
	private static readonly Vector3 BenchmarkPlayerPosition = new(0.0f, 1.0f, 6.0f);
	private static readonly Vector3 BenchmarkCameraPitch = new(-0.2f, 0.0f, 0.0f);

	private ThirdPersonPlayer _player = null!;
	private Node3D _cameraRig = null!;
	private SpringArm3D _springArm = null!;
	private WorldTime _worldTime = null!;
	private PerformanceBenchmarkSampler? _sampler;
	private ulong _warmupStartTicks;
	private ulong _sampleStartTicks;
	private ulong _previousFrameTicks;
	private bool _isConfigured;
	private bool _isFinished;

	public override void _Ready()
	{
		try
		{
			ProcessMode = ProcessModeEnum.Always;
			PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration(
				"FULL_GAME_BENCHMARK_INHERITED");
			PerformanceBenchmarkDiagnostics.ConfigureRuntime();
			Node world = GetParent();
			_player = world.GetNode<ThirdPersonPlayer>("Player");
			_cameraRig = _player.GetNode<Node3D>("CameraRig");
			_springArm = _player.GetNode<SpringArm3D>("CameraRig/SpringArm3D");
			_worldTime = world.GetNode<WorldTime>("WorldTime");
			_worldTime.SetProcess(false);
			_worldTime.SetTimeOfDay(17.0f);
			PlayerFlashlight flashlight =
				_player.GetNode<PlayerFlashlight>(
					"CameraRig/SpringArm3D/Camera3D/Flashlight");
			if (flashlight.IsEnabled)
			{
				flashlight.Toggle();
			}
			FixBenchmarkTransform();
			_isConfigured = true;

			PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration("FULL_GAME_BENCHMARK");
			PerformanceBenchmarkDiagnostics.PrintWorldAudit("FULL_GAME_BENCHMARK", world);
			GD.Print(
				$"FULL_GAME_BENCHMARK_TIMING: warmup={WarmupSeconds:F1}s, " +
				$"sample={SampleSeconds:F1}s");
			_warmupStartTicks = Time.GetTicksUsec();
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	public override void _Process(double delta)
	{
		if (_isFinished)
		{
			return;
		}

		try
		{
			ulong currentTicks = Time.GetTicksUsec();
			if (_sampler is null)
			{
				if ((currentTicks - _warmupStartTicks) / 1_000_000.0 < WarmupSeconds)
				{
					return;
				}

				_worldTime.SetTimeOfDay(17.0f);
				_sampler = new PerformanceBenchmarkSampler();
				_sampleStartTicks = currentTicks;
				_previousFrameTicks = currentTicks;
				return;
			}

			_sampler.AddFrame((currentTicks - _previousFrameTicks) / 1_000_000.0);
			_previousFrameTicks = currentTicks;
			if ((currentTicks - _sampleStartTicks) / 1_000_000.0 < SampleSeconds)
			{
				return;
			}

			_isFinished = true;
			GD.Print(_sampler.CreateReport("FULL_GAME_BENCHMARK"));
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			Fail(exception);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isConfigured)
		{
			FixBenchmarkTransform();
		}
	}

	private void Fail(Exception exception)
	{
		_isFinished = true;
		GD.PushError($"FULL_GAME_BENCHMARK: FAIL - {exception.Message}");
		GetTree().Quit(1);
	}

	private void FixBenchmarkTransform()
	{
		_player.GlobalPosition = BenchmarkPlayerPosition;
		_player.GlobalRotation = Vector3.Zero;
		_player.Velocity = Vector3.Zero;
		_cameraRig.GlobalPosition = BenchmarkPlayerPosition + (Vector3.Up * 0.75f);
		_cameraRig.GlobalRotation = Vector3.Zero;
		_springArm.Rotation = BenchmarkCameraPitch;
	}
}
