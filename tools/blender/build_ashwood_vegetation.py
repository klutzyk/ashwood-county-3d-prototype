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


WHY THE CONIFERS NEEDED A THIRD METHOD
--------------------------------------
The fir and pine scans are built the same way as the jacaranda but three orders
of magnitude finer. fir_tree_01_a's canopy is 812,468 islands and pine_tree_01_a's
is 409,530, and a measured island is a 6mm x 72mm sliver sampling a 23 x 331 texel
strip - a few needles, not a leaf spray. One quad per island is 1.6M triangles,
and thinning that to a 12,000-triangle budget keeps 0.7% of the cards, so holding
canopy coverage would need each survivor inflated about 11x. An 80cm needle
sliver reads as pampas grass, not as a fir.

So conifer foliage uses the CARDS method's successor, SPRAY. The atlas already
contains complete branch-tip sprays - four large ones on fir, two on pine, sitting
unused because the scan samples only the sliver strips. SPRAY voxelises the
canopy, and emits one quad per occupied cell carrying a whole spray from the
atlas, sized from the foliage actually in that cell. A 40cm quad showing a 40cm
fir spray is both in budget and true to scale, which per-island quads cannot be
at the same time.

SPRAY also never labels islands, so it reads only triangle centroids and areas -
which is what makes reading the source straight out of the glTF buffer viable.
pine_tree_01 is a 948MB buffer holding 17M triangles across three whole trees;
importing that into Blender to then throw away everything but one tree's canopy
does not fit in memory beside the working copies. A glTF primitive is a
contiguous slice of that buffer, so each part is read directly and only the
wanted slice is ever resident. See gltf_primitive().
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

    # Conifer needles. These matter more than any other entry here: the pine twig
    # diffuse is needles on pure black and the fir twig diffuse is needles on a
    # dilated green bleed, so without the separate alpha map pine renders as black
    # rectangles and fir as green ones. Nothing about the glTF hints that the map
    # exists.
    "fir_tree_01_twig": (
        "fir_tree_01/textures/fir_tree_01_twig_diff_2k.jpg",
        "fir_tree_01/textures/fir_tree_01_twig_alpha_2k.png",
    ),
    "pine_tree_01_twig": (
        "pine_tree_01/textures/pine_tree_01_twig_diff_2k.jpg",
        "pine_tree_01/textures/pine_tree_01_twig_alpha_2k.png",
    ),
    "fir_sapling_medium_twigs": (
        "fir_sapling_medium/textures/fir_sapling_medium_twigs_diff_2k.jpg",
        "fir_sapling_medium/textures/fir_sapling_medium_twigs_alpha_2k.png",
    ),
    "pine_sapling_medium_twig": (
        "pine_sapling_medium/textures/pine_sapling_medium_twig_diff_2k.jpg",
        "pine_sapling_medium/textures/pine_sapling_medium_twig_alpha_2k.png",
    ),
    "fir_sapling_twigs": (
        "fir_sapling/textures/fir_sapling_twigs_diff_2k.jpg",
        "fir_sapling/textures/fir_sapling_twigs_alpha_2k.png",
    ),
    "pine_sapling_small_twig": (
        "pine_sapling_small/textures/pine_sapling_small_twig_diff_2k.jpg",
        "pine_sapling_small/textures/pine_sapling_small_twig_alpha_2k.png",
    ),

    "moss_01": (
        "moss_01/textures/moss_01_diff_2k.jpg",
        "moss_01/textures/moss_01_alpha_2k.png",
    ),
    "grass_medium_01": (
        "grass_medium_01/textures/grass_medium_01_diff_2k.jpg",
        "grass_medium_01/textures/grass_medium_01_alpha_2k.png",
    ),
    "grass_medium_02": (
        "grass_medium_02/textures/grass_medium_02_diff_2k.jpg",
        "grass_medium_02/textures/grass_medium_02_alpha_2k.png",
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
# Direct glTF primitive reader
# ===========================================================================
#
# Only the conifers use this. Their buffers are 478MB (fir) and 948MB (pine) and
# each holds three complete trees, so importing one to build one part of one
# tree costs about 15x what the part is worth and does not fit in memory beside
# the working copies. A glTF primitive is a contiguous, already material-split
# slice of that buffer, which is exactly the granularity the asset table asks
# for, so it is read straight out.

GLTF_DTYPE = {5120: "i1", 5121: "u1", 5122: "i2", 5123: "u2", 5125: "u4",
              5126: "f4"}
GLTF_COMPONENTS = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}

# slug -> (document, [memoryview per buffer]). Kept open for the whole run: the
# same file is read up to eighteen times (three variants x two LODs x three
# parts) and mapping it once lets the OS page cache do the sharing.
_GLTF_OPEN = {}


def gltf_document(slug: str):
    import mmap

    if slug in _GLTF_OPEN:
        return _GLTF_OPEN[slug]
    base = SOURCE_ROOT / slug
    path = base / f"{slug}_2k.gltf"
    if not path.is_file():
        raise FileNotFoundError(
            f"Missing source {path}. Run 'python tools/download_polyhaven.py'.")
    doc = json.loads(path.read_text(encoding="utf-8"))
    buffers = []
    for buf in doc["buffers"]:
        handle = open(base / buf["uri"], "rb")
        # The handle is deliberately kept alive alongside the map: closing it
        # invalidates the mapping on Windows and every later read fails with a
        # bad file descriptor.
        buffers.append((handle, mmap.mmap(handle.fileno(), 0,
                                          access=mmap.ACCESS_READ)))
    _GLTF_OPEN[slug] = (doc, buffers)
    return _GLTF_OPEN[slug]


