shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

// Ashwood County ground.
//
// Four PBR layers - grass, forest floor, rock, worked soil - blended by weights
// the terrain mesher bakes into vertex colour:
//
//   COLOR.r  forest floor
//   COLOR.g  rock
//   COLOR.b  worked soil / dirt / settled ground
//   COLOR.a  riverbank wetness
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
uniform vec3 rock_tint : source_color = vec3(0.74, 0.74, 0.76);
uniform vec3 soil_tint : source_color = vec3(0.92, 0.86, 0.74);

// Riverbank: wet ground is darker and much glossier than dry ground. Getting this
// wrong is why waterlines usually read as a paint change rather than as a shore.
uniform vec3 wet_tint : source_color = vec3(0.46, 0.46, 0.44);
uniform float wet_roughness : hint_range(0.0, 1.0) = 0.22;

uniform float macro_meters : hint_range(20.0, 600.0) = 185.0;
uniform float macro_strength : hint_range(0.0, 1.0) = 0.34;

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

void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
    vec2 world_uv = world_position.xz;

    // ---- weights --------------------------------------------------------------
    float w_forest = COLOR.r;
    float w_rock = COLOR.g;
    float w_soil = COLOR.b;
    float w_wet = COLOR.a;
    float w_grass = clamp(1.0 - max(max(w_forest, w_rock), w_soil), 0.0, 1.0);

    // ---- base layer samples ---------------------------------------------------
    vec2 uv_grass = world_uv / grass_tile_meters;
    vec2 uv_forest = world_uv / forest_tile_meters;
    vec2 uv_soil = world_uv / soil_tile_meters;

    vec4 grass_c = texture(grass_albedo, uv_grass);
    vec4 grass_a = texture(grass_arm, uv_grass);
    vec3 grass_n = unpack_normal(texture(grass_normal, uv_grass));

    vec4 forest_c = texture(forest_albedo, uv_forest);
    vec4 forest_a = texture(forest_arm, uv_forest);
    vec3 forest_n = unpack_normal(texture(forest_normal, uv_forest));

    vec4 soil_c = texture(soil_albedo, uv_soil);
    vec4 soil_a = texture(soil_arm, uv_soil);
    vec3 soil_n = unpack_normal(texture(soil_normal, uv_soil));

    // ---- rock, projected triplanar where it matters ---------------------------
    // Guarded because on flat ground the whole quad has zero rock weight, so the
    // branch is coherent and genuinely skips the extra fetches.
    vec4 rock_c;
    vec4 rock_a;
    vec3 rock_n;
    if (w_rock > 0.004) {
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

        rock_c = texture(rock_albedo, uv_top) * top_weight + texture(rock_albedo, uv_side) * side_weight;
        rock_a = texture(rock_arm, uv_top) * top_weight + texture(rock_arm, uv_side) * side_weight;
        rock_n = normalize(
            unpack_normal(texture(rock_normal, uv_top)) * top_weight +
            unpack_normal(texture(rock_normal, uv_side)) * side_weight);
    } else {
        rock_c = vec4(0.0);
        rock_a = vec4(1.0);
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

    // ---- waterline ------------------------------------------------------------
    if (w_wet > 0.002) {
        albedo = mix(albedo, albedo * wet_tint, w_wet);
        roughness = mix(roughness, wet_roughness, w_wet);
        occlusion = mix(occlusion, occlusion * 0.85, w_wet);
    }

    ALBEDO = clamp(albedo, vec3(0.0), vec3(1.0));

    // The terrain mesh carries no tangent array - a heightfield's tangent basis is
    // implied by its world-XZ UVs, so storing four more floats per vertex across
    // an eight-kilometre county would be pure waste. NORMAL_MAP needs a tangent
    // basis it does not have here and produces garbage lighting without one, so
    // the basis is rebuilt from the world normal and NORMAL is written directly.
    vec3 t = normalize(vec3(1.0, 0.0, 0.0) - world_normal * world_normal.x);
    vec3 b = normalize(cross(world_normal, t));
    vec3 shaded = normalize(t * normal_map.x + b * normal_map.y + world_normal * normal_map.z);
    NORMAL = normalize((VIEW_MATRIX * vec4(shaded, 0.0)).xyz);
    ROUGHNESS = clamp(roughness, 0.04, 1.0);
    METALLIC = 0.0;
    SPECULAR = 0.45;
    AO = clamp(occlusion, 0.0, 1.0);
    AO_LIGHT_AFFECT = 0.55;
}
