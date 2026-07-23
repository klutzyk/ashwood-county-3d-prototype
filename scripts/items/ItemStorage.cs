#nullable enable

using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Items;

public abstract partial class ItemStorage : Node
{
	[Signal]
	public delegate void InventoryChangedEventHandler();

	private readonly List<ItemDefinition> _items = new();
	private readonly List<int> _quantities = new();

	public virtual int Capacity => 0;
	public int StackCount => _items.Count;
	public bool IsFull => Capacity > 0 && StackCount >= Capacity;

	public bool AddItem(ItemDefinition item, int quantity = 1)
	{
		if (item is null || item.ItemId.IsEmpty || quantity <= 0)
		{
			return false;
		}

		int existingStack = FindItemStack(item.ItemId);
		if (existingStack >= 0)
		{
			_quantities[existingStack] += quantity;
			NotifyChanged();
			return true;
		}

		if (IsFull)
		{
			return false;
		}

		_items.Add(item);
		_quantities.Add(quantity);
		NotifyChanged();
		return true;
	}

	public bool CanAdd(ItemDefinition item)
	{
		return item is not null && !item.ItemId.IsEmpty &&
			(FindItemStack(item.ItemId) >= 0 || !IsFull);
	}

	public bool RemoveItemAt(int stackIndex, int quantity = 1)
	{
		if (!IsValidStack(stackIndex) || quantity <= 0 || quantity > _quantities[stackIndex])
		{
			return false;
		}

		_quantities[stackIndex] -= quantity;
		if (_quantities[stackIndex] == 0)
		{
			_items.RemoveAt(stackIndex);
			_quantities.RemoveAt(stackIndex);
		}

		NotifyChanged();
		return true;
	}

	public bool RemoveItem(StringName itemId, int quantity = 1)
	{
		int stackIndex = FindItemStack(itemId);
		return stackIndex >= 0 && RemoveItemAt(stackIndex, quantity);
	}

	public bool TransferStackTo(int stackIndex, ItemStorage target)
	{
		if (!IsValidStack(stackIndex) || target is null || ReferenceEquals(this, target))
		{
			return false;
		}

		ItemDefinition item = _items[stackIndex];
		int quantity = _quantities[stackIndex];
		if (!target.CanAdd(item) || !target.AddItem(item, quantity))
		{
			return false;
		}

		return RemoveItemAt(stackIndex, quantity);
	}

	public int GetQuantity(StringName itemId)
	{
		int stack = FindItemStack(itemId);
		return stack >= 0 ? _quantities[stack] : 0;
	}

	public ItemDefinition? GetItemAt(int stackIndex)
	{
		return IsValidStack(stackIndex) ? _items[stackIndex] : null;
	}

	public int GetQuantityAt(int stackIndex)
	{
		return IsValidStack(stackIndex) ? _quantities[stackIndex] : 0;
	}

	protected void NotifyChanged()
	{
		EmitSignal(SignalName.InventoryChanged);
	}

	private int FindItemStack(StringName itemId)
	{
		for (int stack = 0; stack < _items.Count; stack++)
		{
			if (_items[stack].ItemId == itemId)
			{
				return stack;
			}
		}

		return -1;
	}

	private bool IsValidStack(int stackIndex)
	{
		return stackIndex >= 0 && stackIndex < _items.Count;
	}
}
