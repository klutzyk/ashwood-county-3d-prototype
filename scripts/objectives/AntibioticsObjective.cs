#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.Objectives;

public enum AntibioticsObjectiveState
{
	SearchPharmacy,
	ReturnToSafePoint,
	Completed,
}

public partial class AntibioticsObjective : Node
{
	public static readonly StringName GroupName = new("antibiotics_objective");
	public static readonly StringName AntibioticsItemId = new("antibiotics");

	[Signal]
	public delegate void StateChangedEventHandler(int state);

	[Signal]
	public delegate void StateRestoredEventHandler(int state);

	[Export] public NodePath PlayerInventoryPath { get; set; } = new("../Player/Inventory");

	public AntibioticsObjectiveState State { get; private set; } = AntibioticsObjectiveState.SearchPharmacy;
	public string DisplayText => State switch
	{
		AntibioticsObjectiveState.SearchPharmacy => "Search the pharmacy for antibiotics",
		AntibioticsObjectiveState.ReturnToSafePoint => "Return the antibiotics to the safe point",
		_ => "Antibiotics delivered",
	};

	private PlayerInventory _playerInventory = null!;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		_playerInventory = GetNode<PlayerInventory>(PlayerInventoryPath);
		_playerInventory.InventoryChanged += OnInventoryChanged;
		OnInventoryChanged();
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_playerInventory))
		{
			_playerInventory.InventoryChanged -= OnInventoryChanged;
		}
	}

	public bool TryComplete()
	{
		if (State != AntibioticsObjectiveState.ReturnToSafePoint ||
			_playerInventory.GetQuantity(AntibioticsItemId) <= 0 ||
			!_playerInventory.RemoveItem(AntibioticsItemId))
		{
			return false;
		}

		SetState(AntibioticsObjectiveState.Completed);
		return true;
	}

	public void RestoreState(AntibioticsObjectiveState state)
	{
		State = state;
		EmitSignal(SignalName.StateRestored, (int)State);
	}

	private void OnInventoryChanged()
	{
		if (State == AntibioticsObjectiveState.SearchPharmacy &&
			_playerInventory.GetQuantity(AntibioticsItemId) > 0)
		{
			SetState(AntibioticsObjectiveState.ReturnToSafePoint);
		}
	}

	private void SetState(AntibioticsObjectiveState state)
	{
		if (State == state)
		{
			return;
		}

		State = state;
		EmitSignal(SignalName.StateChanged, (int)State);
	}
}
