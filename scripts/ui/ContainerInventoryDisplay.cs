#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
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
	private Label _containerLabel = null!;
	private Label _playerLabel = null!;
	private Label _details = null!;
	private Label _status = null!;
	private Button _takeButton = null!;
	private Button _storeButton = null!;
	private Button _useButton = null!;
	private ContainerInventory? _containerInventory;
	private PlayerInventory? _playerInventory;
	private ThirdPersonPlayer? _player;
	private PlayerHealth? _playerHealth;
	private PlayerNeeds? _playerNeeds;
	private int _selectedContainerIndex = -1;
	private int _selectedPlayerIndex = -1;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		_title = GetNode<Label>("Panel/Layout/Title");
		_containerItems = GetNode<ItemList>("Panel/Layout/Columns/ContainerColumn/ContainerItems");
		_playerItems = GetNode<ItemList>("Panel/Layout/Columns/PlayerColumn/PlayerItems");
		_containerLabel = GetNode<Label>("Panel/Layout/Columns/ContainerColumn/ContainerLabel");
		_playerLabel = GetNode<Label>("Panel/Layout/Columns/PlayerColumn/PlayerLabel");
		_details = GetNode<Label>("Panel/Layout/Details");
		_status = GetNode<Label>("Panel/Layout/Status");
		_takeButton = GetNode<Button>("Panel/Layout/Columns/Actions/Take");
		_storeButton = GetNode<Button>("Panel/Layout/Columns/Actions/Store");
		_useButton = GetNode<Button>("Panel/Layout/Columns/Actions/Use");

		_containerItems.ItemSelected += index => SelectContainerItem((int)index);
		_playerItems.ItemSelected += index => SelectPlayerItem((int)index);
		_containerItems.ItemActivated += index =>
		{
			SelectContainerItem((int)index);
			TakeSelected();
		};
		_playerItems.ItemActivated += index =>
		{
			SelectPlayerItem((int)index);
			UseSelected();
		};
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
		_playerHealth = player.GetNode<PlayerHealth>("Health");
		_playerNeeds = player.GetNode<PlayerNeeds>("Needs");
		_containerInventory.InventoryChanged += RefreshContainer;
		_playerInventory.InventoryChanged += RefreshPlayer;
		_playerInventory.ItemUsed += ShowStatus;
		_playerHealth.HealthChanged += OnPlayerConditionChanged;
		_playerNeeds.HungerChanged += OnPlayerConditionChanged;
		_playerNeeds.ThirstChanged += OnPlayerConditionChanged;
		_selectedContainerIndex = -1;
		_selectedPlayerIndex = -1;
		_title.Text = $"{container.DisplayName} Inventory";
		_containerLabel.Text = container.DisplayName;
		_status.Text = string.Empty;
		_details.Text = "Select an item to view its details.";
		Visible = true;
		_player.SetInventoryUiOpen(true);
		Input.MouseMode = Input.MouseModeEnum.Visible;
		RefreshContainer();
		RefreshPlayer();
		SelectInitialItem();
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
		_playerHealth = null;
		_playerNeeds = null;
	}

	public void SelectContainerItem(int index)
	{
		_selectedContainerIndex = _containerInventory is not null &&
			index >= 0 && index < _containerInventory.StackCount ? index : -1;
		if (_selectedContainerIndex >= 0)
		{
			_selectedPlayerIndex = -1;
			_playerItems.DeselectAll();
		}
		RefreshDetails();
		RefreshButtons();
	}

	public void SelectPlayerItem(int index)
	{
		_selectedPlayerIndex = _playerInventory is not null &&
			index >= 0 && index < _playerInventory.StackCount ? index : -1;
		if (_selectedPlayerIndex >= 0)
		{
			_selectedContainerIndex = -1;
			_containerItems.DeselectAll();
		}
		RefreshDetails();
		RefreshButtons();
	}

	public void TakeSelected()
	{
		if (_containerInventory is null || _playerInventory is null || _selectedContainerIndex < 0)
		{
			ShowStatus("Select a container item to take.");
			return;
		}

		ItemDefinition? item = _containerInventory.GetItemAt(_selectedContainerIndex);
		int quantity = _containerInventory.GetQuantityAt(_selectedContainerIndex);
		if (!_containerInventory.TransferStackTo(_selectedContainerIndex, _playerInventory))
		{
			ShowStatus("Player inventory is full.");
			Notify("Inventory full");
			return;
		}

		int destinationIndex = item is null ? -1 : _playerInventory.FindItemStack(item.ItemId);
		if (destinationIndex >= 0)
		{
			_playerItems.GrabFocus();
			_playerItems.Select(destinationIndex);
			SelectPlayerItem(destinationIndex);
		}
		ShowStatus($"Taken {item?.DisplayName} x{quantity}.");
		string destinationText = item?.ItemId == AntibioticsObjective.AntibioticsItemId
			? " (now in player inventory)"
			: string.Empty;
		Notify($"Item taken: {item?.DisplayName} x{quantity}{destinationText}");
	}

	public void StoreSelected()
	{
		if (_containerInventory is null || _playerInventory is null || _selectedPlayerIndex < 0)
		{
			ShowStatus("Select a player item to store.");
			return;
		}

		ItemDefinition? item = _playerInventory.GetItemAt(_selectedPlayerIndex);
		int quantity = _playerInventory.GetQuantityAt(_selectedPlayerIndex);
		if (!_playerInventory.TransferStackTo(_selectedPlayerIndex, _containerInventory))
		{
			ShowStatus("Item could not be stored.");
			return;
		}

		int destinationIndex = item is null ? -1 : _containerInventory.FindItemStack(item.ItemId);
		if (destinationIndex >= 0)
		{
			_containerItems.GrabFocus();
			_containerItems.Select(destinationIndex);
			SelectContainerItem(destinationIndex);
		}
		ShowStatus($"Stored {item?.DisplayName} x{quantity}.");
	}

	public void UseSelected()
	{
		if (_playerInventory is null || _player is null || _selectedPlayerIndex < 0)
		{
			ShowStatus("Select a player item to use.");
			return;
		}

		if (!_playerInventory.UseItemAt(_selectedPlayerIndex, _player))
		{
			ShowStatus("Item cannot be used right now.");
		}
	}

	private void RefreshContainer()
	{
		bool hadContainerSelection = _selectedContainerIndex >= 0;
		_containerItems.Clear();
		if (_containerInventory is null || _containerInventory.StackCount == 0)
		{
			_containerItems.AddItem("Empty");
			_containerItems.SetItemDisabled(0, true);
			_selectedContainerIndex = -1;
			if (hadContainerSelection && _playerInventory is not null && _playerInventory.StackCount > 0)
			{
				int movedItemIndex = _playerInventory.StackCount - 1;
				_playerItems.Select(movedItemIndex);
				SelectPlayerItem(movedItemIndex);
			}
			RefreshDetails();
			RefreshButtons();
			return;
		}

		for (int index = 0; index < _containerInventory.StackCount; index++)
		{
			ItemDefinition item = _containerInventory.GetItemAt(index)!;
			_containerItems.AddItem(
				$"{item.DisplayName} x{_containerInventory.GetQuantityAt(index)}",
				item.Icon);
		}
		_selectedContainerIndex = Mathf.Clamp(_selectedContainerIndex, -1, _containerInventory.StackCount - 1);
		if (_selectedContainerIndex >= 0)
		{
			_containerItems.Select(_selectedContainerIndex);
		}
		RefreshDetails();
		RefreshButtons();
	}

	private void RefreshPlayer()
	{
		bool hadPlayerSelection = _selectedPlayerIndex >= 0;
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
				: $"{slot + 1}. {item.DisplayName} x{_playerInventory.GetQuantityAt(slot)}",
				item?.Icon);
			_playerItems.SetItemDisabled(slot, item is null);
		}
		_playerLabel.Text = $"Player Inventory ({_playerInventory.StackCount}/{PlayerInventory.SlotCount} slots)";

		_selectedPlayerIndex = Mathf.Clamp(_selectedPlayerIndex, -1, _playerInventory.StackCount - 1);
		if (_selectedPlayerIndex >= 0)
		{
			_playerItems.Select(_selectedPlayerIndex);
		}
		else if (hadPlayerSelection && _containerInventory is not null && _containerInventory.StackCount > 0)
		{
			int movedItemIndex = _containerInventory.StackCount - 1;
			_containerItems.Select(movedItemIndex);
			SelectContainerItem(movedItemIndex);
		}
		RefreshDetails();
		RefreshButtons();
	}

	private void RefreshButtons()
	{
		ItemDefinition? containerItem = _containerInventory?.GetItemAt(_selectedContainerIndex);
		ItemDefinition? playerItem = _playerInventory?.GetItemAt(_selectedPlayerIndex);
		_takeButton.Disabled = containerItem is null || _playerInventory is null ||
			!_playerInventory.CanAdd(containerItem);
		_storeButton.Disabled = playerItem is null || _containerInventory is null ||
			!_containerInventory.CanAdd(playerItem);
		_useButton.Disabled = playerItem is null || _player is null || !playerItem.CanUse(_player);
	}

	private void RefreshDetails()
	{
		ItemDefinition? item = null;
		int quantity = 0;
		if (_selectedContainerIndex >= 0 && _containerInventory is not null)
		{
			item = _containerInventory.GetItemAt(_selectedContainerIndex);
			quantity = _containerInventory.GetQuantityAt(_selectedContainerIndex);
		}
		else if (_selectedPlayerIndex >= 0 && _playerInventory is not null)
		{
			item = _playerInventory.GetItemAt(_selectedPlayerIndex);
			quantity = _playerInventory.GetQuantityAt(_selectedPlayerIndex);
		}

		_details.Text = item is null
			? "Select an item to view its details."
			: $"{item.DisplayName}  x{quantity}\n{item.Description}\n" +
				$"Category: {FormatCategory(item.Category)}  Stack limit: {item.StackLimit}\n" +
				$"Effect: {item.EffectDescription}";
	}

	private static string FormatCategory(ItemCategory category)
	{
		return category == ItemCategory.CraftingMaterial
			? "Crafting Material"
			: category.ToString();
	}

	private void SelectInitialItem()
	{
		if (_containerInventory is not null && _containerInventory.StackCount > 0)
		{
			_containerItems.GrabFocus();
			_containerItems.Select(0);
			SelectContainerItem(0);
			return;
		}

		if (_playerInventory is not null && _playerInventory.StackCount > 0)
		{
			_playerItems.GrabFocus();
			_playerItems.Select(0);
			SelectPlayerItem(0);
			return;
		}

		GetNode<Button>("Panel/Layout/Close").GrabFocus();
	}

	private void ShowStatus(string message)
	{
		_status.Text = message;
	}

	private void Notify(string message)
	{
		if (GetTree().GetFirstNodeInGroup(GameplayNotificationDisplay.GroupName) is
			GameplayNotificationDisplay notifications)
		{
			notifications.QueueNotification(message);
		}
	}

	private void OnPlayerConditionChanged(float currentValue, float maximumValue)
	{
		RefreshButtons();
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
		if (_playerHealth is not null)
		{
			_playerHealth.HealthChanged -= OnPlayerConditionChanged;
		}
		if (_playerNeeds is not null)
		{
			_playerNeeds.HungerChanged -= OnPlayerConditionChanged;
			_playerNeeds.ThirstChanged -= OnPlayerConditionChanged;
		}
	}
}
