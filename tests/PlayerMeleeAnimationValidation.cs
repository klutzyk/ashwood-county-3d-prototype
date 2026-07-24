#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Weapons;

namespace AshwoodCounty3DPrototype.Tests;

public partial class PlayerMeleeAnimationValidation : Node
{
	private static readonly string[] AssetPaths =
	{
		"res://assets/characters/player/anim/Animations/Male/Combat/2H/HumanM@CombatIdle2H01.fbx",
		"res://assets/characters/player/anim/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx",
		"res://assets/characters/player/anim/Animations/Male/Combat/HumanM@CombatDamage01.fbx",
	};

	public override async void _Ready()
	{
		try
		{
			foreach (string assetPath in AssetPaths)
			{
				Node root = GD.Load<PackedScene>(assetPath).Instantiate();
				AnimationPlayer player = FindAnimationPlayer(root)
					?? throw new InvalidOperationException($"{assetPath} has no AnimationPlayer.");
				foreach (StringName animationName in player.GetAnimationList())
				{
					if (animationName == "RESET")
					{
						continue;
					}
					Animation animation = player.GetAnimation(animationName);
					Require(animation.GetTrackCount() > 0,
						$"{assetPath} imports animation tracks");
				}
				root.Free();
			}

			Node world = GD.Load<PackedScene>("res://scenes/prototype_world.tscn").Instantiate();
			AddChild(world);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			ThirdPersonPlayer playerCharacter =
				world.GetNode<ThirdPersonPlayer>("Player");
			PlayerMeleeCombat combat =
				playerCharacter.GetNode<PlayerMeleeCombat>("MeleeCombat");
			PlayerAnimationController animationController =
				playerCharacter.GetNode<PlayerAnimationController>("AnimationTree");
			WeaponAttachmentController attachment =
				playerCharacter.GetNode<WeaponAttachmentController>(
					"Visual/Remy/Skeleton3D/RightHandWeaponAttachment");
			AnimationPlayer runtimeAnimationPlayer = FindAnimationPlayer(playerCharacter)
				?? throw new InvalidOperationException("Player has no AnimationPlayer.");
			Skeleton3D remySkeleton = attachment.GetParent<Skeleton3D>();
			combat.SetProcess(false);

			Require(runtimeAnimationPlayer.HasAnimation("TwoHandIdle") &&
				runtimeAnimationPlayer.HasAnimation("MeleeAttack2H01") &&
				runtimeAnimationPlayer.HasAnimation("CombatDamage01"),
				"the minimal two-handed animation set is loaded");
			Require(HasValidRemyTracks(
					runtimeAnimationPlayer,
					runtimeAnimationPlayer.GetAnimation("TwoHandIdle")) &&
				HasValidRemyTracks(
					runtimeAnimationPlayer,
					runtimeAnimationPlayer.GetAnimation("MeleeAttack2H01")),
				"new idle and attack tracks resolve to Remy's skeleton");
			Require(runtimeAnimationPlayer.GetAnimation("Walk").GetTrackCount() > 0 &&
				runtimeAnimationPlayer.GetAnimation("Run").GetTrackCount() > 0,
				"walking and sprinting animations remain available");
			Require(attachment.BoneName == "mixamorig_RightHand" &&
				attachment.EquippedWeapon is not null,
				"the baseball bat remains attached to the right palm");
			for (int frame = 0; frame < 10; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Require(IsUpperBodyAnimated(remySkeleton),
				"two-handed idle produces an animated pose instead of a T-pose");
			SaveRuntimeFrame("idle");

			Input.ActionPress("move_forward");
			for (int frame = 0; frame < 8; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			Require(new Vector2(playerCharacter.Velocity.X, playerCharacter.Velocity.Z)
					.Length() > 0.1f,
				"walking still animates while armed");
			Input.ActionPress("run");
			for (int frame = 0; frame < 8; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			Require(playerCharacter.IsSprinting,
				"sprinting still animates while armed");
			Input.ActionRelease("run");
			Input.ActionRelease("move_forward");
			for (int frame = 0; frame < 30; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			Require(new Vector2(playerCharacter.Velocity.X, playerCharacter.Velocity.Z)
					.Length() < 0.1f,
				"player returns to idle after locomotion");

			Require(combat.TryAttack() &&
				animationController.LastMeleeAnimationName == "MeleeAttack2H01",
				"left click starts the new two-handed attack");
			for (int frame = 0; frame < 10; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Require(IsUpperBodyAnimated(remySkeleton),
				"left click produces an animated attack pose instead of a T-pose");
			SaveRuntimeFrame("attack");
			Require(!combat.RequestAttack(),
				"clicks before frame 42 cannot restart the attack");
			combat._Process((combat.AttackDuration * combat.AttackRestartMoment) + 0.01f);
			Require(combat.RequestAttack() && combat.IsAttacking &&
				animationController.LastMeleeAnimationName == "MeleeAttack2H01",
				"clicks during recovery immediately restart the two-handed attack");
			combat._Process(combat.AttackDuration);
			for (int frame = 0; frame < 50; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			Require(!combat.IsAttacking &&
				attachment.CurrentPoseName ==
					WeaponAttachmentController.TwoHandIdlePoseName,
				"attack recovers to the two-handed idle");
			Require(IsUpperBodyAnimated(remySkeleton),
				"recovery returns to an animated idle");
			SaveRuntimeFrame("recovery");

			GD.Print("PLAYER_MELEE_ANIMATION_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"PLAYER_MELEE_ANIMATION_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static bool IsUpperBodyAnimated(Skeleton3D skeleton)
	{
		int rightArm = skeleton.FindBone("mixamorig_RightArm");
		int leftArm = skeleton.FindBone("mixamorig_LeftArm");
		return rightArm >= 0 && leftArm >= 0 &&
			!skeleton.GetBonePoseRotation(rightArm).IsEqualApprox(Quaternion.Identity) &&
			!skeleton.GetBonePoseRotation(leftArm).IsEqualApprox(Quaternion.Identity);
	}

	private void SaveRuntimeFrame(string state)
	{
		string path = ProjectSettings.GlobalizePath(
			$"user://player_melee_validation_{state}.png");
		GetViewport().GetTexture().GetImage().SavePng(path);
		GD.Print($"PLAYER_MELEE_FRAME: {path}");
	}

	private static bool HasValidRemyTracks(
		AnimationPlayer animationPlayer,
		Animation animation)
	{
		if (animation.GetTrackCount() < 20)
		{
			return false;
		}

		Node animationRoot = animationPlayer.GetNode(animationPlayer.RootNode);
		for (int track = 0; track < animation.GetTrackCount(); track++)
		{
			string path = animation.TrackGetPath(track).ToString();
			int separator = path.LastIndexOf(':');
			if (separator < 0 ||
				animationRoot.GetNodeOrNull(new NodePath(path[..separator])) is not
					Skeleton3D skeleton ||
				skeleton.FindBone(path[(separator + 1)..]) < 0)
			{
				return false;
			}
		}

		return true;
	}

	private static AnimationPlayer? FindAnimationPlayer(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is AnimationPlayer player)
			{
				return player;
			}

			AnimationPlayer? descendant = FindAnimationPlayer(child);
			if (descendant is not null)
			{
				return descendant;
			}
		}

		return null;
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
