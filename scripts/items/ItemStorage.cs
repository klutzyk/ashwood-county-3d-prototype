#nullable enable

using System;
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
		return TryAddItem(item, quantity, out _);
	}

	public bool TryAddItem(ItemDefinition item, int quantity, out int destinationStackIndex)
	{
		destinationStackIndex = -1;
		if (item is null || item.ItemId.IsEmpty || quantity <= 0 ||
			GetAddableQuantity(item) < quantity)
		{
			return false;
		}

		int remaining = quantity;
		int stackLimit = GetStackLimit(item);
		for (int stack = 0; stack < _items.Count && remaining > 0; stack++)
		{
			if (_items[stack].ItemId != item.ItemId || _quantities[stack] >= stackLimit)
			{
				continue;
			}

			int added = Mathf.Min(stackLimit - _quantities[stack], remaining);
			_quantities[stack] += added;
			remaining -= added;
			destinationStackIndex = stack;
		}

		while (remaining > 0)
		{
			int added = Mathf.Min(stackLimit, remaining);
			_items.Add(item);
			_quantities.Add(added);
			remaining -= added;
			destinationStackIndex = _items.Count - 1;
		}

		NotifyChanged();
		return true;
	}

	public bool CanAdd(ItemDefinition item)
	{
		return GetAddableQuantity(item) > 0;
	}

	public int GetAddableQuantity(ItemDefinition item)
	{
		if (item is null || item.ItemId.IsEmpty)
		{
			return 0;
		}

		int stackLimit = GetStackLimit(item);
		long addable = 0;
		foreach (int stack in FindItemStacks(item.ItemId))
		{
			addable += stackLimit - _quantities[stack];
		}

		if (Capacity <= 0)
		{
			return int.MaxValue;
		}

		addable += (long)(Capacity - StackCount) * stackLimit;
		return (int)Math.Min(addable, int.MaxValue);
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
		if (quantity <= 0 || GetQuantity(itemId) < quantity)
		{
			return false;
		}

		int remaining = quantity;
		for (int stack = _items.Count - 1; stack >= 0 && remaining > 0; stack--)
		{
			if (_items[stack].ItemId != itemId)
			{
				continue;
			}

			int removed = Mathf.Min(_quantities[stack], remaining);
			_quantities[stack] -= removed;
			remaining -= removed;
			if (_quantities[stack] == 0)
			{
				_items.RemoveAt(stack);
				_quantities.RemoveAt(stack);
			}
		}
		NotifyChanged();
		return true;
	}

	public void ClearItems()
	{
		if (_items.Count == 0)
		{
			return;
		}

		_items.Clear();
		_quantities.Clear();
		NotifyChanged();
	}

	public bool TransferStackTo(int stackIndex, ItemStorage target)
	{
		return TransferQuantityTo(stackIndex, GetQuantityAt(stackIndex), target, out _);
	}

	public bool TransferQuantityTo(
		int stackIndex,
		int quantity,
		ItemStorage target,
		out int destinationStackIndex)
	{
		destinationStackIndex = -1;
		if (!IsValidStack(stackIndex) || target is null || ReferenceEquals(this, target))
		{
			return false;
		}

		ItemDefinition item = _items[stackIndex];
		if (quantity <= 0 || quantity > _quantities[stackIndex] ||
			!target.TryAddItem(item, quantity, out destinationStackIndex))
		{
			return false;
		}

		return RemoveItemAt(stackIndex, quantity);
	}

	public int GetQuantity(StringName itemId)
	{
		int quantity = 0;
		foreach (int stack in FindItemStacks(itemId))
		{
			quantity += _quantities[stack];
		}
		return quantity;
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

	public int FindItemStack(StringName itemId)
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

	public int SplitStack(int stackIndex, int quantity)
	{
		if (!IsValidStack(stackIndex) || quantity <= 0 ||
			quantity >= _quantities[stackIndex] || IsFull)
		{
			return -1;
		}

		ItemDefinition item = _items[stackIndex];
		_quantities[stackIndex] -= quantity;
		int newStackIndex = stackIndex + 1;
		_items.Insert(newStackIndex, item);
		_quantities.Insert(newStackIndex, quantity);
		NotifyChanged();
		return newStackIndex;
	}

	public bool AddSavedStack(ItemDefinition item, int quantity)
	{
		if (item is null || item.ItemId.IsEmpty || quantity <= 0)
		{
			return false;
		}

		int stackLimit = GetStackLimit(item);
		int requiredStacks = Mathf.CeilToInt(quantity / (float)stackLimit);
		if (Capacity > 0 && StackCount + requiredStacks > Capacity)
		{
			return false;
		}

		int remaining = quantity;
		while (remaining > 0)
		{
			int added = Mathf.Min(stackLimit, remaining);
			_items.Add(item);
			_quantities.Add(added);
			remaining -= added;
		}
		NotifyChanged();
		return true;
	}

	public static int GetStackLimit(ItemDefinition item)
	{
		return Mathf.Max(item.StackLimit, 1);
	}

	private IEnumerable<int> FindItemStacks(StringName itemId)
	{
		for (int stack = 0; stack < _items.Count; stack++)
		{
			if (_items[stack].ItemId == itemId)
			{
				yield return stack;
			}
		}
	}

	private bool IsValidStack(int stackIndex)
	{
		return stackIndex >= 0 && stackIndex < _items.Count;
	}
}
