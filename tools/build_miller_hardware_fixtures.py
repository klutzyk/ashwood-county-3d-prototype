"""Build the project-owned Miller Hardware production fixture pack.

Run with Blender 4.4 or newer:

    blender.exe --background --python tools/build_miller_hardware_fixtures.py

The pack uses metre-scale hard-surface construction, bevelled silhouettes,
authored retail proportions, and previously downloaded CC0 PBR wood and metal
maps.  Output GLBs are intended to be instanced repeatedly in Godot.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import Callable

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_silver_spoon_fixtures as fixture_tools


REPO_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = (
    REPO_ROOT / "assets" / "environment" / "buildings" / "MillerHardware" / "fixtures"
)
DINER_MATERIALS = (
    REPO_ROOT / "assets" / "third_party" / "interiors" / "diner" / "materials"
)
METAL_ROOT = DINER_MATERIALS / "ambientcg" / "Metal049A"
WOOD_ROOT = DINER_MATERIALS / "poly_haven" / "wood_table_worn"

METAL_COLOR = METAL_ROOT / "Metal049A_1K-JPG_Color.jpg"
METAL_NORMAL = METAL_ROOT / "Metal049A_1K-JPG_NormalGL.jpg"
METAL_ROUGHNESS = METAL_ROOT / "Metal049A_1K-JPG_Roughness.jpg"
WOOD_COLOR = WOOD_ROOT / "wood_table_worn_diff_1k.jpg"
WOOD_NORMAL = WOOD_ROOT / "wood_table_worn_nor_gl_1k.jpg"
WOOD_ARM = WOOD_ROOT / "wood_table_worn_arm_1k.jpg"

MATERIALS: dict[str, bpy.types.Material] = {}


def require_textures() -> None:
    expected = (
        METAL_COLOR,
        METAL_NORMAL,
        METAL_ROUGHNESS,
        WOOD_COLOR,
        WOOD_NORMAL,
        WOOD_ARM,
    )
    missing = [path for path in expected if not path.is_file()]
    if missing:
        details = "\n".join(f"  - {path}" for path in missing)
        raise FileNotFoundError(f"Miller fixture source maps are missing:\n{details}")


def add_color_texture(
    tree: bpy.types.NodeTree,
    shader: bpy.types.Node,
    texture_path: Path,
) -> None:
    """Connect a base-colour map using the glTF exporter's supported path."""
    texture = tree.nodes.new("ShaderNodeTexImage")
    texture.image = fixture_tools.load_image(texture_path)
    texture.location = (-620, 250)
    tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])


def textured_metal(
    name: str,
    tint: tuple[float, float, float, float],
    *,
    metallic: float,
    roughness: float,
) -> bpy.types.Material:
    material, tree, shader = fixture_tools.make_principled_material(
        name, tint, metallic=metallic, roughness=roughness
    )
    # Keep painted-enamel colours as the glTF base-colour factor.  Blender's
    # exporter cannot represent the previous MixRGB tint chain faithfully,
    # which washed every department fixture to white in Godot.  The authored
    # roughness and normal maps still supply the worn PBR surface response.
    rough = tree.nodes.new("ShaderNodeTexImage")
    rough.image = fixture_tools.load_image(METAL_ROUGHNESS, non_color=True)
    rough.location = (-600, -10)
    tree.links.new(rough.outputs["Color"], shader.inputs["Roughness"])
    fixture_tools.attach_normal_map(
        tree, shader, METAL_NORMAL, strength=0.24, location=(-610, -330)
    )
    return material


def textured_wood(
    name: str,
    tint: tuple[float, float, float, float],
    *,
    roughness: float,
) -> bpy.types.Material:
    material, tree, shader = fixture_tools.make_principled_material(
        name, tint, roughness=roughness
    )
    add_color_texture(tree, shader, WOOD_COLOR)
    fixture_tools.attach_arm_map(tree, shader, WOOD_ARM, use_metallic=False)
    fixture_tools.attach_normal_map(
        tree, shader, WOOD_NORMAL, strength=0.48, location=(-610, -330)
    )
    return material


