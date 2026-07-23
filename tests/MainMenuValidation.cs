#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Game;
using AshwoodCounty3DPrototype.Save;
using AshwoodCounty3DPrototype.UI;
using GodotFileAccess = Godot.FileAccess;

namespace AshwoodCounty3DPrototype.Tests;

public partial class MainMenuValidation : Node
{
	private const string ValidationSavePath = "user://ashwood_county_menu_validation.json";

	public override async void _Ready()
	{
		try
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			Require(ProjectSettings.GetSetting("application/run/main_scene").AsString() ==
				"res://scenes/ui/main_menu.tscn",
				"project launches through the project-owned main menu");

			MainMenuController menu = CreateMenu();
			AddChild(menu);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			Button newGame = menu.GetNode<Button>("Layout/MenuPanel/Menu/NewGame");
			Button continueButton = menu.GetNode<Button>("Layout/MenuPanel/Menu/Continue");
			Button settings = menu.GetNode<Button>("Layout/MenuPanel/Menu/Settings");
			Control settingsPanel = menu.GetNode<Control>("SettingsPanel");
			ConfirmationDialog confirmation = menu.GetNode<ConfirmationDialog>("OverwriteConfirmation");
			Require(continueButton.Disabled, "Continue is disabled when no valid save exists");
			Require(newGame.HasFocus(), "keyboard navigation starts on New Game");
			settings.EmitSignal(Button.SignalName.Pressed);
			Require(settingsPanel.Visible, "Settings opens from the main menu");
			settingsPanel.GetNode<Button>("Panel/Layout/Back").EmitSignal(Button.SignalName.Pressed);
			Require(!settingsPanel.Visible && settings.HasFocus(),
				"Settings returns focus to the main menu");
			menu.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			SaveGameManager manager = world.GetNode<SaveGameManager>("SaveGameManager");
			manager.SaveFilePath = ValidationSavePath;
			Require(manager.SaveGame() && SaveGameManager.HasValidSaveFile(ValidationSavePath),
				"a gameplay save is recognized as valid by the menu");
			world.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			menu = CreateMenu();
			AddChild(menu);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			continueButton = menu.GetNode<Button>("Layout/MenuPanel/Menu/Continue");
			confirmation = menu.GetNode<ConfirmationDialog>("OverwriteConfirmation");
			Require(!continueButton.Disabled, "Continue is enabled for a valid save");
			menu.RequestNewGame();
			Require(confirmation.Visible && GodotFileAccess.FileExists(ValidationSavePath),
				"New Game asks before overwriting an existing valid save");
			confirmation.Hide();

			using (GodotFileAccess invalid = GodotFileAccess.Open(
				ValidationSavePath, GodotFileAccess.ModeFlags.Write)!)
			{
				invalid.StoreString("{\"Version\":1,\"invalid\":true}");
			}
			menu.RefreshContinueState();
			Require(continueButton.Disabled,
				"Continue disables when an existing file is malformed");
			Require(SaveGameManager.DeleteSaveFile(ValidationSavePath),
				"new-game cleanup can remove save and temporary files");

			GameLaunchContext.RequestContinue();
			Require(GameLaunchContext.ConsumeContinueRequest() &&
				!GameLaunchContext.ConsumeContinueRequest(),
				"Continue launch intent is consumed exactly once");
			GameLaunchContext.RequestContinue();
			GameLaunchContext.RequestNewGame();
			Require(!GameLaunchContext.ConsumeContinueRequest(),
				"New Game clears prior runtime launch state");

			GD.Print("MAIN_MENU_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			SaveGameManager.DeleteSaveFile(ValidationSavePath);
			GD.PushError($"MAIN_MENU_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static MainMenuController CreateMenu()
	{
		MainMenuController menu = (MainMenuController)GD.Load<PackedScene>(
			"res://scenes/ui/main_menu.tscn").Instantiate();
		menu.SaveFilePath = ValidationSavePath;
		return menu;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
