#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

public partial class UserSuppliedAssetLibraryValidation : Node
{
	private const string LibraryRoot =
		"res://assets/third_party/user_supplied/ashwood_2026_07_29";

	private static readonly string[] ExpectedFiles =
	{
		"2001_crown_victoria_police_interceptor_game_prop.glb",
		"abandoned_childrens_slide.glb",
		"antique_globe.glb",
		"basketball.glb",
		"basketball_hoop_panel.glb",
		"bike.glb",
		"bookshelf.glb",
		"bulletin_board.glb",
		"cobwebs_asset_pack.glb",
		"coffee_mug_school_project.glb",
		"corkboard.glb",
		"door_classroom_9_mb.glb",
		"door_wood.glb",
		"doublewindow.glb",
		"file_cabinet.glb",
		"hospitaldoor.glb",
		"manhole.glb",
		"office_chair_game_model_download.glb",
		"old_metal_table_low_poly.glb",
		"old_school_lockers.glb",
		"paper_-_3mb.glb",
		"paper_debris.glb",
		"pencil_low.glb",
		"picnic_table.glb",
		"post_it_notes.glb",
		"rust_filingcabinet-freepoly.org.glb",
		"school_desk.glb",
		"school_lockers_damaged.glb",
		"some_eraser_two.glb",
		"the_pen.glb",
		"trash_can.glb",
		"variety_of_books.glb",
		"whiteboard.glb",
		"wooden_stairs_5_mb.glb",
	};

	private static readonly string[] RejectedFiles =
	{
		"dusty_old_bookshelf_free.glb",
		"retro_computer_setup_free.glb",
		"school_desk (1).glb",
		"house_door_white.glb",
		"window.glb",
		"office_drawer.glb",
		"low_poly__sofa.glb",
		"house_door.glb",
	};

	public override async void _Ready()
	{
		try
		{
			string[] actualFiles = DirAccess
				.GetFilesAt(LibraryRoot)
				.Where(file => file.EndsWith(
					".glb",
					StringComparison.OrdinalIgnoreCase))
				.OrderBy(file => file, StringComparer.Ordinal)
				.ToArray();

			Require(
				actualFiles.SequenceEqual(
					ExpectedFiles.OrderBy(
						file => file,
						StringComparer.Ordinal),
					StringComparer.Ordinal),
				$"curated library contains exactly {ExpectedFiles.Length} approved GLBs");
			foreach (string rejectedFile in RejectedFiles)
			{
				Require(
					!FileAccess.FileExists($"{LibraryRoot}/{rejectedFile}"),
					$"{rejectedFile} remains outside the redistributable curated library");
			}

			foreach (string file in ExpectedFiles)
			{
				string path = $"{LibraryRoot}/{file}";
				PackedScene? scene = GD.Load<PackedScene>(path);
				Require(scene is not null, $"{file} imports as a PackedScene");

				Node3D instance = scene!.Instantiate<Node3D>();
				AddChild(instance);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

				MeshInstance3D[] geometry = instance
					.FindChildren("*", "MeshInstance3D", true, false)
					.OfType<MeshInstance3D>()
					.Where(mesh => mesh.Mesh is not null)
					.ToArray();
				Require(geometry.Length > 0, $"{file} contains visible mesh geometry");

				(Vector3 minimum, Vector3 maximum) =
					CalculateBounds(instance, geometry);
				Vector3 size = maximum - minimum;
				Require(
					size.X > 0.0001f &&
					size.Y > 0.0001f &&
					size.Z > 0.0001f,
					$"{file} has non-degenerate authored bounds");

				GD.Print(
					$"USER_ASSET_BOUNDS: {file} " +
					$"min={Format(minimum)} max={Format(maximum)} " +
					$"size={Format(size)} meshes={geometry.Length}");

				instance.QueueFree();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			GD.Print(
				$"USER_SUPPLIED_ASSET_LIBRARY_VALIDATION: PASS " +
				$"({ExpectedFiles.Length} licensed curated GLBs)");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"USER_SUPPLIED_ASSET_LIBRARY_VALIDATION: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
		}
	}

	private static (Vector3 Minimum, Vector3 Maximum) CalculateBounds(
		Node3D root,
		IEnumerable<MeshInstance3D> geometry)
	{
		Vector3 minimum = new(
			float.PositiveInfinity,
			float.PositiveInfinity,
			float.PositiveInfinity);
		Vector3 maximum = new(
			float.NegativeInfinity,
			float.NegativeInfinity,
			float.NegativeInfinity);
		Transform3D fromWorld = root.GlobalTransform.AffineInverse();

		foreach (MeshInstance3D meshInstance in geometry)
		{
			Aabb bounds = meshInstance.Mesh!.GetAabb();
			Transform3D toRoot =
				fromWorld * meshInstance.GlobalTransform;
			for (int endpoint = 0; endpoint < 8; endpoint++)
			{
				Vector3 point = toRoot * GetEndpoint(bounds, endpoint);
				minimum = new Vector3(
					Mathf.Min(minimum.X, point.X),
					Mathf.Min(minimum.Y, point.Y),
					Mathf.Min(minimum.Z, point.Z));
				maximum = new Vector3(
					Mathf.Max(maximum.X, point.X),
					Mathf.Max(maximum.Y, point.Y),
					Mathf.Max(maximum.Z, point.Z));
			}
		}

		return (minimum, maximum);
	}

	private static Vector3 GetEndpoint(Aabb bounds, int endpoint)
	{
		return bounds.Position + new Vector3(
			(endpoint & 1) == 0 ? 0.0f : bounds.Size.X,
			(endpoint & 2) == 0 ? 0.0f : bounds.Size.Y,
			(endpoint & 4) == 0 ? 0.0f : bounds.Size.Z);
	}

	private static string Format(Vector3 value)
	{
		return
			$"({value.X:0.###},{value.Y:0.###},{value.Z:0.###})";
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
