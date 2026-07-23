#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Zombies;

public enum ZombieAudioCue
{
	Alert,
	Attack,
	Hurt,
	Death,
}

public partial class ZombieAudioFeedback : AudioStreamPlayer3D
{
	[Export(PropertyHint.Range, "-30,0,0.5")] public float CueVolumeDb { get; set; } = -11.0f;
	[Export(PropertyHint.Range, "0,0.5,0.01")] public float MinimumCueInterval { get; set; } = 0.12f;

	private const float MixRate = 22050.0f;
	private readonly RandomNumberGenerator _random = new();
	private ulong _lastCueTime;
	private bool _hasPlayedCue;

	public string LastCueName { get; private set; } = string.Empty;
	public int CueCount { get; private set; }

	public override void _Ready()
	{
		_random.Randomize();
		VolumeDb = CueVolumeDb;
	}

	public override void _ExitTree()
	{
		Stop();
		Stream = null;
	}

	public void PlayCue(ZombieAudioCue cue)
	{
		ulong currentTime = Time.GetTicksMsec();
		if (cue != ZombieAudioCue.Death && _hasPlayedCue &&
			currentTime - _lastCueTime < (ulong)(Mathf.Max(MinimumCueInterval, 0.0f) * 1000.0f))
		{
			return;
		}

		_hasPlayedCue = true;
		_lastCueTime = currentTime;
		LastCueName = cue.ToString();
		CueCount++;

		float duration = cue switch
		{
			ZombieAudioCue.Alert => 0.55f,
			ZombieAudioCue.Attack => 0.34f,
			ZombieAudioCue.Hurt => 0.2f,
			_ => 0.82f,
		};
		Stream = new AudioStreamGenerator
		{
			MixRate = MixRate,
			BufferLength = duration + 0.08f,
		};
		VolumeDb = CueVolumeDb;
		PitchScale = _random.RandfRange(0.92f, 1.08f);
		Play();

		if (GetStreamPlayback() is not AudioStreamGeneratorPlayback playback)
		{
			return;
		}

		int frameCount = Mathf.Min(
			Mathf.CeilToInt(duration * MixRate),
			playback.GetFramesAvailable());
		float phase = 0.0f;
		for (int frame = 0; frame < frameCount; frame++)
		{
			float progress = frame / Mathf.Max((float)frameCount - 1.0f, 1.0f);
			float sample = CreateSample(cue, progress, ref phase);
			playback.PushFrame(new Vector2(sample, sample));
		}
	}

	private float CreateSample(ZombieAudioCue cue, float progress, ref float phase)
	{
		(float startPitch, float endPitch, float amplitude) = cue switch
		{
			ZombieAudioCue.Alert => (72.0f, 48.0f, 0.18f),
			ZombieAudioCue.Attack => (82.0f, 57.0f, 0.2f),
			ZombieAudioCue.Hurt => (112.0f, 66.0f, 0.17f),
			_ => (64.0f, 30.0f, 0.2f),
		};
		float pitch = Mathf.Lerp(startPitch, endPitch, Mathf.SmoothStep(0.0f, 1.0f, progress));
		phase += Mathf.Tau * pitch / MixRate;
		float attack = Mathf.Clamp(progress / 0.08f, 0.0f, 1.0f);
		float release = Mathf.Pow(1.0f - progress, cue == ZombieAudioCue.Hurt ? 0.7f : 1.4f);
		float voice = Mathf.Sin(phase) + (Mathf.Sin(phase * 1.47f) * 0.34f);
		float rasp = _random.RandfRange(-1.0f, 1.0f) * 0.16f;
		return Mathf.Clamp((voice + rasp) * attack * release * amplitude, -1.0f, 1.0f);
	}
}
