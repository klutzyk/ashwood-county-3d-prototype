"""Build the project-owned Willow Outfitters retail fixture pack.

Run with Blender 4.4 or newer:

    blender.exe --background --python tools/build_willow_outfitters_fixtures.py

The pack uses metre-scale authored geometry, softened silhouettes, working
retail proportions, and downloaded CC0 PBR maps.  The generated GLBs are
intended for repeated instancing in Godot rather than unique scene geometry.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import Callable, Sequence

import bpy
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_silver_spoon_fixtures as fixture_tools


REPO_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = (
    REPO_ROOT / "assets" / "environment" / "buildings" / "WillowOutfitters" / "fixtures"
)
SOURCE_ROOT = (
    REPO_ROOT
    / "assets"
    / "third_party"
    / "interiors"
    / "willow_outfitters"
    / "poly_haven"
)
FABRIC_ROOT = SOURCE_ROOT / "materials"
DINER_MATERIALS = (
    REPO_ROOT / "assets" / "third_party" / "interiors" / "diner" / "materials"
)

WOOD_ROOT = DINER_MATERIALS / "poly_haven" / "wood_table_worn"
METAL_ROOT = DINER_MATERIALS / "ambientcg" / "Metal049A"

TEXTURE_SETS = {
    "wood": (
        WOOD_ROOT / "wood_table_worn_diff_1k.jpg",
        WOOD_ROOT / "wood_table_worn_nor_gl_1k.jpg",
        WOOD_ROOT / "wood_table_worn_arm_1k.jpg",
    ),
    "corduroy": (
        FABRIC_ROOT / "ribbed_corduroy" / "ribbed_corduroy_diff_1k.jpg",
        FABRIC_ROOT / "ribbed_corduroy" / "ribbed_corduroy_nor_gl_1k.jpg",
        FABRIC_ROOT / "ribbed_corduroy" / "ribbed_corduroy_arm_1k.jpg",
    ),
    "denim": (
        FABRIC_ROOT / "denim_fabric_06" / "denim_fabric_06_diff_1k.jpg",
        FABRIC_ROOT / "denim_fabric_06" / "denim_fabric_06_nor_gl_1k.jpg",
        FABRIC_ROOT / "denim_fabric_06" / "denim_fabric_06_arm_1k.jpg",
    ),
    "fleece": (
        FABRIC_ROOT / "knitted_fleece" / "knitted_fleece_diff_1k.jpg",
        FABRIC_ROOT / "knitted_fleece" / "knitted_fleece_nor_gl_1k.jpg",
        FABRIC_ROOT / "knitted_fleece" / "knitted_fleece_arm_1k.jpg",
    ),
    "leather": (
        FABRIC_ROOT / "brown_leather" / "brown_leather_albedo_1k.jpg",
        FABRIC_ROOT / "brown_leather" / "brown_leather_nor_gl_1k.jpg",
        FABRIC_ROOT / "brown_leather" / "brown_leather_arm_1k.jpg",
    ),
}

METAL_COLOR = METAL_ROOT / "Metal049A_1K-JPG_Color.jpg"
METAL_NORMAL = METAL_ROOT / "Metal049A_1K-JPG_NormalGL.jpg"
METAL_ROUGHNESS = METAL_ROOT / "Metal049A_1K-JPG_Roughness.jpg"

MATERIALS: dict[str, bpy.types.Material] = {}


def require_textures() -> None:
    paths = [path for texture_set in TEXTURE_SETS.values() for path in texture_set]
    paths.extend((METAL_COLOR, METAL_NORMAL, METAL_ROUGHNESS))
    missing = [path for path in paths if not path.is_file()]
    if missing:
        details = "\n".join(f"  - {path}" for path in missing)
        raise FileNotFoundError(f"Willow fixture source maps are missing:\n{details}")


def add_tinted_texture(
    node_tree: bpy.types.NodeTree,
    shader: bpy.types.Node,
    path: Path,
    tint: tuple[float, float, float, float],
) -> None:
    texture = node_tree.nodes.new("ShaderNodeTexImage")
    texture.name = f"{path.stem}_Color"
    texture.image = fixture_tools.load_image(path)
    texture.location = (-650, 260)
    tint_node = node_tree.nodes.new("ShaderNodeRGB")
    tint_node.name = "WillowColorTint"
    tint_node.outputs["Color"].default_value = tint
    tint_node.location = (-650, 100)
    multiply = node_tree.nodes.new("ShaderNodeMixRGB")
    multiply.name = "WillowTintMultiply"
    multiply.blend_type = "MULTIPLY"
    multiply.inputs["Fac"].default_value = 1.0
    multiply.location = (-140, 230)
    node_tree.links.new(texture.outputs["Color"], multiply.inputs[1])
    node_tree.links.new(tint_node.outputs["Color"], multiply.inputs[2])
    node_tree.links.new(multiply.outputs["Color"], shader.inputs["Base Color"])


def textured_material(
    name: str,
    texture_key: str,
    tint: tuple[float, float, float, float],
    *,
    roughness: float = 0.62,
    normal_strength: float = 0.48,
) -> bpy.types.Material:
    diffuse, normal, arm = TEXTURE_SETS[texture_key]
    material, tree, shader = fixture_tools.make_principled_material(
        name, tint, roughness=roughness
    )
    add_tinted_texture(tree, shader, diffuse, tint)
    fixture_tools.attach_arm_map(tree, shader, arm, use_metallic=False)
    fixture_tools.attach_normal_map(
        tree, shader, normal, strength=normal_strength, location=(-650, -360)
    )
    return material


def create_materials() -> dict[str, bpy.types.Material]:
    wood = textured_material(
        "Willow_Worn_Oak_PBR", "wood", (0.68, 0.47, 0.28, 1.0), roughness=0.58
    )
    dark_wood = textured_material(
        "Willow_Dark_Oak_PBR", "wood", (0.34, 0.19, 0.09, 1.0), roughness=0.68
    )
    olive = textured_material(
        "Willow_Olive_Corduroy_PBR",
        "corduroy",
        (0.46, 0.54, 0.34, 1.0),
        roughness=0.82,
        normal_strength=0.68,
    )
    forest = textured_material(
        "Willow_Forest_Corduroy_PBR",
        "corduroy",
        (0.22, 0.39, 0.24, 1.0),
        roughness=0.84,
        normal_strength=0.65,
    )
    rust = textured_material(
        "Willow_Rust_Corduroy_PBR",
        "corduroy",
        (0.76, 0.33, 0.18, 1.0),
        roughness=0.8,
        normal_strength=0.62,
    )
    denim = textured_material(
        "Willow_Dark_Denim_PBR",
        "denim",
        (0.38, 0.5, 0.62, 1.0),
        roughness=0.78,
        normal_strength=0.72,
    )
    fleece = textured_material(
        "Willow_Tan_Fleece_PBR",
        "fleece",
        (0.82, 0.63, 0.35, 1.0),
        roughness=0.91,
        normal_strength=0.74,
    )
    cream_fleece = textured_material(
        "Willow_Cream_Fleece_PBR",
        "fleece",
        (0.98, 0.91, 0.72, 1.0),
        roughness=0.93,
        normal_strength=0.7,
    )
    leather = textured_material(
        "Willow_Brown_Leather_PBR",
        "leather",
        (0.54, 0.3, 0.15, 1.0),
        roughness=0.58,
        normal_strength=0.5,
    )

    black_metal, tree, shader = fixture_tools.make_principled_material(
        "Willow_Blackened_Steel_PBR",
        (0.055, 0.065, 0.06, 1.0),
        metallic=0.72,
        roughness=0.46,
    )
    add_tinted_texture(tree, shader, METAL_COLOR, (0.16, 0.18, 0.16, 1.0))
    roughness_map = tree.nodes.new("ShaderNodeTexImage")
    roughness_map.image = fixture_tools.load_image(METAL_ROUGHNESS, non_color=True)
    roughness_map.location = (-600, -20)
    tree.links.new(roughness_map.outputs["Color"], shader.inputs["Roughness"])
    fixture_tools.attach_normal_map(
        tree, shader, METAL_NORMAL, strength=0.24, location=(-610, -340)
    )

    brass, tree, shader = fixture_tools.make_principled_material(
        "Willow_Aged_Brass_PBR",
        (0.42, 0.25, 0.075, 1.0),
        metallic=0.78,
        roughness=0.42,
    )
    fixture_tools.attach_normal_map(tree, shader, METAL_NORMAL, strength=0.16)

    rubber, tree, shader = fixture_tools.make_principled_material(
        "Willow_Rubber_PBR", (0.025, 0.029, 0.026, 1.0), roughness=0.82
    )
    fixture_tools.attach_normal_map(tree, shader, METAL_NORMAL, strength=0.12)

    return {
        "wood": wood,
        "dark_wood": dark_wood,
        "olive": olive,
        "forest": forest,
        "rust": rust,
        "denim": denim,
        "fleece": fleece,
        "cream_fleece": cream_fleece,
        "leather": leather,
        "black_metal": black_metal,
        "brass": brass,
        "rubber": rubber,
    }


def new_root(name: str, nominal_dimensions: Sequence[float]) -> bpy.types.Object:
    root = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(root)
    root["fixture_pack"] = "Willow Outfitters"
    root["units"] = "metres"
    root["nominal_dimensions_m"] = ",".join(
        f"{dimension:.3f}" for dimension in nominal_dimensions
    )
    return root


def add_tapered_prism(
    name: str,
    shoulder_width: float,
    hem_width: float,
    depth: float,
    height: float,
    location: Sequence[float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    bevel: float = 0.025,
) -> bpy.types.Object:
    half_top = shoulder_width * 0.5
    half_bottom = hem_width * 0.5
    half_depth = depth * 0.5
    half_height = height * 0.5
    vertices = [
        (-half_bottom, -half_depth, -half_height),
        (half_bottom, -half_depth, -half_height),
        (half_top, -half_depth, half_height),
        (-half_top, -half_depth, half_height),
        (-half_bottom, half_depth, -half_height),
        (half_bottom, half_depth, -half_height),
        (half_top, half_depth, half_height),
        (-half_top, half_depth, half_height),
    ]
    faces = (
        (0, 1, 2, 3),
        (5, 4, 7, 6),
        (4, 0, 3, 7),
        (1, 5, 6, 2),
        (3, 2, 6, 7),
        (4, 5, 1, 0),
    )
    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = Vector(location)
    return fixture_tools.finish_mesh(
        obj, material, root, bevel=bevel, smooth=True, smart_uv=True
    )


def add_cone_between(
    name: str,
    start: Sequence[float],
    end: Sequence[float],
    radius_start: float,
    radius_end: float,
    material: bpy.types.Material,
    root: bpy.types.Object,
) -> bpy.types.Object:
    start_vector = Vector(start)
    end_vector = Vector(end)
    delta = end_vector - start_vector
    midpoint = (start_vector + end_vector) * 0.5
    bpy.ops.mesh.primitive_cone_add(
        vertices=20,
        radius1=radius_start,
        radius2=radius_end,
        depth=delta.length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0.0, 0.0, 1.0)).rotation_difference(
        delta.normalized()
    )
    return fixture_tools.finish_mesh(
        obj, material, root, bevel=0.008, smooth=True, smart_uv=True
    )


def add_hanger(
    prefix: str,
    center: tuple[float, float, float],
    root: bpy.types.Object,
    *,
    width: float = 0.46,
) -> None:
    x, y, z = center
    metal = MATERIALS["brass"]
    fixture_tools.add_tube_between(
        f"{prefix}_HangerLeft",
        (x, y, z - 0.08),
        (x - width * 0.5, y, z - 0.27),
        0.008,
        metal,
        root,
        vertices=10,
    )
    fixture_tools.add_tube_between(
        f"{prefix}_HangerRight",
        (x, y, z - 0.08),
        (x + width * 0.5, y, z - 0.27),
        0.008,
        metal,
        root,
        vertices=10,
    )
    fixture_tools.add_tube_between(
        f"{prefix}_HangerBase",
        (x - width * 0.5, y, z - 0.27),
        (x + width * 0.5, y, z - 0.27),
        0.008,
        metal,
        root,
        vertices=10,
    )
    fixture_tools.add_curve_tube(
        f"{prefix}_HangerHook",
        (
            (x, y, z - 0.08),
            (x, y, z + 0.06),
            (x + 0.045, y, z + 0.11),
            (x + 0.095, y, z + 0.06),
        ),
        0.008,
        metal,
        root,
        resolution=2,
        bevel_resolution=1,
    )


def add_jacket(
    prefix: str,
    center: tuple[float, float, float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    scale: float = 1.0,
    pocket_style: int = 0,
) -> None:
    x, y, z = center
    shoulder = 0.57 * scale
    hem = 0.49 * scale
    depth = 0.17 * scale
    height = 0.82 * scale
    add_hanger(prefix, (x, y, z + 0.62 * scale), root, width=0.48 * scale)
    add_tapered_prism(
        f"{prefix}_Body",
        shoulder,
        hem,
        depth,
        height,
        (x, y, z),
        material,
        root,
        bevel=0.028 * scale,
    )
    sleeve_y = y + 0.005
    add_cone_between(
        f"{prefix}_LeftSleeve",
        (x - shoulder * 0.48, sleeve_y, z + height * 0.35),
        (x - shoulder * 0.62, sleeve_y, z - height * 0.35),
        0.105 * scale,
        0.075 * scale,
        material,
        root,
    )
    add_cone_between(
        f"{prefix}_RightSleeve",
        (x + shoulder * 0.48, sleeve_y, z + height * 0.35),
        (x + shoulder * 0.62, sleeve_y, z - height * 0.35),
        0.105 * scale,
        0.075 * scale,
        material,
        root,
    )
    fixture_tools.add_box(
        f"{prefix}_Zipper",
        (0.018 * scale, depth * 1.08, height * 0.86),
        (x, y - depth * 0.1, z - 0.035 * scale),
        MATERIALS["brass"],
        root,
        bevel=0.004,
    )
    collar_angle = math.radians(18.0)
    fixture_tools.add_box(
        f"{prefix}_LeftCollar",
        (0.19 * scale, depth * 1.18, 0.105 * scale),
        (x - 0.085 * scale, y - 0.005, z + height * 0.45),
        material,
        root,
        bevel=0.014,
        rotation=(0.0, collar_angle, 0.0),
    )
    fixture_tools.add_box(
        f"{prefix}_RightCollar",
        (0.19 * scale, depth * 1.18, 0.105 * scale),
        (x + 0.085 * scale, y - 0.005, z + height * 0.45),
        material,
        root,
        bevel=0.014,
        rotation=(0.0, -collar_angle, 0.0),
    )
    pocket_z = z - 0.13 * scale
    for side_index, side in enumerate((-1.0, 1.0), start=1):
        pocket_x = x + side * 0.145 * scale
        if pocket_style % 2:
            fixture_tools.add_box(
                f"{prefix}_Pocket_{side_index}",
                (0.17 * scale, 0.022 * scale, 0.16 * scale),
                (pocket_x, y - depth * 0.55, pocket_z),
                material,
                root,
                bevel=0.016,
                rotation=(0.0, math.radians(side * 6.0), 0.0),
            )
        else:
            fixture_tools.add_tube_between(
                f"{prefix}_PocketZip_{side_index}",
                (pocket_x - 0.07 * scale, y - depth * 0.58, pocket_z),
                (pocket_x + 0.07 * scale, y - depth * 0.58, pocket_z + 0.025 * scale),
                0.006 * scale,
                MATERIALS["brass"],
                root,
                vertices=8,
            )
    fixture_tools.add_tube_between(
        f"{prefix}_HemSeam",
        (x - hem * 0.43, y - depth * 0.55, z - height * 0.46),
        (x + hem * 0.43, y - depth * 0.55, z - height * 0.46),
        0.007 * scale,
        MATERIALS["leather"],
        root,
        vertices=8,
    )


def add_folded_stack(
    prefix: str,
    center: tuple[float, float, float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    layers: int = 4,
    width: float = 0.48,
    depth: float = 0.34,
) -> None:
    x, y, base_z = center
    for layer in range(layers):
        offset = (layer % 2) * 0.012 - 0.006
        z = base_z + layer * 0.078
        fixture_tools.add_rounded_rect_prism(
            f"{prefix}_Fold_{layer + 1}",
            width - layer * 0.009,
            depth,
            0.072,
            0.055,
            (x + offset, y, z),
            material,
            root,
            corner_segments=6,
            bevel=0.008,
        )
        fixture_tools.add_tube_between(
            f"{prefix}_FoldSeam_{layer + 1}",
            (x - width * 0.34, y - depth * 0.505, z),
            (x + width * 0.34, y - depth * 0.505, z),
            0.004,
            MATERIALS["leather"],
            root,
            vertices=8,
        )


def add_caster(
    prefix: str,
    center: tuple[float, float, float],
    root: bpy.types.Object,
) -> None:
    x, y, z = center
    fixture_tools.add_tube_between(
        f"{prefix}_Fork",
        (x, y, z + 0.12),
        (x, y, z + 0.045),
        0.018,
        MATERIALS["black_metal"],
        root,
        vertices=12,
    )
    fixture_tools.add_cylinder(
        f"{prefix}_Wheel",
        0.055,
        0.045,
        (x, y, z),
        MATERIALS["rubber"],
        root,
        vertices=20,
        rotation=(math.pi / 2.0, 0.0, 0.0),
        bevel=0.006,
    )


def build_clothing_rack() -> bpy.types.Object:
    root = new_root("Willow_ClothingRack", (3.0, 1.0, 2.2))
    metal = MATERIALS["black_metal"]
    for x in (-1.38, 1.38):
        fixture_tools.add_tube_between(
            f"RackUpright_{x:+.0f}",
            (x, 0, 0.15),
            (x, 0, 2.08),
            0.035,
            metal,
            root,
            vertices=16,
        )
        fixture_tools.add_tube_between(
            f"RackFootFront_{x:+.0f}",
            (x, -0.38, 0.16),
            (x, 0.38, 0.16),
            0.035,
            metal,
            root,
            vertices=16,
        )
        add_caster(f"RackCasterA_{x:+.0f}", (x, -0.34, 0.06), root)
        add_caster(f"RackCasterB_{x:+.0f}", (x, 0.34, 0.06), root)
    fixture_tools.add_tube_between(
        "RackCrossbar",
        (-1.42, 0, 2.08),
        (1.42, 0, 2.08),
        0.038,
        metal,
        root,
        vertices=18,
    )
    jacket_materials = (
        MATERIALS["olive"],
        MATERIALS["denim"],
        MATERIALS["rust"],
        MATERIALS["forest"],
        MATERIALS["fleece"],
        MATERIALS["denim"],
        MATERIALS["cream_fleece"],
    )
    for index, material in enumerate(jacket_materials):
        x = -1.08 + index * 0.36
        y = ((index % 3) - 1) * 0.025
        add_jacket(
            f"RackJacket_{index + 1}",
            (x, y, 1.28),
            material,
            root,
            scale=0.88 + (index % 2) * 0.035,
            pocket_style=index,
        )
    return root


def build_wall_apparel_display() -> bpy.types.Object:
    root = new_root("Willow_WallApparelDisplay", (3.7, 0.72, 2.65))
    wood = MATERIALS["dark_wood"]
    metal = MATERIALS["black_metal"]
    fixture_tools.add_box(
        "WallDisplayBack",
        (3.6, 0.14, 2.45),
        (0, 0.27, 1.26),
        wood,
        root,
        bevel=0.022,
    )
    for groove in range(1, 13):
        z = 0.18 + groove * 0.18
        fixture_tools.add_box(
            f"SlatwallGroove_{groove:02d}",
            (3.48, 0.03, 0.024),
            (0, 0.188, z),
            MATERIALS["black_metal"],
            root,
            bevel=0.003,
        )
    fixture_tools.add_box(
        "WallDisplayHeader",
        (3.7, 0.46, 0.18),
        (0, 0.18, 2.52),
        MATERIALS["wood"],
        root,
        bevel=0.035,
    )
    for x in (-1.12, 0.0, 1.12):
        fixture_tools.add_tube_between(
            f"DisplayArm_{x:+.0f}",
            (x, 0.12, 2.22),
            (x, -0.25, 2.22),
            0.026,
            metal,
            root,
            vertices=14,
        )
    add_jacket(
        "WallJacketLeft", (-1.12, -0.32, 1.43), MATERIALS["forest"], root, scale=0.93
    )
    add_jacket(
        "WallJacketCentre",
        (0.0, -0.32, 1.43),
        MATERIALS["rust"],
        root,
        scale=0.93,
        pocket_style=1,
    )
    add_jacket(
        "WallJacketRight",
        (1.12, -0.32, 1.43),
        MATERIALS["denim"],
        root,
        scale=0.93,
        pocket_style=2,
    )
    fixture_tools.add_box(
        "DisplayBench",
        (3.38, 0.58, 0.12),
        (0, -0.02, 0.43),
        MATERIALS["wood"],
        root,
        bevel=0.035,
    )
    for x in (-1.5, -0.5, 0.5, 1.5):
        fixture_tools.add_box(
            f"DisplayBenchSupport_{x:+.1f}",
            (0.09, 0.48, 0.55),
            (x, 0.02, 0.2),
            wood,
            root,
            bevel=0.014,
        )
    return root


def build_folding_table() -> bpy.types.Object:
    root = new_root("Willow_FoldedClothingTable", (2.65, 1.25, 1.38))
    wood = MATERIALS["wood"]
    metal = MATERIALS["black_metal"]
    fixture_tools.add_box(
        "DisplayTableTop", (2.6, 1.15, 0.12), (0, 0, 0.9), wood, root, bevel=0.045
    )
    fixture_tools.add_box(
        "DisplayLowerShelf",
        (2.34, 0.94, 0.09),
        (0, 0, 0.34),
        MATERIALS["dark_wood"],
        root,
        bevel=0.025,
    )
    for x in (-1.1, 1.1):
        for y in (-0.44, 0.44):
            fixture_tools.add_tube_between(
                f"DisplayLeg_{x:+.0f}_{y:+.0f}",
                (x, y, 0.08),
                (x, y, 0.85),
                0.045,
                metal,
                root,
                vertices=16,
            )
    fixture_tools.add_tube_between(
        "TableLongBrace",
        (-1.06, 0, 0.24),
        (1.06, 0, 0.24),
        0.035,
        metal,
        root,
        vertices=14,
    )
    stacks = (
        (-0.82, -0.28, MATERIALS["denim"], 4),
        (-0.22, -0.28, MATERIALS["olive"], 3),
        (0.42, -0.28, MATERIALS["fleece"], 4),
        (0.92, 0.27, MATERIALS["rust"], 3),
        (0.2, 0.27, MATERIALS["cream_fleece"], 4),
        (-0.52, 0.27, MATERIALS["forest"], 3),
    )
    for index, (x, y, material, layers) in enumerate(stacks, start=1):
        add_folded_stack(
            f"TableStack_{index}",
            (x, y, 1.0),
            material,
            root,
            layers=layers,
        )
    for index, (x, material) in enumerate(
        ((-0.7, MATERIALS["denim"]), (0.0, MATERIALS["forest"]), (0.7, MATERIALS["rust"])),
        start=1,
    ):
        add_folded_stack(
            f"LowerShelfStack_{index}",
            (x, 0, 0.43),
            material,
            root,
            layers=2,
            width=0.5,
            depth=0.38,
        )
    return root


def build_boot_cubby() -> bpy.types.Object:
    root = new_root("Willow_BootCubby", (3.15, 0.62, 2.55))
    wood = MATERIALS["dark_wood"]
    fixture_tools.add_box(
        "BootCubbyBack", (3.08, 0.12, 2.42), (0, 0.24, 1.25), wood, root, bevel=0.025
    )
    fixture_tools.add_box(
        "BootCubbyTop", (3.14, 0.61, 0.13), (0, 0, 2.46), MATERIALS["wood"], root, bevel=0.032
    )
    fixture_tools.add_box(
        "BootCubbyBase", (3.14, 0.61, 0.16), (0, 0, 0.12), MATERIALS["wood"], root, bevel=0.03
    )
    for x in (-1.5, -0.5, 0.5, 1.5):
        fixture_tools.add_box(
            f"BootCubbyDivider_{x:+.1f}",
            (0.09, 0.58, 2.25),
            (x, 0, 1.27),
            wood,
            root,
            bevel=0.012,
        )
    for index, z in enumerate((0.82, 1.52, 2.2), start=1):
        fixture_tools.add_box(
            f"BootCubbyShelf_{index}",
            (3.04, 0.58, 0.09),
            (0, 0, z),
            MATERIALS["wood"],
            root,
            bevel=0.014,
        )
        fixture_tools.add_box(
            f"BootCubbyShelfLip_{index}",
            (3.04, 0.06, 0.08),
            (0, -0.3, z + 0.06),
            MATERIALS["brass"],
            root,
            bevel=0.008,
        )
    return root


def add_backpack(
    prefix: str,
    center: tuple[float, float, float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    scale: float = 1.0,
) -> None:
    x, y, z = center
    fixture_tools.add_rounded_rect_prism(
        f"{prefix}_Body",
        0.56 * scale,
        0.24 * scale,
        0.82 * scale,
        0.1 * scale,
        (x, y, z),
        material,
        root,
        corner_segments=8,
        bevel=0.015,
    )
    fixture_tools.add_rounded_rect_prism(
        f"{prefix}_TopFlap",
        0.53 * scale,
        0.29 * scale,
        0.2 * scale,
        0.09 * scale,
        (x, y - 0.02 * scale, z + 0.38 * scale),
        MATERIALS["leather"],
        root,
        corner_segments=8,
        bevel=0.014,
    )
    fixture_tools.add_rounded_rect_prism(
        f"{prefix}_FrontPocket",
        0.4 * scale,
        0.13 * scale,
        0.27 * scale,
        0.065 * scale,
        (x, y - 0.18 * scale, z - 0.12 * scale),
        material,
        root,
        corner_segments=7,
        bevel=0.012,
    )
    for side in (-1.0, 1.0):
        side_x = x + side * 0.32 * scale
        fixture_tools.add_rounded_rect_prism(
            f"{prefix}_SidePocket_{side:+.0f}",
            0.16 * scale,
            0.2 * scale,
            0.28 * scale,
            0.045 * scale,
            (side_x, y, z - 0.1 * scale),
            MATERIALS["leather"],
            root,
            corner_segments=6,
            bevel=0.01,
        )
        fixture_tools.add_curve_tube(
            f"{prefix}_ShoulderStrap_{side:+.0f}",
            (
                (x + side * 0.17 * scale, y + 0.11 * scale, z + 0.32 * scale),
                (x + side * 0.27 * scale, y + 0.2 * scale, z + 0.08 * scale),
                (x + side * 0.23 * scale, y + 0.2 * scale, z - 0.31 * scale),
            ),
            0.028 * scale,
            MATERIALS["leather"],
            root,
            resolution=3,
            bevel_resolution=2,
        )
        fixture_tools.add_box(
            f"{prefix}_Buckle_{side:+.0f}",
            (0.08 * scale, 0.035 * scale, 0.07 * scale),
            (x + side * 0.16 * scale, y - 0.17 * scale, z + 0.26 * scale),
            MATERIALS["brass"],
            root,
            bevel=0.008,
        )
    fixture_tools.add_curve_tube(
        f"{prefix}_CarryHandle",
        (
            (x - 0.12 * scale, y, z + 0.46 * scale),
            (x, y, z + 0.58 * scale),
            (x + 0.12 * scale, y, z + 0.46 * scale),
        ),
        0.022 * scale,
        MATERIALS["leather"],
        root,
        resolution=3,
        bevel_resolution=2,
    )


def build_backpack_display() -> bpy.types.Object:
    root = new_root("Willow_BackpackDisplay", (2.7, 0.75, 2.55))
    fixture_tools.add_box(
        "BackpackDisplayBack",
        (2.62, 0.12, 2.45),
        (0, 0.29, 1.26),
        MATERIALS["dark_wood"],
        root,
        bevel=0.026,
    )
    for groove in range(10):
        z = 0.3 + groove * 0.21
        fixture_tools.add_box(
            f"BackpackSlat_{groove + 1:02d}",
            (2.5, 0.025, 0.025),
            (0, 0.215, z),
            MATERIALS["black_metal"],
            root,
            bevel=0.003,
        )
    backpack_specs = (
        (-0.83, 1.4, MATERIALS["forest"], 0.9),
        (0.0, 1.47, MATERIALS["denim"], 1.0),
        (0.83, 1.4, MATERIALS["rust"], 0.9),
    )
    for index, (x, z, material, scale) in enumerate(backpack_specs, start=1):
        fixture_tools.add_tube_between(
            f"BackpackHook_{index}",
            (x, 0.19, 2.25),
            (x, -0.18, 2.25),
            0.023,
            MATERIALS["black_metal"],
            root,
            vertices=14,
        )
        add_backpack(
            f"Backpack_{index}", (x, -0.25, z), material, root, scale=scale
        )
    fixture_tools.add_box(
        "BackpackBottomShelf",
        (2.5, 0.68, 0.11),
        (0, 0, 0.42),
        MATERIALS["wood"],
        root,
        bevel=0.028,
    )
    return root


def build_checkout_counter() -> bpy.types.Object:
    root = new_root("Willow_CheckoutCounter", (3.4, 1.05, 1.18))
    wood = MATERIALS["dark_wood"]
    fixture_tools.add_box(
        "CounterCarcass", (3.25, 0.88, 0.92), (0, 0, 0.51), wood, root, bevel=0.045
    )
    fixture_tools.add_box(
        "CounterTop",
        (3.42, 1.0, 0.12),
        (0, -0.015, 1.02),
        MATERIALS["wood"],
        root,
        bevel=0.045,
    )
    fixture_tools.add_box(
        "CounterToeKick",
        (3.18, 0.12, 0.16),
        (0, -0.45, 0.13),
        MATERIALS["black_metal"],
        root,
        bevel=0.016,
    )
    for index, x in enumerate((-1.15, -0.38, 0.38, 1.15), start=1):
        fixture_tools.add_box(
            f"CounterFrontPanel_{index}",
            (0.64, 0.05, 0.67),
            (x, -0.455, 0.55),
            MATERIALS["forest"],
            root,
            bevel=0.035,
        )
        fixture_tools.add_box(
            f"CounterPanelInlay_{index}",
            (0.5, 0.03, 0.51),
            (x, -0.487, 0.55),
            MATERIALS["dark_wood"],
            root,
            bevel=0.025,
        )
    for index, x in enumerate((-0.95, 0.0, 0.95), start=1):
        fixture_tools.add_box(
            f"CounterRearDrawer_{index}",
            (0.76, 0.05, 0.22),
            (x, 0.455, 0.75),
            MATERIALS["wood"],
            root,
            bevel=0.018,
        )
        fixture_tools.add_tube_between(
            f"CounterDrawerPull_{index}",
            (x - 0.1, 0.493, 0.75),
            (x + 0.1, 0.493, 0.75),
            0.012,
            MATERIALS["brass"],
            root,
            vertices=12,
        )
    fixture_tools.add_box(
        "CounterBagShelf",
        (2.7, 0.54, 0.08),
        (0, 0.12, 0.34),
        MATERIALS["wood"],
        root,
        bevel=0.018,
    )
    return root


def build_fitting_bench() -> bpy.types.Object:
    root = new_root("Willow_FittingBench", (1.75, 0.56, 1.85))
    fixture_tools.add_rounded_rect_prism(
        "FittingBenchSeat",
        1.7,
        0.52,
        0.14,
        0.08,
        (0, 0, 0.52),
        MATERIALS["leather"],
        root,
        corner_segments=8,
        bevel=0.014,
    )
    for x in (-0.7, 0.7):
        for y in (-0.19, 0.19):
            fixture_tools.add_tube_between(
                f"FittingBenchLeg_{x:+.0f}_{y:+.0f}",
                (x, y, 0.08),
                (x, y, 0.46),
                0.035,
                MATERIALS["black_metal"],
                root,
                vertices=16,
            )
    fixture_tools.add_box(
        "FittingMirrorBack",
        (1.38, 0.08, 1.18),
        (0, 0.23, 1.25),
        MATERIALS["dark_wood"],
        root,
        bevel=0.04,
    )
    fixture_tools.add_box(
        "FittingMirror",
        (1.18, 0.025, 0.98),
        (0, 0.181, 1.25),
        MATERIALS["black_metal"],
        root,
        bevel=0.025,
    )
    for x in (-0.67, 0.67):
        fixture_tools.add_tube_between(
            f"FittingHookStem_{x:+.0f}",
            (x, 0.22, 1.62),
            (x, -0.04, 1.62),
            0.024,
            MATERIALS["brass"],
            root,
            vertices=14,
        )
        fixture_tools.add_uv_sphere(
            f"FittingHookTip_{x:+.0f}",
            0.045,
            (x, -0.07, 1.62),
            MATERIALS["brass"],
            root,
            segments=16,
            rings=8,
        )
    return root


BUILDERS: tuple[tuple[str, Callable[[], bpy.types.Object]], ...] = (
    ("willow_clothing_rack.glb", build_clothing_rack),
    ("willow_wall_apparel_display.glb", build_wall_apparel_display),
    ("willow_folded_clothing_table.glb", build_folding_table),
    ("willow_boot_cubby.glb", build_boot_cubby),
    ("willow_backpack_display.glb", build_backpack_display),
    ("willow_checkout_counter.glb", build_checkout_counter),
    ("willow_fitting_bench.glb", build_fitting_bench),
)


def validate_glb(path: Path) -> None:
    fixture_tools.clear_scene_objects()
    bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"{path.name} imported without mesh geometry")
    minimum, maximum = fixture_tools.world_bounds(meshes)
    dimensions = maximum - minimum
    if min(dimensions) <= 0.02 or max(dimensions) >= 6.0:
        raise RuntimeError(f"{path.name} has implausible dimensions {tuple(dimensions)}")
    materials = {
        slot.material
        for mesh in meshes
        for slot in mesh.material_slots
        if slot.material is not None
    }
    images = {
        node.image.name
        for material in materials
        if material.use_nodes
        for node in material.node_tree.nodes
        if node.type == "TEX_IMAGE" and node.image is not None
    }
    if not materials or not images:
        raise RuntimeError(f"{path.name} is missing textured production materials")
    print(
        "WILLOW_VALIDATE "
        f"file={path.name} "
        f"dimensions_m=({dimensions.x:.3f},{dimensions.y:.3f},{dimensions.z:.3f}) "
        f"meshes={len(meshes)} materials={len(materials)} embedded_images={len(images)}"
    )


def main() -> None:
    require_textures()
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    fixture_tools.OUTPUT_DIR = OUTPUT_DIR
    fixture_tools.configure_scene()
    global MATERIALS
    MATERIALS = create_materials()
    exported: list[Path] = []
    for filename, builder in BUILDERS:
        fixture_tools.clear_scene_objects()
        exported.append(fixture_tools.export_fixture(builder(), filename))
    for path in exported:
        validate_glb(path)
    print(f"WILLOW_COMPLETE files={len(exported)} output={OUTPUT_DIR}")


if __name__ == "__main__":
    main()
