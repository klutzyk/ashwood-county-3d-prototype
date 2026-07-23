#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class InteractionConsistencyValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerInteraction interaction = player.GetNode<PlayerInteraction>("Interaction");
			PlayerHealth health = player.GetNode<PlayerHealth>("Health");
			player.SetPhysicsProcess(false);
			player.GlobalPosition = new Vector3(0.0f, 1.0f, 6.0f);
			foreach (Node node in GetTree().GetNodesInGroup(Interactable.GroupName))
			{
				((Interactable)node).Enabled = false;
			}

			Interactable near = AddInteractable(
				world, "NearOwner", "Near Door", "Open", player.GlobalPosition + new Vector3(0, 0.8f, 1.5f));
			Interactable far = AddInteractable(
				world, "FarOwner", "Far Door", "Open", player.GlobalPosition + new Vector3(0, 0.8f, 2.5f));
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			interaction._PhysicsProcess(0.0);
			Require(interaction.CurrentInteractable == near,
				"nearest valid interactable wins");
			Require(interaction.CurrentPromptText == "Press [E] to Open Near Door",
				"interaction prompts share consistent formatting");

			string latestPrompt = interaction.CurrentPromptText;
			interaction.PromptChanged += text => latestPrompt = text;
			near.ConfigurePrompt("Close", "Near Door", 0.0f);
			Require(latestPrompt == "Press [E] to Close Near Door",
				"prompt changes appear immediately without changing candidates");
			near.SetPromptOverride("Opening Door…");
			Require(latestPrompt == "Opening Door…",
				"temporary interaction feedback can replace the standard prompt");
			near.SetPromptOverride(string.Empty);
			Require(latestPrompt == "Press [E] to Close Near Door",
				"clearing temporary feedback restores the configured prompt");

			StaticBody3D blocker = AddBlocker(
				world, player.GlobalPosition + new Vector3(0, 0.8f, 0.75f));
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			interaction._PhysicsProcess(0.0);
			Require(interaction.CurrentInteractable is null && latestPrompt == string.Empty,
				"wall occlusion immediately clears invalid prompts");

			blocker.GlobalPosition += Vector3.Right * 8.0f;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			interaction._PhysicsProcess(0.0);
			Require(interaction.CurrentInteractable == near,
				"candidate returns when lightweight line of sight is restored");

			int interactionCount = 0;
			near.Interacted += _ => interactionCount++;
			health.RestoreState(0.0f);
			interaction._PhysicsProcess(0.0);
			Require(interaction.CurrentInteractable is null &&
				!near.Interact(player) && interactionCount == 0,
				"dead players neither see prompts nor trigger world interactions");

			health.RestoreState(health.MaximumHealth);
			player.SetInventoryUiOpen(true);
			Require(!near.Interact(player) && interactionCount == 0,
				"blocking inventory UI prevents direct world interaction");
			player.SetInventoryUiOpen(false);
			Require(near.Interact(player) && interactionCount == 1,
				"interaction resumes after blocking UI closes");

			near.ConfigurePrompt("Search", "Near Cabinet", 1.0f);
			interaction._PhysicsProcess(0.0);
			interaction._UnhandledInput(new InputEventAction
			{
				Action = "interact",
				Pressed = true,
			});
			Require(interaction.IsInteracting,
				"hold-search progress still starts on the current candidate");
			blocker.GlobalPosition = player.GlobalPosition + new Vector3(0, 0.8f, 0.75f);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			interaction._PhysicsProcess(0.2);
			Require(!interaction.IsInteracting && interaction.CurrentInteractable is null,
				"losing validity cancels hold progress immediately");

			GD.Print("INTERACTION_CONSISTENCY_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"INTERACTION_CONSISTENCY_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static Interactable AddInteractable(
		Node world,
		string ownerName,
		string displayName,
		string action,
		Vector3 position)
	{
		Node3D owner = new() { Name = ownerName, Position = position };
		Interactable interactable = new() { Name = "Interactable" };
		interactable.ConfigurePrompt(action, displayName, 0.0f);
		world.AddChild(owner);
		owner.AddChild(interactable);
		return interactable;
	}

	private static StaticBody3D AddBlocker(Node world, Vector3 position)
	{
		StaticBody3D blocker = new()
		{
			Name = "InteractionLosBlocker",
			Position = position,
			CollisionLayer = 1,
			CollisionMask = 1,
		};
		CollisionShape3D collision = new()
		{
			Shape = new BoxShape3D { Size = new Vector3(1.0f, 2.0f, 0.25f) },
		};
		world.AddChild(blocker);
		blocker.AddChild(collision);
		return blocker;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
