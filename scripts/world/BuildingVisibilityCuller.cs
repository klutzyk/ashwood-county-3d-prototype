#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.World;

/// <summary>
/// Hides dense, enterable building visuals outside their useful view distance.
/// Collision and gameplay state remain active so culling cannot affect travel,
/// interactions, inventories, or save data.
/// </summary>
public partial class BuildingVisibilityCuller : Node
{
	[Export]
	public NodePath TargetRootPath { get; set; } = new();

	[Export(PropertyHint.MultilineText)]
	public string TargetNames { get; set; } = string.Empty;

	[Export(PropertyHint.Range, "20,180,1")]
	public float ShowDistance { get; set; } = 82.0f;

	[Export(PropertyHint.Range, "25,200,1")]
	public float HideDistance { get; set; } = 92.0f;

	[Export(PropertyHint.Range, "0.05,1,0.05")]
	public float UpdateIntervalSeconds { get; set; } = 0.25f;

	private readonly List<Node3D> _targets = new();
	private readonly HashSet<Node3D> _culledTargets = new();
	private double _elapsedSeconds;

	public override void _Ready()
	{
		if (HideDistance < ShowDistance)
		{
			HideDistance = ShowDistance;
		}

		Node? root = GetNodeOrNull<Node>(TargetRootPath);
		if (root is null)
		{
			GD.PushWarning(
				$"{Name}: target root '{TargetRootPath}' was not found.");
			SetProcess(false);
			return;
		}

		foreach (string targetName in TargetNames.Split(
				',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			Node3D? target = root.GetNodeOrNull<Node3D>(targetName);
			if (target is null)
			{
				GD.PushWarning(
					$"{Name}: building '{targetName}' was not found under '{root.GetPath()}'.");
				continue;
			}

			_targets.Add(target);
		}

		CallDeferred(MethodName.UpdateVisibility);
	}

	public override void _Process(double delta)
	{
		_elapsedSeconds += delta;
		if (_elapsedSeconds < UpdateIntervalSeconds)
		{
			return;
		}

		_elapsedSeconds = 0.0;
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		Camera3D? camera = GetViewport().GetCamera3D();
		if (camera is null)
		{
			return;
		}

		float showDistanceSquared = ShowDistance * ShowDistance;
		float hideDistanceSquared = HideDistance * HideDistance;
		foreach (Node3D target in _targets)
		{
			float distanceSquared = camera.GlobalPosition.DistanceSquaredTo(
				target.GlobalPosition);
			if (target.Visible && distanceSquared > hideDistanceSquared)
			{
				target.Visible = false;
				_culledTargets.Add(target);
			}
			else if (!target.Visible && distanceSquared < showDistanceSquared &&
				_culledTargets.Remove(target))
			{
				target.Visible = true;
			}
		}
	}
}