def create_materials() -> dict[str, bpy.types.Material]:
    return {
        "steel": textured_metal(
            "Miller_Galvanized_Steel_PBR",
            (0.54, 0.56, 0.52, 1.0),
            metallic=0.64,
            roughness=0.48,
        ),
        "dark_steel": textured_metal(
            "Miller_Blackened_Steel_PBR",
            (0.075, 0.085, 0.074, 1.0),
            metallic=0.76,
            roughness=0.52,
        ),
        "green": textured_metal(
            "Miller_Forest_Enamel_PBR",
            (0.12, 0.29, 0.17, 1.0),
            metallic=0.36,
            roughness=0.61,
        ),
        "cream": textured_metal(
            "Miller_Aged_Cream_Enamel_PBR",
            (0.68, 0.61, 0.43, 1.0),
            metallic=0.18,
            roughness=0.67,
        ),
        "red": textured_metal(
            "Miller_Safety_Red_Enamel_PBR",
            (0.50, 0.055, 0.028, 1.0),
            metallic=0.28,
            roughness=0.58,
        ),
        "blue": textured_metal(
            "Miller_Paint_Blue_Enamel_PBR",
            (0.07, 0.18, 0.42, 1.0),
            metallic=0.24,
            roughness=0.55,
        ),
        "yellow": textured_metal(
            "Miller_Safety_Yellow_Enamel_PBR",
            (0.62, 0.43, 0.055, 1.0),
            metallic=0.20,
            roughness=0.62,
        ),
        "wood": textured_wood(
            "Miller_Worn_Oak_PBR", (0.62, 0.42, 0.24, 1.0), roughness=0.65
        ),
        "dark_wood": textured_wood(
            "Miller_Dark_Oak_PBR", (0.29, 0.16, 0.075, 1.0), roughness=0.72
        ),
    }


def new_root(name: str, dimensions: tuple[float, float, float]) -> bpy.types.Object:
    root = fixture_tools.new_root(name, dimensions)
    root["fixture_pack"] = "Miller Hardware"
    return root


def add_price_rail(
    root: bpy.types.Object,
    name: str,
    location: tuple[float, float, float],
    length: float,
    rotation_z: float = 0.0,
) -> None:
    fixture_tools.add_box(
        name,
        (length, 0.035, 0.065),
        location,
        MATERIALS["cream"],
        root,
        bevel=0.006,
        rotation=(0.0, 0.0, rotation_z),
    )


def add_packaged_carton(
    root: bpy.types.Object,
    name: str,
    x: float,
    y: float,
    shelf_z: float,
    dimensions: tuple[float, float, float],
    body: bpy.types.Material,
    accent: bpy.types.Material,
    label: bpy.types.Material,
    *,
    yaw_degrees: float = 0.0,
) -> None:
    width, depth, height = dimensions
    direction = 1.0 if y > 0 else -1.0
    centre_z = shelf_z + 0.045 + height * 0.5
    fixture_tools.add_box(
        f"{name}_Carton",
        dimensions,
        (x, y, centre_z),
        body,
        root,
        bevel=0.018,
        rotation=(0, 0, math.radians(yaw_degrees)),
    )
    fixture_tools.add_box(
        f"{name}_Brand_Band",
        (width * 0.88, 0.018, height * 0.25),
        (x, y + direction * (depth * 0.5 + 0.012), centre_z + height * 0.16),
        accent,
        root,
        bevel=0.004,
        rotation=(0, 0, math.radians(yaw_degrees)),
    )
    fixture_tools.add_box(
        f"{name}_Product_Label",
        (width * 0.58, 0.021, height * 0.20),
        (x, y + direction * (depth * 0.5 + 0.014), centre_z - height * 0.15),
        label,
        root,
        bevel=0.004,
        rotation=(0, 0, math.radians(yaw_degrees)),
    )


