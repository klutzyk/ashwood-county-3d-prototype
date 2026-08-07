#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Settings;

namespace AshwoodCounty3DPrototype.World;

public partial class WorldTime : Node
{
	[Signal]
	public delegate void TimeChangedEventHandler(int hour, int minute);

	[Export(PropertyHint.Range, "30,3600,1,or_greater")]
	public float FullDayDurationSeconds { get; set; } = 240.0f;

	[Export(PropertyHint.Range, "0,24,0.25")]
	public float StartingHour { get; set; } = 17.0f;

	[Export] public NodePath DirectionalLightPath { get; set; } = new("../DirectionalLight3D");
	[Export] public NodePath WorldEnvironmentPath { get; set; } = new("../WorldEnvironment");

	// Ambient is deliberately far below the directional energy. The scene reads
	// as photographed only when the sun clearly dominates the skylight fill; the
	// previous near-parity values plus a 0.48 shadow opacity flattened every
	// surface into the same tone.
	[Export] public float NightAmbientEnergy { get; set; } = 0.10f;
	[Export] public float DayAmbientEnergy { get; set; } = 0.46f;
	[Export] public float NightSkyEnergy { get; set; } = 0.06f;
	[Export] public float DaySkyEnergy { get; set; } = 1.0f;
	[Export] public float NightDirectionalEnergy { get; set; } = 0.09f;
	[Export] public float DayDirectionalEnergy { get; set; } = 1.7f;
	[Export] public Color NightDirectionalColor { get; set; } =
		new(0.5f, 0.58f, 0.78f);
	[Export] public Color DayDirectionalColor { get; set; } =
		new(1.0f, 0.96f, 0.90f);
	[Export] public Color GoldenHourDirectionalColor { get; set; } =
		new(1.0f, 0.72f, 0.48f);
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float GoldenHourColorStrength { get; set; }

	/// <summary>
	/// Longest shadow range the scene may use, in metres. The graphics preset
	/// scales this down; it is applied here rather than in the scene because the
	/// settings pass previously overwrote the authored value on load.
	/// </summary>
	[Export] public float ShadowMaxDistance { get; set; } = 150.0f;

	public float CurrentHour { get; private set; }
	public float DaylightBlend { get; private set; }
	public float WeatherAmbientMultiplier { get; private set; } = 1.0f;
	public float WeatherSkyMultiplier { get; private set; } = 1.0f;
	public float WeatherDirectionalMultiplier { get; private set; } = 1.0f;
	public Color WeatherDirectionalTint { get; private set; } = Colors.White;

	private DirectionalLight3D? _directionalLight;
	private Environment? _environment;
	private int _lastDisplayedMinute = -1;

	public override void _Ready()
	{
		SettingsManager.Instance?.ApplyGraphicsToScene(GetParent());

		// The old scene kept its sun and sky as fixed siblings, resolvable at
		// _Ready. The county world builds its own sun and sky at runtime, later
		// than this node's _Ready fires, so when they exist a caller wires them
		// in with AttachToAtmosphere instead of relying on the path lookup here.
		var light = GetNodeOrNull<DirectionalLight3D>(DirectionalLightPath);
		WorldEnvironment? worldEnvironment = GetNodeOrNull<WorldEnvironment>(WorldEnvironmentPath);
		if (light != null && worldEnvironment?.Environment != null)
		{
			AttachToAtmosphere(light, worldEnvironment.Environment);
		}
	}

	/// <summary>
	/// Points the day/night cycle at a sun and sky built after this node's own
	/// _Ready already ran. Safe to call once the caller knows both exist.
	/// </summary>
	public void AttachToAtmosphere(DirectionalLight3D sun, Environment environment)
	{
		_directionalLight = sun;
		_environment = environment;
		ApplyShadowRange();
		SetTimeOfDay(StartingHour);
	}

