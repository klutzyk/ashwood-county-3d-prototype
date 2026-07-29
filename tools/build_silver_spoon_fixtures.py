"""Build the project-owned Silver Spoon diner fixture pack.

Run with Blender 4.4:

    blender.exe --background --python tools/build_silver_spoon_fixtures.py

The script deliberately uses authored hard-surface construction, metre units,
small bevels, and the already-downloaded PBR maps.  Each fixture is exported as
an independent GLB with embedded textures.
"""

from __future__ import annotations

import math
import re
from pathlib import Path
from typing import Callable, Iterable, Sequence

import bpy
from mathutils import Vector


REPO_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = REPO_ROOT / "assets" / "environment" / "buildings" / "Diner" / "fixtures"
DINER_MATERIALS = REPO_ROOT / "assets" / "third_party" / "interiors" / "diner" / "materials"

METAL_DIR = DINER_MATERIALS / "ambientcg" / "Metal049A"
LEATHER_DIR = DINER_MATERIALS / "poly_haven" / "leather_red_02"
WOOD_DIR = DINER_MATERIALS / "poly_haven" / "wood_table_worn"

METAL_COLOR = METAL_DIR / "Metal049A_1K-JPG_Color.jpg"
METAL_ROUGHNESS = METAL_DIR / "Metal049A_1K-JPG_Roughness.jpg"
METAL_METALNESS = METAL_DIR / "Metal049A_1K-JPG_Metalness.jpg"
METAL_NORMAL = METAL_DIR / "Metal049A_1K-JPG_NormalGL.jpg"
LEATHER_ARM = LEATHER_DIR / "leather_red_02_arm_1k.jpg"
LEATHER_NORMAL = LEATHER_DIR / "leather_red_02_nor_gl_1k.jpg"
WOOD_COLOR = WOOD_DIR / "wood_table_worn_diff_1k.jpg"
WOOD_ARM = WOOD_DIR / "wood_table_worn_arm_1k.jpg"
WOOD_NORMAL = WOOD_DIR / "wood_table_worn_nor_gl_1k.jpg"

EXPECTED_TEXTURES = (
    METAL_COLOR,
    METAL_ROUGHNESS,
    METAL_METALNESS,
    METAL_NORMAL,
    LEATHER_ARM,
    LEATHER_NORMAL,
    WOOD_COLOR,
    WOOD_ARM,
    WOOD_NORMAL,
)

FRONT_Y = -1.0
MATERIALS: dict[str, bpy.types.Material] = {}


def require_source_textures() -> None:
    missing = [path for path in EXPECTED_TEXTURES if not path.is_file()]
    if missing:
        joined = "\n".join(f"  - {path}" for path in missing)
        raise FileNotFoundError(f"Required diner PBR maps are missing:\n{joined}")


def configure_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"
    scene.render.engine = "BLENDER_EEVEE_NEXT"


def load_image(path: Path, non_color: bool = False) -> bpy.types.Image:
    image = bpy.data.images.load(str(path.resolve()), check_existing=True)
    if non_color:
        image.colorspace_settings.name = "Non-Color"
    return image


def make_principled_material(
    name: str,
    base_color: tuple[float, float, float, float],
    *,
    metallic: float = 0.0,
    roughness: float = 0.5,
) -> tuple[bpy.types.Material, bpy.types.NodeTree, bpy.types.Node]:
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    material.diffuse_color = base_color
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    output.location = (620, 0)
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (300, 0)
    principled.inputs["Base Color"].default_value = base_color
    principled.inputs["Metallic"].default_value = metallic
    principled.inputs["Roughness"].default_value = roughness
    material.node_tree.links.new(principled.outputs["BSDF"], output.inputs["Surface"])
    return material, material.node_tree, principled


def attach_normal_map(
    node_tree: bpy.types.NodeTree,
    principled: bpy.types.Node,
    path: Path,
    *,
    strength: float,
    location: tuple[float, float] = (-360, -260),
) -> None:
    texture = node_tree.nodes.new("ShaderNodeTexImage")
    texture.name = f"{path.stem}_Normal"
    texture.label = path.name
    texture.image = load_image(path, non_color=True)
    texture.location = location
    normal = node_tree.nodes.new("ShaderNodeNormalMap")
    normal.location = (-40, -220)
    normal.inputs["Strength"].default_value = strength
    node_tree.links.new(texture.outputs["Color"], normal.inputs["Color"])
    node_tree.links.new(normal.outputs["Normal"], principled.inputs["Normal"])


def attach_arm_map(
    node_tree: bpy.types.NodeTree,
    principled: bpy.types.Node,
    path: Path,
    *,
    use_metallic: bool,
    location: tuple[float, float] = (-600, 30),
) -> None:
    texture = node_tree.nodes.new("ShaderNodeTexImage")
    texture.name = f"{path.stem}_ARM"
    texture.label = path.name
    texture.image = load_image(path, non_color=True)
    texture.location = location
    separate = node_tree.nodes.new("ShaderNodeSeparateColor")
    separate.location = (-320, 40)
    node_tree.links.new(texture.outputs["Color"], separate.inputs["Color"])
    node_tree.links.new(separate.outputs["Green"], principled.inputs["Roughness"])
    if use_metallic:
        node_tree.links.new(separate.outputs["Blue"], principled.inputs["Metallic"])


