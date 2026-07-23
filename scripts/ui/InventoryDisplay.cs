#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.UI;

public partial class InventoryDisplay : HBoxContainer
{
	[Export] public NodePath InventoryPath { get; set; } = new("../../Player/Inventory");

	private PlayerInventory _inventory = null!;
	private readonly Label[] _slotLabels = new Label[PlayerInventory.SlotCount];

	public override void _Ready()
	{
		_inventory = GetNode<PlayerInventory>(InventoryPath);
		for (int slot = 0; slot < _slotLabels.Length; slot++)
		{
			_slotLabels[slot] = GetNode<Label>($"Slot{slot + 1}");
		}

		_inventory.InventoryChanged += Refresh;
		Refresh();
	}

	private void Refresh()
	{
		for (int slot = 0; slot < _slotLabels.Length; slot++)
		{
			ItemDefinition? item = _inventory.GetItemAt(slot);
			_slotLabels[slot].Text = item is null
				? $"{slot + 1}\nEmpty"
				: $"{slot + 1}\n{item.DisplayName} x{_inventory.GetQuantityAt(slot)}";
		}
	}

}
