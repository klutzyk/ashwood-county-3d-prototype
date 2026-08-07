shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_lambert, specular_disabled;

// Ashwood County, seen from kilometres away.
//
// Deliberately textureless. The streamed terrain material tiles a 2K scan every
// six metres, which is correct underfoot and catastrophic at four kilometres: the
// repeat falls far below one pixel, and no amount of mip filtering stops it
// boiling into a field of sparkling moire as the camera moves. The far field is
// the one place where flat colour genuinely beats a photograph.
//
// What actually carries distance is not surface detail but large-scale colour
// variation and correct silhouette, so this spends its whole budget there:
// vertex-weighted biome colour plus a low-frequency field that breaks the hills
// into stands and clearings.
//
// Vertex colour matches the streamed mesher's convention, minus the channels that
// are meaningless at this range:
//   splat.r  forest
//   splat.g  rock
//   grass is the base coat

uniform vec3 grass_color : source_color = vec3(0.32, 0.38, 0.21);
// Matched to county_terrain.gdshader's canopy_color. The streamed terrain fades to
// canopy colour with distance, and this takes over past the streaming radius, so a
// mismatch here would draw a visible ring around the player.
uniform vec3 forest_color : source_color = vec3(0.155, 0.209, 0.117);
uniform vec3 rock_color : source_color = vec3(0.34, 0.33, 0.31);

uniform float macro_meters : hint_range(50.0, 2000.0) = 420.0;
uniform float macro_strength : hint_range(0.0, 1.0) = 0.30;

// Distant slopes facing away from the sun need to stay readable as landform, so a
// little extra normal contrast is added rather than relying on lambert alone.
uniform float slope_shading : hint_range(0.0, 1.0) = 0.35;

// NOTE: the vertex colour is forwarded through an explicit varying rather than
// read as COLOR inside fragment(). In a Godot 4 custom spatial shader, COLOR is
// only the mesh's vertex colour inside vertex(); in fragment() it reads as opaque
// white unless the shader passes it along itself. Reading it directly made every
// splat weight 1.0, which silently blended all layers at once.
varying vec4 splat;
varying vec3 world_position;
varying vec3 world_normal;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float value_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash21(i), hash21(i + vec2(1.0, 0.0)), f.x),
               mix(hash21(i + vec2(0.0, 1.0)), hash21(i + vec2(1.0, 1.0)), f.x), f.y);
}

void vertex() {
    splat = COLOR;
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize((MODEL_MATRIX * vec4(NORMAL, 0.0)).xyz);
}

void fragment() {
    float w_forest = splat.r;
    float w_rock = splat.g;
    float w_grass = clamp(1.0 - max(w_forest, w_rock), 0.0, 1.0);

    float total = max(w_forest + w_rock + w_grass, 0.0001);
    vec3 albedo = (forest_color * w_forest + rock_color * w_rock + grass_color * w_grass) / total;

    // Two scales of variation so ridges do not read as one flat wash of green.
    vec2 p = world_position.xz / macro_meters;
    float variation = value_noise(p) * 0.62 + value_noise(p * 3.1 + 19.0) * 0.38;
    albedo *= 1.0 + (variation - 0.5) * macro_strength;

    // Forested ground gets its own, much finer break-up. Past the imposter range
    // this is all that stands in for individual trees, and a smooth wash of green
    // over a whole mountain is the thing that most gives away that the far field
    // is painted rather than grown. Stands and clearings at roughly tree-clump
    // scale restore the mottling the cards were providing closer in.
    if (w_forest > 0.01) {
        vec2 clump = world_position.xz / 46.0;
        float crowns = value_noise(clump) * 0.58 + value_noise(clump * 2.7 + 11.0) * 0.42;
        albedo = mix(albedo, albedo * (0.68 + crowns * 0.72), w_forest);
    }

    // Steeper ground reads slightly darker and greyer, which is what makes the
    // northern ridgelines legible against the haze.
    float steepness = 1.0 - clamp(world_normal.y, 0.0, 1.0);
    albedo = mix(albedo, albedo * 0.72 + rock_color * 0.18, steepness * slope_shading);

    ALBEDO = clamp(albedo, vec3(0.0), vec3(1.0));
    ROUGHNESS = 1.0;
    METALLIC = 0.0;
}
