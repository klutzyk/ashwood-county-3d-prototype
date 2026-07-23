#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.Objectives;

public enum ServiceStationSuppliesObjectiveState
{
	Locked,
	SearchServiceStation,
	ReturnToSafePoint,
	Completed,
}

public partial class ServiceStationSuppliesObjective : Node
{
	public static readonly StringName GroupName = new("service_station_supplies_objective");
	public static readonly StringName CannedFoodItemId = new("canned_food");
	public static readonly StringName WaterItemId = new("water");
	public static readonly StringName SodaItemId = new("soda");

	[Signal]
	public delegate void StateChangedEventHandler(int state);

	[Signal]
	public delegate void StateRestoredEventHandler(int state);

	[Export] public NodePath PlayerInventoryPath { get; set; } = new("../Player/Inventory");
	[Export] public NodePath AntibioticsObjectivePath { get; set; } = new("../AntibioticsObjective");

	public ServiceStationSuppliesObjectiveState State { get; private set; } =
		ServiceStationSuppliesObjectiveState.Locked;
	public string DisplayText => State switch
	{
		ServiceStationSuppliesObjectiveState.SearchServiceStation =>
			"Search the service station for canned food and a drink",
		ServiceStationSuppliesObjectiveState.ReturnToSafePoint =>
			"Return the emergency supplies to the safe point",
		ServiceStationSuppliesObjectiveState.Completed => "Emergency supplies delivered",
		_ => string.Empty,
	};

	private PlayerInventory _playerInventory = null!;
	private AntibioticsObjective _antibioticsObjective = null!;

	public override void _Ready()
	{
		AddToGroup(GroupName);
		_playerInventory = GetNode<PlayerInventory>(PlayerInventoryPath);
		_antibioticsObjective = GetNode<AntibioticsObjective>(AntibioticsObjectivePath);
		_playerInventory.InventoryChanged += OnInventoryChanged;
		_antibioticsObjective.StateChanged += OnAntibioticsStateChanged;
		_antibioticsObjective.StateRestored += OnAntibioticsStateChanged;
		if (_antibioticsObjective.State == AntibioticsObjectiveState.Completed)
		{
			Activate();
		}
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_playerInventory))
		{
			_playerInventory.InventoryChanged -= OnInventoryChanged;
		}
		if (IsInstanceValid(_antibioticsObjective))
		{
			_antibioticsObjective.StateChanged -= OnAntibioticsStateChanged;
			_antibioticsObjective.StateRestored -= OnAntibioticsStateChanged;
		}
	}

	public bool TryComplete()
	{
		if (State != ServiceStationSuppliesObjectiveState.ReturnToSafePoint ||
			_playerInventory.GetQuantity(CannedFoodItemId) < 1)
		{
			return false;
		}

		StringName drink = _playerInventory.GetQuantity(WaterItemId) > 0
			? WaterItemId
			: SodaItemId;
		if (_playerInventory.GetQuantity(drink) < 1)
		{
			return false;
		}

		_playerInventory.RemoveItem(CannedFoodItemId, 1);
		_playerInventory.RemoveItem(drink, 1);
		SetState(ServiceStationSuppliesObjectiveState.Completed);
		return true;
	}

	public void RestoreState(ServiceStationSuppliesObjectiveState state)
	{
		if (state == ServiceStationSuppliesObjectiveState.Locked &&
			_antibioticsObjective.State == AntibioticsObjectiveState.Completed)
		{
			state = ServiceStationSuppliesObjectiveState.SearchServiceStation;
		}
		State = state;
		EmitSignal(SignalName.StateRestored, (int)State);
	}

	private void OnAntibioticsStateChanged(int state)
	{
		if ((AntibioticsObjectiveState)state == AntibioticsObjectiveState.Completed)
		{
			Activate();
		}
	}

	private void Activate()
	{
		if (State != ServiceStationSuppliesObjectiveState.Locked)
		{
			return;
		}
		SetState(ServiceStationSuppliesObjectiveState.SearchServiceStation);
		OnInventoryChanged();
	}

	private void OnInventoryChanged()
	{
		if (State == ServiceStationSuppliesObjectiveState.SearchServiceStation &&
			_playerInventory.GetQuantity(CannedFoodItemId) >= 1 &&
			(_playerInventory.GetQuantity(WaterItemId) >= 1 ||
				_playerInventory.GetQuantity(SodaItemId) >= 1))
		{
			SetState(ServiceStationSuppliesObjectiveState.ReturnToSafePoint);
		}
	}

	private void SetState(ServiceStationSuppliesObjectiveState state)
	{
		if (State == state)
		{
			return;
		}
		State = state;
		EmitSignal(SignalName.StateChanged, (int)State);
	}
}
