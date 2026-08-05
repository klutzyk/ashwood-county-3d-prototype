// Ashwood County water - reservoir, river, tributary, spillway and plunge pool.
//
// ONE shader serves every water body in the county. Lake, river, creek and
// whitewater differ only by uniform values on the ShaderMaterial, which keeps
// the county to a single shader compile and lets the four materials share the
// same generated normal map. Four separate shaders would have cost four
// pipeline variants and four texture copies for no visual gain.
//
// This is a Forward+ shader and it uses both the screen and depth textures.
//
// WHAT MAKES IT READ AS WATER, in order of importance:
//
//   1. DEPTH. Everything - colour, opacity, refraction strength, refraction
//      blur, foam - is driven by the real distance the view ray travels
//      through the water, read from hint_depth_texture. Shallow water shows
//      the bed; deep water saturates to the extinction colour. A constant
//      tint is the single thing that makes water read as coloured plastic.
//
//   2. REFRACTION that respects occlusion. The refracted sample is validated
//      against the depth buffer before it is used: if the distorted UV lands
//      on something NEARER than the water surface, the undistorted UV is used
//      instead. Without that check, objects standing in front of the water
//      (piers, rocks, the player) smear across it - the classic artefact.
//
//   3. FLOW. Two normal-map layers at different scales and rates are advected
//      along a per-vertex flow vector using the standard two-phase flow-map
//      cycle, so the surface genuinely travels downstream instead of
//      scrolling in a fixed screen direction. Flow speed comes in per-vertex
//      too, so rapids and pools are the same material.
//
// COST: 4 normal fetches + 2 depth fetches + 1 screen fetch = 7 samples per
// pixel, no dynamic branching in the sample path. Cheap enough for an
// integrated Radeon at 1080p; the only real expense is that the whole surface
// is in the transparent pass, so keep the water meshes tight to the wetted
// channel rather than oversized planes.
//
// GEOMETRY CONTRACT (see CountyWater.cs):
//   NORMAL  - must be correct; the ripple perturbs it rather than replacing it.
//   COLOR.r - flow direction X, packed as dir * 0.5 + 0.5
//   COLOR.g - flow direction Z, packed the same way
//   COLOR.b - flow speed 0..1 (0 still water, 1 rapids). Drives advection
//             rate and whitewater.
//   COLOR.a - openness 0..1 (0 at the waterline, 1 in open water). Damps the
//             vertex swell so the mesh edge never lifts out of its bed, and
//             adds the standing wash along the bank.
//   The mesh is authored in world space, so the node holding it must sit at
//   the origin with an identity transform.

shader_type spatial;
render_mode blend_mix, cull_disabled, depth_draw_never, diffuse_burley, specular_schlick_ggx;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D depth_texture : hint_depth_texture, filter_nearest;

// RGB is a tangent-space normal, A is the height field it was derived from.
// Packing the height into alpha means the foam noise costs zero extra fetches.
uniform sampler2D wave_normal : filter_linear_mipmap_anisotropic, repeat_enable;

// ---------------------------------------------------------------- water body
// Per-metre extinction. Red is absorbed hardest in every natural fresh water,
// which is what turns a shallow gravel bed green-brown and a deep reservoir
// blue-black. Tune the RATIO between channels, not just the magnitude.
uniform vec3 absorption = vec3(0.46, 0.19, 0.26);
uniform vec3 scatter_color : source_color = vec3(0.088, 0.150, 0.132);
uniform vec3 deep_color : source_color = vec3(0.017, 0.040, 0.046);
uniform float depth_opaque = 8.0;
uniform float turbidity : hint_range(0.0, 1.0) = 0.45;

// ------------------------------------------------------------------ surface
uniform float macro_scale = 0.045;
uniform float detail_scale = 0.19;
uniform float macro_strength = 1.05;
uniform float detail_strength = 0.55;
uniform float flow_cycle = 0.13;
uniform float flow_metres = 26.0;
uniform float drift_speed = 0.035;
uniform float water_roughness : hint_range(0.0, 1.0) = 0.075;
uniform float wave_height = 0.16;
uniform float wave_length = 13.0;

// -------------------------------------------------------- refraction / sky
uniform float refraction_strength = 0.055;
uniform float refraction_blur = 0.30;
uniform float fresnel_power = 5.0;
uniform vec3 sky_tint : source_color = vec3(0.40, 0.50, 0.60);
uniform float sky_reflection : hint_range(0.0, 3.0) = 0.55;

