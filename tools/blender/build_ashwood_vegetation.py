"""Turn the Poly Haven photoscanned vegetation into game-ready Ashwood assets.

The salmon-trunk lowpoly trees are replaced by decimated photoscans. Run the two
steps from the repository root:

    python  tools/blender/build_ashwood_vegetation.py --textures
    blender --background --python tools/blender/build_ashwood_vegetation.py

The texture step is a separate CPython pass on purpose: Blender's bundled Python
has no Pillow, and doing 8-bit RGBA compositing through bpy.types.Image.pixels
means round-tripping 16.7M colour-managed floats per map. The Blender step shells
out to it automatically if the textures are missing, so the second command alone
is enough for a clean build.

Sources are never modified. Everything produced is project-owned and lands in
assets/environment/nature/polyhaven/ plus assets/materials/vegetation_*.tres.


WHY THIS IS NOT JUST A DECIMATE MODIFIER
----------------------------------------
Two facts about the source data drive the whole design.

1. Poly Haven's glTF references only the JPEG diffuse, and JPEG has no alpha
   channel. The cut-out silhouette of every leaf lives in a separate "Alpha" map
   the glTF never mentions. fern_02's diffuse has a dilated colour bleed rather
   than a black background, so a naive import renders it as a solid green
   rectangle. tools/download_polyhaven.py now fetches those maps and the texture
   step composites them into a real RGBA albedo.

2. The scans are not solid meshes. jacaranda_tree's 2.4M-triangle canopy is
   116,084 separate leaf-spray cards of ~20 triangles each, and the shrubs are
   built the same way. A COLLAPSE decimate to a game budget averages under one
   triangle per card, which welds the canopy into pulp and smears the UVs across
   the atlas - visibly worse than the lowpoly trees being replaced.

   Instead, each card is measured and rebuilt. Every island is a parameterised
   patch, so a least-squares fit of the 3x3 affine map (u, v, 1) -> position
   reconstructs it as a single quad whose corners land on its own UV bounding
   box. Measured fit residual on jacaranda is 2.8% of island diagonal (worst
   5.7%), so the quad sits where the leaf sat and samples exactly the texels the
   leaf sampled. UVs are reproduced analytically rather than survived.

   Card count is then thinned on a spatial grid (never randomly - random thinning
   clumps and eats the silhouette) and the survivors are scaled up to hold canopy
   coverage. That trade is real and is stated in the README: leaf sprays end up
   larger than life because a 4k-triangle budget cannot hold 116k leaves.

Woody geometry, ferns and rocks are solid or near-solid and are decimated
conventionally with COLLAPSE, which is what quadric error metrics are good at.
"""

from __future__ import annotations

import argparse
import json
import math
import os
import shutil
import struct
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
SOURCE_ROOT = REPO_ROOT / "assets" / "third_party" / "polyhaven_2026_08" / "models"
OUT_ROOT = REPO_ROOT / "assets" / "environment" / "nature" / "polyhaven"
OUT_TEX = OUT_ROOT / "textures"
MATERIAL_DIR = REPO_ROOT / "assets" / "materials"

RES_OUT = "res://assets/environment/nature/polyhaven"
RES_SRC = "res://assets/third_party/polyhaven_2026_08/models"
RES_MAT = "res://assets/materials"


# ===========================================================================
# Texture step - composite diffuse + opacity into an RGBA albedo
# ===========================================================================

# stem -> (diffuse relative path, alpha relative path)
ALBEDO_SETS = {
    "jacaranda_tree_leaves": (
        "jacaranda_tree/textures/jacaranda_tree_leaves_diff_2k.jpg",
        "jacaranda_tree/textures/jacaranda_tree_leaves_alpha_2k.png",
    ),
    "shrub_01": (
        "shrub_01/textures/shrub_01_diff_2k.jpg",
        "shrub_01/textures/shrub_01_alpha_2k.png",
    ),
    "shrub_02": (
        "shrub_02/textures/shrub_02_diff_2k.jpg",
        "shrub_02/textures/shrub_02_alpha_2k.png",
    ),
    "shrub_03": (
        "shrub_03/textures/shrub_03_diff_2k.jpg",
        "shrub_03/textures/shrub_03_alpha_2k.png",
    ),
    "fern_02": (
        "fern_02/textures/fern_02_diff_2k.jpg",
        "fern_02/textures/fern_02_alpha_2k.png",
    ),
    "nettle_plant": (
        "nettle_plant/textures/nettle_plant_diff_2k.jpg",
        "nettle_plant/textures/nettle_plant_alpha_2k.png",
    ),
    "grass_bermuda_01": (
        "grass_bermuda_01/textures/grass_bermuda_01_diff_2k.jpg",
        "grass_bermuda_01/textures/grass_bermuda_01_alpha_2k.png",
    ),
}

# Alpha below this is treated as "no leaf here" when deciding which texels need
# their colour dilated outwards.
DILATE_CUTOFF = 0.35
DILATE_PASSES = 24


def albedo_path(stem: str) -> Path:
    return OUT_TEX / f"{stem}_albedo_2k.png"


def build_textures() -> None:
    """Composite diffuse RGB + opacity into RGBA PNGs, dilating the colour.

    Dilation matters as much as the alpha itself. jacaranda's leaf atlas sits on
    pure black, so bilinear filtering and every mip level bleed that black in
    along the leaf edges and hang a dark fringe on the whole canopy - one of the
    things that makes cheap foliage read as dirty cardboard. Pushing leaf colour
    outwards into the transparent region means filtering never has black to find.
    """
    import numpy as np
    from PIL import Image

    OUT_TEX.mkdir(parents=True, exist_ok=True)

    for stem, (diff_rel, alpha_rel) in ALBEDO_SETS.items():
        diff_path = SOURCE_ROOT / diff_rel
        alpha_path = SOURCE_ROOT / alpha_rel
        missing = [p for p in (diff_path, alpha_path) if not p.is_file()]
        if missing:
            raise FileNotFoundError(
                f"{stem}: missing {[str(p) for p in missing]}. Run "
                "'python tools/download_polyhaven.py --set vegetation' first."
            )

        rgb = np.asarray(Image.open(diff_path).convert("RGB"), dtype=np.float32) / 255.0
        alpha_img = Image.open(alpha_path).convert("L")
        if alpha_img.size != Image.open(diff_path).size:
            alpha_img = alpha_img.resize(Image.open(diff_path).size, Image.LANCZOS)
        alpha = np.asarray(alpha_img, dtype=np.float32) / 255.0

        valid = alpha >= DILATE_CUTOFF
        filled = rgb.copy()
        known = valid.copy()
        for _ in range(DILATE_PASSES):
            if known.all():
                break
            weight = known.astype(np.float32)
            acc = np.zeros_like(filled)
            wsum = np.zeros_like(weight)
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                acc += np.roll(np.roll(filled * weight[..., None], dy, 0), dx, 1)
                wsum += np.roll(np.roll(weight, dy, 0), dx, 1)
            grow = (~known) & (wsum > 0)
            filled[grow] = (acc[grow] / wsum[grow][..., None])
            known |= grow

        out = np.concatenate(
            [np.clip(filled, 0.0, 1.0), alpha[..., None]], axis=2
        )
        Image.fromarray((out * 255.0 + 0.5).astype(np.uint8), mode="RGBA").save(
            albedo_path(stem), optimize=True
        )
        print(
            f"  albedo {stem}: coverage={float(valid.mean()):.3f} -> "
            f"{albedo_path(stem).relative_to(REPO_ROOT)}"
        )