def add_gondola_stock(root: bpy.types.Object, variant: str) -> None:
    shelves = (0.34, 0.78, 1.22, 1.66)
    side_positions = (-0.49, 0.49)
    palette = (
        MATERIALS["green"],
        MATERIALS["red"],
        MATERIALS["blue"],
        MATERIALS["yellow"],
        MATERIALS["cream"],
    )
    dark = MATERIALS["dark_steel"]
    cream = MATERIALS["cream"]

    if variant == "tools":
        x_positions = (-1.53, -1.02, -0.51, 0.0, 0.51, 1.02, 1.53)
        for side_index, y in enumerate(side_positions):
            for shelf_index, shelf_z in enumerate(shelves):
                for slot_index, x in enumerate(x_positions):
                    if (side_index * 7 + shelf_index * 3 + slot_index) % 13 == 0:
                        continue
                    body = palette[(slot_index + shelf_index * 2 + side_index) % len(palette)]
                    accent = palette[(slot_index * 2 + shelf_index + 2) % len(palette)]
                    width = 0.40 if slot_index % 3 else 0.34
                    height = 0.29 if shelf_index % 2 else 0.25
                    add_packaged_carton(
                        root,
                        f"Tools_S{side_index+1}_R{shelf_index+1}_P{slot_index+1}",
                        x,
                        y,
                        shelf_z,
                        (width, 0.24, height),
                        body,
                        accent,
                        cream,
                        yaw_degrees=((slot_index + shelf_index) % 3 - 1) * 1.2,
                    )
                    if (slot_index + shelf_index) % 4 == 0:
                        fixture_tools.add_box(
                            f"Tools_S{side_index+1}_R{shelf_index+1}_P{slot_index+1}_Handle",
                            (width * 0.38, 0.028, 0.055),
                            (x, y, shelf_z + height + 0.075),
                            dark,
                            root,
                            bevel=0.008,
                        )
    elif variant == "fasteners":
        x_positions = (-1.55, -1.16, -0.77, -0.38, 0.0, 0.38, 0.77, 1.16, 1.55)
        for side_index, y in enumerate(side_positions):
            for shelf_index, shelf_z in enumerate(shelves):
                for slot_index, x in enumerate(x_positions):
                    if (slot_index + shelf_index * 2 + side_index) % 17 == 0:
                        continue
                    body = (
                        MATERIALS["green"]
                        if (slot_index + shelf_index) % 3
                        else MATERIALS["yellow"]
                    )
                    accent = palette[(slot_index + shelf_index + side_index + 1) % 4]
                    add_packaged_carton(
                        root,
                        f"Fasteners_S{side_index+1}_R{shelf_index+1}_P{slot_index+1}",
                        x,
                        y,
                        shelf_z,
                        (0.30, 0.25, 0.22),
                        body,
                        accent,
                        cream,
                        yaw_degrees=((slot_index + side_index) % 3 - 1) * 0.8,
                    )
                    fixture_tools.add_box(
                        f"Fasteners_S{side_index+1}_R{shelf_index+1}_P{slot_index+1}_Lid",
                        (0.32, 0.27, 0.028),
                        (x, y, shelf_z + 0.285),
                        dark,
                        root,
                        bevel=0.006,
                    )
    elif variant == "general":
        x_positions = (-1.47, -0.88, -0.29, 0.29, 0.88, 1.47)
        for side_index, y in enumerate(side_positions):
            direction = 1.0 if y > 0 else -1.0
            for shelf_index, shelf_z in enumerate(shelves):
                for slot_index, x in enumerate(x_positions):
                    material = palette[(slot_index + shelf_index * 2 + side_index) % len(palette)]
                    if shelf_index % 2 == 0:
                        fixture_tools.add_cylinder(
                            f"General_S{side_index+1}_R{shelf_index+1}_Can{slot_index+1}",
                            0.18,
                            0.28,
                            (x, y, shelf_z + 0.19),
                            material,
                            root,
                            vertices=24,
                            bevel=0.010,
                        )
                        fixture_tools.add_torus(
                            f"General_S{side_index+1}_R{shelf_index+1}_Can{slot_index+1}_Rim",
                            0.17,
                            0.012,
                            (x, y, shelf_z + 0.34),
                            dark,
                            root,
                            major_segments=20,
                            minor_segments=8,
                        )
                        fixture_tools.add_box(
                            f"General_S{side_index+1}_R{shelf_index+1}_Can{slot_index+1}_Label",
                            (0.24, 0.018, 0.10),
                            (x, y + direction * 0.188, shelf_z + 0.19),
                            cream,
                            root,
                            bevel=0.004,
                        )
                    else:
                        add_packaged_carton(
                            root,
                            f"General_S{side_index+1}_R{shelf_index+1}_P{slot_index+1}",
                            x,
                            y,
                            shelf_z,
                            (0.43, 0.25, 0.27),
                            material,
                            palette[(slot_index + 2) % len(palette)],
                            cream,
                            yaw_degrees=((slot_index + shelf_index) % 3 - 1) * 1.0,
                        )
    else:
        raise ValueError(f"Unsupported Miller gondola stock variant: {variant}")


