#nullable enable

using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Items;

public partial class PlayerInventory : Node
{
	[Signal]
	public delegate void InventoryChangedEventHandler();

	private readonly Dictionary<StringName, int> _quantities = new();

	public bool AddItem(ItemDefinition item, int quantity = 1)
	{
		if (item is null || item.ItemId.IsEmpty || quantity <= 0)
		{
			return false;
		}

		_quantities.TryGetValue(item.ItemId, out int currentQuantity);
		_quantities[item.ItemId] = currentQuantity + quantity;
		EmitSignal(SignalName.InventoryChanged);
		return true;
	}

	public int GetQuantity(StringName itemId)
	{
		return _quantities.GetValueOrDefault(itemId);
	}
}