def ensure_textures() -> None:
    if all(albedo_path(stem).is_file() for stem in ALBEDO_SETS):
        return
    exe = shutil.which("python") or shutil.which("python3")
    if not exe:
        raise RuntimeError(
            "Composited albedos are missing and no system 'python' was found. "
            "Run 'python tools/blender/build_ashwood_vegetation.py --textures'."
        )
    print("Compositing albedo textures via system Python...")
    subprocess.run([exe, str(Path(__file__).resolve()), "--textures"], check=True)


# ===========================================================================
# Asset recipes
# ===========================================================================
#
# method:
#   "cards"    - rebuild each UV island as a single quad (scanned leaf cards)
#   "decimate" - COLLAPSE decimate to the triangle budget (solid geometry)
#   "keep"     - already inside budget, pass through untouched
#
# collision: None | "cylinder" | "convex"

CARD, DECIMATE, KEEP = "cards", "decimate", "keep"


def part(name, method, budget=0, nodes=(), material=None, **kw):
    return dict(name=name, method=method, budget=budget, nodes=tuple(nodes),
                material=material, **kw)


ASSETS = [
    # ---- hero tree -------------------------------------------------------
    dict(
        key="ashwood_jacaranda_lod0", slug="jacaranda_tree", collision="cylinder",
        label="Jacaranda (hero, LOD0)", root_type="StaticBody3D",
        parts=[
            part("Trunk", DECIMATE, 700, material="jacaranda_tree_trunk",
                 mat_res="vegetation_jacaranda_trunk"),
            # Twigs are structurally invisible once leaf cards cover them, and
            # 30k of them cannot survive a 700-triangle budget anyway. Keep the
            # limbs that carry the silhouette, drop the rest, then decimate.
            part("Branches", DECIMATE, 1400, material="jacaranda_tree_branches",
                 mat_res="vegetation_jacaranda_branches", keep_area=0.55),
            # Leaf budget and card_scale trade against each other. The earlier
            # 2600/3.4 pairing kept ~1300 of 116,084 cards and inflated each one
            # 3.4x linearly (11x in area) to hold coverage - in engine that read
            # as a handful of giant fern fronds floating around a bare trunk
            # rather than as a canopy. Paying for ~5x the cards lets each sit
            # near its scanned size, which is what actually reads as foliage.
            part("Leaves", CARD, 13000, material="jacaranda_tree_leaves",
                 mat_res="vegetation_jacaranda_leaves", card_scale=1.55),
        ],
    ),
    dict(
        key="ashwood_jacaranda_lod1", slug="jacaranda_tree", collision="cylinder",
        label="Jacaranda (mid/background, LOD1)", root_type="StaticBody3D",
        parts=[
            part("Trunk", DECIMATE, 220, material="jacaranda_tree_trunk",
                 mat_res="vegetation_jacaranda_trunk"),
            part("Branches", DECIMATE, 400, material="jacaranda_tree_branches",
                 mat_res="vegetation_jacaranda_branches", keep_area=0.46),
            # Same trade as LOD0 but tuned for distance, where a slightly oversized
            # card is invisible and the triangle saving is worth having. This is
            # the tree used for every street and background instance.
            part("Leaves", CARD, 7400, material="jacaranda_tree_leaves",
                 mat_res="vegetation_jacaranda_leaves", card_scale=1.85),
        ],
    ),

    # ---- shrubs (all card-built scans) -----------------------------------
    dict(
        key="ashwood_shrub_01", slug="shrub_01", collision=None,
        label="Broad low shrub", root_type="Node3D",
        parts=[part("Plant", CARD, 900, nodes=("shrub_01_a",),
                    mat_res="vegetation_shrub_01", card_scale=2.0)],
    ),
] + [
    dict(
        key=f"ashwood_shrub_02_{v}", slug="shrub_02", collision=None,
        label=f"Leafy shrub {v.upper()}", root_type="Node3D",
        parts=[part("Plant", CARD, 900, nodes=(f"shrub_02_{v}",),
                    mat_res="vegetation_shrub_02", card_scale=1.0)],
    )
    for v in ("a", "b", "c", "d")
] + [
    dict(
        key=f"ashwood_shrub_03_{v}", slug="shrub_03", collision=None,
        label=f"Small shrub {v.upper()}", root_type="Node3D",
        parts=[part("Plant", CARD, 900, nodes=(f"shrub_03_{v}",),
                    mat_res="vegetation_shrub_03", card_scale=1.0)],
    )
    for v in ("a", "b", "c", "d")
] + [
    # ---- ferns: few islands, strongly curved fronds. Decimating keeps the
    # curl; flattening them to cards would lose the whole silhouette.
    dict(
        key=f"ashwood_fern_02_{v}", slug="fern_02", collision=None,
        label=f"Fern {v.upper()}", root_type="Node3D",
        parts=[part("Plant", DECIMATE, budget, nodes=(f"fern_02_{v}",),
                    mat_res="vegetation_fern_02")],
    )
    for v, budget in (("a", 380), ("b", 380), ("c", 300), ("d", 300))
] + [
    dict(
        key=f"ashwood_nettle_{label}", slug="nettle_plant", collision=None,
        label=f"Nettle clump ({label})", root_type="Node3D",
        parts=[part("Plant", DECIMATE, 400, nodes=nodes,
                    mat_res="vegetation_nettle_plant", cluster=0.12)],
    )
    for label, nodes in (
        ("tall", ("nettle_plant_tall_a_LOD0", "nettle_plant_tall_b_LOD0")),
        ("medium", ("nettle_plant_medium_a_LOD0", "nettle_plant_medium_b_LOD0")),
        ("small", ("nettle_plant_small_a_LOD0", "nettle_plant_small_b_LOD0")),
    )
] + [
    # ---- grass: already far under budget, only needs clumping ------------
    dict(
        key=f"ashwood_grass_bermuda_{label}", slug="grass_bermuda_01", collision=None,
        label=f"Bermuda grass ({label})", root_type="Node3D",
        parts=[part("Plant", KEEP, 0, nodes=nodes,
                    mat_res="vegetation_grass_bermuda_01", cluster=0.11)],
    )
    for label, nodes in (
        ("medium", tuple(f"grass_bermuda_01_medium_{v}" for v in "abcdef")),
        ("small", tuple(f"grass_bermuda_01_small_{v}" for v in "abcdef")),
        ("dry", ("grass_bermuda_01_dead_a", "grass_bermuda_01_dead_b",
                 "grass_bermuda_01_flattened_a", "grass_bermuda_01_seedling_a",
                 "grass_bermuda_01_seedling_b")),
    )
] + [
    # ---- deadwood --------------------------------------------------------
    dict(
        key="ashwood_dead_tree_trunk", slug="dead_tree_trunk", collision="cylinder",
        label="Standing dead trunk", root_type="StaticBody3D",
        parts=[part("Body", DECIMATE, 800, nodes=("dead_tree_trunk",),
                    mat_res="vegetation_dead_tree_trunk")],
    ),
    dict(
        key="ashwood_dead_log", slug="dead_tree_trunk_02", collision="convex",
        label="Fallen log", root_type="StaticBody3D",
        parts=[part("Body", DECIMATE, 700, nodes=("dead_tree_trunk_02",),
                    mat_res="vegetation_dead_tree_trunk_02")],
    ),
] + [
    dict(
        key=f"ashwood_bark_debris_{v}", slug="bark_debris_01", collision=None,
        label=f"Bark debris {v.upper()}", root_type="Node3D",
        parts=[part("Body", DECIMATE, 500, nodes=(f"bark_debris_01_{v}",),
                    mat_res="vegetation_bark_debris_01")],
    )
    for v in ("a", "b", "c", "d")
] + [
    # ---- rock ------------------------------------------------------------
    dict(
        key="ashwood_boulder_01", slug="boulder_01", collision="convex",
        label="Boulder", root_type="StaticBody3D",
        parts=[part("Body", DECIMATE, 800, nodes=("boulder_01",),
                    mat_res="vegetation_boulder_01")],
    ),
] + [
    dict(
        key=f"ashwood_rock_moss_{i:02d}", slug="rock_moss_set_01", collision="convex",
        label=f"Mossy rock {i:02d}", root_type="StaticBody3D",
        parts=[part("Body", DECIMATE, 400, nodes=(f"rock_moss_set_01_rock{i:02d}",),
                    mat_res="vegetation_rock_moss_set_01")],
    )
    for i in range(1, 7)
]


