"""Build reusable, textured Ashwood Grocery fixtures.

Run with Blender 4.4 or newer:

    blender.exe --background --python tools/build_ashwood_grocery_fixtures.py

The resulting GLBs use metre-scale construction, softened edges, real PBR
maps, and enough component detail to read as finished retail equipment rather
than placeholder primitives. Small merchandise remains separate instanced
assets in the Godot interior scene.
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import Sequence

import bpy

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_silver_spoon_fixtures as fixture_tools


REPO_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = (
    REPO_ROOT
    / "assets"
    / "environment"
    / "buildings"
    / "AshwoodGrocery"
    / "fixtures"
)

MATERIALS: dict[str, bpy.types.Material] = {}


def grocery_root(
    name: str,
    dimensions: Sequence[float],
) -> bpy.types.Object:
    root = fixture_tools.new_root(name, tuple(dimensions))
    root["fixture_pack"] = "Ashwood Grocery"
    root["design_period"] = "late 1970s rural American grocery"
    return root


def create_grocery_materials() -> dict[str, bpy.types.Material]:
    materials = fixture_tools.create_materials()

    green, tree, shader = fixture_tools.make_principled_material(
        "AG_Weathered_Green_Enamel_PBR",
        (0.075, 0.20, 0.12, 1.0),
        metallic=0.34,
        roughness=0.56,
    )
    fixture_tools.attach_normal_map(
        tree,
        shader,
        fixture_tools.METAL_NORMAL,
        strength=0.25,
    )

    cream, tree, shader = fixture_tools.make_principled_material(
        "AG_Aged_Cream_Enamel_PBR",
        (0.62, 0.57, 0.42, 1.0),
        metallic=0.08,
        roughness=0.59,
    )
    fixture_tools.attach_normal_map(
        tree,
        shader,
        fixture_tools.METAL_NORMAL,
        strength=0.14,
    )

    orange, tree, shader = fixture_tools.make_principled_material(
        "AG_Price_Rail_Orange_PBR",
        (0.57, 0.17, 0.035, 1.0),
        metallic=0.03,
        roughness=0.48,
    )
    fixture_tools.attach_normal_map(
        tree,
        shader,
        fixture_tools.METAL_NORMAL,
        strength=0.10,
    )

    dark, tree, shader = fixture_tools.make_principled_material(
        "AG_Dark_Rubber_Conveyor_PBR",
        (0.018, 0.022, 0.020, 1.0),
        metallic=0.0,
        roughness=0.76,
    )
    fixture_tools.attach_normal_map(
        tree,
        shader,
        fixture_tools.METAL_NORMAL,
        strength=0.18,
    )

    glass, tree, shader = fixture_tools.make_principled_material(
        "AG_Refrigerator_Glass",
        (0.52, 0.68, 0.67, 0.32),
        metallic=0.0,
        roughness=0.16,
    )
    shader.inputs["Alpha"].default_value = 0.32
    shader.inputs["Transmission Weight"].default_value = 0.18
    glass.diffuse_color = (0.52, 0.68, 0.67, 0.32)
    glass.surface_render_method = "DITHERED"

    materials.update(
        {
            "grocery_green": green,
            "grocery_cream": cream,
            "grocery_orange": orange,
            "grocery_dark": dark,
            "grocery_glass": glass,
        }
    )
    return materials


def add_price_rail(
    root: bpy.types.Object,
    name: str,
    width: float,
    y: float,
    z: float,
    *,
    back: bool = False,
) -> None:
    direction = -1.0 if back else 1.0
    fixture_tools.add_box(
        f"{name}_PriceRail",
        (width, 0.035, 0.075),
        (0.0, y, z),
        MATERIALS["grocery_orange"],
        root,
        bevel=0.010,
    )
    for index, x in enumerate((-width * 0.31, 0.0, width * 0.31), start=1):
        fixture_tools.add_box(
            f"{name}_Ticket_{index}",
            (0.24, 0.012, 0.05),
            (x, y + direction * 0.024, z),
            MATERIALS["formica"],
            root,
            bevel=0.004,
        )


def build_gondola_aisle() -> bpy.types.Object:
    root = grocery_root("AshwoodGrocery_GondolaAisle", (5.20, 1.18, 2.20))
    steel = MATERIALS["grocery_green"]
    cream = MATERIALS["grocery_cream"]
    chrome = MATERIALS["chrome"]
    rubber = MATERIALS["grocery_dark"]

    fixture_tools.add_box(
        "Gondola_CentralSpine",
        (5.05, 0.10, 1.93),
        (0.0, 0.0, 1.08),
        cream,
        root,
        bevel=0.025,
    )
    fixture_tools.add_box(
        "Gondola_LowerPlinth",
        (5.18, 1.12, 0.20),
        (0.0, 0.0, 0.12),
        steel,
        root,
        bevel=0.028,
    )
    fixture_tools.add_box(
        "Gondola_RubberToeKickFront",
        (5.05, 0.055, 0.13),
        (0.0, -0.565, 0.11),
        rubber,
        root,
        bevel=0.008,
    )
    fixture_tools.add_box(
        "Gondola_RubberToeKickRear",
        (5.05, 0.055, 0.13),
        (0.0, 0.565, 0.11),
        rubber,
        root,
        bevel=0.008,
    )

    for post_index, post_x in enumerate((-2.48, -1.25, 0.0, 1.25, 2.48), start=1):
        fixture_tools.add_box(
            f"Gondola_Upright_{post_index}",
            (0.065, 0.13, 2.02),
            (post_x, 0.0, 1.11),
            steel,
            root,
            bevel=0.012,
        )
        for slot_index, slot_z in enumerate(
            (0.42, 0.66, 0.90, 1.14, 1.38, 1.62, 1.86),
            start=1,
        ):
            fixture_tools.add_box(
                f"Gondola_PostSlot_{post_index}_{slot_index}",
                (0.025, 0.145, 0.045),
                (post_x, -0.002, slot_z),
                rubber,
                root,
                bevel=0.003,
            )

    for side_index, side in enumerate((-1.0, 1.0), start=1):
        for shelf_index, shelf_z in enumerate((0.38, 0.78, 1.18, 1.58, 1.98), start=1):
            shelf_depth = 0.50 if shelf_index <= 2 else 0.42
            shelf_y = side * (shelf_depth * 0.5 + 0.06)
            fixture_tools.add_box(
                f"Gondola_Side_{side_index}_Shelf_{shelf_index}",
                (5.05, shelf_depth, 0.055),
                (0.0, shelf_y, shelf_z),
                cream,
                root,
                bevel=0.012,
            )
            front_y = side * (shelf_depth + 0.055)
            add_price_rail(
                root,
                f"Gondola_Side_{side_index}_Shelf_{shelf_index}",
                4.96,
                front_y,
                shelf_z + 0.015,
                back=side < 0.0,
            )
            for bracket_index, bracket_x in enumerate(
                (-2.32, -1.18, 0.0, 1.18, 2.32),
                start=1,
            ):
                fixture_tools.add_tube_between(
                    f"Gondola_Bracket_{side_index}_{shelf_index}_{bracket_index}",
                    (bracket_x, side * 0.08, shelf_z - 0.04),
                    (bracket_x, side * (shelf_depth + 0.01), shelf_z - 0.04),
                    0.014,
                    chrome,
                    root,
                    vertices=12,
                )

    for end_index, end_x in enumerate((-2.57, 2.57), start=1):
        fixture_tools.add_box(
            f"Gondola_EndCap_{end_index}",
            (0.12, 1.12, 2.06),
            (end_x, 0.0, 1.08),
            steel,
            root,
            bevel=0.025,
        )
        fixture_tools.add_box(
            f"Gondola_EndSignPanel_{end_index}",
            (0.14, 0.82, 0.42),
            (end_x + (-0.075 if end_x < 0 else 0.075), 0.0, 1.73),
            cream,
            root,
            bevel=0.025,
        )
    return root


def build_checkout_lane() -> bpy.types.Object:
    root = grocery_root("AshwoodGrocery_CheckoutLane", (3.55, 1.02, 1.22))
    wood = MATERIALS["wood"]
    cream = MATERIALS["grocery_cream"]
    green = MATERIALS["grocery_green"]
    chrome = MATERIALS["chrome"]
    belt = MATERIALS["grocery_dark"]
    orange = MATERIALS["grocery_orange"]

    fixture_tools.add_box(
        "Checkout_Cabinet",
        (3.28, 0.82, 0.78),
        (0.0, 0.0, 0.46),
        wood,
        root,
        bevel=0.035,
    )
    fixture_tools.add_box(
        "Checkout_RecessedToeKick",
        (3.10, 0.70, 0.14),
        (0.0, 0.025, 0.12),
        belt,
        root,
        bevel=0.010,
    )
    fixture_tools.add_box(
        "Checkout_ChromeTopRim",
        (3.48, 0.96, 0.085),
        (0.0, 0.0, 0.89),
        chrome,
        root,
        bevel=0.025,
    )
    fixture_tools.add_box(
        "Checkout_FormicaTop",
        (3.40, 0.90, 0.045),
        (0.0, 0.0, 0.94),
        cream,
        root,
        bevel=0.020,
    )
    fixture_tools.add_box(
        "Checkout_Conveyor",
        (1.62, 0.63, 0.055),
        (-0.70, -0.02, 0.985),
        belt,
        root,
        bevel=0.055,
    )
    for roller_index, roller_x in enumerate((-1.43, 0.03), start=1):
        fixture_tools.add_cylinder(
            f"Checkout_BeltRoller_{roller_index}",
            0.055,
            0.63,
            (roller_x, -0.02, 0.985),
            chrome,
            root,
            vertices=20,
            rotation=(math.pi / 2.0, 0.0, 0.0),
        )
    fixture_tools.add_box(
        "Checkout_RegisterPlinth",
        (0.70, 0.72, 0.12),
        (0.44, 0.0, 1.01),
        green,
        root,
        bevel=0.025,
    )
    fixture_tools.add_box(
        "Checkout_BaggingDeck",
        (1.00, 0.78, 0.065),
        (1.25, 0.0, 0.985),
        cream,
        root,
        bevel=0.035,
    )
    for rail_index, rail_y in enumerate((-0.34, 0.34), start=1):
        fixture_tools.add_tube_between(
            f"Checkout_BagRail_{rail_index}",
            (0.88, rail_y, 1.0),
            (1.58, rail_y, 1.0),
            0.018,
            chrome,
            root,
            vertices=14,
        )
    fixture_tools.add_tube_between(
        "Checkout_LaneSignPost",
        (0.50, 0.36, 0.95),
        (0.50, 0.36, 2.03),
        0.025,
        chrome,
        root,
        vertices=16,
    )
    fixture_tools.add_rounded_rect_prism(
        "Checkout_LaneSign",
        0.52,
        0.12,
        0.34,
        0.05,
        (0.50, 0.36, 2.03),
        orange,
        root,
        bevel=0.008,
    )
    for panel_index, panel_x in enumerate((-1.18, -0.39, 0.40, 1.19), start=1):
        fixture_tools.add_box(
            f"Checkout_FrontPanel_{panel_index}",
            (0.68, 0.035, 0.48),
            (panel_x, -0.425, 0.49),
            green,
            root,
            bevel=0.025,
        )
    return root


def build_produce_table() -> bpy.types.Object:
    root = grocery_root("AshwoodGrocery_ProduceIsland", (3.35, 1.72, 1.20))
    wood = MATERIALS["wood"]
    dark = MATERIALS["grocery_dark"]
    green = MATERIALS["grocery_green"]
    cream = MATERIALS["grocery_cream"]
    orange = MATERIALS["grocery_orange"]

    fixture_tools.add_box(
        "Produce_BasePlinth",
        (3.10, 1.48, 0.30),
        (0.0, 0.0, 0.17),
        wood,
        root,
        bevel=0.050,
    )
    fixture_tools.add_box(
        "Produce_RecessedToeKick",
        (2.88, 1.28, 0.14),
        (0.0, 0.0, 0.10),
        dark,
        root,
        bevel=0.018,
    )
    fixture_tools.add_box(
        "Produce_MiddleCabinet",
        (2.78, 1.18, 0.54),
        (0.0, 0.0, 0.47),
        wood,
        root,
        bevel=0.040,
    )
    for side_index, side in enumerate((-1.0, 1.0), start=1):
        bin_y = side * 0.56
        for bin_index, bin_x in enumerate((-1.05, 0.0, 1.05), start=1):
            fixture_tools.add_box(
                f"Produce_BinFloor_{side_index}_{bin_index}",
                (0.96, 0.57, 0.08),
                (bin_x, bin_y, 0.82),
                cream,
                root,
                bevel=0.020,
                rotation=(side * math.radians(7.0), 0.0, 0.0),
            )
            fixture_tools.add_box(
                f"Produce_BinFront_{side_index}_{bin_index}",
                (0.96, 0.08, 0.38),
                (bin_x, side * 0.84, 0.91),
                wood,
                root,
                bevel=0.025,
            )
            for divider_side, divider_x in enumerate(
                (bin_x - 0.48, bin_x + 0.48),
                start=1,
            ):
                fixture_tools.add_box(
                    f"Produce_Divider_{side_index}_{bin_index}_{divider_side}",
                    (0.055, 0.56, 0.31),
                    (divider_x, bin_y, 0.93),
                    wood,
                    root,
                    bevel=0.018,
                )
            fixture_tools.add_box(
                f"Produce_PricePlate_{side_index}_{bin_index}",
                (0.50, 0.025, 0.16),
                (bin_x, side * 0.888, 1.04),
                orange,
                root,
                bevel=0.018,
            )
    fixture_tools.add_box(
        "Produce_CentreHeader",
        (3.30, 0.18, 0.38),
        (0.0, 0.0, 1.08),
        green,
        root,
        bevel=0.040,
    )
    return root


def build_refrigerator_case() -> bpy.types.Object:
    root = grocery_root("AshwoodGrocery_RefrigeratorCase", (3.20, 0.92, 2.52))
    steel = MATERIALS["stainless"]
    green = MATERIALS["grocery_green"]
    cream = MATERIALS["grocery_cream"]
    chrome = MATERIALS["chrome"]
    rubber = MATERIALS["grocery_dark"]
    glass = MATERIALS["grocery_glass"]

    fixture_tools.add_box(
        "ColdCase_InsulatedBody",
        (3.18, 0.88, 2.50),
        (0.0, 0.0, 1.25),
        cream,
        root,
        bevel=0.035,
    )
    fixture_tools.add_box(
        "ColdCase_RearLiner",
        (3.00, 0.06, 2.10),
        (0.0, 0.40, 1.25),
        steel,
        root,
        bevel=0.018,
    )
    fixture_tools.add_box(
        "ColdCase_CompressorPlinth",
        (3.10, 0.82, 0.32),
        (0.0, 0.0, 0.19),
        green,
        root,
        bevel=0.028,
    )
    for vent_index, vent_x in enumerate(
        (-1.18, -0.88, -0.58, -0.28, 0.02, 0.32, 0.62, 0.92, 1.22),
        start=1,
    ):
        fixture_tools.add_box(
            f"ColdCase_CompressorVent_{vent_index}",
            (0.19, 0.025, 0.055),
            (vent_x, -0.423, 0.19),
            rubber,
            root,
            bevel=0.010,
        )
    for shelf_index, shelf_z in enumerate((0.53, 0.92, 1.31, 1.70, 2.09), start=1):
        fixture_tools.add_box(
            f"ColdCase_Shelf_{shelf_index}",
            (2.92, 0.64, 0.045),
            (0.0, 0.04, shelf_z),
            steel,
            root,
            bevel=0.012,
        )
        fixture_tools.add_box(
            f"ColdCase_ShelfPriceRail_{shelf_index}",
            (2.88, 0.035, 0.065),
            (0.0, -0.30, shelf_z + 0.025),
            green,
            root,
            bevel=0.009,
        )
    fixture_tools.add_box(
        "ColdCase_GlassLeft",
        (1.46, 0.035, 1.92),
        (-0.77, -0.435, 1.42),
        glass,
        root,
        bevel=0.012,
    )
    fixture_tools.add_box(
        "ColdCase_GlassRight",
        (1.46, 0.035, 1.92),
        (0.77, -0.435, 1.42),
        glass,
        root,
        bevel=0.012,
    )
    for frame_index, frame_x in enumerate((-1.53, 0.0, 1.53), start=1):
        fixture_tools.add_box(
            f"ColdCase_DoorFrame_{frame_index}",
            (0.055, 0.07, 2.02),
            (frame_x, -0.46, 1.42),
            green,
            root,
            bevel=0.012,
        )
    for handle_index, handle_x in enumerate((-0.12, 0.12), start=1):
        fixture_tools.add_tube_between(
            f"ColdCase_DoorHandle_{handle_index}",
            (handle_x, -0.505, 1.02),
            (handle_x, -0.505, 1.80),
            0.018,
            chrome,
            root,
            vertices=16,
        )
        for mount_index, mount_z in enumerate((1.06, 1.76), start=1):
            fixture_tools.add_tube_between(
                f"ColdCase_HandleMount_{handle_index}_{mount_index}",
                (handle_x, -0.455, mount_z),
                (handle_x, -0.505, mount_z),
                0.012,
                chrome,
                root,
                vertices=12,
            )
    fixture_tools.add_box(
        "ColdCase_TopLightBox",
        (3.10, 0.14, 0.20),
        (0.0, -0.37, 2.35),
        green,
        root,
        bevel=0.025,
    )
    return root


def build_wall_produce_bins() -> bpy.types.Object:
    root = grocery_root("AshwoodGrocery_WallProduceBins", (3.35, 0.92, 2.10))
    wood = MATERIALS["wood"]
    green = MATERIALS["grocery_green"]
    cream = MATERIALS["grocery_cream"]
    orange = MATERIALS["grocery_orange"]
    dark = MATERIALS["grocery_dark"]

    fixture_tools.add_box(
        "WallBins_Back",
        (3.25, 0.12, 1.98),
        (0.0, 0.39, 1.03),
        green,
        root,
        bevel=0.035,
    )
    fixture_tools.add_box(
        "WallBins_ToeKick",
        (3.22, 0.82, 0.18),
        (0.0, 0.0, 0.10),
        dark,
        root,
        bevel=0.025,
    )
    for row_index, row_z in enumerate((0.48, 1.03, 1.58), start=1):
        for column_index, column_x in enumerate((-1.08, 0.0, 1.08), start=1):
            fixture_tools.add_box(
                f"WallBin_Floor_{row_index}_{column_index}",
                (0.98, 0.70, 0.065),
                (column_x, 0.02, row_z),
                cream,
                root,
                bevel=0.018,
                rotation=(math.radians(-8.0), 0.0, 0.0),
            )
            fixture_tools.add_box(
                f"WallBin_Front_{row_index}_{column_index}",
                (0.98, 0.08, 0.33),
                (column_x, -0.35, row_z + 0.10),
                wood,
                root,
                bevel=0.025,
            )
            fixture_tools.add_box(
                f"WallBin_Label_{row_index}_{column_index}",
                (0.44, 0.025, 0.14),
                (column_x, -0.398, row_z + 0.19),
                orange,
                root,
                bevel=0.016,
            )
    fixture_tools.add_box(
        "WallBins_Header",
        (3.30, 0.18, 0.34),
        (0.0, 0.30, 1.92),
        green,
        root,
        bevel=0.035,
    )
    return root


def main() -> None:
    global MATERIALS
    fixture_tools.require_source_textures()
    fixture_tools.configure_scene()
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    fixture_tools.OUTPUT_DIR = OUTPUT_DIR
    MATERIALS = create_grocery_materials()

    fixture_specs = (
        ("grocery_gondola_aisle.glb", build_gondola_aisle),
        ("grocery_checkout_lane.glb", build_checkout_lane),
        ("grocery_produce_table.glb", build_produce_table),
        ("grocery_refrigerator_case.glb", build_refrigerator_case),
        ("grocery_wall_produce_bins.glb", build_wall_produce_bins),
    )
    for filename, builder in fixture_specs:
        fixture_tools.clear_scene_objects()
        root = builder()
        fixture_tools.export_fixture(root, filename)

    print(
        "ASHWOOD_GROCERY_FIXTURES: PASS "
        f"count={len(fixture_specs)} output={OUTPUT_DIR}"
    )


if __name__ == "__main__":
    main()
