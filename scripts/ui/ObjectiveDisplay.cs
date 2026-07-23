#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Objectives;

namespace AshwoodCounty3DPrototype.UI;

public partial class ObjectiveDisplay : Control
{
	[Export] public NodePath ObjectivePath { get; set; } = new("../../AntibioticsObjective");
	[Export] public NodePath SuppliesObjectivePath { get; set; } =
		new("../../ServiceStationSuppliesObjective");

	private AntibioticsObjective _objective = null!;
	private ServiceStationSuppliesObjective _suppliesObjective = null!;
	private Label _objectiveText = null!;

	public override void _Ready()
	{
		_objective = GetNode<AntibioticsObjective>(ObjectivePath);
		_suppliesObjective = GetNode<ServiceStationSuppliesObjective>(SuppliesObjectivePath);
		_objectiveText = GetNode<Label>("ObjectiveText");
		_objective.StateChanged += OnStateChanged;
		_objective.StateRestored += OnStateChanged;
		_suppliesObjective.StateChanged += OnStateChanged;
		_suppliesObjective.StateRestored += OnStateChanged;
		Refresh();
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_objective))
		{
			_objective.StateChanged -= OnStateChanged;
			_objective.StateRestored -= OnStateChanged;
		}
		if (IsInstanceValid(_suppliesObjective))
		{
			_suppliesObjective.StateChanged -= OnStateChanged;
			_suppliesObjective.StateRestored -= OnStateChanged;
		}
	}

	private void OnStateChanged(int state)
	{
		Refresh();
	}

	private void Refresh()
	{
		string displayText = _objective.State == AntibioticsObjectiveState.Completed
			? _suppliesObjective.DisplayText
			: _objective.DisplayText;
		_objectiveText.Text = $"OBJECTIVE\n{displayText}";
	}
}
