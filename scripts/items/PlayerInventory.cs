#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Items;

public partial class PlayerInventory : ItemStorage
{
	public const int SlotCount = 4;

	[Signal]
	public delegate void ItemUsedEventHandler(string message);

	public override int Capacity => SlotCount;

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GetParent<ThirdPersonPlayer>().IsInventoryUiOpen || @event is InputEventKey { Echo: true })
		{
			return;
		}

		string[] actions = { "use_slot_1", "use_slot_2", "use_slot_3", "use_slot_4" };
		for (int slot = 0; slot < actions.Length; slot++)
		{
			if (!@event.IsActionPressed(actions[slot]))
			{
				continue;
			}

			if (UseItemAt(slot, GetParent()))
			{
				GetViewport().SetInputAsHandled();
			}
			return;
		}
	}

	public bool UseItemAt(int slot, Node user)
	{
		ItemDefinition? item = GetItemAt(slot);
		if (item is null || GetQuantityAt(slot) <= 0)
		{
			return false;
		}

		if (!item.Use(user))
		{
			return false;
		}

		string feedback = item.UseFeedback;
		if (!RemoveItemAt(slot))
		{
			return false;
		}

		EmitSignal(SignalName.ItemUsed, feedback);
		return true;
	}
}
