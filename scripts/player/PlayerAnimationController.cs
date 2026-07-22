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
	private const string IdlePath = "res://Animations/Idle.fbx";
	private const string WalkPath = "res://Animations/Walking.fbx";
	private const string RunPath = "res://Animations/Fast Run.fbx";
	private const float BlendSpeed = 8.0f;

	private ThirdPersonPlayer _player = null!;
	private float _idleWalkBlend;
	private float _runBlend;

	public override void _Ready()
	{
		_player = GetParent<ThirdPersonPlayer>();
		AnimationPlayer animationPlayer = FindDescendant<AnimationPlayer>(_player)
			?? throw new InvalidOperationException("Remy is missing an AnimationPlayer.");

		AddLocomotionAnimations(animationPlayer);
		ConfigureBlendTree(animationPlayer);
	}

	public override void _Process(double delta)
	{
		float horizontalSpeed = new Vector2(_player.Velocity.X, _player.Velocity.Z).Length();
		float walkTarget = Mathf.Clamp(horizontalSpeed / _player.WalkSpeed, 0.0f, 1.0f);
		float runTarget = Input.IsActionPressed("run") && horizontalSpeed > 0.1f ? 1.0f : 0.0f;
		float blendStep = BlendSpeed * (float)delta;

		_idleWalkBlend = Mathf.MoveToward(_idleWalkBlend, walkTarget, blendStep);
		_runBlend = Mathf.MoveToward(_runBlend, runTarget, blendStep);
		Set("parameters/IdleWalk/blend_amount", _idleWalkBlend);
		Set("parameters/RunBlend/blend_amount", _runBlend);
	}

	private void AddLocomotionAnimations(AnimationPlayer animationPlayer)
	{
		AnimationLibrary library = animationPlayer.GetAnimationLibrary("");
		AddAnimation(library, IdleAnimationName, IdlePath);
		AddAnimation(library, WalkAnimationName, WalkPath);
		AddAnimation(library, RunAnimationName, RunPath);
	}

	private static void AddAnimation(AnimationLibrary library, string name, string assetPath)
	{
		PackedScene animationScene = ResourceLoader.Load<PackedScene>(assetPath);
		Node sourceRoot = animationScene.Instantiate();
		AnimationPlayer sourcePlayer = FindDescendant<AnimationPlayer>(sourceRoot)
			?? throw new InvalidOperationException($"{assetPath} is missing an AnimationPlayer.");
		Animation sourceAnimation = sourcePlayer.GetAnimation(SourceAnimationName);
		Animation animation = (Animation)sourceAnimation.Duplicate(true);

		animation.LoopMode = Animation.LoopModeEnum.Linear;
		KeepRootMotionInPlace(animation);
		library.AddAnimation(name, animation);
		sourceRoot.Free();
	}

	private void ConfigureBlendTree(AnimationPlayer animationPlayer)
	{
		AnimationNodeBlendTree blendTree = new();
		blendTree.AddNode("Idle", CreateAnimationNode(IdleAnimationName), new Vector2(-500.0f, -120.0f));
		blendTree.AddNode("Walk", CreateAnimationNode(WalkAnimationName), new Vector2(-500.0f, 40.0f));
		blendTree.AddNode("IdleWalk", new AnimationNodeBlend2(), new Vector2(-260.0f, -60.0f));
		blendTree.AddNode("Run", CreateAnimationNode(RunAnimationName), new Vector2(-260.0f, 120.0f));
		blendTree.AddNode("RunBlend", new AnimationNodeBlend2(), new Vector2(0.0f, 0.0f));

		blendTree.ConnectNode("IdleWalk", 0, "Idle");
		blendTree.ConnectNode("IdleWalk", 1, "Walk");
		blendTree.ConnectNode("RunBlend", 0, "IdleWalk");
		blendTree.ConnectNode("RunBlend", 1, "Run");
		blendTree.ConnectNode("output", 0, "RunBlend");

		AnimPlayer = GetPathTo(animationPlayer);
		TreeRoot = blendTree;
		Active = true;
	}

	private static AnimationNodeAnimation CreateAnimationNode(string animationName)
	{
		return new AnimationNodeAnimation { Animation = animationName };
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
