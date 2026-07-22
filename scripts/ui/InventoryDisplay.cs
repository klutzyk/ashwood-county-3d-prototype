#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.UI;

public partial class InventoryDisplay : HBoxContainer
{
	[Export] public NodePath InventoryPath { get; set; } = new("../../Player/Inventory");
	[Export] public NodePath FeedbackPath { get; set; } = new("../ItemFeedback");

	private PlayerInventory _inventory = null!;
	private Label _feedback = null!;
	private readonly Label[] _slotLabels = new Label[PlayerInventory.SlotCount];
	private float _feedbackRemaining;

	public override void _Ready()
	{
		_inventory = GetNode<PlayerInventory>(InventoryPath);
		_feedback = GetNode<Label>(FeedbackPath);
		for (int slot = 0; slot < _slotLabels.Length; slot++)
		{
			_slotLabels[slot] = GetNode<Label>($"Slot{slot + 1}");
		}

		_inventory.InventoryChanged += Refresh;
		_inventory.ItemUsed += ShowFeedback;
		Refresh();
		_feedback.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (_feedbackRemaining <= 0.0f)
		{
			return;
		}

		_feedbackRemaining = Mathf.Max(_feedbackRemaining - (float)delta, 0.0f);
		_feedback.Visible = _feedbackRemaining > 0.0f;
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

	private void ShowFeedback(string message)
	{
		_feedback.Text = message;
		_feedback.Visible = !string.IsNullOrWhiteSpace(message);
		_feedbackRemaining = 2.5f;
	}
}
