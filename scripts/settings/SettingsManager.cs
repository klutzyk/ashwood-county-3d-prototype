#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Settings;

public partial class SettingsManager : Node
{
	[Signal]
	public delegate void SettingsChangedEventHandler();

	public const string DefaultSettingsFilePath = "user://ashwood_county_settings.cfg";
	public static SettingsManager? Instance { get; private set; }

	[Export] public string SettingsFilePath { get; set; } = DefaultSettingsFilePath;
	public SettingsData Current { get; private set; } = new();

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		EnsureAudioBus("Ambient");
		EnsureAudioBus("Effects");
		LoadSettings();
		ApplySettings();
	}

	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void SetAndSave(SettingsData settings)
	{
		Current = Sanitize(settings);
		ApplySettings();
		SaveSettings();
		EmitSignal(SignalName.SettingsChanged);
	}

	public void Restore(SettingsData settings)
	{
		SetAndSave(settings);
	}

	public void ResetDefaults()
	{
		SetAndSave(new SettingsData());
	}

	public void LoadSettings()
	{
		SettingsData loaded = new();
		using ConfigFile config = new();
		if (config.Load(SettingsFilePath) == Error.Ok)
		{
			loaded.MasterVolume = (float)config.GetValue("audio", "master", loaded.MasterVolume);
			loaded.AmbientVolume = (float)config.GetValue("audio", "ambient", loaded.AmbientVolume);
			loaded.EffectsVolume = (float)config.GetValue("audio", "effects", loaded.EffectsVolume);
			loaded.MouseSensitivity =
				(float)config.GetValue("controls", "mouse_sensitivity", loaded.MouseSensitivity);
			loaded.Fullscreen = (bool)config.GetValue("display", "fullscreen", loaded.Fullscreen);
			loaded.VSync = (bool)config.GetValue("display", "vsync", loaded.VSync);
			loaded.Resolution = new Vector2I(
				(int)config.GetValue("display", "width", loaded.Resolution.X),
				(int)config.GetValue("display", "height", loaded.Resolution.Y));
			loaded.GraphicsPreset = (GraphicsPreset)(int)config.GetValue(
				"graphics",
				"preset",
				(int)loaded.GraphicsPreset);
		}
		Current = Sanitize(loaded);
	}

	public Error SaveSettings()
	{
		using ConfigFile config = new();
		config.SetValue("audio", "master", Current.MasterVolume);
		config.SetValue("audio", "ambient", Current.AmbientVolume);
		config.SetValue("audio", "effects", Current.EffectsVolume);
		config.SetValue("controls", "mouse_sensitivity", Current.MouseSensitivity);
		config.SetValue("display", "fullscreen", Current.Fullscreen);
		config.SetValue("display", "vsync", Current.VSync);
		config.SetValue("display", "width", Current.Resolution.X);
		config.SetValue("display", "height", Current.Resolution.Y);
		config.SetValue("graphics", "preset", (int)Current.GraphicsPreset);
		return config.Save(SettingsFilePath);
	}

	public void ApplySettings()
	{
		SetBusVolume("Master", Current.MasterVolume);
		SetBusVolume("Ambient", Current.AmbientVolume);
		SetBusVolume("Effects", Current.EffectsVolume);

		if (DisplayServer.GetName() != "headless")
		{
			DisplayServer.WindowSetVsyncMode(Current.VSync
				? DisplayServer.VSyncMode.Enabled
				: DisplayServer.VSyncMode.Disabled);
			DisplayServer.WindowSetMode(Current.Fullscreen
				? DisplayServer.WindowMode.Fullscreen
				: DisplayServer.WindowMode.Windowed);
			if (!Current.Fullscreen)
			{
				DisplayServer.WindowSetSize(Current.Resolution);
			}
		}
		ApplyGraphicsToScene(GetTree().CurrentScene);
	}

	public void ApplyGraphicsToScene(Node? scene)
	{
		if (scene is null)
		{
			return;
		}

		float shadowDistance = Current.GraphicsPreset switch
		{
			GraphicsPreset.Low => 24.0f,
			GraphicsPreset.Medium => 34.0f,
			_ => 42.0f,
		};
		foreach (Node node in Enumerate(scene))
		{
			if (node is DirectionalLight3D directional)
			{
				directional.ShadowEnabled = true;
				directional.DirectionalShadowMaxDistance = shadowDistance;
			}
		}
	}

	private static SettingsData Sanitize(SettingsData source)
	{
		SettingsData settings = source.Copy();
		settings.MasterVolume = Mathf.Clamp(settings.MasterVolume, 0.0f, 1.0f);
		settings.AmbientVolume = Mathf.Clamp(settings.AmbientVolume, 0.0f, 1.0f);
		settings.EffectsVolume = Mathf.Clamp(settings.EffectsVolume, 0.0f, 1.0f);
		settings.MouseSensitivity = Mathf.Clamp(settings.MouseSensitivity, 0.001f, 0.006f);
		settings.Resolution = new Vector2I(
			Mathf.Clamp(settings.Resolution.X, 960, 3840),
			Mathf.Clamp(settings.Resolution.Y, 540, 2160));
		if (!System.Enum.IsDefined(settings.GraphicsPreset))
		{
			settings.GraphicsPreset = GraphicsPreset.Medium;
		}
		return settings;
	}

	private static void EnsureAudioBus(string busName)
	{
		if (AudioServer.GetBusIndex(busName) >= 0)
		{
			return;
		}
		AudioServer.AddBus();
		int index = AudioServer.BusCount - 1;
		AudioServer.SetBusName(index, busName);
		AudioServer.SetBusSend(index, "Master");
	}

	private static void SetBusVolume(string busName, float linearVolume)
	{
		int index = AudioServer.GetBusIndex(busName);
		if (index >= 0)
		{
			AudioServer.SetBusVolumeDb(
				index,
				linearVolume <= 0.001f ? -80.0f : Mathf.LinearToDb(linearVolume));
		}
	}

	private static System.Collections.Generic.IEnumerable<Node> Enumerate(Node root)
	{
		yield return root;
		foreach (Node child in root.GetChildren())
		{
			foreach (Node descendant in Enumerate(child))
			{
				yield return descendant;
			}
		}
	}
}
