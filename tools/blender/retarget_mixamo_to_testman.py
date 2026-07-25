"""Bake one Mixamo animation onto Testman's AccuRIG skeleton.

Run with Blender, for example:
blender --background --python retarget_mixamo_to_testman.py -- \
  --remy assets/characters/player/Remy.fbx \
  --animation assets/characters/player/anim/Walking.fbx \
  --testman assets/characters/player/testman.fbx \
  --output generated/testman_walking.fbx

This uses Blender's standard FBX and pose APIs. It does not require an addon.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


BONE_MAP = (
    ("mixamorig_Hips", "pelvis"),
    ("mixamorig_Spine", "spine_01"),
    ("mixamorig_Spine1", "spine_03"),
    ("mixamorig_Spine2", "spine_05"),
    ("mixamorig_Neck", "neck_01"),
    ("mixamorig_Head", "head"),
    ("mixamorig_LeftShoulder", "clavicle_l"),
    ("mixamorig_LeftArm", "upperarm_l"),
    ("mixamorig_LeftForeArm", "lowerarm_l"),
    ("mixamorig_LeftHand", "hand_l"),
    ("mixamorig_RightShoulder", "clavicle_r"),
    ("mixamorig_RightArm", "upperarm_r"),
    ("mixamorig_RightForeArm", "lowerarm_r"),
    ("mixamorig_RightHand", "hand_r"),
    ("mixamorig_LeftUpLeg", "thigh_l"),
    ("mixamorig_LeftLeg", "calf_l"),
    ("mixamorig_LeftFoot", "foot_l"),
    ("mixamorig_LeftToeBase", "ball_l"),
    ("mixamorig_RightUpLeg", "thigh_r"),
    ("mixamorig_RightLeg", "calf_r"),
    ("mixamorig_RightFoot", "foot_r"),
    ("mixamorig_RightToeBase", "ball_r"),
    ("mixamorig_LeftHandThumb1", "thumb_01_l"),
    ("mixamorig_LeftHandThumb2", "thumb_02_l"),
    ("mixamorig_LeftHandThumb3", "thumb_03_l"),
    ("mixamorig_LeftHandIndex1", "index_01_l"),
    ("mixamorig_LeftHandIndex2", "index_02_l"),
    ("mixamorig_LeftHandIndex3", "index_03_l"),
    ("mixamorig_LeftHandMiddle1", "middle_01_l"),
    ("mixamorig_LeftHandMiddle2", "middle_02_l"),
    ("mixamorig_LeftHandMiddle3", "middle_03_l"),
    ("mixamorig_LeftHandRing1", "ring_01_l"),
    ("mixamorig_LeftHandRing2", "ring_02_l"),
    ("mixamorig_LeftHandRing3", "ring_03_l"),
    ("mixamorig_LeftHandPinky1", "pinky_01_l"),
    ("mixamorig_LeftHandPinky2", "pinky_02_l"),
    ("mixamorig_LeftHandPinky3", "pinky_03_l"),
    ("mixamorig_RightHandThumb1", "thumb_01_r"),
    ("mixamorig_RightHandThumb2", "thumb_02_r"),
    ("mixamorig_RightHandThumb3", "thumb_03_r"),
    ("mixamorig_RightHandIndex1", "index_01_r"),
    ("mixamorig_RightHandIndex2", "index_02_r"),
    ("mixamorig_RightHandIndex3", "index_03_r"),
    ("mixamorig_RightHandMiddle1", "middle_01_r"),
    ("mixamorig_RightHandMiddle2", "middle_02_r"),
    ("mixamorig_RightHandMiddle3", "middle_03_r"),
    ("mixamorig_RightHandRing1", "ring_01_r"),
    ("mixamorig_RightHandRing2", "ring_02_r"),
    ("mixamorig_RightHandRing3", "ring_03_r"),
    ("mixamorig_RightHandPinky1", "pinky_01_r"),
    ("mixamorig_RightHandPinky2", "pinky_02_r"),
    ("mixamorig_RightHandPinky3", "pinky_03_r"),
)


def blender_source_name(name: str) -> str:
    return name.replace("mixamorig_", "mixamorig:", 1)


def reset_blender() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(path: Path) -> list[bpy.types.Object]:
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=str(path.resolve()), use_anim=True)
    return [obj for obj in bpy.data.objects if obj not in before]


def find_armature(
    objects: list[bpy.types.Object],
    required_bone: str,
    label: str,
) -> bpy.types.Object:
    matches = [
        obj
        for obj in objects
        if obj.type == "ARMATURE" and required_bone in obj.data.bones
    ]
    if len(matches) != 1:
        raise RuntimeError(
            f"Expected one {label} armature containing {required_bone!r}; "
            f"found {len(matches)}."
        )
    return matches[0]


def find_action(
    armature: bpy.types.Object,
    newly_created_actions: set[bpy.types.Action],
) -> bpy.types.Action:
    animation_data = armature.animation_data
    if animation_data and animation_data.action:
        return animation_data.action
    if animation_data:
        for track in animation_data.nla_tracks:
            for strip in track.strips:
                if strip.action:
                    return strip.action
    candidates = [
        action for action in bpy.data.actions if action not in newly_created_actions
    ]
    if len(candidates) == 1:
        return candidates[0]
    raise RuntimeError("Could not identify the imported Mixamo action.")


def validate_mapping(source: bpy.types.Object, target: bpy.types.Object) -> None:
    missing_source = [
        source_name
        for source_name, _ in BONE_MAP
        if blender_source_name(source_name) not in source.data.bones
    ]
    missing_target = [
        target_name
        for _, target_name in BONE_MAP
        if target_name not in target.data.bones
    ]
    if missing_source or missing_target:
        raise RuntimeError(
            "Retarget bone map is incomplete. "
            f"Missing source bones: {missing_source}; missing target bones: {missing_target}"
        )
    if "root" not in target.data.bones:
        raise RuntimeError("Testman requires its AccuRIG 'root' bone for root motion.")


def action_frame_range(action: bpy.types.Action) -> tuple[int, int]:
    start, end = action.frame_range
    return int(round(start)), int(round(end))


def character_scale(source: bpy.types.Object, target: bpy.types.Object) -> float:
    source_height = (
        source.data.bones["mixamorig:Head"].matrix_local.translation
        - source.data.bones["mixamorig:Hips"].matrix_local.translation
    ).length
    target_height = (
        target.data.bones["head"].matrix_local.translation
        - target.data.bones["pelvis"].matrix_local.translation
    ).length
    return target_height / source_height if source_height > 1e-6 else 1.0


def clear_pose(armature: bpy.types.Object) -> None:
    identity = Matrix.Identity(4)
    for pose_bone in armature.pose.bones:
        pose_bone.matrix_basis = identity


def set_global_rotation(
    source: bpy.types.Object,
    target: bpy.types.Object,
    source_name: str,
    target_name: str,
) -> None:
    source_name = blender_source_name(source_name)
    source_pose = source.pose.bones[source_name].matrix
    source_rest = source.data.bones[source_name].matrix_local
    target_rest = target.data.bones[target_name].matrix_local
    target_pose = target.pose.bones[target_name]
    current_translation = target_pose.matrix.translation.copy()

    source_motion = (
        source_pose.to_3x3() @ source_rest.to_3x3().inverted()
    ).normalized()
    desired = (source_motion @ target_rest.to_3x3()).normalized().to_4x4()
    desired.translation = current_translation
    target_pose.matrix = desired


def apply_root_motion(
    source: bpy.types.Object,
    target: bpy.types.Object,
    scale: float,
    preserve_root_motion: bool,
) -> None:
    source_hips = source.pose.bones["mixamorig:Hips"].matrix
    source_rest = source.data.bones["mixamorig:Hips"].matrix_local
    delta = (source_hips.translation - source_rest.translation) * scale

    root = target.pose.bones["root"]
    root_matrix = root.matrix.copy()
    if preserve_root_motion:
        root_matrix.translation += Vector((delta.x, delta.y, 0.0))
    root.matrix = root_matrix

    pelvis = target.pose.bones["pelvis"]
    pelvis_matrix = pelvis.matrix.copy()
    pelvis_matrix.translation.z += delta.z
    pelvis.matrix = pelvis_matrix


def key_pose(target: bpy.types.Object, frame: int, preserve_root_motion: bool) -> None:
    for _, target_name in BONE_MAP:
        pose_bone = target.pose.bones[target_name]
        pose_bone.rotation_mode = "QUATERNION"
        pose_bone.keyframe_insert(
            data_path="rotation_quaternion",
            frame=frame,
            group=target_name,
        )

    target.pose.bones["pelvis"].keyframe_insert(
        data_path="location",
        frame=frame,
        group="pelvis",
    )
    if preserve_root_motion:
        target.pose.bones["root"].keyframe_insert(
            data_path="location",
            frame=frame,
            group="root",
        )


def bake_action(
    source: bpy.types.Object,
    target: bpy.types.Object,
    source_action: bpy.types.Action,
    action_name: str,
    preserve_root_motion: bool,
) -> bpy.types.Action:
    source.animation_data_create()
    source.animation_data.action = source_action
    target.animation_data_clear()
    target.animation_data_create()
    baked_action = bpy.data.actions.new(action_name)
    target.animation_data.action = baked_action

    start, end = action_frame_range(source_action)
    scene = bpy.context.scene
    scene.frame_start = start
    scene.frame_end = end
    scale = character_scale(source, target)

    for frame in range(start, end + 1):
        scene.frame_set(frame)
        clear_pose(target)
        apply_root_motion(source, target, scale, preserve_root_motion)
        for source_name, target_name in BONE_MAP:
            set_global_rotation(source, target, source_name, target_name)
        key_pose(target, frame, preserve_root_motion)

    scene.frame_set(start)
    return baked_action


def export_target(
    target_objects: list[bpy.types.Object],
    target_armature: bpy.types.Object,
    output_path: Path,
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in target_objects:
        if obj.type in {"ARMATURE", "MESH"}:
            obj.select_set(True)
    bpy.context.view_layer.objects.active = target_armature

    bpy.ops.export_scene.fbx(
        filepath=str(output_path.resolve()),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True,
        bake_anim_simplify_factor=0.0,
        path_mode="COPY",
        embed_textures=True,
    )


def retarget_file(
    remy_path: Path,
    animation_path: Path,
    testman_path: Path,
    output_path: Path,
    preserve_root_motion: bool = True,
    fps: int = 30,
) -> None:
    for path in (remy_path, animation_path, testman_path):
        if not path.is_file():
            raise FileNotFoundError(path)

    reset_blender()
    scene = bpy.context.scene
    scene.render.fps = fps

    actions_before = set(bpy.data.actions)

    animation_objects = import_fbx(animation_path)
    animation_armature = find_armature(
        animation_objects,
        "mixamorig:Hips",
        "Mixamo animation",
    )

    source_action = find_action(animation_armature, actions_before)

    testman_objects = import_fbx(testman_path)
    testman = find_armature(testman_objects, "pelvis", "Testman")

    validate_mapping(animation_armature, testman)

    action_name = animation_path.stem.replace(" ", "_")

    bake_action(
        animation_armature,
        testman,
        source_action,
        action_name,
        preserve_root_motion,
    )
    export_target(testman_objects, testman, output_path)
    print(f"RETARGET_COMPLETE: {animation_path} -> {output_path}")


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--remy", type=Path, required=True)
    parser.add_argument("--animation", type=Path, required=True)
    parser.add_argument("--testman", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--fps", type=int, default=30)
    parser.add_argument(
        "--root-motion",
        choices=("preserve", "in-place"),
        default="preserve",
    )
    return parser.parse_args(argv)


def blender_args() -> list[str]:
    return sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []


def main() -> None:
    args = parse_args(blender_args())
    retarget_file(
        args.remy,
        args.animation,
        args.testman,
        args.output,
        preserve_root_motion=args.root_motion == "preserve",
        fps=args.fps,
    )


if __name__ == "__main__":
    main()
