#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Settings;
using AshwoodCounty3DPrototype.UI;
using GodotFileAccess = Godot.FileAccess;

namespace AshwoodCounty3DPrototype.Tests;

public partial class SettingsValidation : Node
{
	private const string ValidationSettingsPath =
		"user://ashwood_county_settings_validation.cfg";

	public override async void _Ready()
	{
		SettingsManager? manager = SettingsManager.Instance;
		if (manager is null)
		{
			Fail("settings autoload is unavailable");
			return;
		}

		string originalPath = manager.SettingsFilePath;
		SettingsData original = manager.Current.Copy();
		bool originalFileExisted = GodotFileAccess.FileExists(originalPath);
		try
		{
			DeleteValidationSettings();
			manager.SettingsFilePath = ValidationSettingsPath;
			SettingsData expected = new()
			{
				MasterVolume = 0.42f,
				AmbientVolume = 0.36f,
				EffectsVolume = 0.27f,
				MouseSensitivity = 0.0042f,
				Fullscreen = false,
				VSync = false,
				Resolution = new Vector2I(1600, 900),
				GraphicsPreset = GraphicsPreset.High,
			};
			manager.SetAndSave(expected);
			Require(GodotFileAccess.FileExists(ValidationSettingsPath),
				"settings persist in a file separate from gameplay saves");
			manager.LoadSettings();
			manager.ApplySettings();
			AssertSettings(manager.Current, expected);
			Require(AudioServer.GetBusIndex("Ambient") >= 0 &&
				AudioServer.GetBusIndex("Effects") >= 0,
				"dedicated ambient and effects audio buses exist");
			Require(Mathf.Abs(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master")) -
				Mathf.LinearToDb(expected.MasterVolume)) < 0.05f &&
				Mathf.Abs(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Ambient")) -
				Mathf.LinearToDb(expected.AmbientVolume)) < 0.05f,
				"master and ambient volume settings apply safely");

			SettingsMenuController menu = (SettingsMenuController)GD.Load<PackedScene>(
				"res://scenes/ui/settings_menu.tscn").Instantiate();
			AddChild(menu);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			menu.Open();
			Require(menu.Visible &&
				Mathf.IsEqualApprox((float)menu.GetNode<HSlider>(
					"Panel/Margin/Layout/Grid/Sensitivity").Value, expected.MouseSensitivity),
				"reusable settings menu opens with persisted controls");
			Require(menu.GetNode<OptionButton>("Panel/Margin/Layout/Grid/Resolution").ItemCount == 3 &&
				menu.GetNode<OptionButton>("Panel/Margin/Layout/Grid/Preset").ItemCount == 3,
				"resolution and Low/Medium/High preset choices stay focused");
			Require(Mathf.IsEqualApprox(
				(float)menu.GetNode<Timer>("RevertTimer").WaitTime, 10.0f),
				"display changes have a ten-second confirmation/revert timer");

			menu.GetNode<HSlider>("Panel/Margin/Layout/Grid/Master").Value = 55.0;
			menu.GetNode<HSlider>("Panel/Margin/Layout/Grid/Sensitivity").Value = 0.0033;
			menu.GetNode<OptionButton>("Panel/Margin/Layout/Grid/Preset")
				.Select((int)GraphicsPreset.Low);
			menu.Apply();
			Require(Mathf.IsEqualApprox(manager.Current.MasterVolume, 0.55f) &&
				Mathf.IsEqualApprox(manager.Current.MouseSensitivity, 0.0033f) &&
				manager.Current.GraphicsPreset == GraphicsPreset.Low,
				"menu applies audio, controls and graphics settings");

			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			DirectionalLight3D light = world.GetNode<DirectionalLight3D>("DirectionalLight3D");
			Require(Mathf.IsEqualApprox(player.MouseSensitivity, 0.0033f) &&
				Mathf.IsEqualApprox(light.DirectionalShadowMaxDistance, 24.0f),
				"mouse sensitivity and Low graphics preset apply to gameplay");
			foreach (string playerName in new[] { "Wind", "ZombieGroans", "DayInsects", "NightCrickets" })
			{
				Require(world.GetNode<AudioStreamPlayer>($"AtmosphereAudio/{playerName}").Bus == "Ambient",
					$"{playerName} routes through the ambient setting");
			}

			bool closed = false;
			menu.Closed += () => closed = true;
			menu.Close();
			Require(closed && !menu.Visible, "settings closes cleanly for menu reuse");

			GD.Print("SETTINGS_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			Fail(exception.Message);
		}
		finally
		{
			manager.SettingsFilePath = originalPath;
			manager.Restore(original);
			if (!originalFileExisted)
			{
				DeleteFile(originalPath);
			}
			DeleteValidationSettings();
		}
	}

	private static void AssertSettings(SettingsData actual, SettingsData expected)
	{
		Require(Mathf.IsEqualApprox(actual.MasterVolume, expected.MasterVolume) &&
			Mathf.IsEqualApprox(actual.AmbientVolume, expected.AmbientVolume) &&
			Mathf.IsEqualApprox(actual.EffectsVolume, expected.EffectsVolume) &&
			Mathf.IsEqualApprox(actual.MouseSensitivity, expected.MouseSensitivity) &&
			actual.Fullscreen == expected.Fullscreen &&
			actual.VSync == expected.VSync &&
			actual.Resolution == expected.Resolution &&
			actual.GraphicsPreset == expected.GraphicsPreset,
			"all settings reload from the persistent config");
	}

	private static void DeleteValidationSettings()
	{
		DeleteFile(ValidationSettingsPath);
	}

	private static void DeleteFile(string path)
	{
		if (GodotFileAccess.FileExists(path))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
		}
	}

	private void Fail(string message)
	{
		DeleteValidationSettings();
		GD.PushError($"SETTINGS_VALIDATION: FAIL - {message}");
		GetTree().Quit(1);
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