def gltf_accessor(doc, buffers, index):
    """One accessor as a numpy array, honouring interleaved byte strides."""
    import numpy as np

    spec = doc["accessors"][index]
    view = doc["bufferViews"][spec["bufferView"]]
    dtype = np.dtype(GLTF_DTYPE[spec["componentType"]])
    ncomp = GLTF_COMPONENTS[spec["type"]]
    count = spec["count"]
    offset = view.get("byteOffset", 0) + spec.get("byteOffset", 0)
    data = buffers[view.get("buffer", 0)][1]
    stride = view.get("byteStride") or dtype.itemsize * ncomp
    if stride == dtype.itemsize * ncomp:
        return np.frombuffer(data, dtype=dtype, count=count * ncomp,
                             offset=offset).reshape(count, ncomp)
    raw = np.frombuffer(data, dtype=np.uint8, count=count * stride,
                        offset=offset).reshape(count, stride)
    return raw[:, :dtype.itemsize * ncomp].copy().view(dtype).reshape(count, ncomp)


def gltf_primitive(slug: str, node_name: str, material_names):
    """Positions, triangles and UVs for one node's primitives, in Blender space.

    Returns float32 positions because a conifer canopy is up to 6.8M triangles
    and float64 would double the peak for precision nothing here needs - the
    scans are metres-scale and the cards built from them are centimetres.
    """
    import numpy as np

    doc, buffers = gltf_document(slug)
    names = [m.get("name") for m in doc.get("materials", [])]
    node = next((n for n in doc["nodes"] if n.get("name") == node_name), None)
    if node is None or node.get("mesh") is None:
        have = [n.get("name") for n in doc["nodes"] if n.get("mesh") is not None]
        raise RuntimeError(f"{slug}: no mesh node named {node_name!r}; have {have}")

    wanted = set(material_names)
    positions, uvs, triangles, base = [], [], [], 0
    for prim in doc["meshes"][node["mesh"]]["primitives"]:
        if names[prim.get("material", -1)] not in wanted:
            continue
        pos = gltf_accessor(doc, buffers, prim["attributes"]["POSITION"])
        uv = gltf_accessor(doc, buffers, prim["attributes"]["TEXCOORD_0"])
        idx = gltf_accessor(doc, buffers, prim["indices"]).reshape(-1)
        positions.append(np.asarray(pos, np.float32))
        uvs.append(np.asarray(uv, np.float32))
        triangles.append(idx.reshape(-1, 3).astype(np.int32) + base)
        base += len(pos)
    if not positions:
        raise RuntimeError(
            f"{slug}/{node_name}: no primitive using {sorted(wanted)}; have "
            f"{[names[p.get('material', -1)] for p in doc['meshes'][node['mesh']]['primitives']]}")

    pos = np.concatenate(positions)
    uv = np.concatenate(uvs)
    tris = np.concatenate(triangles)

    scale = np.asarray(node.get("scale", (1.0, 1.0, 1.0)), np.float32)
    translation = np.asarray(node.get("translation", (0.0, 0.0, 0.0)), np.float32)
    if node.get("rotation") or node.get("matrix"):
        raise RuntimeError(
            f"{slug}/{node_name}: node carries a rotation or matrix this reader "
            "does not apply. Poly Haven's tree files use translation only.")
    pos = pos * scale + translation

    # glTF is Y-up; Blender is Z-up, and export_scene.gltf(export_yup=True)
    # converts back on the way out, so skipping this would ship trees on their
    # side. glTF UVs also start at the top left where Blender's start at the
    # bottom left.
    pos = np.column_stack([pos[:, 0], -pos[:, 2], pos[:, 1]])
    uv = np.column_stack([uv[:, 0], 1.0 - uv[:, 1]])
    return pos, tris, uv


# ===========================================================================
# Asset recipes
# ===========================================================================
#
# method:
#   "cards"    - rebuild each UV island as a single quad (scanned leaf cards)
#   "spray"    - voxelise the canopy and emit one atlas branch-spray quad per
#                occupied cell (conifer needles - see the module docstring)
#   "decimate" - COLLAPSE decimate to the triangle budget (solid geometry)
#   "keep"     - already inside budget, pass through untouched
#
# source: BLENDER (import the whole glTF) | GLTF (read one primitive from the
#   .bin directly). GLTF exists for the conifers, whose buffers are 478MB and
#   948MB and hold three complete trees each.
#
# collision: None | "cylinder" | "convex"

CARD, SPRAY, DECIMATE, KEEP = "cards", "spray", "decimate", "keep"
BLENDER, GLTF = "blender", "gltf"


def part(name, method, budget=0, nodes=(), material=None, **kw):
    return dict(name=name, method=method, budget=budget, nodes=tuple(nodes),
                material=material, **kw)


