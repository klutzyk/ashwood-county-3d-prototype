#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Items;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Weapons;

namespace AshwoodCounty3DPrototype.Tests;

public partial class PlayerMeleeAnimationValidation : Node
{
	private static readonly string[] AssetPaths =
	{
		"res://assets/characters/player/anim/Standing Melee Attack Downward.fbx",
	};
	private static readonly string[] OneHandAssetPaths =
	{
		"res://assets/characters/player/mix_anim/1h/standing idle.fbx",
		"res://assets/characters/player/mix_anim/1h/standing walk forward.fbx",
		"res://assets/characters/player/mix_anim/1h/standing run forward.fbx",
		"res://assets/characters/player/mix_anim/1h/standing melee combo attack ver. 1.fbx",
		"res://assets/characters/player/mix_anim/1h/standing melee combo attack ver. 2.fbx",
		"res://assets/characters/player/mix_anim/1h/standing melee combo attack ver. 3.fbx",
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
					Require(animationName == "mixamo_com" &&
						animation.GetTrackCount() == 53,
						$"{assetPath} imports the expected Mixamo animation tracks");
				}
				root.Free();
			}
			foreach (string assetPath in OneHandAssetPaths)
			{
				Node root = GD.Load<PackedScene>(assetPath).Instantiate();
				AnimationPlayer player = FindAnimationPlayer(root)
					?? throw new InvalidOperationException($"{assetPath} has no AnimationPlayer.");
				Require(player.HasAnimation("mixamo_com"),
					$"{assetPath} imports its Mixamo animation");
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
					"Visual/Warrior/Skeleton3D/RightHandWeaponAttachment");
			combat.SetProcess(false);

			Require(combat.TryAttack() &&
				animationController.LastMeleeAnimationName == "MeleeAttackDownward",
				"left click starts the standing downward attack");
			Require(!combat.RequestAttack(),
				"clicks before frame 42 cannot restart the attack");
			combat._Process((combat.AttackDuration * combat.AttackRestartMoment) + 0.01f);
			Require(combat.RequestAttack() && combat.IsAttacking &&
				animationController.LastMeleeAnimationName == "MeleeAttackDownward",
				"clicks during recovery immediately restart the downward attack");
			combat._Process(combat.AttackDuration);
			Require(!combat.IsAttacking &&
				attachment.CurrentPoseName ==
					WeaponAttachmentController.TwoHandIdlePoseName,
				"downward attack recovers to the two-handed idle");

			MeleeWeaponDefinition axe = GD.Load<MeleeWeaponDefinition>(
				"res://assets/weapons/wooden_axe.tres");
			MeleeWeaponDefinition hammer = GD.Load<MeleeWeaponDefinition>(
				"res://assets/weapons/wooden_hammer.tres");
			Require(
				axe.Attachment?.Handedness == WeaponHandedness.OneHanded &&
				hammer.Attachment?.Handedness == WeaponHandedness.OneHanded,
				"axe and hammer use one-handed attachment definitions");
			Node3D axeScene = axe.Attachment!.WeaponScene!.Instantiate<Node3D>();
			Node3D hammerScene = hammer.Attachment!.WeaponScene!.Instantiate<Node3D>();
			Require(
				axeScene.HasNode("GripOffset") &&
				hammerScene.HasNode("GripOffset"),
				"axe and hammer instantiate through the shared weapon scene hierarchy");
			axeScene.Free();
			hammerScene.Free();

			animationController.SetWeaponHandedness(WeaponHandedness.OneHanded);
			animationController._Process(0.2);
			Require(
				Mathf.IsEqualApprox(
					animationController
						.Get("parameters/WeaponIdle/blend_amount")
						.AsSingle(),
					1.0f),
				"one-handed weapons select their authored locomotion set");
			for (int comboStep = 1; comboStep <= 3; comboStep++)
			{
				animationController.PlayMeleeAttack(comboStep, combat.AttackDuration);
				Require(
					animationController.LastMeleeAnimationName ==
						$"OneHandCombo{comboStep}",
					$"one-handed combo step {comboStep} selects its authored clip");
			}

			GD.Print("PLAYER_MELEE_ANIMATION_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError($"PLAYER_MELEE_ANIMATION_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
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
