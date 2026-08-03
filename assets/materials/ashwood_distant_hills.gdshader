// Ashwood County distant hills / vista landform.
//
// This covers a very large amount of screen but is never seen closer than a few
// hundred metres, so it carries NO textures at all: everything is procedural
// value noise. It is cheaper than a StandardMaterial3D with an albedo map and it
// cannot stretch, tile or pop.
//
// The failure it replaces is untextured flat green, which reads as a painted
// backdrop because it has no internal value variation and no aerial
// perspective. What produces the "forested ridge at 400m" impression is, in
// order of importance:
//
//   1. Value variation across the canopy at roughly tree-clump scale, so the
//      slope is broken into lighter and darker masses instead of one flat fill.
//   2. Aerial perspective. Distant hills desaturate and shift toward the sky.
//      This is applied here in addition to the Environment fog so the vista
//      still separates from the near ground even when fog is tuned for gameplay
//      readability rather than for depth.
//   3. Exposed ground on steep faces, so ridges read as landform and not as an
//      extruded silhouette.
//
// Works identically on GL Compatibility and Forward+: no screen or depth texture.
// Shadow receiving is disabled because at this range the directional shadow
// atlas has no useful resolution left and only produces acne.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, shadows_disabled;

// --- Palette ----------------------------------------------------------------
// Base canopy mass, and the lighter mass that sits on top of it.
uniform vec3 canopy_color : source_color = vec3(0.129, 0.169, 0.110);
uniform vec3 canopy_highlight : source_color = vec3(0.239, 0.286, 0.161);
// Exposed earth / dead scrub on faces too steep to hold trees.
uniform vec3 slope_color : source_color = vec3(0.216, 0.196, 0.153);
// Sun-caught ridge tops. Slightly warm; this is what gives the skyline an edge.
uniform vec3 ridge_color : source_color = vec3(0.310, 0.333, 0.243);
// Cool shadow pooled in the valley floors.
uniform vec3 valley_color : source_color = vec3(0.086, 0.114, 0.106);

// --- Aerial perspective -----------------------------------------------------
uniform vec3 haze_color : source_color = vec3(0.478, 0.522, 0.569);
uniform float haze_begin = 260.0;
uniform float haze_end = 1500.0;
uniform float haze_strength : hint_range(0.0, 1.0) = 0.55;

// --- Landform bands ---------------------------------------------------------
// World Y of the valley floor and of the ridge line. Drives the valley/ridge
// colour gradient; set these to the actual vertical extent of the vista mesh.
uniform float valley_y = 0.0;
uniform float ridge_y = 120.0;

// --- Canopy break-up --------------------------------------------------------
// Metres per noise cell. canopy_meters is the tree-mass scale; the detail
// octave stops the masses reading as identical blobs.
uniform float canopy_meters : hint_range(2.0, 400.0) = 38.0;
uniform float canopy_detail_meters : hint_range(1.0, 100.0) = 9.0;
uniform float canopy_variation : hint_range(0.0, 1.0) = 0.55;

// --- Slope response ---------------------------------------------------------
// Normalised slope: 0.0 = flat, 0.5 = 45 degrees, 1.0 = vertical.
uniform float slope_begin : hint_range(0.0, 1.0) = 0.30;
uniform float slope_end : hint_range(0.0, 1.0) = 0.62;

// --- Surface ----------------------------------------------------------------
uniform float hills_roughness : hint_range(0.0, 1.0) = 0.96;
uniform float hills_specular : hint_range(0.0, 1.0) = 0.04;
// Tilts shading normals toward world up. Low-poly vista meshes otherwise show
// hard facet edges that read as origami rather than as distant landform.
uniform float normal_softening : hint_range(0.0, 1.0) = 0.45;
// Very low-frequency normal perturbation from the canopy noise. Keep small:
// high-frequency normals at this distance alias into sparkle.
uniform float canopy_relief : hint_range(0.0, 1.0) = 0.22;

uniform vec3 hills_tint : source_color = vec3(1.0, 1.0, 1.0);
uniform float hills_brightness : hint_range(0.0, 2.0) = 1.0;

varying vec3 v_world_pos;
varying vec3 v_world_normal;

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

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}

void fragment() {
	vec3 wp = v_world_pos;
	vec3 ng = normalize(v_world_normal);
	float view_dist = length(VERTEX);

	float slope01 = acos(clamp(ng.y, -1.0, 1.0)) * 0.636619772;

	// Canopy masses. Two octaves is enough at this range and keeps this cheap.
	vec2 cp = wp.xz / max(canopy_meters, 0.01);
	float canopy = value_noise(cp) * 0.68
		+ value_noise(wp.xz / max(canopy_detail_meters, 0.01)) * 0.32;

	// Base colour: canopy mass shading, lifted toward the highlight by noise.
	vec3 color = mix(canopy_color, canopy_highlight,
		clamp(canopy * canopy_variation * 2.0, 0.0, 1.0));

	// Valley shadow to ridge light. Gives the landform vertical structure even
	// where the directional light hits every face at a similar angle.
	float h = clamp((wp.y - valley_y) / max(ridge_y - valley_y, 0.001), 0.0, 1.0);
	color = mix(mix(valley_color, color, smoothstep(0.0, 0.35, h)), ridge_color,
		smoothstep(0.72, 1.0, h) * 0.55);

	// Steep faces shed their tree cover.
	float bare = smoothstep(slope_begin, slope_end, slope01);
	color = mix(color, slope_color, bare * 0.85);

	// Aerial perspective, on top of whatever the Environment fog does.
	float haze = smoothstep(haze_begin, haze_end, view_dist) * haze_strength;
	color = mix(color, haze_color, haze);

	// Soften facets and add a whisper of canopy relief so the surface is not
	// perfectly smooth under the key light.
	vec3 soft_n = normalize(mix(ng, vec3(0.0, 1.0, 0.0), normal_softening));
	// Two independently offset noise taps, so the perturbation is not locked to
	// the diagonal and does not read as directional streaking across the vista.
	float relief_x = (value_noise(cp + vec2(11.3, 5.1)) - 0.5) * canopy_relief;
	float relief_z = (value_noise(cp + vec2(-7.9, 3.3)) - 0.5) * canopy_relief;
	vec3 world_n = normalize(soft_n + vec3(relief_x, 0.0, relief_z));

	ALBEDO = color * hills_tint * hills_brightness;
	NORMAL = normalize((VIEW_MATRIX * vec4(world_n, 0.0)).xyz);
	ROUGHNESS = hills_roughness;
	METALLIC = 0.0;
	SPECULAR = hills_specular;
}