def build_gondola_aisle(stock_variant: str) -> bpy.types.Object:
    root = new_root(
        f"Miller_Gondola_{stock_variant.title()}",
        (3.8, 1.15, 2.28),
    )
    root["stock_variant"] = stock_variant
    steel = MATERIALS["steel"]
    green = MATERIALS["green"]
    cream = MATERIALS["cream"]
    dark = MATERIALS["dark_steel"]

    fixture_tools.add_box(
        "Weighted_Platform", (3.74, 1.10, 0.12), (0, 0, 0.08), dark, root, bevel=0.025
    )
    fixture_tools.add_box(
        "Central_Back_Panel", (3.62, 0.09, 1.90), (0, 0, 1.10), green, root, bevel=0.018
    )
    for index, x in enumerate((-1.78, -0.89, 0.0, 0.89, 1.78), start=1):
        fixture_tools.add_box(
            f"Perforated_Upright_{index}",
            (0.055, 0.14, 2.10),
            (x, 0, 1.12),
            dark,
            root,
            bevel=0.009,
        )
        for slot_index, z in enumerate((0.36, 0.67, 0.98, 1.29, 1.60, 1.91), start=1):
            fixture_tools.add_box(
                f"Upright_{index}_Slot_{slot_index}",
                (0.028, 0.19, 0.055),
                (x, 0, z),
                cream,
                root,
                bevel=0.004,
            )
    for side_index, side in enumerate((-1.0, 1.0), start=1):
        for shelf_index, z in enumerate((0.34, 0.78, 1.22, 1.66), start=1):
            y = side * (0.31 + shelf_index * 0.035)
            fixture_tools.add_box(
                f"Side_{side_index}_Shelf_{shelf_index}",
                (3.70, 0.52, 0.055),
                (0, y, z),
                steel,
                root,
                bevel=0.010,
            )
            fixture_tools.add_box(
                f"Side_{side_index}_Shelf_Lip_{shelf_index}",
                (3.70, 0.045, 0.085),
                (0, side * (abs(y) + 0.25), z + 0.055),
                cream,
                root,
                bevel=0.006,
            )
        fixture_tools.add_box(
            f"Side_{side_index}_Top_Cap",
            (3.72, 0.46, 0.08),
            (0, side * 0.31, 2.11),
            green,
            root,
            bevel=0.012,
        )
    add_gondola_stock(root, stock_variant)
    return root


def build_checkout_counter() -> bpy.types.Object:
    root = new_root("Miller_Checkout_Counter", (3.65, 1.08, 1.14))
    wood = MATERIALS["wood"]
    dark_wood = MATERIALS["dark_wood"]
    steel = MATERIALS["steel"]
    green = MATERIALS["green"]
    brass = MATERIALS["yellow"]

    fixture_tools.add_box(
        "Recessed_Plith", (3.42, 0.83, 0.18), (0, 0.07, 0.11), dark_wood, root, bevel=0.025
    )
    fixture_tools.add_box(
        "Cabinet_Carcass", (3.52, 0.90, 0.73), (0, 0, 0.52), wood, root, bevel=0.035
    )
    fixture_tools.add_box(
        "Worktop", (3.68, 1.06, 0.105), (0, 0, 0.94), green, root, bevel=0.035
    )
    for panel_index, x in enumerate((-1.28, -0.43, 0.43, 1.28), start=1):
        fixture_tools.add_box(
            f"Customer_Face_Panel_{panel_index}",
            (0.72, 0.035, 0.52),
            (x, -0.468, 0.53),
            dark_wood,
            root,
            bevel=0.018,
        )
        fixture_tools.add_box(
            f"Panel_Inset_{panel_index}",
            (0.60, 0.025, 0.40),
            (x, -0.492, 0.53),
            wood,
            root,
            bevel=0.012,
        )
    fixture_tools.add_box(
        "Register_Riser", (0.78, 0.58, 0.12), (-1.12, 0.08, 1.03), steel, root, bevel=0.025
    )
    fixture_tools.add_box(
        "Bagging_Well", (0.78, 0.60, 0.075), (1.16, -0.01, 1.01), steel, root, bevel=0.018
    )
    fixture_tools.add_box(
        "Receipt_Rail", (0.62, 0.045, 0.055), (-0.34, -0.53, 1.01), brass, root, bevel=0.008
    )
    for drawer_index, x in enumerate((-0.75, 0.0, 0.75), start=1):
        fixture_tools.add_box(
            f"Staff_Drawer_{drawer_index}",
            (0.58, 0.045, 0.18),
            (x, 0.47, 0.69),
            green,
            root,
            bevel=0.012,
        )
        fixture_tools.add_cylinder(
            f"Drawer_Pull_{drawer_index}",
            0.025,
            0.13,
            (x, 0.512, 0.69),
            brass,
            root,
            vertices=16,
            rotation=(math.pi / 2, 0, 0),
        )
    return root


