"""Create a game-ready hero-tree derivative from the retained Poly Haven source.

Run from the repository root:
  blender --background --python tools/blender/optimize_poly_haven_tree_small_02.py

The source remains untouched. The output is a project-owned GLB derivative with
planar leaf tessellation dissolved and conservative branch/trunk decimation.
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy


REPO_ROOT = Path(__file__).resolve().parents[2]
SOURCE = (
    REPO_ROOT
    / "assets"
    / "third_party"
    / "environment"
    / "vegetation"
    / "poly_haven"
    / "tree_small_02"
    / "tree_small_02_1k.gltf"
)
OUTPUT = (
    REPO_ROOT
    / "assets"
    / "environment"
    / "nature"
    / "ashwood_hero_tree_small_02.glb"
)

LEAF_TRIANGLE_BUDGET = 82_000
TRUNK_DECIMATION_RATIO = 0.42
BRANCH_DECIMATION_RATIO = 0.28
TOTAL_TRIANGLE_BUDGET = 125_000


def triangle_count(obj: bpy.types.Object) -> int:
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def apply_modifier(obj: bpy.types.Object, modifier: bpy.types.Modifier) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    obj.select_set(False)


def main() -> None:
    if not SOURCE.is_file():
        raise FileNotFoundError(f"Poly Haven tree source not found: {SOURCE}")

    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE))

    tree = next(
        obj
        for obj in bpy.context.scene.objects
        if obj.type == "MESH" and obj.name.startswith("tree_small_02_LOD0")
    )
    for obj in list(bpy.context.scene.objects):
        if obj != tree:
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.context.view_layer.objects.active = tree
    tree.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.separate(type="MATERIAL")
    bpy.ops.object.mode_set(mode="OBJECT")
    parts = [obj for obj in bpy.context.selected_objects if obj.type == "MESH"]

    source_triangles = sum(triangle_count(obj) for obj in parts)
    for obj in parts:
        material_names = " ".join(
            material.name.lower() for material in obj.data.materials if material
        )
        if "leaves" in material_names:
            dissolve = obj.modifiers.new("Planar leaf cleanup", "DECIMATE")
            dissolve.decimate_type = "DISSOLVE"
            dissolve.angle_limit = math.radians(1.5)
            dissolve.use_dissolve_boundaries = False
            apply_modifier(obj, dissolve)

            # The scan contains individually modelled leaves. Preserve their
            # silhouette but remove enough leaf density for a close-range game
            # asset instead of a film-render mesh.
            current_triangles = triangle_count(obj)
            if current_triangles > LEAF_TRIANGLE_BUDGET:
                collapse = obj.modifiers.new("Leaf density budget", "DECIMATE")
                collapse.decimate_type = "COLLAPSE"
                collapse.ratio = max(LEAF_TRIANGLE_BUDGET / current_triangles, 0.03)
                collapse.use_collapse_triangulate = True
                apply_modifier(obj, collapse)
        else:
            ratio = (
                TRUNK_DECIMATION_RATIO
                if "trunk" in material_names
                else BRANCH_DECIMATION_RATIO
            )
            collapse = obj.modifiers.new("Woody geometry budget", "DECIMATE")
            collapse.decimate_type = "COLLAPSE"
            collapse.ratio = ratio
            collapse.use_collapse_triangulate = True
            apply_modifier(obj, collapse)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    hero_tree = bpy.context.active_object
    hero_tree.name = "AshwoodHeroTreeSmall02"
    hero_tree.data.name = "AshwoodHeroTreeSmall02Mesh"

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=str(OUTPUT),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_image_format="AUTO",
        export_materials="EXPORT",
        export_cameras=False,
        export_lights=False,
    )

    optimized_triangles = triangle_count(hero_tree)
    print(f"ASHWOOD_HERO_TREE_SOURCE_TRIANGLES={source_triangles}")
    print(f"ASHWOOD_HERO_TREE_OPTIMIZED_TRIANGLES={optimized_triangles}")
    print(f"ASHWOOD_HERO_TREE_OUTPUT={OUTPUT}")
    if optimized_triangles > TOTAL_TRIANGLE_BUDGET:
        raise RuntimeError(
            "Hero-tree optimization exceeded the project triangle budget: "
            f"{optimized_triangles:,} > {TOTAL_TRIANGLE_BUDGET:,}."
        )


if __name__ == "__main__":
    main()
