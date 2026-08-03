// Blackwater River surface.
//
// Authored for the GL Compatibility renderer: no screen texture, no depth texture,
// no SSR. Everything is procedural so the shader carries no texture dependencies.
//
// Shore foam is derived analytically from the same gorge channel profile that
// OldMillBridge.cs uses to generate the terrain, so the foam band tracks the real
// waterline without needing a depth buffer.
shader_type spatial;
render_mode blend_mix, cull_back, diffuse_burley, specular_schlick_ggx, depth_prepass_alpha;

uniform vec3 shallow_color : source_color = vec3(0.196, 0.278, 0.271);
uniform vec3 deep_color : source_color = vec3(0.043, 0.078, 0.094);
uniform vec3 foam_color : source_color = vec3(0.78, 0.80, 0.79);
uniform float flow_speed = 0.16;
uniform float wave_scale = 0.42;
uniform float wave_strength : hint_range(0.0, 2.0) = 0.55;
uniform float water_roughness : hint_range(0.0, 1.0) = 0.08;
uniform float water_alpha : hint_range(0.0, 1.0) = 0.88;
uniform float fresnel_power = 4.0;

// Must mirror OldMillBridge.ChannelCenterX / the meander terms in GorgeHeight().
uniform float channel_center_x = -176.0;
uniform float meander_amplitude = 6.0;
uniform float meander_period = 45.0;
uniform float water_level = -8.5;
// Depth in metres below which the surface starts breaking into whitewater.
uniform float foam_width = 1.5;

float hash21(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
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

float fbm(vec2 p) {
	float total = 0.0;
	float amplitude = 0.5;
	for (int i = 0; i < 4; i++) {
		total += amplitude * value_noise(p);
		p *= 2.03;
		amplitude *= 0.5;
	}
	return total;
}

// Height field of the ripple surface, sampled in world XZ.
float ripple(vec2 world_xz, float t) {
	// Two layers scrolling downstream (+Z) at different rates, plus a slow
	// cross-current layer, so the surface never visibly tiles or pulses.
	float a = fbm(world_xz * wave_scale + vec2(0.0, -t * flow_speed * 6.0));
	float b = fbm(world_xz * wave_scale * 2.17 + vec2(t * flow_speed * 1.4, -t * flow_speed * 9.0));
	float c = fbm(world_xz * wave_scale * 0.41 + vec2(-t * flow_speed * 0.7, -t * flow_speed * 2.0));
	return a * 0.5 + b * 0.3 + c * 0.2;
}

float smooth01(float t) {
	t = clamp(t, 0.0, 1.0);
	return t * t * (3.0 - 2.0 * t);
}

// Exact mirror of OldMillBridge.GorgeHeight(). Sampling the real bed height here
// gives a true water depth without a depth buffer, which the Compatibility
// renderer does not expose. Depth then drives both the shore foam band and the
// alpha falloff, so the water always terminates precisely at the waterline
// instead of clipping through the bank.
float gorge_height(vec2 p) {
	float cx = channel_center_x
		+ meander_amplitude * sin(p.y / meander_period)
		+ 2.5 * sin(p.y / 17.0);
	float d = abs(p.x - cx);

	float h;
	if (d <= 20.0) {
		h = -10.5;
	} else if (d <= 26.0) {
		h = mix(-10.5, -8.0, smooth01((d - 20.0) / 6.0));
	} else if (d <= 32.0) {
		h = mix(-8.0, -2.5, smooth01((d - 26.0) / 6.0));
	} else if (d <= 36.0) {
		h = mix(-2.5, 0.0, smooth01((d - 32.0) / 4.0));
	} else {
		return 0.0;
	}

	float bank_blend = sin(3.14159265 * clamp((d - 18.0) / 18.0, 0.0, 1.0));

	float ledge = 1.45;
	float ledge_base = floor(h / ledge) * ledge;
	float ledge_fraction = (h - ledge_base) / ledge;
	float terraced = ledge_base + ledge * smooth01(clamp(ledge_fraction * 1.85 - 0.42, 0.0, 1.0));
	h = mix(h, terraced, 0.72 * bank_blend);

	float detail =
		0.85 * sin(p.x * 0.31 + p.y * 0.17)
		+ 0.55 * sin(p.x * 0.73 - p.y * 0.41)
		+ 0.30 * sin(p.x * 1.47 + p.y * 1.13);
	return h + detail * bank_blend * 0.75;
}

void fragment() {
	vec2 world_xz = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xz;
	float t = TIME;

	// Finite-difference the ripple field to build a tangent-space normal.
	float eps = 0.35;
	float h = ripple(world_xz, t);
	float hx = ripple(world_xz + vec2(eps, 0.0), t);
	float hz = ripple(world_xz + vec2(0.0, eps), t);
	vec2 gradient = vec2(hx - h, hz - h) / eps;

	NORMAL_MAP = normalize(vec3(-gradient.x, -gradient.y, 1.0)) * 0.5 + 0.5;
	NORMAL_MAP_DEPTH = wave_strength;

	// Fresnel-weighted depth tint: steep viewing angles read as deep water.
	float facing = clamp(dot(normalize(NORMAL), normalize(VIEW)), 0.0, 1.0);
	float fresnel = pow(1.0 - facing, fresnel_power);
	vec3 body = mix(deep_color, shallow_color, clamp(facing * 0.85 + h * 0.25, 0.0, 1.0));

	// True water depth from the analytic bed height.
	float depth = water_level - gorge_height(world_xz);

	// Shallow water lightens toward the bed; deep water saturates to deep_color.
	body = mix(body, shallow_color * 1.35, clamp(1.0 - depth / 2.6, 0.0, 1.0) * 0.55);

	// Foam gathers where the water runs thin over the bed, broken up by noise so
	// it reads as moving whitewater rather than a drawn contour line.
	float foam_noise = fbm(world_xz * 1.35 + vec2(0.0, -t * flow_speed * 4.0));
	float shallow_band = smoothstep(foam_width, 0.12, depth);
	float foam = smoothstep(0.42, 0.92, shallow_band * (0.5 + foam_noise * 0.85));

	ALBEDO = mix(body, foam_color, foam);
	ROUGHNESS = mix(water_roughness, 0.82, foam);
	METALLIC = 0.0;
	SPECULAR = mix(0.5, 0.15, foam);

	// Fade out exactly at the waterline. Anywhere the bed is above the water
	// plane this drives alpha to zero, so the surface can never clip through
	// the bank no matter how the meander moves.
	float shoreline_fade = smoothstep(-0.02, 0.30, depth);
	ALPHA = clamp((mix(water_alpha, 1.0, foam) + fresnel * 0.06) * shoreline_fade, 0.0, 1.0);
}
