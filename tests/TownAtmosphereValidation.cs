#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Interactions;

namespace AshwoodCounty3DPrototype.Tests;

public partial class TownAtmosphereValidation : Node
{
	public override async void _Ready()
	{
		try
		{
			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			Node3D atmosphere = world.GetNode<Node3D>("Props/TownAtmospherePass");
			string[] areas =
			{
				"CommercialAlley",
				"DinerFrontage",
				"ResidentialVerge",
				"MaintenanceLayby",
			};
			foreach (string area in areas)
			{
				Require(atmosphere.HasNode(area), $"atmosphere pass dresses {area}");
			}

			Require(atmosphere.HasNode("CommercialAlley/Dumpster") &&
				atmosphere.HasNode("CommercialAlley/NewsBoxA"),
				"commercial alley includes civic clutter");
			Require(atmosphere.HasNode("DinerFrontage/Bench") &&
				atmosphere.HasNode("DinerFrontage/ShoppingTrolley"),
				"diner frontage includes public-use props");
			Require(atmosphere.HasNode("ResidentialVerge/BrokenFence") &&
				atmosphere.HasNode("ResidentialVerge/UtilityBox"),
				"residential verge includes damaged and utility dressing");
			Require(atmosphere.GetNode("MaintenanceLayby")
				.FindChildren("Tyre*", string.Empty, false, false).Count == 3,
				"maintenance layby includes a restrained tyre pile");

			int geometryCount = 0;
			foreach (Node node in atmosphere.FindChildren("*", string.Empty, true, false))
			{
				Require(node is not CollisionObject3D,
					"atmosphere props remain visual-only and navigation-safe");
				Require(node is not Interactable && node is not SearchableContainer,
					"atmosphere props do not imply new interactions");
				if (node is GeometryInstance3D geometry)
				{
					geometryCount++;
					Require(geometry.VisibilityRangeEnd > 0.0f ||
						IsPartOfInstantiatedAsset(node, atmosphere),
						"new primitive atmosphere geometry uses distance culling");
				}
			}
			Require(geometryCount >= 20 && geometryCount <= 36,
				"atmosphere pass uses a restrained geometry budget");

			GD.Print("TOWN_ATMOSPHERE_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"TOWN_ATMOSPHERE_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static bool IsPartOfInstantiatedAsset(Node node, Node atmosphere)
	{
		Node? current = node;
		while (current is not null && current != atmosphere)
		{
			if (current.SceneFilePath.Length > 0)
			{
				return true;
			}

			current = current.GetParent();
		}

		return false;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