def build_pegboard_tool_wall() -> bpy.types.Object:
    root = new_root("Miller_Pegboard_Tool_Wall", (4.25, 0.58, 2.72))
    wood = MATERIALS["wood"]
    dark_wood = MATERIALS["dark_wood"]
    dark = MATERIALS["dark_steel"]
    steel = MATERIALS["steel"]
    green = MATERIALS["green"]

    fixture_tools.add_box(
        "Heavy_Base_Cabinet", (4.20, 0.54, 0.64), (0, 0, 0.34), dark_wood, root, bevel=0.035
    )
    fixture_tools.add_box(
        "Pegboard_Back", (4.08, 0.08, 1.82), (0, 0.13, 1.59), wood, root, bevel=0.018
    )
    for row in range(9):
        for column in range(21):
            x = -1.9 + column * 0.19
            z = 0.83 + row * 0.19
            fixture_tools.add_cylinder(
                f"Peg_{row:02d}_{column:02d}",
                0.011,
                0.105,
                (x, 0.07, z),
                dark,
                root,
                vertices=8,
                rotation=(math.pi / 2, 0, 0),
                bevel=0.001,
            )
    for hook_index, (x, z, length) in enumerate(
        (
            (-1.55, 2.15, 0.30),
            (-0.95, 1.75, 0.26),
            (-0.32, 2.28, 0.34),
            (0.34, 1.55, 0.28),
            (0.95, 2.02, 0.32),
            (1.52, 1.70, 0.27),
        ),
        start=1,
    ):
        fixture_tools.add_tube_between(
            f"Display_Hook_{hook_index}_Stem",
            (x, 0.02, z),
            (x, -length, z),
            0.018,
            steel,
            root,
            vertices=12,
        )
        fixture_tools.add_tube_between(
            f"Display_Hook_{hook_index}_Tip",
            (x, -length, z),
            (x, -length, z + 0.09),
            0.018,
            steel,
            root,
            vertices=12,
        )
    fixture_tools.add_box(
        "Header", (4.24, 0.22, 0.40), (0, 0.05, 2.57), green, root, bevel=0.025
    )
    for door_index, x in enumerate((-1.55, -0.52, 0.52, 1.55), start=1):
        fixture_tools.add_box(
            f"Base_Door_{door_index}",
            (0.90, 0.04, 0.45),
            (x, -0.286, 0.36),
            wood,
            root,
            bevel=0.014,
        )
        fixture_tools.add_cylinder(
            f"Base_Door_Knob_{door_index}",
            0.025,
            0.08,
            (x + 0.31, -0.33, 0.38),
            steel,
            root,
            vertices=16,
            rotation=(math.pi / 2, 0, 0),
        )
    return root


def add_paint_can(
    root: bpy.types.Object,
    name: str,
    location: tuple[float, float, float],
    body_material: bpy.types.Material,
) -> None:
    fixture_tools.add_cylinder(
        f"{name}_Body",
        0.145,
        0.29,
        location,
        body_material,
        root,
        vertices=24,
        bevel=0.008,
    )
    fixture_tools.add_torus(
        f"{name}_Top_Rim",
        0.136,
        0.010,
        (location[0], location[1], location[2] + 0.15),
        MATERIALS["steel"],
        root,
        major_segments=24,
        minor_segments=8,
    )
    fixture_tools.add_torus(
        f"{name}_Bottom_Rim",
        0.136,
        0.009,
        (location[0], location[1], location[2] - 0.15),
        MATERIALS["steel"],
        root,
        major_segments=24,
        minor_segments=8,
    )
    fixture_tools.add_curve_tube(
        f"{name}_Handle",
        (
            (location[0] - 0.12, location[1], location[2] + 0.04),
            (location[0], location[1] - 0.08, location[2] + 0.18),
            (location[0] + 0.12, location[1], location[2] + 0.04),
        ),
        0.008,
        MATERIALS["steel"],
        root,
        bevel_resolution=2,
        resolution=2,
    )


