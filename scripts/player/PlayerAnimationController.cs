#nullable enable

using System;
using System.Collections.Generic;
using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerAnimationController : AnimationTree
{
	private const string SourceAnimationName = "mixamo_com";
	private const string IdleAnimationName = "Idle";
	private const string WalkAnimationName = "Walk";
	private const string RunAnimationName = "Run";
	private const string TwoHandIdleAnimationName = "TwoHandIdle";
	private const string MeleeAttackAnimationName = "MeleeAttack2H01";
	private const string CombatDamageAnimationName = "CombatDamage01";
	private const string TwoHandIdlePath =
		"res://assets/characters/player/anim/Animations/Male/Combat/2H/HumanM@CombatIdle2H01.fbx";
	private const string MeleeAttackPath =
		"res://assets/characters/player/anim/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx";
	private const string CombatDamagePath =
		"res://assets/characters/player/anim/Animations/Male/Combat/HumanM@CombatDamage01.fbx";
	private const string IdlePath = "res://assets/characters/player/Idle.fbx";
	private const string WalkPath = "res://assets/characters/player/Walking.fbx";
	private const string RunPath = "res://assets/characters/player/Fast Run.fbx";
	private const float BlendSpeed = 8.0f;

	private ThirdPersonPlayer _player = null!;
	private float _idleWalkBlend;
	private float _runBlend;
	private bool _isTwoHandedWeaponEquipped;
	private float _twoHandIdleBlend;
	private AnimationPlayer _animationPlayer = null!;
	private PlayerHealth _playerHealth = null!;
	private float _meleeAnimationLength;
	public StringName LastMeleeAnimationName { get; private set; } = new();

	public void SetTwoHandedWeaponEquipped(bool equipped)
	{
		_isTwoHandedWeaponEquipped = equipped;
	}
	
	public override void _Ready()
	{
		_player = GetParent<ThirdPersonPlayer>();
		_animationPlayer = FindDescendant<AnimationPlayer>(_player)
			?? throw new InvalidOperationException("Remy is missing an AnimationPlayer.");

		AddLocomotionAnimations(_animationPlayer);
		AddMeleeAnimations(_animationPlayer);
		ConfigureBlendTree(_animationPlayer);
		_playerHealth = _player.GetNode<PlayerHealth>("Health");
		_playerHealth.Damaged += OnPlayerDamaged;
	}

	public override void _ExitTree()
	{
		if (IsInstanceValid(_playerHealth))
		{
			_playerHealth.Damaged -= OnPlayerDamaged;
		}
	}

	public override void _Process(double delta)
	{
		float horizontalSpeed =
			new Vector2(_player.Velocity.X, _player.Velocity.Z).Length();

		float walkTarget =
			Mathf.Clamp(horizontalSpeed / _player.WalkSpeed, 0.0f, 1.0f);

		float runTarget =
			_player.IsSprinting && horizontalSpeed > 0.1f
				? 1.0f
				: 0.0f;

		float blendStep = BlendSpeed * (float)delta;

		float twoHandTarget =
			_isTwoHandedWeaponEquipped && horizontalSpeed < 0.1f
				? 1.0f
				: 0.0f;

		_twoHandIdleBlend = Mathf.MoveToward(
			_twoHandIdleBlend,
			twoHandTarget,
			blendStep
		);

		_idleWalkBlend = Mathf.MoveToward(
			_idleWalkBlend,
			walkTarget,
			blendStep
		);

		_runBlend = Mathf.MoveToward(
			_runBlend,
			runTarget,
			blendStep
		);

		Set("parameters/IdleType/blend_amount", _twoHandIdleBlend);
		Set("parameters/IdleWalk/blend_amount", _idleWalkBlend);
		Set("parameters/RunBlend/blend_amount", _runBlend);
	}

	public void PlayMeleeAttack(int comboStep, float attackDuration)
	{
		float duration = Mathf.Max(attackDuration, 0.05f);
		Set("parameters/AttackSpeed/scale", _meleeAnimationLength / duration);
		Set("parameters/MeleeAttack/request", 1);
		LastMeleeAnimationName = MeleeAttackAnimationName;
	}

	private void AddLocomotionAnimations(AnimationPlayer animationPlayer)
	{
		AnimationLibrary library = animationPlayer.GetAnimationLibrary("");

		AddAnimation(library, IdleAnimationName, IdlePath);
		AddRetargetedAnimation(library, TwoHandIdleAnimationName, TwoHandIdlePath);
		AddAnimation(library, WalkAnimationName, WalkPath);
		AddAnimation(library, RunAnimationName, RunPath);
	}

	private static float AddAnimation(
		AnimationLibrary library,
		string name,
		string assetPath,
		bool shouldLoop = true)
	{
		PackedScene animationScene = ResourceLoader.Load<PackedScene>(assetPath);
		Node sourceRoot = animationScene.Instantiate();
		AnimationPlayer sourcePlayer = FindDescendant<AnimationPlayer>(sourceRoot)
			?? throw new InvalidOperationException($"{assetPath} is missing an AnimationPlayer.");
		Animation sourceAnimation = sourcePlayer.GetAnimation(SourceAnimationName);
		Animation animation = (Animation)sourceAnimation.Duplicate(true);

		animation.LoopMode = shouldLoop
			? Animation.LoopModeEnum.Linear
			: Animation.LoopModeEnum.None;
		KeepRootMotionInPlace(animation);
		library.AddAnimation(name, animation);
		float animationLength = (float)animation.Length;
		sourceRoot.Free();
		return animationLength;
	}

	private static float AddRetargetedAnimation(
		AnimationLibrary library,
		string name,
		string assetPath,
		bool shouldLoop = true)
	{
		PackedScene animationScene = ResourceLoader.Load<PackedScene>(assetPath);
		Node sourceRoot = animationScene.Instantiate();
		AnimationPlayer sourcePlayer = FindDescendant<AnimationPlayer>(sourceRoot)
			?? throw new InvalidOperationException($"{assetPath} is missing an AnimationPlayer.");
		StringName sourceName = GetFirstAnimationName(sourcePlayer, assetPath);
		Animation animation = (Animation)sourcePlayer.GetAnimation(sourceName).Duplicate(true);
		Animation targetReference = library.GetAnimation(IdleAnimationName);

		RetargetRotationTracks(animation, targetReference);
		animation.LoopMode = shouldLoop
			? Animation.LoopModeEnum.Linear
			: Animation.LoopModeEnum.None;
		library.AddAnimation(name, animation);
		float animationLength = (float)animation.Length;
		sourceRoot.Free();
		return animationLength;
	}

	private void ConfigureBlendTree(AnimationPlayer animationPlayer)
	{
		AnimationNodeBlendTree blendTree = new();

		blendTree.AddNode(
			"Idle",
			CreateAnimationNode(IdleAnimationName),
			new Vector2(-700.0f, -180.0f)
		);

		blendTree.AddNode(
			"TwoHandIdle",
			CreateAnimationNode(TwoHandIdleAnimationName),
			new Vector2(-700.0f, -60.0f)
		);

		blendTree.AddNode(
			"IdleType",
			new AnimationNodeBlend2(),
			new Vector2(-480.0f, -120.0f)
		);

		blendTree.AddNode(
			"Walk",
			CreateAnimationNode(WalkAnimationName),
			new Vector2(-480.0f, 40.0f)
		);

		blendTree.AddNode(
			"IdleWalk",
			new AnimationNodeBlend2(),
			new Vector2(-260.0f, -40.0f)
		);

		blendTree.AddNode(
			"Run",
			CreateAnimationNode(RunAnimationName),
			new Vector2(-260.0f, 140.0f)
		);

		blendTree.AddNode(
			"RunBlend",
			new AnimationNodeBlend2(),
			new Vector2(0.0f, 0.0f)
		);

		blendTree.AddNode(
			"AttackClip",
			CreateAnimationNode(MeleeAttackAnimationName),
			new Vector2(-40.0f, 180.0f));
		blendTree.AddNode(
			"AttackSpeed",
			new AnimationNodeTimeScale(),
			new Vector2(180.0f, 180.0f));
		AnimationNodeOneShot meleeAttack = new();
		meleeAttack.Set("fadein_time", 0.07f);
		meleeAttack.Set("fadeout_time", 0.14f);
		blendTree.AddNode(
			"MeleeAttack",
			meleeAttack,
			new Vector2(420.0f, 0.0f));
		blendTree.AddNode(
			"DamageClip",
			CreateAnimationNode(CombatDamageAnimationName),
			new Vector2(420.0f, 180.0f));
		AnimationNodeOneShot damageReaction = new();
		damageReaction.Set("fadein_time", 0.05f);
		damageReaction.Set("fadeout_time", 0.12f);
		blendTree.AddNode(
			"DamageReaction",
			damageReaction,
			new Vector2(650.0f, 0.0f));

		blendTree.ConnectNode("IdleType", 0, "Idle");
		blendTree.ConnectNode("IdleType", 1, "TwoHandIdle");

		blendTree.ConnectNode("IdleWalk", 0, "IdleType");
		blendTree.ConnectNode("IdleWalk", 1, "Walk");

		blendTree.ConnectNode("RunBlend", 0, "IdleWalk");
		blendTree.ConnectNode("RunBlend", 1, "Run");

		blendTree.ConnectNode("AttackSpeed", 0, "AttackClip");
		blendTree.ConnectNode("MeleeAttack", 0, "RunBlend");
		blendTree.ConnectNode("MeleeAttack", 1, "AttackSpeed");
		blendTree.ConnectNode("DamageReaction", 0, "MeleeAttack");
		blendTree.ConnectNode("DamageReaction", 1, "DamageClip");
		blendTree.ConnectNode("output", 0, "DamageReaction");

		AnimPlayer = GetPathTo(animationPlayer);
		TreeRoot = blendTree;
		Active = true;
	}

	private void AddMeleeAnimations(AnimationPlayer animationPlayer)
	{
		AnimationLibrary library = animationPlayer.GetAnimationLibrary("");
		_meleeAnimationLength = AddRetargetedAnimation(
			library,
			MeleeAttackAnimationName,
			MeleeAttackPath,
			shouldLoop: false);
		AddRetargetedAnimation(
			library,
			CombatDamageAnimationName,
			CombatDamagePath,
			shouldLoop: false);
	}

	private void OnPlayerDamaged(float damageAmount)
	{
		Set("parameters/DamageReaction/request", 1);
	}

	private static StringName GetFirstAnimationName(
		AnimationPlayer animationPlayer,
		string assetPath)
	{
		foreach (StringName animationName in animationPlayer.GetAnimationList())
		{
			if (animationName != "RESET")
			{
				return animationName;
			}
		}

		throw new InvalidOperationException($"{assetPath} contains no animation.");
	}

	private static void RetargetRotationTracks(
		Animation animation,
		Animation targetReference)
	{
		string targetSkeletonPath = GetTargetSkeletonPath(targetReference);
		for (int track = animation.GetTrackCount() - 1; track >= 0; track--)
		{
			string sourcePath = animation.TrackGetPath(track).ToString();
			int separator = sourcePath.LastIndexOf(':');
			string? targetBone = separator >= 0
				? GetTargetBoneName(sourcePath[(separator + 1)..])
				: null;

			if (animation.TrackGetType(track) != Animation.TrackType.Rotation3D ||
				targetBone is null)
			{
				animation.RemoveTrack(track);
				continue;
			}

			animation.TrackSetPath(
				track,
				new NodePath($"{targetSkeletonPath}:{targetBone}"));
		}
	}

	private static string GetTargetSkeletonPath(Animation targetReference)
	{
		for (int track = 0; track < targetReference.GetTrackCount(); track++)
		{
			string path = targetReference.TrackGetPath(track).ToString();
			if (path.EndsWith(":mixamorig_Hips", StringComparison.Ordinal))
			{
				return path[..path.LastIndexOf(':')];
			}
		}

		throw new InvalidOperationException("Remy animation tracks do not target the hips.");
	}

	private static string? GetTargetBoneName(string sourceBone)
	{
		if (sourceBone.StartsWith("B-", StringComparison.Ordinal))
		{
			sourceBone = sourceBone[2..];
		}

		if (CoreBoneMap.TryGetValue(sourceBone, out string? targetBone))
		{
			return targetBone;
		}

		int sideSeparator = sourceBone.LastIndexOf('.');
		if (sideSeparator < 0)
		{
			return null;
		}

		string side = sourceBone[(sideSeparator + 1)..] == "L" ? "Left" : "Right";
		string finger = sourceBone[..sideSeparator]
			.Replace("indexFinger0", "Index", StringComparison.Ordinal)
			.Replace("middleFinger0", "Middle", StringComparison.Ordinal)
			.Replace("ringFinger0", "Ring", StringComparison.Ordinal)
			.Replace("pinky0", "Pinky", StringComparison.Ordinal)
			.Replace("thumb0", "Thumb", StringComparison.Ordinal);
		return finger == sourceBone[..sideSeparator]
			? null
			: $"mixamorig_{side}Hand{finger}";
	}

	private static readonly Dictionary<string, string> CoreBoneMap = new()
	{
		["hips"] = "mixamorig_Hips",
		["spine"] = "mixamorig_Spine",
		["spineProxy"] = "mixamorig_Spine1",
		["chest"] = "mixamorig_Spine2",
		["neck"] = "mixamorig_Neck",
		["head"] = "mixamorig_Head",
		["shoulder.L"] = "mixamorig_LeftShoulder",
		["upperArm.L"] = "mixamorig_LeftArm",
		["forearm.L"] = "mixamorig_LeftForeArm",
		["hand.L"] = "mixamorig_LeftHand",
		["shoulder.R"] = "mixamorig_RightShoulder",
		["upperArm.R"] = "mixamorig_RightArm",
		["forearm.R"] = "mixamorig_RightForeArm",
		["hand.R"] = "mixamorig_RightHand",
		["thigh.L"] = "mixamorig_LeftUpLeg",
		["shin.L"] = "mixamorig_LeftLeg",
		["foot.L"] = "mixamorig_LeftFoot",
		["toe.L"] = "mixamorig_LeftToeBase",
		["thigh.R"] = "mixamorig_RightUpLeg",
		["shin.R"] = "mixamorig_RightLeg",
		["foot.R"] = "mixamorig_RightFoot",
		["toe.R"] = "mixamorig_RightToeBase",
	};

	private static AnimationNodeAnimation CreateAnimationNode(string animationName)
	{
		return new AnimationNodeAnimation
		{
			Animation = animationName
		};
	}

	private static void KeepRootMotionInPlace(Animation animation)
	{
		for (int track = 0; track < animation.GetTrackCount(); track++)
		{
			if (animation.TrackGetType(track) != Animation.TrackType.Position3D ||
				!animation.TrackGetPath(track).ToString().EndsWith(":mixamorig_Hips"))
			{
				continue;
			}

			Vector3 startingPosition = animation.TrackGetKeyValue(track, 0).AsVector3();
			for (int key = 0; key < animation.TrackGetKeyCount(track); key++)
			{
				Vector3 position = animation.TrackGetKeyValue(track, key).AsVector3();
				position.X = startingPosition.X;
				position.Z = startingPosition.Z;
				animation.TrackSetKeyValue(track, key, position);
			}
		}
	}

	private static T? FindDescendant<T>(Node node) where T : Node
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is T match)
			{
				return match;
			}

			T? descendant = FindDescendant<T>(child);
			if (descendant is not null)
			{
				return descendant;
			}
		}

		return null;
	}
}
