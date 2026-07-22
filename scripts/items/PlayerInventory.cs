#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Items;

public partial class PlayerInventory : Node
{
	public const int SlotCount = 4;

	[Signal]
	public delegate void InventoryChangedEventHandler();

	private readonly ItemDefinition?[] _items = new ItemDefinition?[SlotCount];
	private readonly int[] _quantities = new int[SlotCount];

	public int Capacity => SlotCount;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Echo: true } || !@event.IsActionPressed("use_slot_1"))
		{
			return;
		}

		if (UseItemAt(0, GetParent()))
		{
			GetViewport().SetInputAsHandled();
		}
	}

	public bool AddItem(ItemDefinition item, int quantity = 1)
	{
		if (item is null || item.ItemId.IsEmpty || quantity <= 0)
		{
			return false;
		}

		int slot = FindItemSlot(item.ItemId);
		if (slot < 0)
		{
			slot = FindEmptySlot();
		}

		if (slot < 0)
		{
			return false;
		}

		_items[slot] ??= item;
		_quantities[slot] += quantity;
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public int GetQuantity(StringName itemId)
	{
		int slot = FindItemSlot(itemId);
		return slot >= 0 ? _quantities[slot] : 0;
	}

	public ItemDefinition? GetItemAt(int slot)
	{
		return IsValidSlot(slot) ? _items[slot] : null;
	}

	public int GetQuantityAt(int slot)
	{
		return IsValidSlot(slot) ? _quantities[slot] : 0;
	}

	public bool UseItemAt(int slot, Node user)
	{
		if (!IsValidSlot(slot) || _items[slot] is not ItemDefinition item || _quantities[slot] <= 0)
		{
			return false;
		}

		if (!item.Use(user))
		{
			return false;
		}

		_quantities[slot]--;
		if (_quantities[slot] == 0)
		{
			_items[slot] = null;
		}

		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	private int FindItemSlot(StringName itemId)
	{
		for (int slot = 0; slot < SlotCount; slot++)
		{
			if (_items[slot]?.ItemId == itemId)
			{
				return slot;
			}
		}

		return -1;
	}

	private int FindEmptySlot()
	{
		for (int slot = 0; slot < SlotCount; slot++)
		{
			if (_items[slot] is null)
			{
				return slot;
			}
		}

		return -1;
	}

	private static bool IsValidSlot(int slot)
	{
		return slot >= 0 && slot < SlotCount;
	}
}
