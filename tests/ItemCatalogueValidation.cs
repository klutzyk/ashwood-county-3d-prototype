#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Tests;

public partial class ItemCatalogueValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			PlayerHealth health = player.GetNode<PlayerHealth>("Health");
			PlayerNeeds needs = player.GetNode<PlayerNeeds>("Needs");

			BandageItem bandage = Load<BandageItem>("bandage");
			BandageItem medkit = Load<BandageItem>("medkit");
			DamageResistanceItem painkillers = Load<DamageResistanceItem>("painkillers");
			NeedRestoringItem soda = Load<NeedRestoringItem>("soda");
			NeedRestoringItem cannedFood = Load<NeedRestoringItem>("canned_food");
			NeedRestoringItem chocolate = Load<NeedRestoringItem>("chocolate");
			ItemDefinition scrap = Load<ItemDefinition>("scrap");

			foreach (string itemId in new[]
			{
				"bandage", "food", "water", "antibiotics", "medkit", "painkillers",
				"soda", "canned_food", "chocolate", "scrap",
			})
			{
				ItemDefinition item = Load<ItemDefinition>(itemId);
				Require(item.ItemId == itemId && !string.IsNullOrWhiteSpace(item.DisplayName) &&
					!string.IsNullOrWhiteSpace(item.Description) && item.StackLimit > 0 &&
					item.Icon is not null, $"{itemId} has complete catalogue metadata");
			}

			Require(medkit.HealthRestored > bandage.HealthRestored,
				"medkit restores more health than bandage");
			Require(soda.ThirstRestored > 0.0f && soda.HungerRestored > 0.0f,
				"soda restores thirst and a small amount of hunger");
			Require(cannedFood.HungerRestored > chocolate.HungerRestored &&
				chocolate.HungerRestored > 0.0f,
				"canned food restores more hunger than chocolate");
			Require(scrap.Category == ItemCategory.CraftingMaterial && !scrap.CanUse(player),
				"scrap is a non-usable crafting material");

			health.RestoreState(100.0f);
			Require(painkillers.Use(player) && health.HasDamageResistance,
				"painkillers apply temporary damage resistance");
			Require(health.ApplyDamage(20.0f) && Mathf.IsEqualApprox(health.CurrentHealth, 85.0f),
				"painkiller resistance reduces incoming damage by 25 percent");
			Require(!painkillers.CanUse(player),
				"painkillers cannot be stacked while the prototype effect is active");

			needs.RestoreState(50.0f, 50.0f);
			Require(soda.Use(player) &&
				Mathf.IsEqualApprox(needs.CurrentHunger, 55.0f) &&
				Mathf.IsEqualApprox(needs.CurrentThirst, 75.0f),
				"soda restores both configured needs");

			GD.Print("ITEM_CATALOGUE_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"ITEM_CATALOGUE_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static T Load<T>(string itemId) where T : ItemDefinition
	{
		return GD.Load<T>($"res://assets/items/{itemId}.tres")
			?? throw new InvalidOperationException($"Could not load item {itemId}.");
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