def create_materials() -> dict[str, bpy.types.Material]:
    stainless, tree, shader = make_principled_material(
        "SS_Stainless_PBR", (0.64, 0.66, 0.67, 1.0), metallic=0.68, roughness=0.36
    )
    color = tree.nodes.new("ShaderNodeTexImage")
    color.name = "Metal049A_Color"
    color.label = METAL_COLOR.name
    color.image = load_image(METAL_COLOR)
    color.location = (-620, 260)
    tint = tree.nodes.new("ShaderNodeRGB")
    tint.name = "CommercialStainlessTint"
    tint.location = (-610, 90)
    tint.outputs["Color"].default_value = (0.42, 0.45, 0.47, 1.0)
    multiply = tree.nodes.new("ShaderNodeMixRGB")
    multiply.name = "TintMetal049A"
    multiply.blend_type = "MULTIPLY"
    multiply.inputs["Fac"].default_value = 1.0
    multiply.location = (-130, 210)
    tree.links.new(color.outputs["Color"], multiply.inputs[1])
    tree.links.new(tint.outputs["Color"], multiply.inputs[2])
    tree.links.new(multiply.outputs["Color"], shader.inputs["Base Color"])
    attach_normal_map(tree, shader, METAL_NORMAL, strength=0.32, location=(-620, -360))

    chrome, tree, shader = make_principled_material(
        "SS_Polished_Chrome", (0.82, 0.86, 0.88, 1.0), metallic=0.82, roughness=0.22
    )
    attach_normal_map(tree, shader, METAL_NORMAL, strength=0.10)

    red_vinyl, tree, shader = make_principled_material(
        "SS_Red_Vinyl_PBR", (0.13, 0.004, 0.007, 1.0), metallic=0.0, roughness=0.46
    )
    attach_arm_map(tree, shader, LEATHER_ARM, use_metallic=False)
    attach_normal_map(tree, shader, LEATHER_NORMAL, strength=0.58)

    dark_red_vinyl, tree, shader = make_principled_material(
        "SS_Dark_Red_Vinyl_PBR", (0.115, 0.004, 0.007, 1.0), metallic=0.0, roughness=0.52
    )
    attach_arm_map(tree, shader, LEATHER_ARM, use_metallic=False)
    attach_normal_map(tree, shader, LEATHER_NORMAL, strength=0.42)

    wood, tree, shader = make_principled_material(
        "SS_Period_Wood_Laminate_PBR", (0.25, 0.10, 0.035, 1.0), roughness=0.47
    )
    color = tree.nodes.new("ShaderNodeTexImage")
    color.name = "WoodTableWorn_Color"
    color.label = WOOD_COLOR.name
    color.image = load_image(WOOD_COLOR)
    color.location = (-610, 250)
    tree.links.new(color.outputs["Color"], shader.inputs["Base Color"])
    attach_arm_map(tree, shader, WOOD_ARM, use_metallic=False)
    attach_normal_map(tree, shader, WOOD_NORMAL, strength=0.44)

    formica, tree, shader = make_principled_material(
        "SS_Cream_Formica_PBR", (0.72, 0.64, 0.47, 1.0), metallic=0.0, roughness=0.38
    )
    roughness = tree.nodes.new("ShaderNodeTexImage")
    roughness.name = "Formica_Roughness"
    roughness.label = METAL_ROUGHNESS.name
    roughness.image = load_image(METAL_ROUGHNESS, non_color=True)
    roughness.location = (-560, 20)
    tree.links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
    attach_normal_map(tree, shader, METAL_NORMAL, strength=0.055)

    black_steel, tree, shader = make_principled_material(
        "SS_Seasoned_Black_Steel", (0.025, 0.028, 0.027, 1.0), metallic=0.76, roughness=0.42
    )
    roughness = tree.nodes.new("ShaderNodeTexImage")
    roughness.name = "BlackSteel_Roughness"
    roughness.label = METAL_ROUGHNESS.name
    roughness.image = load_image(METAL_ROUGHNESS, non_color=True)
    roughness.location = (-560, 20)
    tree.links.new(roughness.outputs["Color"], shader.inputs["Roughness"])
    attach_normal_map(tree, shader, METAL_NORMAL, strength=0.22)

    materials = {
        "stainless": stainless,
        "chrome": chrome,
        "red_vinyl": red_vinyl,
        "dark_red_vinyl": dark_red_vinyl,
        "wood": wood,
        "formica": formica,
        "black_steel": black_steel,
    }
    solid_specs = {
        "black_rubber": ((0.012, 0.014, 0.014, 1.0), 0.0, 0.72),
        "control_black": ((0.025, 0.028, 0.03, 1.0), 0.18, 0.40),
        "control_cream": ((0.66, 0.61, 0.48, 1.0), 0.0, 0.46),
        "indicator_red": ((0.58, 0.018, 0.009, 1.0), 0.12, 0.24),
        "indicator_green": ((0.015, 0.34, 0.045, 1.0), 0.06, 0.27),
        "indicator_blue": ((0.02, 0.13, 0.60, 1.0), 0.05, 0.22),
        "oil": ((0.30, 0.105, 0.008, 1.0), 0.14, 0.18),
        "porcelain": ((0.84, 0.81, 0.70, 1.0), 0.0, 0.20),
    }
    for key, (base, metallic, roughness_value) in solid_specs.items():
        material, _, _ = make_principled_material(
            f"SS_{key.title().replace('_', '_')}",
            base,
            metallic=metallic,
            roughness=roughness_value,
        )
        materials[key] = material
    return materials


def new_root(name: str, nominal_dimensions: tuple[float, float, float]) -> bpy.types.Object:
    root = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(root)
    root["fixture_pack"] = "Silver Spoon Diner"
    root["units"] = "metres"
    root["nominal_dimensions_m"] = ",".join(f"{value:.3f}" for value in nominal_dimensions)
    return root


