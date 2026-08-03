// Ashwood County terrain.
//
// A four-layer PBR terrain blend (grass / forest floor / bare dirt / rock)
// selected by SLOPE, HEIGHT and noise. Written to run identically on the GL
// Compatibility and Forward+ renderers: no screen texture, no depth texture,
// no SSR, no derivatives-in-branches, no texture arrays.
//
// The three things this shader exists to fix:
//
//   1. STRETCHING. The rock layer is projected triplanar in WORLD space, so a
//      vertical cliff face samples the XY/ZY planes instead of smearing a
//      top-down projection down the wall. The three ground layers use a
//      top-down world XZ projection, which cannot stretch on near-horizontal
//      ground and costs a third of a triplanar fetch.
//
//   2. TILING. Every albedo is sampled at TWO frequencies (macro tile and a
//      detail tile at a deliberately non-integer ratio) and cross-faded by an
//      amount that itself varies with low-frequency noise, so neither the
//      macro nor the detail period is ever visible as a grid. A separate
//      large-scale macro-variation tint breaks up whatever survives at range.
//
//   3. PAINTED-CARDBOARD SURFACES. NORMAL and ROUGHNESS are driven from the
//      nor_gl and ARM maps for all four layers. Nothing here uses a constant
//      roughness.
//
// Layer boundaries are never straight: every threshold has world-space fBm
// noise added to it before the smoothstep, and the transitions themselves are
// height-blended through the ARM red (AO) channel so layers interlock at
// gravel scale instead of cross-fading like an airbrush.
//
// GEOMETRY REQUIREMENTS
//   - Needs correct vertex NORMALs (SurfaceTool.GenerateNormals()).
//   - Does NOT need UVs, tangents or vertex colours. All projection is world
//     space, so the material can be dropped onto any mesh as-is.
//   - Vertex colours are OPTIONAL and off by default. See use_vertex_color_mask.
//
// COST: 24 texture fetches per pixel, and 12 sampler slots. Ground layers are
// 4 fetches each (2 albedo frequencies + normal + ARM) = 12; rock is 12 (3
// triplanar axes x 2 albedo frequencies, + 3 normal + 3 ARM). There is no
// dynamic branching, so the cost is uniform across the frame and there are no
// derivative artifacts at mask boundaries. If this needs to get cheaper, drop
// detail_blend to 0 first: that removes 4 fetches and costs the least quality.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx;

// ---------------------------------------------------------------------------
// Layer 0 - GRASS. The base layer; everything else is composited over it.
// ---------------------------------------------------------------------------
uniform sampler2D grass_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D grass_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

// ---------------------------------------------------------------------------
// Layer 1 - FOREST FLOOR / leaf litter. Composited over grass in noise pockets.
// ---------------------------------------------------------------------------
uniform sampler2D forest_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D forest_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D forest_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

// ---------------------------------------------------------------------------
// Layer 2 - BARE DIRT / gravel. Moderate slopes plus scattered scour patches.
// ---------------------------------------------------------------------------
uniform sampler2D dirt_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D dirt_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D dirt_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

// ---------------------------------------------------------------------------
// Layer 3 - ROCK. Steep faces and high ground. Triplanar, world space.
// ---------------------------------------------------------------------------
uniform sampler2D rock_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D rock_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D rock_arm : hint_default_white, filter_linear_mipmap_anisotropic, repeat_enable;

// --- Tiling -----------------------------------------------------------------
// Size in METRES that one full texture tile covers. Smaller = more repeats.
uniform float grass_tile_meters : hint_range(0.25, 40.0) = 2.6;
uniform float forest_tile_meters : hint_range(0.25, 40.0) = 3.0;
uniform float dirt_tile_meters : hint_range(0.25, 40.0) = 2.2;
uniform float rock_tile_meters : hint_range(0.25, 40.0) = 4.5;

// Second, finer sample of every albedo. Keep this non-integer: an integer ratio
// makes the two frequencies phase-lock and reintroduces the grid we are hiding.
uniform float detail_tile_ratio : hint_range(1.5, 16.0) = 5.37;
uniform float detail_blend : hint_range(0.0, 1.0) = 0.45;
// The detail tile aliases into noise once it is smaller than a pixel, so fade
// it out with distance rather than paying for shimmer.
uniform float detail_fade_begin = 16.0;
uniform float detail_fade_end = 55.0;

