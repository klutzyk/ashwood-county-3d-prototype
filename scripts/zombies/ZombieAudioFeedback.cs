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
		float throatPhase = 0.0f;
		float raspState = 0.0f;
		for (int frame = 0; frame < frameCount; frame++)
		{
			float progress = frame / Mathf.Max((float)frameCount - 1.0f, 1.0f);
			float sample = CreateSample(
				cue,
				progress,
				ref phase,
				ref throatPhase,
				ref raspState);
			playback.PushFrame(new Vector2(sample, sample));
		}
	}

	private float CreateSample(
		ZombieAudioCue cue,
		float progress,
		ref float phase,
		ref float throatPhase,
		ref float raspState)
	{
		(float startPitch, float endPitch, float amplitude) = cue switch
		{
			ZombieAudioCue.Alert => (68.0f, 43.0f, 0.17f),
			ZombieAudioCue.Attack => (86.0f, 52.0f, 0.19f),
			ZombieAudioCue.Hurt => (118.0f, 62.0f, 0.16f),
			_ => (61.0f, 27.0f, 0.19f),
		};
		float pitchWobble = Mathf.Sin(progress * Mathf.Tau * 5.2f) *
			Mathf.Lerp(3.2f, 0.7f, progress);
		float pitch = Mathf.Lerp(
			startPitch,
			endPitch,
			Mathf.SmoothStep(0.0f, 1.0f, progress)) + pitchWobble;
		phase += Mathf.Tau * pitch / MixRate;
		throatPhase += Mathf.Tau * (pitch * 2.37f) / MixRate;
		float attackDuration = cue == ZombieAudioCue.Hurt ? 0.025f : 0.065f;
		float attack = Mathf.SmoothStep(
			0.0f,
			1.0f,
			Mathf.Clamp(progress / attackDuration, 0.0f, 1.0f));
		float releasePower = cue switch
		{
			ZombieAudioCue.Hurt => 0.6f,
			ZombieAudioCue.Death => 1.05f,
			_ => 1.35f,
		};
		float release = Mathf.Pow(1.0f - progress, releasePower);
		float glottal =
			Mathf.Sin(phase) +
			(Mathf.Sin(phase * 2.0f) * 0.38f) +
			(Mathf.Sin(phase * 3.0f) * 0.16f);
		float throat = Mathf.Sin(throatPhase) *
			(0.2f + (Mathf.Sin(phase * 0.5f) * 0.08f));
		float rawNoise = _random.RandfRange(-1.0f, 1.0f);
		raspState = Mathf.Lerp(raspState, rawNoise, 0.075f);
		float raspAmount = cue == ZombieAudioCue.Attack ? 0.42f : 0.3f;
		float voice = Mathf.Clamp((glottal * 0.62f) + throat, -1.4f, 1.4f);
		float pulse = 0.88f + (Mathf.Sin(progress * Mathf.Tau * 2.3f) * 0.12f);
		return Mathf.Clamp(
			(voice + (raspState * raspAmount)) * attack * release * pulse * amplitude,
			-1.0f,
			1.0f);
	}
}
