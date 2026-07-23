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
		if (_suppliesObjective.State == ServiceStationSuppliesObjectiveState.Completed)
		{
			return;
		}
		Tween tween = CreateTween();
		_objectiveText.Modulate = new Color(1.0f, 0.92f, 0.62f, 1.0f);
		tween.TweenProperty(_objectiveText, "modulate", Colors.White, 0.35f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void Refresh()
	{
		if (_suppliesObjective.State == ServiceStationSuppliesObjectiveState.Completed)
		{
			_objectiveText.Text = "OBJECTIVES COMPLETE\nEmergency supplies delivered";
			_objectiveText.Modulate = new Color(0.72f, 0.92f, 0.68f, 1.0f);
			return;
		}

		bool firstObjective = _objective.State != AntibioticsObjectiveState.Completed;
		string displayText = firstObjective
			? _objective.DisplayText
			: _suppliesObjective.DisplayText;
		_objectiveText.Text = $"CURRENT OBJECTIVE  {(firstObjective ? "1 / 2" : "2 / 2")}\n" +
			displayText;
		_objectiveText.Modulate = Colors.White;
	}
}
