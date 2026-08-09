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

	/// <summary>
	/// Pushes the current graphics preset onto everything in the scene that can
	/// act on it: the viewport's 3D render scale, the sun's shadow range, the
	/// terrain material's sample count, and the county's streaming radii.
	///
	/// Called on load and whenever settings change, so a preset switch takes
	/// effect without a restart for everything except the renderer itself, which
	/// Godot can only pick at startup.
	/// </summary>
	public void ApplyGraphicsToScene(Node? scene)
	{
		if (scene is null)
		{
			return;
		}

		GraphicsPreset preset = Current.GraphicsPreset;

		Viewport? viewport = scene.GetViewport();
		if (viewport != null)
		{
			GraphicsQuality.ApplyToViewport(viewport, preset);
		}

		float shadowDistance = GraphicsQuality.ShadowDistance(preset);

		foreach (Node node in Enumerate(scene))
		{
			if (node is DirectionalLight3D directional)
			{
				const string authoredShadowMeta = "_ashwood_authored_shadow_distance";
				if (!directional.HasMeta(authoredShadowMeta))
				{
					directional.SetMeta(
						authoredShadowMeta,
						directional.DirectionalShadowMaxDistance);
				}
				float authoredShadowDistance = (float)directional
					.GetMeta(authoredShadowMeta).AsDouble();
				directional.ShadowEnabled = true;
				directional.DirectionalShadowMode = preset == GraphicsPreset.Low
					? DirectionalLight3D.ShadowMode.Parallel2Splits
					: DirectionalLight3D.ShadowMode.Parallel4Splits;

				// Only ever shorten a range, never extend one. These numbers used
				// to be 24-42m, chosen for the Main Street slice, and applying
				// them blindly cut the county sun's 1200m cascade set down to 42m
				// so nothing past the nearest field cast a shadow at all.
				directional.DirectionalShadowMaxDistance = Mathf.Min(
					authoredShadowDistance,
					shadowDistance);
			}
			else if (node is Camera3D camera)
			{
				const string authoredFarMeta = "_ashwood_authored_camera_far";
				if (!camera.HasMeta(authoredFarMeta))
				{
					camera.SetMeta(authoredFarMeta, camera.Far);
				}
				float authoredFar = (float)camera.GetMeta(authoredFarMeta).AsDouble();
				camera.Far = Mathf.Min(
					authoredFar,
					GraphicsQuality.CameraFarDistance(preset));
			}
			else if (node is WorldEnvironment worldEnvironment &&
				worldEnvironment.Environment is not null)
			{
				bool mobileRenderer = ProjectSettings.GetSetting(
					"rendering/renderer/rendering_method", "mobile").AsString() == "mobile";
				worldEnvironment.Environment.SsaoEnabled =
					!mobileRenderer && GraphicsQuality.Ssao(preset);
				worldEnvironment.Environment.SsilEnabled = false;
				worldEnvironment.Environment.GlowEnabled =
					!mobileRenderer && GraphicsQuality.Glow(preset);
			}
		}

		ApplyTerrainQuality(preset);
	}

	/// <summary>
	/// Sets the terrain material's quality uniforms.
	///
	/// This is the largest single cost in the frame - swapping the splat material
	/// for a plain one measured 21.5 to 43.5 FPS - and almost all of it is texture
	/// fetches, so the two uniforms that control how many are taken are the most
	/// valuable quality knobs in the game.
	///
	/// The material is a shared resource rather than per-instance, so setting it
	/// once reaches every terrain chunk.
	/// </summary>
	private static void ApplyTerrainQuality(GraphicsPreset preset)
	{
		const string path = "res://assets/materials/county_terrain.tres";
		if (!ResourceLoader.Exists(path) ||
			ResourceLoader.Load(path) is not ShaderMaterial material)
		{
			return;
		}

		material.SetShaderParameter(
			"anti_tiling", GraphicsQuality.TerrainSamples(preset) > 1 ? 1.0f : 0.0f);
		material.SetShaderParameter(
			"enable_triplanar", GraphicsQuality.TerrainTriplanar(preset) ? 1.0f : 0.0f);
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
