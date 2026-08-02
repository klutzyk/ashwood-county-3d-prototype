#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Items;

public readonly record struct ItemStackRestoreData(
	ItemDefinition Definition,
	int Quantity,
	int SlotIndex = -1);

public abstract partial class ItemStorage : Node
{
	[Signal]
	public delegate void InventoryChangedEventHandler();

	private readonly List<ItemDefinition?> _items = new();
	private readonly List<int> _quantities = new();

	public virtual int Capacity => 0;
	public virtual float WeightCapacityKg => 0.0f;
	protected virtual bool PreserveSlotPositions => false;
	// Container inventories override this for version-1 saves created before
	// physical slot limits existed. Runtime additions still obey Capacity.
	protected virtual bool AllowRestorePastCapacity => false;

	public int StackCount
	{
		get
		{
			int count = 0;
			for (int slot = 0; slot < _items.Count; slot++)
			{
				if (_items[slot] is not null)
				{
					count++;
				}
			}
			return count;
		}
	}

	public int StorageSlotCount => _items.Count;
	public int TotalItemCount
	{
		get
		{
			int count = 0;
			foreach (int slot in GetOccupiedSlotIndices())
			{
				count += _quantities[slot];
			}
			return count;
		}
	}

	public float TotalWeightKg
	{
		get
		{
			float weight = 0.0f;
			foreach (int slot in GetOccupiedSlotIndices())
			{
				weight += Mathf.Max(_items[slot]!.UnitWeightKg, 0.0f) * _quantities[slot];
			}
			return weight;
		}
	}

	public bool IsFull => Capacity > 0 && StackCount >= Capacity;
	public bool HasCapacityOverflow => Capacity > 0 && StackCount > Capacity;
	public int OverflowStackCount => Capacity > 0
		? Mathf.Max(StackCount - Capacity, 0)
		: 0;
	public bool HasWeightLimit => WeightCapacityKg > 0.0f;

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