def build_paint_display() -> bpy.types.Object:
    root = new_root("Miller_Paint_Display", (3.62, 0.72, 2.58))
    steel = MATERIALS["steel"]
    dark = MATERIALS["dark_steel"]
    cream = MATERIALS["cream"]
    green = MATERIALS["green"]
    colors = (MATERIALS["cream"], MATERIALS["blue"], MATERIALS["red"], MATERIALS["yellow"])

    fixture_tools.add_box(
        "Weighted_Base", (3.58, 0.70, 0.15), (0, 0, 0.09), dark, root, bevel=0.024
    )
    fixture_tools.add_box(
        "Rear_Spine", (3.48, 0.09, 2.15), (0, 0.28, 1.22), green, root, bevel=0.018
    )
    for shelf_index, z in enumerate((0.38, 0.78, 1.18, 1.58, 1.98), start=1):
        fixture_tools.add_box(
            f"Paint_Shelf_{shelf_index}",
            (3.48, 0.62, 0.055),
            (0, -0.01, z),
            steel,
            root,
            bevel=0.010,
        )
        add_price_rail(root, f"Paint_Price_Rail_{shelf_index}", (0, -0.33, z + 0.06), 3.42)
        if shelf_index < 5:
            for can_index, x in enumerate((-1.38, -0.92, -0.46, 0, 0.46, 0.92, 1.38), start=1):
                add_paint_can(
                    root,
                    f"Shelf_{shelf_index}_Can_{can_index}",
                    (x, -0.04, z + 0.19),
                    colors[(shelf_index + can_index) % len(colors)],
                )
    fixture_tools.add_box(
        "Paint_Header", (3.60, 0.28, 0.40), (0, 0.16, 2.40), cream, root, bevel=0.028
    )
    return root


def build_plumbing_rack() -> bpy.types.Object:
    root = new_root("Miller_Plumbing_Rack", (3.85, 0.90, 2.55))
    steel = MATERIALS["steel"]
    dark = MATERIALS["dark_steel"]
    green = MATERIALS["green"]
    cream = MATERIALS["cream"]

    for x in (-1.80, 1.80):
        fixture_tools.add_box(
            f"Frame_{'L' if x < 0 else 'R'}",
            (0.09, 0.76, 2.42),
            (x, 0, 1.24),
            dark,
            root,
            bevel=0.012,
        )
    for z in (0.14, 0.70, 1.26, 1.82, 2.38):
        fixture_tools.add_box(
            f"Crossbar_{z:.2f}", (3.66, 0.08, 0.08), (0, 0.28, z), dark, root, bevel=0.01
        )
        if z < 2.30:
            fixture_tools.add_box(
                f"Bin_Shelf_{z:.2f}", (3.60, 0.72, 0.055), (0, 0, z + 0.04), steel, root, bevel=0.01
            )
    for pipe_index, (y, z, radius) in enumerate(
        ((-0.24, 0.38, 0.055), (0.02, 0.92, 0.065), (-0.16, 1.46, 0.048), (0.10, 2.02, 0.075)),
        start=1,
    ):
        fixture_tools.add_tube_between(
            f"Pipe_{pipe_index}",
            (-1.52, y, z),
            (1.46, y, z),
            radius,
            cream if pipe_index % 2 else steel,
            root,
            vertices=20,
        )
        for fitting_index, x in enumerate((-1.20, -0.40, 0.40, 1.20), start=1):
            fixture_tools.add_torus(
                f"Pipe_{pipe_index}_Coupling_{fitting_index}",
                radius * 1.18,
                radius * 0.20,
                (x, y, z),
                green,
                root,
                major_segments=18,
                minor_segments=8,
                rotation=(0, math.pi / 2, 0),
            )
    return root


