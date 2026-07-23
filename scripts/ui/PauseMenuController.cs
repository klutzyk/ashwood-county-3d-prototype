#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Game;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Save;

namespace AshwoodCounty3DPrototype.UI;

public partial class PauseMenuController : Control
{
	[Export] public NodePath PlayerPath { get; set; } = new("../../Player");
	[Export] public NodePath SaveManagerPath { get; set; } = new("../../SaveGameManager");
	[Export] public NodePath InventoryUiPath { get; set; } = new("../ContainerInventoryWindow");
	[Export] public string MainMenuScenePath { get; set; } = "res://scenes/ui/main_menu.tscn";

	private ThirdPersonPlayer _player = null!;
	private PlayerHealth _health = null!;
	private SaveGameManager _saveManager = null!;
	private ContainerInventoryDisplay _inventoryUi = null!;
	private Control _menuPanel = null!;
	private SettingsMenuController _settingsMenu = null!;
	private Button _resumeButton = null!;
	private bool _ownsPause;

	public bool IsOpen => Visible;
	public bool IsSettingsOpen => _settingsMenu.Visible;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		_player = GetNode<ThirdPersonPlayer>(PlayerPath);
		_health = _player.GetNode<PlayerHealth>("Health");
		_saveManager = GetNode<SaveGameManager>(SaveManagerPath);
		_inventoryUi = GetNode<ContainerInventoryDisplay>(InventoryUiPath);
		_menuPanel = GetNode<Control>("MenuPanel");
		_settingsMenu = GetNode<SettingsMenuController>("SettingsMenu");
		_resumeButton = GetNode<Button>("MenuPanel/Margin/Buttons/Resume");

		_resumeButton.Pressed += Close;
		GetNode<Button>("MenuPanel/Margin/Buttons/Save").Pressed += Save;
		GetNode<Button>("MenuPanel/Margin/Buttons/Load").Pressed += Load;
		GetNode<Button>("MenuPanel/Margin/Buttons/Settings").Pressed += OpenSettings;
		GetNode<Button>("MenuPanel/Margin/Buttons/MainMenu").Pressed += ReturnToMainMenu;
		GetNode<Button>("MenuPanel/Margin/Buttons/Quit").Pressed += () => GetTree().Quit();
		_settingsMenu.Closed += CloseSettings;
		Visible = false;
	}

	public override void _ExitTree()
	{
		if (_ownsPause && GetTree() is not null)
		{
			GetTree().Paused = false;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo ||
			(keyEvent.Keycode != Key.Escape && keyEvent.PhysicalKeycode != Key.Escape))
		{
			return;
		}

		if (_settingsMenu.Visible)
		{
			_settingsMenu.Close();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (Visible)
		{
			Close();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (CanOpen())
		{
			Open();
			GetViewport().SetInputAsHandled();
		}
	}

	public bool CanOpen()
	{
		return !_health.IsDead && !_inventoryUi.IsOpen && !GetTree().Paused;
	}

	public void Open()
	{
		if (!CanOpen())
		{
			return;
		}

		Visible = true;
		_menuPanel.Visible = true;
		_ownsPause = true;
		GetTree().Paused = true;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_resumeButton.GrabFocus();
	}

	public void Close()
	{
		if (!Visible)
		{
			return;
		}

		if (_settingsMenu.Visible)
		{
			_settingsMenu.Close();
		}
		Visible = false;
		_menuPanel.Visible = true;
		if (_ownsPause)
		{
			GetTree().Paused = false;
			_ownsPause = false;
		}
		if (!_health.IsDead && !_inventoryUi.IsOpen)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	public void Save()
	{
		_saveManager.SaveGame();
	}

	public void Load()
	{
		if (_saveManager.LoadGame())
		{
			Close();
		}
	}

	public void OpenSettings()
	{
		_menuPanel.Visible = false;
		_settingsMenu.Open();
	}

	public void ReturnToMainMenu()
	{
		GameLaunchContext.RequestNewGame();
		GetTree().Paused = false;
		_ownsPause = false;
		GetTree().ChangeSceneToFile(MainMenuScenePath);
	}

	private void CloseSettings()
	{
		_menuPanel.Visible = true;
		GetNode<Button>("MenuPanel/Margin/Buttons/Settings").GrabFocus();
	}
}