// --- Boundary breakup -------------------------------------------------------
uniform float blend_noise_meters : hint_range(1.0, 200.0) = 11.0;
uniform float blend_noise_detail_meters : hint_range(0.25, 40.0) = 2.7;
// Lower = harder, more interlocked layer transitions. 0 disables height blending.
uniform float height_blend_contrast : hint_range(0.01, 1.0) = 0.28;

// --- Layer 1 (forest floor) placement --------------------------------------
uniform float forest_amount : hint_range(0.0, 1.0) = 0.5;
uniform float forest_noise_amount : hint_range(0.0, 1.5) = 0.55;
uniform float forest_edge_softness : hint_range(0.005, 0.5) = 0.12;
// Slope (0 flat .. 1 vertical) above which leaf litter stops holding.
uniform float forest_max_slope : hint_range(0.0, 1.0) = 0.34;
// 0 = ignore height entirely (default, works on any geometry). At 1.0 leaf
// litter is confined to the world-Y BAND between forest_height_low and
// forest_height_high, so it can be pinned either to a valley floor or to a
// gorge rim without inverting any edges.
uniform float forest_height_influence : hint_range(0.0, 1.0) = 0.0;
uniform float forest_height_low = -12.0;
uniform float forest_height_high = 6.0;
uniform float forest_height_feather : hint_range(0.01, 60.0) = 4.0;

// --- Layer 2 (dirt) placement ----------------------------------------------
uniform float dirt_slope_begin : hint_range(0.0, 1.0) = 0.10;
uniform float dirt_slope_end : hint_range(0.0, 1.0) = 0.26;
uniform float dirt_patch_amount : hint_range(0.0, 1.0) = 0.22;
uniform float dirt_noise_amount : hint_range(0.0, 1.5) = 0.5;
uniform float dirt_edge_softness : hint_range(0.005, 0.5) = 0.10;

// --- Layer 3 (rock) placement ----------------------------------------------
// Slope is normalised: 0.0 = flat, 0.5 = 45 degrees, 1.0 = vertical.
uniform float rock_slope_begin : hint_range(0.0, 1.0) = 0.28;
uniform float rock_slope_end : hint_range(0.0, 1.0) = 0.46;
uniform float rock_noise_amount : hint_range(0.0, 0.6) = 0.14;
// Height-driven rock is OFF by default so the material behaves the same on any
// mesh regardless of where it sits in world Y.
uniform float rock_height_amount : hint_range(0.0, 1.0) = 0.0;
uniform float rock_height_begin = 14.0;
uniform float rock_height_blend : hint_range(0.5, 100.0) = 10.0;
uniform float triplanar_sharpness : hint_range(1.0, 16.0) = 5.0;

// --- Surface response -------------------------------------------------------
uniform float ground_normal_strength : hint_range(0.0, 3.0) = 1.0;
uniform float rock_normal_strength : hint_range(0.0, 3.0) = 1.0;
// ARM green is remapped into this range, so roughness is never a flat constant.
uniform float roughness_min : hint_range(0.0, 1.0) = 0.55;
uniform float roughness_max : hint_range(0.0, 1.0) = 1.0;
uniform float ao_strength : hint_range(0.0, 1.0) = 0.85;
uniform float ao_light_affect : hint_range(0.0, 1.0) = 0.35;
uniform float specular_amount : hint_range(0.0, 1.0) = 0.3;
// Set true if lit relief reads inverted (a nor_dx map, or a mirrored import).
uniform bool flip_normal_green = false;

// --- Colour -----------------------------------------------------------------
uniform vec3 terrain_tint : source_color = vec3(1.0, 1.0, 1.0);
uniform float terrain_brightness : hint_range(0.0, 2.0) = 1.0;
// Large-scale patchy tint. This is what kills repetition in wide shots.
uniform float macro_variation_meters : hint_range(4.0, 400.0) = 46.0;
uniform float macro_variation_strength : hint_range(0.0, 1.0) = 0.35;
uniform vec3 macro_dark_tint : source_color = vec3(0.70, 0.72, 0.64);
uniform vec3 macro_light_tint : source_color = vec3(1.20, 1.16, 1.05);