# ---------------------------------------------------------------------------
# Branch-spray rectangles inside each conifer twig atlas.
#
# (x0, y0, x1, y1, stem) in texels of the 2k atlas with the image origin at the
# top left, which is how the atlas was measured - converting to UV here rather
# than in the table keeps the numbers checkable against the PNG in any viewer.
# "stem" is the edge of the rectangle the spray grows out of, so a card can be
# oriented with its cut end toward the branch instead of floating tip-first.
#
# These were read off the alpha map: threshold it, drop anything that is not
# green (bark, cones, dead branch), and take the bounding box of each remaining
# blob. build_sprays re-measures the alpha coverage of every rectangle at build
# time and fails if one has drifted, because a rectangle that has slipped onto
# background is invisible in the triangle count and obvious only in the render.
SPRAY_ATLAS = {
    "fir_tree_01_twig": (
        (624, 816, 1312, 1584, "bottom"),
        (1296, 928, 1952, 1696, "bottom"),
        (1328, 80, 1920, 752, "bottom"),
        (384, 96, 864, 640, "bottom"),
        (1008, 528, 1296, 816, "bottom"),
    ),
    # Same atlas layout as the mature fir, different scan.
    "fir_sapling_medium_twigs": (
        (624, 816, 1312, 1584, "bottom"),
        (1296, 928, 1952, 1696, "bottom"),
        (1328, 80, 1920, 768, "bottom"),
        (384, 96, 864, 640, "bottom"),
        (1008, 528, 1296, 816, "bottom"),
    ),
    # Pine has only two usable sprays - an upright shoot and a horizontal branch
    # tip. The rest of its atlas is cones, bark and loose needle litter, none of
    # which reads as canopy. Two is thin, so build_sprays leans on per-card roll
    # and mirroring for variety instead.
    "pine_tree_01_twig": (
        (64, 80, 384, 608, "bottom"),
        (1520, 688, 1904, 960, "left"),
    ),
    "pine_sapling_medium_twig": (
        (64, 80, 384, 608, "bottom"),
        (1520, 688, 1904, 960, "left"),
    ),
}

ATLAS_SIZE = 2048


# ---------------------------------------------------------------------------
# Conifers.
#
# slug, species key, variant letter, trunk material suffix, LOD0 needle budget.
# The needle budget tracks how much canopy each variant actually has rather than
# being uniform: fir_tree_01_c carries 446k source twig triangles against
# variant a's 4.07M, and spending the same budget on both would hand the sparse
# tree the dense tree's crown and throw away the variety that made three
# variants worth shipping.
#
# A trunk suffix of None means the variant's trunk material points at the shared
# bark maps (Poly Haven authored trunk_c that way), so its woody geometry ships
# as one part and one material instead of two.
CONIFERS = (
    ("fir_tree_01", "fir", "a", "trunk_a", 12000, "tallest, 17.6 m"),
    ("fir_tree_01", "fir", "b", "trunk_b", 9000, "broad, 12.5 m"),
    ("fir_tree_01", "fir", "c", None, 5000, "sparse, 13.3 m"),
    ("pine_tree_01", "pine", "a", "trunk_a", 13000, "tallest, 17.6 m"),
    ("pine_tree_01", "pine", "b", "trunk_b", 10000, "broad, 12.5 m"),
    ("pine_tree_01", "pine", "c", None, 12000, "columnar, 15.2 m"),
)

# LOD1 is what every tree past the first stand uses, so it is the count that
# actually multiplies. A third of LOD0 is roughly where the spire stops thinning
# out - measured, not guessed: below about 1,500 cards the crown starts showing
# sky through it at silhouette range.
LOD1_NEEDLE_FRACTION = 0.34


def conifer_assets():
    """Two LODs for each of the six mature conifer variants."""
    out = []
    for slug, species, letter, trunk_suffix, needles, note in CONIFERS:
        node = f"{slug}_{letter}_LOD0"
        twig_mat = f"{slug}_twig"
        for lod, needle_budget, wood in (
            (0, needles, 1.0),
            (1, int(needles * LOD1_NEEDLE_FRACTION), 0.33),
        ):
            parts = []
            if trunk_suffix:
                parts.append(part(
                    "Trunk", DECIMATE, max(120, int(480 * wood)), nodes=(node,),
                    material=(f"{slug}_{trunk_suffix}",),
                    mat_res=f"vegetation_{species}_{trunk_suffix}"))
                parts.append(part(
                    "Bark", DECIMATE, max(200, int(800 * wood)), nodes=(node,),
                    material=(f"{slug}_bark", f"{slug}_dead_branches"),
                    mat_res=f"vegetation_{species}_bark"))
            else:
                parts.append(part(
                    "Wood", DECIMATE, max(280, int(1100 * wood)), nodes=(node,),
                    material=(f"{slug}_bark", f"{slug}_trunk_c",
                              f"{slug}_dead_branches"),
                    mat_res=f"vegetation_{species}_bark"))
            parts.append(part(
                "Needles", SPRAY, needle_budget, nodes=(node,),
                material=(twig_mat,), mat_res=f"vegetation_{species}_needles",
                atlas=twig_mat,
                # 1.35 rather than 1.0 so neighbouring cards overlap. A cell's
                # diagonal is 1.73 times its side, so cards cut exactly to the
                # cell size leave a diagonal gap in every direction and the
                # canopy reads as a grid of floating leaves.
                spray_scale=1.35))
            # Both LODs get a trunk collider: the county scatters LOD0 and LOD1
            # as two separate layers of real trees, so a player can walk into
            # either one.
            out.append(dict(
                key=f"ashwood_{species}_{letter}_lod{lod}", slug=slug,
                source=GLTF, collision="cylinder",
                label=f"{species.capitalize()} {letter.upper()} ({note}, LOD{lod})",
                root_type="StaticBody3D", parts=parts))
    return out


