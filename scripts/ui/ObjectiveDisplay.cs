#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Objectives;

namespace AshwoodCounty3DPrototype.UI;

public partial class ObjectiveDisplay : Control
{
	[Export] public NodePath ObjectivePath { get; set; } = new("../../AntibioticsObjective");
	[Export] public float CompletionMessageDuration { get; set; } = 3.0f;

	private AntibioticsObjective _objective = null!;
	private Label _objectiveText = null!;
	private Label _completionMessage = null!;
	private float _completionMessageRemaining;

	public override void _Ready()
	{
		_objective = GetNode<AntibioticsObjective>(ObjectivePath);
		_objectiveText = GetNode<Label>("ObjectiveText");
		_completionMessage = GetNode<Label>("CompletionMessage");
		_objective.StateChanged += OnStateChanged;
		_objective.CompletionMessageRequested += ShowCompletionMessage;
		_completionMessage.Visible = false;
		Refresh();
	}

	public override void _Process(double delta)
	{
		if (_completionMessageRemaining <= 0.0f)
		{
			return;
		}

		_completionMessageRemaining = Mathf.Max(_completionMessageRemaining - (float)delta, 0.0f);
		_completionMessage.Visible = _completionMessageRemaining > 0.0f;
	}

	private void OnStateChanged(int state)
	{
		Refresh();
	}

	private void Refresh()
	{
		_objectiveText.Text = $"OBJECTIVE\n{_objective.DisplayText}";
	}

	private void ShowCompletionMessage(string message)
	{
		_completionMessage.Text = message;
		_completionMessage.Visible = true;
		_completionMessageRemaining = Mathf.Max(CompletionMessageDuration, 0.0f);
	}
}
