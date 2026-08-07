#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.World.County;

/// <summary>
/// Sky, sun, shadows and aerial perspective for the open county.
///
/// The Main Street slice was lit for a two-hundred-metre street: dense depth fog,
/// a single shadow cascade, a short camera far plane. None of that survives being
/// asked to render an eight-kilometre valley - the fog turns the middle distance
/// into grey soup and the shadows either shimmer at your feet or vanish at fifty
/// metres. This node owns the county's own rig so the two never fight.
///
/// Depth precision is the reason the far plane is 9km rather than something
/// generous: Fire Lookout stands 1.4km above the southern river mouth, and pushing
/// the far plane further starts z-fighting distant terrain against itself.
/// </summary>
[Tool]
public partial class CountyAtmosphere : Node3D
{
    [Export] public bool ApplyOnReady { get; set; } = true;

    /// <summary>Sun elevation in degrees above the horizon.</summary>
    [Export(PropertyHint.Range, "-10,90,0.5")]
    public float SunElevation { get; set; } = 34.0f;

    /// <summary>Sun bearing in degrees, 0 = from the north.</summary>
    [Export(PropertyHint.Range, "0,360,1")]
    public float SunAzimuth { get; set; } = 232.0f;

    [Export] public Color SunColor { get; set; } = new(1.0f, 0.947f, 0.878f);

    [Export(PropertyHint.Range, "0,4,0.05")]
    public float SunEnergy { get; set; } = 1.15f;

    /// <summary>
    /// How thick the air reads over distance. Aerial perspective is the main cue
    /// the eye uses to judge scale, so this is what makes the far ridgelines feel
    /// kilometres away rather than like a painted backdrop.
    ///
    /// Exponential fog reaches about 63 percent opacity at 1/density metres, so
    /// this puts the half-way point near 9km - beyond the far corner of the county.
    /// The whole landmass stays legible from the ridges while still gaining depth.
    /// Anything above roughly 0.0005 erases the landscape entirely.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,0.01,0.0001")]
    public float HazeDensity { get; set; } = 0.00006f;

    /// <summary>
    /// Haze colour. Deliberately darker than it looks like it should be.
    ///
    /// Fog blends the landscape toward this colour, so a pale haze does not read as
    /// distance - it reads as the world being erased. At the previous
    /// (0.60, 0.68, 0.80) even 28 percent fog over three kilometres turned forested
    /// ridges into flat grey-green, because the haze was several times brighter
    /// than the terrain underneath it. Keeping it dark and blue preserves the
    /// depth cue while leaving the far ridgelines legible as landform.
    /// </summary>
    [Export] public Color HazeColor { get; set; } = new(0.42f, 0.52f, 0.66f);

    /// <summary>Far plane. Also the distance the fog is tuned to saturate at.</summary>
    [Export] public float ViewDistance { get; set; } = 9000.0f;

    [Export] public bool EnableVolumetricFog { get; set; }

    private const string SkyPanoramaPath = "res://assets/environment/sky/ashwood_late_afternoon_sky.hdr";

    private WorldEnvironment? _worldEnvironment;
    private DirectionalLight3D? _sun;

    public override void _Ready()
    {
        if (ApplyOnReady)
        {
            Apply();
        }
    }

    /// <summary>Builds or updates the environment and sun. Safe to call repeatedly.</summary>
    public void Apply()
    {
        EnsureNodes();
        ConfigureSun();
        ConfigureEnvironment();
        ConfigureCamera();
    }

    private void EnsureNodes()
    {
        _worldEnvironment = GetNodeOrNull<WorldEnvironment>("WorldEnvironment");
        if (_worldEnvironment == null)
        {
            _worldEnvironment = new WorldEnvironment { Name = "WorldEnvironment" };
            AddChild(_worldEnvironment);
            _worldEnvironment.Owner = Owner ?? this;
        }

        _sun = GetNodeOrNull<DirectionalLight3D>("Sun");
        if (_sun == null)
        {
            _sun = new DirectionalLight3D { Name = "Sun" };
            AddChild(_sun);
            _sun.Owner = Owner ?? this;
        }
    }

    private void ConfigureSun()
    {
        if (_sun == null)
        {
            return;
        }

        _sun.LightColor = SunColor;
        _sun.LightEnergy = SunEnergy;
        _sun.LightAngularDistance = 0.55f;
        _sun.ShadowEnabled = OS.GetEnvironment("NO_SHADOW") != "1";

        // Four cascades over 1.2km. Everything past that is lit but unshadowed,
        // which nobody notices at distance and saves the whole far field.
        _sun.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Parallel4Splits;
        _sun.DirectionalShadowMaxDistance = 1200.0f;
        _sun.DirectionalShadowSplit1 = 0.045f;   // ~54m: the ground around the player
        _sun.DirectionalShadowSplit2 = 0.14f;    // ~168m: nearby buildings and trees
        _sun.DirectionalShadowSplit3 = 0.42f;    // ~504m: the local valley
        _sun.DirectionalShadowBlendSplits = true;
        _sun.DirectionalShadowFadeStart = 0.92f;

        // Long shadows at a low sun angle are exactly where peter-panning and acne
        // fight each other; a normal-offset bias handles both better than depth
        // bias alone on terrain this large.
        _sun.ShadowBias = 0.045f;
        _sun.ShadowNormalBias = 1.6f;
        _sun.ShadowBlur = 1.0f;

        float elevation = Mathf.DegToRad(SunElevation);
        float azimuth = Mathf.DegToRad(SunAzimuth);
        _sun.Rotation = new Vector3(-elevation, azimuth, 0.0f);
    }

