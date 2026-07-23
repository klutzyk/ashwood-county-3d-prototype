#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.World;

public partial class AtmosphereAudio : Node
{
	[Export] public NodePath WorldTimePath { get; set; } = new("../WorldTime");
	[Export(PropertyHint.Range, "-80,6,0.5")] public float WindVolumeDb { get; set; } = -24.0f;
	[Export(PropertyHint.Range, "-80,6,0.5")] public float ZombieGroanVolumeDb { get; set; } = -28.0f;
	[Export(PropertyHint.Range, "-80,6,0.5")] public float DayInsectsVolumeDb { get; set; } = -31.0f;
	[Export(PropertyHint.Range, "-80,6,0.5")] public float NightCricketsVolumeDb { get; set; } = -27.0f;
	[Export] public float MinimumGroanInterval { get; set; } = 9.0f;
	[Export] public float MaximumGroanInterval { get; set; } = 22.0f;

	private const float MixRate = 22050.0f;
	private const float SilentVolumeDb = -80.0f;

	private readonly RandomNumberGenerator _random = new();
	private WorldTime _worldTime = null!;
	private AudioStreamPlayer _windPlayer = null!;
	private AudioStreamPlayer _groanPlayer = null!;
	private AudioStreamPlayer _insectsPlayer = null!;
	private AudioStreamPlayer _cricketsPlayer = null!;
	private AudioStreamGeneratorPlayback _windPlayback = null!;
	private AudioStreamGeneratorPlayback _groanPlayback = null!;
	private AudioStreamGeneratorPlayback _insectsPlayback = null!;
	private AudioStreamGeneratorPlayback _cricketsPlayback = null!;
	private float _windTime;
	private float _insectTime;
	private float _cricketTime;
	private float _windNoise;
	private float _groanWaitRemaining;
	private float _groanElapsed;
	private float _groanDuration;
	private float _groanPitch;

	public float DaylightBlend { get; private set; }

	public override void _Ready()
	{
		_worldTime = GetNode<WorldTime>(WorldTimePath);
		_windPlayer = GetNode<AudioStreamPlayer>("Wind");
		_groanPlayer = GetNode<AudioStreamPlayer>("ZombieGroans");
		_insectsPlayer = GetNode<AudioStreamPlayer>("DayInsects");
		_cricketsPlayer = GetNode<AudioStreamPlayer>("NightCrickets");
		_random.Randomize();
		_groanWaitRemaining = RandomGroanInterval();

		_windPlayback = StartGenerator(_windPlayer);
		_groanPlayback = StartGenerator(_groanPlayer);
		_insectsPlayback = StartGenerator(_insectsPlayer);
		_cricketsPlayback = StartGenerator(_cricketsPlayer);
		UpdateDayNightMix();
	}

	public override void _Process(double delta)
	{
		UpdateDayNightMix();
		FillPlayback(_windPlayback, NextWindSample);
		FillPlayback(_groanPlayback, NextGroanSample);
		FillPlayback(_insectsPlayback, NextDayInsectSample);
		FillPlayback(_cricketsPlayback, NextCricketSample);
	}

	public override void _ExitTree()
	{
		StopGenerator(_windPlayer);
		StopGenerator(_groanPlayer);
		StopGenerator(_insectsPlayer);
		StopGenerator(_cricketsPlayer);
		_windPlayback = null!;
		_groanPlayback = null!;
		_insectsPlayback = null!;
		_cricketsPlayback = null!;
	}

	private static AudioStreamGeneratorPlayback StartGenerator(AudioStreamPlayer player)
	{
		player.Stream = new AudioStreamGenerator
		{
			MixRate = MixRate,
			BufferLength = 0.3f,
		};
		player.Play();
		return player.GetStreamPlayback() as AudioStreamGeneratorPlayback
			?? throw new InvalidOperationException("Atmosphere audio generator failed to start.");
	}

	private static void StopGenerator(AudioStreamPlayer player)
	{
		player.Stop();
		player.Stream = null;
	}

	private static void FillPlayback(
		AudioStreamGeneratorPlayback playback,
		Func<float> createSample)
	{
		int framesAvailable = playback.GetFramesAvailable();
		for (int frame = 0; frame < framesAvailable; frame++)
		{
			float sample = Mathf.Clamp(createSample(), -1.0f, 1.0f);
			playback.PushFrame(new Vector2(sample, sample));
		}
	}

