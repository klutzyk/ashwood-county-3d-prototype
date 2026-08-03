#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>
/// Renders res://assets/materials/ashwood_terrain.gdshader and
/// res://assets/materials/ashwood_distant_hills.gdshader in isolation, on
/// purpose-built landform that stresses exactly the failures the material
/// exists to fix:
///
///   - ground at player eye height (the "low-resolution brown smear" framing),
///   - flat ground at a grazing angle out to the horizon (tiling and repetition),
///   - a 50 degree gorge wall and a 63 degree escarpment (vertical smearing,
///     which is what triplanar projection is here to remove),
///   - the slope transition band where grass hands over to dirt and then rock
///     (which must never read as a straight line or a rectangular patch),
///   - the distant vista, which must sit in fog and not read as flat green.
///
/// Lighting and Environment are copied from the same source as
/// OldMillBridgeVisualReview so these captures are comparable with the rest of
/// the review set rather than being lit flatteringly in isolation.
///
/// This is an art acceptance check. It does not assert on pixels; it produces
/// images a human has to look at. It DOES assert that every texture slot on the
/// materials actually resolved, because a silently unbound albedo sampler is
/// what produced the untextured terrain in the first place.
/// </summary>
public partial class TerrainMaterialPreview : Node3D
{
	private const string TerrainMaterialPath =
		"res://assets/materials/ashwood_terrain.tres";
	private const string GorgeMaterialPath =
		"res://assets/materials/ashwood_terrain_gorge.tres";
	private const string HillsMaterialPath =
		"res://assets/materials/ashwood_distant_hills.tres";

	/// <summary>Every sampler uniform the terrain shader declares.</summary>
	private static readonly string[] TerrainSamplerUniforms =
	{
		"grass_albedo", "grass_normal", "grass_arm",
		"forest_albedo", "forest_normal", "forest_arm",
		"dirt_albedo", "dirt_normal", "dirt_arm",
		"rock_albedo", "rock_normal", "rock_arm",
	};

	private readonly record struct ReviewShot(
		string FileName,
		Vector3 CameraPosition,
		Vector3 CameraTarget,
		float Fov);