	/// <summary>
	/// Re-applies the authored shadow range after the graphics pass. The shared
	/// settings pass clamps every DirectionalLight3D to 24-42 m, which was tuned
	/// for the old Compatibility renderer; under Forward+ with four shadow
	/// splits that range ends shadows in the middle of the street and is a
	/// large part of why the town read as pasted-together.
	/// </summary>
	private void ApplyShadowRange()
	{
		if (_directionalLight == null)
		{
			return;
		}

		float scale = SettingsManager.Instance?.Current.GraphicsPreset switch
		{
			GraphicsPreset.Low => 0.45f,
			GraphicsPreset.Medium => 0.75f,
			_ => 1.0f,
		};
		_directionalLight.DirectionalShadowMaxDistance =
			Mathf.Max(ShadowMaxDistance * scale, 16.0f);
	}

	public override void _Process(double delta)
	{
		if (_directionalLight == null || _environment == null)
		{
			return;
		}

		float duration = Mathf.Max(FullDayDurationSeconds, 1.0f);
		SetTimeOfDay(CurrentHour + ((float)delta * 24.0f / duration));
	}

	public void SetTimeOfDay(float hour)
	{
		CurrentHour = Mathf.PosMod(hour, 24.0f);
		if (_directionalLight == null || _environment == null)
		{
			return;
		}

		UpdateLighting();
		EmitTimeWhenMinuteChanges();
	}

	/// <summary>
	/// Applies weather as a multiplier over the authored day/night curve. Keeping
	/// the two systems composed here prevents weather transitions from fighting
	/// the clock or compounding energy every frame.
	/// </summary>
	public void SetWeatherInfluence(
		float ambientMultiplier,
		float skyMultiplier,
		float directionalMultiplier,
		Color directionalTint)
	{
		WeatherAmbientMultiplier = Mathf.Max(ambientMultiplier, 0.0f);
		WeatherSkyMultiplier = Mathf.Max(skyMultiplier, 0.0f);
		WeatherDirectionalMultiplier = Mathf.Max(directionalMultiplier, 0.0f);
		WeatherDirectionalTint = directionalTint;
		if (_directionalLight != null && _environment != null)
		{
			UpdateLighting();
		}
	}

	private void UpdateLighting()
	{
		if (_directionalLight == null || _environment == null)
		{
			return;
		}

		float sunHeight = Mathf.Sin(((CurrentHour - 6.0f) / 24.0f) * Mathf.Tau);
		float daylight = Mathf.Clamp((sunHeight + 0.12f) / 0.55f, 0.0f, 1.0f);
		daylight = daylight * daylight * (3.0f - (2.0f * daylight));
		DaylightBlend = daylight;
		float goldenHour = Mathf.Clamp(
			1.0f - (Mathf.Abs(sunHeight - 0.2f) / 0.34f),
			0.0f,
			1.0f) * daylight * GoldenHourColorStrength;

		_directionalLight.RotationDegrees = new Vector3(
			-(CurrentHour - 6.0f) * 15.0f,
			-28.0f,
			0.0f);
		_directionalLight.LightEnergy = Mathf.Lerp(
			NightDirectionalEnergy,
			DayDirectionalEnergy,
			daylight) * WeatherDirectionalMultiplier;
		Color daylightColor = NightDirectionalColor.Lerp(
			DayDirectionalColor,
			daylight);
		Color timeOfDayColor = daylightColor.Lerp(
			GoldenHourDirectionalColor,
			goldenHour);
		_directionalLight.LightColor = new Color(
			timeOfDayColor.R * WeatherDirectionalTint.R,
			timeOfDayColor.G * WeatherDirectionalTint.G,
			timeOfDayColor.B * WeatherDirectionalTint.B,
			1.0f);
		_environment.AmbientLightEnergy = Mathf.Lerp(
			NightAmbientEnergy,
			DayAmbientEnergy,
			daylight) * WeatherAmbientMultiplier;
		_environment.BackgroundEnergyMultiplier = Mathf.Lerp(
			NightSkyEnergy,
			DaySkyEnergy,
			daylight) * WeatherSkyMultiplier;
	}

	private void EmitTimeWhenMinuteChanges()
	{
		int totalMinutes = Mathf.FloorToInt(CurrentHour * 60.0f) % (24 * 60);
		if (totalMinutes == _lastDisplayedMinute)
		{
			return;
		}

		_lastDisplayedMinute = totalMinutes;
		EmitSignal(SignalName.TimeChanged, totalMinutes / 60, totalMinutes % 60);
	}
}
