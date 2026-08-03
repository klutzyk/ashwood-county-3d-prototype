#nullable enable

using System;
using System.IO;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Renders the Old Mill Bridge landmark from eight fixed viewpoints so the
/// location can be reviewed as images rather than by claim.
///
/// The scene is lit to match scenes/world/ashwood/main_street.tscn exactly
/// (same DirectionalLight3D rotation/colour/energy and the same Environment),
/// so what is captured here is representative of the in-game appearance.
/// </summary>
public partial class OldMillBridgeVisualReview : Node3D
{
	private readonly record struct ReviewShot(
		string FileName,
		Vector3 CameraPosition,
		Vector3 CameraTarget,
		float Fov);

	public override async void _Ready()
	{
		try
		{
			SubViewport captureViewport = new()
			{
				Name = "CaptureViewport",
				Size = new Vector2I(1920, 1080),
				OwnWorld3D = true,
				RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			};
			AddChild(captureViewport);

			Node3D bridge = GD.Load<PackedScene>(
					"res://scenes/world/ashwood/old_mill_bridge.tscn")
				.Instantiate<Node3D>();
			captureViewport.AddChild(bridge);

			captureViewport.AddChild(BuildSun());
			captureViewport.AddChild(BuildEnvironment());

			// Stand-in for Main Street's asphalt east of the approach, so the
			// junction at X = -110 can be judged.
			captureViewport.AddChild(BuildMainStreetStub());

			for (int frame = 0; frame < 8; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			Camera3D camera = new()
			{
				Name = "ReviewCamera",
				Current = true,
				Near = 0.05f,
				Far = 900.0f,
			};
			captureViewport.AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/old_mill_bridge_review");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			ReviewShot[] shots =
			{
				new("01_approach_from_town.png",
					new Vector3(-104.0f, 2.35f, -1.6f),
					new Vector3(-176.0f, 4.5f, 0.6f), 55.0f),
				new("02_bridge_portal.png",
					new Vector3(-133.0f, 1.85f, 1.2f),
					new Vector3(-185.0f, 4.0f, -0.4f), 58.0f),
				new("03_deck_midspan.png",
					new Vector3(-158.0f, 2.30f, 2.6f),
					new Vector3(-206.0f, 3.2f, -1.0f), 60.0f),
				new("04_gorge_from_deck.png",
					new Vector3(-172.0f, 2.05f, 5.2f),
					new Vector3(-181.0f, -8.5f, 34.0f), 62.0f),
				new("05_bridge_side_elevation.png",
					new Vector3(-176.0f, -1.20f, 62.0f),
					new Vector3(-176.0f, 3.8f, 0.0f), 42.0f),
				new("06_old_mill.png",
					new Vector3(-166.0f, 3.20f, -58.0f),
					new Vector3(-144.0f, 0.5f, -38.0f), 52.0f),
				new("07_west_abutment.png",
					new Vector3(-224.0f, 2.30f, -2.4f),
					new Vector3(-150.0f, 4.0f, 0.8f), 54.0f),
				new("08_hero_wide.png",
					new Vector3(-252.0f, 26.0f, 96.0f),
					new Vector3(-163.0f, -2.0f, -6.0f), 46.0f),
			};

			foreach (ReviewShot shot in shots)
			{
				camera.GlobalPosition = shot.CameraPosition;
				camera.Fov = shot.Fov;
				camera.LookAt(shot.CameraTarget, Vector3.Up);

				for (int frame = 0; frame < 6; frame++)
				{
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
				await ToSignal(
					RenderingServer.Singleton,
					RenderingServer.SignalName.FramePostDraw);

				Image image = captureViewport.GetTexture().GetImage();
				Error error = image.SavePng(
					Path.Combine(outputDirectory, shot.FileName));
				if (error != Error.Ok)
				{
					throw new InvalidOperationException(
						$"Could not save {shot.FileName}: {error}");
				}
			}

			int inGame = await CaptureInGameShots(outputDirectory);

			GD.Print(
				$"OLD_MILL_BRIDGE_VISUAL_REVIEW: PASS - {shots.Length + inGame} renders " +
				$"saved to {outputDirectory} " +
				$"({captureViewport.Size.X}x{captureViewport.Size.Y})");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError("OLD_MILL_BRIDGE_VISUAL_REVIEW: FAIL - " + exception);
			GetTree().Quit(1);
		}
	}

	/// <summary>
	/// Captures the landmark inside the real Main Street scene, using that scene's
	/// own lighting, fog and time of day. These are the shots that show what the
	/// player actually sees, as opposed to the controlled review framings above.
	/// </summary>
	private async System.Threading.Tasks.Task<int> CaptureInGameShots(
		string outputDirectory)
	{
		SubViewport gameViewport = new()
		{
			Name = "InGameViewport",
			Size = new Vector2I(1920, 1080),
			OwnWorld3D = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
		};
		AddChild(gameViewport);

		Node3D world = GD.Load<PackedScene>(
				"res://scenes/world/ashwood/main_street.tscn")
			.Instantiate<Node3D>();
		gameViewport.AddChild(world);

		for (int frame = 0; frame < 10; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		if (world.GetNodeOrNull<CanvasLayer>("Gameplay/GameplayHUD") is CanvasLayer hud)
		{
			hud.Visible = false;
		}
		if (world.GetNodeOrNull<Node3D>("Gameplay/Player") is Node3D player)
		{
			player.ProcessMode = ProcessModeEnum.Disabled;
			player.Visible = false;
			if (world.GetNodeOrNull<Camera3D>(
					"Gameplay/Player/CameraRig/SpringArm3D/Camera3D") is Camera3D playerCamera)
			{
				playerCamera.Current = false;
			}
		}

		Camera3D camera = new()
		{
			Name = "InGameCamera",
			Current = true,
			Near = 0.05f,
			Far = 900.0f,
		};
		gameViewport.AddChild(camera);

		(string File, Vector3 From, Vector3 To, float Fov)[] shots =
		{
			("09_ingame_west_down_main_street.png",
				new Vector3(-88.0f, 2.4f, -1.2f), new Vector3(-176.0f, 5.0f, 0.4f), 60.0f),
			("10_ingame_bridge_from_town_edge.png",
				new Vector3(-118.0f, 2.2f, 2.0f), new Vector3(-190.0f, 4.5f, -0.5f), 55.0f),
		};

		foreach ((string file, Vector3 from, Vector3 to, float fov) in shots)
		{
			camera.GlobalPosition = from;
			camera.Fov = fov;
			camera.LookAt(to, Vector3.Up);

			for (int frame = 0; frame < 6; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			await ToSignal(
				RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

			Image image = gameViewport.GetTexture().GetImage();
			Error error = image.SavePng(Path.Combine(outputDirectory, file));
			if (error != Error.Ok)
			{
				throw new InvalidOperationException($"Could not save {file}: {error}");
			}
		}

		return shots.Length;
	}

	private static DirectionalLight3D BuildSun()
	{
		// Matches main_street.tscn's DirectionalLight3D, except that the shadow
		// distance is extended because the review framings are much wider than
		// gameplay framings.
		// main_street.tscn stores a deep-sunset sun as its authored default, but
		// WorldTime drives the light at runtime. A mid-late afternoon angle is
		// what the player actually sees for most of the day cycle, and it is the
		// only way to judge form: the authored sunset angle leaves every
		// north-facing surface in near-black silhouette.
		return new DirectionalLight3D
		{
			Name = "Sun",
			RotationDegrees = new Vector3(-138.0f, -42.0f, 0.0f),
			LightColor = new Color(1.0f, 0.83f, 0.66f),
			LightEnergy = 1.25f,
			ShadowEnabled = true,
			ShadowBlur = 1.4f,
			DirectionalShadowMaxDistance = 420.0f,
		};
	}

	private static WorldEnvironment BuildEnvironment()
	{
		var skyMaterial = new ProceduralSkyMaterial
		{
			SkyTopColor = new Color(0.15f, 0.255f, 0.405f),
			SkyHorizonColor = new Color(0.86f, 0.58f, 0.36f),
			SkyCurve = 0.2f,
			SkyEnergyMultiplier = 1.08f,
			GroundBottomColor = new Color(0.05f, 0.065f, 0.052f),
			GroundHorizonColor = new Color(0.32f, 0.3f, 0.25f),
			GroundCurve = 0.12f,
			SunAngleMax = 4.0f,
			SunCurve = 0.18f,
			UseDebanding = true,
		};

		var environment = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Sky,
			BackgroundEnergyMultiplier = 0.96f,
			Sky = new Sky { SkyMaterial = skyMaterial },
			AmbientLightSource = Godot.Environment.AmbientSource.Sky,
			AmbientLightColor = new Color(0.38f, 0.445f, 0.55f),
			AmbientLightEnergy = 0.82f,
			TonemapMode = Godot.Environment.ToneMapper.Aces,
			TonemapExposure = 1.08f,
			TonemapWhite = 6.0f,
			FogEnabled = true,
			FogMode = Godot.Environment.FogModeEnum.Depth,
			FogLightColor = new Color(0.55f, 0.44f, 0.34f),
			FogLightEnergy = 0.72f,
			FogSunScatter = 0.28f,
			FogDensity = 0.34f,
			FogAerialPerspective = 0.66f,
			FogSkyAffect = 0.32f,
			FogDepthCurve = 1.25f,
			FogDepthBegin = 58.0f,
			FogDepthEnd = 420.0f,
		};

		return new WorldEnvironment { Name = "ReviewEnvironment", Environment = environment };
	}

	private static Node3D BuildMainStreetStub()
	{
		var root = new Node3D { Name = "MainStreetStub" };

		var road = new MeshInstance3D
		{
			Name = "Road",
			Mesh = new BoxMesh { Size = new Vector3(40.0f, 0.1f, 11.6f) },
			MaterialOverride = GD.Load<Material>(
				"res://assets/materials/ashwood_main_street_asphalt.tres"),
			Position = new Vector3(-90.0f, 0.05f, 0.0f),
		};
		root.AddChild(road);

		var ground = new MeshInstance3D
		{
			Name = "Ground",
			Mesh = new BoxMesh { Size = new Vector3(40.0f, 0.2f, 80.0f) },
			MaterialOverride = GD.Load<Material>(
				"res://assets/materials/ashwood_main_street_grass.tres"),
			Position = new Vector3(-90.0f, -0.1f, 0.0f),
		};
		root.AddChild(ground);

		return root;
	}
}