// --- Optional artist control -----------------------------------------------
// OFF by default. Meshes with no COLOR array report COLOR = (1,1,1,1), which
// would force every layer to full weight, so this must stay opt-in.
// When enabled the mesh generator must write vertex colours where:
//     R = extra forest floor, G = extra dirt, B = extra rock, A = unused.
// Neutral (shader decides everything) is COLOR = (0, 0, 0, 1).
uniform bool use_vertex_color_mask = false;

varying vec3 v_world_pos;
varying vec3 v_world_normal;

// Dave Hoskins style hash: no trig, stable across GL and Vulkan backends.
float hash21(vec2 p) {
	vec3 p3 = fract(vec3(p.x, p.y, p.x) * 0.1031);
	p3 += dot(p3, vec3(p3.y, p3.z, p3.x) + 33.33);
	return fract((p3.x + p3.y) * p3.z);
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

float fbm3(vec2 p) {
	float v = value_noise(p) * 0.5;
	v += value_noise(p * 2.03) * 0.32;
	v += value_noise(p * 4.11) * 0.18;
	return v;
}

// Unpack a tangent-space normal, reconstructing Z from XY rather than trusting
// the blue channel. A hint_normal texture is imported with RGTC compression,
// which stores only red and green; reading .z directly would return garbage on
// exactly the maps this shader is built around. This mirrors what Godot's own
// NORMAL_MAP path does internally.
vec3 unpack_normal(vec3 packed_normal) {
	vec2 xy = packed_normal.xy * 2.0 - 1.0;
	return vec3(xy, sqrt(max(0.0, 1.0 - dot(xy, xy))));
}

// Height-aware transition. Instead of cross-fading two layers, pick whichever
// layer is "taller" at this texel, using the ARM red channel (AO) as a stand-in
// for surface height. Turns every boundary into an interlocking edge that reads
// as pebbles and litter sitting in the grass rather than an airbrushed gradient.
float height_blend(float height_a, float height_b, float t, float contrast) {
	float a = height_a + (1.0 - t);
	float b = height_b + t;
	float m = max(a, b) - max(contrast, 0.001);
	float wa = max(a - m, 0.0);
	float wb = max(b - m, 0.0);
	return clamp(wb / (wa + wb + 0.00001), 0.0, 1.0);
}

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	// MODEL_NORMAL_MATRIX keeps normals correct if the terrain mesh is scaled
	// non-uniformly (a plane stretched 100x1x100 is a very common authoring move).
	v_world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
	vec3 wp = v_world_pos;
	vec3 ng = normalize(v_world_normal);
	float gsign = flip_normal_green ? -1.0 : 1.0;

	// VERTEX is view space in the fragment stage, so its length is the distance
	// to the camera. Works on both renderers without a depth texture.
	float view_dist = length(VERTEX);

	// Normalised slope: 0 = flat, 0.5 = 45 degrees, 1 = vertical.
	float slope01 = acos(clamp(ng.y, -1.0, 1.0)) * 0.636619772;

	// Boundary breakup noise, two frequencies so masks are ragged at both the
	// landform scale and the footstep scale.
	float n_coarse = fbm3(wp.xz / max(blend_noise_meters, 0.01));
	float n_fine = value_noise(wp.xz / max(blend_noise_detail_meters, 0.01));
	float nb = n_coarse * 0.7 + n_fine * 0.3;
	float ns = (nb - 0.5) * 2.0;

	// Two-frequency albedo mix. The mix amount is itself modulated by the coarse
	// noise, so the detail tile does not appear at a constant strength either.
	float dfade = 1.0 - smoothstep(detail_fade_begin, detail_fade_end, view_dist);
	float dmix = clamp(detail_blend * dfade * mix(0.4, 1.0, n_coarse), 0.0, 1.0);

	// ---- Ground layers: top-down world XZ projection ------------------------
	// Sampling is written out per layer rather than factored into a helper that
	// takes sampler arguments, to keep the shader on the most conservative
	// subset of the language that both renderer backends accept.
	vec2 uv_grass = wp.xz / max(grass_tile_meters, 0.01);
	vec3 grass_a = mix(
		texture(grass_albedo, uv_grass).rgb,
		texture(grass_albedo, uv_grass * detail_tile_ratio).rgb, dmix);
	vec3 grass_n = unpack_normal(texture(grass_normal, uv_grass).xyz);
	vec3 grass_m = texture(grass_arm, uv_grass).rgb;

	vec2 uv_forest = wp.xz / max(forest_tile_meters, 0.01);
	vec3 forest_a = mix(
		texture(forest_albedo, uv_forest).rgb,
		texture(forest_albedo, uv_forest * detail_tile_ratio).rgb, dmix);
	vec3 forest_n = unpack_normal(texture(forest_normal, uv_forest).xyz);
	vec3 forest_m = texture(forest_arm, uv_forest).rgb;

	vec2 uv_dirt = wp.xz / max(dirt_tile_meters, 0.01);
	vec3 dirt_a = mix(
		texture(dirt_albedo, uv_dirt).rgb,
		texture(dirt_albedo, uv_dirt * detail_tile_ratio).rgb, dmix);
	vec3 dirt_n = unpack_normal(texture(dirt_normal, uv_dirt).xyz);
	vec3 dirt_m = texture(dirt_arm, uv_dirt).rgb;

	// ---- Rock: world-space triplanar ---------------------------------------
	vec3 tw = pow(abs(ng), vec3(triplanar_sharpness));
	tw /= max(tw.x + tw.y + tw.z, 0.0001);
	float rs = max(rock_tile_meters, 0.01);
	vec2 uv_rx = wp.zy / rs;
	vec2 uv_ry = wp.xz / rs;
	vec2 uv_rz = wp.xy / rs;

	vec3 rock_ax = mix(texture(rock_albedo, uv_rx).rgb,
		texture(rock_albedo, uv_rx * detail_tile_ratio).rgb, dmix);
	vec3 rock_ay = mix(texture(rock_albedo, uv_ry).rgb,
		texture(rock_albedo, uv_ry * detail_tile_ratio).rgb, dmix);
	vec3 rock_az = mix(texture(rock_albedo, uv_rz).rgb,
		texture(rock_albedo, uv_rz * detail_tile_ratio).rgb, dmix);
	vec3 rock_a = rock_ax * tw.x + rock_ay * tw.y + rock_az * tw.z;

	vec3 rock_m = texture(rock_arm, uv_rx).rgb * tw.x
		+ texture(rock_arm, uv_ry).rgb * tw.y
		+ texture(rock_arm, uv_rz).rgb * tw.z;

	// Whiteout triplanar normal blend, producing a world-space normal directly.
	vec3 rnx = unpack_normal(texture(rock_normal, uv_rx).xyz);
	vec3 rny = unpack_normal(texture(rock_normal, uv_ry).xyz);
	vec3 rnz = unpack_normal(texture(rock_normal, uv_rz).xyz);
	rnx.y *= gsign;
	rny.y *= gsign;
	rnz.y *= gsign;
	rnx.xy *= rock_normal_strength;
	rny.xy *= rock_normal_strength;
	rnz.xy *= rock_normal_strength;
	rnx = vec3(rnx.xy + ng.zy, abs(rnx.z) * ng.x);
	rny = vec3(rny.xy + ng.xz, abs(rny.z) * ng.y);
	rnz = vec3(rnz.xy + ng.xy, abs(rnz.z) * ng.z);
	vec3 rock_wn = normalize(
		vec3(rnx.z, rnx.y, rnx.x) * tw.x
		+ vec3(rny.x, rny.z, rny.y) * tw.y
		+ rnz * tw.z);

	// ---- Layer masks -------------------------------------------------------
	// Band, not a one-sided ramp: 1 inside [low, high], feathered at both ends.
	float fhf = max(forest_height_feather, 0.01);
	float forest_h =
		smoothstep(forest_height_low, forest_height_low + fhf, wp.y)
		* (1.0 - smoothstep(forest_height_high - fhf, forest_height_high, wp.y));
	float forest_drive = forest_amount * mix(1.0, forest_h, forest_height_influence);
	float m_forest = smoothstep(
		0.5 - forest_edge_softness, 0.5 + forest_edge_softness,
		forest_drive + ns * forest_noise_amount);
	m_forest *= 1.0 - smoothstep(forest_max_slope, forest_max_slope + 0.18, slope01);

	float m_dirt_slope = smoothstep(
		dirt_slope_begin, dirt_slope_end, slope01 + ns * dirt_noise_amount * 0.15);
	float m_dirt_patch = smoothstep(
		0.5 - dirt_edge_softness, 0.5 + dirt_edge_softness,
		dirt_patch_amount + ns * dirt_noise_amount);
	float m_dirt = clamp(max(m_dirt_slope, m_dirt_patch), 0.0, 1.0);

	float m_rock_slope = smoothstep(
		rock_slope_begin, rock_slope_end, slope01 + ns * rock_noise_amount);
	float m_rock_height = smoothstep(
		rock_height_begin, rock_height_begin + rock_height_blend,
		wp.y + ns * 6.0) * rock_height_amount;
	float m_rock = clamp(max(m_rock_slope, m_rock_height), 0.0, 1.0);

	vec3 vmask = COLOR.rgb * (use_vertex_color_mask ? 1.0 : 0.0);
	m_forest = clamp(m_forest + vmask.r, 0.0, 1.0);
	m_dirt = clamp(m_dirt + vmask.g, 0.0, 1.0);
	m_rock = clamp(m_rock + vmask.b, 0.0, 1.0);

	// ---- Composite ---------------------------------------------------------
	vec3 albedo = grass_a;
	vec3 tnormal = grass_n;
	vec3 arm = grass_m;

	float t = height_blend(arm.r, forest_m.r, m_forest, height_blend_contrast);
	albedo = mix(albedo, forest_a, t);
	tnormal = mix(tnormal, forest_n, t);
	arm = mix(arm, forest_m, t);

	t = height_blend(arm.r, dirt_m.r, m_dirt, height_blend_contrast);
	albedo = mix(albedo, dirt_a, t);
	tnormal = mix(tnormal, dirt_n, t);
	arm = mix(arm, dirt_m, t);

	// Ground tangent-space normal into world space. The XZ projection frame is
	// tangent = +X, bitangent = +Z, up = +Y, and the result is whiteout-blended
	// onto the geometric normal so sloped ground still bends the detail relief.
	tnormal.y *= gsign;
	tnormal.xy *= ground_normal_strength;
	vec3 dn = vec3(tnormal.x, tnormal.z, tnormal.y);
	vec3 ground_wn = normalize(vec3(
		ng.x + dn.x,
		ng.y * max(dn.y, 0.02),
		ng.z + dn.z));

	float t_rock = height_blend(arm.r, rock_m.r, m_rock, height_blend_contrast);
	albedo = mix(albedo, rock_a, t_rock);
	arm = mix(arm, rock_m, t_rock);
	vec3 world_n = normalize(mix(ground_wn, rock_wn, t_rock));

	// ---- Macro variation ---------------------------------------------------
	float macro = fbm3(wp.xz / max(macro_variation_meters, 0.01));
	vec3 macro_mul = mix(
		vec3(1.0), mix(macro_dark_tint, macro_light_tint, macro),
		macro_variation_strength);

	ALBEDO = albedo * macro_mul * terrain_tint * terrain_brightness;
	NORMAL = normalize((VIEW_MATRIX * vec4(world_n, 0.0)).xyz);
	ROUGHNESS = clamp(mix(roughness_min, roughness_max, arm.g), 0.0, 1.0);
	METALLIC = 0.0;
	SPECULAR = specular_amount;
	AO = mix(1.0, arm.r, ao_strength);
	AO_LIGHT_AFFECT = ao_light_affect;
}
