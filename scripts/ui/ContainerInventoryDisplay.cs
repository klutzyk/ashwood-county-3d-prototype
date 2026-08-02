#nullable enable

using System;
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

	[Export] public NodePath PlayerPath { get; set; } = new("../../Player");
	[Export] public float UiSoundVolumeDb { get; set; } = -10.0f;
	[Export] public AudioStream? OpenSound { get; set; }
	[Export] public AudioStream? TransferSound { get; set; }
	[Export] public AudioStream? ErrorSound { get; set; }
	[Export] public AudioStream? UseSound { get; set; }
	[Export] public AudioStream? CloseSound { get; set; }

	public bool IsOpen => Visible;
	public bool IsContainerOpen => Visible && CurrentContainer is not null;
	public SearchableContainer? CurrentContainer { get; private set; }

	private Label _title = null!;
	private ItemList _containerItems = null!;
	private ItemList _playerItems = null!;
	private Label _containerLabel = null!;
	private Label _playerLabel = null!;
	private Control _containerColumn = null!;
	private Control _actionsColumn = null!;
	private Control? _fieldActions;
	private Label? _transferLabel;
	private Label _details = null!;
	private Label _status = null!;
	private Button _takeButton = null!;
	private Button _storeButton = null!;
	private Button _takeQuantityButton = null!;
	private Button _storeQuantityButton = null!;
	private Button _splitButton = null!;
	private Button _useButton = null!;
	private Button? _takeAllButton;
	private Button? _assignQuickButton;
	private Button? _fieldSplitButton;
	private Button? _fieldUseButton;
	private Button? _fieldAssignQuickButton;
	private Label? _inputHint;
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
	private AudioStreamPlayer _uiAudio = null!;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		EnsureControllerInventoryBinding();
		LoadDefaultSounds();
		_uiAudio = new AudioStreamPlayer
		{
			Name = "InventoryUiAudio",
			VolumeDb = UiSoundVolumeDb,
			MaxPolyphony = 4,
			Bus = "Effects",
		};
		AddChild(_uiAudio);
		_title = GetNode<Label>("Panel/Layout/Title");
		_containerItems = GetNode<ItemList>("Panel/Layout/Columns/ContainerColumn/ContainerItems");
		_playerItems = GetNode<ItemList>("Panel/Layout/Columns/PlayerColumn/PlayerItems");
		_containerLabel = GetNode<Label>("Panel/Layout/Columns/ContainerColumn/ContainerLabel");
		_playerLabel = GetNode<Label>("Panel/Layout/Columns/PlayerColumn/PlayerLabel");
		_containerColumn = GetNode<Control>("Panel/Layout/Columns/ContainerColumn");
		_actionsColumn = GetNode<Control>("Panel/Layout/Columns/Actions");
		_fieldActions = GetNodeOrNull<Control>("Panel/Layout/FieldActions");
		_transferLabel = GetNodeOrNull<Label>("Panel/Layout/Columns/Actions/TransferLabel");
		_details = GetNode<Label>("Panel/Layout/Details");
		_status = GetNode<Label>("Panel/Layout/Status");
		_takeButton = GetNode<Button>("Panel/Layout/Columns/Actions/Take");
		_storeButton = GetNode<Button>("Panel/Layout/Columns/Actions/Store");
		_takeQuantityButton = GetNode<Button>("Panel/Layout/Columns/Actions/TakeQuantity");
		_storeQuantityButton = GetNode<Button>("Panel/Layout/Columns/Actions/StoreQuantity");
		_splitButton = GetNode<Button>("Panel/Layout/Columns/Actions/Split");
		_useButton = GetNode<Button>("Panel/Layout/Columns/Actions/Use");
		_takeAllButton = GetNodeOrNull<Button>("Panel/Layout/Columns/Actions/TakeAll");
		_assignQuickButton = GetNodeOrNull<Button>("Panel/Layout/Columns/Actions/AssignQuick");
		_fieldSplitButton = GetNodeOrNull<Button>("Panel/Layout/FieldActions/FieldSplit");
		_fieldUseButton = GetNodeOrNull<Button>("Panel/Layout/FieldActions/FieldUse");
		_fieldAssignQuickButton = GetNodeOrNull<Button>(
			"Panel/Layout/FieldActions/FieldAssignQuick");
		_inputHint = GetNodeOrNull<Label>("Panel/Layout/InputHint");
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
			if (CurrentContainer is null)
			{
				UseSelected();
			}
			else
			{
				StoreSelected();
			}
		};
		_containerItems.FocusEntered += () => SetActiveColumn(true);
		_playerItems.FocusEntered += () => SetActiveColumn(false);
		_takeButton.Pressed += TakeSelected;
		_storeButton.Pressed += StoreSelected;
		_takeQuantityButton.Pressed += () => OpenQuantityDialog(QuantityAction.Take);
		_storeQuantityButton.Pressed += () => OpenQuantityDialog(QuantityAction.Store);
		_splitButton.Pressed += OpenSplitDialog;
		_useButton.Pressed += UseSelected;
		if (_takeAllButton is not null)
		{
			_takeAllButton.Pressed += TakeAll;
		}
		if (_assignQuickButton is not null)
		{
			_assignQuickButton.Pressed += AssignSelectedToFirstQuickSlot;
		}
		if (_fieldSplitButton is not null)
		{
			_fieldSplitButton.Pressed += OpenSplitDialog;
		}
		if (_fieldUseButton is not null)
		{
			_fieldUseButton.Pressed += UseSelected;
		}
		if (_fieldAssignQuickButton is not null)
		{
			_fieldAssignQuickButton.Pressed += AssignSelectedToFirstQuickSlot;
		}
		_quantityDialog.Confirmed += ConfirmQuantityAction;
		_quantityDialog.Canceled += () => _quantityAction = QuantityAction.None;
		GetNode<Button>("Panel/Layout/Close").Pressed += Close;
		Visible = false;
	}

	public override void _ExitTree()
	{
		DisconnectInventories();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Echo: true })
		{
			return;
		}

		if (!Visible)
		{
			if (@event.IsActionPressed("toggle_inventory"))
			{
				OpenBackpack();
				GetViewport().SetInputAsHandled();
			}
			return;
		}

		if (@event.IsActionPressed("toggle_inventory"))
		{
			Close();
			GetViewport().SetInputAsHandled();
			return;
		}

		bool isRawEscape = @event is InputEventKey keyEvent && keyEvent.Pressed &&
			(keyEvent.Keycode == Key.Escape || keyEvent.PhysicalKeycode == Key.Escape);
		if (@event.IsActionPressed("ui_cancel") || isRawEscape)
		{
			if (_quantityDialog.Visible)
			{
				_quantityDialog.Hide();
				_quantityAction = QuantityAction.None;
			}
			else
			{
				Close();
			}
			GetViewport().SetInputAsHandled();
			return;
		}

		if (_selectedPlayerIndex < 0 || _quantityDialog.Visible)
		{
			return;
		}

		string[] quickSlotActions =
		{
			"use_slot_1",
			"use_slot_2",
			"use_slot_3",
			"use_slot_4",
		};
		for (int slot = 0; slot < quickSlotActions.Length; slot++)
		{
			if (!@event.IsActionPressed(quickSlotActions[slot]))
			{
				continue;
			}
			AssignSelectedToQuickSlot(slot);
			GetViewport().SetInputAsHandled();
			return;
		}
	}

	public void OpenBackpack()
	{
		ThirdPersonPlayer? player = GetNodeOrNull<ThirdPersonPlayer>(PlayerPath);
		if (player is null)
		{
			return;
		}

		OpenInternal(null, player);
	}

	public void Open(SearchableContainer container, Node interactor)
	{
		if (container is null || interactor is not ThirdPersonPlayer player)
		{
			return;
		}

		OpenInternal(container, player);
	}

	private void OpenInternal(SearchableContainer? container, ThirdPersonPlayer player)
	{
		DisconnectInventories();
		CurrentContainer = container;
		_containerInventory = container?.Inventory;
		_player = player;
		_playerInventory = player.GetNode<PlayerInventory>("Inventory");
		_playerHealth = player.GetNode<PlayerHealth>("Health");
		_playerNeeds = player.GetNode<PlayerNeeds>("Needs");
		if (_containerInventory is not null)
		{
			_containerInventory.InventoryChanged += RefreshContainer;
		}
		_playerInventory.InventoryChanged += RefreshPlayer;
		_playerInventory.ItemUsed += OnItemUsed;
		_playerHealth.HealthChanged += OnPlayerConditionChanged;
		_playerNeeds.HungerChanged += OnPlayerConditionChanged;
		_playerNeeds.ThirstChanged += OnPlayerConditionChanged;
		_selectedContainerIndex = -1;
		_selectedPlayerIndex = -1;
		ConfigureModeVisibility(container is not null);
		_status.Text = container is null
			? "Field pack ready. Select a supply to inspect, use or assign."
			: "Double-click or press Accept to quick-move a stack.";
		_details.Text = "Select a supply to inspect its weight and field use.";
		Visible = true;
		_player.SetInventoryUiOpen(true);
		Input.MouseMode = Input.MouseModeEnum.Visible;
		PlayUiSound(OpenSound);
		RefreshContainer();
		RefreshPlayer();
		UpdateInputHint();
		SelectInitialItem();
		QueueInventoryLayoutRefresh();
	}

	public void Close()
	{
		if (!Visible)
		{
			return;
		}

		_quantityDialog.Hide();
		_quantityAction = QuantityAction.None;
		Visible = false;
		_player?.SetInventoryUiOpen(false);
		Input.MouseMode = Input.MouseModeEnum.Captured;
		PlayUiSound(CloseSound);
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
		_selectedContainerIndex = _containerInventory?.GetItemAt(index) is not null ? index : -1;
		if (_selectedContainerIndex >= 0)
		{
			_selectedPlayerIndex = -1;
			_playerItems.DeselectAll();
		}
		RefreshDetails();
		RefreshButtons();
		RefreshSelectionPresentation();
	}

	public void SelectPlayerItem(int index)
	{
		_selectedPlayerIndex = _playerInventory?.GetItemAt(index) is not null ? index : -1;
		if (_selectedPlayerIndex >= 0)
		{
			_selectedContainerIndex = -1;
			_containerItems.DeselectAll();
		}
		RefreshDetails();
		RefreshButtons();
		RefreshSelectionPresentation();
	}

	public void TakeSelected()
	{
		if (_containerInventory is null || _playerInventory is null || _selectedContainerIndex < 0)
		{
			ShowStatus("Select a container item to take.");
			return;
		}

		ItemDefinition? item = _containerInventory.GetItemAt(_selectedContainerIndex);
		int requested = _containerInventory.GetQuantityAt(_selectedContainerIndex);
		if (item is null || requested <= 0)
		{
			ShowStatus("That stack is no longer available.");
			return;
		}

		int moved = _containerInventory.TransferUpTo(
			_selectedContainerIndex,
			requested,
			_playerInventory,
			out int destinationIndex);
		if (moved <= 0)
		{
			ShowTransferFailure(item, _playerInventory, playerDestination: true);
			return;
		}

		FollowPlayerSelection(destinationIndex);
		int leftBehind = requested - moved;
		ShowStatus(leftBehind == 0
			? $"Taken {item.DisplayName} x{moved}."
			: $"Taken {item.DisplayName} x{moved}; {leftBehind} left behind (pack limit). ");
		string destinationText = item.ItemId == AntibioticsObjective.AntibioticsItemId
			? " (now in player inventory)"
			: string.Empty;
		Notify($"Item taken: {item.DisplayName} x{moved}{destinationText}");
		PlayUiSound(TransferSound);
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
			if (item is not null && quantity > 0 &&
				quantity > _playerInventory.GetAddableQuantity(item) &&
				_playerInventory.GetAddableQuantity(item) > 0)
			{
				ShowStatus($"Only {_playerInventory.GetAddableQuantity(item)} can fit right now.");
			}
			else if (item is not null)
			{
				ShowTransferFailure(item, _playerInventory, playerDestination: true);
			}
			else
			{
				ShowStatus("Item could not be taken.");
			}
			return false;
		}

		FollowPlayerSelection(destinationIndex);
		ShowStatus($"Taken {item.DisplayName} x{quantity}.");
		string destinationText = item.ItemId == AntibioticsObjective.AntibioticsItemId
			? " (now in player inventory)"
			: string.Empty;
		Notify($"Item taken: {item.DisplayName} x{quantity}{destinationText}");
		PlayUiSound(TransferSound);
		return true;
	}

	public void TakeAll()
	{
		if (_containerInventory is null || _playerInventory is null ||
			_containerInventory.StackCount == 0)
		{
			ShowStatus("There is nothing to take.");
			return;
		}

		int moved = _containerInventory.TransferAllPossibleTo(
			_playerInventory,
			out int movedStacks);
		if (moved <= 0)
		{
			ShowStatus("Nothing fits. Free a slot or reduce carried weight.");
			Notify("Inventory full");
			return;
		}

		int firstPlayerSlot = FirstOccupiedSlot(_playerInventory);
		FollowPlayerSelection(firstPlayerSlot);
		bool leftBehind = _containerInventory.StackCount > 0;
		ShowStatus($"Taken {moved} item{(moved == 1 ? string.Empty : "s")} from " +
			$"{movedStacks} stack{(movedStacks == 1 ? string.Empty : "s")}." +
			(leftBehind ? " Pack limits left some supplies behind." : string.Empty));
		Notify($"Loot collected: {moved} item{(moved == 1 ? string.Empty : "s")}");
		PlayUiSound(TransferSound);
	}

	public void StoreSelected()
	{
		if (_containerInventory is null || _playerInventory is null || _selectedPlayerIndex < 0)
		{
			ShowStatus(CurrentContainer is null
				? "Open a world container before storing items."
				: "Select a player item to store.");
			return;
		}

		ItemDefinition? item = _playerInventory.GetItemAt(_selectedPlayerIndex);
		int requested = _playerInventory.GetQuantityAt(_selectedPlayerIndex);
		if (item is null || requested <= 0)
		{
			ShowStatus("That stack is no longer available.");
			return;
		}

		int moved = _playerInventory.TransferUpTo(
			_selectedPlayerIndex,
			requested,
			_containerInventory,
			out int destinationIndex);
		if (moved <= 0)
		{
			ShowTransferFailure(item, _containerInventory, playerDestination: false);
			return;
		}

		FollowContainerSelection(destinationIndex);
		int kept = requested - moved;
		ShowStatus(kept == 0
			? $"Stored {item.DisplayName} x{moved}."
			: $"Stored {item.DisplayName} x{moved}; {kept} kept in your pack.");
		Notify($"Item stored: {item.DisplayName} x{moved}");
		PlayUiSound(TransferSound);
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
			if (item is not null)
			{
				ShowTransferFailure(item, _containerInventory, playerDestination: false);
			}
			else
			{
				ShowStatus("Item could not be stored.");
			}
			return false;
		}

		FollowContainerSelection(destinationIndex);
		ShowStatus($"Stored {item.DisplayName} x{quantity}.");
		Notify($"Item stored: {item.DisplayName} x{quantity}");
		PlayUiSound(TransferSound);
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
			ShowStatus("Stack could not be split. A free slot is required.");
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
		ShowStatus($"Split off x{quantity} into a separate slot.");
		PlayUiSound(TransferSound);
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

	public bool AssignSelectedToQuickSlot(int quickSlot)
	{
		if (_playerInventory is null || _selectedPlayerIndex < 0 ||
			quickSlot < 0 || quickSlot >= PlayerInventory.QuickSlotCount)
		{
			ShowStatus("Select a pack item before assigning a quick slot.");
			return false;
		}

		if (_selectedPlayerIndex == quickSlot)
		{
			ShowStatus($"Already assigned to quick slot {quickSlot + 1}.");
			return true;
		}

		ItemDefinition? item = _playerInventory.GetItemAt(_selectedPlayerIndex);
		if (item is null || !_playerInventory.SwapStacks(_selectedPlayerIndex, quickSlot))
		{
			ShowStatus("Quick slot assignment failed.");
			return false;
		}

		_playerItems.GrabFocus();
		_playerItems.Select(quickSlot);
		SelectPlayerItem(quickSlot);
		ShowStatus($"{item.DisplayName} assigned to quick slot {quickSlot + 1} (key {quickSlot + 5}).");
		PlayUiSound(UseSound);
		return true;
	}

	private void AssignSelectedToFirstQuickSlot()
	{
		if (_playerInventory is null || _selectedPlayerIndex < 0)
		{
			ShowStatus("Select a pack item before assigning a quick slot.");
			return;
		}

		for (int slot = 0; slot < PlayerInventory.QuickSlotCount; slot++)
		{
			if (_playerInventory.GetItemAt(slot) is null)
			{
				AssignSelectedToQuickSlot(slot);
				return;
			}
		}
		ShowStatus("Quick slots are occupied. Press 5-8 to choose one to swap.");
	}

	private void RefreshContainer()
	{
		_containerItems.Clear();
		if (_containerInventory is null)
		{
			_containerItems.AddItem("NO CONTAINER OPEN  |  Search the world to compare and store supplies.");
			_containerItems.SetItemDisabled(0, true);
			_containerItems.SetItemCustomFgColor(0, new Color(0.38f, 0.4f, 0.35f));
			_selectedContainerIndex = -1;
			_containerLabel.Text = "WORLD STORAGE";
			UpdateWindowTitle();
			RefreshDetails();
			RefreshButtons();
			return;
		}

		if (_containerInventory.StackCount == 0)
		{
			_containerItems.AddItem("EMPTY  |  Nothing remains in this container.");
			_containerItems.SetItemDisabled(0, true);
			_containerItems.SetItemCustomFgColor(0, new Color(0.38f, 0.4f, 0.35f));
			_selectedContainerIndex = -1;
		}
		else
		{
			foreach (int index in _containerInventory.GetOccupiedSlotIndices())
			{
				ItemDefinition item = _containerInventory.GetItemAt(index)!;
				int listIndex = _containerItems.AddItem(
					FormatStackLine(item, _containerInventory.GetQuantityAt(index)),
					item.Icon);
				_containerItems.SetItemMetadata(listIndex, index);
				_containerItems.SetItemTooltip(listIndex, BuildTooltip(item));
				_containerItems.SetItemCustomFgColor(
					listIndex,
					new Color(0.86f, 0.87f, 0.78f));
				_containerItems.SetItemCustomBgColor(
					listIndex,
					index % 2 == 0
						? new Color(0.045f, 0.055f, 0.05f, 0.88f)
						: new Color(0.03f, 0.04f, 0.036f, 0.78f));
			}
		}

		UpdateContainerLabel();
		UpdateWindowTitle();
		if (_selectedContainerIndex >= 0 &&
			_containerInventory.GetItemAt(_selectedContainerIndex) is not null)
		{
			int listIndex = FindListIndexForStorageSlot(_containerItems, _selectedContainerIndex);
			if (listIndex >= 0)
			{
				_containerItems.Select(listIndex);
			}
		}
		else
		{
			_selectedContainerIndex = -1;
		}
		RefreshDetails();
		RefreshButtons();
		RefreshSelectionPresentation();
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
			string slotName = slot < PlayerInventory.QuickSlotCount
				? $"Q{slot + 1} [{slot + 5}]"
				: $"FIELD {slot - PlayerInventory.QuickSlotCount + 1}";
			_playerItems.AddItem(item is null
				? $"{slotName}  |  --"
				: $"{slotName}  |  {FormatStackLine(item, _playerInventory.GetQuantityAt(slot))}",
				item?.Icon);
			_playerItems.SetItemMetadata(slot, slot);
			_playerItems.SetItemDisabled(slot, item is null);
			if (item is not null)
			{
				_playerItems.SetItemTooltip(slot, BuildTooltip(item));
				_playerItems.SetItemCustomFgColor(slot, new Color(0.88f, 0.89f, 0.81f));
				_playerItems.SetItemCustomBgColor(
					slot,
					slot < PlayerInventory.QuickSlotCount
						? new Color(0.055f, 0.07f, 0.065f, 0.95f)
						: new Color(0.035f, 0.045f, 0.04f, 0.82f));
			}
			else
			{
				_playerItems.SetItemCustomFgColor(slot, new Color(0.28f, 0.3f, 0.27f));
				_playerItems.SetItemCustomBgColor(
					slot,
					new Color(0.018f, 0.024f, 0.022f, 0.62f));
			}
		}
		UpdatePlayerLabel();

		if (_selectedPlayerIndex >= 0 &&
			_playerInventory.GetItemAt(_selectedPlayerIndex) is not null)
		{
			_playerItems.Select(_selectedPlayerIndex);
		}
		else
		{
			_selectedPlayerIndex = -1;
		}
		RefreshDetails();
		RefreshButtons();
		RefreshSelectionPresentation();
	}

	private void UpdateWindowTitle()
	{
		if (CurrentContainer is null || _containerInventory is null)
		{
			_title.Text = "FIELD PACK";
			return;
		}

		_title.Text = _containerInventory.StackCount == 0
			? $"{CurrentContainer.DisplayName.ToUpperInvariant()}  |  EMPTY"
			: CurrentContainer.DisplayName.ToUpperInvariant();
	}

	private void UpdateContainerLabel()
	{
		if (CurrentContainer is null || _containerInventory is null)
		{
			return;
		}

		string capacity = _containerInventory.Capacity > 0
			? $"{_containerInventory.StackCount}/{_containerInventory.Capacity} SLOTS"
			: $"{_containerInventory.StackCount} STACKS";
		_containerLabel.Text = $"{CurrentContainer.DisplayName.ToUpperInvariant()}  |  " +
			$"{capacity}  |  {_containerInventory.TotalItemCount} ITEMS";
	}

	private void UpdatePlayerLabel()
	{
		if (_playerInventory is null)
		{
			return;
		}

		_playerLabel.Text = $"FIELD PACK  |  {_playerInventory.StackCount}/" +
			$"{_playerInventory.Capacity} SLOTS  |  {_playerInventory.TotalWeightKg:0.0}/" +
			$"{_playerInventory.WeightCapacityKg:0.0} KG";
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
			(_containerInventory?.GetQuantityAt(_selectedContainerIndex) ?? 0) <= 1;
		_storeQuantityButton.Disabled = _storeButton.Disabled ||
			(_playerInventory?.GetQuantityAt(_selectedPlayerIndex) ?? 0) <= 1;
		ItemStorage? selectedStorage = containerItem is not null
			? _containerInventory
			: playerItem is not null ? _playerInventory : null;
		int selectedIndex = containerItem is not null
			? _selectedContainerIndex
			: _selectedPlayerIndex;
		_splitButton.Disabled = selectedStorage is null || selectedStorage.IsFull ||
			selectedStorage.GetQuantityAt(selectedIndex) <= 1;
		_useButton.Disabled = playerItem is null || _player is null || !playerItem.CanUse(_player);
		if (_takeAllButton is not null)
		{
			_takeAllButton.Disabled = _containerInventory is null || _playerInventory is null ||
				!AnyItemCanMove(_containerInventory, _playerInventory);
		}
		if (_assignQuickButton is not null)
		{
			_assignQuickButton.Disabled = playerItem is null ||
				_selectedPlayerIndex < PlayerInventory.QuickSlotCount;
		}
		if (_fieldSplitButton is not null)
		{
			_fieldSplitButton.Disabled = _splitButton.Disabled;
		}
		if (_fieldUseButton is not null)
		{
			_fieldUseButton.Disabled = _useButton.Disabled;
		}
		if (_fieldAssignQuickButton is not null)
		{
			_fieldAssignQuickButton.Disabled = playerItem is null ||
				_selectedPlayerIndex < PlayerInventory.QuickSlotCount;
		}
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
			ShowStatus("Stack could not be split. A free slot is required.");
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
		_quantityDialog.DialogText = $"Choose an amount from 1 to {maximum}.";
		_quantity.MaxValue = maximum;
		_quantity.Value = maximum;
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
		string location = string.Empty;
		if (_selectedContainerIndex >= 0 && _containerInventory is not null)
		{
			item = _containerInventory.GetItemAt(_selectedContainerIndex);
			quantity = _containerInventory.GetQuantityAt(_selectedContainerIndex);
			location = CurrentContainer?.DisplayName ?? "Container";
		}
		else if (_selectedPlayerIndex >= 0 && _playerInventory is not null)
		{
			item = _playerInventory.GetItemAt(_selectedPlayerIndex);
			quantity = _playerInventory.GetQuantityAt(_selectedPlayerIndex);
			location = _selectedPlayerIndex < PlayerInventory.QuickSlotCount
				? $"Quick Slot {_selectedPlayerIndex + 1}"
				: $"Field Slot {_selectedPlayerIndex - PlayerInventory.QuickSlotCount + 1}";
		}

		_details.Text = item is null
			? "Select a supply to inspect its weight and field use."
			: $"{item.DisplayName}  x{quantity}  |  {location}\n" +
				$"{item.Description}\n" +
				$"{FormatCategory(item.Category).ToUpperInvariant()}  |  " +
				$"{item.UnitWeightKg:0.00} kg each  |  " +
				$"{item.UnitWeightKg * quantity:0.00} kg stack  |  " +
				$"Stack limit {item.StackLimit}\n" +
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
			int firstSlot = FirstOccupiedSlot(_containerInventory);
			int firstListIndex = FindListIndexForStorageSlot(_containerItems, firstSlot);
			_containerItems.GrabFocus();
			_containerItems.Select(firstListIndex);
			SelectContainerItem(firstSlot);
			return;
		}

		if (_playerInventory is not null && _playerInventory.StackCount > 0)
		{
			int firstSlot = FirstOccupiedSlot(_playerInventory);
			_playerItems.GrabFocus();
			_playerItems.Select(firstSlot);
			SelectPlayerItem(firstSlot);
			return;
		}

		GetNode<Button>("Panel/Layout/Close").GrabFocus();
	}

	private void FollowPlayerSelection(int storageSlot)
	{
		if (storageSlot < 0 || _playerInventory?.GetItemAt(storageSlot) is null)
		{
			return;
		}
		_playerItems.GrabFocus();
		_playerItems.Select(storageSlot);
		SelectPlayerItem(storageSlot);
	}

	private void FollowContainerSelection(int storageSlot)
	{
		if (storageSlot < 0 || _containerInventory?.GetItemAt(storageSlot) is null)
		{
			return;
		}
		int listIndex = FindListIndexForStorageSlot(_containerItems, storageSlot);
		if (listIndex < 0)
		{
			return;
		}
		_containerItems.GrabFocus();
		_containerItems.Select(listIndex);
		SelectContainerItem(storageSlot);
	}

	private void ShowTransferFailure(
		ItemDefinition item,
		ItemStorage destination,
		bool playerDestination)
	{
		if (destination.GetSlotAddableQuantity(item) <= 0)
		{
			ShowStatus(playerDestination
				? "Player inventory is full."
				: "Container has no free compatible slot.");
			if (playerDestination)
			{
				Notify("Inventory full");
			}
			PlayUiSound(ErrorSound);
			return;
		}

		if (destination.GetWeightAddableQuantity(item) <= 0)
		{
			ShowStatus(playerDestination
				? $"Too heavy. Pack limit is {destination.WeightCapacityKg:0.0} kg."
				: "That container cannot support more weight.");
			if (playerDestination)
			{
				Notify("Too heavy");
			}
			PlayUiSound(ErrorSound);
			return;
		}

		ShowStatus(playerDestination
			? "Item could not be taken."
			: "Item could not be stored.");
		PlayUiSound(ErrorSound);
	}

	private void UpdateInputHint()
	{
		if (_inputHint is null)
		{
			return;
		}
		_inputHint.Text = CurrentContainer is null
			? "ACCEPT: USE   |   5-8: ASSIGN QUICK SLOT   |   I / VIEW: CLOSE   |   ESC / B: BACK"
			: "ACCEPT: QUICK MOVE   |   TAB / D-PAD: NAVIGATE   |   5-8: ASSIGN QUICK SLOT   |   ESC / B: CLOSE";
	}

	private void ConfigureModeVisibility(bool hasContainer)
	{
		_containerColumn.Visible = hasContainer;
		_actionsColumn.Visible = hasContainer;
		if (_fieldActions is not null)
		{
			_fieldActions.Visible = !hasContainer;
		}

		if (!hasContainer && _fieldUseButton is not null)
		{
			_playerItems.FocusNeighborBottom = _playerItems.GetPathTo(_fieldUseButton);
			_fieldSplitButton!.FocusNeighborTop = _fieldSplitButton.GetPathTo(_playerItems);
			_fieldUseButton.FocusNeighborTop = _fieldUseButton.GetPathTo(_playerItems);
			_fieldAssignQuickButton!.FocusNeighborTop =
				_fieldAssignQuickButton.GetPathTo(_playerItems);
		}
		else
		{
			_playerItems.FocusNeighborBottom = new NodePath(string.Empty);
		}
		SetActiveColumn(hasContainer);
	}

	private void SetActiveColumn(bool containerActive)
	{
		bool showContainerFocus = containerActive && CurrentContainer is not null;
		_containerLabel.AddThemeColorOverride(
			"font_color",
			showContainerFocus
				? new Color(0.98f, 0.79f, 0.38f)
				: new Color(0.58f, 0.6f, 0.53f));
		_playerLabel.AddThemeColorOverride(
			"font_color",
			!showContainerFocus
				? new Color(0.98f, 0.79f, 0.38f)
				: new Color(0.58f, 0.6f, 0.53f));
	}

	private void RefreshSelectionPresentation()
	{
		for (int listIndex = 0; listIndex < _containerItems.ItemCount; listIndex++)
		{
			Variant metadata = _containerItems.GetItemMetadata(listIndex);
			if (metadata.VariantType != Variant.Type.Int)
			{
				continue;
			}
			int storageSlot = metadata.AsInt32();
			bool selected = storageSlot == _selectedContainerIndex;
			_containerItems.SetItemCustomFgColor(
				listIndex,
				selected
					? new Color(1.0f, 0.93f, 0.72f)
					: new Color(0.86f, 0.87f, 0.78f));
			_containerItems.SetItemCustomBgColor(
				listIndex,
				selected
					? new Color(0.29f, 0.26f, 0.11f, 0.98f)
					: storageSlot % 2 == 0
						? new Color(0.045f, 0.055f, 0.05f, 0.88f)
						: new Color(0.03f, 0.04f, 0.036f, 0.78f));
		}

		for (int slot = 0; slot < _playerItems.ItemCount; slot++)
		{
			ItemDefinition? item = _playerInventory?.GetItemAt(slot);
			bool selected = item is not null && slot == _selectedPlayerIndex;
			_playerItems.SetItemCustomFgColor(
				slot,
				selected
					? new Color(1.0f, 0.93f, 0.72f)
					: item is null
						? new Color(0.28f, 0.3f, 0.27f)
						: new Color(0.88f, 0.89f, 0.81f));
			_playerItems.SetItemCustomBgColor(
				slot,
				selected
					? new Color(0.29f, 0.26f, 0.11f, 0.98f)
					: item is null
						? new Color(0.018f, 0.024f, 0.022f, 0.62f)
						: slot < PlayerInventory.QuickSlotCount
							? new Color(0.055f, 0.07f, 0.065f, 0.95f)
							: new Color(0.035f, 0.045f, 0.04f, 0.82f));
		}
	}

	private void QueueInventoryLayoutRefresh()
	{
		_containerItems.ForceUpdateListSize();
		_playerItems.ForceUpdateListSize();
		GetNode<Container>("Panel/Layout").QueueSort();
		QueueSubtreeRedraw(this);
		Callable.From(() =>
		{
			if (!IsInsideTree())
			{
				return;
			}
			GetNode<Container>("Panel/Layout").QueueSort();
			QueueSubtreeRedraw(this);
		}).CallDeferred();
	}

	private static void QueueSubtreeRedraw(Node node)
	{
		if (node is CanvasItem canvasItem)
		{
			canvasItem.QueueRedraw();
		}
		foreach (Node child in node.GetChildren())
		{
			QueueSubtreeRedraw(child);
		}
	}

	private static string FormatStackLine(ItemDefinition item, int quantity)
	{
		return $"{item.DisplayName}  x{quantity}/{item.StackLimit}   |   " +
			$"{item.UnitWeightKg * quantity:0.00} KG";
	}

	private static string BuildTooltip(ItemDefinition item)
	{
		return $"{item.DisplayName}\n{item.Description}\n{item.EffectDescription}";
	}

	private static bool AnyItemCanMove(ItemStorage source, ItemStorage destination)
	{
		foreach (int slot in source.GetOccupiedSlotIndices())
		{
			ItemDefinition? item = source.GetItemAt(slot);
			if (item is not null && destination.CanAdd(item))
			{
				return true;
			}
		}
		return false;
	}

	private static void EnsureControllerInventoryBinding()
	{
		if (!InputMap.HasAction("toggle_inventory"))
		{
			InputMap.AddAction("toggle_inventory");
		}
		foreach (InputEvent inputEvent in InputMap.ActionGetEvents("toggle_inventory"))
		{
			if (inputEvent is InputEventJoypadButton { ButtonIndex: JoyButton.Back })
			{
				return;
			}
		}
		InputMap.ActionAddEvent("toggle_inventory", new InputEventJoypadButton
		{
			ButtonIndex = JoyButton.Back,
		});
	}

	private void LoadDefaultSounds()
	{
		OpenSound ??= GD.Load<AudioStream>(
			"res://assets/third_party/audio/kenney_rpg_audio/Audio/handleSmallLeather.ogg");
		TransferSound ??= GD.Load<AudioStream>(
			"res://assets/third_party/audio/kenney_rpg_audio/Audio/handleCoins2.ogg");
		ErrorSound ??= GD.Load<AudioStream>(
			"res://assets/third_party/audio/kenney_rpg_audio/Audio/metalClick.ogg");
		UseSound ??= GD.Load<AudioStream>(
			"res://assets/third_party/audio/kenney_rpg_audio/Audio/bookPlace2.ogg");
		CloseSound ??= GD.Load<AudioStream>(
			"res://assets/third_party/audio/kenney_rpg_audio/Audio/handleSmallLeather2.ogg");
	}

	private void PlayUiSound(AudioStream? sound)
	{
		if (sound is null || !IsInstanceValid(_uiAudio))
		{
			return;
		}
		_uiAudio.Stream = sound;
		_uiAudio.VolumeDb = UiSoundVolumeDb;
		_uiAudio.Play();
	}

	private void OnItemUsed(string message)
	{
		ShowStatus(message);
		PlayUiSound(UseSound);
	}

	private static int FirstOccupiedSlot(ItemStorage storage)
	{
		foreach (int slot in storage.GetOccupiedSlotIndices())
		{
			return slot;
		}
		return -1;
	}

	private static int FindListIndexForStorageSlot(ItemList list, int storageSlot)
	{
		for (int index = 0; index < list.ItemCount; index++)
		{
			Variant metadata = list.GetItemMetadata(index);
			if (metadata.VariantType == Variant.Type.Int && metadata.AsInt32() == storageSlot)
			{
				return index;
			}
		}
		return -1;
	}

	private void ShowStatus(string message)
	{
		_status.Text = message.TrimEnd();
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
		if (_playerHealth is not null && _playerHealth.CurrentHealth <= 0.0f)
		{
			Close();
			return;
		}
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
			_playerInventory.ItemUsed -= OnItemUsed;
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
