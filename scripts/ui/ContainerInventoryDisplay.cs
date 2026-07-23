#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.UI;

public partial class ContainerInventoryDisplay : Control
{
	private enum QuantityAction
	{
		None,
		Take,
		Store,
		SplitContainer,
		SplitPlayer,
	}

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
	private Button _takeQuantityButton = null!;
	private Button _storeQuantityButton = null!;
	private Button _splitButton = null!;
	private Button _useButton = null!;
	private ConfirmationDialog _quantityDialog = null!;
	private SpinBox _quantity = null!;
	private ContainerInventory? _containerInventory;
	private PlayerInventory? _playerInventory;
	private ThirdPersonPlayer? _player;
	private PlayerHealth? _playerHealth;
	private PlayerNeeds? _playerNeeds;
	private int _selectedContainerIndex = -1;
	private int _selectedPlayerIndex = -1;
	private QuantityAction _quantityAction;

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
		_takeQuantityButton = GetNode<Button>("Panel/Layout/Columns/Actions/TakeQuantity");
		_storeQuantityButton = GetNode<Button>("Panel/Layout/Columns/Actions/StoreQuantity");
		_splitButton = GetNode<Button>("Panel/Layout/Columns/Actions/Split");
		_useButton = GetNode<Button>("Panel/Layout/Columns/Actions/Use");
		_quantityDialog = GetNode<ConfirmationDialog>("QuantityDialog");
		_quantity = _quantityDialog.GetNode<SpinBox>("Quantity");

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
		_takeQuantityButton.Pressed += () => OpenQuantityDialog(QuantityAction.Take);
		_storeQuantityButton.Pressed += () => OpenQuantityDialog(QuantityAction.Store);
		_splitButton.Pressed += OpenSplitDialog;
		_useButton.Pressed += UseSelected;
		_quantityDialog.Confirmed += ConfirmQuantityAction;
		_quantityDialog.Canceled += () => _quantityAction = QuantityAction.None;
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

