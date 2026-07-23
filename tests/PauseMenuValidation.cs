#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class PauseMenuValidation : Node
{
	private const string ValidationSavePath = "user://ashwood_county_pause_validation.json";

	public override async void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		try
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			PauseMenuController pause = world.GetNode<PauseMenuController>(
				"PerformanceUI/PauseMenu");
			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerHealth health = player.GetNode<PlayerHealth>("Health");
			SaveGameManager saveManager = world.GetNode<SaveGameManager>("SaveGameManager");
			ContainerInventoryDisplay inventory = world.GetNode<ContainerInventoryDisplay>(
				"PerformanceUI/ContainerInventoryWindow");
			saveManager.SaveFilePath = ValidationSavePath;

			Require(!pause.IsOpen && pause.CanOpen(), "pause starts closed during live gameplay");
			pause.Open();
			Require(pause.IsOpen && GetTree().Paused && !player.CanUseWorldInteractions,
				"opening the menu pauses simulation and blocks gameplay interaction");
			Require(Input.MouseMode == Input.MouseModeEnum.Visible,
				"opening the menu releases the mouse");

			pause.OpenSettings();
			Require(pause.IsSettingsOpen && GetTree().Paused,
				"settings can be used without resuming gameplay");
			pause.GetNode<SettingsMenuController>("SettingsMenu").Close();
			Require(!pause.IsSettingsOpen &&
				pause.GetNode<Control>("MenuPanel").Visible,
				"closing settings returns to pause actions");

			Vector3 savedPosition = player.GlobalPosition;
			pause.Save();
			Require(SaveGameManager.HasValidSaveFile(ValidationSavePath),
				"pause-menu Save uses the existing valid save slot");
			player.GlobalPosition += new Vector3(3.0f, 0.0f, 0.0f);
			pause.Load();
			Require(!GetTree().Paused && !pause.IsOpen &&
				player.GlobalPosition.IsEqualApprox(savedPosition),
				"successful pause-menu Load restores state and resumes cleanly");

			inventory.Visible = true;
			Require(!pause.CanOpen(), "pause does not open over container inventory");
			inventory.Visible = false;
			health.RestoreState(0.0f);
			Require(!pause.CanOpen(), "pause does not open over the death overlay");
			health.RestoreState(health.MaximumHealth);

			pause.Open();
			pause.Close();
			Require(!GetTree().Paused && !pause.IsOpen,
				"Resume restores gameplay simulation state");
			if (DisplayServer.GetName() != "headless")
			{
				Require(Input.MouseMode == Input.MouseModeEnum.Captured,
					"Resume restores mouse capture");
			}

			GD.Print("PAUSE_MENU_VALIDATION: PASS");
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GetTree().Paused = false;
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GD.PushError($"PAUSE_MENU_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
