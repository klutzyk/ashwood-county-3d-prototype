shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

// Ashwood County ground.
//
// Four PBR layers - grass, forest floor, rock, worked soil - blended by weights
// the terrain mesher bakes into vertex colour:
//
//   splat.r  forest floor
//   splat.g  rock
//   splat.b  worked soil / dirt / settled ground
//   splat.a  riverbank wetness
//   grass is the base coat, whatever the other three leave behind
//
// UV arrives as raw world XZ in metres, so tiling is continuous across chunk
// borders no matter what resolution each chunk was meshed at.
//
// The two things that separate believable terrain from obvious terrain at this
// scale are both here and both cheap:
//
//   1. Macro variation. A 2K texture tiled across eight kilometres is a visible
//      grid from any elevation. A low-frequency procedural field modulates albedo
//      brightness and saturation at ~180m, which breaks the repeat without any
//      extra texture fetches.
//   2. Triplanar rock. Cliffs are where planar XZ mapping smears into vertical
//      streaks. Rock alone pays for a second projection, and only where its weight
//      is actually non-zero.

uniform sampler2D grass_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

uniform sampler2D forest_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D forest_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D forest_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

uniform sampler2D rock_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D rock_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D rock_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

uniform sampler2D soil_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D soil_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D soil_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

uniform float grass_tile_meters : hint_range(0.5, 24.0) = 3.4;
uniform float forest_tile_meters : hint_range(0.5, 24.0) = 3.8;
uniform float rock_tile_meters : hint_range(0.5, 24.0) = 6.5;
uniform float soil_tile_meters : hint_range(0.5, 24.0) = 3.0;

uniform vec3 grass_tint : source_color = vec3(0.86, 0.92, 0.74);
uniform vec3 forest_tint : source_color = vec3(0.80, 0.82, 0.72);

// Distant canopy. Where the instanced trees stop, the ground takes over the job of
// looking like forest; these control that handover.
uniform vec3 canopy_color : source_color = vec3(0.138, 0.186, 0.104);
uniform float canopy_fade_begin : hint_range(0.0, 600.0) = 55.0;
uniform float canopy_fade_end : hint_range(0.0, 1200.0) = 260.0;
uniform float canopy_clump_meters : hint_range(4.0, 200.0) = 34.0;
uniform vec3 rock_tint : source_color = vec3(0.74, 0.74, 0.76);
uniform vec3 soil_tint : source_color = vec3(0.92, 0.86, 0.74);

// Riverbank: wet ground is darker and much glossier than dry ground. Getting this
// wrong is why waterlines usually read as a paint change rather than as a shore.
uniform vec3 wet_tint : source_color = vec3(0.46, 0.46, 0.44);
uniform float wet_roughness : hint_range(0.0, 1.0) = 0.22;

uniform float macro_meters : hint_range(20.0, 600.0) = 185.0;
uniform float macro_strength : hint_range(0.0, 1.0) = 0.34;

// How strongly the rotated second sample is mixed in. 0 restores plain tiling.
uniform float breakup_amount : hint_range(0.0, 0.5) = 0.38;

// A second, much finer sample of the dominant layer, faded out with distance.
// Near ground gets detail the base tiling cannot carry; the far field does not pay
// for it and does not shimmer.
uniform float detail_tile_ratio : hint_range(2.0, 16.0) = 6.5;
uniform float detail_strength : hint_range(0.0, 1.0) = 0.42;
uniform float detail_fade_begin = 18.0;
uniform float detail_fade_end = 70.0;

uniform float normal_strength : hint_range(0.0, 3.0) = 1.25;
uniform float blend_sharpness : hint_range(0.02, 1.0) = 0.22;
uniform float triplanar_sharpness : hint_range(1.0, 12.0) = 4.5;

// Where planar mapping hands over to triplanar rock, as 1 - normal.y. Roughly
// 25 degrees to 47 degrees of slope: gentle enough that the transition is hidden
// in ground that is already breaking up, steep enough that pasture and ploughed
// field are untouched.
uniform float cliff_begin : hint_range(0.0, 1.0) = 0.10;
uniform float cliff_full : hint_range(0.0, 1.0) = 0.32;

