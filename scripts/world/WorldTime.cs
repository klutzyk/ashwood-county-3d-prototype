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
	[Export] public float NightAmbientEnergy { get; set; } = 0.24f;
	[Export] public float DayAmbientEnergy { get; set; } = 0.8f;
	[Export] public float NightSkyEnergy { get; set; } = 0.18f;
	[Export] public float DaySkyEnergy { get; set; } = 0.7f;
	[Export] public float NightDirectionalEnergy { get; set; } = 0.06f;
	[Export] public float DayDirectionalEnergy { get; set; } = 0.65f;
	[Export] public Color NightDirectionalColor { get; set; } =
		new(0.5f, 0.58f, 0.78f);
	[Export] public Color DayDirectionalColor { get; set; } =
		new(0.83f, 0.85f, 0.88f);
	[Export] public Color GoldenHourDirectionalColor { get; set; } =
		new(1.0f, 0.68f, 0.43f);
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float GoldenHourColorStrength { get; set; }

	public float CurrentHour { get; private set; }
	public float DaylightBlend { get; private set; }
	public float WeatherAmbientMultiplier { get; private set; } = 1.0f;
	public float WeatherSkyMultiplier { get; private set; } = 1.0f;
	public float WeatherDirectionalMultiplier { get; private set; } = 1.0f;
	public Color WeatherDirectionalTint { get; private set; } = Colors.White;

	private DirectionalLight3D _directionalLight = null!;
	private Environment _environment = null!;
	private int _lastDisplayedMinute = -1;

	public override void _Ready()
	{
		_directionalLight = GetNode<DirectionalLight3D>(DirectionalLightPath);
		WorldEnvironment worldEnvironment = GetNode<WorldEnvironment>(WorldEnvironmentPath);
		_environment = worldEnvironment.Environment
			?? throw new System.InvalidOperationException("World time requires an Environment resource.");
		SettingsManager.Instance?.ApplyGraphicsToScene(GetParent());
		SetTimeOfDay(StartingHour);
	}

	public override void _Process(double delta)
	{
		float duration = Mathf.Max(FullDayDurationSeconds, 1.0f);
		SetTimeOfDay(CurrentHour + ((float)delta * 24.0f / duration));
	}

	public void SetTimeOfDay(float hour)
	{
		CurrentHour = Mathf.PosMod(hour, 24.0f);
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
		UpdateLighting();
	}

	private void UpdateLighting()
	{
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
