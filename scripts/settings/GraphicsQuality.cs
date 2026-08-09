#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Settings;

/// <summary>
/// The graphics levers that actually move the frame, and what each preset sets
/// them to.
///
/// Every value here was measured on the target machine (an integrated AMD laptop
/// at 1280x720) rather than guessed, because almost every intuition about this
/// project's cost turned out to be wrong. What the measurements found, in order
/// of how much frame time they are worth:
///
///   terrain splat shader   ~24ms   swapping it for a plain material took the
///                                  frame from 21.5 to 43.5 FPS. It samples four
///                                  PBR layers at three maps each, twice over for
///                                  anti-tiling, plus a triplanar rock branch -
///                                  more than fifty texture fetches on the pixels
///                                  that hit every branch.
///   vegetation             ~26ms   removing it took 21.5 to 50 FPS. Alpha-tested
///                                  foliage is overdraw: a grass quad shades every
///                                  pixel it covers and then throws most of them
///                                  away, and they stack many deep near the camera.
///   3D render scale        ~17ms   half scale took 15 to 20 FPS.
///   Forward+ to Mobile     ~21ms   15.2 to 22.4 FPS. Forward+ is a clustered
///                                  desktop pipeline; Mobile uses a classic
///                                  forward light list and is far cheaper here.
///
/// And, importantly, what turned out NOT to matter, each verified rather than
/// assumed: MSAA (2x cost nothing measurable), shadow map size (4096 to 2048 was
/// noise), draw call count, primitive count, texture memory, and terrain
/// collision. Those are the usual suspects and none of them were the problem, so
/// they are deliberately not exposed as quality knobs that do nothing.
/// </summary>
public static class GraphicsQuality
{
    /// <summary>
    /// Resolution the 3D scene renders at, as a fraction of the window. The UI
    /// always renders at full resolution, so text stays sharp; only the world is
    /// scaled. This is the single most reliable lever for a weak GPU and is what
    /// most console games lean on to hold a frame rate.
    /// </summary>
    public static float RenderScale(GraphicsPreset preset) => preset switch
    {
        GraphicsPreset.Low => 0.6f,
        GraphicsPreset.Medium => 0.8f,
        _ => 1.0f,
    };

    /// <summary>
    /// Anti-tiling samples in the terrain material. At 1 the shader takes one
    /// sample per map instead of blending a second rotated one, which halves its
    /// texture fetches - by far the largest single saving available - at the cost
    /// of visible repetition on open ground.
    /// </summary>
    public static int TerrainSamples(GraphicsPreset preset) => preset switch
    {
        GraphicsPreset.Low => 1,
        _ => 2,
    };

    /// <summary>
    /// Whether steep ground gets the triplanar rock branch. Turning it off leaves
    /// cliffs stretched, so it is only dropped at Low where the alternative is an
    /// unplayable frame rate.
    /// </summary>
    public static bool TerrainTriplanar(GraphicsPreset preset) =>
        preset != GraphicsPreset.Low;

    /// <summary>
    /// Multiplier on every scatter layer's instance count. Foliage is overdraw
    /// bound, so this scales close to linearly with the vegetation cost.
    /// </summary>
    public static float VegetationDensity(GraphicsPreset preset) => preset switch
    {
        GraphicsPreset.Low => 0.35f,
        GraphicsPreset.Medium => 0.65f,
        _ => 1.0f,
    };

    /// <summary>
    /// Multiplier on how far scatter layers stay visible. Cutting range removes
    /// whole layers of overdraw rather than thinning them, so it is worth more
    /// than density alone for the same visual loss.
    /// </summary>
    public static float VegetationRange(GraphicsPreset preset) => preset switch
    {
        GraphicsPreset.Low => 0.55f,
        GraphicsPreset.Medium => 0.8f,
        _ => 1.0f,
    };

    /// <summary>Chunks of terrain streamed around the player.</summary>
    public static int TerrainRadius(GraphicsPreset preset) => preset switch
    {
        GraphicsPreset.Low => 4,
        GraphicsPreset.Medium => 6,
        _ => 8,
    };

    /// <summary>Chunks of vegetation streamed around the player.</summary>
    public static int VegetationRadius(GraphicsPreset preset) => preset switch
    {
        GraphicsPreset.Low => 3,
        GraphicsPreset.Medium => 5,
        _ => 6,
    };

    /// <summary>
    /// Shadow distance in metres. Shadow map resolution measured as noise, but
    /// distance decides how much geometry is re-submitted per cascade, which does
    /// cost.
    /// </summary>
    public static float ShadowDistance(GraphicsPreset preset) => preset switch
    {
        GraphicsPreset.Low => 260.0f,
        GraphicsPreset.Medium => 620.0f,
        _ => 1200.0f,
    };

    /// <summary>
    /// SSAO measured cheap here, but it is the first thing to drop if a machine
    /// is still short of its target, and players expect the option.
    /// </summary>
    public static bool Ssao(GraphicsPreset preset) => preset != GraphicsPreset.Low;

    /// <summary>Screen-space glow. Cheap, but another fullscreen pass.</summary>
    public static bool Glow(GraphicsPreset preset) => preset == GraphicsPreset.High;

    /// <summary>
    /// Applies the settings that live in ProjectSettings and so can only be read
    /// at startup, plus the viewport scale which can change at any time.
    /// </summary>
    public static void ApplyToViewport(Viewport viewport, GraphicsPreset preset)
    {
        viewport.Scaling3DScale = RenderScale(preset);

        // Bilinear rather than FSR: FSR's sharpening pass costs more than it is
        // worth at these scales, and on a GPU this weak the pass itself shows up
        // in the frame.
        viewport.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;

        // MSAA measured as costing nothing here, so it stays on above Low purely
        // for edge quality on the tree cards, which are the worst aliasing source
        // in the scene.
        viewport.Msaa3D = preset == GraphicsPreset.Low
            ? Viewport.Msaa.Disabled
            : Viewport.Msaa.Msaa2X;

        viewport.ScreenSpaceAA = preset == GraphicsPreset.Low
            ? Viewport.ScreenSpaceAAEnum.Disabled
            : Viewport.ScreenSpaceAAEnum.Fxaa;
    }
}