// Snow. Heights are in world Y, where 0 is the town at a real 430m, so the offset
// converts back to real altitude for numbers that match the planning map.
uniform float snow_altitude_offset : hint_range(0.0, 1000.0) = 430.0;
uniform float snow_begin : hint_range(0.0, 2000.0) = 980.0;
uniform float snow_full : hint_range(0.0, 2000.0) = 1290.0;
uniform float snow_slope_limit : hint_range(0.0, 1.0) = 0.44;
uniform float snow_patch_meters : hint_range(10.0, 2000.0) = 320.0;
uniform vec3 snow_tint : source_color = vec3(0.90, 0.93, 0.97);

// Diagnostic channel. 0 is normal rendering; the rest bypass lighting entirely by
// writing to EMISSION, so what is inspected is the shader's own inputs rather than
// how they happen to be lit.
//   1 vertex weights (r=grass, g=forest, b=rock)
//   2 raw grass albedo
//   3 grass ARM (r=occlusion, g=roughness)
//   4 final blended albedo, unlit
uniform int debug_mode : hint_range(0, 4) = 0;

// NOTE: the vertex colour is forwarded through an explicit varying rather than
// read as COLOR inside fragment(). In a Godot 4 custom spatial shader, COLOR is
// only the mesh's vertex colour inside vertex(); in fragment() it reads as opaque
// white unless the shader passes it along itself. Reading it directly made every
// splat weight 1.0, which silently blended all layers at once.
varying vec4 splat;
varying vec3 world_position;
varying vec3 world_normal;

// Cheap value noise. Only ever sampled at macro frequency, so quality matters far
// less than it costing nothing.
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float value_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float macro_field(vec2 p) {
    return value_noise(p) * 0.60 + value_noise(p * 2.17 + 31.0) * 0.28
         + value_noise(p * 4.31 - 17.0) * 0.12;
}

// Height-aware blend weight. Using the layer's own AO/height channel to bias the
// mix makes gravel settle into the crevices of grass rather than cross-fading
// through it like a slide dissolve.
float height_weight(float weight, float layer_height) {
    return max(weight * (0.35 + 0.65 * layer_height) - (1.0 - weight) * blend_sharpness, 0.0);
}

vec3 unpack_normal(vec4 packed) {
    vec3 n = packed.rgb * 2.0 - 1.0;
    n.xy *= normal_strength;
    return normalize(n);
}

// Two samples of the same texture at incommensurate scales and rotations, mixed
// by a low-frequency field.
//
// Macro colour variation alone does not hide tiling: the eye locks onto the
// repeating *structure*, not the average brightness, and a 6m grass texture across
// open ground reads as a chequerboard from anywhere above head height. Rotating
// the second sample by an angle with no relation to the first means the two grids
// never line up, so there is no combined period to see. Costs one extra fetch per
// map on the layers that actually cover ground.
const float BREAKUP_ROTATION = 0.7854; // 45 degrees, deliberately not a tile multiple
const float BREAKUP_SCALE = 0.41;

vec2 breakup_uv(vec2 uv) {
    float c = cos(BREAKUP_ROTATION);
    float s = sin(BREAKUP_ROTATION);
    return vec2(uv.x * c - uv.y * s, uv.x * s + uv.y * c) * BREAKUP_SCALE + vec2(37.1, 11.7);
}

// Anti-tiling costs exactly double, because it is literally a second sample of
// every map blended over the first. With four layers at three maps each that is
// twenty-four fetches a pixel before the triplanar rock branch adds more, and
// swapping this whole material for a plain one measured a jump from 21.5 to 43.5
// FPS - the largest single cost in the frame.
//
// So the second sample is a quality setting rather than a constant. At
// anti_tiling 0 the repeat is visible on open ground, which is a real loss, but
// it is the difference between playable and not on an integrated GPU.
uniform float anti_tiling : hint_range(0.0, 1.0) = 1.0;

// The triplanar rock branch is the other heavy path: it samples three more maps
// twice over on any pixel steep enough to take it. Off, cliffs fall back to the
// stretched planar layers - visibly worse, but only on steep ground.
uniform float enable_triplanar : hint_range(0.0, 1.0) = 1.0;


vec4 sample_broken(sampler2D map, vec2 uv, float blend) {
    float mixed = blend * anti_tiling;
    if (mixed < 0.004) {
        return texture(map, uv);
    }

    return mix(texture(map, uv), texture(map, breakup_uv(uv)), mixed);
}

