#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Zombies;

namespace AshwoodCounty3DPrototype.Tests;

public partial class HudPresentationVisualReview : Node
{
	public override async void _Ready()
	{
		try
		{
			DisplayServer.WindowSetSize(new Vector2I(1600, 900));
			Node3D world = GD.Load<PackedScene>(
				"res://scenes/world/ashwood/main_street.tscn").Instantiate<Node3D>();
			AddChild(world);
			await WaitFrames(3);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Gameplay/Player");
			player.SetPhysicsProcess(false);
			foreach (Node child in world.GetNode("Gameplay/Zombies").GetChildren())
			{
				if (child is PrototypeZombie zombie)
				{
					zombie.ProcessMode = ProcessModeEnum.Disabled;
				}
			}

			PlayerInventory inventory = player.GetNode<PlayerInventory>("Inventory");
			inventory.ClearItems();
			AddAt(inventory, "bandage", 3, 0);
			AddAt(inventory, "water", 2, 1);
			AddAt(inventory, "scrap", 6, 3);
			player.GetNode<PlayerHealth>("Health").RestoreState(72.0f);
			player.GetNode<PlayerStamina>("Stamina").RestoreState(64.0f, canSprint: true);
			player.GetNode<PlayerNeeds>("Needs").RestoreState(58.0f, 41.0f);

			Control? fps = world.GetNodeOrNull<Control>("Gameplay/GameplayHUD/FpsLabel");
			if (fps is not null)
			{
				fps.Visible = false;
			}
			Control? prompt = world.GetNodeOrNull<Control>(
				"Gameplay/GameplayHUD/InteractionPrompt");
			if (prompt is not null)
			{
				prompt.Visible = false;
			}

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/ui_presentation_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
			await WaitFrames(8);
			await Capture(Path.Combine(outputDirectory, "01_gameplay_hud.png"));
			GD.Print($"HUD_PRESENTATION_VISUAL_REVIEW: {outputDirectory} (1600x900)");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"HUD_PRESENTATION_VISUAL_REVIEW: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private async Task Capture(string path)
	{
		await ToSignal(
			RenderingServer.Singleton,
			RenderingServer.SignalName.FramePostDraw);
		Image image = GetViewport().GetTexture().GetImage();
		if (image.IsEmpty())
		{
			throw new InvalidOperationException("Rendered HUD capture was empty.");
		}
		Error error = image.SavePng(path);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Could not save HUD review shot: {error}");
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static void AddAt(
		PlayerInventory inventory,
		string itemId,
		int quantity,
		int slot)
	{
		ItemDefinition item = GD.Load<ItemDefinition>($"res://assets/items/{itemId}.tres");
		if (!inventory.AddSavedStackAt(item, quantity, slot))
		{
			throw new InvalidOperationException(
				$"Could not stage {itemId} x{quantity} in quick slot {slot + 1}.");
		}
	}
}