// --------------------------------------------------------------------- foam
uniform vec3 foam_color : source_color = vec3(0.855, 0.880, 0.870);
uniform float shore_foam_depth = 1.6;
uniform float shore_foam_amount : hint_range(0.0, 2.0) = 1.0;
uniform float rapid_foam : hint_range(0.0, 2.0) = 0.9;
uniform float rapid_threshold : hint_range(0.0, 1.0) = 0.34;
uniform float foam_scale = 0.30;
uniform float shore_alpha_fade = 0.45;

varying vec3 world_pos;
varying vec3 world_normal;

float linear_view_depth(float raw, vec2 uv, mat4 inv_proj) {
	vec3 ndc = vec3(uv * 2.0 - 1.0, raw);
	vec4 view = inv_proj * vec4(ndc, 1.0);
	return -(view.z / view.w);
}

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);

	// Swell. Three non-harmonic sines rather than one, so the crest pattern has
	// no visible period from the air. Amplitude is gated by COLOR.a, which the
	// mesher drives to zero at the waterline: a shoreline vertex that bobs
	// lifts out of its own bed and produces a flickering gap against the bank.
	float k = 6.2831853 / max(wave_length, 0.5);
	float amp = wave_height * COLOR.a;
	float h =
		  0.55 * sin(world_pos.x * k * 0.83 + TIME * 0.61)
		+ 0.45 * sin(world_pos.z * k + TIME * 0.47)
		+ 0.22 * sin((world_pos.x + world_pos.z * 1.7) * k * 1.9 - TIME * 1.13);

	VERTEX.y += h * amp;
	world_pos.y += h * amp;
}

