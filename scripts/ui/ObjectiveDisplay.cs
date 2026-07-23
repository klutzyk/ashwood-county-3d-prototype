#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Objectives;

namespace AshwoodCounty3DPrototype.UI;

public partial class ObjectiveDisplay : Control
{
	[Export] public NodePath ObjectivePath { get; set; } = new("../../AntibioticsObjective");

	private AntibioticsObjective _objective = null!;
	private Label _objectiveText = null!;

	public override void _Ready()
	{
		_objective = GetNode<AntibioticsObjective>(ObjectivePath);
		_objectiveText = GetNode<Label>("ObjectiveText");
		_objective.StateChanged += OnStateChanged;
		Refresh();
	}

	private void OnStateChanged(int state)
	{
		Refresh();
	}

	private void Refresh()
	{
		_objectiveText.Text = $"OBJECTIVE\n{_objective.DisplayText}";
	}
}