    private void ConfigureEnvironment()
    {
        if (_worldEnvironment == null)
        {
            return;
        }

        Godot.Environment environment = _worldEnvironment.Environment ?? new Godot.Environment();

        environment.BackgroundMode = Godot.Environment.BGMode.Sky;
        environment.BackgroundEnergyMultiplier = 1.0f;
        environment.Sky = BuildSky(environment.Sky);

        environment.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
        environment.AmbientLightSkyContribution = 1.0f;
        environment.AmbientLightEnergy = 0.85f;
        environment.ReflectedLightSource = Godot.Environment.ReflectionSource.Sky;

        environment.TonemapMode = Godot.Environment.ToneMapper.Aces;
        environment.TonemapExposure = 0.95f;
        environment.TonemapWhite = 6.0f;

        // The editor viewport already carries more per-frame overhead than the
        // running game (gizmos, selection outlines, no occlusion tuning), so the
        // heaviest post effects are skipped there rather than doubling up on cost
        // for a viewport that is for inspection, not for judging final lighting.
        bool cheap = Engine.IsEditorHint();

        // SSAO at county scale wants a wider radius than a room interior, or it
        // only darkens the grass blades and leaves the landforms flat.
        environment.SsaoEnabled = !cheap && OS.GetEnvironment("NO_SSAO") != "1";
        environment.SsaoRadius = 2.4f;
        environment.SsaoIntensity = 1.9f;
        environment.SsaoPower = 1.5f;
        environment.SsaoDetail = 0.4f;
        environment.SsaoHorizon = 0.07f;
        environment.SsaoSharpness = 0.96f;
        environment.SsaoLightAffect = 0.15f;

        environment.SsrEnabled = false;
        environment.SsilEnabled = false;

        environment.GlowEnabled = !cheap;
        environment.GlowNormalized = true;
        environment.GlowIntensity = 0.4f;
        environment.GlowStrength = 1.0f;
        environment.GlowBloom = 0.03f;
        environment.GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Softlight;
        environment.GlowHdrThreshold = 1.25f;
        environment.SetGlowLevel(0, 0.0f);
        environment.SetGlowLevel(1, 0.15f);
        environment.SetGlowLevel(2, 0.55f);
        environment.SetGlowLevel(3, 0.65f);
        environment.SetGlowLevel(4, 0.4f);

        // Exponential depth fog, not the street slice's dense linear fog. At this
        // density the far rim of the county sits at roughly 90 percent haze, which
        // reads as distance rather than as a wall of grey.
        environment.FogEnabled = OS.GetEnvironment("NO_FOG") != "1";
        environment.FogMode = Godot.Environment.FogModeEnum.Exponential;
        environment.FogLightColor = HazeColor;
        environment.FogLightEnergy = 0.7f;
        environment.FogSunScatter = 0.16f;
        environment.FogDensity = HazeDensity;
        environment.FogAerialPerspective = 0.22f;
        environment.FogSkyAffect = 0.06f;

        // Height fog: cold air pools in the river valley and the lake basin at
        // dawn, which is free atmosphere and reads beautifully from the ridges.
        environment.FogHeightDensity = 0.28f;
        environment.FogHeight = -90.0f;

        // Volumetric fog is genuinely expensive on the integrated GPU this targets,
        // so it stays opt-in rather than on by default.
        environment.VolumetricFogEnabled = EnableVolumetricFog;
        if (EnableVolumetricFog)
        {
            environment.VolumetricFogDensity = 0.012f;
            environment.VolumetricFogAlbedo = new Color(0.86f, 0.89f, 0.95f);
            environment.VolumetricFogLength = 220.0f;
            environment.VolumetricFogDetailSpread = 1.8f;
            environment.VolumetricFogAmbientInject = 0.25f;
        }

        _worldEnvironment.Environment = environment;
    }

    private Sky BuildSky(Sky? existing)
    {
        Sky sky = existing ?? new Sky();

        // Reuse the panorama if it is already set up, so a hand-tuned sky in the
        // scene file is not clobbered every time this runs.
        if (sky.SkyMaterial is PanoramaSkyMaterial)
        {
            return sky;
        }

        if (ResourceLoader.Exists(SkyPanoramaPath) &&
            ResourceLoader.Load(SkyPanoramaPath) is Texture2D panorama)
        {
            sky.SkyMaterial = new PanoramaSkyMaterial
            {
                Panorama = panorama,
                EnergyMultiplier = 1.0f,
            };
        }
        else
        {
            // A physical sky is a reasonable fallback and still gives correct
            // sun-relative scattering, which matters more than the exact image.
            sky.SkyMaterial = new PhysicalSkyMaterial
            {
                RayleighCoefficient = 2.1f,
                MieCoefficient = 0.0045f,
                Turbidity = 3.4f,
                GroundColor = new Color(0.24f, 0.26f, 0.24f),
            };
        }

        sky.RadianceSize = Sky.RadianceSizeEnum.Size256;
        sky.ProcessMode = Sky.ProcessModeEnum.Realtime;
        return sky;
    }

    private void ConfigureCamera()
    {
        // The camera lives with the player, not with this node, so reach for it
        // through the viewport rather than assuming a scene layout.
        Camera3D? camera = GetViewport()?.GetCamera3D();
        if (camera == null)
        {
            return;
        }

        camera.Far = ViewDistance;

        // 0.25m near plane. Closer buys nothing at third person and costs depth
        // precision that the far ridgelines need.
        camera.Near = 0.25f;
    }
}
