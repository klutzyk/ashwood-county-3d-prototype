#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Game;
using AshwoodCounty3DPrototype.Save;

namespace AshwoodCounty3DPrototype.UI;

public partial class MainMenuController : Control
{
	[Export] public string GameplayScenePath { get; set; } = "res://scenes/prototype_world.tscn";
	[Export] public string SaveFilePath { get; set; } = SaveGameManager.DefaultSaveFilePath;

	private Button _newGame = null!;
	private Button _continue = null!;
	private Button _settings = null!;
	private Control _settingsPanel = null!;
	private ConfirmationDialog _overwriteConfirmation = null!;

	public override void _Ready()
	{
		_newGame = GetNode<Button>("Layout/MenuPanel/Menu/NewGame");
		_continue = GetNode<Button>("Layout/MenuPanel/Menu/Continue");
		_settings = GetNode<Button>("Layout/MenuPanel/Menu/Settings");
		_settingsPanel = GetNode<Control>("SettingsPanel");
		_overwriteConfirmation = GetNode<ConfirmationDialog>("OverwriteConfirmation");

		_newGame.Pressed += RequestNewGame;
		_continue.Pressed += ContinueGame;
		_settings.Pressed += OpenSettings;
		GetNode<Button>("Layout/MenuPanel/Menu/Quit").Pressed += () => GetTree().Quit();
		GetNode<Button>("SettingsPanel/Panel/Layout/Back").Pressed += CloseSettings;
		_overwriteConfirmation.Confirmed += StartNewGame;

		_settingsPanel.Visible = false;
		RefreshContinueState();
		_newGame.GrabFocus();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void RefreshContinueState()
	{
		_continue.Disabled = !SaveGameManager.HasValidSaveFile(SaveFilePath);
	}

	public void RequestNewGame()
	{
		if (SaveGameManager.HasValidSaveFile(SaveFilePath))
		{
			_overwriteConfirmation.PopupCentered();
			return;
		}
		StartNewGame();
	}

	public void ContinueGame()
	{
		if (!SaveGameManager.HasValidSaveFile(SaveFilePath))
		{
			RefreshContinueState();
			return;
		}

		GameLaunchContext.RequestContinue();
		if (GetTree().ChangeSceneToFile(GameplayScenePath) != Error.Ok)
		{
			GameLaunchContext.RequestNewGame();
		}
	}

	private void StartNewGame()
	{
		GameLaunchContext.RequestNewGame();
		if (!SaveGameManager.DeleteSaveFile(SaveFilePath))
		{
			GD.PushWarning("Could not remove the existing save file.");
			RefreshContinueState();
			return;
		}
		GetTree().ChangeSceneToFile(GameplayScenePath);
	}

	private void OpenSettings()
	{
		_settingsPanel.Visible = true;
		GetNode<Button>("SettingsPanel/Panel/Layout/Back").GrabFocus();
	}

	private void CloseSettings()
	{
		_settingsPanel.Visible = false;
		_settings.GrabFocus();
	}
}
