#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

public enum PlayerMeleeAudioCue
{
	SwingLight,
	SwingHeavy,
	FleshImpact,
	Exhausted,
}

/// <summary>
/// Lightweight local melee transients. Commercial-safe authored cues provide
/// the primary swing/contact layer; runtime synthesis remains as a resilient
/// fallback and supplies the exhausted-breath response.
/// </summary>
public partial class PlayerMeleeAudioFeedback : AudioStreamPlayer3D
{
	private const string LightSwingPath =
		"res://assets/third_party/audio/kenney_rpg_audio/Audio/cloth2.ogg";
	private const string HeavySwingPath =
		"res://assets/third_party/audio/kenney_rpg_audio/Audio/cloth4.ogg";
	private const string FleshImpactPath =
		"res://assets/third_party/audio/kenney_rpg_audio/Audio/chop.ogg";

	[Export(PropertyHint.Range, "-30,0,0.5")]
	public float CueVolumeDb { get; set; } = -8.0f;

	[Export(PropertyHint.Range, "0,0.25,0.01")]
	public float MinimumCueInterval { get; set; } = 0.035f;
	[Export] public AudioStream? LightSwingStream { get; set; }
	[Export] public AudioStream? HeavySwingStream { get; set; }
	[Export] public AudioStream? FleshImpactStream { get; set; }

	private const float MixRate = 22050.0f;
	private readonly RandomNumberGenerator _random = new();
	private ulong _lastCueTime;

	public string LastCueName { get; private set; } = string.Empty;
	public int CueCount { get; private set; }

	public override void _Ready()
	{
		_random.Randomize();
		Bus = "Effects";
		VolumeDb = CueVolumeDb;
		LightSwingStream ??= LoadOptionalStream(LightSwingPath);
		HeavySwingStream ??= LoadOptionalStream(HeavySwingPath);
		FleshImpactStream ??= LoadOptionalStream(FleshImpactPath);
	}

	public override void _ExitTree()
	{
		Stop();
		Stream = null;
	}

	public void PlayCue(PlayerMeleeAudioCue cue)
	{
		ulong now = Time.GetTicksMsec();
		if (cue != PlayerMeleeAudioCue.FleshImpact &&
			now - _lastCueTime < (ulong)(Mathf.Max(MinimumCueInterval, 0.0f) * 1000.0f))
		{
			return;
		}

		_lastCueTime = now;
		LastCueName = cue.ToString();
		CueCount++;
		AudioStream? authoredStream = cue switch
		{
			PlayerMeleeAudioCue.SwingLight => LightSwingStream,
			PlayerMeleeAudioCue.SwingHeavy => HeavySwingStream,
			PlayerMeleeAudioCue.FleshImpact => FleshImpactStream,
			_ => null,
		};
		if (authoredStream is not null)
		{
			Stream = authoredStream;
			VolumeDb = cue == PlayerMeleeAudioCue.FleshImpact
				? CueVolumeDb - 1.5f
				: CueVolumeDb - 3.0f;
			PitchScale = _random.RandfRange(0.94f, 1.06f);
			Play();
			return;
		}

		float duration = cue switch
		{
			PlayerMeleeAudioCue.SwingLight => 0.19f,
			PlayerMeleeAudioCue.SwingHeavy => 0.24f,
			PlayerMeleeAudioCue.FleshImpact => 0.17f,
			_ => 0.22f,
		};
		Stream = new AudioStreamGenerator
		{
			MixRate = MixRate,
			BufferLength = duration + 0.06f,
		};
		VolumeDb = CueVolumeDb;
		PitchScale = _random.RandfRange(0.96f, 1.04f);
		Play();

		if (GetStreamPlayback() is not AudioStreamGeneratorPlayback playback)
		{
			return;
		}

		int frameCount = Mathf.Min(
			Mathf.CeilToInt(duration * MixRate),
			playback.GetFramesAvailable());
		float filteredNoise = 0.0f;
		float phase = 0.0f;
		for (int frame = 0; frame < frameCount; frame++)
		{
			float progress = frame / Mathf.Max(frameCount - 1.0f, 1.0f);
			float sample = CreateSample(cue, progress, ref filteredNoise, ref phase);
			playback.PushFrame(new Vector2(sample, sample));
		}
	}

	private static AudioStream? LoadOptionalStream(string path)
	{
		return ResourceLoader.Exists(path)
			? ResourceLoader.Load<AudioStream>(path)
			: null;
	}

	private float CreateSample(
		PlayerMeleeAudioCue cue,
		float progress,
		ref float filteredNoise,
		ref float phase)
	{
		float rawNoise = _random.RandfRange(-1.0f, 1.0f);
		float smoothing = cue == PlayerMeleeAudioCue.FleshImpact ? 0.22f : 0.065f;
		filteredNoise = Mathf.Lerp(filteredNoise, rawNoise, smoothing);

		if (cue is PlayerMeleeAudioCue.SwingLight or PlayerMeleeAudioCue.SwingHeavy)
		{
			float heavy = cue == PlayerMeleeAudioCue.SwingHeavy ? 1.0f : 0.0f;
			float centre = Mathf.Lerp(0.48f, 0.56f, progress);
			float width = Mathf.Lerp(0.34f, 0.4f, heavy);
			float envelope = Mathf.Exp(-Mathf.Pow((progress - centre) / width, 2.0f) * 3.4f);
			float air = filteredNoise * Mathf.Lerp(0.19f, 0.25f, heavy);
			float flutter = Mathf.Sin(progress * Mathf.Tau * Mathf.Lerp(16.0f, 11.0f, heavy)) *
				Mathf.Lerp(0.028f, 0.045f, heavy);
			return Mathf.Clamp((air + flutter) * envelope, -0.5f, 0.5f);
		}

		if (cue == PlayerMeleeAudioCue.FleshImpact)
		{
			float frequency = Mathf.Lerp(112.0f, 48.0f, progress);
			phase += Mathf.Tau * frequency / MixRate;
			float thud = Mathf.Sin(phase) * 0.27f;
			float crack = filteredNoise * Mathf.Exp(-progress * 23.0f) * 0.46f;
			float body = filteredNoise * 0.16f;
			float envelope = Mathf.Pow(1.0f - progress, 2.2f);
			return Mathf.Clamp((thud + crack + body) * envelope, -0.8f, 0.8f);
		}

		float breathEnvelope = Mathf.Sin(progress * Mathf.Pi) * (1.0f - (progress * 0.35f));
		float breathPulse = 0.65f + (Mathf.Sin(progress * Mathf.Tau * 3.0f) * 0.35f);
		return filteredNoise * breathEnvelope * breathPulse * 0.13f;
	}
}