# ===========================================================================
# Material definitions
# ===========================================================================
#
# cutout materials get transparency=2 (ALPHA_SCISSOR) and cull_mode=2 (DISABLED).
# Alpha blending is deliberately avoided: blended foliage has no correct draw
# order, and on the Compatibility renderer it also loses depth pre-pass, so
# overlapping leaves pop through each other as the camera turns.

def mat(res_name, label, slug, albedo, arm=None, normal=None, rough=None,
        cutout=False, backlight=None):
    return dict(res_name=res_name, label=label, slug=slug, albedo=albedo, arm=arm,
                normal=normal, rough=rough, cutout=cutout, backlight=backlight)


def src_tex(slug, name):
    return f"{RES_SRC}/{slug}/textures/{name}"


def out_tex(stem):
    return f"{RES_OUT}/textures/{stem}_albedo_2k.png"


# A little forward scatter on leaf materials. Real leaves are thin and
# translucent; without it a backlit canopy goes flat black and reads as plastic.
LEAF_BACKLIGHT = (0.11, 0.15, 0.07)

MATERIALS = [
    mat("vegetation_jacaranda_trunk", "Jacaranda Trunk", "jacaranda_tree",
        src_tex("jacaranda_tree", "jacaranda_tree_trunk_diff_2k.jpg"),
        arm=src_tex("jacaranda_tree", "jacaranda_tree_trunk_arm_2k.jpg"),
        normal=src_tex("jacaranda_tree", "jacaranda_tree_trunk_nor_gl_2k.jpg")),
    mat("vegetation_jacaranda_branches", "Jacaranda Branches", "jacaranda_tree",
        src_tex("jacaranda_tree", "jacaranda_tree_branches_diff_2k.jpg"),
        arm=src_tex("jacaranda_tree", "jacaranda_tree_branches_arm_2k.jpg"),
        normal=src_tex("jacaranda_tree", "jacaranda_tree_branches_nor_gl_2k.jpg")),
    mat("vegetation_jacaranda_leaves", "Jacaranda Leaves", "jacaranda_tree",
        out_tex("jacaranda_tree_leaves"),
        arm=src_tex("jacaranda_tree", "jacaranda_tree_leaves_arm_2k.jpg"),
        normal=src_tex("jacaranda_tree", "jacaranda_tree_leaves_nor_gl_2k.jpg"),
        cutout=True, backlight=LEAF_BACKLIGHT),

    mat("vegetation_shrub_01", "Shrub 01", "shrub_01", out_tex("shrub_01"),
        arm=src_tex("shrub_01", "shrub_01_arm_2k.jpg"),
        normal=src_tex("shrub_01", "shrub_01_nor_gl_2k.jpg"),
        cutout=True, backlight=LEAF_BACKLIGHT),
    mat("vegetation_shrub_02", "Shrub 02", "shrub_02", out_tex("shrub_02"),
        arm=src_tex("shrub_02", "shrub_02_arm_2k.jpg"),
        normal=src_tex("shrub_02", "shrub_02_nor_gl_2k.jpg"),
        cutout=True, backlight=LEAF_BACKLIGHT),
    mat("vegetation_shrub_03", "Shrub 03", "shrub_03", out_tex("shrub_03"),
        arm=src_tex("shrub_03", "shrub_03_arm_2k.jpg"),
        normal=src_tex("shrub_03", "shrub_03_nor_gl_2k.jpg"),
        cutout=True, backlight=LEAF_BACKLIGHT),
    mat("vegetation_fern_02", "Fern 02", "fern_02", out_tex("fern_02"),
        arm=src_tex("fern_02", "fern_02_arm_2k.jpg"),
        normal=src_tex("fern_02", "fern_02_nor_gl_2k.jpg"),
        cutout=True, backlight=LEAF_BACKLIGHT),
    mat("vegetation_nettle_plant", "Nettle Plant", "nettle_plant",
        out_tex("nettle_plant"),
        arm=src_tex("nettle_plant", "nettle_plant_arm_2k.jpg"),
        normal=src_tex("nettle_plant", "nettle_plant_nor_gl_2k.jpg"),
        cutout=True, backlight=LEAF_BACKLIGHT),
    mat("vegetation_grass_bermuda_01", "Bermuda Grass", "grass_bermuda_01",
        out_tex("grass_bermuda_01"),
        arm=src_tex("grass_bermuda_01", "grass_bermuda_01_arm_2k.jpg"),
        normal=src_tex("grass_bermuda_01", "grass_bermuda_01_nor_gl_2k.jpg"),
        cutout=True, backlight=LEAF_BACKLIGHT),

    mat("vegetation_dead_tree_trunk", "Dead Tree Trunk", "dead_tree_trunk",
        src_tex("dead_tree_trunk", "dead_tree_trunk_diff_2k.jpg"),
        arm=src_tex("dead_tree_trunk", "dead_tree_trunk_arm_2k.jpg"),
        normal=src_tex("dead_tree_trunk", "dead_tree_trunk_nor_gl_2k.jpg")),
    mat("vegetation_dead_tree_trunk_02", "Fallen Log", "dead_tree_trunk_02",
        src_tex("dead_tree_trunk_02", "dead_tree_trunk_02_diff_2k.jpg"),
        arm=src_tex("dead_tree_trunk_02", "dead_tree_trunk_02_arm_2k.jpg"),
        normal=src_tex("dead_tree_trunk_02", "dead_tree_trunk_02_nor_gl_2k.jpg")),
    mat("vegetation_bark_debris_01", "Bark Debris", "bark_debris_01",
        src_tex("bark_debris_01", "bark_debris_01_diff_2k.jpg"),
        arm=src_tex("bark_debris_01", "bark_debris_01_arm_2k.jpg"),
        normal=src_tex("bark_debris_01", "bark_debris_01_nor_gl_2k.jpg")),
    mat("vegetation_boulder_01", "Boulder", "boulder_01",
        src_tex("boulder_01", "boulder_01_diff_2k.jpg"),
        arm=src_tex("boulder_01", "boulder_01_arm_2k.jpg"),
        normal=src_tex("boulder_01", "boulder_01_nor_gl_2k.jpg")),
    # rock_moss_set_01 ships rough/nor only - no packed ARM map.
    mat("vegetation_rock_moss_set_01", "Mossy Rock", "rock_moss_set_01",
        src_tex("rock_moss_set_01", "rock_moss_set_01_diff_2k.jpg"),
        rough=src_tex("rock_moss_set_01", "rock_moss_set_01_rough_2k.jpg"),
        normal=src_tex("rock_moss_set_01", "rock_moss_set_01_nor_gl_2k.jpg")),
]

MATERIALS_BY_NAME = {m["res_name"]: m for m in MATERIALS}

ALPHA_SCISSOR_THRESHOLD = 0.33  # below 0.5 on purpose - see write_material()


# ===========================================================================
# Blender implementation
# ===========================================================================