void fragment() {
	float facing = FRONT_FACING ? 1.0 : -1.0;
	vec3 geometric = normalize(world_normal) * facing;

	// ---- flow-mapped ripple ------------------------------------------------
	// The flow vector is metres per flow cycle, not metres per second: the
	// two-phase cross-fade below resets the advection every cycle, which is
	// what stops a flow map from shearing itself into mush.
	vec2 flow = (COLOR.rg * 2.0 - 1.0) * COLOR.b;
	// A constant slow drift keeps still water alive; without it a lake surface
	// with zero flow freezes into a static bump map the moment you stop moving.
	vec2 advect = flow + vec2(0.62, 0.78) * drift_speed;

	float phase_a = TIME * flow_cycle;
	float pa0 = fract(phase_a);
	float pa1 = fract(phase_a + 0.5);
	float mix_a = abs(1.0 - 2.0 * pa0);

	vec2 uv_a = world_pos.xz * macro_scale;
	vec2 step_a = advect * flow_metres * macro_scale;
	vec4 a0 = texture(wave_normal, uv_a - step_a * pa0);
	vec4 a1 = texture(wave_normal, uv_a - step_a * pa1 + vec2(0.37, 0.19));
	vec4 na = mix(a0, a1, mix_a);

	float phase_b = TIME * flow_cycle * 2.3 + 0.31;
	float pb0 = fract(phase_b);
	float pb1 = fract(phase_b + 0.5);
	float mix_b = abs(1.0 - 2.0 * pb0);

	vec2 uv_b = world_pos.xz * detail_scale + vec2(0.61, 0.13);
	vec2 step_b = advect * flow_metres * detail_scale;
	vec4 b0 = texture(wave_normal, uv_b - step_b * pb0);
	vec4 b1 = texture(wave_normal, uv_b - step_b * pb1 + vec2(0.11, 0.53));
	vec4 nb = mix(b0, b1, mix_b);

	// Perturb the geometric normal horizontally rather than building a tangent
	// frame. For the near-horizontal bodies this is exactly a tangent-space
	// lookup, and it degrades sanely on the spillway chute, which is close to
	// vertical and has no meaningful UV parameterisation.
	vec2 ripple = (na.xy - 0.5) * 2.0 * macro_strength + (nb.xy - 0.5) * 2.0 * detail_strength;
	vec3 surface_normal = normalize(geometric + vec3(ripple.x, 0.0, ripple.y));
	NORMAL = normalize((VIEW_MATRIX * vec4(surface_normal, 0.0)).xyz);

	// ---- how much water is the ray looking through -------------------------
	float surface_depth = -VERTEX.z;
	float raw_bed = texture(depth_texture, SCREEN_UV).x;
	float bed_depth = linear_view_depth(raw_bed, SCREEN_UV, INV_PROJECTION_MATRIX);
	float thickness = bed_depth - surface_depth;

	// ---- refraction --------------------------------------------------------
	// Distortion is proportional to how much water is actually there (a
	// two-centimetre film cannot bend anything) and inversely proportional to
	// distance, so the wobble stays a constant size on screen instead of
	// turning the far bank into static.
	vec3 view_normal = normalize((VIEW_MATRIX * vec4(surface_normal, 0.0)).xyz);
	float bend = refraction_strength
		* clamp(thickness / 2.5, 0.0, 1.0)
		/ max(surface_depth * 0.05, 1.0);
	vec2 refract_uv = SCREEN_UV + view_normal.xy * bend;

	// Reject the refracted sample if it lands on geometry standing in FRONT of
	// the water. Skipping this is what smears bridge piers and wading figures
	// sideways across the surface.
	float raw_refract = texture(depth_texture, refract_uv).x;
	float refract_bed = linear_view_depth(raw_refract, refract_uv, INV_PROJECTION_MATRIX);
	if (refract_bed < surface_depth) {
		refract_uv = SCREEN_UV;
		refract_bed = bed_depth;
	}

	float path = max(refract_bed - surface_depth, 0.0);
	float depth_t = clamp(path / depth_opaque, 0.0, 1.0);

	// Silt scatters as well as absorbs, so the bed does not just dim with
	// depth, it goes progressively out of focus. One mip step per few metres
	// is enough to sell suspended sediment.
	vec3 refracted = textureLod(screen_texture, refract_uv, min(path * refraction_blur, 4.0)).rgb;

	vec3 transmit = exp(-absorption * path) * (1.0 - turbidity * depth_t);
	float opacity = clamp(1.0 - dot(transmit, vec3(0.3333)), 0.0, 1.0);

	// ---- foam --------------------------------------------------------------
	// The noise is the alpha channel of the same two normal fetches, advected
	// with the flow, so foam travels downstream with the surface it sits on.
	float grain = na.a * 0.62 + nb.a * 0.38;
	float grain_fine = texture(wave_normal, world_pos.xz * foam_scale
		- advect * flow_metres * foam_scale * pa0).a;

	// Standing wash where the water runs thin over its bed, plus a tighter lip
	// right at the waterline so the band has an edge instead of a gradient.
	float shallow = 1.0 - smoothstep(0.0, shore_foam_depth, path);
	float lip = 1.0 - smoothstep(0.0, shore_foam_depth * 0.24, path);
	float bank = 1.0 - COLOR.a;
	float shore_foam = smoothstep(0.30, 0.80, max(shallow, bank * 0.85) * (0.42 + grain * 1.05));
	shore_foam = clamp(shore_foam + lip * 0.5, 0.0, 1.0) * shore_foam_amount;

	// Whitewater on gradient. Broken by two noise scales so rapids read as
	// tumbling rather than as a painted stripe.
	float speed = smoothstep(rapid_threshold, 1.0, COLOR.b);
	float rapids = smoothstep(0.26, 0.86, speed * (0.30 + grain_fine * 0.85 + grain * 0.45)) * rapid_foam;

	float foam = clamp(shore_foam + rapids, 0.0, 1.0);

	// ---- shading -----------------------------------------------------------
	float fresnel = pow(1.0 - clamp(dot(NORMAL, normalize(-VERTEX)), 0.0, 1.0), fresnel_power);

	vec3 body = mix(scatter_color, deep_color, depth_t);

	// ALBEDO carries only what should respond to light: the in-water scatter
	// and the foam. The refracted image is already lit scene colour and goes
	// through EMISSION, otherwise the riverbed gets shaded twice and every
	// shadow that falls on the water also darkens the gravel underneath it.
	ALBEDO = mix(body * opacity, foam_color, foam);
	EMISSION = refracted * transmit * (1.0 - foam)
		+ sky_tint * fresnel * sky_reflection * (1.0 - foam * 0.7);

	// A turbid county river is not a mirror. Roughness stays well off zero so
	// the sky reflection is a broad sheen rather than a mirrored skyline, and
	// foam is close to matte.
	ROUGHNESS = mix(water_roughness, 0.74, foam);
	METALLIC = 0.0;
	SPECULAR = mix(0.55, 0.18, foam);

	// The only place alpha is not 1: the last few centimetres at the waterline,
	// where the surface is faded out against the bed. That soft intersection is
	// what removes the hard cut-out edge a water plane otherwise leaves on a
	// beach, and it lets the mesher be a whole cell sloppy about the shore.
	ALPHA = clamp(smoothstep(0.0, shore_alpha_fade, thickness), 0.0, 1.0);
}