	public override async void _Ready()
	{
		try
		{
			var terrainMaterial = GD.Load<ShaderMaterial>(TerrainMaterialPath);
			var gorgeMaterial = GD.Load<ShaderMaterial>(GorgeMaterialPath);
			var hillsMaterial = GD.Load<ShaderMaterial>(HillsMaterialPath);

			VerifyMaterial(terrainMaterial, TerrainMaterialPath, TerrainSamplerUniforms);
			VerifyMaterial(gorgeMaterial, GorgeMaterialPath, TerrainSamplerUniforms);
			VerifyMaterial(hillsMaterial, HillsMaterialPath, Array.Empty<string>());

			SubViewport captureViewport = new()
			{
				Name = "CaptureViewport",
				Size = new Vector2I(1920, 1080),
				OwnWorld3D = true,
				RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			};
			AddChild(captureViewport);

			// Near landform: rolling ground, a meandering gorge with 50 degree
			// walls, and a 63 degree escarpment on the east side.
			captureViewport.AddChild(new MeshInstance3D
			{
				Name = "Terrain",
				Mesh = BuildTerrainMesh(),
				MaterialOverride = gorgeMaterial,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
			});

			// Vista band behind the near landform.
			captureViewport.AddChild(new MeshInstance3D
			{
				Name = "DistantHills",
				Mesh = BuildHillsMesh(),
				MaterialOverride = hillsMaterial,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			});

			// A second patch under the general (non-gorge) terrain material, sited
			// well away from the gorge so both variants can be judged side by side.
			var generalPatch = new MeshInstance3D
			{
				Name = "GeneralTerrainPatch",
				Mesh = BuildFlatPatchMesh(),
				MaterialOverride = terrainMaterial,
				// Clear of the main landform (which ends at z = 60) so the two
				// meshes never intersect.
				Position = new Vector3(0.0f, 0.02f, 105.0f),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.On,
			};
			captureViewport.AddChild(generalPatch);

			captureViewport.AddChild(BuildSun());
			captureViewport.AddChild(BuildEnvironment());

			// A 1.8m reference figure, so texture scale can be judged against
			// something with a known size instead of by eye.
			captureViewport.AddChild(new MeshInstance3D
			{
				Name = "ScaleReference",
				Mesh = new CapsuleMesh { Radius = 0.3f, Height = 1.8f },
				Position = new Vector3(24.0f, TerrainHeight(24.0f, 22.0f) + 0.9f, 22.0f),
				MaterialOverride = new StandardMaterial3D
				{
					AlbedoColor = new Color(0.82f, 0.24f, 0.18f),
					Roughness = 0.85f,
				},
			});

			for (int frame = 0; frame < 10; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			Camera3D camera = new()
			{
				Name = "ReviewCamera",
				Current = true,
				Near = 0.05f,
				Far = 3000.0f,
			};
			captureViewport.AddChild(camera);

			string outputDirectory = ProjectSettings.GlobalizePath(
				"res://.godot/terrain_material_preview");
			DirAccess.MakeDirRecursiveAbsolute(outputDirectory);

			float eye = TerrainHeight(26.0f, 20.0f) + 1.65f;
			ReviewShot[] shots =
			{
				// The exact framing the screenshot complaint came from: standing on
				// the ground, looking at it a few metres ahead.
				new("01_ground_at_eye_height.png",
					new Vector3(26.0f, eye, 20.0f),
					new Vector3(23.0f, TerrainHeight(23.0f, 12.0f), 12.0f), 60.0f),

				// Grazing angle to the horizon. Any surviving tile grid shows here.
				new("02_ground_grazing_to_horizon.png",
					new Vector3(30.0f, TerrainHeight(30.0f, 55.0f) + 2.1f, 55.0f),
					new Vector3(26.0f, TerrainHeight(26.0f, -40.0f) + 1.0f, -40.0f), 65.0f),

				// Straight down, close. Worst case for a stretched top-down
				// projection and for detail-tile resolution.
				new("03_ground_top_down_close.png",
					new Vector3(24.0f, TerrainHeight(24.0f, 20.0f) + 3.2f, 20.0f),
					new Vector3(24.0f, TerrainHeight(24.0f, 20.0f), 20.05f), 55.0f),

				// 50 degree gorge wall seen from inside the channel. This is the
				// shot that proves triplanar is working: no vertical streaking.
				new("04_gorge_wall_triplanar.png",
					new Vector3(-2.0f, -7.5f, 6.0f),
					new Vector3(14.0f, -2.0f, 4.0f), 58.0f),

				// The rim, where rock hands back to dirt and then grass.
				new("05_rim_slope_transition.png",
					new Vector3(26.0f, 6.0f, 18.0f),
					new Vector3(6.0f, -6.0f, 6.0f), 55.0f),

				// 63 degree escarpment face.
				new("06_escarpment_face.png",
					new Vector3(20.0f, 6.5f, -6.0f),
					new Vector3(34.0f, 8.0f, -6.0f), 52.0f),

				// Wide landform. Macro variation should read as terrain colour
				// change, and no repeating grid should be visible anywhere.
				new("07_wide_landform.png",
					new Vector3(-70.0f, 46.0f, 92.0f),
					new Vector3(6.0f, -4.0f, 0.0f), 50.0f),

				// The vista on its own.
				new("08_distant_hills.png",
					new Vector3(0.0f, 22.0f, 120.0f),
					new Vector3(0.0f, 60.0f, -700.0f), 48.0f),

				// Vista and near ground together: they have to sit in the same
				// atmosphere, with the hills clearly further away.
				new("09_hills_over_terrain.png",
					new Vector3(40.0f, 12.0f, 150.0f),
					new Vector3(0.0f, 20.0f, -500.0f), 55.0f),

				// General (non-gorge) terrain variant.
				new("10_general_terrain_variant.png",
					new Vector3(6.0f, 3.4f, 119.0f),
					new Vector3(-2.0f, 0.0f, 101.0f), 58.0f),
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

			GD.Print(
				$"TERRAIN_MATERIAL_PREVIEW: PASS - {shots.Length} renders saved to " +
				$"{outputDirectory} ({captureViewport.Size.X}x{captureViewport.Size.Y})");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError("TERRAIN_MATERIAL_PREVIEW: FAIL - " + exception);
			GetTree().Quit(1);
		}
	}

	/// <summary>
	/// Fails loudly if a material did not load, has no shader, or leaves any
	/// sampler slot empty. An unbound albedo sampler renders as flat white or
	/// flat colour, which is the exact defect this material set replaces, and it
	/// is silent at runtime otherwise.
	/// </summary>
	private static void VerifyMaterial(
		ShaderMaterial? material, string path, IReadOnlyList<string> samplerUniforms)
	{
		if (material is null)
		{
			throw new InvalidOperationException($"Could not load {path}");
		}
		if (material.Shader is null)
		{
			throw new InvalidOperationException($"{path} has no shader assigned");
		}

		var missing = new List<string>();
		foreach (string uniform in samplerUniforms)
		{
			Variant value = material.GetShaderParameter(uniform);
			if (value.VariantType == Variant.Type.Nil ||
				value.As<Texture2D>() is null)
			{
				missing.Add(uniform);
			}
		}

		if (missing.Count > 0)
		{
			throw new InvalidOperationException(
				$"{path} has unbound texture slots: {string.Join(", ", missing)}");
		}

		GD.Print(
			$"TERRAIN_MATERIAL_PREVIEW: {path} ok " +
			$"({samplerUniforms.Count} texture slots bound)");
	}

	// ======================================================================
	// Landform
	// ======================================================================

	private const float TerrainHalfExtent = 60.0f;
	private const int TerrainSteps = 150;

	/// <summary>
	/// Preview landform. Deliberately covers the whole slope range in one mesh:
	/// near-flat rolling ground, a meandering gorge with ~50 degree walls, and a
	/// ~63 degree escarpment, so a single scene exercises every layer mask and
	/// both projection paths in the shader.
	/// </summary>
	private static float TerrainHeight(float x, float z)
	{
		float rolling =
			1.6f * Mathf.Sin(x * 0.06f + z * 0.05f)
			+ 0.9f * Mathf.Sin(x * 0.13f - z * 0.11f)
			+ 0.4f * Mathf.Sin(x * 0.29f + z * 0.23f);

		// Meandering channel centred near x = 0.
		float centre = 6.0f * Mathf.Sin(z * 0.045f);
		float d = Mathf.Abs(x - centre);
		float gorge;
		if (d <= 8.0f)
		{
			gorge = -12.0f;
		}
		else if (d <= 18.0f)
		{
			// 12 m of fall over 10 m of run: about 50 degrees.
			gorge = Mathf.Lerp(-12.0f, 0.0f, Smooth((d - 8.0f) / 10.0f));
		}
		else
		{
			gorge = 0.0f;
		}

		// Escarpment: 14 m of rise over 7 m of run, about 63 degrees.
		float escarpment = 14.0f * Smooth((x - 28.0f) / 7.0f);

		return rolling + gorge + escarpment;
	}

	private static float Smooth(float t)
	{
		t = Mathf.Clamp(t, 0.0f, 1.0f);
		return t * t * (3.0f - 2.0f * t);
	}

	private static ArrayMesh BuildTerrainMesh()
	{
		var surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		float step = (TerrainHalfExtent * 2.0f) / TerrainSteps;
		for (int ix = 0; ix < TerrainSteps; ix++)
		{
			float x0 = -TerrainHalfExtent + ix * step;
			float x1 = x0 + step;
			for (int iz = 0; iz < TerrainSteps; iz++)
			{
				float z0 = -TerrainHalfExtent + iz * step;
				float z1 = z0 + step;

				Vector3 v00 = new(x0, TerrainHeight(x0, z0), z0);
				Vector3 v01 = new(x0, TerrainHeight(x0, z1), z1);
				Vector3 v10 = new(x1, TerrainHeight(x1, z0), z0);
				Vector3 v11 = new(x1, TerrainHeight(x1, z1), z1);

				// Winding chosen so face normals point up (+Y).
				AddVertex(surface, v00);
				AddVertex(surface, v11);
				AddVertex(surface, v10);

				AddVertex(surface, v00);
				AddVertex(surface, v01);
				AddVertex(surface, v11);
			}
		}

		surface.GenerateNormals();
		return surface.Commit();
	}

	/// <summary>Gently rolling patch used to show the general terrain variant.</summary>
	private static ArrayMesh BuildFlatPatchMesh()
	{
		var surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		const float half = 26.0f;
		const int steps = 64;
		float step = (half * 2.0f) / steps;

		for (int ix = 0; ix < steps; ix++)
		{
			float x0 = -half + ix * step;
			float x1 = x0 + step;
			for (int iz = 0; iz < steps; iz++)
			{
				float z0 = -half + iz * step;
				float z1 = z0 + step;

				Vector3 v00 = new(x0, PatchHeight(x0, z0), z0);
				Vector3 v01 = new(x0, PatchHeight(x0, z1), z1);
				Vector3 v10 = new(x1, PatchHeight(x1, z0), z0);
				Vector3 v11 = new(x1, PatchHeight(x1, z1), z1);

				AddVertex(surface, v00);
				AddVertex(surface, v11);
				AddVertex(surface, v10);

				AddVertex(surface, v00);
				AddVertex(surface, v01);
				AddVertex(surface, v11);
			}
		}

		surface.GenerateNormals();
		return surface.Commit();
	}

	private static float PatchHeight(float x, float z)
	{
		return 1.1f * Mathf.Sin(x * 0.09f + z * 0.07f)
			+ 0.6f * Mathf.Sin(x * 0.19f - z * 0.15f)
			+ 2.6f * Smooth((x - 6.0f) / 9.0f);
	}

	// ======================================================================
	// Vista
	// ======================================================================

	private static float HillsHeight(float x, float z)
	{
		float h =
			60.0f * (0.5f + 0.5f * Mathf.Sin(x * 0.0045f))
				* (0.5f + 0.5f * Mathf.Sin(z * 0.0061f + 2.1f))
			+ 34.0f * (0.5f + 0.5f * Mathf.Sin(x * 0.0111f - 1.2f))
			+ 16.0f * (0.5f + 0.5f * Mathf.Sin(x * 0.021f + z * 0.017f));

		// Fall away toward the near edge so the vista does not form a wall
		// directly behind the playable landform.
		float approach = Smooth((-z - 260.0f) / 220.0f);
		return h * approach;
	}

	private static ArrayMesh BuildHillsMesh()
	{
		var surface = new SurfaceTool();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		const float minX = -1400.0f;
		const float maxX = 1400.0f;
		const float minZ = -1600.0f;
		const float maxZ = -180.0f;
		const int stepsX = 90;
		const int stepsZ = 60;

		float dx = (maxX - minX) / stepsX;
		float dz = (maxZ - minZ) / stepsZ;

		for (int ix = 0; ix < stepsX; ix++)
		{
			float x0 = minX + ix * dx;
			float x1 = x0 + dx;
			for (int iz = 0; iz < stepsZ; iz++)
			{
				float z0 = minZ + iz * dz;
				float z1 = z0 + dz;

				Vector3 v00 = new(x0, HillsHeight(x0, z0), z0);
				Vector3 v01 = new(x0, HillsHeight(x0, z1), z1);
				Vector3 v10 = new(x1, HillsHeight(x1, z0), z0);
				Vector3 v11 = new(x1, HillsHeight(x1, z1), z1);

				AddVertex(surface, v00);
				AddVertex(surface, v11);
				AddVertex(surface, v10);

				AddVertex(surface, v00);
				AddVertex(surface, v01);
				AddVertex(surface, v11);
			}
		}

		surface.GenerateNormals();
		return surface.Commit();
	}

	/// <summary>
	/// The terrain shader projects in world space and needs neither UVs nor
	/// tangents, but a UV is written anyway so the mesh stays usable with
	/// ordinary materials during debugging.
	/// </summary>
	private static void AddVertex(SurfaceTool surface, Vector3 v)
	{
		surface.SetUV(new Vector2(v.X * 0.1f, v.Z * 0.1f));
		surface.AddVertex(v);
	}

	// ======================================================================
	// Lighting
	// ======================================================================

	private static DirectionalLight3D BuildSun()
	{
		// Same sun as OldMillBridgeVisualReview: a mid-late afternoon angle,
		// which is what the player sees for most of the day cycle and the only
		// angle that shows form on both north and south facing slopes.
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
			// Extended past the bridge review's 420 m so the vista band is inside
			// the fog ramp instead of being clamped to full fog colour.
			FogDepthEnd = 1600.0f,
		};

		return new WorldEnvironment { Name = "ReviewEnvironment", Environment = environment };
	}
}