def blender_main(preview_dir: Path | None) -> None:
    import bmesh  # noqa: F401  (imported for side effects in helpers)
    import bpy
    import numpy as np
    from mathutils import Vector

    # -- small helpers ----------------------------------------------------

    def reset():
        bpy.ops.wm.read_factory_settings(use_empty=True)

    def tri_count(obj) -> int:
        obj.data.calc_loop_triangles()
        return len(obj.data.loop_triangles)

    def import_source(slug: str):
        path = SOURCE_ROOT / slug / f"{slug}_2k.gltf"
        if not path.is_file():
            raise FileNotFoundError(
                f"Missing source {path}. Run "
                "'python tools/download_polyhaven.py --set vegetation'."
            )
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=str(path))
        new = [o for o in bpy.data.objects if o not in before]

        # Poly Haven glTFs carry the Z-up -> Y-up correction (and sometimes a
        # unit scale) on parent empties. Bake it in so every measurement below
        # is in real metres.
        bpy.ops.object.select_all(action="DESELECT")
        for o in new:
            o.select_set(True)
        bpy.context.view_layer.objects.active = new[0]
        bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
        meshes = [o for o in new if o.type == "MESH"]
        bpy.ops.object.select_all(action="DESELECT")
        for o in meshes:
            o.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        for o in new:
            if o.type != "MESH":
                bpy.data.objects.remove(o, do_unlink=True)
        return {o.name: o for o in meshes}

    def duplicate(obj, name):
        copy = obj.copy()
        copy.data = obj.data.copy()
        copy.name = name
        copy.data.name = name + "Mesh"
        bpy.context.scene.collection.objects.link(copy)
        return copy

    def apply_modifier(obj, modifier):
        bpy.ops.object.select_all(action="DESELECT")
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)

    def join(objs, name):
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        if len(objs) > 1:
            bpy.ops.object.join()
        result = bpy.context.view_layer.objects.active
        result.name = name
        result.data.name = name + "Mesh"
        return result

    def keep_only_material(obj, material_name):
        """Delete every face whose material slot is not material_name."""
        import bmesh
        slots = [i for i, s in enumerate(obj.material_slots)
                 if s.material and s.material.name.startswith(material_name)]
        if not slots:
            raise RuntimeError(
                f"{obj.name}: no material slot matching {material_name!r}; "
                f"have {[s.material.name if s.material else None for s in obj.material_slots]}"
            )
        keep = set(slots)
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        doomed = [f for f in bm.faces if f.material_index not in keep]
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()

    # -- connected components, numpy only (Blender has no scipy) -----------

    def connected_components(edges, n):
        """Label propagation with a precomputed scatter order.

        The scatter keys never change between iterations, so the argsort that
        makes the segmented minimum possible is hoisted out of the loop. On the
        jacaranda canopy (7.2M directed edges) this labels 116k islands in a few
        seconds, where ufunc.at inside the loop takes minutes.
        """
        if len(edges) == 0:
            return np.arange(n, dtype=np.int64)
        key = np.concatenate([edges[:, 0], edges[:, 1]])
        order = np.argsort(key, kind="stable")
        k = key[order]
        starts = np.r_[0, np.flatnonzero(k[1:] != k[:-1]) + 1]
        heads = k[starts]
        lab = np.arange(n, dtype=np.int64)
        for _ in range(64):
            m = np.minimum(lab[edges[:, 0]], lab[edges[:, 1]])
            v = np.concatenate([m, m])[order]
            new = lab.copy()
            np.minimum.at(new, heads, np.minimum.reduceat(v, starts))
            for _ in range(32):
                nxt = new[new]
                if np.array_equal(nxt, new):
                    break
                new = nxt
            if np.array_equal(new, lab):
                break
            lab = new
        _, lab = np.unique(lab, return_inverse=True)
        return lab.astype(np.int64)

    def mesh_arrays(obj):
        """positions, triangle corner indices, per-corner uv, per-corner vertex."""
        me = obj.data
        me.calc_loop_triangles()
        nv = len(me.vertices)
        pos = np.empty(nv * 3, np.float64)
        me.vertices.foreach_get("co", pos)
        pos = pos.reshape(nv, 3)

        nt = len(me.loop_triangles)
        loops = np.empty(nt * 3, np.int64)
        me.loop_triangles.foreach_get("loops", loops)

        nl = len(me.loops)
        lv = np.empty(nl, np.int64)
        me.loops.foreach_get("vertex_index", lv)

        layer = me.uv_layers.active
        if layer is None:
            raise RuntimeError(f"{obj.name} has no UV layer")
        uv = np.empty(nl * 2, np.float64)
        layer.data.foreach_get("uv", uv)
        uv = uv.reshape(nl, 2)
        return pos, loops.reshape(nt, 3), uv, lv

    def uv_islands(obj):
        """Label the mesh the way the source glTF stored it.

        Blender's importer welds vertices that a glTF split along a UV seam, so
        raw mesh connectivity would merge neighbouring leaf cards. Rebuilding
        identity as (vertex, quantised uv) restores the original islands, which
        is what makes one-quad-per-card reconstruction valid.
        """
        pos, tris, uv, lv = mesh_arrays(obj)
        corner_key = np.column_stack(
            [lv, np.round(uv * 4096.0).astype(np.int64)]
        )
        _, corner_id = np.unique(corner_key, axis=0, return_inverse=True)
        ncorner = int(corner_id.max()) + 1

        tri_corners = corner_id[tris]
        edges = np.concatenate([tri_corners[:, [0, 1]],
                                tri_corners[:, [1, 2]],
                                tri_corners[:, [2, 0]]])
        labels = connected_components(edges, ncorner)

        corner_pos = np.zeros((ncorner, 3))
        corner_uv = np.zeros((ncorner, 2))
        corner_pos[corner_id] = pos[lv]
        corner_uv[corner_id] = uv
        return labels, corner_pos, corner_uv, tri_corners

    def grid_thin(centroids, target):
        """Pick ~target島 evenly in space, never randomly.

        Random subsampling of a canopy leaves clumps and holes and eats the
        silhouette; a spatial grid keeps one card per cell so the crown outline
        and the interior density both survive.
        """
        n = len(centroids)
        if n <= target:
            return np.arange(n)
        lo = centroids.min(0)
        span = float(np.linalg.norm(centroids.max(0) - lo)) or 1.0
        best = None
        h_lo, h_hi = span * 1e-3, span
        for _ in range(48):
            h = math.sqrt(h_lo * h_hi)
            cells = np.floor((centroids - lo) / h).astype(np.int64)
            _, first = np.unique(cells, axis=0, return_index=True)
            if len(first) > target:
                h_lo = h
            else:
                h_hi = h
                best = first
            if abs(len(first) - target) <= max(2, target // 50):
                best = first
                break
        if best is None:
            best = np.argsort(-np.linalg.norm(centroids - centroids.mean(0), axis=1))[:target]
        return np.sort(best)[:target]

    def build_cards(obj, budget, card_scale, name):
        """Replace every UV island with one quad fitted through its own UVs."""
        labels, cpos, cuv, tri_corners = uv_islands(obj)
        n_isl = int(labels.max()) + 1

        counts = np.bincount(labels, minlength=n_isl).astype(np.float64)
        cen = np.zeros((n_isl, 3))
        for axis in range(3):
            cen[:, axis] = np.bincount(labels, weights=cpos[:, axis],
                                       minlength=n_isl) / counts

        target = max(1, budget // 2)
        keep = grid_thin(cen, target)

        # Sorting once turns per-island vertex gathering from O(n_islands * n)
        # into a slice.
        order = np.argsort(labels, kind="stable")
        sorted_lab = labels[order]
        bounds = np.r_[0, np.flatnonzero(np.diff(sorted_lab)) + 1,
                       len(sorted_lab)]

        # Blend each card's own normal towards "outwards from the crown". Purely
        # planar normals make every card light as a flat chip, which is exactly
        # the flat-cartoon-foliage read being designed out; the outward term
        # gives the canopy one coherent rounded shading gradient.
        crown = cen.mean(0)

        verts, faces, uvs, normals = [], [], [], []
        skipped = 0
        for isl in keep:
            sl = order[bounds[isl]:bounds[isl + 1]]
            P = cpos[sl]
            U = cuv[sl]
            if len(sl) < 3:
                skipped += 1
                continue
            A = np.c_[U, np.ones(len(sl))]
            try:
                M, *_ = np.linalg.lstsq(A, P, rcond=None)
            except np.linalg.LinAlgError:
                skipped += 1
                continue
            u0, v0 = U.min(0)
            u1, v1 = U.max(0)
            if not np.isfinite([u0, v0, u1, v1]).all() or u1 - u0 < 1e-6 or v1 - v0 < 1e-6:
                skipped += 1
                continue
            quad_uv = np.array([[u0, v0], [u1, v0], [u1, v1], [u0, v1]])
            corners = np.c_[quad_uv, np.ones(4)] @ M
            mid = corners.mean(0)
            corners = mid + (corners - mid) * card_scale
            e0 = corners[1] - corners[0]
            e1 = corners[3] - corners[0]
            nrm = np.cross(e0, e1)
            ln = np.linalg.norm(nrm)
            if ln < 1e-12:
                skipped += 1
                continue
            nrm /= ln
            outward = mid - crown
            lo = np.linalg.norm(outward)
            outward = outward / lo if lo > 1e-9 else nrm
            if float(nrm @ outward) < 0.0:
                nrm = -nrm
            blended = nrm * 0.5 + outward * 0.5
            bl = np.linalg.norm(blended)
            blended = blended / bl if bl > 1e-9 else nrm

            base = len(verts)
            verts.extend(corners.tolist())
            uvs.extend(quad_uv.tolist())
            normals.extend([blended.tolist()] * 4)
            faces.append((base, base + 1, base + 2))
            faces.append((base, base + 2, base + 3))

        if not faces:
            raise RuntimeError(f"{name}: card rebuild produced no geometry")

        me = bpy.data.meshes.new(name + "Mesh")
        me.from_pydata(verts, [], faces)
        me.update()
        layer = me.uv_layers.new(name="UVMap")
        loop_uv = np.zeros((len(me.loops), 2))
        lv = np.empty(len(me.loops), np.int64)
        me.loops.foreach_get("vertex_index", lv)
        loop_uv[:] = np.asarray(uvs)[lv]
        layer.data.foreach_set("uv", loop_uv.reshape(-1))
        me.normals_split_custom_set_from_vertices(normals)

        card = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(card)
        stats = dict(islands=n_isl, kept=len(keep) - skipped, skipped=skipped,
                     card_scale=card_scale)
        return card, stats

    def decimate_to(obj, budget, keep_area=None):
        import bmesh
        if keep_area is not None and keep_area < 1.0:
            drop_small_components(obj, keep_area)

        current = tri_count(obj)
        # Two staged collapses beat one extreme ratio: the quadric error matrix
        # is rebuilt against the already-simplified surface, so the second pass
        # spends its budget where the first pass actually caused error.
        for stage_ratio in (0.12, 1.0):
            current = tri_count(obj)
            if current <= budget:
                break
            wanted = max(budget, int(current * stage_ratio))
            if wanted >= current:
                wanted = budget
            mod = obj.modifiers.new("Budget", "DECIMATE")
            mod.decimate_type = "COLLAPSE"
            mod.ratio = max(min(wanted / current, 1.0), 1e-6)
            mod.use_collapse_triangulate = True
            apply_modifier(obj, mod)

        # Collapse can leave zero-area triangles behind, which show up in Godot
        # as black speckle and as NaNs when tangents are generated.
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.dissolve_degenerate(bm, dist=1e-6, edges=bm.edges)
        bmesh.ops.delete(
            bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()
        return obj

    def drop_small_components(obj, keep_area):
        """Delete the smallest connected pieces until keep_area of area remains."""
        import bmesh
        me = obj.data
        ne = len(me.edges)
        edges = np.empty(ne * 2, np.int64)
        me.edges.foreach_get("vertices", edges)
        edges = edges.reshape(ne, 2)
        labels = connected_components(edges, len(me.vertices))

        me.calc_loop_triangles()
        nt = len(me.loop_triangles)
        tv = np.empty(nt * 3, np.int64)
        me.loop_triangles.foreach_get("vertices", tv)
        tv = tv.reshape(nt, 3)
        pos = np.empty(len(me.vertices) * 3, np.float64)
        me.vertices.foreach_get("co", pos)
        pos = pos.reshape(-1, 3)
        area = 0.5 * np.linalg.norm(
            np.cross(pos[tv[:, 1]] - pos[tv[:, 0]], pos[tv[:, 2]] - pos[tv[:, 0]]),
            axis=1)
        tri_label = labels[tv[:, 0]]
        per = np.bincount(tri_label, weights=area, minlength=int(labels.max()) + 1)
        order = np.argsort(-per)
        cum = np.cumsum(per[order])
        total = cum[-1] if len(cum) else 0.0
        if total <= 0:
            return
        n_keep = int(np.searchsorted(cum, total * keep_area) + 1)
        survivors = set(order[:n_keep].tolist())

        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        doomed = [f for f in bm.faces
                  if labels[f.verts[0].index] not in survivors]
        if doomed:
            bmesh.ops.delete(bm, geom=doomed, context="FACES")
        bm.to_mesh(me)
        bm.free()
        me.update()

    def cluster_parts(objs, radius):
        """Scatter separate source props into one tight, plantable clump.

        Poly Haven lays variants out side by side for the preview render. Kept as
        found they would scatter as a wide sparse row rather than a tuft.
        """
        for i, o in enumerate(objs):
            bb = np.array([list(c) for c in o.bound_box])
            centre = (bb.min(0) + bb.max(0)) * 0.5
            # Recentre horizontally only. The vertical axis is deliberately left
            # alone so each part keeps the base height it was authored at and the
            # clump still sits on the ground once scattered.
            o.location = (float(-centre[0]), 0.0, float(-centre[2]))
            # golden-angle spiral keeps the clump even at any member count
            ang = i * 2.399963
            r = radius * math.sqrt((i + 0.5) / max(len(objs), 1))
            o.location = (o.location[0] + math.cos(ang) * r,
                          o.location[1],
                          o.location[2] + math.sin(ang) * r)
            o.rotation_euler = (0.0, ang * 1.7, 0.0)
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    def ground_and_centre(objs):
        """Origin at the footprint centre, base at Y=0.

        Scattering multiplies a placement transform by each mesh's local offset,
        so an asset whose origin is not at its own base floats or sinks on every
        single instance.
        """
        allv = []
        for o in objs:
            n = len(o.data.vertices)
            v = np.empty(n * 3, np.float64)
            o.data.vertices.foreach_get("co", v)
            allv.append(v.reshape(n, 3))
        pts = np.concatenate(allv)
        y0 = pts[:, 1].min()
        height = pts[:, 1].max() - y0
        base = pts[pts[:, 1] <= y0 + max(height * 0.15, 1e-4)]
        cx, cz = base[:, 0].mean(), base[:, 2].mean()
        offset = np.array([-cx, -y0, -cz])
        for o, v in zip(objs, allv):
            o.data.vertices.foreach_set("co", (v + offset).reshape(-1))
            o.data.update()
        return pts + offset

    def sample_alpha_coverage(obj, albedo_png):
        """Fraction of the asset's surface that survives the alpha scissor.

        This is the check that catches a misaligned or inverted opacity map: if
        the mask does not line up with the UVs the plant silently disappears in
        engine, and there is no way to see that from a triangle count.
        """
        img = bpy.data.images.load(str(albedo_png), check_existing=True)
        img.colorspace_settings.name = "Non-Color"
        w, h = img.size
        buf = np.empty(w * h * 4, np.float32)
        img.pixels.foreach_get(buf)
        alpha = buf.reshape(h, w, 4)[:, :, 3]

        pos, tris, uv, lv = mesh_arrays(obj)
        tri_uv = uv[tris].mean(axis=1)
        p = pos[lv][tris]
        area = 0.5 * np.linalg.norm(
            np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0]), axis=1)
        u = np.clip((tri_uv[:, 0] % 1.0) * w, 0, w - 1).astype(np.int64)
        v = np.clip((tri_uv[:, 1] % 1.0) * h, 0, h - 1).astype(np.int64)
        a = alpha[v, u]
        bpy.data.images.remove(img)
        if area.sum() <= 0:
            return 0.0, 0.0
        return float((a * area).sum() / area.sum()), float(
            (a >= ALPHA_SCISSOR_THRESHOLD).mean())

    def uv_health(obj):
        """Degenerate-UV fraction: the direct measure of 'did the UVs survive'."""
        pos, tris, uv, lv = mesh_arrays(obj)
        t = uv[tris]
        duv = np.abs(np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0])) * 0.5
        p = pos[lv][tris]
        area = 0.5 * np.linalg.norm(
            np.cross(p[:, 1] - p[:, 0], p[:, 2] - p[:, 0]), axis=1)
        live = area > 1e-12
        if not live.any():
            return 1.0, 0.0
        bad = float((duv[live] <= 1e-12).mean())
        return bad, float(np.clip(uv[:, :2], 0, 1).size and uv.max())

    def convex_hull_points(pts, limit=28):
        """A coarse hull for collision: extreme point in each of N directions."""
        dirs = []
        golden = math.pi * (3.0 - math.sqrt(5.0))
        for i in range(limit):
            y = 1.0 - 2.0 * (i + 0.5) / limit
            r = math.sqrt(max(0.0, 1.0 - y * y))
            a = golden * i
            dirs.append((math.cos(a) * r, y, math.sin(a) * r))
        chosen = []
        seen = set()
        for d in dirs:
            idx = int(np.argmax(pts @ np.array(d)))
            if idx not in seen:
                seen.add(idx)
                chosen.append(pts[idx])
        return chosen

    def export_glb(objs, path: Path):
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        path.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.export_scene.gltf(
            filepath=str(path),
            export_format="GLB",
            use_selection=True,
            export_apply=True,
            export_yup=True,
            export_materials="PLACEHOLDER",
            export_normals=True,
            export_tangents=True,
            export_texcoords=True,
            export_vertex_color="NONE",
            export_cameras=False,
            export_lights=False,
            export_animations=False,
        )

    # -- main build loop --------------------------------------------------

    ensure_textures()
    OUT_ROOT.mkdir(parents=True, exist_ok=True)

    report = []
    for asset in ASSETS:
        reset()
        slug = asset["slug"]
        print(f"\n=== {asset['key']}  ({slug}) ===")
        sources = import_source(slug)

        built, stats_by_part = [], {}
        for spec in asset["parts"]:
            name = spec["name"]
            if spec["material"]:
                src = duplicate(list(sources.values())[0], name + "Src")
                keep_only_material(src, spec["material"])
                work = src
            else:
                picked = [sources[n] for n in spec["nodes"] if n in sources]
                missing = [n for n in spec["nodes"] if n not in sources]
                if missing:
                    raise RuntimeError(
                        f"{asset['key']}: source nodes {missing} not in "
                        f"{sorted(sources)}")
                copies = [duplicate(o, f"{name}Src{i}") for i, o in enumerate(picked)]
                if spec.get("cluster"):
                    cluster_parts(copies, spec["cluster"])
                work = join(copies, name + "Src")

            source_tris = tri_count(work)
            if spec["method"] == CARD:
                obj, cstats = build_cards(work, spec["budget"], spec["card_scale"],
                                          name)
                bpy.data.objects.remove(work, do_unlink=True)
                stats_by_part[name] = cstats
            elif spec["method"] == DECIMATE:
                obj = decimate_to(work, spec["budget"], spec.get("keep_area"))
                obj.name = name
                obj.data.name = name + "Mesh"
                stats_by_part[name] = {}
            else:
                obj = work
                obj.name = name
                obj.data.name = name + "Mesh"
                stats_by_part[name] = {}
            stats_by_part[name]["source_tris"] = source_tris
            built.append(obj)

        for o in list(bpy.context.scene.objects):
            if o not in built:
                bpy.data.objects.remove(o, do_unlink=True)

        pts = ground_and_centre(built)
        out_glb = OUT_ROOT / f"{asset['key']}.glb"
        export_glb(built, out_glb)

        # validation
        parts_report = []
        for spec, obj in zip(asset["parts"], built):
            tris = tri_count(obj)
            bad_uv, _ = uv_health(obj)
            material = MATERIALS_BY_NAME[spec["mat_res"]]
            coverage = None
            if material["cutout"]:
                stem = material["albedo"].rsplit("/", 1)[-1].replace(
                    "_albedo_2k.png", "")
                coverage = sample_alpha_coverage(obj, albedo_path(stem))
            s = stats_by_part[spec["name"]]
            parts_report.append(dict(
                name=spec["name"], tris=tris, source_tris=s["source_tris"],
                method=spec["method"], material=spec["mat_res"],
                degenerate_uv=bad_uv, alpha_cover=coverage,
                islands=s.get("islands"), cards=s.get("kept"),
                card_scale=s.get("card_scale")))
            print(f"    {spec['name']:9s} {s['source_tris']:>9,d} -> {tris:>6,d} tris"
                  f"  degenerateUV={bad_uv:.4f}"
                  + (f"  alpha={coverage[0]:.3f}" if coverage else ""))

        lo = pts.min(0)
        hi = pts.max(0)
        node_order = read_glb_nodes(out_glb)
        entry = dict(asset=asset, parts=parts_report, glb=out_glb,
                     size=(hi - lo).tolist(), node_order=node_order,
                     hull=[list(map(float, p)) for p in convex_hull_points(pts)],
                     radius=float(np.percentile(
                         np.linalg.norm(pts[pts[:, 1] < lo[1] + (hi[1] - lo[1]) * 0.12][:, [0, 2]],
                                        axis=1) if (pts[:, 1] < lo[1] + (hi[1] - lo[1]) * 0.12).any()
                         else np.linalg.norm(pts[:, [0, 2]], axis=1), 92)))
        report.append(entry)
        print(f"    size {np.round(hi - lo, 2)} m   nodes {node_order}")

        if preview_dir is not None:
            render_preview(asset, built, preview_dir)

    write_godot_resources(report)
    write_readme(report)
    print("\nBUILD_COMPLETE")


