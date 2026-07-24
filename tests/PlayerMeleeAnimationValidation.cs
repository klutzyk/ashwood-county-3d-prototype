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
			combat.SetProcess(false);

			Require(runtimeAnimationPlayer.HasAnimation("TwoHandIdle") &&
				runtimeAnimationPlayer.HasAnimation("MeleeAttack2H01") &&
				runtimeAnimationPlayer.HasAnimation("CombatDamage01"),
				"the minimal two-handed animation set is loaded");
			Require(attachment.BoneName == "mixamorig_RightHand" &&
				attachment.EquippedWeapon is not null,
				"the baseball bat remains attached to the right palm");
			Require(combat.TryAttack() &&
				animationController.LastMeleeAnimationName == "MeleeAttack2H01",
				"left click starts the new two-handed attack");
			Require(!combat.RequestAttack(),
				"clicks before frame 42 cannot restart the attack");
			combat._Process((combat.AttackDuration * combat.AttackRestartMoment) + 0.01f);
			Require(combat.RequestAttack() && combat.IsAttacking &&
				animationController.LastMeleeAnimationName == "MeleeAttack2H01",
				"clicks during recovery immediately restart the two-handed attack");
			combat._Process(combat.AttackDuration);
			Require(!combat.IsAttacking &&
				attachment.CurrentPoseName ==
					WeaponAttachmentController.TwoHandIdlePoseName,
				"attack recovers to the two-handed idle");

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
