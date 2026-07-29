#nullable enable

using System;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class AshwoodSchoolValidation : Node
{
	private const string SchoolPath =
		"res://assets/environment/buildings/AshwoodSchool/ashwood_school.tscn";

	public override async void _Ready()
	{
		Node3D? school = null;
		try
		{
			school = GD.Load<PackedScene>(SchoolPath)
				.Instantiate<Node3D>();
			AddChild(school);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

			Node3D authored =
				school.GetNode<Node3D>("AuthoredSchool");
			Require(
				(int)authored.GetMeta("storeys") == 2,
				"school is explicitly authored as a two-storey building");
			Require(
				authored.GetMeta("layout").AsString() == "hand_authored",
				"school records its deterministic hand-authored layout");

			Node3D architecture =
				authored.GetNode<Node3D>("Architecture");
			MeshInstance3D[] authoredMeshes = architecture
				.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>()
				.Where(mesh => mesh.Mesh is BoxMesh)
				.ToArray();
			Require(
				authoredMeshes.Length >= 150,
				$"school has a fully articulated architectural shell " +
				$"({authoredMeshes.Length} authored textured pieces)");
			foreach (MeshInstance3D mesh in authoredMeshes)
			{
				BoxMesh box = (BoxMesh)mesh.Mesh!;
				Require(
					box.Material is StandardMaterial3D material &&
					material.AlbedoTexture is not null,
					$"{mesh.GetPath()} uses a textured PBR material");
			}

			Node3D stairwell =
				architecture.GetNode<Node3D>("Stairwell");
			Require(
				stairwell.HasNode("AuthoredWoodenStairs"),
				"the visible staircase comes from the licensed external model");
			CollisionShape3D stairCollision =
				stairwell.GetNode<CollisionShape3D>(
					"InvisibleWalkableStairRamp/Collision");
			Require(
				stairCollision.Shape is BoxShape3D &&
				!stairCollision.Disabled,
				"the staircase has one active continuous walkable ramp");
			Require(
				Mathf.Abs(
					(float)stairwell.GetMeta("rise_metres") - 3.36f) <
					0.01f,
				"stair rise matches the authored floor-to-floor height");

			Node3D annex =
				architecture.GetNode<Node3D>("DoubleHeightGymAnnex");
			Require(
				annex.GetMeta("room").AsString() ==
					"double_height_gymnasium",
				"school includes a dedicated double-height gym annex");
			Require(
				annex.HasNode("GymFloor") &&
				annex.HasNode("GymRoof") &&
				annex.HasNode("OpenGymConnectorDoor"),
				"gym has complete architecture and a traversable connector");

			Node[] classrooms = authored
				.FindChildren("Classroom*", "Node3D", true, false)
				.ToArray();
			Require(
				classrooms.Length == 5,
				$"school has five furnished teaching rooms " +
				$"(found {classrooms.Length})");
			Node[] desks = authored
				.FindChildren("StudentDesk_*", "Node3D", true, false)
				.ToArray();
			Require(
				desks.Length == 45,
				$"classrooms contain 45 licensed desk assets " +
				$"(found {desks.Length})");
			Require(
				authored.FindChildren("*RestroomToilet", "Node3D", true, false)
					.Count == 2 &&
				authored.FindChildren("*RestroomSink", "Node3D", true, false)
					.Count == 2,
				"both fully enclosed restrooms contain proper fixtures");

			Node[] importedProps = authored
				.FindChildren("*", "Node3D", true, false)
				.Where(node => node.HasMeta("source_path"))
				.ToArray();
			int distinctSourcedModels = importedProps
				.Select(node => node.GetMeta("source_path").AsString())
				.Distinct(StringComparer.Ordinal)
				.Count();
			Require(
				importedProps.Length >= 145,
				$"interior and facade use dense licensed asset dressing " +
				$"({importedProps.Length} sourced prop instances)");
			Require(
				distinctSourcedModels >= 29,
				"school uses at least 29 distinct sourced prop models");

			Node3D areaDressing =
				school.GetNode<Node3D>("AreaDressing");
			Require(
				areaDressing.GetMeta("_minimum_clear_corridor_width")
					.AsDouble() >= 1.8,
				"exterior dressing preserves a 1.8 metre entrance route");
			Require(
				areaDressing
					.FindChildren("*", "Node3D", true, false)
					.Count >= 27,
				"school grounds have a dense hand-placed activity-yard pass");

			Require(
				school.HasNode(
					"AuthoredSchool/ExteriorIdentity/OpenDoubleEntrance"),
				"street-facing main entrance uses an open double-door composition");
			Require(
				school.HasNode(
					"AuthoredSchool/ExteriorIdentity/EntranceThresholdRamp"),
				"street entrance includes a shallow forward-walk threshold");

			GD.Print(
				$"ASHWOOD_SCHOOL_VALIDATION: PASS " +
				$"({authoredMeshes.Length} architecture pieces, " +
				$"{importedProps.Length} sourced props, " +
				$"{distinctSourcedModels} distinct sourced models, " +
				$"{desks.Length} desks)");
			school.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			if (school is not null && IsInstanceValid(school))
			{
				school.QueueFree();
			}
			GD.PushError(
				$"ASHWOOD_SCHOOL_VALIDATION: FAIL - {exception.Message}");
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
