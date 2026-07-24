#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class TestmanRetargetController : Node
{
	[Export] public NodePath SourceSkeletonPath { get; set; } = new();
	[Export] public NodePath TargetSkeletonPath { get; set; } = new();

	private Skeleton3D _source = null!;
	private Skeleton3D _target = null!;
	private readonly List<(int Source, int Target)> _bones = new();

	public override void _Ready()
	{
		_source = GetNode<Skeleton3D>(SourceSkeletonPath);
		_target = GetNode<Skeleton3D>(TargetSkeletonPath);

		foreach ((string source, string target) in BoneNames)
		{
			int sourceIndex = _source.FindBone(source);
			int targetIndex = _target.FindBone(target);
			if (sourceIndex >= 0 && targetIndex >= 0)
			{
				_bones.Add((sourceIndex, targetIndex));
			}
		}

		if (_bones.Count < 20)
		{
			throw new InvalidOperationException(
				"Testman is missing required humanoid retarget bones.");
		}
	}

	public override void _Process(double delta)
	{
		foreach ((int sourceIndex, int targetIndex) in _bones)
		{
			Transform3D sourcePose = _source.GetBoneGlobalPose(sourceIndex);
			Transform3D sourceRest = _source.GetBoneGlobalRest(sourceIndex);
			Transform3D targetRest = _target.GetBoneGlobalRest(targetIndex);
			Transform3D targetPose = _target.GetBoneGlobalPose(targetIndex);
			Basis motion = sourcePose.Basis * sourceRest.Basis.Inverse();

			targetPose.Basis = (motion * targetRest.Basis).Orthonormalized();
			_target.SetBoneGlobalPose(targetIndex, targetPose);
		}
	}

	private static readonly (string Source, string Target)[] BoneNames =
	{
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
	};
}
