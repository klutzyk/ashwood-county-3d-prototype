#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.UI;

public partial class InventoryDisplay : HBoxContainer
{
	[Export] public NodePath InventoryPath { get; set; } = new("../../Player/Inventory");

	private PlayerInventory _inventory = null!;
	private readonly Label[] _slotLabels = new Label[PlayerInventory.QuickSlotCount];

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
			int quantity = _inventory.GetQuantityAt(slot);
			_slotLabels[slot].Text = item is null
				? $"{slot + 5}\nEMPTY"
				: $"{slot + 5}  x{quantity}\n{item.DisplayName.ToUpperInvariant()}";
			// Keep the slot frame at full opacity so an empty quick slot remains a
			// readable affordance instead of disappearing into the world image.
			_slotLabels[slot].Modulate = Colors.White;
			_slotLabels[slot].AddThemeColorOverride(
				"font_color",
				item is null
					? new Color(0.62f, 0.65f, 0.59f, 0.96f)
					: new Color(0.92f, 0.89f, 0.77f));
			_slotLabels[slot].TooltipText = item is null
				? $"Quick slot {slot + 1} (key {slot + 5}) - Empty"
				: $"Quick slot {slot + 1} (key {slot + 5})\n" +
					$"{item.DisplayName} x{quantity}\n{item.EffectDescription}";
		}
	}

}
