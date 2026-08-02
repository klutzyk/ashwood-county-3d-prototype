#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.World;

public enum WeatherKind
{
	Clear,
	Overcast,
	Rain,
	Storm,
	Fog,
}

/// <summary>
/// Art-directable weather values. Profiles deliberately contain no timing or
/// scene references so they can be reused by every district in the county.
/// </summary>
[GlobalClass]
public partial class WeatherProfile : Resource
{
	[Export] public string DisplayName { get; set; } = "Clear";
	[Export] public WeatherKind Kind { get; set; } = WeatherKind.Clear;

	[ExportGroup("Lighting")]
	[Export(PropertyHint.Range, "0.05,1.5,0.01")]
	public float AmbientMultiplier { get; set; } = 1.0f;
	[Export(PropertyHint.Range, "0.05,1.5,0.01")]
	public float SkyEnergyMultiplier { get; set; } = 1.0f;
	[Export(PropertyHint.Range, "0.05,1.5,0.01")]
	public float DirectionalMultiplier { get; set; } = 1.0f;
	[Export] public Color DirectionalTint { get; set; } = Colors.White;
	[Export(PropertyHint.Range, "0.4,1.8,0.01")]
	public float Exposure { get; set; } = 1.0f;
	[Export(PropertyHint.Range, "0.35,1.4,0.01")]
	public float Saturation { get; set; } = 0.92f;

	[ExportGroup("Sky")]
	[Export] public Color SkyTopColor { get; set; } = new(0.23f, 0.34f, 0.48f);
	[Export] public Color SkyHorizonColor { get; set; } = new(0.72f, 0.58f, 0.46f);
	[Export] public Color CloudColor { get; set; } = new(0.82f, 0.86f, 0.92f);
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float CloudOpacity { get; set; } = 0.34f;

	[ExportGroup("Fog")]
	[Export] public Color FogColor { get; set; } = new(0.52f, 0.48f, 0.43f);
	[Export(PropertyHint.Range, "0,1,0.001")]
	public float FogDensity { get; set; } = 0.2f;
	[Export(PropertyHint.Range, "0,500,1,or_greater")]
	public float FogDepthBegin { get; set; } = 78.0f;
	[Export(PropertyHint.Range, "1,1000,1,or_greater")]
	public float FogDepthEnd { get; set; } = 310.0f;
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float FogSkyAffect { get; set; } = 0.18f;
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float FogAerialPerspective { get; set; } = 0.48f;

	[ExportGroup("Conditions")]
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float PrecipitationIntensity { get; set; }
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float WindIntensity { get; set; } = 0.2f;
	[Export(PropertyHint.Range, "0,12,0.1")]
	public float LightningPerMinute { get; set; }
	[Export(PropertyHint.Range, "0,2,0.05")]
	public float LightningBrightness { get; set; } = 0.8f;
}
