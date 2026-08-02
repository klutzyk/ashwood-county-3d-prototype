#nullable enable

using System;
using System.IO;
using Godot;
using AshwoodCounty3DPrototype.Interactions;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.UI;

namespace AshwoodCounty3DPrototype.Tests;

public partial class InventoryStorageVisualReview : Node
{
	public override async void _Ready()
	{
		try
		{
			GD.Print("INVENTORY_STORAGE_VISUAL_REVIEW: preparing fixture");
			SubViewport viewport = new()
			{
				Name = "InventoryReviewViewport",
				Size = new Vector2I(1600, 900),
				OwnWorld3D = true,
				RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			};
			AddChild(viewport);

			Node content = new() { Name = "ReviewContent" };
			viewport.AddChild(content);
			InventoryReviewPlayer player = new() { Name = "Player" };
			player.AddChild(new PlayerHealth { Name = "Health" });
			player.AddChild(new PlayerNeeds
			{
				Name = "Needs",
				HungerDecreasePerSecond = 0.0f,
				ThirstDecreasePerSecond = 0.0f,
			});
			player.AddChild(new PlayerInventory { Name = "Inventory" });
			content.AddChild(player);

			CanvasLayer reviewHud = new() { Name = "ReviewHUD" };
			content.AddChild(reviewHud);
			ColorRect background = new()
			{
				Name = "ReviewBackground",
				Color = new Color(0.035f, 0.043f, 0.038f, 1.0f),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			reviewHud.AddChild(background);

			CanvasLayer sourceHud = GD.Load<PackedScene>(
					"res://scenes/ui/gameplay_hud.tscn")
				.Instantiate<CanvasLayer>();
			ContainerInventoryDisplay display =
				sourceHud.GetNode<ContainerInventoryDisplay>("ContainerInventoryWindow");
			sourceHud.RemoveChild(display);
			sourceHud.Free();
			reviewHud.AddChild(display);

			SearchableContainer cache = new()
			{
				Name = "ReviewSupplyCache",
				DisplayName = "Bakery Emergency Pantry",
				SearchDuration = 0.0f,
			};
			cache.AddChild(new Interactable { Name = "Interactable" });
			cache.AddChild(new ContainerInventory
			{
				Name = "Inventory",
				SlotCapacity = 18,
			});
			content.AddChild(cache);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GD.Print("INVENTORY_STORAGE_VISUAL_REVIEW: fixture ready");

			PlayerInventory playerInventory = player.GetNode<PlayerInventory>("Inventory");
			playerInventory.ClearItems();
			Add(playerInventory, "bandage", 3);
			Add(playerInventory, "water", 2);
			Add(playerInventory, "canned_food", 2);
			Add(playerInventory, "scrap", 6);
			Add(playerInventory, "painkillers", 1);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/inventory_storage_visual_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			display.OpenBackpack();
			display.SelectPlayerItem(4);
			ItemList reviewPlayerItems = display.GetNode<ItemList>(
				"Panel/Layout/Columns/PlayerColumn/PlayerItems");
			Control fieldActions = display.GetNode<Control>("Panel/Layout/FieldActions");
			Control transferActions = display.GetNode<Control>("Panel/Layout/Columns/Actions");
			Require(
				fieldActions.IsVisibleInTree() && !transferActions.IsVisibleInTree(),
				"field mode must attach its actions below the full-width player pack");
			Require(
				!string.IsNullOrEmpty(reviewPlayerItems.FocusNeighborBottom.ToString()),
				"field pack must expose explicit controller focus into its action row");
			reviewPlayerItems.Select(4);
			GD.Print("INVENTORY_STORAGE_VISUAL_REVIEW: capturing field inventory");
			await Capture(
				viewport,
				Path.Combine(outputDirectory, "01_field_inventory.png"));
			display.Close();
			display.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			CanvasLayer containerSourceHud = GD.Load<PackedScene>(
					"res://scenes/ui/gameplay_hud.tscn")
				.Instantiate<CanvasLayer>();
			display = containerSourceHud.GetNode<ContainerInventoryDisplay>(
				"ContainerInventoryWindow");
			containerSourceHud.RemoveChild(display);
			containerSourceHud.Free();
			reviewHud.AddChild(display);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			cache.Inventory.ClearItems();
			Add(cache.Inventory, "chocolate", 4);
			Add(cache.Inventory, "water", 3);
			Add(cache.Inventory, "medkit", 1);
			Add(cache.Inventory, "canned_food", 3);
			cache.RestoreSearchedState(true);
			display.Open(cache, player);
			fieldActions = display.GetNode<Control>("Panel/Layout/FieldActions");
			transferActions = display.GetNode<Control>("Panel/Layout/Columns/Actions");
			Require(
				!fieldActions.IsVisibleInTree() && transferActions.IsVisibleInTree(),
				"container mode must restore the dedicated two-pane transfer column");
			Require(
				display.GetNode<Control>("Panel/Layout/Close").IsVisibleInTree(),
				"close action must remain visible in the two-pane layout");
			display.SelectContainerItem(2);
			display.GetNode<ItemList>(
				"Panel/Layout/Columns/ContainerColumn/ContainerItems").Select(2);
			GD.Print("INVENTORY_STORAGE_VISUAL_REVIEW: capturing transfer view");
			await Capture(
				viewport,
				Path.Combine(outputDirectory, "02_container_transfer.png"));
			Button takeButton = display.GetNode<Button>("Panel/Layout/Columns/Actions/Take");
			takeButton.GrabFocus();
			await Capture(
				viewport,
				Path.Combine(outputDirectory, "03_controller_action_focus.png"));

			GD.Print($"INVENTORY_STORAGE_VISUAL_REVIEW: {outputDirectory} (1600x900)");
			display.QueueFree();
			cache.QueueFree();
			player.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			viewport.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"INVENTORY_STORAGE_VISUAL_REVIEW: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private async System.Threading.Tasks.Task Capture(SubViewport viewport, string path)
	{
		for (int frame = 0; frame < 8; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		await ToSignal(
			RenderingServer.Singleton,
			RenderingServer.SignalName.FramePostDraw);
		Error error = viewport.GetTexture().GetImage().SavePng(path);
		if (error != Error.Ok)
		{
			throw new InvalidOperationException($"Could not save inventory review shot: {error}");
		}
	}

	private static void Add(ItemStorage storage, string itemId, int quantity)
	{
		ItemDefinition item = GD.Load<ItemDefinition>($"res://assets/items/{itemId}.tres");
		if (!storage.AddItem(item, quantity))
		{
			throw new InvalidOperationException($"Could not add {itemId} x{quantity}.");
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

public partial class InventoryReviewPlayer : ThirdPersonPlayer
{
	public override void _Ready()
	{
		// The review needs only inventory ownership and condition nodes, not the
		// production character mesh, camera rig, collision or movement systems.
	}

	public override void _PhysicsProcess(double delta)
	{
	}

	public override void _UnhandledInput(InputEvent @event)
	{
	}
}
