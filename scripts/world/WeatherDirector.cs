#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Transitions between authored county weather profiles and keeps local rain
/// centred on the player. The director is deterministic when RandomSeed is set,
/// which makes both save integration and automated visual review practical.
/// </summary>
public partial class WeatherDirector : Node3D
{
	[Signal]
	public delegate void WeatherChangedEventHandler(string displayName);
	[Signal]
	public delegate void LightningFlashedEventHandler();

	[ExportGroup("Scene References")]
	[Export] public NodePath WorldTimePath { get; set; } = new("../WorldTime");
	[Export] public NodePath WorldEnvironmentPath { get; set; } = new("../../WorldEnvironment");
	[Export] public NodePath PlayerPath { get; set; } = new("../Player");
	[Export] public NodePath PrecipitationPath { get; set; } = new("Precipitation");
	[Export] public NodePath WeatherAudioPath { get; set; } = new("WeatherAudio");

	[ExportGroup("Weather Schedule")]
	[Export] public Godot.Collections.Array<WeatherProfile> Profiles { get; set; } = new();
	[Export(PropertyHint.Range, "0,32,1")]
	public int StartingWeatherIndex { get; set; }
	[Export] public bool AutoCycle { get; set; } = true;
	[Export(PropertyHint.Range, "30,7200,1,or_greater")]
	public float MinimumWeatherDurationSeconds { get; set; } = 420.0f;
	[Export(PropertyHint.Range, "30,7200,1,or_greater")]
	public float MaximumWeatherDurationSeconds { get; set; } = 900.0f;
	[Export(PropertyHint.Range, "0.1,180,0.1,or_greater")]
	public float TransitionDurationSeconds { get; set; } = 32.0f;
	[Export] public int RandomSeed { get; set; }

	[ExportGroup("Presentation")]
	[Export(PropertyHint.Range, "-60,0,0.5")]
	public float RainVolumeDb { get; set; } = -18.0f;
	[Export(PropertyHint.Range, "0,20,0.25")]
	public float PrecipitationHeight { get; set; } = 8.5f;
	[Export(PropertyHint.Range, "0.02,0.5,0.01")]
	public float LightningFlashDuration { get; set; } = 0.14f;

	public WeatherProfile? CurrentProfile => Profiles.Count == 0
		? null
		: Profiles[Mathf.Clamp(_targetIndex, 0, Profiles.Count - 1)];
	public int CurrentWeatherIndex => _targetIndex;
	public float TransitionProgress { get; private set; } = 1.0f;
	public float PrecipitationIntensity => _current.PrecipitationIntensity;
	public float WindIntensity => _current.WindIntensity;
	public float SecondsUntilWeatherChange => _secondsUntilChange;
	public ulong ScheduleRandomState => _scheduleRandom.State;
	public float SecondsUntilLightning => float.IsPositiveInfinity(_secondsUntilLightning)
		? -1.0f
		: Mathf.Max(_secondsUntilLightning, 0.0f);
	public ulong LightningRandomState => _lightningRandom.State;

	private const float MixRate = 22050.0f;
	private const float SilentVolumeDb = -80.0f;

	private readonly RandomNumberGenerator _scheduleRandom = new();
	private readonly RandomNumberGenerator _lightningRandom = new();
	private readonly RandomNumberGenerator _audioRandom = new();
	private WorldTime _worldTime = null!;
	private Godot.Environment _environment = null!;
	private ProceduralSkyMaterial? _skyMaterial;
	private Node3D? _player;
	private GpuParticles3D? _precipitation;
	private AudioStreamPlayer? _weatherAudio;
	private AudioStreamGeneratorPlayback? _weatherPlayback;
	private WeatherSnapshot _from;
	private WeatherSnapshot _to;
	private WeatherSnapshot _current;
	private int _targetIndex;
	private float _transitionElapsed;
	private float _secondsUntilChange;
	private float _secondsUntilLightning = float.PositiveInfinity;
	private float _lightningRemaining;
	private float _rainNoise;

