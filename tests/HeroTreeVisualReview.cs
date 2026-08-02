#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Renders the project-owned, optimized Poly Haven tree in the Compatibility
/// renderer. This is an art and import acceptance check, not a gameplay test.
/// </summary>
public partial class HeroTreeVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 Position,
		Vector3 Target,
		float Fov);

	public override async void _Ready()
	{
		try
		{
			PackedScene treeScene = GD.Load<PackedScene>(
				"res://assets/environment/nature/ashwood_hero_tree_small_02.glb");
			Node3D tree = treeScene.Instantiate<Node3D>();
			tree.Name = "HeroTree";
			tree.Scale = Vector3.One * 1.7f;
			AddChild(tree);
			ReportMeshMaterials(tree);

			MeshInstance3D ground = new()
			{
				Name = "Ground",
				Mesh = new PlaneMesh
				{
					Size = new Vector2(30.0f, 30.0f),
					Material = new StandardMaterial3D
					{
						AlbedoColor = new Color("#526047"),
						Roughness = 0.94f,
					},
				},
			};
			AddChild(ground);

			WorldEnvironment worldEnvironment = new()
			{
				Environment = BuildEnvironment(),
			};
			AddChild(worldEnvironment);

			DirectionalLight3D keyLight = new()
			{
				LightColor = new Color("#ffd4ad"),
				LightEnergy = 1.35f,
				ShadowEnabled = true,
				RotationDegrees = new Vector3(-42.0f, -32.0f, 0.0f),
			};
			AddChild(keyLight);

			DirectionalLight3D fillLight = new()
			{
				LightColor = new Color("#a9c7df"),
				LightEnergy = 0.26f,
				ShadowEnabled = false,
				RotationDegrees = new Vector3(-25.0f, 142.0f, 0.0f),
			};
			AddChild(fillLight);

			Camera3D camera = new()
			{
				Name = "ReviewCamera",
				Current = true,
				Near = 0.05f,
				Far = 120.0f,
			};
			AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/hero_tree_visual_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);
			ReviewShot[] shots =
			{
				new(
					"01_full_silhouette.png",
					new Vector3(9.2f, 4.4f, 11.2f),
					new Vector3(0.0f, 3.1f, 0.0f),
					43.0f),
				new(
					"02_canopy_and_alpha.png",
					new Vector3(5.4f, 5.8f, 6.2f),
					new Vector3(0.0f, 4.6f, 0.0f),
					48.0f),
				new(
					"03_trunk_and_branch_materials.png",
					new Vector3(3.2f, 1.65f, 4.5f),
					new Vector3(0.0f, 1.75f, 0.0f),
					40.0f),
			};

			await WaitFrames(12);
			foreach (ReviewShot shot in shots)
			{
				camera.GlobalPosition = shot.Position;
				camera.Fov = shot.Fov;
				camera.LookAt(shot.Target, Vector3.Up);
				await WaitFrames(6);
				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);
				Error error = GetViewport().GetTexture().GetImage().SavePng(
					Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			int meshCount = CountMeshInstances(tree);
			GD.Print(
				$"HERO_TREE_VISUAL_REVIEW: PASS - {meshCount} mesh nodes; " +
				$"renders saved to {outputDirectory}");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError("HERO_TREE_VISUAL_REVIEW: FAIL - " + exception);
			GetTree().Quit(1);
		}
	}

	private static Godot.Environment BuildEnvironment()
	{
		ProceduralSkyMaterial skyMaterial = new()
		{
			SkyTopColor = new Color("#6889a0"),
			SkyHorizonColor = new Color("#d9b58f"),
			GroundBottomColor = new Color("#29302b"),
			GroundHorizonColor = new Color("#a49478"),
			SunAngleMax = 16.0f,
			SunCurve = 0.08f,
		};
		Sky sky = new()
		{
			SkyMaterial = skyMaterial,
			RadianceSize = Sky.RadianceSizeEnum.Size128,
		};
		return new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Sky,
			Sky = sky,
			AmbientLightSource = Godot.Environment.AmbientSource.Sky,
			AmbientLightEnergy = 0.68f,
			TonemapMode = Godot.Environment.ToneMapper.Filmic,
		};
	}

	private static int CountMeshInstances(Node node)
	{
		int count = node is MeshInstance3D ? 1 : 0;
		foreach (Node child in node.GetChildren())
		{
			count += CountMeshInstances(child);
		}
		return count;
	}

	private static void ReportMeshMaterials(Node node)
	{
		if (node is MeshInstance3D meshInstance && meshInstance.Mesh is not null)
		{
			for (int surface = 0; surface < meshInstance.Mesh.GetSurfaceCount(); surface++)
			{
				Material? material = meshInstance.Mesh.SurfaceGetMaterial(surface);
				GD.Print(
					$"HERO_TREE_MATERIAL: node={meshInstance.Name} " +
					$"surface={surface} name={material?.ResourceName ?? "<none>"} " +
					$"type={material?.GetClass() ?? "<none>"}");
			}
		}
		foreach (Node child in node.GetChildren())
		{
			ReportMeshMaterials(child);
		}
	}

	private async Task WaitFrames(int frameCount)
	{
		for (int frame = 0; frame < frameCount; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}
}