	private void UpdateDayNightMix()
	{
		float sunHeight = Mathf.Sin(((_worldTime.CurrentHour - 6.0f) / 24.0f) * Mathf.Tau);
		DaylightBlend = Mathf.SmoothStep(0.0f, 1.0f, Mathf.Clamp((sunHeight + 0.08f) / 0.45f, 0.0f, 1.0f));
		float nightBlend = 1.0f - DaylightBlend;

		_windPlayer.VolumeDb = WindVolumeDb + Mathf.Lerp(1.5f, 0.0f, DaylightBlend);
		_groanPlayer.VolumeDb = ZombieGroanVolumeDb + Mathf.Lerp(2.0f, -2.0f, DaylightBlend);
		_insectsPlayer.VolumeDb = BlendVolume(DayInsectsVolumeDb, DaylightBlend);
		_cricketsPlayer.VolumeDb = BlendVolume(NightCricketsVolumeDb, nightBlend);
	}

	private float NextWindSample()
	{
		_windTime += 1.0f / MixRate;
		float rawNoise = _random.RandfRange(-1.0f, 1.0f);
		_windNoise = Mathf.Lerp(_windNoise, rawNoise, 0.018f);
		float slowGust = 0.58f + (0.26f * Mathf.Sin(_windTime * 0.43f));
		return _windNoise * slowGust * 0.8f;
	}

	private float NextDayInsectSample()
	{
		_insectTime += 1.0f / MixRate;
		float time = _insectTime;
		float pulse = Mathf.Pow(Mathf.Max(Mathf.Sin(time * 3.1f), 0.0f), 12.0f);
		float carrier = Mathf.Sin(time * Mathf.Tau * 4800.0f);
		return carrier * pulse * 0.13f;
	}

	private float NextCricketSample()
	{
		_cricketTime += 1.0f / MixRate;
		float time = _cricketTime;
		float rhythm = Mathf.Sin(time * Mathf.Tau * 3.7f);
		float pulse = rhythm > 0.58f ? (rhythm - 0.58f) / 0.42f : 0.0f;
		float carrier = Mathf.Sin(time * Mathf.Tau * 4100.0f);
		return carrier * pulse * 0.16f;
	}

	private float NextGroanSample()
	{
		const float sampleDelta = 1.0f / MixRate;
		if (_groanDuration <= 0.0f)
		{
			_groanWaitRemaining -= sampleDelta;
			if (_groanWaitRemaining <= 0.0f)
			{
				StartGroan();
			}
			return 0.0f;
		}

		_groanElapsed += sampleDelta;
		float progress = Mathf.Clamp(_groanElapsed / _groanDuration, 0.0f, 1.0f);
		float envelope = Mathf.Sin(progress * Mathf.Pi);
		float wobble = 1.0f + (0.09f * Mathf.Sin(_groanElapsed * 4.7f));
		float fundamental = Mathf.Sin(_groanElapsed * Mathf.Tau * _groanPitch * wobble);
		float harmonic = Mathf.Sin(_groanElapsed * Mathf.Tau * _groanPitch * 1.53f) * 0.34f;
		float breath = _random.RandfRange(-1.0f, 1.0f) * 0.12f;
		float sample = (fundamental + harmonic + breath) * envelope * 0.22f;

		if (_groanElapsed >= _groanDuration)
		{
			_groanDuration = 0.0f;
			_groanElapsed = 0.0f;
			_groanWaitRemaining = RandomGroanInterval();
		}
		return sample;
	}

	private void StartGroan()
	{
		_groanDuration = _random.RandfRange(1.6f, 3.1f);
		_groanPitch = _random.RandfRange(48.0f, 73.0f);
		_groanElapsed = 0.0f;
	}

	private float RandomGroanInterval()
	{
		float minimum = Mathf.Max(MinimumGroanInterval, 0.5f);
		float maximum = Mathf.Max(MaximumGroanInterval, minimum);
		return _random.RandfRange(minimum, maximum);
	}

	private static float BlendVolume(float baseVolumeDb, float blend)
	{
		float clampedBlend = Mathf.Clamp(blend, 0.0f, 1.0f);
		if (clampedBlend <= 0.001f)
		{
			return SilentVolumeDb;
		}
		return baseVolumeDb + Mathf.LinearToDb(clampedBlend);
	}
}