	public override void _Ready()
	{
		_worldTime = GetNode<WorldTime>(WorldTimePath);
		WorldEnvironment worldEnvironment = GetNode<WorldEnvironment>(WorldEnvironmentPath);
		_environment = worldEnvironment.Environment
			?? throw new InvalidOperationException("Dynamic weather requires an Environment resource.");
		_skyMaterial = _environment.Sky?.SkyMaterial as ProceduralSkyMaterial;
		_player = GetNodeOrNull<Node3D>(PlayerPath);
		_precipitation = GetNodeOrNull<GpuParticles3D>(PrecipitationPath);
		_weatherAudio = GetNodeOrNull<AudioStreamPlayer>(WeatherAudioPath);
		if (_weatherAudio is not null)
		{
			_weatherAudio.Bus = "Ambient";
		}

		if (RandomSeed == 0)
		{
			_scheduleRandom.Randomize();
			_lightningRandom.Randomize();
			_audioRandom.Randomize();
		}
		else
		{
			ulong baseSeed = unchecked((ulong)(uint)RandomSeed);
			_scheduleRandom.Seed = MixSeed(baseSeed, 0xA0761D6478BD642FUL);
			_lightningRandom.Seed = MixSeed(baseSeed, 0xE7037ED1A0B428DBUL);
			_audioRandom.Seed = MixSeed(baseSeed, 0x8EBC6AF09C88C6E3UL);
		}

		if (Profiles.Count == 0)
		{
			GD.PushWarning("WeatherDirector has no weather profiles and will remain inactive.");
			SetProcess(false);
			return;
		}

		SetWeather(Mathf.Clamp(StartingWeatherIndex, 0, Profiles.Count - 1), immediate: true);
		ResetWeatherTimer();
	}

	public override void _Process(double delta)
	{
		float frameDelta = Mathf.Max((float)delta, 0.0f);
		UpdateSchedule(frameDelta);
		bool transitionChanged = UpdateTransition(frameDelta);
		bool lightningChanged = UpdateLightning(frameDelta);
		if (transitionChanged || lightningChanged)
		{
			ApplyCurrentWeather();
		}
		FollowPlayer();
		FillWeatherAudio();
	}

	public override void _ExitTree()
	{
		if (_weatherAudio is not null)
		{
			_weatherAudio.Stop();
			_weatherAudio.Stream = null;
		}
		_weatherPlayback = null;
	}

	public void SetWeather(int profileIndex, bool immediate = false)
	{
		if (profileIndex < 0 || profileIndex >= Profiles.Count)
		{
			throw new ArgumentOutOfRangeException(
				nameof(profileIndex),
				profileIndex,
				"Weather profile index is outside the configured profile list.");
		}

		WeatherProfile profile = Profiles[profileIndex]
			?? throw new InvalidOperationException($"Weather profile {profileIndex} is null.");
		_targetIndex = profileIndex;
		_to = WeatherSnapshot.FromProfile(profile);
		_transitionElapsed = immediate ? TransitionDurationSeconds : 0.0f;
		TransitionProgress = immediate ? 1.0f : 0.0f;
		if (immediate)
		{
			_from = _to;
			_current = _to;
			ApplyCurrentWeather();
		}
		else
		{
			_from = _current;
		}

		ScheduleLightning(forceReset: true);
		EmitSignal(SignalName.WeatherChanged, profile.DisplayName);
	}