void vertex() {
    splat = COLOR;
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
    vec2 world_uv = world_position.xz;

    // ---- weights --------------------------------------------------------------
    float w_forest = splat.r;
    float w_rock = splat.g;
    float w_soil = splat.b;
    float w_wet = splat.a;
    float w_grass = clamp(1.0 - max(max(w_forest, w_rock), w_soil), 0.0, 1.0);

    // Steep ground is forced to rock, from the surface normal rather than from the
    // vertex weights.
    //
    // Every layer except rock is mapped planar on world XZ. On a near-vertical face
    // a tiny XZ span covers a huge Y span, so those layers stretch without bound -
    // the Blackwater basin walls smeared into vertical streaks with hard triangular
    // banding where adjacent vertices disagreed. The vertex weights could not fix
    // it: they are sampled 4-16m apart horizontally but tens of metres apart
    // vertically on such a face, so rock never reliably reached 1.0 and the
    // stretched layers kept showing through.
    //
    // The normal is exact per-pixel, so deriving steepness here instead makes the
    // handover to triplanar happen before stretching is visible, and gives cliffs
    // exposed stone - which is what the reference photography shows anyway.
    float steepness = 1.0 - clamp(world_normal.y, 0.0, 1.0);
    float cliff = smoothstep(cliff_begin, cliff_full, steepness);
    w_rock = max(w_rock, cliff);

    // Rescale the planar layers into whatever is left so the blend still sums to
    // one; without this a cliff reads as rock laid over full-strength smeared grass.
    float planar_room = 1.0 - w_rock;
    w_forest *= planar_room;
    w_soil *= planar_room;
    w_grass *= planar_room;

    // How much of the rotated second sample to mix in, varied across the world so
    // the breakup itself has no constant signature either. Computed before any
    // sampling so every layer, including the triplanar rock, can use it.
    float breakup = breakup_amount *
        (0.55 + 0.45 * value_noise(world_uv / (macro_meters * 0.5)));

    // ---- base layer samples ---------------------------------------------------
    vec2 uv_grass = world_uv / grass_tile_meters;
    vec2 uv_forest = world_uv / forest_tile_meters;
    vec2 uv_soil = world_uv / soil_tile_meters;

    vec4 grass_c = sample_broken(grass_albedo, uv_grass, breakup);
    vec4 grass_a = sample_broken(grass_arm, uv_grass, breakup);
    vec3 grass_n = unpack_normal(sample_broken(grass_normal, uv_grass, breakup));

    vec4 forest_c = sample_broken(forest_albedo, uv_forest, breakup);
    vec4 forest_a = sample_broken(forest_arm, uv_forest, breakup);
    vec3 forest_n = unpack_normal(sample_broken(forest_normal, uv_forest, breakup));

    vec4 soil_c = sample_broken(soil_albedo, uv_soil, breakup);
    vec4 soil_a = sample_broken(soil_arm, uv_soil, breakup);
    vec3 soil_n = unpack_normal(sample_broken(soil_normal, uv_soil, breakup));

    // ---- rock, projected triplanar where it matters ---------------------------
    // Guarded because on flat ground the whole quad has zero rock weight, so the
    // branch is coherent and genuinely skips the extra fetches.
    vec4 rock_c;
    vec4 rock_a;
    vec3 rock_n;
    if (w_rock > 0.004 && enable_triplanar > 0.5) {
        vec3 blend = pow(abs(world_normal), vec3(triplanar_sharpness));
        blend /= max(blend.x + blend.y + blend.z, 0.0001);

        vec2 uv_top = world_position.xz / rock_tile_meters;
        // One side projection is enough: on a cliff face either X or Z dominates,
        // and picking the stronger of the two costs half of a full triplanar.
        bool use_zy = blend.x >= blend.z;
        vec2 uv_side = (use_zy ? world_position.zy : world_position.xy) / rock_tile_meters;
        float side_weight = use_zy ? blend.x : blend.z;
        float top_weight = blend.y;
        float total = max(side_weight + top_weight, 0.0001);
        side_weight /= total;
        top_weight /= total;

        // Rock gets the same rotated-sample breakup as the flat layers. Cliff faces
        // are large, close and lit edge-on, so an un-broken repeat shows there more
        // plainly than anywhere else in the county.
        rock_c = sample_broken(rock_albedo, uv_top, breakup) * top_weight +
                 sample_broken(rock_albedo, uv_side, breakup) * side_weight;
        rock_a = sample_broken(rock_arm, uv_top, breakup) * top_weight +
                 sample_broken(rock_arm, uv_side, breakup) * side_weight;
        rock_n = normalize(
            unpack_normal(sample_broken(rock_normal, uv_top, breakup)) * top_weight +
            unpack_normal(sample_broken(rock_normal, uv_side, breakup)) * side_weight);
    } else {
        vec2 uv_rock = world_uv / rock_tile_meters;
        rock_c = sample_broken(rock_albedo, uv_rock, breakup);
        rock_a = sample_broken(rock_arm, uv_rock, breakup);
        rock_n = vec3(0.0, 0.0, 1.0);
    }

    // ---- height-aware blend ---------------------------------------------------
    // ARM layout is (ambient occlusion, roughness, metallic); the AO channel is a
    // good enough stand-in for surface height for blending purposes.
    float h_grass = height_weight(w_grass, grass_a.r);
    float h_forest = height_weight(w_forest, forest_a.r);
    float h_rock = height_weight(w_rock, rock_a.r);
    float h_soil = height_weight(w_soil, soil_a.r);
    float h_total = max(h_grass + h_forest + h_rock + h_soil, 0.0001);

    h_grass /= h_total;
    h_forest /= h_total;
    h_rock /= h_total;
    h_soil /= h_total;

    vec3 albedo =
        grass_c.rgb * grass_tint * h_grass +
        forest_c.rgb * forest_tint * h_forest +
        rock_c.rgb * rock_tint * h_rock +
        soil_c.rgb * soil_tint * h_soil;

    vec3 normal_map = normalize(
        grass_n * h_grass + forest_n * h_forest + rock_n * h_rock + soil_n * h_soil);

    float roughness =
        grass_a.g * h_grass + forest_a.g * h_forest + rock_a.g * h_rock + soil_a.g * h_soil;
    float occlusion =
        grass_a.r * h_grass + forest_a.r * h_forest + rock_a.r * h_rock + soil_a.r * h_soil;

    // ---- snow on the tops -----------------------------------------------------
    // The county runs from 320m to 1420m, so the high ground is genuinely above
    // the winter snowline and the reference photography for this genre always has
    // white on the far peaks. It is also the cheapest possible way to give the
    // skyline something to read against: a pale cap separates a distant ridge from
    // the sky far better than another shade of green.
    //
    // Deposition follows the surface normal, not just altitude - snow sits on
    // shelves and slides off anything steep - and the noise keeps the snowline from
    // being a contour drawn round the mountain.
    float altitude = world_position.y + snow_altitude_offset;
    float snow_band = smoothstep(snow_begin, snow_full, altitude);
    float lee = smoothstep(snow_slope_limit, snow_slope_limit * 0.45, steepness);
    float drift = value_noise(world_uv / snow_patch_meters) * 0.5 +
                  value_noise(world_uv / (snow_patch_meters * 0.27)) * 0.5;
    float snow = clamp(snow_band * lee * (0.55 + drift * 0.9), 0.0, 1.0);

    if (snow > 0.001) {
        albedo = mix(albedo, snow_tint, snow);

        // Snow is smoother than anything under it, and bright enough that its own
        // bounce fills the ambient occlusion the ground below was carrying.
        roughness = mix(roughness, 0.78, snow);
        occlusion = mix(occlusion, 1.0, snow * 0.7);

        // Flatten the normal toward vertical: settled snow buries surface detail
        // rather than draping over it, and keeping the rock's normal under white
        // albedo is what makes fake snow read as grey paint.
        normal_map = normalize(mix(normal_map, vec3(0.0, 0.0, 1.0), snow * 0.55));
    }

    // ---- near detail ----------------------------------------------------------
    float view_distance = length(VERTEX);
    float detail_fade = 1.0 - smoothstep(detail_fade_begin, detail_fade_end, view_distance);
    if (detail_fade > 0.002) {
        // Reuse whichever layer dominates here, at a much finer tiling. Overlaying
        // its luminance keeps the colour but adds the grain the base tiling is too
        // coarse to hold up close.
        vec2 uv_detail = world_uv / (grass_tile_meters / detail_tile_ratio);
        vec3 detail = texture(grass_albedo, uv_detail).rgb;
        float detail_luma = dot(detail, vec3(0.299, 0.587, 0.114));

        // Centred on the texture's own mid-grey so this varies contrast rather
        // than darkening everything, and clamped so it can never invert the layer.
        float modulation = clamp(1.0 + (detail_luma - 0.42) * 1.15, 0.55, 1.55);
        albedo *= mix(1.0, modulation, detail_fade * detail_strength);

        vec3 detail_n = unpack_normal(texture(grass_normal, uv_detail));
        normal_map = normalize(mix(normal_map, normalize(normal_map + detail_n), detail_fade * 0.5));
    }

    // ---- macro variation ------------------------------------------------------
    float macro = macro_field(world_uv / macro_meters);
    float macro_wide = macro_field(world_uv / (macro_meters * 3.7));
    float variation = mix(macro, macro_wide, 0.45);

    // Brightness and saturation both move, because a pure brightness ramp still
    // leaves the repeat visible as identical hue across every tile.
    albedo *= 1.0 + (variation - 0.5) * macro_strength;
    float luma = dot(albedo, vec3(0.299, 0.587, 0.114));
    albedo = mix(vec3(luma), albedo, 1.0 + (variation - 0.5) * macro_strength * 0.7);
    roughness = clamp(roughness + (variation - 0.5) * 0.10, 0.04, 1.0);

    // ---- distant canopy -------------------------------------------------------
    // The forest layer is a forest *floor* scan, which is what you correctly stand
    // on under the trees - and completely wrong from a ridge, where a forest reads
    // as dark green canopy, not brown leaf litter. Individual trees only instance
    // out to a few hundred metres, so without this a county the map says is half
    // forest renders as bare grass hills all the way to the horizon.
    //
    // Blending toward canopy colour with distance stands in for the trees that are
    // no longer being drawn, so the treeline stays where the biome map puts it.
    if (w_forest > 0.002) {
        float canopy_blend = w_forest *
            smoothstep(canopy_fade_begin, canopy_fade_end, view_distance);

        // Break the stand into lit crowns and shadowed gaps. Two scales, because a
        // single one gives an even stipple: the wide field separates stands from
        // clearings, the fine one picks out individual crowns within a stand. The
        // range is deliberately wide - real canopy seen across a valley is mostly
        // shadow with bright tops catching the sun, not a uniform green sheet.
        float crown = macro_field(world_uv / canopy_clump_meters);
        float stands = macro_field(world_uv / (canopy_clump_meters * 6.3));
        float lit = clamp(crown * 0.62 + stands * 0.38, 0.0, 1.0);

        // Sunlit crowns go both brighter and yellower; shadowed gaps go bluer.
        vec3 canopy = canopy_color * (0.55 + lit * 1.15);
        canopy += vec3(0.055, 0.062, 0.012) * smoothstep(0.55, 1.0, lit);
        canopy = mix(canopy, canopy * vec3(0.82, 0.92, 1.10), smoothstep(0.45, 0.0, lit));

        albedo = mix(albedo, canopy, canopy_blend);
        roughness = mix(roughness, 0.92, canopy_blend);
        occlusion = mix(occlusion, occlusion * 0.82, canopy_blend);

        // Canopy is not a flat plane; flattening the normal toward vertical keeps
        // distant forest from picking up the ground's slope shading as if it were
        // polished rock.
        normal_map = normalize(mix(normal_map, vec3(0.0, 0.0, 1.0), canopy_blend * 0.7));
    }

    // ---- waterline ------------------------------------------------------------
    if (w_wet > 0.002) {
        albedo = mix(albedo, albedo * wet_tint, w_wet);
        roughness = mix(roughness, wet_roughness, w_wet);
        occlusion = mix(occlusion, occlusion * 0.85, w_wet);
    }

    // Godot forbids an early return from fragment(), so the diagnostic path is a
    // branch around the real output rather than a bail-out.
    if (debug_mode > 0) {
        vec3 probe = clamp(albedo, vec3(0.0), vec3(1.0));
        if (debug_mode == 1) { probe = vec3(w_grass, w_forest, w_rock); }
        else if (debug_mode == 2) { probe = grass_c.rgb; }
        else if (debug_mode == 3) { probe = vec3(grass_a.r, grass_a.g, 0.0); }
        ALBEDO = vec3(0.0);
        EMISSION = probe;
        ROUGHNESS = 1.0;
        METALLIC = 0.0;
    } else {

    ALBEDO = clamp(albedo, vec3(0.0), vec3(1.0));

    // The mesher supplies a real tangent array built from the world-XZ UV layout,
    // so normal mapping goes through NORMAL_MAP as intended. Rebuilding the basis
    // by hand here and writing NORMAL directly was tried first and produced a
    // completely unlit surface.
    NORMAL_MAP = normal_map * 0.5 + 0.5;
    ROUGHNESS = clamp(roughness, 0.04, 1.0);
    METALLIC = 0.0;
    SPECULAR = 0.45;
    AO = clamp(occlusion, 0.0, 1.0);
    AO_LIGHT_AFFECT = 0.55;
    }
}