		int quantity = _containerInventory.GetQuantityAt(_selectedContainerIndex);
		TakeSelectedQuantity(quantity);
	}

	public bool TakeSelectedQuantity(int quantity)
	{
		if (_containerInventory is null || _playerInventory is null ||
			_selectedContainerIndex < 0)
		{
			ShowStatus("Select a container item to take.");
			return false;
		}

		ItemDefinition? item = _containerInventory.GetItemAt(_selectedContainerIndex);
		if (item is null || !_containerInventory.TransferQuantityTo(
			_selectedContainerIndex,
			quantity,
			_playerInventory,
			out int destinationIndex))
		{
			ShowStatus("Player inventory is full.");
			Notify("Inventory full");
			return false;
		}

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
		return true;
	}

	public void StoreSelected()
	{
		if (_containerInventory is null || _playerInventory is null || _selectedPlayerIndex < 0)
		{
			ShowStatus("Select a player item to store.");
			return;
		}

		int quantity = _playerInventory.GetQuantityAt(_selectedPlayerIndex);
		StoreSelectedQuantity(quantity);
	}

	public bool StoreSelectedQuantity(int quantity)
	{
		if (_containerInventory is null || _playerInventory is null || _selectedPlayerIndex < 0)
		{
			ShowStatus("Select a player item to store.");
			return false;
		}

		ItemDefinition? item = _playerInventory.GetItemAt(_selectedPlayerIndex);
		if (item is null || !_playerInventory.TransferQuantityTo(
			_selectedPlayerIndex,
			quantity,
			_containerInventory,
			out int destinationIndex))
		{
			ShowStatus("Item could not be stored.");
			return false;
		}

		if (destinationIndex >= 0)
		{
			_containerItems.GrabFocus();
			_containerItems.Select(destinationIndex);
			SelectContainerItem(destinationIndex);
		}
		ShowStatus($"Stored {item?.DisplayName} x{quantity}.");
		Notify($"Item stored: {item?.DisplayName} x{quantity}");
		return true;
	}

	public bool SplitSelectedStack(int quantity)
	{
		ItemStorage? storage;
		ItemList list;
		int sourceIndex;
		bool isContainer = _selectedContainerIndex >= 0;
		if (isContainer)
		{
			storage = _containerInventory;
			list = _containerItems;
			sourceIndex = _selectedContainerIndex;
		}
		else
		{
			storage = _playerInventory;
			list = _playerItems;
			sourceIndex = _selectedPlayerIndex;
		}

		int splitIndex = storage?.SplitStack(sourceIndex, quantity) ?? -1;
		if (splitIndex < 0)
		{
			ShowStatus("Stack could not be split.");
			return false;
		}

		list.GrabFocus();
		list.Select(splitIndex);
		if (isContainer)
		{
			SelectContainerItem(splitIndex);
		}
		else
		{
			SelectPlayerItem(splitIndex);
		}
		ShowStatus($"Split off x{quantity}.");
		return true;
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
			UpdateContainerTitle();
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
		UpdateContainerTitle();
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

	private void UpdateContainerTitle()
	{
		if (CurrentContainer is null || _containerInventory is null)
		{
			return;
		}

		_title.Text = _containerInventory.StackCount == 0
			? $"{CurrentContainer.DisplayName} • Empty"
			: $"{CurrentContainer.DisplayName} • {_containerInventory.StackCount} " +
				$"stack{(_containerInventory.StackCount == 1 ? string.Empty : "s")}";
	}

	private void RefreshButtons()
	{
		ItemDefinition? containerItem = _containerInventory?.GetItemAt(_selectedContainerIndex);
		ItemDefinition? playerItem = _playerInventory?.GetItemAt(_selectedPlayerIndex);
		_takeButton.Disabled = containerItem is null || _playerInventory is null ||
			!_playerInventory.CanAdd(containerItem);
		_storeButton.Disabled = playerItem is null || _containerInventory is null ||
			!_containerInventory.CanAdd(playerItem);
		_takeQuantityButton.Disabled = _takeButton.Disabled ||
			_containerInventory!.GetQuantityAt(_selectedContainerIndex) <= 1;
		_storeQuantityButton.Disabled = _storeButton.Disabled ||
			_playerInventory!.GetQuantityAt(_selectedPlayerIndex) <= 1;
		ItemStorage? selectedStorage = containerItem is not null
			? _containerInventory
			: playerItem is not null ? _playerInventory : null;
		int selectedIndex = containerItem is not null
			? _selectedContainerIndex
			: _selectedPlayerIndex;
		_splitButton.Disabled = selectedStorage is null || selectedStorage.IsFull ||
			selectedStorage.GetQuantityAt(selectedIndex) <= 1;
		_useButton.Disabled = playerItem is null || _player is null || !playerItem.CanUse(_player);
	}

	private void OpenQuantityDialog(QuantityAction action)
	{
		ItemDefinition? item;
		int available;
		int destinationCapacity;
		switch (action)
		{
			case QuantityAction.Take:
				item = _containerInventory?.GetItemAt(_selectedContainerIndex);
				available = _containerInventory?.GetQuantityAt(_selectedContainerIndex) ?? 0;
				destinationCapacity = item is null ? 0 : _playerInventory?.GetAddableQuantity(item) ?? 0;
				break;
			case QuantityAction.Store:
				item = _playerInventory?.GetItemAt(_selectedPlayerIndex);
				available = _playerInventory?.GetQuantityAt(_selectedPlayerIndex) ?? 0;
				destinationCapacity = item is null ? 0 : _containerInventory?.GetAddableQuantity(item) ?? 0;
				break;
			default:
				return;
		}

		int maximum = Mathf.Min(available, destinationCapacity);
		if (item is null || maximum <= 0)
		{
			ShowStatus("No quantity can be transferred.");
			return;
		}
		ShowQuantityDialog(action, maximum, $"Transfer {item.DisplayName}");
	}

	private void OpenSplitDialog()
	{
		bool isContainer = _selectedContainerIndex >= 0;
		ItemStorage? storage = isContainer ? _containerInventory : _playerInventory;
		int index = isContainer ? _selectedContainerIndex : _selectedPlayerIndex;
		ItemDefinition? item = storage?.GetItemAt(index);
		int maximum = (storage?.GetQuantityAt(index) ?? 0) - 1;
		if (item is null || maximum <= 0 || storage!.IsFull)
		{
			ShowStatus("Stack could not be split.");
			return;
		}
		ShowQuantityDialog(
			isContainer ? QuantityAction.SplitContainer : QuantityAction.SplitPlayer,
			maximum,
			$"Split {item.DisplayName}");
	}

	private void ShowQuantityDialog(QuantityAction action, int maximum, string title)
	{
		_quantityAction = action;
		_quantityDialog.Title = title;
		_quantityDialog.DialogText = $"Choose 1 to {maximum}.";
		_quantity.MaxValue = maximum;
		_quantity.Value = 1;
		_quantityDialog.PopupCentered();
		_quantity.GetLineEdit().GrabFocus();
		_quantity.GetLineEdit().SelectAll();
	}

	private void ConfirmQuantityAction()
	{
		int quantity = Mathf.RoundToInt(_quantity.Value);
		switch (_quantityAction)
		{
			case QuantityAction.Take:
				TakeSelectedQuantity(quantity);
				break;
			case QuantityAction.Store:
				StoreSelectedQuantity(quantity);
				break;
			case QuantityAction.SplitContainer:
			case QuantityAction.SplitPlayer:
				SplitSelectedStack(quantity);
				break;
		}
		_quantityAction = QuantityAction.None;
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