		AddItemInternal(item, quantity, out destinationStackIndex);
		NotifyChanged();
		return true;
	}

	public bool CanAdd(ItemDefinition item)
	{
		return GetAddableQuantity(item) > 0;
	}

	public int GetAddableQuantity(ItemDefinition item)
	{
		return Math.Min(GetSlotAddableQuantity(item), GetWeightAddableQuantity(item));
	}

	public int GetSlotAddableQuantity(ItemDefinition item)
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

		addable += (long)Mathf.Max(Capacity - StackCount, 0) * stackLimit;
		return (int)Math.Min(addable, int.MaxValue);
	}

	public int GetWeightAddableQuantity(ItemDefinition item)
	{
		if (item is null || item.ItemId.IsEmpty)
		{
			return 0;
		}

		float unitWeight = Mathf.Max(item.UnitWeightKg, 0.0f);
		if (!HasWeightLimit || unitWeight <= 0.0f)
		{
			return int.MaxValue;
		}

		float remainingWeight = Mathf.Max(WeightCapacityKg - TotalWeightKg, 0.0f);
		double addable = Math.Floor((remainingWeight + 0.0001f) / unitWeight);
		return (int)Math.Min(addable, int.MaxValue);
	}

	public bool RemoveItemAt(int stackIndex, int quantity = 1)
	{
		if (!IsValidStack(stackIndex) || quantity <= 0 || quantity > _quantities[stackIndex])
		{
			return false;
		}

		RemoveItemAtInternal(stackIndex, quantity);
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
			if (_items[stack]?.ItemId != itemId)
			{
				continue;
			}

			int removed = Mathf.Min(_quantities[stack], remaining);
			RemoveItemAtInternal(stack, removed);
			remaining -= removed;
		}
		NotifyChanged();
		return true;
	}

	public void ClearItems()
	{
		if (StackCount == 0)
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

		ItemDefinition item = _items[stackIndex]!;
		if (quantity <= 0 || quantity > _quantities[stackIndex] ||
			target.GetAddableQuantity(item) < quantity)
		{
			return false;
		}

		// Mutate both sides before either signal is emitted. UI, objectives and save
		// listeners can therefore never observe a duplicated intermediate state.
		target.AddItemInternal(item, quantity, out destinationStackIndex);
		RemoveItemAtInternal(stackIndex, quantity);
		target.NotifyChanged();
		NotifyChanged();
		return true;
	}

	public int TransferUpTo(
		int stackIndex,
		int requestedQuantity,
		ItemStorage target,
		out int destinationStackIndex)
	{
		destinationStackIndex = -1;
		if (!IsValidStack(stackIndex) || requestedQuantity <= 0 ||
			target is null || ReferenceEquals(this, target))
		{
			return 0;
		}

		ItemDefinition item = _items[stackIndex]!;
		int movedQuantity = Mathf.Min(
			requestedQuantity,
			Mathf.Min(_quantities[stackIndex], target.GetAddableQuantity(item)));
		if (movedQuantity <= 0)
		{
			return 0;
		}

		target.AddItemInternal(item, movedQuantity, out destinationStackIndex);
		RemoveItemAtInternal(stackIndex, movedQuantity);
		target.NotifyChanged();
		NotifyChanged();
		return movedQuantity;
	}

	public int TransferAllPossibleTo(ItemStorage target, out int fullyMovedStacks)
	{
		fullyMovedStacks = 0;
		if (target is null || ReferenceEquals(this, target))
		{
			return 0;
		}

		int movedItems = 0;
		int slot = 0;
		while (slot < _items.Count)
		{
			ItemDefinition? item = _items[slot];
			if (item is null)
			{
				slot++;
				continue;
			}

			int originalQuantity = _quantities[slot];
			int moved = Mathf.Min(originalQuantity, target.GetAddableQuantity(item));
			if (moved <= 0)
			{
				slot++;
				continue;
			}

			target.AddItemInternal(item, moved, out _);
			RemoveItemAtInternal(slot, moved);
			movedItems += moved;
			if (moved == originalQuantity)
			{
				fullyMovedStacks++;
			}

			if (PreserveSlotPositions || moved < originalQuantity)
			{
				slot++;
			}
		}

		if (movedItems > 0)
		{
			target.NotifyChanged();
			NotifyChanged();
		}
		return movedItems;
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
		return IsValidSlotIndex(stackIndex) ? _items[stackIndex] : null;
	}

	public int GetQuantityAt(int stackIndex)
	{
		return IsValidStack(stackIndex) ? _quantities[stackIndex] : 0;
	}

	public IEnumerable<int> GetOccupiedSlotIndices()
	{
		for (int slot = 0; slot < _items.Count; slot++)
		{
			if (_items[slot] is not null)
			{
				yield return slot;
			}
		}
	}

	protected void NotifyChanged()
	{
		EmitSignal(SignalName.InventoryChanged);
	}

	public int FindItemStack(StringName itemId)
	{
		foreach (int stack in GetOccupiedSlotIndices())
		{
			if (_items[stack]!.ItemId == itemId)
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

		ItemDefinition item = _items[stackIndex]!;
		_quantities[stackIndex] -= quantity;
		int newStackIndex;
		if (PreserveSlotPositions)
		{
			newStackIndex = FindAvailableSlot();
			SetSlot(newStackIndex, item, quantity);
		}
		else
		{
			newStackIndex = stackIndex + 1;
			_items.Insert(newStackIndex, item);
			_quantities.Insert(newStackIndex, quantity);
		}
		NotifyChanged();
		return newStackIndex;
	}

	public bool SwapStacks(int firstSlot, int secondSlot)
	{
		if (!PreserveSlotPositions || !IsValidStack(firstSlot) || secondSlot < 0 ||
			(Capacity > 0 && secondSlot >= Capacity) || firstSlot == secondSlot)
		{
			return false;
		}

		EnsureSlotExists(secondSlot);
		(_items[firstSlot], _items[secondSlot]) = (_items[secondSlot], _items[firstSlot]);
		(_quantities[firstSlot], _quantities[secondSlot]) =
			(_quantities[secondSlot], _quantities[firstSlot]);
		NotifyChanged();
		return true;
	}

	public bool AddSavedStack(ItemDefinition item, int quantity)
	{
		return AddSavedStackAt(item, quantity, -1);
	}

	public bool AddSavedStackAt(ItemDefinition item, int quantity, int preferredSlot)
	{
		if (item is null || item.ItemId.IsEmpty || quantity <= 0 ||
			GetWeightAddableQuantity(item) < quantity)
		{
			return false;
		}

		int requiredStacks = RequiredStackCount(item, quantity);
		if (Capacity > 0 && StackCount + requiredStacks > Capacity)
		{
			return false;
		}
		if (PreserveSlotPositions && preferredSlot >= 0 &&
			((Capacity > 0 && preferredSlot >= Capacity) || GetItemAt(preferredSlot) is not null))
		{
			return false;
		}

		AddSavedStackInternal(item, quantity, preferredSlot);
		NotifyChanged();
		return true;
	}

	public bool CanRestoreSavedStacks(IReadOnlyList<ItemStackRestoreData> stacks)
	{
		if (stacks is null)
		{
			return false;
		}

		long requiredStacks = 0;
		double totalWeight = 0.0;
		HashSet<int> occupiedPreferredSlots = new();
		foreach (ItemStackRestoreData stack in stacks)
		{
			if (stack.Definition is null || stack.Definition.ItemId.IsEmpty || stack.Quantity <= 0)
			{
				return false;
			}

			requiredStacks += RequiredStackCount(stack.Definition, stack.Quantity);
			totalWeight +=
				(double)Mathf.Max(stack.Definition.UnitWeightKg, 0.0f) * stack.Quantity;
			if (PreserveSlotPositions && stack.SlotIndex >= 0 &&
				(stack.Quantity > GetStackLimit(stack.Definition) ||
					(Capacity > 0 && stack.SlotIndex >= Capacity) ||
					!occupiedPreferredSlots.Add(stack.SlotIndex)))
			{
				return false;
			}
		}

		return (Capacity <= 0 || requiredStacks <= Capacity || AllowRestorePastCapacity) &&
			(!HasWeightLimit || totalWeight <= WeightCapacityKg + 0.001);
	}

	public bool RestoreSavedStacks(IReadOnlyList<ItemStackRestoreData> stacks)
	{
		if (!CanRestoreSavedStacks(stacks))
		{
			return false;
		}

		_items.Clear();
		_quantities.Clear();
		if (PreserveSlotPositions)
		{
			// Place authored slot records first so an older slot-less record cannot
			// consume a reserved quick slot while the save is being reconstructed.
			foreach (ItemStackRestoreData stack in stacks)
			{
				if (stack.SlotIndex >= 0)
				{
					SetSlot(stack.SlotIndex, stack.Definition, stack.Quantity);
				}
			}
			foreach (ItemStackRestoreData stack in stacks)
			{
				if (stack.SlotIndex < 0)
				{
					AddSavedStackInternal(stack.Definition, stack.Quantity, -1);
				}
			}
		}
		else
		{
			foreach (ItemStackRestoreData stack in stacks)
			{
				AddSavedStackInternal(stack.Definition, stack.Quantity, -1);
			}
		}
		NotifyChanged();
		return true;
	}

	public static int GetStackLimit(ItemDefinition item)
	{
		return Mathf.Max(item.StackLimit, 1);
	}

	private static int RequiredStackCount(ItemDefinition item, int quantity)
	{
		return Mathf.CeilToInt(quantity / (float)GetStackLimit(item));
	}

	private void AddItemInternal(
		ItemDefinition item,
		int quantity,
		out int destinationStackIndex)
	{
		destinationStackIndex = -1;
		int remaining = quantity;
		int stackLimit = GetStackLimit(item);
		foreach (int stack in FindItemStacks(item.ItemId))
		{
			if (remaining <= 0)
			{
				break;
			}
			if (_quantities[stack] >= stackLimit)
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
			int slot = FindAvailableSlot();
			SetSlot(slot, item, added);
			remaining -= added;
			destinationStackIndex = slot;
		}
	}

	private void AddSavedStackInternal(ItemDefinition item, int quantity, int preferredSlot)
	{
		int remaining = quantity;
		int stackLimit = GetStackLimit(item);
		bool firstStack = true;
		while (remaining > 0)
		{
			int added = Mathf.Min(stackLimit, remaining);
			int slot = PreserveSlotPositions && firstStack && preferredSlot >= 0
				? preferredSlot
				: FindAvailableSlot();
			SetSlot(slot, item, added);
			remaining -= added;
			firstStack = false;
		}
	}

	private void RemoveItemAtInternal(int stackIndex, int quantity)
	{
		_quantities[stackIndex] -= quantity;
		if (_quantities[stackIndex] > 0)
		{
			return;
		}

		if (PreserveSlotPositions)
		{
			_items[stackIndex] = null;
			_quantities[stackIndex] = 0;
		}
		else
		{
			_items.RemoveAt(stackIndex);
			_quantities.RemoveAt(stackIndex);
		}
	}

	private IEnumerable<int> FindItemStacks(StringName itemId)
	{
		foreach (int stack in GetOccupiedSlotIndices())
		{
			if (_items[stack]!.ItemId == itemId)
			{
				yield return stack;
			}
		}
	}

	private int FindAvailableSlot()
	{
		if (PreserveSlotPositions)
		{
			for (int slot = 0; slot < _items.Count; slot++)
			{
				if (_items[slot] is null)
				{
					return slot;
				}
			}
		}
		return _items.Count;
	}

	private void SetSlot(int slot, ItemDefinition item, int quantity)
	{
		EnsureSlotExists(slot);
		_items[slot] = item;
		_quantities[slot] = quantity;
	}

	private void EnsureSlotExists(int slot)
	{
		while (_items.Count <= slot)
		{
			_items.Add(null);
			_quantities.Add(0);
		}
	}

	private bool IsValidSlotIndex(int stackIndex)
	{
		return stackIndex >= 0 && stackIndex < _items.Count;
	}

	private bool IsValidStack(int stackIndex)
	{
		return IsValidSlotIndex(stackIndex) && _items[stackIndex] is not null;
	}
}
