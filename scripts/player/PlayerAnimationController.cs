#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Weapons;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerAnimationController : AnimationTree
{
	private const string SourceAnimationName = "mixamo_com";
	private const string IdleAnimationName = "WarriorIdle";
	private const string WalkAnimationName = "Walk";
	private const string RunAnimationName = "Run";
	private const string TwoHandIdleAnimationName = "TwoHandIdle";
	private const string MeleeAttackAnimationName = "MeleeAttackDownward";
	private const string OneHandIdleAnimationName = "OneHandIdle";
	private const string OneHandWalkAnimationName = "OneHandWalk";
	private const string OneHandCrouchIdleAnimationName = "OneHandCrouchIdle";
	private const string CrouchWalkAnimationName = "CrouchWalk";
	private const string OneHandStandTransitionAnimationName =
		"OneHandCrouchToStand";
	private const string OneHandJumpAnimationName = "OneHandJump";
	private const string TwoHandIdlePath =
		"res://assets/characters/player/2hand Idle.fbx";
	private const string MeleeAttackPath =
		"res://assets/characters/player/anim/Standing Melee Attack Downward.fbx";
	private const string OneHandAnimationDirectory =
		"res://assets/characters/player/mix_anim/1h/";
	private const string IdlePath =
		"res://assets/characters/player/mix_anim/WarriorIdle_withskin.fbx";
	private const string WalkPath = "res://assets/characters/player/Walking.fbx";
	private const string RunPath = "res://assets/characters/player/Fast Run.fbx";
	private const string CrouchWalkPath =
		"res://assets/characters/player/Crouched Walking.fbx";
	private const float BlendSpeed = 8.0f;
	private static readonly string[] OneHandAttackAnimationNames =
	{
		"OneHandHorizontal",
		"OneHandDownward",
		"OneHand360Low",
	};
	private static readonly string[] OneHandAttackPaths =
	{
		OneHandAnimationDirectory + "Standing Melee Attack Horizontal.fbx",
		OneHandAnimationDirectory + "Standing Melee Attack Downward.fbx",
		OneHandAnimationDirectory + "Standing Melee Attack 360 Low.fbx",
	};

	private ThirdPersonPlayer _player = null!;
	private float _idleWalkBlend;
	private float _runBlend;
	private float _twoHandIdleBlend;
	private float _oneHandBlend;
	private float _crouchBlend;
	private WeaponHandedness _weaponHandedness = WeaponHandedness.TwoHanded;
	private bool _wasCrouching;
	private bool _wasAirborne;
	private AnimationPlayer _animationPlayer = null!;
	private float _meleeAnimationLength;
	private float _oneHandJumpAnimationLength;
	private readonly float[] _oneHandMeleeAnimationLengths = new float[3];
	private AnimationNodeAnimation _attackClipNode = null!;
	public StringName LastMeleeAnimationName { get; private set; } = new();

	public void SetTwoHandedWeaponEquipped(bool equipped)
	{
		SetWeaponHandedness(
			equipped ? WeaponHandedness.TwoHanded : WeaponHandedness.OneHanded);
	}

	public void SetWeaponHandedness(WeaponHandedness handedness)
	{
		_weaponHandedness = handedness;
	}
	
	public override void _Ready()
	{
		_player = GetParent<ThirdPersonPlayer>();
		_animationPlayer = FindDescendant<AnimationPlayer>(_player)
			?? throw new InvalidOperationException(
				"Visible player character is missing an AnimationPlayer.");

		AddLocomotionAnimations(_animationPlayer);
		AddMeleeAnimations(_animationPlayer);
		ConfigureBlendTree(_animationPlayer);
		_wasAirborne = _player.IsAirborne;
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
		float crouchMoveTarget = _player.IsCrouching
			? Mathf.Clamp(
				horizontalSpeed / Mathf.Max(_player.CrouchSpeed, 0.1f),
				0.0f,
				1.0f)
			: 0.0f;

		float blendStep = BlendSpeed * (float)delta;

		float twoHandTarget =
			_weaponHandedness == WeaponHandedness.TwoHanded
				? 1.0f
				: 0.0f;
		float oneHandTarget =
			_weaponHandedness == WeaponHandedness.OneHanded
				? 1.0f
				: 0.0f;

		_twoHandIdleBlend = Mathf.MoveToward(
			_twoHandIdleBlend,
			twoHandTarget,
			blendStep
		);
		_oneHandBlend = Mathf.MoveToward(
			_oneHandBlend,
			oneHandTarget,
			blendStep);
		bool isOneHanded =
			_weaponHandedness == WeaponHandedness.OneHanded;
		bool isCrouching = isOneHanded && _player.IsCrouching;
		bool isAirborne = isOneHanded && _player.IsAirborne;
		_crouchBlend = Mathf.MoveToward(
			_crouchBlend,
			isCrouching ? 1.0f : 0.0f,
			blendStep);

		if (_wasCrouching && !isCrouching)
		{
			Set("parameters/StandTransition/request", 1);
		}
		if (!_wasAirborne && isAirborne)
		{
			float airborneDuration =
				(2.0f * Mathf.Max(_player.JumpVelocity, 0.1f)) /
				Mathf.Max(_player.Gravity, 0.1f);
			Set(
				"parameters/JumpSpeed/scale",
				_oneHandJumpAnimationLength / airborneDuration);
			Set("parameters/JumpTransition/request", 1);
		}
		_wasCrouching = isCrouching;
		_wasAirborne = isAirborne;

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
		Set("parameters/WeaponIdle/blend_amount", _oneHandBlend);
		Set("parameters/WeaponWalk/blend_amount", _oneHandBlend);
		Set("parameters/CrouchMove/blend_amount", crouchMoveTarget);
		Set("parameters/CrouchBlend/blend_amount", _crouchBlend);
		Set("parameters/IdleWalk/blend_amount", _idleWalkBlend);
		Set("parameters/RunBlend/blend_amount", _runBlend);
	}

	public void PlayMeleeAttack(int comboStep, float attackDuration)
	{
		string animationName = MeleeAttackAnimationName;
		float animationLength = _meleeAnimationLength;
		if (_weaponHandedness == WeaponHandedness.OneHanded)
		{
			int attackIndex = Mathf.Clamp(comboStep - 1, 0, 2);
			animationName = OneHandAttackAnimationNames[attackIndex];
			animationLength = _oneHandMeleeAnimationLengths[attackIndex];
		}

		float duration = Mathf.Max(attackDuration, 0.05f);
		_attackClipNode.Animation = animationName;
		Set("parameters/AttackSpeed/scale", animationLength / duration);
		Set("parameters/MeleeAttack/request", 1);
		LastMeleeAnimationName = animationName;
	}

	private void AddLocomotionAnimations(AnimationPlayer animationPlayer)
	{
		AnimationLibrary library = animationPlayer.GetAnimationLibrary("");
		float oneHandStandingHipsHeight =
			GetInitialHipsHeight(
				OneHandAnimationDirectory + "standing idle.fbx");

		AddAnimation(library, IdleAnimationName, IdlePath);
		AddAnimation(library, TwoHandIdleAnimationName, TwoHandIdlePath);
		AddAnimation(library, WalkAnimationName, WalkPath);
		AddAnimation(library, RunAnimationName, RunPath);
		AddAnimation(
			library,
			OneHandIdleAnimationName,
			OneHandAnimationDirectory + "standing idle.fbx");
		AddAnimation(
			library,
			OneHandWalkAnimationName,
			OneHandAnimationDirectory + "standing walk forward.fbx");
		AddAnimation(
			library,
			OneHandCrouchIdleAnimationName,
			OneHandAnimationDirectory + "crouch idle.fbx",
			hipsHeightReference: oneHandStandingHipsHeight);
		AddAnimation(
			library,
			CrouchWalkAnimationName,
			CrouchWalkPath,
			hipsHeightReference: oneHandStandingHipsHeight);
		AddAnimation(
			library,
			OneHandStandTransitionAnimationName,
			OneHandAnimationDirectory + "crouch to standing idle.fbx",
			shouldLoop: false,
			hipsHeightReference: oneHandStandingHipsHeight);
		_oneHandJumpAnimationLength = AddAnimation(
			library,
			OneHandJumpAnimationName,
			OneHandAnimationDirectory + "standing jump.fbx",
			shouldLoop: false);
	}

	private static float AddAnimation(
		AnimationLibrary library,
		string name,
		string assetPath,
		bool shouldLoop = true,
		float? hipsHeightReference = null)
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
		MakeHipsTranslationInPlace(animation, hipsHeightReference);
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
			"OneHandIdle",
			CreateAnimationNode(OneHandIdleAnimationName),
			new Vector2(-700.0f, 40.0f));
		blendTree.AddNode(
			"WeaponIdle",
			new AnimationNodeBlend2(),
			new Vector2(-260.0f, -120.0f));

		blendTree.AddNode(
			"Walk",
			CreateAnimationNode(WalkAnimationName),
			new Vector2(-480.0f, 40.0f)
		);
		blendTree.AddNode(
			"OneHandWalk",
			CreateAnimationNode(OneHandWalkAnimationName),
			new Vector2(-480.0f, 140.0f));
		blendTree.AddNode(
			"WeaponWalk",
			new AnimationNodeBlend2(),
			new Vector2(-260.0f, 40.0f));

		blendTree.AddNode(
			"IdleWalk",
			new AnimationNodeBlend2(),
			new Vector2(-40.0f, -40.0f)
		);

		blendTree.AddNode(
			"Run",
			CreateAnimationNode(RunAnimationName),
			new Vector2(-260.0f, 160.0f)
		);
		blendTree.AddNode(
			"RunBlend",
			new AnimationNodeBlend2(),
			new Vector2(180.0f, 0.0f)
		);
		blendTree.AddNode(
			"CrouchIdle",
			CreateAnimationNode(OneHandCrouchIdleAnimationName),
			new Vector2(-40.0f, 320.0f));
		blendTree.AddNode(
			"CrouchWalk",
			CreateAnimationNode(CrouchWalkAnimationName),
			new Vector2(-40.0f, 440.0f));
		blendTree.AddNode(
			"CrouchMove",
			new AnimationNodeBlend2(),
			new Vector2(180.0f, 360.0f));
		blendTree.AddNode(
			"CrouchBlend",
			new AnimationNodeBlend2(),
			new Vector2(360.0f, 40.0f));

		blendTree.AddNode(
			"StandTransitionClip",
			CreateAnimationNode(OneHandStandTransitionAnimationName),
			new Vector2(140.0f, 360.0f));
		AnimationNodeOneShot standTransition = new();
		standTransition.Set("fadein_time", 0.08f);
		standTransition.Set("fadeout_time", 0.12f);
		blendTree.AddNode(
			"StandTransition",
			standTransition,
			new Vector2(560.0f, 40.0f));

		blendTree.AddNode(
			"JumpClip",
			CreateAnimationNode(OneHandJumpAnimationName),
			new Vector2(360.0f, 360.0f));
		blendTree.AddNode(
			"JumpSpeed",
			new AnimationNodeTimeScale(),
			new Vector2(560.0f, 320.0f));
		AnimationNodeOneShot jumpTransition = new();
		jumpTransition.Set("fadein_time", 0.06f);
		jumpTransition.Set("fadeout_time", 0.12f);
		blendTree.AddNode(
			"JumpTransition",
			jumpTransition,
			new Vector2(760.0f, 40.0f));

		_attackClipNode = CreateAnimationNode(MeleeAttackAnimationName);
		blendTree.AddNode(
			"AttackClip",
			_attackClipNode,
			new Vector2(560.0f, 420.0f));
		blendTree.AddNode(
			"AttackSpeed",
			new AnimationNodeTimeScale(),
			new Vector2(760.0f, 360.0f));
		AnimationNodeOneShot meleeAttack = new();
		meleeAttack.Set("fadein_time", 0.07f);
		meleeAttack.Set("fadeout_time", 0.14f);
		blendTree.AddNode(
			"MeleeAttack",
			meleeAttack,
			new Vector2(960.0f, 0.0f));

		blendTree.ConnectNode("IdleType", 0, "Idle");
		blendTree.ConnectNode("IdleType", 1, "TwoHandIdle");
		blendTree.ConnectNode("WeaponIdle", 0, "IdleType");
		blendTree.ConnectNode("WeaponIdle", 1, "OneHandIdle");

		blendTree.ConnectNode("WeaponWalk", 0, "Walk");
		blendTree.ConnectNode("WeaponWalk", 1, "OneHandWalk");
		blendTree.ConnectNode("IdleWalk", 0, "WeaponIdle");
		blendTree.ConnectNode("IdleWalk", 1, "WeaponWalk");

		blendTree.ConnectNode("RunBlend", 0, "IdleWalk");
		blendTree.ConnectNode("RunBlend", 1, "Run");
		blendTree.ConnectNode("CrouchMove", 0, "CrouchIdle");
		blendTree.ConnectNode("CrouchMove", 1, "CrouchWalk");
		blendTree.ConnectNode("CrouchBlend", 0, "RunBlend");
		blendTree.ConnectNode("CrouchBlend", 1, "CrouchMove");

		blendTree.ConnectNode("StandTransition", 0, "CrouchBlend");
		blendTree.ConnectNode("StandTransition", 1, "StandTransitionClip");
		blendTree.ConnectNode("JumpSpeed", 0, "JumpClip");
		blendTree.ConnectNode("JumpTransition", 0, "StandTransition");
		blendTree.ConnectNode("JumpTransition", 1, "JumpSpeed");

		blendTree.ConnectNode("AttackSpeed", 0, "AttackClip");
		blendTree.ConnectNode("MeleeAttack", 0, "JumpTransition");
		blendTree.ConnectNode("MeleeAttack", 1, "AttackSpeed");
		blendTree.ConnectNode("output", 0, "MeleeAttack");

		AnimPlayer = GetPathTo(animationPlayer);
		TreeRoot = blendTree;
		Active = true;
	}

	private void AddMeleeAnimations(AnimationPlayer animationPlayer)
	{
		AnimationLibrary library = animationPlayer.GetAnimationLibrary("");
		_meleeAnimationLength = AddAnimation(
			library,
			MeleeAttackAnimationName,
			MeleeAttackPath,
			shouldLoop: false);
		for (int index = 0; index < OneHandAttackAnimationNames.Length; index++)
		{
			_oneHandMeleeAnimationLengths[index] = AddAnimation(
				library,
				OneHandAttackAnimationNames[index],
				OneHandAttackPaths[index],
				shouldLoop: false);
		}
	}

	private static AnimationNodeAnimation CreateAnimationNode(string animationName)
	{
		return new AnimationNodeAnimation
		{
			Animation = animationName
		};
	}

	private static void MakeHipsTranslationInPlace(
		Animation animation,
		float? hipsHeightReference)
	{
		for (int track = animation.GetTrackCount() - 1; track >= 0; track--)
		{
			if (animation.TrackGetType(track) == Animation.TrackType.Position3D &&
				animation.TrackGetPath(track).ToString().EndsWith(":mixamorig_Hips"))
			{
				if (!hipsHeightReference.HasValue)
				{
					animation.RemoveTrack(track);
					continue;
				}

				int keyCount = animation.TrackGetKeyCount(track);
				if (keyCount == 0)
				{
					continue;
				}

				Vector3 referencePosition =
					animation.TrackGetKeyValue(track, 0).AsVector3();
				for (int key = 0; key < keyCount; key++)
				{
					Vector3 position =
						animation.TrackGetKeyValue(track, key).AsVector3();
					position.X = referencePosition.X;
					position.Y -= hipsHeightReference.Value;
					position.Z = referencePosition.Z;
					animation.TrackSetKeyValue(track, key, position);
				}
			}
		}
	}

	private static float GetInitialHipsHeight(string assetPath)
	{
		PackedScene animationScene = ResourceLoader.Load<PackedScene>(assetPath);
		Node sourceRoot = animationScene.Instantiate();
		AnimationPlayer sourcePlayer = FindDescendant<AnimationPlayer>(sourceRoot)
			?? throw new InvalidOperationException(
				$"{assetPath} is missing an AnimationPlayer.");
		Animation animation = sourcePlayer.GetAnimation(SourceAnimationName);
		for (int track = 0; track < animation.GetTrackCount(); track++)
		{
			if (animation.TrackGetType(track) == Animation.TrackType.Position3D &&
				animation.TrackGetPath(track).ToString()
					.EndsWith(":mixamorig_Hips") &&
				animation.TrackGetKeyCount(track) > 0)
			{
				float hipsHeight =
					animation.TrackGetKeyValue(track, 0).AsVector3().Y;
				sourceRoot.Free();
				return hipsHeight;
			}
		}

		sourceRoot.Free();
		throw new InvalidOperationException(
			$"{assetPath} is missing a hips position track.");
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
