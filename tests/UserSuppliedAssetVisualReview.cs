#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Builds a neutral, consistently lit gallery of every approved user-supplied
/// asset wrapper. In a rendered run it captures one overview and four focused
/// contact-sheet views. In a headless run it still loads, instantiates, and
/// bounds-checks every wrapper before exiting.
/// </summary>
public partial class UserSuppliedAssetVisualReview : Node3D
{
	private enum GalleryBay
	{
		LargeOutdoor,
		Architecture,
		Furniture,
		Tabletop,
	}

	private readonly record struct AssetSpec(
		string DisplayName,
		string ScenePath,
		GalleryBay Bay,
		float YawDegrees = 0.0f,
		float BaseHeight = 0.0f);

	private readonly record struct GalleryLayout(
		float CentreZ,
		float Spacing,
		float Width,
		float Depth,
		string Title);

	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target,
		float Fov);

	private const string WrapperRoot =
		"res://assets/environment/props/user_supplied";

	private static readonly AssetSpec[] Assets =
	{
		new("Crown Victoria Police", $"{WrapperRoot}/crown_victoria_police.tscn",
			GalleryBay.LargeOutdoor, -22.0f),
		new("Abandoned Slide", $"{WrapperRoot}/abandoned_slide.tscn",
			GalleryBay.LargeOutdoor, -15.0f),
		new("Basketball Hoop", $"{WrapperRoot}/basketball_hoop.tscn",
			GalleryBay.LargeOutdoor),
		new("Bicycle", $"{WrapperRoot}/bicycle.tscn",
			GalleryBay.LargeOutdoor, 62.0f),
		new("Picnic Table", $"{WrapperRoot}/picnic_table.tscn",
			GalleryBay.LargeOutdoor, -18.0f),
		new("Wooden Stairs", $"{WrapperRoot}/wooden_stairs.tscn",
			GalleryBay.LargeOutdoor),
		new("Manhole", $"{WrapperRoot}/manhole.tscn",
			GalleryBay.LargeOutdoor),

		new("Classroom Double Door",
			$"{WrapperRoot}/classroom_double_door.tscn",
			GalleryBay.Architecture),
		new("Wood Door", $"{WrapperRoot}/wood_door.tscn",
			GalleryBay.Architecture),
		new("Double Window", $"{WrapperRoot}/double_window.tscn",
			GalleryBay.Architecture, 0.0f, 0.45f),
		new("Hospital Door", $"{WrapperRoot}/hospital_door.tscn",
			GalleryBay.Architecture),
		new("Hero Bookshelf", $"{WrapperRoot}/hero_bookshelf.tscn",
			GalleryBay.Architecture, -90.0f),
		new("Old School Lockers", $"{WrapperRoot}/old_school_lockers.tscn",
			GalleryBay.Architecture),
		new("Damaged School Lockers",
			$"{WrapperRoot}/damaged_school_lockers.tscn",
			GalleryBay.Architecture),
		new("Whiteboard", $"{WrapperRoot}/whiteboard.tscn",
			GalleryBay.Architecture, 0.0f, 0.9f),

		new("School Desk", $"{WrapperRoot}/school_desk.tscn",
			GalleryBay.Furniture, -16.0f),
		new("File Cabinet", $"{WrapperRoot}/file_cabinet.tscn",
			GalleryBay.Furniture, 12.0f),
		new("Rusty Cabinet", $"{WrapperRoot}/rusty_cabinet.tscn",
			GalleryBay.Furniture, -10.0f),
		new("Office Chair", $"{WrapperRoot}/office_chair.tscn",
			GalleryBay.Furniture, 22.0f),
		new("Old Metal Table", $"{WrapperRoot}/old_metal_table.tscn",
			GalleryBay.Furniture, -18.0f),
		new("Trash Can", $"{WrapperRoot}/trash_can.tscn",
			GalleryBay.Furniture, 14.0f),
		new("Bulletin Board", $"{WrapperRoot}/bulletin_board.tscn",
			GalleryBay.Furniture, 0.0f, 1.15f),
		new("Corkboard", $"{WrapperRoot}/corkboard.tscn",
			GalleryBay.Furniture, 0.0f, 1.15f),

		new("Antique Globe", $"{WrapperRoot}/antique_globe.tscn",
			GalleryBay.Tabletop, -15.0f, 0.82f),
		new("Basketball", $"{WrapperRoot}/basketball.tscn",
			GalleryBay.Tabletop, 0.0f, 0.82f),
		new("Books Cluster", $"{WrapperRoot}/books_cluster.tscn",
			GalleryBay.Tabletop, -18.0f, 0.82f),
		new("Coffee Mug", $"{WrapperRoot}/coffee_mug.tscn",
			GalleryBay.Tabletop, 18.0f, 0.82f),
		new("Paper Stack", $"{WrapperRoot}/paper_stack.tscn",
			GalleryBay.Tabletop, 10.0f, 0.82f),
		new("Paper Debris", $"{WrapperRoot}/paper_debris.tscn",
			GalleryBay.Tabletop, -12.0f, 0.82f),
		new("Pencil", $"{WrapperRoot}/pencil.tscn",
			GalleryBay.Tabletop, 15.0f, 0.82f),
		new("Pen", $"{WrapperRoot}/pen.tscn",
			GalleryBay.Tabletop, -15.0f, 0.82f),
		new("Eraser", $"{WrapperRoot}/eraser.tscn",
			GalleryBay.Tabletop, 10.0f, 0.82f),
		new("Post-it Notes", $"{WrapperRoot}/post_it_notes.tscn",
			GalleryBay.Tabletop, -8.0f, 0.82f),
		new("Cobweb Pack", $"{WrapperRoot}/cobweb_pack.tscn",
			GalleryBay.Tabletop, 0.0f, 1.35f),
	};

	private static readonly Dictionary<GalleryBay, GalleryLayout> Layouts = new()
	{
		[GalleryBay.LargeOutdoor] =
			new(0.0f, 7.0f, 50.0f, 11.0f, "LARGE / OUTDOOR"),
		[GalleryBay.Architecture] =
			new(-29.0f, 4.8f, 42.0f, 10.0f, "ARCHITECTURE"),
		[GalleryBay.Furniture] =
			new(-57.0f, 4.2f, 38.0f, 10.0f, "FURNITURE / OFFICE"),
		[GalleryBay.Tabletop] =
			new(-84.0f, 2.55f, 32.0f, 8.0f, "TABLETOP / MICRO DETAIL"),
	};

	private readonly Dictionary<string, Vector3> _measuredSizes =
		new(StringComparer.Ordinal);

	public override async void _Ready()
	{
		try
		{
			AddReviewEnvironment();
			Node3D gallery = new() { Name = "Gallery" };
			AddChild(gallery);

			AddGallerySurfaces(gallery);
			await BuildAssetGallery(gallery);

			Require(
				_measuredSizes.Count == Assets.Length,
				$"gallery contains all {Assets.Length} approved wrappers");

			bool headless = DisplayServer.GetName().Equals(
				"headless",
				StringComparison.OrdinalIgnoreCase);
			if (headless)
			{
				GD.Print(
					$"USER_SUPPLIED_ASSET_VISUAL_REVIEW: PASS " +
					$"({Assets.Length} wrappers loaded; headless capture skipped)");
				GetTree().Quit(0);
				return;
			}

			await CaptureGallery();
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"USER_SUPPLIED_ASSET_VISUAL_REVIEW: FAIL - " +
				exception.Message);
			GetTree().Quit(1);
		}
	}

	private async System.Threading.Tasks.Task BuildAssetGallery(Node3D gallery)
	{
		foreach (GalleryBay bay in Enum.GetValues<GalleryBay>())
		{
			AssetSpec[] bayAssets = Assets
				.Where(asset => asset.Bay == bay)
				.ToArray();
			GalleryLayout layout = Layouts[bay];

			for (int index = 0; index < bayAssets.Length; index++)
			{
				AssetSpec asset = bayAssets[index];
				Require(
					ResourceLoader.Exists(asset.ScenePath),
					$"missing wrapper: {asset.ScenePath}");

				PackedScene? packedScene =
					GD.Load<PackedScene>(asset.ScenePath);
				Require(
					packedScene is not null,
					$"wrapper does not load as PackedScene: {asset.ScenePath}");

				Node3D instance = packedScene!.Instantiate<Node3D>();
				instance.Name = MakeSafeNodeName(asset.DisplayName);
				gallery.AddChild(instance);
				await ToSignal(
					GetTree(),
					SceneTree.SignalName.ProcessFrame);

				MeshInstance3D[] geometry = instance
					.FindChildren("*", "MeshInstance3D", true, false)
					.OfType<MeshInstance3D>()
					.Where(mesh => mesh.Mesh is not null)
					.ToArray();
				Require(
					geometry.Length > 0,
					$"{asset.DisplayName} wrapper has no mesh geometry");

				(Vector3 minimum, Vector3 maximum) =
					CalculateBounds(instance, geometry);
				Vector3 size = maximum - minimum;
				Require(
					IsValidSize(size),
					$"{asset.DisplayName} has invalid bounds {Format(size)}");
				_measuredSizes.Add(asset.ScenePath, size);

				foreach (MeshInstance3D mesh in geometry)
				{
					mesh.VisibilityRangeEnd = 0.0f;
					mesh.VisibilityRangeEndMargin = 0.0f;
				}

				float x =
					(index - ((bayAssets.Length - 1) * 0.5f)) *
					layout.Spacing;
				instance.Position = new Vector3(
					x,
					asset.BaseHeight,
					layout.CentreZ);
				instance.RotationDegrees =
					new Vector3(0.0f, asset.YawDegrees, 0.0f);

				if (bay == GalleryBay.Tabletop &&
					asset.DisplayName != "Cobweb Pack")
				{
					AddPedestal(
						gallery,
						new Vector3(x, 0.39f, layout.CentreZ));
				}

				float labelHeight = asset.BaseHeight + maximum.Y + 0.38f;
				if (bay == GalleryBay.Tabletop)
				{
					labelHeight = Mathf.Max(labelHeight, 1.62f);
				}

				AddAssetLabel(
					gallery,
					asset.DisplayName,
					size,
					new Vector3(x, labelHeight, layout.CentreZ + 0.1f),
					bay == GalleryBay.Tabletop);

				GD.Print(
					$"USER_ASSET_REVIEW_ITEM: {asset.DisplayName} " +
					$"size={Format(size)} meshes={geometry.Length}");
			}
		}
	}

	private void AddReviewEnvironment()
	{
		Godot.Environment environment = new()
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.055f, 0.065f, 0.078f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.73f, 0.78f, 0.84f),
			AmbientLightEnergy = 0.72f,
			ReflectedLightSource =
				Godot.Environment.ReflectionSource.Bg,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};
		AddChild(new WorldEnvironment
		{
			Name = "ReviewEnvironment",
			Environment = environment,
		});

		AddChild(new DirectionalLight3D
		{
			Name = "KeyLight",
			LightColor = new Color(1.0f, 0.91f, 0.78f),
			LightEnergy = 1.3f,
			ShadowEnabled = true,
			DirectionalShadowMaxDistance = 120.0f,
			RotationDegrees = new Vector3(-48.0f, -132.0f, 0.0f),
		});
		AddChild(new DirectionalLight3D
		{
			Name = "CoolFill",
			LightColor = new Color(0.66f, 0.78f, 1.0f),
			LightEnergy = 0.42f,
			ShadowEnabled = false,
			RotationDegrees = new Vector3(-32.0f, 42.0f, 0.0f),
		});
	}

	private void AddGallerySurfaces(Node3D gallery)
	{
		StandardMaterial3D floorMaterial = new()
		{
			AlbedoColor = new Color(0.19f, 0.215f, 0.245f),
			Roughness = 0.84f,
			Metallic = 0.0f,
		};
		StandardMaterial3D wallMaterial = new()
		{
			AlbedoColor = new Color(0.28f, 0.305f, 0.34f),
			Roughness = 0.9f,
			Metallic = 0.0f,
		};

		foreach ((GalleryBay bay, GalleryLayout layout) in Layouts)
		{
			AddBox(
				gallery,
				$"{bay}Floor",
				new Vector3(layout.Width, 0.12f, layout.Depth),
				new Vector3(0.0f, -0.07f, layout.CentreZ),
				floorMaterial);
			AddBox(
				gallery,
				$"{bay}Backdrop",
				new Vector3(layout.Width, 4.8f, 0.12f),
				new Vector3(0.0f, 2.34f, layout.CentreZ - 2.15f),
				wallMaterial);

			AddBayTitle(
				gallery,
				layout.Title,
				new Vector3(
					0.0f,
					4.35f,
					layout.CentreZ - 1.95f));
			AddScaleReference(
				gallery,
				new Vector3(
					(-layout.Width * 0.5f) + 0.8f,
					0.0f,
					layout.CentreZ + 2.4f));
		}
	}

	private static void AddBox(
		Node3D parent,
		string name,
		Vector3 size,
		Vector3 position,
		Material material)
	{
		BoxMesh box = new()
		{
			Size = size,
			Material = material,
		};
		parent.AddChild(new MeshInstance3D
		{
			Name = name,
			Mesh = box,
			Position = position,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
		});
	}

	private static void AddPedestal(Node3D parent, Vector3 position)
	{
		StandardMaterial3D material = new()
		{
			AlbedoColor = new Color(0.37f, 0.39f, 0.42f),
			Roughness = 0.72f,
		};
		AddBox(
			parent,
			"MicroDetailPedestal",
			new Vector3(1.55f, 0.78f, 1.08f),
			position,
			material);
	}

	private static void AddScaleReference(Node3D parent, Vector3 position)
	{
		StandardMaterial3D material = new()
		{
			AlbedoColor = new Color(0.95f, 0.64f, 0.12f),
			Roughness = 0.58f,
		};
		AddBox(
			parent,
			"OneMetreReference",
			new Vector3(0.08f, 1.0f, 0.08f),
			position + new Vector3(0.0f, 0.5f, 0.0f),
			material);
		AddPlainLabel(
			parent,
			"1 m",
			position + new Vector3(0.0f, 1.18f, 0.0f),
			0.0045f,
			30);
	}

	private static void AddBayTitle(
		Node3D parent,
		string text,
		Vector3 position)
	{
		Label3D label = AddPlainLabel(parent, text, position, 0.006f, 46);
		label.Modulate = new Color(1.0f, 0.77f, 0.29f);
		label.OutlineSize = 12;
	}

	private static void AddAssetLabel(
		Node3D parent,
		string name,
		Vector3 size,
		Vector3 position,
		bool compact)
	{
		string text =
			$"{name}\n{size.X:0.00} x {size.Y:0.00} x {size.Z:0.00} m";
		AddPlainLabel(
			parent,
			text,
			position,
			compact ? 0.0034f : 0.0041f,
			compact ? 24 : 29);
	}

	private static Label3D AddPlainLabel(
		Node3D parent,
		string text,
		Vector3 position,
		float pixelSize,
		int fontSize)
	{
		Label3D label = new()
		{
			Text = text,
			Position = position,
			PixelSize = pixelSize,
			FontSize = fontSize,
			OutlineSize = 8,
			Modulate = new Color(0.95f, 0.965f, 0.98f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			NoDepthTest = true,
		};
		parent.AddChild(label);
		return label;
	}

	private async System.Threading.Tasks.Task CaptureGallery()
	{
		Camera3D camera = new()
		{
			Name = "ReviewCamera",
			Current = true,
			Near = 0.05f,
			Far = 190.0f,
		};
		AddChild(camera);

		ReviewShot[] shots =
		{
			new(
				"00_full_gallery.png",
				new Vector3(0.0f, 68.0f, 17.0f),
				new Vector3(0.0f, 0.7f, -43.0f),
				63.0f),
			new(
				"01_large_outdoor.png",
				new Vector3(0.0f, 9.2f, 24.0f),
				new Vector3(0.0f, 1.15f, 0.0f),
				58.0f),
			new(
				"02_architecture.png",
				new Vector3(0.0f, 7.8f, -8.0f),
				new Vector3(0.0f, 1.35f, -29.0f),
				58.0f),
			new(
				"03_furniture_office.png",
				new Vector3(0.0f, 7.0f, -38.0f),
				new Vector3(0.0f, 1.15f, -57.0f),
				59.0f),
			new(
				"04_tabletop_micro_detail.png",
				new Vector3(0.0f, 4.9f, -68.5f),
				new Vector3(0.0f, 1.05f, -84.0f),
				57.0f),
		};

		string outputDirectory = ProjectSettings.GlobalizePath(
			"res://.godot/user_supplied_asset_visual_review");
		Error directoryError =
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
		Require(
			directoryError == Error.Ok ||
			directoryError == Error.AlreadyExists,
			$"could not create review directory: {directoryError}");

		foreach (ReviewShot shot in shots)
		{
			camera.Position = shot.Position;
			camera.Fov = shot.Fov;
			camera.LookAt(shot.Target, Vector3.Up);

			for (int frame = 0; frame < 5; frame++)
			{
				await ToSignal(
					GetTree(),
					SceneTree.SignalName.ProcessFrame);
			}
			await ToSignal(
				RenderingServer.Singleton,
				RenderingServer.SignalName.FramePostDraw);

			Image image = GetViewport().GetTexture().GetImage();
			Require(!image.IsEmpty(), $"captured empty image for {shot.FileName}");
			Error saveError = image.SavePng(
				Path.Combine(outputDirectory, shot.FileName));
			Require(
				saveError == Error.Ok,
				$"could not save {shot.FileName}: {saveError}");
		}

		GD.Print(
			$"USER_SUPPLIED_ASSET_VISUAL_REVIEW: PASS " +
			$"({Assets.Length} wrappers; captures at {outputDirectory})");
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

	private static bool IsValidSize(Vector3 size)
	{
		return
			float.IsFinite(size.X) &&
			float.IsFinite(size.Y) &&
			float.IsFinite(size.Z) &&
			size.X > 0.0001f &&
			size.Y > 0.0001f &&
			size.Z > 0.0001f;
	}

	private static string MakeSafeNodeName(string displayName)
	{
		return displayName
			.Replace(" ", string.Empty, StringComparison.Ordinal)
			.Replace("-", string.Empty, StringComparison.Ordinal)
			.Replace("/", string.Empty, StringComparison.Ordinal);
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
