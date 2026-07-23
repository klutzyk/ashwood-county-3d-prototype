#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Objectives;

public partial class PrototypeSafePoint : Node3D
{
	private Interactable _interactable = null!;
	private AntibioticsObjective _objective = null!;
	private ServiceStationSuppliesObjective _suppliesObjective = null!;
	private SearchableContainer _storage = null!;

	public override void _Ready()
	{
		_interactable = GetNode<Interactable>("Interactable");
		_storage = GetNode<SearchableContainer>("Storage");
		_storage.RestoreSearchedState(true);
		_storage.SetInteractionEnabled(false);
		_objective = GetTree().GetFirstNodeInGroup(AntibioticsObjective.GroupName) as AntibioticsObjective
			?? throw new System.InvalidOperationException("Prototype safe point requires an antibiotics objective.");
		_suppliesObjective = GetTree().GetFirstNodeInGroup(
			ServiceStationSuppliesObjective.GroupName) as ServiceStationSuppliesObjective
			?? throw new System.InvalidOperationException(
				"Prototype safe point requires a service-station supplies objective.");
		_interactable.Interacted += OnInteracted;
		_objective.StateChanged += OnObjectiveStateChanged;
		_objective.StateRestored += OnObjectiveStateChanged;
		_suppliesObjective.StateChanged += OnObjectiveStateChanged;
		_suppliesObjective.StateRestored += OnObjectiveStateChanged;
		ConfigurePrompt();
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_interactable))
		{
			_interactable.Interacted -= OnInteracted;
		}
		if (IsInstanceValid(_objective))
		{
			_objective.StateChanged -= OnObjectiveStateChanged;
			_objective.StateRestored -= OnObjectiveStateChanged;
		}
		if (IsInstanceValid(_suppliesObjective))
		{
			_suppliesObjective.StateChanged -= OnObjectiveStateChanged;
			_suppliesObjective.StateRestored -= OnObjectiveStateChanged;
		}
	}

	private void OnInteracted(Node interactor)
	{
		if (_objective.TryComplete())
		{
			return;
		}
		if (_suppliesObjective.TryComplete())
		{
			return;
		}
		if (_objective.State == AntibioticsObjectiveState.Completed)
		{
			ContainerInventoryDisplay? display = GetTree()
				.GetFirstNodeInGroup(ContainerInventoryDisplay.GroupName) as
				ContainerInventoryDisplay;
			display?.Open(_storage, interactor);
		}
	}

	private void OnObjectiveStateChanged(int state)
	{
		ConfigurePrompt();
	}

	private void ConfigurePrompt()
	{
		string action = _objective.State != AntibioticsObjectiveState.Completed
			? "Deliver antibiotics at"
			: _suppliesObjective.State ==
				ServiceStationSuppliesObjectiveState.ReturnToSafePoint
				? "Deliver supplies at"
				: "Open";
		_interactable.ConfigurePrompt(action, "Safe Point", 0.0f);
	}
}
