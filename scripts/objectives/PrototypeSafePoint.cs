#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Objectives;

public partial class PrototypeSafePoint : Node3D
{
	private Interactable _interactable = null!;
	private AntibioticsObjective _objective = null!;

	public override void _Ready()
	{
		_interactable = GetNode<Interactable>("Interactable");
		_objective = GetTree().GetFirstNodeInGroup(AntibioticsObjective.GroupName) as AntibioticsObjective
			?? throw new System.InvalidOperationException("Prototype safe point requires an antibiotics objective.");
		_interactable.Interacted += OnInteracted;
	}

	private void OnInteracted(Node interactor)
	{
		_objective.TryComplete();
	}
}
