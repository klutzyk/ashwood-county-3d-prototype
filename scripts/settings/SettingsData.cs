#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Settings;

public enum GraphicsPreset
{
	Low,
	Medium,
	High,
}

public sealed class SettingsData
{
	public float MasterVolume { get; set; } = 1.0f;
	public float AmbientVolume { get; set; } = 1.0f;
	public float EffectsVolume { get; set; } = 1.0f;
	public float MouseSensitivity { get; set; } = 0.0025f;
	public bool Fullscreen { get; set; }
	public bool VSync { get; set; } = true;
	public Vector2I Resolution { get; set; } = new(1280, 720);
	public GraphicsPreset GraphicsPreset { get; set; } = GraphicsPreset.High;

	public SettingsData Copy()
	{
		return (SettingsData)MemberwiseClone();
	}
}
