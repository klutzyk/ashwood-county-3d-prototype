#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Moves the real gameplay player across a county chunk boundary while sampling
/// rendered frame times. This catches streaming stalls that a fixed camera cannot.
/// </summary>
public partial class CountyStreamingPerformanceBenchmark : Node
{
    private const double WarmupSeconds = 8.0;
    private const double SampleSeconds = 18.0;
    private const float TravelSpeed = 10.0f;
    private static readonly Vector2 Start = new(-600.0f, 600.0f);

    private ThirdPersonPlayer _player = null!;
    private WorldTime _worldTime = null!;
    private PerformanceBenchmarkSampler? _sampler;
    private ulong _warmupStart;
    private ulong _sampleStart;
    private ulong _previousFrame;
    private bool _finished;

    public override void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            PerformanceBenchmarkDiagnostics.ConfigureRuntime();
            Node world = GetParent();
            _player = world.GetNode<ThirdPersonPlayer>("Player");
            _worldTime = world.GetNode<WorldTime>("WorldTime");
            _worldTime.SetProcess(false);
            _worldTime.SetTimeOfDay(17.0f);
            PlacePlayer(Start.X);
            PerformanceBenchmarkDiagnostics.PrintRuntimeConfiguration(
                "COUNTY_STREAMING_BENCHMARK");
            GD.Print(
                $"COUNTY_STREAMING_BENCHMARK_PATH: start=({Start.X:F0},{Start.Y:F0}), " +
                $"speed={TravelSpeed:F1}m/s, chunk_size={CountyChunks.Size:F0}m");
            _warmupStart = Time.GetTicksUsec();
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    public override void _Process(double delta)
    {
        if (_finished)
        {
            return;
        }

        try
        {
            ulong now = Time.GetTicksUsec();
            if (_sampler is null)
            {
                if ((now - _warmupStart) / 1_000_000.0 < WarmupSeconds)
                {
                    return;
                }

                _sampler = new PerformanceBenchmarkSampler();
                _sampleStart = now;
                _previousFrame = now;
                return;
            }

            _sampler.AddFrame((now - _previousFrame) / 1_000_000.0);
            _previousFrame = now;
            if ((now - _sampleStart) / 1_000_000.0 < SampleSeconds)
            {
                return;
            }

            _finished = true;
            GD.Print(_sampler.CreateReport("COUNTY_STREAMING_BENCHMARK"));
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_sampler is null)
        {
            PlacePlayer(Start.X);
            return;
        }

        double elapsed = (Time.GetTicksUsec() - _sampleStart) / 1_000_000.0;
        PlacePlayer(Start.X + (float)elapsed * TravelSpeed);
    }

    private void PlacePlayer(float x)
    {
        float y = CountyMap.Height(x, Start.Y) + 1.2f;
        _player.GlobalPosition = new Vector3(x, y, Start.Y);
        _player.Velocity = Vector3.Zero;
    }

    private void Fail(Exception exception)
    {
        _finished = true;
        GD.PushError($"COUNTY_STREAMING_BENCHMARK: FAIL - {exception}");
        GetTree().Quit(1);
    }
}