def build_lumber_rack() -> bpy.types.Object:
    root = new_root("Miller_Lumber_Rack", (4.4, 1.25, 2.85))
    dark = MATERIALS["dark_steel"]
    yellow = MATERIALS["yellow"]
    wood = MATERIALS["wood"]
    dark_wood = MATERIALS["dark_wood"]

    for x in (-2.05, 0, 2.05):
        fixture_tools.add_box(
            f"Vertical_Post_{x:+.2f}", (0.12, 0.13, 2.75), (x, 0.48, 1.39), dark, root, bevel=0.015
        )
        fixture_tools.add_box(
            f"Base_Foot_{x:+.2f}", (0.30, 1.12, 0.11), (x, 0, 0.08), yellow, root, bevel=0.018
        )
        for arm_index, z in enumerate((0.54, 1.10, 1.66, 2.22), start=1):
            fixture_tools.add_box(
                f"Post_{x:+.2f}_Arm_{arm_index}",
                (0.13, 1.00, 0.10),
                (x, 0, z),
                yellow,
                root,
                bevel=0.012,
            )
            fixture_tools.add_box(
                f"Post_{x:+.2f}_Stop_{arm_index}",
                (0.14, 0.10, 0.24),
                (x, -0.47, z + 0.09),
                dark,
                root,
                bevel=0.009,
            )
    for tier_index, z in enumerate((0.63, 1.19, 1.75, 2.31), start=1):
        for board_index, y in enumerate((-0.30, -0.08, 0.14, 0.36), start=1):
            fixture_tools.add_box(
                f"Tier_{tier_index}_Board_{board_index}",
                (4.16, 0.16, 0.095),
                (0, y, z),
                wood if board_index % 2 else dark_wood,
                root,
                bevel=0.012,
                rotation=(0, 0, math.radians((board_index - 2.5) * 0.35)),
            )
    fixture_tools.add_box(
        "Lumber_Header", (4.36, 0.24, 0.34), (0, 0.47, 2.66), yellow, root, bevel=0.025
    )
    return root


def build_warehouse_shelf() -> bpy.types.Object:
    """Build a stocked, metre-scale back-room rack with readable construction."""
    root = new_root("Miller_Stocked_Warehouse_Shelf", (3.75, 0.78, 2.56))
    dark = MATERIALS["dark_steel"]
    steel = MATERIALS["steel"]
    green = MATERIALS["green"]
    cream = MATERIALS["cream"]
    red = MATERIALS["red"]
    blue = MATERIALS["blue"]
    yellow = MATERIALS["yellow"]
    wood = MATERIALS["wood"]

    for x in (-1.76, 1.76):
        for y in (-0.31, 0.31):
            fixture_tools.add_box(
                f"Upright_{x:+.2f}_{y:+.2f}",
                (0.075, 0.075, 2.48),
                (x, y, 1.25),
                dark,
                root,
                bevel=0.010,
            )
    for tier_index, z in enumerate((0.18, 0.76, 1.34, 1.92, 2.46), start=1):
        fixture_tools.add_box(
            f"Shelf_{tier_index}",
            (3.62, 0.72, 0.065),
            (0, 0, z),
            steel,
            root,
            bevel=0.010,
        )
        fixture_tools.add_box(
            f"Shelf_{tier_index}_Front_Rail",
            (3.66, 0.055, 0.12),
            (0, -0.35, z),
            green,
            root,
            bevel=0.009,
        )
    fixture_tools.add_tube_between(
        "Rear_Crossbrace_A",
        (-1.68, 0.34, 0.24),
        (1.68, 0.34, 2.38),
        0.018,
        yellow,
        root,
        vertices=12,
    )
    fixture_tools.add_tube_between(
        "Rear_Crossbrace_B",
        (1.68, 0.34, 0.24),
        (-1.68, 0.34, 2.38),
        0.018,
        yellow,
        root,
        vertices=12,
    )

    # Individually bevelled totes with separate lids keep the silhouette from
    # reading as a stack of placeholder blocks.
    for index, (x, z, material) in enumerate(
        (
            (-1.28, 0.42, green),
            (-0.42, 0.42, blue),
            (0.48, 0.42, red),
            (1.28, 0.42, cream),
            (-1.18, 1.00, cream),
            (-0.28, 1.00, green),
            (0.70, 1.00, blue),
            (1.35, 1.00, red),
        ),
        start=1,
    ):
        width = 0.66 if index % 3 else 0.54
        fixture_tools.add_box(
            f"Storage_Tote_{index}",
            (width, 0.50, 0.34),
            (x, -0.01, z),
            material,
            root,
            bevel=0.055,
        )
        fixture_tools.add_box(
            f"Storage_Tote_{index}_Lid",
            (width + 0.06, 0.54, 0.055),
            (x, -0.01, z + 0.19),
            dark,
            root,
            bevel=0.018,
        )
        fixture_tools.add_box(
            f"Storage_Tote_{index}_Label",
            (width * 0.52, 0.018, 0.11),
            (x, -0.27, z + 0.015),
            cream,
            root,
            bevel=0.006,
        )

    for index, x in enumerate((-1.42, -0.86, -0.30, 0.26, 0.82, 1.38), start=1):
        material = (red, blue, yellow)[index % 3]
        fixture_tools.add_cylinder(
            f"Maintenance_Can_{index}",
            0.18,
            0.38,
            (x, -0.02, 1.56),
            material,
            root,
            vertices=24,
            bevel=0.012,
        )
        fixture_tools.add_torus(
            f"Maintenance_Can_{index}_Rim",
            0.17,
            0.012,
            (x, -0.02, 1.76),
            dark,
            root,
            major_segments=20,
            minor_segments=8,
        )

    for index, x in enumerate((-1.28, -0.42, 0.44, 1.28), start=1):
        fixture_tools.add_box(
            f"Parts_Crate_{index}_Body",
            (0.68, 0.52, 0.34),
            (x, 0, 2.17),
            wood,
            root,
            bevel=0.025,
        )
        for slat_index, z in enumerate((2.08, 2.18, 2.28), start=1):
            fixture_tools.add_box(
                f"Parts_Crate_{index}_Slat_{slat_index}",
                (0.62, 0.035, 0.045),
                (x, -0.277, z),
                dark,
                root,
                bevel=0.005,
            )
    return root


