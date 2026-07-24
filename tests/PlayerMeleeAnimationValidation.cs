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
		"res://assets/characters/player/anim/Standing Melee Attack Downward.fbx",
		"res://assets/characters/player/anim/Standing Melee Combo Attack Ver. 2.fbx",
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
			combat.SetProcess(false);

			Require(combat.TryAttack() &&
				animationController.LastMeleeAnimationName == "MeleeAttackDownward",
				"a single input starts the standing downward attack");
			combat._Process(0.12f);
			Require(combat.RequestAttack(),
				"second quick input is buffered during downward anticipation");
			combat._Process(0.12f);
			Require(combat.RequestAttack() &&
				combat.IsUsingAuthoredCombo &&
				animationController.LastMeleeAnimationName == "MeleeComboAttack",
				"three quick inputs crossfade into authored Combo Ver. 2");
			Require(attachment.CurrentPoseName ==
				WeaponAttachmentController.MeleeComboPoseName,
				"authored combo selects its clipping-conscious bat grip");

			combat._Process(combat.AuthoredComboDuration * 0.36f);
			Require(combat.AuthoredComboHitsApplied == 1,
				"first combo hit follows the first authored motion peak");
			combat._Process(combat.AuthoredComboDuration * 0.12f);
			Require(combat.AuthoredComboHitsApplied == 2,
				"second combo hit follows the backhand motion peak");
			combat._Process(combat.AuthoredComboDuration * 0.13f);
			Require(combat.AuthoredComboHitsApplied == 3,
				"third combo hit follows the finishing motion peak");
			combat._Process(combat.AuthoredComboDuration * 0.4f);
			Require(!combat.IsAttacking && !combat.IsUsingAuthoredCombo,
				"authored combo includes recovery and returns input control");

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
