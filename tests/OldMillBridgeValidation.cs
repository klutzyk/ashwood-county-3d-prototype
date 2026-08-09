#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.World;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Validates the Old Mill Bridge as it is actually integrated into Main Street.
///
/// Every check below is a physics query against the live scene, not an inspection
/// of node names: the point is to prove the crossing is genuinely walkable, that
/// the player cannot fall into the gorge, and that the old containment wall no
/// longer seals the west end of Main Street.
/// </summary>
public partial class OldMillBridgeValidation : Node3D
{
	private readonly List<string> _failures = new();

	public override async void _Ready()
	{
		try
		{
			Node3D world = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/main_street.tscn")
				.Instantiate<Node3D>();
			AddChild(world);

			// Let the generated geometry and its collision register with the
			// physics server before querying it.
			for (int frame = 0; frame < 8; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}

			// Stop the player and zombies interfering with the space queries.
			if (world.GetNodeOrNull<Node3D>("Gameplay/Player") is Node3D player)
			{
				player.ProcessMode = ProcessModeEnum.Disabled;
			}

			CheckBridgePresent(world);
			CheckCrossingIsWalkable();
			CheckParapetsBlockFalls();
			CheckWaterBelowDeck(world);
			CheckWestApproachOpened();
			CheckNewWestBoundaryExists();

			if (_failures.Count == 0)
			{
				GD.Print("OLD_MILL_BRIDGE_VALIDATION: PASS - all checks passed");
				GetTree().Quit(0);
				return;
			}

			foreach (string failure in _failures)
			{
				GD.PrintErr($"OLD_MILL_BRIDGE_VALIDATION: FAIL - {failure}");
			}
			GD.PrintErr(
				$"OLD_MILL_BRIDGE_VALIDATION: FAIL - {_failures.Count} check(s) failed");
			GetTree().Quit(1);
		}
		catch (Exception exception)
		{
			GD.PushError("OLD_MILL_BRIDGE_VALIDATION: FAIL - " + exception);
			GetTree().Quit(1);
		}
	}

	private void CheckBridgePresent(Node3D world)
	{
		var bridge = world.GetNodeOrNull<Node3D>("Environment/OldMillBridge");
		if (bridge == null)
		{
			_failures.Add("Environment/OldMillBridge is not instanced in Main Street");
			return;
		}

		if (bridge.GetChildCount() == 0)
		{
			_failures.Add("OldMillBridge generated no geometry");
		}
	}

	/// <summary>
	/// Walks the whole route from Main Street's west end to the far side of the
	/// bridge and proves that at every step there is at least one lane of solid
	/// surface at road height. A hole anywhere along the crossing fails here.
	/// </summary>
	private void CheckCrossingIsWalkable()
	{
		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;
		var gaps = new List<float>();

		for (float x = -108.0f; x >= -248.0f; x -= 2.0f)
		{
			bool laneFound = false;

			foreach (float z in new[] { -3.5f, 0.0f, 3.5f })
			{
				var query = PhysicsRayQueryParameters3D.Create(
					new Vector3(x, 8.0f, z), new Vector3(x, -16.0f, z));
				Godot.Collections.Dictionary hit = space.IntersectRay(query);

				if (hit.Count == 0)
				{
					continue;
				}

				float y = hit["position"].AsVector3().Y;

				// Road, deck and approach surfaces all finish at Y = 0.1.
				if (y > -0.45f && y < 0.65f)
				{
					laneFound = true;
					break;
				}
			}

			if (!laneFound)
			{
				gaps.Add(x);
			}
		}

		if (gaps.Count > 0)
		{
			_failures.Add(
				$"crossing has no walkable lane at {gaps.Count} station(s), " +
				$"first at x={gaps[0]:F1}");
		}
	}

	/// <summary>
	/// The player cannot jump, so the deck parapets are the only thing preventing a
	/// fall into the gorge. Fire outward at walking height from several points along
	/// the span and require a solid hit before the deck edge on both sides.
	/// </summary>
	private void CheckParapetsBlockFalls()
	{
		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;

		foreach (float x in new[] { -146.0f, -160.0f, -176.0f, -192.0f, -206.0f })
		{
			foreach (int side in new[] { -1, 1 })
			{
				var query = PhysicsRayQueryParameters3D.Create(
					new Vector3(x, 0.75f, 0.0f),
					new Vector3(x, 0.75f, side * 14.0f));
				Godot.Collections.Dictionary hit = space.IntersectRay(query);

				if (hit.Count == 0)
				{
					_failures.Add(
						$"deck at x={x:F0} has no parapet on the " +
						$"{(side < 0 ? "north" : "south")} side");
					continue;
				}

				float z = Mathf.Abs(hit["position"].AsVector3().Z);
				if (z > 7.6f)
				{
					_failures.Add(
						$"deck at x={x:F0} first blocks at z={z:F2}, " +
						"beyond the deck edge");
				}
			}
		}
	}

	private void CheckWaterBelowDeck(Node3D world)
	{
		var bridge = world.GetNodeOrNull<OldMillBridge>(
			"Environment/OldMillBridge");
		if (bridge == null)
		{
			_failures.Add("Old Mill Bridge is missing while checking river height");
			return;
		}

		float waterY = bridge.CountyIntegrationMode
			? CountyMap.WaterSurfaceY(
				OldMillBridge.ChannelCenterX, bridge.GlobalPosition.Z)
			: bridge.GetNodeOrNull<Node3D>("BlackwaterSurface")?.GlobalPosition.Y
				?? float.MaxValue;
		if (waterY > -3.0f)
		{
			_failures.Add(
				$"river surface sits at Y={waterY:F2}, too close to the deck at Y=0.1");
		}
	}

	/// <summary>
	/// The original PrototypeSafetyBoundary West wall stood at x = -110.5 and would
	/// stop the player ever reaching the bridge. Prove that stretch is now clear.
	/// </summary>
	private void CheckWestApproachOpened()
	{
		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;

		var query = PhysicsRayQueryParameters3D.Create(
			new Vector3(-108.0f, 1.0f, 0.0f), new Vector3(-129.0f, 1.0f, 0.0f));
		Godot.Collections.Dictionary hit = space.IntersectRay(query);

		if (hit.Count > 0)
		{
			float x = hit["position"].AsVector3().X;
			_failures.Add(
				$"west approach is still blocked at x={x:F2}; the containment wall " +
				"was not moved");
		}
	}

	/// <summary>The world must still be sealed, just further out.</summary>
	private void CheckNewWestBoundaryExists()
	{
		PhysicsDirectSpaceState3D space = GetWorld3D().DirectSpaceState;

		var query = PhysicsRayQueryParameters3D.Create(
			new Vector3(-250.0f, 2.0f, 0.0f), new Vector3(-264.0f, 2.0f, 0.0f));
		Godot.Collections.Dictionary hit = space.IntersectRay(query);

		if (hit.Count == 0)
		{
			_failures.Add(
				"no containment wall beyond the west landing; the player can walk " +
				"off the edge of the world");
		}
	}
}
