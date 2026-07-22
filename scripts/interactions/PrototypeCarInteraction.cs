#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Items;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class PrototypeCarInteraction : StaticBody3D
{
	[Export] public ItemDefinition RewardItem { get; set; } = null!;

	private Interactable _interactable = null!;
	private bool _searched;

	public override void _Ready()
	{
		_interactable = GetNode<Interactable>("Interactable");
		_interactable.Interacted += OnInteracted;
	}

	private void OnInteracted(Node interactor)
	{
		if (_searched || RewardItem is null)
		{
			return;
		}

		PlayerInventory? inventory = interactor.GetNodeOrNull<PlayerInventory>("Inventory");
		if (inventory is null || !inventory.AddItem(RewardItem))
		{
			return;
		}

		_searched = true;
		_interactable.Enabled = false;
		GD.Print($"Found 1 {RewardItem.DisplayName}.");
	}
}