def build_fastener_bins() -> bpy.types.Object:
    root = new_root("Miller_Fastener_Bins", (3.82, 0.72, 2.55))
    dark = MATERIALS["dark_steel"]
    green = MATERIALS["green"]
    cream = MATERIALS["cream"]
    steel = MATERIALS["steel"]

    fixture_tools.add_box(
        "Steel_Carcass", (3.78, 0.65, 2.44), (0, 0.03, 1.25), dark, root, bevel=0.028
    )
    for row in range(5):
        for column in range(8):
            x = -1.58 + column * 0.45
            z = 0.34 + row * 0.42
            fixture_tools.add_box(
                f"Bin_{row+1}_{column+1}_Body",
                (0.38, 0.52, 0.32),
                (x, -0.12, z),
                green if (row + column) % 3 else steel,
                root,
                bevel=0.018,
                rotation=(math.radians(-4.5), 0, 0),
            )
            fixture_tools.add_box(
                f"Bin_{row+1}_{column+1}_Label",
                (0.26, 0.025, 0.09),
                (x, -0.404, z + 0.04),
                cream,
                root,
                bevel=0.005,
            )
            fixture_tools.add_cylinder(
                f"Bin_{row+1}_{column+1}_Pull",
                0.018,
                0.07,
                (x, -0.435, z - 0.07),
                steel,
                root,
                vertices=12,
                rotation=(math.pi / 2, 0, 0),
            )
    fixture_tools.add_box(
        "Fasteners_Header", (3.80, 0.70, 0.30), (0, 0, 2.38), cream, root, bevel=0.024
    )
    return root


BUILDERS: tuple[tuple[str, Callable[[], bpy.types.Object]], ...] = (
    ("miller_gondola_aisle.glb", lambda: build_gondola_aisle("tools")),
    ("miller_gondola_fasteners.glb", lambda: build_gondola_aisle("fasteners")),
    ("miller_gondola_general.glb", lambda: build_gondola_aisle("general")),
    ("miller_checkout_counter.glb", build_checkout_counter),
    ("miller_pegboard_tool_wall.glb", build_pegboard_tool_wall),
    ("miller_paint_display.glb", build_paint_display),
    ("miller_plumbing_rack.glb", build_plumbing_rack),
    ("miller_lumber_rack.glb", build_lumber_rack),
    ("miller_warehouse_shelf.glb", build_warehouse_shelf),
    ("miller_fastener_bins.glb", build_fastener_bins),
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
        root = builder()
        exported.append(fixture_tools.export_fixture(root, filename))
    for path in exported:
        fixture_tools.validate_glb(path)
    print(f"MILLER_HARDWARE_COMPLETE files={len(exported)} output={OUTPUT_DIR}")


if __name__ == "__main__":
    main()