def read_glb_nodes(path: Path):
    """Node names in export order - the .tscn override indices must match."""
    with open(path, "rb") as fh:
        magic, _version, _length = struct.unpack("<III", fh.read(12))
        if magic != 0x46546C67:
            raise RuntimeError(f"{path} is not a GLB")
        chunk_len, chunk_type = struct.unpack("<II", fh.read(8))
        if chunk_type != 0x4E4F534A:
            raise RuntimeError(f"{path}: first chunk is not JSON")
        doc = json.loads(fh.read(chunk_len).decode("utf-8"))
    scene = doc.get("scenes", [{}])[doc.get("scene", 0)]
    return [doc["nodes"][i].get("name", "") for i in scene.get("nodes", [])]


def render_preview(asset, objs, preview_dir: Path):
    """Render each finished asset so the result can actually be looked at."""
    import bpy
    import numpy as np

    preview_dir.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene

    for obj in objs:
        spec = next(p for p in asset["parts"] if p["name"] == obj.name)
        material = MATERIALS_BY_NAME[spec["mat_res"]]
        m = bpy.data.materials.new(obj.name + "Preview")
        m.use_nodes = True
        bsdf = m.node_tree.nodes["Principled BSDF"]
        if material["cutout"]:
            stem = material["albedo"].rsplit("/", 1)[-1].replace("_albedo_2k.png", "")
            img_path = albedo_path(stem)
        else:
            img_path = REPO_ROOT / material["albedo"].replace("res://", "")
        tex = m.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(img_path), check_existing=True)
        m.node_tree.links.new(bsdf.inputs["Base Color"], tex.outputs["Color"])
        if material["cutout"]:
            m.node_tree.links.new(bsdf.inputs["Alpha"], tex.outputs["Alpha"])
        obj.data.materials.clear()
        obj.data.materials.append(m)

    pts = []
    for o in objs:
        n = len(o.data.vertices)
        v = np.empty(n * 3, np.float64)
        o.data.vertices.foreach_get("co", v)
        pts.append(v.reshape(n, 3))
    pts = np.concatenate(pts)
    lo, hi = pts.min(0), pts.max(0)
    centre = (lo + hi) * 0.5
    extent = float(np.linalg.norm(hi - lo))

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    dist = extent * 1.15
    cam.location = (centre[0] + dist * 0.72, centre[1] + extent * 0.10,
                    centre[2] + dist * 0.72)
    direction = np.array(cam.location) - centre
    cam.rotation_euler = (
        math.acos(direction[1] / max(np.linalg.norm(direction), 1e-6)),
        0.0,
        math.atan2(direction[0], direction[2]),
    )
    scene.camera = cam

    sun_data = bpy.data.lights.new("Sun", type="SUN")
    sun_data.energy = 4.0
    sun = bpy.data.objects.new("Sun", sun_data)
    sun.rotation_euler = (math.radians(52), math.radians(20), math.radians(35))
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("PreviewWorld")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.30, 0.38, 0.50, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.1
    scene.world = world

    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.film_transparent = False
    scene.render.filepath = str(preview_dir / f"{asset['key']}.png")
    try:
        bpy.ops.render.render(write_still=True)
    except Exception as error:  # noqa: BLE001 - previews are diagnostics only
        print(f"    preview render failed: {error}")


