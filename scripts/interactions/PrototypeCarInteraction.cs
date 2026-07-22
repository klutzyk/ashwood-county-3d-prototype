#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class PrototypeCarInteraction : StaticBody3D
{
	public override void _Ready()
	{
		GetNode<Interactable>("Interactable").Interacted += OnInteracted;
	}

	private static void OnInteracted(Node interactor)
	{
		GD.Print("Search complete.");
	}
}