def activate(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def add_bevel(obj: bpy.types.Object, width: float, segments: int = 3) -> None:
    if width <= 0.0:
        return
    activate(obj)
    modifier = obj.modifiers.new(name="AuthoredEdgeBevel", type="BEVEL")
    modifier.width = width
    modifier.segments = segments
    modifier.limit_method = "ANGLE"
    modifier.angle_limit = math.radians(24.0)
    modifier.harden_normals = True
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def shade_smooth_by_angle(obj: bpy.types.Object) -> None:
    if obj.type != "MESH":
        return
    activate(obj)
    try:
        bpy.ops.object.shade_smooth_by_angle()
    except (AttributeError, RuntimeError):
        for polygon in obj.data.polygons:
            polygon.use_smooth = True


def project_uv(obj: bpy.types.Object, *, smart: bool = False) -> None:
    if obj.type != "MESH" or not obj.data.polygons:
        return
    activate(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    if smart:
        bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    else:
        bpy.ops.uv.cube_project(
            cube_size=1.0,
            correct_aspect=True,
            clip_to_bounds=False,
            scale_to_bounds=False,
        )
    bpy.ops.object.mode_set(mode="OBJECT")


def finish_mesh(
    obj: bpy.types.Object,
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    bevel: float = 0.0,
    smooth: bool = False,
    smart_uv: bool = False,
) -> bpy.types.Object:
    obj.parent = root
    if not obj.data.materials:
        obj.data.materials.append(material)
    if bevel > 0.0:
        add_bevel(obj, bevel)
    if smooth:
        shade_smooth_by_angle(obj)
    project_uv(obj, smart=smart_uv)
    return obj


def add_box(
    name: str,
    size: Sequence[float],
    location: Sequence[float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    bevel: float = 0.012,
    rotation: Sequence[float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = Vector(size)
    activate(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_mesh(obj, material, root, bevel=bevel)


def add_cylinder(
    name: str,
    radius: float,
    depth: float,
    location: Sequence[float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    vertices: int = 24,
    rotation: Sequence[float] = (0.0, 0.0, 0.0),
    bevel: float = 0.004,
    scale_xy: tuple[float, float] = (1.0, 1.0),
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        end_fill_type="NGON",
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale.x = scale_xy[0]
    obj.scale.y = scale_xy[1]
    activate(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_mesh(obj, material, root, bevel=bevel, smooth=True, smart_uv=True)


def add_cone(
    name: str,
    radius1: float,
    radius2: float,
    depth: float,
    location: Sequence[float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    vertices: int = 32,
    bevel: float = 0.004,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius1,
        radius2=radius2,
        depth=depth,
        end_fill_type="NGON",
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    return finish_mesh(obj, material, root, bevel=bevel, smooth=True, smart_uv=True)


def add_torus(
    name: str,
    major_radius: float,
    minor_radius: float,
    location: Sequence[float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    rotation: Sequence[float] = (0.0, 0.0, 0.0),
    major_segments: int = 32,
    minor_segments: int = 8,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        major_radius=major_radius,
        minor_radius=minor_radius,
        major_segments=major_segments,
        minor_segments=minor_segments,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    return finish_mesh(obj, material, root, smooth=True, smart_uv=True)


def add_uv_sphere(
    name: str,
    radius: float,
    location: Sequence[float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    scale: Sequence[float] = (1.0, 1.0, 1.0),
    segments: int = 20,
    rings: int = 10,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=radius,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = Vector(scale)
    activate(obj)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_mesh(obj, material, root, smooth=True, smart_uv=True)


def add_tube_between(
    name: str,
    start: Sequence[float],
    end: Sequence[float],
    radius: float,
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    vertices: int = 12,
    bevel: float = 0.002,
) -> bpy.types.Object:
    start_vector = Vector(start)
    end_vector = Vector(end)
    delta = end_vector - start_vector
    if delta.length <= 0.00001:
        raise ValueError(f"Tube {name} has zero length")
    midpoint = (start_vector + end_vector) * 0.5
    obj = add_cylinder(
        name,
        radius,
        delta.length,
        midpoint,
        material,
        root,
        vertices=vertices,
        bevel=bevel,
    )
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0.0, 0.0, 1.0)).rotation_difference(delta.normalized())
    return obj


def add_curve_tube(
    name: str,
    points: Sequence[Sequence[float]],
    radius: float,
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    resolution: int = 2,
    bevel_resolution: int = 2,
) -> bpy.types.Object:
    curve = bpy.data.curves.new(name=f"{name}_Curve", type="CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = resolution
    curve.bevel_depth = radius
    curve.bevel_resolution = bevel_resolution
    curve.resolution_u = resolution
    spline = curve.splines.new(type="BEZIER")
    spline.bezier_points.add(len(points) - 1)
    for bezier_point, coordinate in zip(spline.bezier_points, points):
        bezier_point.co = Vector(coordinate)
        bezier_point.handle_left_type = "AUTO"
        bezier_point.handle_right_type = "AUTO"
    obj = bpy.data.objects.new(name, curve)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = root
    curve.materials.append(material)
    activate(obj)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    shade_smooth_by_angle(obj)
    project_uv(obj, smart=True)
    return obj


def rounded_rectangle_points(width: float, depth: float, radius: float, segments: int) -> list[tuple[float, float]]:
    half_width = width * 0.5
    half_depth = depth * 0.5
    radius = min(radius, half_width - 0.001, half_depth - 0.001)
    corners = (
        (half_width - radius, half_depth - radius, 0.0),
        (-half_width + radius, half_depth - radius, 90.0),
        (-half_width + radius, -half_depth + radius, 180.0),
        (half_width - radius, -half_depth + radius, 270.0),
    )
    points: list[tuple[float, float]] = []
    for center_x, center_y, start_angle in corners:
        for index in range(segments + 1):
            angle = math.radians(start_angle + (90.0 * index / segments))
            points.append((center_x + math.cos(angle) * radius, center_y + math.sin(angle) * radius))
    return points


def add_rounded_rect_prism(
    name: str,
    width: float,
    depth: float,
    height: float,
    radius: float,
    location: Sequence[float],
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    corner_segments: int = 6,
    bevel: float = 0.004,
) -> bpy.types.Object:
    outline = rounded_rectangle_points(width, depth, radius, corner_segments)
    count = len(outline)
    vertices = [(x, y, -height * 0.5) for x, y in outline]
    vertices.extend((x, y, height * 0.5) for x, y in outline)
    faces: list[tuple[int, ...]] = []
    faces.append(tuple(reversed(range(count))))
    faces.append(tuple(range(count, count * 2)))
    for index in range(count):
        next_index = (index + 1) % count
        faces.append((index, next_index, count + next_index, count + index))
    mesh = bpy.data.meshes.new(name=f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = Vector(location)
    return finish_mesh(obj, material, root, bevel=bevel, smooth=True)


def add_open_basin(
    name: str,
    center: Sequence[float],
    top_size: tuple[float, float],
    bottom_size: tuple[float, float],
    top_z: float,
    bottom_z: float,
    material: bpy.types.Material,
    root: bpy.types.Object,
) -> bpy.types.Object:
    center_x, center_y, _ = center
    top_w, top_d = top_size
    bottom_w, bottom_d = bottom_size
    vertices = [
        (center_x - top_w / 2, center_y - top_d / 2, top_z),
        (center_x + top_w / 2, center_y - top_d / 2, top_z),
        (center_x + top_w / 2, center_y + top_d / 2, top_z),
        (center_x - top_w / 2, center_y + top_d / 2, top_z),
        (center_x - bottom_w / 2, center_y - bottom_d / 2, bottom_z),
        (center_x + bottom_w / 2, center_y - bottom_d / 2, bottom_z),
        (center_x + bottom_w / 2, center_y + bottom_d / 2, bottom_z),
        (center_x - bottom_w / 2, center_y + bottom_d / 2, bottom_z),
    ]
    faces = [
        (4, 5, 6, 7),
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
    ]
    mesh = bpy.data.meshes.new(name=f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return finish_mesh(obj, material, root, bevel=0.01, smooth=True)


def add_rectangular_wire_frame(
    prefix: str,
    center: tuple[float, float, float],
    width: float,
    depth: float,
    radius: float,
    material: bpy.types.Material,
    root: bpy.types.Object,
    *,
    vertices: int = 10,
) -> None:
    center_x, center_y, z = center
    corners = [
        (center_x - width / 2, center_y - depth / 2, z),
        (center_x + width / 2, center_y - depth / 2, z),
        (center_x + width / 2, center_y + depth / 2, z),
        (center_x - width / 2, center_y + depth / 2, z),
    ]
    for index in range(4):
        add_tube_between(
            f"{prefix}_Edge_{index + 1}",
            corners[index],
            corners[(index + 1) % 4],
            radius,
            material,
            root,
            vertices=vertices,
            bevel=radius * 0.35,
        )


def material_slug(name: str) -> str:
    return re.sub(r"[^a-z0-9]+", "_", name.lower()).strip("_")


def consolidate_by_material(root: bpy.types.Object, asset_name: str) -> None:
    mesh_objects = [obj for obj in root.children_recursive if obj.type == "MESH"]
    groups: dict[str, list[bpy.types.Object]] = {}
    for obj in mesh_objects:
        material_name = obj.data.materials[0].name if obj.data.materials else "unassigned"
        groups.setdefault(material_name, []).append(obj)
    for material_name, objects in groups.items():
        bpy.ops.object.select_all(action="DESELECT")
        for obj in objects:
            obj.select_set(True)
        active = objects[0]
        bpy.context.view_layer.objects.active = active
        if len(objects) > 1:
            bpy.ops.object.join()
        active.name = f"{asset_name}_{material_slug(material_name)}"
        active.parent = root


def world_bounds(objects: Iterable[bpy.types.Object]) -> tuple[Vector, Vector]:
    minimum = Vector((math.inf, math.inf, math.inf))
    maximum = Vector((-math.inf, -math.inf, -math.inf))
    found = False
    for obj in objects:
        if obj.type != "MESH":
            continue
        found = True
        for corner in obj.bound_box:
            world_corner = obj.matrix_world @ Vector(corner)
            minimum.x = min(minimum.x, world_corner.x)
            minimum.y = min(minimum.y, world_corner.y)
            minimum.z = min(minimum.z, world_corner.z)
            maximum.x = max(maximum.x, world_corner.x)
            maximum.y = max(maximum.y, world_corner.y)
            maximum.z = max(maximum.z, world_corner.z)
    if not found:
        raise RuntimeError("No mesh geometry found while calculating fixture bounds")
    return minimum, maximum


def export_fixture(root: bpy.types.Object, filename: str) -> Path:
    asset_name = Path(filename).stem
    consolidate_by_material(root, asset_name)
    descendants = list(root.children_recursive)
    minimum, maximum = world_bounds(descendants)
    dimensions = maximum - minimum
    root["measured_bounds_min_m"] = ",".join(f"{value:.4f}" for value in minimum)
    root["measured_bounds_max_m"] = ",".join(f"{value:.4f}" for value in maximum)
    root["measured_dimensions_m"] = ",".join(f"{value:.4f}" for value in dimensions)
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for obj in descendants:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    destination = OUTPUT_DIR / filename
    bpy.ops.export_scene.gltf(
        filepath=str(destination.resolve()),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_yup=True,
        export_materials="EXPORT",
        export_image_format="AUTO",
        export_cameras=False,
        export_lights=False,
        export_extras=True,
    )
    print(
        "SILVER_SPOON_EXPORT "
        f"file={destination.name} "
        f"dimensions_m=({dimensions.x:.3f},{dimensions.y:.3f},{dimensions.z:.3f}) "
        f"meshes={sum(1 for obj in descendants if obj.type == 'MESH')} "
        f"bytes={destination.stat().st_size}"
    )
    return destination


def clear_scene_objects() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def build_booth_bay() -> bpy.types.Object:
    root = new_root("SilverSpoon_BoothBay", (1.72, 1.48, 1.10))
    red = MATERIALS["red_vinyl"]
    dark_red = MATERIALS["dark_red_vinyl"]
    wood = MATERIALS["wood"]
    chrome = MATERIALS["chrome"]
    rubber = MATERIALS["black_rubber"]

    for side_index, side in enumerate((-1.0, 1.0), start=1):
        add_box(
            f"Booth_{side_index}_LaminatePlinth",
            (1.62, 0.52, 0.34),
            (0.0, side * 0.53, 0.18),
            wood,
            root,
            bevel=0.025,
        )
        add_box(
            f"Booth_{side_index}_RecessedToeKick",
            (1.48, 0.035, 0.16),
            (0.0, side * 0.265, 0.12),
            rubber,
            root,
            bevel=0.006,
        )
        add_rounded_rect_prism(
            f"Booth_{side_index}_SeatCushion",
            1.56,
            0.49,
            0.145,
            0.075,
            (0.0, side * 0.425, 0.515),
            red,
            root,
            bevel=0.008,
        )
        add_rounded_rect_prism(
            f"Booth_{side_index}_BackCore",
            1.60,
            0.17,
            0.67,
            0.075,
            (0.0, side * 0.69, 0.775),
            red,
            root,
            bevel=0.008,
        )
        panel_front_y = side * 0.59
        for panel_index, panel_x in enumerate((-0.52, 0.0, 0.52), start=1):
            add_rounded_rect_prism(
                f"Booth_{side_index}_ChannelPanel_{panel_index}",
                0.47,
                0.032,
                0.565,
                0.035,
                (panel_x, panel_front_y, 0.795),
                red,
                root,
                bevel=0.005,
            )
        for seam_index, seam_x in enumerate((-0.775, -0.26, 0.26, 0.775), start=1):
            add_tube_between(
                f"Booth_{side_index}_UpholsterySeam_{seam_index}",
                (seam_x, side * 0.568, 0.55),
                (seam_x, side * 0.568, 1.045),
                0.006,
                dark_red,
                root,
                vertices=10,
                bevel=0.0015,
            )
        for row_index, button_z in enumerate((0.70, 0.91), start=1):
            for column_index, button_x in enumerate((-0.50, 0.0, 0.50), start=1):
                add_cylinder(
                    f"Booth_{side_index}_Button_{row_index}_{column_index}",
                    0.017,
                    0.018,
                    (button_x, side * 0.566, button_z),
                    dark_red,
                    root,
                    vertices=16,
                    rotation=(math.pi / 2.0, 0.0, 0.0),
                    bevel=0.002,
                )
        add_tube_between(
            f"Booth_{side_index}_ChromeTopRail",
            (-0.79, side * 0.695, 1.105),
            (0.79, side * 0.695, 1.105),
            0.018,
            chrome,
            root,
            vertices=16,
        )
        for end_index, end_x in enumerate((-0.79, 0.79), start=1):
            add_tube_between(
                f"Booth_{side_index}_ChromeEndRail_{end_index}",
                (end_x, side * 0.685, 0.49),
                (end_x, side * 0.685, 1.09),
                0.016,
                chrome,
                root,
                vertices=14,
            )
        rail_y = side * 0.18
        add_tube_between(
            f"Booth_{side_index}_ChromeKickRail",
            (-0.68, rail_y, 0.255),
            (0.68, rail_y, 0.255),
            0.022,
            chrome,
            root,
            vertices=16,
        )
        for bracket_index, bracket_x in enumerate((-0.62, 0.62), start=1):
            add_tube_between(
                f"Booth_{side_index}_KickRailBracket_{bracket_index}",
                (bracket_x, side * 0.275, 0.18),
                (bracket_x, rail_y, 0.255),
                0.012,
                chrome,
                root,
                vertices=12,
            )
    return root


def build_formica_table() -> bpy.types.Object:
    root = new_root("SilverSpoon_FormicaPedestalTable", (1.36, 0.80, 0.78))
    chrome = MATERIALS["chrome"]
    formica = MATERIALS["formica"]
    rubber = MATERIALS["black_rubber"]
    black = MATERIALS["control_black"]

    add_cylinder(
        "Table_RubberFloorPad",
        0.305,
        0.025,
        (0.0, 0.0, 0.015),
        rubber,
        root,
        vertices=36,
        scale_xy=(1.0, 0.72),
        bevel=0.004,
    )
    add_cylinder(
        "Table_CastChromeBase",
        0.30,
        0.04,
        (0.0, 0.0, 0.042),
        chrome,
        root,
        vertices=36,
        scale_xy=(1.0, 0.72),
        bevel=0.006,
    )
    add_cone("Table_PedestalFlare", 0.17, 0.075, 0.18, (0.0, 0.0, 0.145), chrome, root)
    add_cylinder("Table_PedestalColumn", 0.061, 0.51, (0.0, 0.0, 0.48), chrome, root, vertices=28)
    add_torus("Table_LowerCollar", 0.072, 0.012, (0.0, 0.0, 0.235), chrome, root)
    add_torus("Table_UpperCollar", 0.074, 0.012, (0.0, 0.0, 0.685), chrome, root)
    add_box("Table_UndersideBrace", (0.74, 0.12, 0.055), (0.0, 0.0, 0.695), black, root, bevel=0.018)
    add_rounded_rect_prism(
        "Table_ChromeEdgeAndSubstrate",
        1.36,
        0.80,
        0.072,
        0.13,
        (0.0, 0.0, 0.735),
        chrome,
        root,
        bevel=0.004,
    )
    add_rounded_rect_prism(
        "Table_CreamFormicaTop",
        1.325,
        0.765,
        0.038,
        0.112,
        (0.0, 0.0, 0.776),
        formica,
        root,
        bevel=0.005,
    )
    return root


def build_service_counter() -> bpy.types.Object:
    root = new_root("SilverSpoon_ServiceCounter", (3.78, 1.06, 1.04))
    wood = MATERIALS["wood"]
    formica = MATERIALS["formica"]
    chrome = MATERIALS["chrome"]
    stainless = MATERIALS["stainless"]
    rubber = MATERIALS["black_rubber"]
    red = MATERIALS["dark_red_vinyl"]
    black = MATERIALS["control_black"]

    add_box("Counter_LaminateCarcass", (3.50, 0.68, 0.79), (0.0, 0.025, 0.48), wood, root, bevel=0.024)
    add_box("Counter_RecessedToeKick", (3.34, 0.12, 0.17), (0.0, -0.345, 0.16), rubber, root, bevel=0.006)
    add_box("Counter_ServerSideStainlessBand", (3.38, 0.035, 0.22), (0.0, 0.375, 0.69), stainless, root, bevel=0.006)
    panel_width = 0.635
    for panel_index, panel_x in enumerate((-1.38, -0.69, 0.0, 0.69, 1.38), start=1):
        add_rounded_rect_prism(
            f"Counter_FrontPanel_{panel_index}",
            panel_width,
            0.045,
            0.56,
            0.025,
            (panel_x, -0.342, 0.51),
            wood,
            root,
            bevel=0.004,
        )
        add_box(
            f"Counter_FrontPanelInset_{panel_index}",
            (panel_width - 0.09, 0.018, 0.45),
            (panel_x, -0.370, 0.50),
            red,
            root,
            bevel=0.018,
        )
        handle_x = panel_x + panel_width * 0.31
        add_tube_between(
            f"Counter_PanelHandle_{panel_index}",
            (handle_x, -0.402, 0.61),
            (handle_x, -0.402, 0.77),
            0.010,
            chrome,
            root,
            vertices=12,
        )
        add_tube_between(
            f"Counter_HandleStandOffA_{panel_index}",
            (handle_x, -0.365, 0.62),
            (handle_x, -0.402, 0.62),
            0.008,
            chrome,
            root,
            vertices=10,
        )
        add_tube_between(
            f"Counter_HandleStandOffB_{panel_index}",
            (handle_x, -0.365, 0.76),
            (handle_x, -0.402, 0.76),
            0.008,
            chrome,
            root,
            vertices=10,
        )
    for corner_index, corner_x in enumerate((-1.76, 1.76), start=1):
        add_tube_between(
            f"Counter_ChromeCornerPost_{corner_index}",
            (corner_x, -0.338, 0.12),
            (corner_x, -0.338, 0.91),
            0.018,
            chrome,
            root,
            vertices=14,
        )
    add_rounded_rect_prism(
        "Counter_ChromeTopRim",
        3.78,
        0.86,
        0.075,
        0.095,
        (0.0, -0.015, 0.925),
        chrome,
        root,
        bevel=0.005,
    )
    add_rounded_rect_prism(
        "Counter_FormicaWorktop",
        3.72,
        0.80,
        0.038,
        0.078,
        (0.0, -0.015, 0.970),
        formica,
        root,
        bevel=0.004,
    )
    for well_index, well_x in enumerate((-0.74, 0.0, 0.74), start=1):
        add_rounded_rect_prism(
            f"Counter_CutleryWellRim_{well_index}",
            0.40,
            0.19,
            0.025,
            0.035,
            (well_x, 0.235, 0.998),
            stainless,
            root,
            bevel=0.003,
        )
        add_rounded_rect_prism(
            f"Counter_CutleryWellInsert_{well_index}",
            0.33,
            0.13,
            0.018,
            0.026,
            (well_x, 0.235, 1.010),
            black,
            root,
            bevel=0.002,
        )
    add_tube_between(
        "Counter_CustomerFootRail",
        (-1.62, -0.53, 0.30),
        (1.62, -0.53, 0.30),
        0.027,
        chrome,
        root,
        vertices=16,
    )
    for bracket_index, bracket_x in enumerate((-1.42, -0.48, 0.48, 1.42), start=1):
        add_tube_between(
            f"Counter_FootRailBracket_{bracket_index}",
            (bracket_x, -0.36, 0.18),
            (bracket_x, -0.53, 0.30),
            0.015,
            chrome,
            root,
            vertices=12,
        )
    add_box("Counter_ReceiptRail", (1.15, 0.025, 0.032), (0.96, -0.43, 1.005), chrome, root, bevel=0.009)
    return root


def build_sink_station() -> bpy.types.Object:
    root = new_root("SilverSpoon_TripleSinkPrepStation", (2.72, 0.80, 1.39))
    steel = MATERIALS["stainless"]
    chrome = MATERIALS["chrome"]
    black = MATERIALS["black_rubber"]
    red = MATERIALS["indicator_red"]
    blue = MATERIALS["indicator_blue"]

    add_box("Sink_FrontTopRail", (2.70, 0.10, 0.065), (0.0, -0.31, 0.91), steel, root, bevel=0.012)
    add_box("Sink_BackTopRail", (2.70, 0.10, 0.065), (0.0, 0.31, 0.91), steel, root, bevel=0.012)
    for divider_index, divider_x in enumerate((-0.30, 0.30), start=1):
        add_box(
            f"Sink_BasinDivider_{divider_index}",
            (0.12, 0.53, 0.065),
            (divider_x, 0.0, 0.91),
            steel,
            root,
            bevel=0.010,
        )
    for board_index, board_x in enumerate((-1.105, 1.105), start=1):
        add_box(
            f"Sink_Drainboard_{board_index}",
            (0.43, 0.53, 0.065),
            (board_x, 0.0, 0.91),
            steel,
            root,
            bevel=0.012,
        )
        for groove_index, groove_x in enumerate((-0.13, -0.065, 0.0, 0.065, 0.13), start=1):
            add_tube_between(
                f"Sink_Drainboard_{board_index}_Groove_{groove_index}",
                (board_x + groove_x, -0.22, 0.947),
                (board_x + groove_x, 0.22, 0.947),
                0.007,
                chrome,
                root,
                vertices=10,
                bevel=0.0015,
            )
    for basin_index, basin_x in enumerate((-0.60, 0.0, 0.60), start=1):
        add_open_basin(
            f"Sink_Basin_{basin_index}",
            (basin_x, 0.0, 0.0),
            (0.48, 0.52),
            (0.38, 0.40),
            0.91,
            0.655,
            steel,
            root,
        )
        add_cylinder(
            f"Sink_Drain_{basin_index}",
            0.046,
            0.012,
            (basin_x, 0.0, 0.662),
            black,
            root,
            vertices=24,
            bevel=0.002,
        )
        add_curve_tube(
            f"Sink_GooseneckFaucet_{basin_index}",
            (
                (basin_x, 0.285, 0.96),
                (basin_x, 0.285, 1.235),
                (basin_x, 0.13, 1.365),
                (basin_x, -0.07, 1.31),
                (basin_x, -0.12, 1.17),
            ),
            0.017,
            chrome,
            root,
        )
        add_cylinder(
            f"Sink_FaucetAerator_{basin_index}",
            0.023,
            0.055,
            (basin_x, -0.12, 1.145),
            chrome,
            root,
            vertices=18,
            bevel=0.003,
        )
        for knob_index, (knob_offset, knob_material) in enumerate(((-0.095, red), (0.095, blue)), start=1):
            knob_x = basin_x + knob_offset
            add_cylinder(
                f"Sink_ControlStem_{basin_index}_{knob_index}",
                0.022,
                0.055,
                (knob_x, 0.292, 1.02),
                chrome,
                root,
                vertices=16,
                rotation=(math.pi / 2.0, 0.0, 0.0),
                bevel=0.003,
            )
            add_tube_between(
                f"Sink_ControlHandleA_{basin_index}_{knob_index}",
                (knob_x - 0.045, 0.255, 1.02),
                (knob_x + 0.045, 0.255, 1.02),
                0.011,
                knob_material,
                root,
                vertices=10,
            )
            add_tube_between(
                f"Sink_ControlHandleB_{basin_index}_{knob_index}",
                (knob_x, 0.255, 0.975),
                (knob_x, 0.255, 1.065),
                0.011,
                knob_material,
                root,
                vertices=10,
            )
    add_box("Sink_Backsplash", (2.70, 0.045, 0.30), (0.0, 0.365, 1.07), steel, root, bevel=0.015)
    add_box("Sink_LowerPrepShelf", (2.40, 0.50, 0.045), (0.0, 0.0, 0.25), steel, root, bevel=0.012)
    for leg_index, (leg_x, leg_y) in enumerate(
        ((-1.23, -0.27), (-1.23, 0.27), (1.23, -0.27), (1.23, 0.27)),
        start=1,
    ):
        add_cylinder(
            f"Sink_Leg_{leg_index}",
            0.033,
            0.86,
            (leg_x, leg_y, 0.46),
            steel,
            root,
            vertices=18,
            bevel=0.004,
        )
        add_cylinder(
            f"Sink_AdjustableFoot_{leg_index}",
            0.052,
            0.045,
            (leg_x, leg_y, 0.025),
            black,
            root,
            vertices=18,
            bevel=0.004,
        )
    return root


def build_griddle_range() -> bpy.types.Object:
    root = new_root("SilverSpoon_GriddleRange", (1.50, 0.90, 1.10))
    steel = MATERIALS["stainless"]
    chrome = MATERIALS["chrome"]
    iron = MATERIALS["black_steel"]
    black = MATERIALS["control_black"]
    rubber = MATERIALS["black_rubber"]
    red = MATERIALS["indicator_red"]
    green = MATERIALS["indicator_green"]

    add_box("Range_CabinetBody", (1.44, 0.78, 0.67), (0.0, 0.0, 0.45), steel, root, bevel=0.025)
    add_box("Range_RecessedPlinth", (1.30, 0.68, 0.11), (0.0, 0.035, 0.14), black, root, bevel=0.008)
    add_box("Range_TopDeck", (1.48, 0.82, 0.075), (0.0, 0.0, 0.80), steel, root, bevel=0.016)
    add_rounded_rect_prism(
        "Range_SeasonedGriddlePlate",
        0.86,
        0.65,
        0.038,
        0.025,
        (-0.275, -0.015, 0.855),
        iron,
        root,
        bevel=0.004,
    )
    add_box("Range_GreaseGutter", (0.86, 0.075, 0.045), (-0.275, -0.335, 0.855), steel, root, bevel=0.008)
    add_box("Range_GreaseDrawer", (0.27, 0.075, 0.065), (-0.57, -0.395, 0.80), steel, root, bevel=0.008)
    for burner_index, burner_y in enumerate((-0.19, 0.19), start=1):
        add_torus(
            f"Range_BurnerOuter_{burner_index}",
            0.145,
            0.025,
            (0.49, burner_y, 0.856),
            iron,
            root,
            major_segments=28,
        )
        add_torus(
            f"Range_BurnerInner_{burner_index}",
            0.075,
            0.018,
            (0.49, burner_y, 0.857),
            iron,
            root,
            major_segments=24,
        )
        add_tube_between(
            f"Range_BurnerGrateX_{burner_index}",
            (0.31, burner_y, 0.87),
            (0.67, burner_y, 0.87),
            0.012,
            iron,
            root,
            vertices=10,
        )
        add_tube_between(
            f"Range_BurnerGrateY_{burner_index}",
            (0.49, burner_y - 0.18, 0.87),
            (0.49, burner_y + 0.18, 0.87),
            0.012,
            iron,
            root,
            vertices=10,
        )
    add_box("Range_RearSplash", (1.44, 0.055, 0.28), (0.0, 0.385, 0.965), steel, root, bevel=0.012)
    add_box("Range_ControlFascia", (1.40, 0.065, 0.21), (0.0, -0.405, 0.69), steel, root, bevel=0.012)
    knob_positions = (-0.57, -0.38, -0.19, 0.07, 0.31, 0.55)
    for knob_index, knob_x in enumerate(knob_positions, start=1):
        add_cylinder(
            f"Range_KnobCollar_{knob_index}",
            0.055,
            0.018,
            (knob_x, -0.447, 0.70),
            chrome,
            root,
            vertices=24,
            rotation=(math.pi / 2.0, 0.0, 0.0),
            bevel=0.003,
        )
        add_cylinder(
            f"Range_ControlKnob_{knob_index}",
            0.043,
            0.043,
            (knob_x, -0.477, 0.70),
            black,
            root,
            vertices=20,
            rotation=(math.pi / 2.0, 0.0, 0.0),
            bevel=0.005,
        )
        add_box(
            f"Range_KnobIndicator_{knob_index}",
            (0.009, 0.012, 0.032),
            (knob_x, -0.503, 0.726),
            MATERIALS["control_cream"],
            root,
            bevel=0.002,
        )
    add_uv_sphere("Range_RedPilotLamp", 0.022, (-0.68, -0.465, 0.765), red, root, scale=(1.0, 0.45, 1.0))
    add_uv_sphere("Range_GreenPilotLamp", 0.022, (-0.62, -0.465, 0.765), green, root, scale=(1.0, 0.45, 1.0))
    for door_index, door_x in enumerate((-0.37, 0.37), start=1):
        add_box(
            f"Range_CabinetDoor_{door_index}",
            (0.61, 0.045, 0.39),
            (door_x, -0.415, 0.39),
            steel,
            root,
            bevel=0.015,
        )
        add_tube_between(
            f"Range_DoorHandle_{door_index}",
            (door_x - 0.19, -0.46, 0.52),
            (door_x + 0.19, -0.46, 0.52),
            0.014,
            chrome,
            root,
            vertices=12,
        )
        for vent_index, vent_z in enumerate((0.25, 0.285, 0.32), start=1):
            add_box(
                f"Range_DoorVent_{door_index}_{vent_index}",
                (0.28, 0.018, 0.016),
                (door_x, -0.445, vent_z),
                black,
                root,
                bevel=0.004,
            )
    for foot_index, foot_x in enumerate((-0.61, 0.61), start=1):
        for row_index, foot_y in enumerate((-0.28, 0.28), start=1):
            add_cylinder(
                f"Range_Foot_{foot_index}_{row_index}",
                0.042,
                0.11,
                (foot_x, foot_y, 0.065),
                rubber,
                root,
                vertices=18,
                bevel=0.004,
            )
    return root


def add_fryer_basket(
    basket_index: int,
    center_x: float,
    root: bpy.types.Object,
    steel: bpy.types.Material,
    black: bpy.types.Material,
) -> None:
    top_z = 0.98
    bottom_z = 0.79
    top_width = 0.38
    top_depth = 0.43
    bottom_width = 0.30
    bottom_depth = 0.34
    center_y = -0.01
    add_rectangular_wire_frame(
        f"Fryer_Basket_{basket_index}_Top",
        (center_x, center_y, top_z),
        top_width,
        top_depth,
        0.010,
        steel,
        root,
    )
    add_rectangular_wire_frame(
        f"Fryer_Basket_{basket_index}_Bottom",
        (center_x, center_y, bottom_z),
        bottom_width,
        bottom_depth,
        0.007,
        steel,
        root,
        vertices=8,
    )
    top_corners = [
        (center_x - top_width / 2, center_y - top_depth / 2, top_z),
        (center_x + top_width / 2, center_y - top_depth / 2, top_z),
        (center_x + top_width / 2, center_y + top_depth / 2, top_z),
        (center_x - top_width / 2, center_y + top_depth / 2, top_z),
    ]
    bottom_corners = [
        (center_x - bottom_width / 2, center_y - bottom_depth / 2, bottom_z),
        (center_x + bottom_width / 2, center_y - bottom_depth / 2, bottom_z),
        (center_x + bottom_width / 2, center_y + bottom_depth / 2, bottom_z),
        (center_x - bottom_width / 2, center_y + bottom_depth / 2, bottom_z),
    ]
    for corner_index, (bottom_corner, top_corner) in enumerate(zip(bottom_corners, top_corners), start=1):
        add_tube_between(
            f"Fryer_Basket_{basket_index}_Corner_{corner_index}",
            bottom_corner,
            top_corner,
            0.007,
            steel,
            root,
            vertices=8,
            bevel=0.0015,
        )
    for grid_index in range(1, 6):
        fraction = grid_index / 6.0
        x = center_x - bottom_width / 2 + bottom_width * fraction
        add_tube_between(
            f"Fryer_Basket_{basket_index}_BottomLongWire_{grid_index}",
            (x, center_y - bottom_depth / 2, bottom_z),
            (x, center_y + bottom_depth / 2, bottom_z),
            0.0045,
            steel,
            root,
            vertices=8,
            bevel=0.001,
        )
    for grid_index in range(1, 7):
        fraction = grid_index / 7.0
        y = center_y - bottom_depth / 2 + bottom_depth * fraction
        add_tube_between(
            f"Fryer_Basket_{basket_index}_BottomCrossWire_{grid_index}",
            (center_x - bottom_width / 2, y, bottom_z),
            (center_x + bottom_width / 2, y, bottom_z),
            0.0045,
            steel,
            root,
            vertices=8,
            bevel=0.001,
        )
    for level_index, fraction in enumerate((0.25, 0.50, 0.75), start=1):
        z = bottom_z + (top_z - bottom_z) * fraction
        width = bottom_width + (top_width - bottom_width) * fraction
        depth = bottom_depth + (top_depth - bottom_depth) * fraction
        add_rectangular_wire_frame(
            f"Fryer_Basket_{basket_index}_SideCourse_{level_index}",
            (center_x, center_y, z),
            width,
            depth,
            0.0045,
            steel,
            root,
            vertices=8,
        )
    rod_left = center_x - 0.11
    rod_right = center_x + 0.11
    handle_y = -0.76
    handle_z = 1.18
    add_tube_between(
        f"Fryer_Basket_{basket_index}_HandleRodLeft",
        (rod_left, center_y - top_depth / 2, top_z),
        (center_x - 0.065, handle_y, handle_z),
        0.010,
        steel,
        root,
        vertices=10,
    )
    add_tube_between(
        f"Fryer_Basket_{basket_index}_HandleRodRight",
        (rod_right, center_y - top_depth / 2, top_z),
        (center_x + 0.065, handle_y, handle_z),
        0.010,
        steel,
        root,
        vertices=10,
    )
    add_tube_between(
        f"Fryer_Basket_{basket_index}_HandleCrossbar",
        (center_x - 0.09, handle_y, handle_z),
        (center_x + 0.09, handle_y, handle_z),
        0.015,
        black,
        root,
        vertices=14,
    )


def build_twin_fryer() -> bpy.types.Object:
    root = new_root("SilverSpoon_TwinDeepFryer", (1.20, 1.20, 1.22))
    steel = MATERIALS["stainless"]
    chrome = MATERIALS["chrome"]
    black = MATERIALS["control_black"]
    rubber = MATERIALS["black_rubber"]
    oil = MATERIALS["oil"]
    red = MATERIALS["indicator_red"]
    green = MATERIALS["indicator_green"]

    add_box("Fryer_CabinetBody", (1.14, 0.75, 0.72), (0.0, 0.0, 0.47), steel, root, bevel=0.024)
    add_box("Fryer_RecessedBase", (1.02, 0.64, 0.11), (0.0, 0.02, 0.14), black, root, bevel=0.007)
    add_box("Fryer_FrontTopRail", (1.18, 0.10, 0.075), (0.0, -0.335, 0.855), steel, root, bevel=0.012)
    add_box("Fryer_BackTopRail", (1.18, 0.10, 0.075), (0.0, 0.335, 0.855), steel, root, bevel=0.012)
    add_box("Fryer_LeftTopRail", (0.09, 0.58, 0.075), (-0.545, 0.0, 0.855), steel, root, bevel=0.010)
    add_box("Fryer_RightTopRail", (0.09, 0.58, 0.075), (0.545, 0.0, 0.855), steel, root, bevel=0.010)
    add_box("Fryer_CentreDivider", (0.08, 0.58, 0.075), (0.0, 0.0, 0.855), steel, root, bevel=0.010)
    for well_index, well_x in enumerate((-0.285, 0.285), start=1):
        add_open_basin(
            f"Fryer_Well_{well_index}",
            (well_x, 0.0, 0.0),
            (0.44, 0.54),
            (0.36, 0.43),
            0.855,
            0.58,
            MATERIALS["black_steel"],
            root,
        )
        add_rounded_rect_prism(
            f"Fryer_OilSurface_{well_index}",
            0.35,
            0.42,
            0.012,
            0.025,
            (well_x, 0.0, 0.70),
            oil,
            root,
            bevel=0.002,
        )
        add_box(
            f"Fryer_ControlTower_{well_index}",
            (0.49, 0.16, 0.38),
            (well_x, 0.315, 1.03),
            steel,
            root,
            bevel=0.018,
        )
        add_cylinder(
            f"Fryer_ThermostatCollar_{well_index}",
            0.065,
            0.018,
            (well_x, 0.225, 1.075),
            chrome,
            root,
            vertices=24,
            rotation=(math.pi / 2.0, 0.0, 0.0),
            bevel=0.003,
        )
        add_cylinder(
            f"Fryer_ThermostatKnob_{well_index}",
            0.052,
            0.045,
            (well_x, 0.194, 1.075),
            black,
            root,
            vertices=20,
            rotation=(math.pi / 2.0, 0.0, 0.0),
            bevel=0.005,
        )
        add_uv_sphere(
            f"Fryer_RedPilot_{well_index}",
            0.021,
            (well_x - 0.095, 0.20, 1.16),
            red,
            root,
            scale=(1.0, 0.45, 1.0),
        )
        add_uv_sphere(
            f"Fryer_GreenPilot_{well_index}",
            0.021,
            (well_x - 0.035, 0.20, 1.16),
            green,
            root,
            scale=(1.0, 0.45, 1.0),
        )
        add_fryer_basket(well_index, well_x, root, steel, rubber)
        add_box(
            f"Fryer_CabinetDoor_{well_index}",
            (0.48, 0.045, 0.43),
            (well_x, -0.398, 0.42),
            steel,
            root,
            bevel=0.014,
        )
        add_tube_between(
            f"Fryer_DoorHandle_{well_index}",
            (well_x - 0.15, -0.445, 0.56),
            (well_x + 0.15, -0.445, 0.56),
            0.013,
            chrome,
            root,
            vertices=12,
        )
        for vent_index, vent_z in enumerate((0.275, 0.31, 0.345), start=1):
            add_box(
                f"Fryer_DoorVent_{well_index}_{vent_index}",
                (0.24, 0.018, 0.015),
                (well_x, -0.43, vent_z),
                black,
                root,
                bevel=0.003,
            )
        add_cylinder(
            f"Fryer_DrainValve_{well_index}",
            0.028,
            0.07,
            (well_x, -0.44, 0.22),
            chrome,
            root,
            vertices=16,
            rotation=(math.pi / 2.0, 0.0, 0.0),
            bevel=0.003,
        )
        add_tube_between(
            f"Fryer_DrainHandle_{well_index}",
            (well_x - 0.045, -0.48, 0.22),
            (well_x + 0.045, -0.48, 0.22),
            0.011,
            black,
            root,
            vertices=10,
        )
    for foot_index, (foot_x, foot_y) in enumerate(
        ((-0.48, -0.28), (-0.48, 0.28), (0.48, -0.28), (0.48, 0.28)),
        start=1,
    ):
        add_cylinder(
            f"Fryer_Foot_{foot_index}",
            0.043,
            0.11,
            (foot_x, foot_y, 0.065),
            rubber,
            root,
            vertices=18,
            bevel=0.004,
        )
    return root


def add_canopy_shell(root: bpy.types.Object, material: bpy.types.Material) -> None:
    lower_width = 2.40
    lower_depth = 1.05
    upper_width = 1.92
    upper_depth = 0.58
    lower_z = 0.10
    upper_z = 0.68
    vertices = [
        (-lower_width / 2, -lower_depth / 2, lower_z),
        (lower_width / 2, -lower_depth / 2, lower_z),
        (lower_width / 2, lower_depth / 2, lower_z),
        (-lower_width / 2, lower_depth / 2, lower_z),
        (-upper_width / 2, -upper_depth / 2, upper_z),
        (upper_width / 2, -upper_depth / 2, upper_z),
        (upper_width / 2, upper_depth / 2, upper_z),
        (-upper_width / 2, upper_depth / 2, upper_z),
    ]
    faces = [
        (0, 1, 5, 4),
        (1, 2, 6, 5),
        (2, 3, 7, 6),
        (3, 0, 4, 7),
        (4, 5, 6, 7),
    ]
    mesh = bpy.data.meshes.new("Extractor_CanopyShell_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new("Extractor_CanopyShell", mesh)
    bpy.context.scene.collection.objects.link(obj)
    finish_mesh(obj, material, root, bevel=0.018, smooth=True)


def build_extractor_hood() -> bpy.types.Object:
    root = new_root("SilverSpoon_CommercialExtractorHood", (2.44, 1.09, 1.10))
    steel = MATERIALS["stainless"]
    chrome = MATERIALS["chrome"]
    iron = MATERIALS["black_steel"]
    black = MATERIALS["black_rubber"]

    add_canopy_shell(root, steel)
    add_box("Extractor_FrontFascia", (2.44, 0.085, 0.20), (0.0, -0.505, 0.17), steel, root, bevel=0.015)
    add_box("Extractor_RearGreaseChannel", (2.34, 0.10, 0.09), (0.0, 0.46, 0.15), steel, root, bevel=0.012)
    add_box("Extractor_LeftLowerFrame", (0.085, 0.98, 0.12), (-1.18, 0.0, 0.13), chrome, root, bevel=0.014)
    add_box("Extractor_RightLowerFrame", (0.085, 0.98, 0.12), (1.18, 0.0, 0.13), chrome, root, bevel=0.014)
    add_box("Extractor_DuctNeck", (1.02, 0.49, 0.37), (0.0, 0.0, 0.865), steel, root, bevel=0.018)
    add_box("Extractor_DuctSeamBand", (1.08, 0.54, 0.055), (0.0, 0.0, 0.715), chrome, root, bevel=0.010)
    filter_rotation = (math.radians(17.0), 0.0, 0.0)
    for filter_index, filter_x in enumerate((-0.78, -0.26, 0.26, 0.78), start=1):
        add_box(
            f"Extractor_BaffleFilter_{filter_index}",
            (0.47, 0.55, 0.025),
            (filter_x, 0.06, 0.205),
            iron,
            root,
            bevel=0.010,
            rotation=filter_rotation,
        )
        for slat_index, slat_x in enumerate((-0.17, -0.102, -0.034, 0.034, 0.102, 0.17), start=1):
            add_box(
                f"Extractor_Filter_{filter_index}_Baffle_{slat_index}",
                (0.018, 0.50, 0.016),
                (filter_x + slat_x, 0.052, 0.218),
                steel,
                root,
                bevel=0.004,
                rotation=filter_rotation,
            )
        add_tube_between(
            f"Extractor_FilterPull_{filter_index}",
            (filter_x - 0.07, -0.22, 0.15),
            (filter_x + 0.07, -0.22, 0.15),
            0.011,
            chrome,
            root,
            vertices=12,
        )
    for rivet_index, rivet_x in enumerate((-1.05, -0.75, -0.45, -0.15, 0.15, 0.45, 0.75, 1.05), start=1):
        add_cylinder(
            f"Extractor_FrontRivet_{rivet_index}",
            0.012,
            0.010,
            (rivet_x, -0.552, 0.18),
            chrome,
            root,
            vertices=14,
            rotation=(math.pi / 2.0, 0.0, 0.0),
            bevel=0.002,
        )
    add_box("Extractor_GreaseCup", (0.13, 0.12, 0.14), (1.03, 0.43, 0.075), steel, root, bevel=0.010)
    for bracket_index, bracket_x in enumerate((-0.88, 0.88), start=1):
        add_box(
            f"Extractor_WallBracket_{bracket_index}",
            (0.24, 0.08, 0.30),
            (bracket_x, 0.54, 0.48),
            black,
            root,
            bevel=0.012,
        )
    return root


BUILDERS: tuple[tuple[str, Callable[[], bpy.types.Object]], ...] = (
    ("diner_booth_bay.glb", build_booth_bay),
    ("diner_pedestal_table.glb", build_formica_table),
    ("diner_service_counter.glb", build_service_counter),
    ("diner_commercial_sink.glb", build_sink_station),
    ("diner_griddle_range.glb", build_griddle_range),
    ("diner_deep_fryer.glb", build_twin_fryer),
    ("diner_extractor_hood.glb", build_extractor_hood),
)


def validate_glb(path: Path) -> None:
    clear_scene_objects()
    bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"{path.name} imported without mesh geometry")
    minimum, maximum = world_bounds(meshes)
    dimensions = maximum - minimum
    if min(dimensions) <= 0.01:
        raise RuntimeError(f"{path.name} has implausible dimensions {tuple(dimensions)}")
    used_materials = {
        slot.material
        for obj in meshes
        for slot in obj.material_slots
        if slot.material is not None
    }
    images = {
        node.image.name
        for material in used_materials
        if material.use_nodes
        for node in material.node_tree.nodes
        if node.type == "TEX_IMAGE" and node.image is not None
    }
    if not used_materials:
        raise RuntimeError(f"{path.name} imported without materials")
    if not images:
        raise RuntimeError(f"{path.name} imported without embedded PBR images")
    print(
        "SILVER_SPOON_VALIDATE "
        f"file={path.name} "
        f"dimensions_m=({dimensions.x:.3f},{dimensions.y:.3f},{dimensions.z:.3f}) "
        f"meshes={len(meshes)} materials={len(used_materials)} embedded_images={len(images)}"
    )


def main() -> None:
    require_source_textures()
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    configure_scene()
    global MATERIALS
    MATERIALS = create_materials()
    exported: list[Path] = []
    for filename, builder in BUILDERS:
        clear_scene_objects()
        root = builder()
        exported.append(export_fixture(root, filename))
    for path in exported:
        validate_glb(path)
    print(f"SILVER_SPOON_COMPLETE files={len(exported)} output={OUTPUT_DIR}")


if __name__ == "__main__":
    main()