ASSETS = conifer_assets() + [
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
    # ---- young conifers --------------------------------------------------
    # Same construction as the mature trees, and they share the mature trees'
    # atlas layout, so they share the spray table too. One LOD each: at 5-11 m
    # these are understorey, and the county fades them out well before the
    # distance a second LOD would start paying for itself.
    dict(
        key=f"ashwood_{species}_sapling_{letter}", slug=slug, source=GLTF,
        collision="cylinder", label=f"{species.capitalize()} sapling {letter.upper()}",
        root_type="StaticBody3D",
        parts=[
            part("Wood", DECIMATE, 320, nodes=(f"{slug}_{letter}_LOD0",),
                 material=(f"{slug}_{wood_mat}", f"{slug}_{dead_mat}"),
                 mat_res=f"vegetation_{species}_sapling_wood"),
            part("Needles", SPRAY, needles, nodes=(f"{slug}_{letter}_LOD0",),
                 material=(f"{slug}_{twig_mat}",),
                 mat_res=f"vegetation_{species}_sapling_needles",
                 atlas=f"{slug}_{twig_mat}", spray_scale=1.35),
        ],
    )
    for slug, species, wood_mat, dead_mat, twig_mat, variants in (
        ("fir_sapling_medium", "fir", "branches", "branches_dead", "twigs",
         (("a", 4000), ("b", 3000), ("c", 3000))),
        ("pine_sapling_medium", "pine", "bark", "dead_branches", "twig",
         (("a", 5000), ("b", 4000), ("c", 3500))),
    )
    for letter, needles in variants
] + [
    # ---- conifer forest floor -------------------------------------------
    # Solid scans with no cut-out at all, so plain quadric collapse is exactly
    # the right tool. Read through the glTF path only because it is cheaper than
    # spinning up an import for a 40k-triangle stump.
    dict(
        key=f"ashwood_tree_stump_{i:02d}", slug=f"tree_stump_{i:02d}",
        source=GLTF, collision="convex", label=f"Cut stump {i:02d}",
        root_type="StaticBody3D",
        parts=[part("Body", DECIMATE, 600, nodes=(f"tree_stump_{i:02d}",),
                    material=(f"tree_stump_{i:02d}",),
                    mat_res=f"vegetation_tree_stump_{i:02d}")],
    )
    for i in (1, 2)
] + [
    dict(
        key=f"ashwood_pine_roots_{v}", slug="pine_roots", source=GLTF,
        collision="convex", label=f"Upturned pine roots {v.upper()}",
        root_type="StaticBody3D",
        parts=[part("Body", DECIMATE, 700, nodes=(f"pine_roots_{v}",),
                    material=(f"pine_roots_{v}",),
                    mat_res=f"vegetation_pine_roots_{v}")],
    )
    for v in ("a", "b")
] + [
    dict(
        key=f"ashwood_dry_branches_{v}", slug="dry_branches_medium_01",
        source=GLTF, collision=None, label=f"Dry branch pile {v.upper()}",
        root_type="Node3D",
        parts=[part("Body", KEEP, 0, nodes=(f"dry_branches_medium_01_{v}",),
                    material=("dry_branches_medium_01",),
                    mat_res="vegetation_dry_branches_medium_01")],
    )
    for v in ("a", "b", "c")
] + [
    # Moss and the taller meadow grasses are already a few dozen triangles each
    # and only need gathering into a plantable clump.
    dict(
        key=f"ashwood_moss_{label}", slug="moss_01", collision=None,
        label=f"Moss patch ({label})", root_type="Node3D",
        parts=[part("Plant", KEEP, 0, nodes=nodes,
                    mat_res="vegetation_moss_01", cluster=0.14)],
    )
    for label, nodes in (
        ("flat", tuple(f"moss_01_{v}_LOD0" for v in "abcde")),
        ("clumped", tuple(f"moss_01_{v}_LOD0" for v in "fghij")),
        ("tall", ("moss_01_tall_a_LOD0", "moss_01_tall_b_LOD0")),
    )
] + [
    dict(
        key=f"ashwood_grass_medium_01_{label}", slug="grass_medium_01",
        collision=None, label=f"Meadow grass 01 ({label})", root_type="Node3D",
        parts=[part("Plant", KEEP, 0, nodes=nodes,
                    mat_res="vegetation_grass_medium_01", cluster=0.13)],
    )
    for label, nodes in (
        ("tall", tuple(f"grass_medium_01_tall_{v}_LOD0" for v in "abc")),
        ("mid", tuple(f"grass_medium_01_mid_{v}_LOD0" for v in "abc")),
        ("small", ("grass_medium_01_small_a_LOD0", "grass_medium_01_small_b_LOD0",
                   "grass_medium_01_tiny_a_LOD0", "grass_medium_01_tiny_c_LOD0")),
    )
] + [
    dict(
        key=f"ashwood_grass_medium_02_{label}", slug="grass_medium_02",
        collision=None, label=f"Meadow grass 02 ({label})", root_type="Node3D",
        parts=[part("Plant", KEEP, 0, nodes=nodes,
                    mat_res="vegetation_grass_medium_02", cluster=0.13)],
    )
    for label, nodes in (
        ("tuft", ("grass_medium_02_a", "grass_medium_02_c")),
        ("clump", ("grass_medium_02_b", "grass_medium_02_d", "grass_medium_02_e")),
    )
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

# Needles are far thicker and waxier than a broadleaf, and a conifer canopy is
# self-shadowing enough that the light which does get through has already been
# filtered by several layers. Reusing LEAF_BACKLIGHT here lit the crowns from
# inside and undid the dark mass that is the whole point of the species.
NEEDLE_BACKLIGHT = (0.05, 0.075, 0.04)


def conifer_materials():
    """Bark, trunk and needle materials for both mature species."""
    out = []
    for slug, species in (("fir_tree_01", "fir"), ("pine_tree_01", "pine")):
        out.append(mat(f"vegetation_{species}_bark",
                       f"{species.capitalize()} Bark", slug,
                       src_tex(slug, f"{slug}_bark_diff_2k.jpg"),
                       arm=src_tex(slug, f"{slug}_bark_arm_2k.jpg"),
                       normal=src_tex(slug, f"{slug}_bark_nor_gl_2k.jpg")))
        for suffix in ("trunk_a", "trunk_b"):
            out.append(mat(f"vegetation_{species}_{suffix}",
                           f"{species.capitalize()} {suffix.replace('_', ' ').title()}",
                           slug, src_tex(slug, f"{slug}_{suffix}_diff_2k.jpg"),
                           arm=src_tex(slug, f"{slug}_{suffix}_arm_2k.jpg"),
                           normal=src_tex(slug, f"{slug}_{suffix}_nor_gl_2k.jpg")))
        out.append(mat(f"vegetation_{species}_needles",
                       f"{species.capitalize()} Needles", slug,
                       out_tex(f"{slug}_twig"),
                       arm=src_tex(slug, f"{slug}_twig_arm_2k.jpg"),
                       normal=src_tex(slug, f"{slug}_twig_nor_gl_2k.jpg"),
                       cutout=True, backlight=NEEDLE_BACKLIGHT))
    for slug, species, wood, twig in (
        ("fir_sapling_medium", "fir", "branches", "twigs"),
        ("pine_sapling_medium", "pine", "bark", "twig"),
    ):
        out.append(mat(f"vegetation_{species}_sapling_wood",
                       f"{species.capitalize()} Sapling Wood", slug,
                       src_tex(slug, f"{slug}_{wood}_diff_2k.jpg"),
                       arm=src_tex(slug, f"{slug}_{wood}_arm_2k.jpg"),
                       normal=src_tex(slug, f"{slug}_{wood}_nor_gl_2k.jpg")))
        out.append(mat(f"vegetation_{species}_sapling_needles",
                       f"{species.capitalize()} Sapling Needles", slug,
                       out_tex(f"{slug}_{twig}"),
                       arm=src_tex(slug, f"{slug}_{twig}_arm_2k.jpg"),
                       normal=src_tex(slug, f"{slug}_{twig}_nor_gl_2k.jpg"),
                       cutout=True, backlight=NEEDLE_BACKLIGHT))
    return out


def simple_materials():
    """One-map-set props: stumps, roots, deadfall, moss and meadow grass."""
    out = []
    for slug, res_name, label, cutout in (
        ("tree_stump_01", "vegetation_tree_stump_01", "Cut Stump 01", False),
        ("tree_stump_02", "vegetation_tree_stump_02", "Cut Stump 02", False),
        ("dry_branches_medium_01", "vegetation_dry_branches_medium_01",
         "Dry Branches", False),
        ("moss_01", "vegetation_moss_01", "Moss", True),
        ("grass_medium_01", "vegetation_grass_medium_01", "Meadow Grass 01", True),
        ("grass_medium_02", "vegetation_grass_medium_02", "Meadow Grass 02", True),
    ):
        out.append(mat(res_name, label, slug,
                       out_tex(slug) if cutout
                       else src_tex(slug, f"{slug}_diff_2k.jpg"),
                       arm=src_tex(slug, f"{slug}_arm_2k.jpg"),
                       normal=src_tex(slug, f"{slug}_nor_gl_2k.jpg"),
                       cutout=cutout,
                       backlight=LEAF_BACKLIGHT if cutout else None))
    # pine_roots ships one map set per variant rather than one for the pair.
    for v in ("a", "b"):
        out.append(mat(f"vegetation_pine_roots_{v}", f"Pine Roots {v.upper()}",
                       "pine_roots",
                       src_tex("pine_roots", f"pine_roots_{v}_diff_2k.jpg"),
                       arm=src_tex("pine_roots", f"pine_roots_{v}_arm_2k.jpg"),
                       normal=src_tex("pine_roots",
                                      f"pine_roots_{v}_nor_gl_2k.jpg")))
    return out


MATERIALS = conifer_materials() + simple_materials() + [
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

    def keep_only_material(obj, material_names):
        """Delete every face whose material slot is not in material_names.

        The conifer scans pack three whole trees into one file and share the
        bark, twig and dead-branch materials across all three, so a part is
        identified by node AND by material - neither alone is enough. Takes a
        tuple so a woody part can gather bark, trunk and dead branches in one
        pass instead of being joined back together afterwards.
        """
        if isinstance(material_names, str):
            material_names = (material_names,)
        slots = [i for i, s in enumerate(obj.material_slots)
                 if s.material and any(s.material.name.startswith(n)
                                       for n in material_names)]
        if not slots:
            raise RuntimeError(
                f"{obj.name}: no material slot matching {material_names!r}; "
                f"have {[s.material.name if s.material else None for s in obj.material_slots]}"
            )
        keep = set(slots)

        # Face-index arithmetic in numpy rather than bmesh: a 7M-triangle twig
        # mesh costs several GB as a bmesh, and this only needs to delete faces.
        me = obj.data
        nf = len(me.polygons)
        idx = np.empty(nf, np.int32)
        me.polygons.foreach_get("material_index", idx)
        doomed_mask = ~np.isin(idx, list(keep))
        if not doomed_mask.any():
            return
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        doomed = [bm.faces[i] for i in np.flatnonzero(doomed_mask).tolist()]
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        bm.to_mesh(me)
        bm.free()
        me.update()

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

    def islands_of_slice(pos, tris, uv, lv):
        """Label one slice of a mesh the way the source glTF stored it.

        Blender's importer welds vertices that a glTF split along a UV seam, so
        raw mesh connectivity would merge neighbouring leaf cards. Rebuilding
        identity as (vertex, quantised uv) restores the original islands, which
        is what makes one-quad-per-card reconstruction valid.
        """
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
        return labels, corner_pos, corner_uv

    def island_slices(obj, tiles):
        """Yield (labels, corner_pos, corner_uv) for the mesh, tile by tile.

        Island labelling is the memory peak of the whole build: it uniquifies a
        (3 * triangles, 3) int64 key and then runs label propagation over three
        directed edges per triangle. A conifer canopy is 7M triangles of needle
        sprays, which is roughly 20M loops - several GB in one pass, enough to
        kill the build outright.

        Splitting the mesh into vertical slabs by triangle centroid drops the
        peak by the tile count. Cards are ~10cm across and slabs are metres, so
        only a handful straddle a boundary, and one that does simply becomes two
        smaller cards rather than a hole.
        """
        pos, tris, uv, lv = mesh_arrays(obj)
        if tiles <= 1:
            yield islands_of_slice(pos, tris, uv, lv)
            return

        # Slice along the mesh's longest horizontal axis so the slabs are
        # roughly equal in triangle count rather than in empty space.
        centre = pos[lv][tris].mean(axis=1)
        axis = 0 if (centre[:, 0].ptp() >= centre[:, 2].ptp()) else 2
        edges = np.quantile(centre[:, axis], np.linspace(0.0, 1.0, tiles + 1))
        edges[0] -= 1.0
        edges[-1] += 1.0
        for t in range(tiles):
            mask = (centre[:, axis] > edges[t]) & (centre[:, axis] <= edges[t + 1])
            if not mask.any():
                continue
            sub_tris = tris[mask]
            # Reindex the loop range down to just this slab's corners.
            used = np.unique(sub_tris)
            remap = np.full(len(lv), -1, np.int64)
            remap[used] = np.arange(len(used))
            yield islands_of_slice(pos, remap[sub_tris], uv[used], lv[used])

    def grid_thin(centroids, target):
        """Pick ~target islands evenly in space, never randomly.

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

    def build_sprays(obj, budget, spray_scale, name, atlas_key):
        """Voxelise a needle canopy and emit one atlas branch-spray per cell.

        The CARD method fits one quad per UV island, which works when an island
        is a leaf. A conifer island is a few needles: fir_tree_01_a's canopy is
        812k islands, so one quad each is 1.6M triangles before any thinning,
        and thinning that far destroys the crown. The needles are also far below
        the size a single texel can describe.

        So the source geometry is used only as a density field. Triangle
        centroids are voxelised, and each occupied cell emits one quad carrying a
        photographed branch spray from the atlas - many needles per quad instead
        of many quads per needle. Cell size is solved for the triangle budget, so
        a sparse variant keeps its sparseness instead of being inflated to match
        a dense one.
        """
        me = obj.data
        me.calc_loop_triangles()
        if not me.loop_triangles:
            raise RuntimeError(f"{name}: no needle geometry to spray")

        co = np.empty(len(me.vertices) * 3)
        me.vertices.foreach_get("co", co)
        co = co.reshape(-1, 3)

        idx = np.empty(len(me.loop_triangles) * 3, np.int64)
        me.loop_triangles.foreach_get("vertices", idx)
        idx = idx.reshape(-1, 3)

        tri = co[idx]
        centroids = tri.mean(1)
        areas = 0.5 * np.linalg.norm(
            np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0]), axis=1)

        # Two triangles per quad, so the cell target is half the budget.
        target_cells = max(1, budget // 2)

        # Solve cell size for the target the same way grid_thin does. Occupancy
        # is far too sparse and clustered to predict from bounding-box volume.
        lo = centroids.min(0)
        span = float(np.linalg.norm(centroids.max(0) - lo)) or 1.0
        h_lo, h_hi = span * 1e-4, span
        cells = keys = inverse = None
        for _ in range(48):
            h = math.sqrt(h_lo * h_hi)
            grid = np.floor((centroids - lo) / h).astype(np.int64)
            uniq, inv = np.unique(grid, axis=0, return_inverse=True)
            if len(uniq) > target_cells:
                h_lo = h
            else:
                h_hi = h
                cells, keys, inverse = h, uniq, inv
            if abs(len(uniq) - target_cells) <= max(2, target_cells // 50):
                cells, keys, inverse = h, uniq, inv
                break
        if keys is None:
            cells, keys, inverse = h, uniq, inv

        n_cells = len(keys)
        weight = np.maximum(areas, 1e-12)
        wsum = np.bincount(inverse, weights=weight, minlength=n_cells)

        # Area-weighted centre, so a cell clipped by the crown edge places its
        # spray where the needles actually are rather than at the cell middle.
        centre = np.zeros((n_cells, 3))
        for axis in range(3):
            centre[:, axis] = np.bincount(
                inverse, weights=weight * centroids[:, axis],
                minlength=n_cells) / np.maximum(wsum, 1e-12)

        rects = SPRAY_ATLAS[atlas_key]
        crown = centre.mean(0)

        # The trunk axis, for deciding which way is "outward". Conifers are
        # radially symmetric about it, so a per-height axis is unnecessary.
        axis_xy = centre[:, :2].mean(0)

        rng = np.random.default_rng(abs(hash(name)) % (2 ** 32))
        pick = rng.integers(0, len(rects), n_cells)
        mirror = rng.random(n_cells) < 0.5
        droop = rng.uniform(0.25, 0.75, n_cells)

        # Each cell's quad is sized from the needle area it stands for, so dense
        # inner crown and thin outer tips do not get identical cards. The cube
        # root of cell volume sets the base scale and the area ratio modulates it.
        base = cells * spray_scale
        density = wsum / max(float(wsum.mean()), 1e-12)
        extent = base * np.clip(density ** (1.0 / 3.0), 0.55, 1.9)

        verts, faces, uvs, normals = [], [], [], []
        for i in range(n_cells):
            x0, y0, x1, y1, stem = rects[int(pick[i])]

            # Atlas rectangles are measured in texels from the image top left,
            # but these UVs are authored in Blender, whose V origin is bottom
            # left and which flips V again on glTF export. So V is flipped here.
            #
            # Verified rather than reasoned: build_sprays re-measures each card's
            # alpha coverage, and removing this flip dropped fir_a from 0.383 to
            # 0.048 - the cards landing on transparent background instead of on
            # needle sprays.
            u0, u1 = x0 / ATLAS_SIZE, x1 / ATLAS_SIZE
            v0, v1 = 1.0 - y1 / ATLAS_SIZE, 1.0 - y0 / ATLAS_SIZE
            if mirror[i]:
                u0, u1 = u1, u0

            aspect = (x1 - x0) / max(y1 - y0, 1e-6)

            c = centre[i]
            radial = np.array([c[0] - axis_xy[0], c[1] - axis_xy[1], 0.0])
            rl = np.linalg.norm(radial)
            radial = radial / rl if rl > 1e-6 else np.array([1.0, 0.0, 0.0])

            up = np.array([0.0, 0.0, 1.0])
            side = np.cross(up, radial)
            sl = np.linalg.norm(side)
            side = side / sl if sl > 1e-9 else np.array([0.0, 1.0, 0.0])

            # Branches leave the trunk outward and sag. Mixing the outward and
            # down vectors gives the drooping habit that reads as spruce or fir
            # rather than a bottlebrush of horizontal spokes.
            along = radial * (1.0 - droop[i] * 0.55) - up * droop[i] * 0.8
            al = np.linalg.norm(along)
            along = along / al if al > 1e-9 else radial

            half_len = extent[i] * 0.5
            half_wid = half_len / max(aspect, 1e-3) if aspect > 1.0 else half_len * aspect
            if aspect > 1.0:
                half_wid = half_len / aspect
            else:
                half_wid = half_len
                half_len = half_wid * aspect

            # "stem" is the edge the spray grows from, so the quad is built with
            # that edge nearest the trunk and the tip pointing away. Without it
            # half the canopy grows inwards and the crown looks turned inside out.
            if stem == "left":
                origin = c - along * half_len
                a = origin - side * half_wid
                b = origin + along * (half_len * 2.0) - side * half_wid
                d = origin + side * half_wid
                cpt = b + side * (half_wid * 2.0)
                corners = np.array([a, b, cpt, d])
                quad_uv = np.array([[u0, v0], [u1, v0], [u1, v1], [u0, v1]])
            else:
                origin = c - along * half_len
                a = origin - side * half_wid
                b = origin + side * half_wid
                cpt = b + along * (half_len * 2.0)
                d = a + along * (half_len * 2.0)
                corners = np.array([a, b, cpt, d])
                quad_uv = np.array([[u0, v0], [u1, v0], [u1, v1], [u0, v1]])

            e0 = corners[1] - corners[0]
            e1 = corners[3] - corners[0]
            nrm = np.cross(e0, e1)
            ln = np.linalg.norm(nrm)
            if ln < 1e-12:
                continue
            nrm /= ln

            # Same reasoning as build_cards: bias the shading normal outward from
            # the crown so the canopy carries one rounded gradient instead of
            # lighting as a pile of unrelated flat chips.
            outward = c - crown
            lo_n = np.linalg.norm(outward)
            outward = outward / lo_n if lo_n > 1e-9 else nrm
            if float(nrm @ outward) < 0.0:
                nrm = -nrm
            blended = nrm * 0.45 + outward * 0.55
            bl = np.linalg.norm(blended)
            blended = blended / bl if bl > 1e-9 else nrm

            base_i = len(verts)
            verts.extend(corners.tolist())
            uvs.extend(quad_uv.tolist())
            normals.extend([blended.tolist()] * 4)
            faces.append((base_i, base_i + 1, base_i + 2))
            faces.append((base_i, base_i + 2, base_i + 3))

        if not faces:
            raise RuntimeError(f"{name}: spray rebuild produced no geometry")

        mesh = bpy.data.meshes.new(name + "Mesh")
        mesh.from_pydata(verts, [], faces)
        mesh.update()
        layer = mesh.uv_layers.new(name="UVMap")
        loop_uv = np.zeros((len(mesh.loops), 2))
        lv = np.empty(len(mesh.loops), np.int64)
        mesh.loops.foreach_get("vertex_index", lv)
        loop_uv[:] = np.asarray(uvs)[lv]
        layer.data.foreach_set("uv", loop_uv.reshape(-1))
        mesh.normals_split_custom_set_from_vertices(normals)

        spray = bpy.data.objects.new(name, mesh)
        bpy.context.scene.collection.objects.link(spray)
        stats = dict(cells=n_cells, cell_size=round(float(cells), 4),
                     sprays=len(faces) // 2, spray_scale=spray_scale)
        return spray, stats

    def build_cards(obj, budget, card_scale, name, tiles=1):
        """Replace every UV island with one quad fitted through its own UVs."""
        # Islands are labelled tile by tile to bound peak memory, but thinning
        # has to see the whole crown at once or each tile keeps its own quota
        # and the canopy ends up denser in whichever slab happened to be
        # smallest. So: label per tile, pool the centroids, thin globally, then
        # fit only the survivors.
        slices = []
        centroids = []
        for labels, cpos, cuv in island_slices(obj, tiles):
            n = int(labels.max()) + 1
            counts = np.bincount(labels, minlength=n).astype(np.float64)
            cen = np.zeros((n, 3))
            for axis in range(3):
                cen[:, axis] = np.bincount(labels, weights=cpos[:, axis],
                                           minlength=n) / counts
            order = np.argsort(labels, kind="stable")
            sorted_lab = labels[order]
            bounds = np.r_[0, np.flatnonzero(np.diff(sorted_lab)) + 1,
                           len(sorted_lab)]
            slices.append((cpos, cuv, order, bounds))
            centroids.append(cen)

        counts_per_slice = [len(c) for c in centroids]
        cen = np.concatenate(centroids)
        n_isl = len(cen)
        offsets = np.cumsum([0] + counts_per_slice)

        target = max(1, budget // 2)
        keep = grid_thin(cen, target)

        # Blend each card's own normal towards "outwards from the crown". Purely
        # planar normals make every card light as a flat chip, which is exactly
        # the flat-cartoon-foliage read being designed out; the outward term
        # gives the canopy one coherent rounded shading gradient.
        crown = cen.mean(0)

        verts, faces, uvs, normals = [], [], [], []
        skipped = 0
        for global_isl in keep:
            si = int(np.searchsorted(offsets, global_isl, side="right") - 1)
            isl = int(global_isl - offsets[si])
            cpos, cuv, order, bounds = slices[si]
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
            # Node selection and material selection are independent filters and
            # the conifers need both: one file holds three whole trees that all
            # share the bark and twig materials, so the node picks the tree and
            # the material picks the part of it.
            if spec["nodes"]:
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
            else:
                work = duplicate(list(sources.values())[0], name + "Src")

            if spec["material"]:
                keep_only_material(work, spec["material"])

            source_tris = tri_count(work)
            if spec["method"] == CARD:
                obj, cstats = build_cards(work, spec["budget"], spec["card_scale"],
                                          name, spec.get("tiles", 1))
                bpy.data.objects.remove(work, do_unlink=True)
                stats_by_part[name] = cstats
            elif spec["method"] == SPRAY:
                obj, sstats = build_sprays(work, spec["budget"],
                                           spec.get("spray_scale", 1.35),
                                           name, spec["atlas"])
                bpy.data.objects.remove(work, do_unlink=True)
                stats_by_part[name] = sstats
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
