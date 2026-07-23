#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Objectives;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ObjectiveFlowValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			PackedScene worldScene = GD.Load<PackedScene>("res://scenes/prototype_world.tscn");
			Node world = worldScene.Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerInventory playerInventory = player.GetNode<PlayerInventory>("Inventory");
			AntibioticsObjective objective = world.GetNode<AntibioticsObjective>("AntibioticsObjective");
			SearchableContainer cabinet = world.GetNode<SearchableContainer>(
				"Buildings/Pharmacy/Interior/MedicineCabinet/SearchableContainer");
			ContainerInventoryDisplay inventoryUi = world.GetNode<ContainerInventoryDisplay>(
				"PerformanceUI/ContainerInventoryWindow");

			Require(objective.State == AntibioticsObjectiveState.SearchPharmacy, "objective starts in search state");
			Require(!cabinet.IsSearched, "pharmacy cabinet starts unsearched");
			Require(cabinet.Inventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 1,
				"cabinet owns one antibiotics item before searching");
			Require(playerInventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 0,
				"searchable contents are not automatically given to the player");

			cabinet.GetNode<Interactable>("Interactable").Interact(player);
			Require(cabinet.IsSearched && inventoryUi.IsOpen, "search opens the real container inventory UI");
			Require(playerInventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 0,
				"search completion still does not transfer antibiotics");

			inventoryUi.SelectContainerItem(0);
			inventoryUi.TakeSelected();
			Require(cabinet.Inventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 0,
				"Take removes antibiotics from the container");
			Require(playerInventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 1,
				"Take explicitly transfers antibiotics to the player");
			Require(objective.State == AntibioticsObjectiveState.ReturnToSafePoint,
				"taking antibiotics advances the structured objective state");

			inventoryUi.Close();
			world.GetNode<Interactable>("PrototypeSafePoint/Interactable").Interact(player);
			Require(objective.State == AntibioticsObjectiveState.Completed,
				"safe-point interaction completes the objective");
			Require(playerInventory.GetQuantity(AntibioticsObjective.AntibioticsItemId) == 0,
				"completion submits the antibiotics");

			GD.Print("OBJECTIVE_FLOW_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"OBJECTIVE_FLOW_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
