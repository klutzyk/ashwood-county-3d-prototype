#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.World;

namespace AshwoodCounty3DPrototype.Tests;

public partial class WeatherDirectorValidation : Node
{
	public override async void _Ready()
	{
		Exception? failure = null;
		Node3D root = null!;
		DirectionalLight3D sun = null!;
		ProceduralSkyMaterial skyMaterial = null!;
		Sky sky = null!;
		Godot.Environment environment = null!;
		WorldEnvironment worldEnvironment = null!;
		Node gameplay = null!;
		WorldTime worldTime = null!;
		Node3D player = null!;
		WeatherDirector weather = null!;
		AudioStreamPlayer weatherAudio = null!;
		GpuParticles3D precipitation = null!;
		try
		{
			root = new Node3D { Name = "WeatherTestWorld" };
			sun = new DirectionalLight3D
			{
				Name = "DirectionalLight3D",
				LightEnergy = 1.0f,
			};
			skyMaterial = new ProceduralSkyMaterial();
			sky = new Sky { SkyMaterial = skyMaterial };
			environment = new Godot.Environment
			{
				BackgroundMode = Godot.Environment.BGMode.Sky,
				Sky = sky,
				AmbientLightEnergy = 1.0f,
				BackgroundEnergyMultiplier = 1.0f,
			};
			worldEnvironment = new WorldEnvironment
			{
				Name = "WorldEnvironment",
				Environment = environment,
			};
			gameplay = new Node { Name = "Gameplay" };
			worldTime = new WorldTime
			{
				Name = "WorldTime",
				StartingHour = 16.75f,
				FullDayDurationSeconds = 14400.0f,
				DirectionalLightPath = new NodePath("../../DirectionalLight3D"),
				WorldEnvironmentPath = new NodePath("../../WorldEnvironment"),
			};
			player = new Node3D
			{
				Name = "Player",
				Position = new Vector3(3.0f, 1.0f, 4.0f),
			};
			weather = GD.Load<PackedScene>(
				"res://scenes/environment/dynamic_weather.tscn").Instantiate<WeatherDirector>();
			weather.AutoCycle = false;
			weather.RandomSeed = 5150;
			weather.TransitionDurationSeconds = 0.12f;

			root.AddChild(sun);
			root.AddChild(worldEnvironment);
			root.AddChild(gameplay);
			gameplay.AddChild(worldTime);
			gameplay.AddChild(player);
			gameplay.AddChild(weather);
			AddChild(root);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Require(weather.Profiles.Count == 5, "county weather scene exposes five authored conditions");
			Require(weather.CurrentProfile?.Kind == WeatherKind.Clear,
				"weather begins with the authored broken-sunshine profile");
			Require(weather.GetNode<GpuParticles3D>("Precipitation").Emitting == false,
				"clear weather does not spend fill rate on rain");
			weatherAudio = weather.GetNode<AudioStreamPlayer>("WeatherAudio");
			Require(weatherAudio.Bus == "Ambient" && !weatherAudio.Playing &&
				weatherAudio.Stream is null,
				"clear weather stays on the Ambient bus without running a silent generator");
			float clearSunEnergy = sun.LightEnergy;
			float clearFogDensity = environment.FogDensity;

			Require(weather.SetWeatherByKind(WeatherKind.Rain), "rain profile is addressable by kind");
			ulong scheduleStateBeforeRainAudio = weather.ScheduleRandomState;
			await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
			precipitation = weather.GetNode<GpuParticles3D>("Precipitation");
			Require(weather.TransitionProgress >= 0.99f, "weather transitions finish smoothly");
			Require(weather.PrecipitationIntensity >= 0.75f && precipitation.Emitting,
				"rain enables the local high-density precipitation field");
			Require(environment.FogDensity > clearFogDensity,
				"rain closes distant visibility without hiding the immediate combat space");
			Require(sun.LightEnergy < clearSunEnergy && worldTime.WeatherDirectionalMultiplier < 0.4f,
				"rain softens direct light through the shared day-night curve");
			Require(skyMaterial.SkyCoverModulate.A > 0.9f,
				"rain transitions the existing cloud cover instead of swapping the sky abruptly");
			Require(precipitation.GlobalPosition.DistanceTo(
				player.GlobalPosition + (Vector3.Up * weather.PrecipitationHeight)) < 0.05f,
				"precipitation follows the player at a bounded local cost");
			Require(weatherAudio.Playing && weatherAudio.Stream is AudioStreamGenerator,
				"rain starts procedural ambience only while precipitation is audible");
			Require(weather.ScheduleRandomState == scheduleStateBeforeRainAudio,
				"procedural rain noise cannot perturb the deterministic weather schedule");

			Require(weather.SetWeatherByKind(WeatherKind.Fog, immediate: true),
				"fog profile is addressable by kind");
			Require(environment.FogDepthBegin <= 12.0f && environment.FogDepthEnd <= 120.0f,
				"Blackwater fog creates a distinct close-range navigation condition");
			Require(weather.PrecipitationIntensity <= 0.06f,
				"fog does not masquerade as another full rain state");

			Require(weather.SetWeatherByKind(WeatherKind.Storm, immediate: true),
				"storm profile is addressable by kind");
			Require(weather.CurrentProfile?.LightningPerMinute >= 2.0f &&
				weather.WindIntensity >= 0.95f,
				"storm carries lightning and maximum wind metadata for presentation/gameplay hooks");
			float savedLightningTimer = weather.SecondsUntilLightning;
			ulong savedLightningState = weather.LightningRandomState;
			Require(savedLightningTimer > 0.0f,
				"storm schedules its next strike on the deterministic lightning stream");

			Require(weather.SetWeatherByKind(WeatherKind.Clear, immediate: true),
				"clear profile can be restored deterministically");
			Require(!precipitation.Emitting && Mathf.IsZeroApprox(precipitation.AmountRatio),
				"returning to clear weather releases the rain effect cleanly");
			Require(!weatherAudio.Playing,
				"returning to clear weather stops rather than continuously filling silent audio");

			ulong savedScheduleState = weather.ScheduleRandomState;
			Require(weather.RestoreWeatherState(
				WeatherKind.Storm,
				123.5f,
				savedScheduleState,
				savedLightningTimer,
				savedLightningState) &&
				weather.CurrentProfile?.Kind == WeatherKind.Storm &&
				Mathf.IsEqualApprox(weather.SecondsUntilWeatherChange, 123.5f) &&
				weather.ScheduleRandomState == savedScheduleState &&
				Mathf.IsEqualApprox(weather.SecondsUntilLightning, savedLightningTimer) &&
				weather.LightningRandomState == savedLightningState,
				"durable weather, schedule and lightning state restore without replaying a transition");

		}
		catch (Exception exception)
		{
			failure = exception;
		}

		try
		{
			if (weather is not null && IsInstanceValid(weather))
			{
				weather.SetProcess(false);
			}
			if (weatherAudio is not null && IsInstanceValid(weatherAudio))
			{
				weatherAudio.Stop();
				weatherAudio.Stream = null;
			}
			if (worldEnvironment is not null && IsInstanceValid(worldEnvironment))
			{
				worldEnvironment.Environment = null;
			}
			if (environment is not null)
			{
				environment.Sky = null;
			}
			if (sky is not null)
			{
				sky.SkyMaterial = null;
			}
			if (root is not null && IsInstanceValid(root))
			{
				root.QueueFree();
			}

			precipitation = null!;
			weatherAudio = null!;
			weather = null!;
			player = null!;
			worldTime = null!;
			gameplay = null!;
			worldEnvironment = null!;
			environment = null!;
			sky = null!;
			skyMaterial = null!;
			sun = null!;
			root = null!;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}
		catch (Exception cleanupException)
		{
			failure ??= cleanupException;
		}

		if (failure is null)
		{
			GD.Print("WEATHER_DIRECTOR_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		else
		{
			GD.PushError($"WEATHER_DIRECTOR_VALIDATION: FAIL - {failure.Message}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