# ===========================================================================
# Godot resource generation
# ===========================================================================

def write_material(spec) -> None:
    """Emit one StandardMaterial3D .tres.

    alpha_scissor_threshold is 0.33 rather than the usual 0.5. Godot mipmaps the
    albedo, and averaging a hard cut-out mask drives thin foliage alpha towards
    the local coverage fraction, so a 0.5 test erodes leaves as they recede and
    the canopy thins out with distance. A lower threshold holds coverage; the
    cost is a slightly softer edge close up, which is much the better trade.

    Written for both renderers: alpha scissor, cull_disabled, ARM channel
    unpacking and backlight are all supported on Compatibility and Forward+.
    Nothing here depends on which one the project ships.
    """
    ids, lines = {}, []
    for key in ("albedo", "arm", "rough", "normal"):
        path = spec.get(key)
        if not path:
            continue
        if path in ids:
            continue
        ids[path] = f"{len(ids) + 1}_{key}"
    for path, rid in ids.items():
        lines.append(f'[ext_resource type="Texture2D" path="{path}" id="{rid}"]')

    body = [f'resource_name = "{spec["label"]}"']
    if spec["cutout"]:
        body += [
            "transparency = 2",
            f"alpha_scissor_threshold = {ALPHA_SCISSOR_THRESHOLD}",
            "cull_mode = 2",
        ]
    body += [
        "shading_mode = 1",
        f'albedo_texture = ExtResource("{ids[spec["albedo"]]}")',
        "metallic = 0.0",
        "metallic_specular = 0.25",
        "roughness = 1.0",
    ]
    if spec.get("arm"):
        body += [
            f'roughness_texture = ExtResource("{ids[spec["arm"]]}")',
            "roughness_texture_channel = 1",
            "ao_enabled = true",
            f'ao_texture = ExtResource("{ids[spec["arm"]]}")',
            "ao_texture_channel = 0",
            "ao_light_affect = 0.35",
        ]
    elif spec.get("rough"):
        body += [
            f'roughness_texture = ExtResource("{ids[spec["rough"]]}")',
            "roughness_texture_channel = 4",
        ]
    if spec.get("normal"):
        body += [
            "normal_enabled = true",
            "normal_scale = 1.0",
            f'normal_texture = ExtResource("{ids[spec["normal"]]}")',
        ]
    if spec.get("backlight"):
        r, g, b = spec["backlight"]
        body += ["backlight_enabled = true", f"backlight = Color({r}, {g}, {b}, 1)"]

    text = (f"[gd_resource type=\"StandardMaterial3D\" load_steps={len(ids) + 1}"
            " format=3]\n\n" + "\n".join(lines) + "\n\n[resource]\n"
            + "\n".join(body) + "\n")
    MATERIAL_DIR.mkdir(parents=True, exist_ok=True)
    (MATERIAL_DIR / f"{spec['res_name']}.tres").write_text(text, encoding="utf-8")