	public bool SetWeatherByKind(WeatherKind kind, bool immediate = false)
	{
		for (int index = 0; index < Profiles.Count; index++)
		{
			if (Profiles[index]?.Kind != kind)
			{
				continue;
			}

			SetWeather(index, immediate);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Restores the durable part of a weather save without replaying a long
	/// transition. A transient lightning flash is intentionally not persisted.
	/// </summary>
	public bool RestoreWeatherState(
		WeatherKind kind,
		float secondsUntilChange,
		ulong scheduleRandomState = 0,
		float secondsUntilLightning = -1.0f,
		ulong lightningRandomState = 0)
	{
		if (!SetWeatherByKind(kind, immediate: true))
		{
			return false;
		}

		_secondsUntilChange = Mathf.Max(secondsUntilChange, 0.0f);
		if (scheduleRandomState != 0)
		{
			_scheduleRandom.State = scheduleRandomState;
		}
		if (lightningRandomState != 0)
		{
			_lightningRandom.State = lightningRandomState;
		}
		if (_to.LightningPerMinute <= 0.01f)
		{
			_secondsUntilLightning = float.PositiveInfinity;
		}
		else if (secondsUntilLightning >= 0.0f)
		{
			_secondsUntilLightning = secondsUntilLightning;
		}
		return true;
	}

	private void UpdateSchedule(float delta)
	{
		if (!AutoCycle || Profiles.Count <= 1)
		{
			return;
		}

		_secondsUntilChange -= delta;
		if (_secondsUntilChange > 0.0f)
		{
			return;
		}

		int offset = _scheduleRandom.RandiRange(1, Profiles.Count - 1);
		int nextIndex = (_targetIndex + offset) % Profiles.Count;
		SetWeather(nextIndex);
		ResetWeatherTimer();
	}

	private bool UpdateTransition(float delta)
	{
		if (TransitionProgress >= 1.0f)
		{
			_current = _to;
			return false;
		}

		_transitionElapsed += delta;
		float duration = Mathf.Max(TransitionDurationSeconds, 0.001f);
		TransitionProgress = Mathf.Clamp(_transitionElapsed / duration, 0.0f, 1.0f);
		float easedProgress = TransitionProgress * TransitionProgress *
			(3.0f - (2.0f * TransitionProgress));
		_current = WeatherSnapshot.Lerp(_from, _to, easedProgress);
		return true;
	}

	private bool UpdateLightning(float delta)
	{
		float previousRemaining = _lightningRemaining;
		_lightningRemaining = Mathf.Max(_lightningRemaining - delta, 0.0f);
		if (_current.LightningPerMinute <= 0.01f)
		{
			// Preserve the target storm's scheduled strike while its intensity is
			// still blending up from a non-lightning profile.
			if (_to.LightningPerMinute <= 0.01f)
			{
				_secondsUntilLightning = float.PositiveInfinity;
			}
			return !Mathf.IsEqualApprox(previousRemaining, _lightningRemaining);
		}

		_secondsUntilLightning -= delta;
		if (_secondsUntilLightning > 0.0f)
		{
			return !Mathf.IsEqualApprox(previousRemaining, _lightningRemaining);
		}

		_lightningRemaining = Mathf.Max(LightningFlashDuration, 0.02f);
		ScheduleLightning(forceReset: true);
		EmitSignal(SignalName.LightningFlashed);
		return true;
	}

	private void ApplyCurrentWeather()
	{
		float flashRatio = LightningFlashDuration <= 0.0f
			? 0.0f
			: Mathf.Clamp(_lightningRemaining / LightningFlashDuration, 0.0f, 1.0f);
		float flash = Mathf.Sin(flashRatio * Mathf.Pi) * _current.LightningBrightness;
		_worldTime.SetWeatherInfluence(
			_current.AmbientMultiplier + (flash * 0.36f),
			_current.SkyEnergyMultiplier + (flash * 0.24f),
			_current.DirectionalMultiplier + (flash * 0.55f),
			_current.DirectionalTint.Lerp(new Color(0.72f, 0.82f, 1.0f), flashRatio));

		_environment.FogEnabled = _current.FogDensity > 0.0001f;
		_environment.FogLightColor = _current.FogColor;
		_environment.FogDensity = _current.FogDensity;
		_environment.FogDepthBegin = _current.FogDepthBegin;
		_environment.FogDepthEnd = Mathf.Max(
			_current.FogDepthEnd,
			_current.FogDepthBegin + 1.0f);
		_environment.FogSkyAffect = _current.FogSkyAffect;
		_environment.FogAerialPerspective = _current.FogAerialPerspective;
		_environment.TonemapExposure = _current.Exposure + (flash * 0.08f);
		_environment.AdjustmentEnabled = true;
		_environment.AdjustmentSaturation = _current.Saturation;

		if (_skyMaterial is not null)
		{
			_skyMaterial.SkyTopColor = _current.SkyTopColor;
			_skyMaterial.SkyHorizonColor = _current.SkyHorizonColor;
			_skyMaterial.SkyCoverModulate = new Color(
				_current.CloudColor.R,
				_current.CloudColor.G,
				_current.CloudColor.B,
				_current.CloudOpacity);
		}

		if (_precipitation is not null)
		{
			float rain = Mathf.Clamp(_current.PrecipitationIntensity, 0.0f, 1.0f);
			_precipitation.AmountRatio = rain;
			_precipitation.Emitting = rain > 0.01f;
		}

		if (_weatherAudio is not null)
		{
			float audibleRain = Mathf.Clamp(_current.PrecipitationIntensity, 0.0f, 1.0f);
			UpdateWeatherAudio(audibleRain);
		}
	}

	private void FollowPlayer()
	{
		if (_player is null || _precipitation is null || !_precipitation.Emitting)
		{
			return;
		}

		Vector3 target = _player.GlobalPosition + (Vector3.Up * PrecipitationHeight);
		_precipitation.GlobalPosition = target;
	}

	private void ResetWeatherTimer()
	{
		float minimum = Mathf.Max(MinimumWeatherDurationSeconds, 1.0f);
		float maximum = Mathf.Max(MaximumWeatherDurationSeconds, minimum);
		_secondsUntilChange = _scheduleRandom.RandfRange(minimum, maximum);
	}

	private void ScheduleLightning(bool forceReset)
	{
		float strikesPerMinute = _to.LightningPerMinute;
		if (strikesPerMinute <= 0.01f)
		{
			_secondsUntilLightning = float.PositiveInfinity;
			return;
		}

		if (!forceReset && !float.IsPositiveInfinity(_secondsUntilLightning))
		{
			return;
		}

		float meanInterval = 60.0f / strikesPerMinute;
		_secondsUntilLightning = meanInterval *
			_lightningRandom.RandfRange(0.55f, 1.6f);
	}

	private void UpdateWeatherAudio(float audibleRain)
	{
		if (_weatherAudio is null)
		{
			return;
		}

		if (audibleRain <= 0.001f)
		{
			_weatherAudio.VolumeDb = SilentVolumeDb;
			if (_weatherAudio.Playing)
			{
				_weatherAudio.Stop();
			}
			_weatherPlayback = null;
			return;
		}

		if (!_weatherAudio.Playing || _weatherPlayback is null)
		{
			StartWeatherAudio();
		}
		_weatherAudio.VolumeDb =
			RainVolumeDb + Mathf.LinearToDb(Mathf.Sqrt(audibleRain));
	}

	private void StartWeatherAudio()
	{
		if (_weatherAudio is null)
		{
			return;
		}

		_weatherAudio.Stream = new AudioStreamGenerator
		{
			MixRate = MixRate,
			BufferLength = 0.25f,
		};
		_weatherAudio.VolumeDb = SilentVolumeDb;
		_weatherAudio.Play();
		_weatherPlayback = _weatherAudio.GetStreamPlayback() as AudioStreamGeneratorPlayback;
	}

	private void FillWeatherAudio()
	{
		float rain = Mathf.Clamp(_current.PrecipitationIntensity, 0.0f, 1.0f);
		if (_weatherPlayback is null || rain <= 0.001f)
		{
			return;
		}

		int framesAvailable = _weatherPlayback.GetFramesAvailable();
		float wind = Mathf.Clamp(_current.WindIntensity, 0.0f, 1.0f);
		for (int frame = 0; frame < framesAvailable; frame++)
		{
			float whiteNoise = _audioRandom.RandfRange(-1.0f, 1.0f);
			_rainNoise = Mathf.Lerp(_rainNoise, whiteNoise, 0.075f + (wind * 0.08f));
			float hiss = (whiteNoise * 0.34f) + (_rainNoise * 0.66f);
			float sample = hiss * rain * (0.26f + (wind * 0.12f));
			_weatherPlayback.PushFrame(new Vector2(sample, sample));
		}
	}

	private static ulong MixSeed(ulong seed, ulong salt)
	{
		// SplitMix64 finalizer gives each deterministic subsystem an independent
		// sequence while retaining a single designer-facing RandomSeed.
		ulong mixed = unchecked(seed + salt + 0x9E3779B97F4A7C15UL);
		mixed = unchecked((mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL);
		mixed = unchecked((mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL);
		return mixed ^ (mixed >> 31);
	}

	private readonly record struct WeatherSnapshot(
		float AmbientMultiplier,
		float SkyEnergyMultiplier,
		float DirectionalMultiplier,
		Color DirectionalTint,
		float Exposure,
		float Saturation,
		Color SkyTopColor,
		Color SkyHorizonColor,
		Color CloudColor,
		float CloudOpacity,
		Color FogColor,
		float FogDensity,
		float FogDepthBegin,
		float FogDepthEnd,
		float FogSkyAffect,
		float FogAerialPerspective,
		float PrecipitationIntensity,
		float WindIntensity,
		float LightningPerMinute,
		float LightningBrightness)
	{
		public static WeatherSnapshot FromProfile(WeatherProfile profile)
		{
			return new WeatherSnapshot(
				profile.AmbientMultiplier,
				profile.SkyEnergyMultiplier,
				profile.DirectionalMultiplier,
				profile.DirectionalTint,
				profile.Exposure,
				profile.Saturation,
				profile.SkyTopColor,
				profile.SkyHorizonColor,
				profile.CloudColor,
				profile.CloudOpacity,
				profile.FogColor,
				profile.FogDensity,
				profile.FogDepthBegin,
				profile.FogDepthEnd,
				profile.FogSkyAffect,
				profile.FogAerialPerspective,
				profile.PrecipitationIntensity,
				profile.WindIntensity,
				profile.LightningPerMinute,
				profile.LightningBrightness);
		}

		public static WeatherSnapshot Lerp(
			WeatherSnapshot from,
			WeatherSnapshot to,
			float weight)
		{
			float blend = Mathf.Clamp(weight, 0.0f, 1.0f);
			return new WeatherSnapshot(
				Mathf.Lerp(from.AmbientMultiplier, to.AmbientMultiplier, blend),
				Mathf.Lerp(from.SkyEnergyMultiplier, to.SkyEnergyMultiplier, blend),
				Mathf.Lerp(from.DirectionalMultiplier, to.DirectionalMultiplier, blend),
				from.DirectionalTint.Lerp(to.DirectionalTint, blend),
				Mathf.Lerp(from.Exposure, to.Exposure, blend),
				Mathf.Lerp(from.Saturation, to.Saturation, blend),
				from.SkyTopColor.Lerp(to.SkyTopColor, blend),
				from.SkyHorizonColor.Lerp(to.SkyHorizonColor, blend),
				from.CloudColor.Lerp(to.CloudColor, blend),
				Mathf.Lerp(from.CloudOpacity, to.CloudOpacity, blend),
				from.FogColor.Lerp(to.FogColor, blend),
				Mathf.Lerp(from.FogDensity, to.FogDensity, blend),
				Mathf.Lerp(from.FogDepthBegin, to.FogDepthBegin, blend),
				Mathf.Lerp(from.FogDepthEnd, to.FogDepthEnd, blend),
				Mathf.Lerp(from.FogSkyAffect, to.FogSkyAffect, blend),
				Mathf.Lerp(from.FogAerialPerspective, to.FogAerialPerspective, blend),
				Mathf.Lerp(from.PrecipitationIntensity, to.PrecipitationIntensity, blend),
				Mathf.Lerp(from.WindIntensity, to.WindIntensity, blend),
				Mathf.Lerp(from.LightningPerMinute, to.LightningPerMinute, blend),
				Mathf.Lerp(from.LightningBrightness, to.LightningBrightness, blend));
		}
	}
}
