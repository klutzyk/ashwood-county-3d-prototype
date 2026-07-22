#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.UI;

public partial class ContainerInventoryDisplay : Control
{
	public static readonly StringName GroupName = new("container_inventory_ui");

	public bool IsOpen => Visible;
	public SearchableContainer? CurrentContainer { get; private set; }

	private Label _title = null!;
	private ItemList _containerItems = null!;
	private ItemList _playerItems = null!;
	private Label _status = null!;
	private Button _takeButton = null!;
	private Button _storeButton = null!;
	private Button _useButton = null!;
	private ContainerInventory? _containerInventory;
	private PlayerInventory? _playerInventory;
	private ThirdPersonPlayer? _player;
	private int _selectedContainerIndex = -1;
	private int _selectedPlayerIndex = -1;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		_title = GetNode<Label>("Panel/Layout/Title");
		_containerItems = GetNode<ItemList>("Panel/Layout/Columns/ContainerColumn/ContainerItems");
		_playerItems = GetNode<ItemList>("Panel/Layout/Columns/PlayerColumn/PlayerItems");
		_status = GetNode<Label>("Panel/Layout/Status");
		_takeButton = GetNode<Button>("Panel/Layout/Columns/Actions/Take");
		_storeButton = GetNode<Button>("Panel/Layout/Columns/Actions/Store");
		_useButton = GetNode<Button>("Panel/Layout/Columns/Actions/Use");

		_containerItems.ItemSelected += index => SelectContainerItem((int)index);
		_playerItems.ItemSelected += index => SelectPlayerItem((int)index);
		_takeButton.Pressed += TakeSelected;
		_storeButton.Pressed += StoreSelected;
		_useButton.Pressed += UseSelected;
		GetNode<Button>("Panel/Layout/Close").Pressed += Close;
		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Visible || @event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo ||
			(keyEvent.Keycode != Key.Escape && keyEvent.PhysicalKeycode != Key.Escape))
		{
			return;
		}

		Close();
		GetViewport().SetInputAsHandled();
	}

	public void Open(SearchableContainer container, Node interactor)
	{
		if (interactor is not ThirdPersonPlayer player)
		{
			return;
		}

		DisconnectInventories();
		CurrentContainer = container;
		_containerInventory = container.Inventory;
		_player = player;
		_playerInventory = player.GetNode<PlayerInventory>("Inventory");
		_containerInventory.InventoryChanged += RefreshContainer;
		_playerInventory.InventoryChanged += RefreshPlayer;
		_playerInventory.ItemUsed += ShowStatus;
		_selectedContainerIndex = -1;
		_selectedPlayerIndex = -1;
		_title.Text = container.DisplayName;
		_status.Text = string.Empty;
		Visible = true;
		_player.SetInventoryUiOpen(true);
		Input.MouseMode = Input.MouseModeEnum.Visible;
		RefreshContainer();
		RefreshPlayer();
	}

	public void Close()
	{
		if (!Visible)
		{
			return;
		}

		Visible = false;
		_player?.SetInventoryUiOpen(false);
		Input.MouseMode = Input.MouseModeEnum.Captured;
		DisconnectInventories();
		CurrentContainer = null;
		_containerInventory = null;
		_playerInventory = null;
		_player = null;
	}

	public void SelectContainerItem(int index)
	{
		_selectedContainerIndex = _containerInventory is not null &&
			index >= 0 && index < _containerInventory.StackCount ? index : -1;
		RefreshButtons();
	}

	public void SelectPlayerItem(int index)
	{
		_selectedPlayerIndex = _playerInventory is not null &&
			index >= 0 && index < _playerInventory.StackCount ? index : -1;
		RefreshButtons();
	}

	public void TakeSelected()
	{
		if (_containerInventory is null || _playerInventory is null || _selectedContainerIndex < 0)
		{
			return;
		}

		if (!_containerInventory.TransferStackTo(_selectedContainerIndex, _playerInventory))
		{
			ShowStatus("Player inventory is full.");
		}
	}

	public void StoreSelected()
	{
		if (_containerInventory is null || _playerInventory is null || _selectedPlayerIndex < 0)
		{
			return;
		}

		if (!_playerInventory.TransferStackTo(_selectedPlayerIndex, _containerInventory))
		{
			ShowStatus("Item could not be stored.");
		}
	}

	public void UseSelected()
	{
		if (_playerInventory is null || _player is null || _selectedPlayerIndex < 0)
		{
			return;
		}

		if (!_playerInventory.UseItemAt(_selectedPlayerIndex, _player))
		{
			ShowStatus("Item cannot be used right now.");
		}
	}

	private void RefreshContainer()
	{
		_containerItems.Clear();
		if (_containerInventory is null || _containerInventory.StackCount == 0)
		{
			_containerItems.AddItem("Empty");
			_containerItems.SetItemDisabled(0, true);
			_selectedContainerIndex = -1;
			RefreshButtons();
			return;
		}

		for (int index = 0; index < _containerInventory.StackCount; index++)
		{
			ItemDefinition item = _containerInventory.GetItemAt(index)!;
			_containerItems.AddItem($"{item.DisplayName} x{_containerInventory.GetQuantityAt(index)}");
		}
		_selectedContainerIndex = Mathf.Clamp(_selectedContainerIndex, -1, _containerInventory.StackCount - 1);
		if (_selectedContainerIndex >= 0)
		{
			_containerItems.Select(_selectedContainerIndex);
		}
		RefreshButtons();
	}

	private void RefreshPlayer()
	{
		_playerItems.Clear();
		if (_playerInventory is null)
		{
			return;
		}

		for (int slot = 0; slot < PlayerInventory.SlotCount; slot++)
		{
			ItemDefinition? item = _playerInventory.GetItemAt(slot);
			_playerItems.AddItem(item is null
				? $"{slot + 1}. Empty"
				: $"{slot + 1}. {item.DisplayName} x{_playerInventory.GetQuantityAt(slot)}");
			_playerItems.SetItemDisabled(slot, item is null);
		}

		_selectedPlayerIndex = Mathf.Clamp(_selectedPlayerIndex, -1, _playerInventory.StackCount - 1);
		if (_selectedPlayerIndex >= 0)
		{
			_playerItems.Select(_selectedPlayerIndex);
		}
		RefreshButtons();
	}

	private void RefreshButtons()
	{
		_takeButton.Disabled = _selectedContainerIndex < 0;
		_storeButton.Disabled = _selectedPlayerIndex < 0;
		_useButton.Disabled = _selectedPlayerIndex < 0;
	}

	private void ShowStatus(string message)
	{
		_status.Text = message;
	}

	private void DisconnectInventories()
	{
		if (_containerInventory is not null)
		{
			_containerInventory.InventoryChanged -= RefreshContainer;
		}
		if (_playerInventory is not null)
		{
			_playerInventory.InventoryChanged -= RefreshPlayer;
			_playerInventory.ItemUsed -= ShowStatus;
		}
	}
}