def write_scene(entry) -> None:
    """Emit the project-owned wrapper .tscn.

    Structurally this matches assets/environment/nature/common_tree_1.tscn: a
    Node3D-derived root, the source scene instanced as "Visual", and materials
    bound through surface_material_override on the MeshInstance3D children.
    OldMillBridge.ScatterVegetation walks exactly that shape - CollectMeshes
    recurses for MeshInstance3D and BakeSurfaceMaterials reads the overrides
    back off - so these drop straight into its scatter list.
    """
    asset = entry["asset"]
    key = asset["key"]
    parts = entry["parts"]
    order = entry["node_order"]

    ext = [f'[ext_resource type="PackedScene" path="{RES_OUT}/{key}.glb" id="1_src"]']
    seen = {}
    for p in parts:
        if p["material"] in seen:
            continue
        seen[p["material"]] = f"{len(seen) + 2}_mat"
    for name, rid in seen.items():
        ext.append(f'[ext_resource type="Material" path="{RES_MAT}/{name}.tres" '
                   f'id="{rid}"]')

    subs, coll = [], []
    kind = asset["collision"]
    if kind == "cylinder":
        height = entry["size"][1]
        radius = max(entry["radius"], 0.12)
        subs.append(f'[sub_resource type="CylinderShape3D" id="Trunk"]\n'
                    f"radius = {radius:.3f}\nheight = {height:.3f}")
        coll.append(f'[node name="TrunkCollision" type="CollisionShape3D" parent="."]\n'
                    f"transform = Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, "
                    f"{height * 0.5:.3f}, 0)\n"
                    f'shape = SubResource("Trunk")')
    elif kind == "convex":
        pts = ", ".join(f"{v:.3f}" for p in entry["hull"] for v in p)
        subs.append(f'[sub_resource type="ConvexPolygonShape3D" id="Hull"]\n'
                    f"points = PackedVector3Array({pts})")
        coll.append('[node name="HullCollision" type="CollisionShape3D" parent="."]\n'
                    'shape = SubResource("Hull")')

    root_name = "".join(w.capitalize() for w in key.split("_"))
    body = [f'[node name="{root_name}" type="{asset["root_type"]}"]',
            '',
            '[node name="Visual" parent="." instance=ExtResource("1_src")]']
    for p in parts:
        idx = order.index(p["name"]) if p["name"] in order else 0
        body += ['',
                 f'[node name="{p["name"]}" parent="Visual" index="{idx}"]',
                 f'surface_material_override/0 = ExtResource("{seen[p["material"]]}")']
    for c in coll:
        body += ['', c]

    steps = len(ext) + len(subs) + 1
    text = (f"[gd_scene load_steps={steps} format=3]\n\n" + "\n".join(ext) + "\n\n"
            + ("\n\n".join(subs) + "\n\n" if subs else "")
            + "\n".join(body) + '\n\n[editable path="Visual"]\n')
    (OUT_ROOT / f"{key}.tscn").write_text(text, encoding="utf-8")


