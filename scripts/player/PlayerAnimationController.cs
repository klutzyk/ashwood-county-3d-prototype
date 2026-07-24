#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerAnimationController : AnimationTree
{
	private const string SourceAnimationName = "mixamo_com";
	private const string IdleAnimationName = "Idle";
	private const string WalkAnimationName = "Walk";
	private const string RunAnimationName = "Run";
	private const string TwoHandIdleAnimationName = "TwoHandIdle";
	private const string MeleeAttackAnimationName = "MeleeAttackDownward";
	private const string TwoHandIdlePath =
	"res://assets/characters/player/2hand Idle.fbx";
	private const string MeleeAttackPath =
		"res://assets/characters/player/anim/Standing Melee Attack Downward.fbx";
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
		AddAnimation(library, TwoHandIdleAnimationName, TwoHandIdlePath);
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

		blendTree.ConnectNode("IdleType", 0, "Idle");
		blendTree.ConnectNode("IdleType", 1, "TwoHandIdle");

		blendTree.ConnectNode("IdleWalk", 0, "IdleType");
		blendTree.ConnectNode("IdleWalk", 1, "Walk");

		blendTree.ConnectNode("RunBlend", 0, "IdleWalk");
		blendTree.ConnectNode("RunBlend", 1, "Run");

		blendTree.ConnectNode("AttackSpeed", 0, "AttackClip");
		blendTree.ConnectNode("MeleeAttack", 0, "RunBlend");
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
	}

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
