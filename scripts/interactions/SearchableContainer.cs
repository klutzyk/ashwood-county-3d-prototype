#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Interactions;

public partial class SearchableContainer : Node3D
{
	public static readonly StringName GroupName = new("searchable_containers");

	[Export] public string DisplayName { get; set; } = "Container";
	[Export] public float SearchDuration { get; set; } = 2.0f;
	[Export] public LootTable? LootTable { get; set; }
	[Export] public Godot.Collections.Array<ItemDefinition> InitialItems { get; set; } = new();

	public bool IsSearched { get; private set; }
	public ContainerInventory Inventory { get; private set; } = null!;

	private Interactable _interactable = null!;
	private readonly RandomNumberGenerator _random = new();

	public override void _Ready()
	{
		AddToGroup(GroupName);
		_interactable = GetNode<Interactable>("Interactable");
		Inventory = GetNode<ContainerInventory>("Inventory");
		_interactable.Interacted += OnInteracted;
		_random.Randomize();
		foreach (ItemDefinition item in InitialItems)
		{
			Inventory.AddItem(item);
		}
		ConfigureInteraction();
	}

	public void RestoreSearchedState(bool isSearched)
	{
		IsSearched = isSearched;
		ConfigureInteraction();
	}

	public void SetInteractionEnabled(bool enabled)
	{
		_interactable.Enabled = enabled;
	}

	public void SetLootSeed(ulong seed)
	{
		_random.Seed = seed;
	}

	private void OnInteracted(Node interactor)
	{
		if (!IsSearched)
		{
			LootTable?.GenerateInto(Inventory, _random);
			IsSearched = true;
			ConfigureInteraction();
		}

		ContainerInventoryDisplay? display = GetTree()
			.GetFirstNodeInGroup(ContainerInventoryDisplay.GroupName) as ContainerInventoryDisplay;
		display?.Open(this, interactor);
	}

	private void ConfigureInteraction()
	{
		_interactable.ConfigurePrompt(
			IsSearched ? "Open" : "Search",
			DisplayName,
			IsSearched ? 0.0f : SearchDuration);
	}
}
