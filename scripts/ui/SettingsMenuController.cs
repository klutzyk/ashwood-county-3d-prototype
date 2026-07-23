#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Settings;

namespace AshwoodCounty3DPrototype.UI;

public partial class SettingsMenuController : Control
{
	[Signal]
	public delegate void ClosedEventHandler();

	private static readonly Vector2I[] Resolutions =
	{
		new(1280, 720),
		new(1600, 900),
		new(1920, 1080),
	};

	private HSlider _master = null!;
	private HSlider _ambient = null!;
	private HSlider _effects = null!;
	private HSlider _sensitivity = null!;
	private OptionButton _windowMode = null!;
	private CheckButton _vsync = null!;
	private OptionButton _resolution = null!;
	private OptionButton _preset = null!;
	private ConfirmationDialog _displayConfirmation = null!;
	private Timer _revertTimer = null!;
	private SettingsData? _displayBackup;

	public override void _Ready()
	{
		_master = GetNode<HSlider>("Panel/Margin/Layout/Grid/Master");
		_ambient = GetNode<HSlider>("Panel/Margin/Layout/Grid/Ambient");
		_effects = GetNode<HSlider>("Panel/Margin/Layout/Grid/Effects");
		_sensitivity = GetNode<HSlider>("Panel/Margin/Layout/Grid/Sensitivity");
		_windowMode = GetNode<OptionButton>("Panel/Margin/Layout/Grid/WindowMode");
		_vsync = GetNode<CheckButton>("Panel/Margin/Layout/Grid/VSync");
		_resolution = GetNode<OptionButton>("Panel/Margin/Layout/Grid/Resolution");
		_preset = GetNode<OptionButton>("Panel/Margin/Layout/Grid/Preset");
		_displayConfirmation = GetNode<ConfirmationDialog>("DisplayConfirmation");
		_revertTimer = GetNode<Timer>("RevertTimer");

		_windowMode.AddItem("Windowed");
		_windowMode.AddItem("Fullscreen");
		foreach (Vector2I resolution in Resolutions)
		{
			_resolution.AddItem($"{resolution.X} × {resolution.Y}");
		}
		foreach (GraphicsPreset preset in System.Enum.GetValues<GraphicsPreset>())
		{
			_preset.AddItem(preset.ToString());
		}

		GetNode<Button>("Panel/Margin/Layout/Buttons/Apply").Pressed += Apply;
		GetNode<Button>("Panel/Margin/Layout/Buttons/Back").Pressed += Close;
		_displayConfirmation.Confirmed += KeepDisplaySettings;
		_displayConfirmation.Canceled += RevertDisplaySettings;
		_revertTimer.Timeout += RevertDisplaySettings;
		Visible = false;
	}

	public void Open()
	{
		LoadControls();
		Visible = true;
		_master.GrabFocus();
	}

	public void Close()
	{
		if (_displayBackup is not null)
		{
			RevertDisplaySettings();
		}
		Visible = false;
		EmitSignal(SignalName.Closed);
	}

	public void Apply()
	{
		SettingsManager manager = SettingsManager.Instance
			?? throw new System.InvalidOperationException("Settings manager is unavailable.");
		SettingsData previous = manager.Current.Copy();
		SettingsData next = ReadControls(previous);
		bool displayChanged = previous.Fullscreen != next.Fullscreen ||
			previous.Resolution != next.Resolution;
		manager.SetAndSave(next);
		if (displayChanged && DisplayServer.GetName() != "headless")
		{
			_displayBackup = previous;
			_revertTimer.Start(10.0);
			_displayConfirmation.PopupCentered();
		}
	}

	private void LoadControls()
	{
		SettingsData settings = (SettingsManager.Instance?.Current ?? new SettingsData()).Copy();
		_master.Value = settings.MasterVolume * 100.0f;
		_ambient.Value = settings.AmbientVolume * 100.0f;
		_effects.Value = settings.EffectsVolume * 100.0f;
		_sensitivity.Value = settings.MouseSensitivity;
		_windowMode.Select(settings.Fullscreen ? 1 : 0);
		_vsync.ButtonPressed = settings.VSync;
		_resolution.Select(FindResolution(settings.Resolution));
		_preset.Select((int)settings.GraphicsPreset);
	}

	private SettingsData ReadControls(SettingsData settings)
	{
		settings.MasterVolume = (float)_master.Value / 100.0f;
		settings.AmbientVolume = (float)_ambient.Value / 100.0f;
		settings.EffectsVolume = (float)_effects.Value / 100.0f;
		settings.MouseSensitivity = (float)_sensitivity.Value;
		settings.Fullscreen = _windowMode.Selected == 1;
		settings.VSync = _vsync.ButtonPressed;
		settings.Resolution = Resolutions[Mathf.Clamp(_resolution.Selected, 0, Resolutions.Length - 1)];
		settings.GraphicsPreset = (GraphicsPreset)Mathf.Clamp(
			_preset.Selected,
			0,
			System.Enum.GetValues<GraphicsPreset>().Length - 1);
		return settings;
	}

	private void KeepDisplaySettings()
	{
		_revertTimer.Stop();
		_displayBackup = null;
	}

	private void RevertDisplaySettings()
	{
		_revertTimer.Stop();
		if (_displayBackup is not null)
		{
			SettingsManager.Instance?.Restore(_displayBackup);
			_displayBackup = null;
			LoadControls();
		}
		_displayConfirmation.Hide();
	}

	private static int FindResolution(Vector2I resolution)
	{
		for (int index = 0; index < Resolutions.Length; index++)
		{
			if (Resolutions[index] == resolution)
			{
				return index;
			}
		}
		return 0;
	}
}