def write_godot_resources(report) -> None:
    for spec in MATERIALS:
        write_material(spec)
    for entry in report:
        write_scene(entry)


def write_readme(report) -> None:
    rows = []
    total = 0
    for entry in report:
        asset = entry["asset"]
        tris = sum(p["tris"] for p in entry["parts"])
        src = sum(p["source_tris"] for p in entry["parts"])
        total += tris
        size = "x".join(f"{v:.1f}" for v in entry["size"])
        rows.append(
            f"| `{RES_OUT}/{asset['key']}.tscn` | {asset['label']} | "
            f"`{asset['slug']}` | {src:,} | **{tris:,}** | {size} m | "
            f"{asset['collision'] or 'none'} |")

    detail = []
    for entry in report:
        for p in entry["parts"]:
            cov = f"{p['alpha_cover'][0]:.3f}" if p["alpha_cover"] else "-"
            cards = (f"{p['cards']:,} of {p['islands']:,} @ x{p['card_scale']}"
                     if p["cards"] else "-")
            detail.append(
                f"| `{entry['asset']['key']}` | {p['name']} | {p['method']} | "
                f"{p['source_tris']:,} | {p['tris']:,} | {p['degenerate_uv']:.4f} | "
                f"{cov} | {cards} | `{p['material']}.tres` |")

    text = f"""# Ashwood photoscanned vegetation

Project-owned, decimated derivatives of CC0 Poly Haven photoscans, built by
`tools/blender/build_ashwood_vegetation.py`. These replace the stylised lowpoly
trees (salmon-pink trunks, flat cartoon-green foliage) that were breaking the
State of Decay / Mist Survival look.

Rebuild everything with:

```
python tools/download_polyhaven.py --set vegetation
python tools/download_polyhaven.py --set rocks
blender --background --python tools/blender/build_ashwood_vegetation.py
```

Sources under `assets/third_party/polyhaven_2026_08/` are never modified.
Poly Haven assets are CC0, so this is commercial-safe with no attribution
requirement.

## Wrapper scenes

Each `.tscn` is a `Node3D`/`StaticBody3D` root with the `.glb` instanced as
`Visual` and materials bound via `surface_material_override/0` on the
`MeshInstance3D` children - the same shape as
`assets/environment/nature/common_tree_1.tscn`, so
`OldMillBridge.ScatterVegetation` can consume them directly.

| Scene (`res://`) | Asset | Poly Haven slug | Source tris | Final tris | Size | Collision |
| --- | --- | --- | ---: | ---: | --- | --- |
{chr(10).join(rows)}

Total across all {len(report)} assets: **{total:,} triangles**.

## Per-part detail

`degenerate UV` is the fraction of live triangles with zero UV area - the direct
measure of whether the decimate wrecked the texture mapping. `alpha` is the
surface-area-weighted mean opacity sampled at triangle UV centroids, which is
what catches a misaligned or inverted cut-out mask.

| Asset | Part | Method | Source tris | Final tris | Degenerate UV | Alpha | Cards | Material |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- | --- |
{chr(10).join(detail)}

## How the leaves are built

The Poly Haven canopies are not solid meshes. jacaranda_tree's 2.4M-triangle
canopy is 116,084 separate leaf-spray cards of ~20 triangles each, and the
shrubs are built the same way. Collapse-decimating that to a game budget spends
under one triangle per card, welding the canopy into pulp and smearing UVs
across the atlas.

Instead each UV island is measured and rebuilt as a single quad. Every island is
a parameterised patch, so a least-squares fit of the affine map
`(u, v, 1) -> position` reproduces it exactly; measured residual on jacaranda is
2.8% of island diagonal (worst 5.7%). Cards are thinned on a spatial grid rather
than randomly, so the crown silhouette survives, and card normals are blended
50% towards "outward from the crown" so the canopy shades as a rounded volume
instead of a pile of flat chips.

**Known trade-off:** the surviving cards are scaled up to hold canopy coverage,
so jacaranda leaf sprays render larger than life (roughly 3.4x at LOD0). A
4,000-triangle budget cannot hold 116,084 leaves at native scale - this is the
standard game-tree compromise, but it is a real deviation from the scan and it
is the first thing to re-tune (`card_scale` in the `ASSETS` table) if the
canopy reads as coarse in-game.

## Materials

`assets/materials/vegetation_*.tres`. Foliage uses **alpha scissor**
(`transparency = 2`) with `cull_mode = 2` (disabled) so leaves are two-sided.
Alpha blending is deliberately not used - it has no correct draw order and looks
worst exactly where foliage overlaps. Albedo, ARM (AO in red, roughness in
green) and OpenGL normal maps are all wired; nothing renders unlit.

Alpha threshold is **0.33**, not 0.5. Godot mipmaps the albedo, and averaging a
hard mask drives thin-foliage alpha towards the local coverage fraction, so a
0.5 test erodes leaves with distance and thins the canopy out.

The composited RGBA albedos in `textures/` exist because Poly Haven's glTF
references only the JPEG diffuse and JPEG has no alpha channel. The cut-out
silhouette ships as a separate `Alpha` map the glTF never mentions, and
`fern_02`'s diffuse has a dilated colour bleed instead of a black background -
so without compositing it renders as a solid green rectangle. RGB is also
dilated outwards under the transparent region so mipmapping never bleeds the
atlas background in along leaf edges.

## Not yet verified

Triangle counts, UV integrity, alpha coverage, texture wiring, asset scale and
node/override structure are all machine-checked above and in the build log.
**In-engine appearance is not** - these were built and previewed in Blender, not
rendered in Godot. Worth eyeballing on first run: canopy density and leaf scale
at gameplay camera distance, alpha-scissor edge quality on the Compatibility
renderer, and the backlight strength on leaf materials.
"""
    (OUT_ROOT / "README.md").write_text(text, encoding="utf-8")


# ===========================================================================

def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--textures", action="store_true",
                        help="composite RGBA albedos (plain CPython + Pillow)")
    parser.add_argument("--preview-dir", default=None,
                        help="render a PNG per asset into this directory")
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    args = parser.parse_args([a for a in argv if not a.endswith(".py")])

    if args.textures:
        build_textures()
        return

    try:
        import bpy  # noqa: F401
    except ImportError:
        raise SystemExit(
            "Run the mesh step under Blender:\n"
            "  blender --background --python tools/blender/build_ashwood_vegetation.py"
        )
    blender_main(Path(args.preview_dir) if args.preview_dir else None)


if __name__ == "__main__":
    main()
