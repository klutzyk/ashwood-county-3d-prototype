"""Render a built .glb to a PNG, without Godot.

The project normally judges assets by rendering them in Godot, but that is not
always available - and an asset that has only ever been checked by its triangle
count has not been checked at all. A conifer can hit its budget exactly and
still have an inside-out crown, a stretched atlas or a canopy of grey rectangles
where the alpha failed. Those are all visible instantly and only in a picture.

    blender --background --python tools/blender/preview_asset.py -- \
        --glb assets/environment/nature/polyhaven/ashwood_fir_a_lod0.glb \
        --out .godot/veg_preview/fir_a.png

Renders on a mid-grey backdrop so both the dark needle mass and the cut-out
silhouette are readable; a white or black background hides one or the other.
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def clear_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def scene_bounds(objects):
    lo = Vector((1e18, 1e18, 1e18))
    hi = Vector((-1e18, -1e18, -1e18))
    for obj in objects:
        if obj.type != "MESH":
            continue
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            for axis in range(3):
                lo[axis] = min(lo[axis], world[axis])
                hi[axis] = max(hi[axis], world[axis])
    return lo, hi


def main() -> None:
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--glb", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--size", type=int, default=900)
    args = parser.parse_args(argv)

    clear_scene()
    bpy.ops.import_scene.gltf(filepath=str(Path(args.glb).resolve()))
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        raise SystemExit(f"{args.glb}: imported no meshes")

    lo, hi = scene_bounds(meshes)
    centre = (lo + hi) * 0.5
    height = hi.z - lo.z
    radius = max((hi - lo).length * 0.5, 0.001)

    # Alpha clip, not blend. Blended foliage without sorting renders as a mess of
    # half-transparent shells and would misrepresent how the asset actually looks.
    for mat in bpy.data.materials:
        mat.blend_method = "CLIP"
        if hasattr(mat, "alpha_threshold"):
            mat.alpha_threshold = 0.35
        mat.use_backface_culling = False

    # Three-quarter view from slightly above the midpoint: a straight-on
    # elevation hides the crown's depth and a top-down hides the silhouette,
    # which is the single most important thing to judge on a conifer.
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.lens = 70.0
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    bpy.context.scene.collection.objects.link(cam)

    angle = math.radians(35.0)
    distance = radius * 3.1
    cam.location = (
        centre.x + math.cos(angle) * distance,
        centre.y - math.sin(angle) * distance,
        centre.z + height * 0.18,
    )
    direction = centre - Vector(cam.location)
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam

    sun_data = bpy.data.lights.new("Sun", type="SUN")
    sun_data.energy = 3.2
    sun = bpy.data.objects.new("Sun", sun_data)
    bpy.context.scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(52.0), 0.0, math.radians(35.0))

    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (
        0.34, 0.38, 0.44, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.1
    bpy.context.scene.world = world

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = args.size
    scene.render.resolution_y = args.size
    scene.render.film_transparent = False
    scene.render.filepath = str(Path(args.out).resolve())

    out_path = Path(args.out).resolve()
    out_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.render.render(write_still=True)

    tris = sum(len(o.data.loop_triangles) for o in meshes
               if (o.data.calc_loop_triangles() or True))
    print(f"PREVIEW: {Path(args.glb).name} h={height:.2f}m "
          f"w={max(hi.x - lo.x, hi.y - lo.y):.2f}m tris={tris} -> {out_path}")


if __name__ == "__main__":
    main()
