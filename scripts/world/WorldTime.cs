#nullable enable

using Godot;

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

	public float CurrentHour { get; private set; }

	private DirectionalLight3D _directionalLight = null!;
	private Environment _environment = null!;
	private int _lastDisplayedMinute = -1;

	public override void _Ready()
	{
		_directionalLight = GetNode<DirectionalLight3D>(DirectionalLightPath);
		WorldEnvironment worldEnvironment = GetNode<WorldEnvironment>(WorldEnvironmentPath);
		_environment = worldEnvironment.Environment
			?? throw new System.InvalidOperationException("World time requires an Environment resource.");
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

	private void UpdateLighting()
	{
		float sunHeight = Mathf.Sin(((CurrentHour - 6.0f) / 24.0f) * Mathf.Tau);
		float daylight = Mathf.Clamp((sunHeight + 0.12f) / 0.55f, 0.0f, 1.0f);
		daylight = daylight * daylight * (3.0f - (2.0f * daylight));

		_directionalLight.RotationDegrees = new Vector3(
			-(CurrentHour - 6.0f) * 15.0f,
			-28.0f,
			0.0f);
		_directionalLight.LightEnergy = Mathf.Lerp(NightDirectionalEnergy, DayDirectionalEnergy, daylight);
		_directionalLight.LightColor = new Color(
			Mathf.Lerp(0.5f, 0.83f, daylight),
			Mathf.Lerp(0.58f, 0.85f, daylight),
			Mathf.Lerp(0.78f, 0.88f, daylight));
		_environment.AmbientLightEnergy = Mathf.Lerp(NightAmbientEnergy, DayAmbientEnergy, daylight);
		_environment.BackgroundEnergyMultiplier = Mathf.Lerp(NightSkyEnergy, DaySkyEnergy, daylight);
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
