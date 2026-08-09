shader_type spatial;

// Two triangles standing in for a tree.
//
// Used past the range where real canopy geometry is affordable. The jacaranda is
// 10.6k triangles even at LOD1, so the few hundred trees a stand needs to read as
// woodland would cost more than the rest of the frame on an integrated GPU. This
// costs two triangles and one texture fetch, which is what lets the forest be
// dense enough to look like a forest at all.
//
// The atlas is a horizontal strip of ATLAS_CELLS views, baked evenly around the
// compass by tools/BakeTreeImposters.cs. The cell nearest the current view angle
// is chosen per vertex, so a tree turns as you walk around it instead of spinning
// to follow the camera - the giveaway that betrays a naive billboard.

render_mode blend_mix, depth_draw_opaque, cull_disabled, diffuse_lambert, specular_disabled;

uniform sampler2D imposter_atlas : source_color, filter_linear_mipmap, repeat_disable;

// Must match AngleCount in the baker.
uniform int atlas_cells = 8;

// Species stacked one per row. Each instance picks its row from the MultiMesh's
// custom data, which is what lets every conifer share a single batch instead of
// needing one draw call per species.
uniform int atlas_rows = 1;

// Cutout rather than blending: alpha-blended foliage needs back-to-front sorting,
// which for tens of thousands of scattered instances is both expensive and still
// wrong. A scissor is order-independent and costs nothing.
uniform float alpha_threshold : hint_range(0.0, 1.0) = 0.34;

uniform vec3 canopy_tint : source_color = vec3(1.0, 1.0, 1.0);

// A flat card lit by its own facing direction reads as cardboard. Bending the
// shading normal outward across the quad fakes enough roundness that a stand of
// imposters holds up next to real trees.
uniform float roundness : hint_range(0.0, 1.0) = 0.55;

varying vec2 atlas_uv;
varying vec3 shade_normal;

// Per-instance tint from the MultiMesh. Forwarded through an explicit varying
// because in a Godot 4 spatial shader COLOR is only the instance/vertex colour
// inside vertex(); read in fragment() it is opaque white, which silently makes
// every tree identical - the exact bug that made the terrain splat weights all
// read as 1.0 earlier in this project.
varying vec3 instance_tint;

void vertex() {
    vec3 origin = MODEL_MATRIX[3].xyz;

    // Yaw-only billboard. Pitching toward the camera as well would let trees lean
    // back as you climb a ridge, which is instantly wrong on a hillside.
    vec3 to_camera = CAMERA_POSITION_WORLD - origin;
    to_camera.y = 0.0;
    float span = length(to_camera);
    vec3 facing = span > 0.0001 ? to_camera / span : vec3(0.0, 0.0, 1.0);
    vec3 side = normalize(cross(vec3(0.0, 1.0, 0.0), facing));

    // Scale lives in the instance transform, which the billowing basis above
    // discards, so it is reapplied by hand.
    float scale_x = length(MODEL_MATRIX[0].xyz);
    float scale_y = length(MODEL_MATRIX[1].xyz);

    vec3 world = origin
        + side * (VERTEX.x * scale_x)
        + vec3(0.0, 1.0, 0.0) * (VERTEX.y * scale_y);

    POSITION = PROJECTION_MATRIX * VIEW_MATRIX * vec4(world, 1.0);

    // Which baked view to show. The baker placed its camera at
    // centre + (sin a, 0, cos a) * distance, so the same atan recovers a.
    float cells = float(atlas_cells);
    float angle = atan(facing.x, facing.z) / 6.2831853 + 0.5;
    float cell = floor(fract(angle) * cells + 0.5);
    cell = mod(cell, cells);

    // INSTANCE_CUSTOM.r carries the species row, normalised. Rounding rather than
    // flooring keeps an instance off the seam between two rows when the value is
    // reconstructed from the 8-bit custom data channel.
    float rows = float(atlas_rows);
    float row = clamp(floor(INSTANCE_CUSTOM.r * rows + 0.5), 0.0, rows - 1.0);

    vec2 quad_uv = vec2(UV.x, UV.y);
    atlas_uv = vec2(
        (cell + quad_uv.x) / cells,
        (row + quad_uv.y) / rows);

    // Canopy is lit from above far more than from the side. Weighting the shading
    // normal toward vertical rather than toward the camera stops every card facing
    // the sun at once, which flattened a whole valley of trees into the same pale
    // yellow and was the loudest tell that they were cardboard.
    vec3 bent = normalize(side * (UV.x * 2.0 - 1.0) * 0.75 + vec3(0.0, 1.35, 0.0) + facing * 0.35);
    shade_normal = normalize(mix(vec3(0.0, 1.0, 0.0), bent, roundness));

    instance_tint = COLOR.rgb;
}

void fragment() {
    vec4 texel = texture(imposter_atlas, atlas_uv);

    // Discard before anything else so cut-out pixels cost nothing further.
    if (texel.a < alpha_threshold) {
        discard;
    }

    ALBEDO = texel.rgb * canopy_tint * instance_tint;
    NORMAL = normalize((VIEW_MATRIX * vec4(shade_normal, 0.0)).xyz);
    ROUGHNESS = 0.92;
    METALLIC = 0.0;
    SPECULAR = 0.1;
    ALPHA = 1.0;
}
