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
		"res://assets/characters/player/anim/comboright.fbx",
		"res://assets/characters/player/anim/comboleft.fbx",
		"res://assets/characters/player/anim/combodown.fbx",
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
				animationController.LastMeleeAnimationName == "MeleeComboRight",
				"first input starts the authored right combo stage");
			Require(combat.RequestAttack() && combat.RequestAttack() &&
				combat.QueuedComboAttacks == 2,
				"two additional quick inputs queue two separate attacks");
			Require(!combat.RequestAttack(),
				"the three-click combo rejects further animation spam");

			combat._Process(combat.AttackDuration);
			Require(combat.ComboStep == 2 &&
				animationController.LastMeleeAnimationName == "MeleeComboLeft",
				"second click starts the authored left combo stage");
			combat._Process(combat.AttackDuration);
			Require(combat.ComboStep == 3 &&
				animationController.LastMeleeAnimationName == "MeleeComboDown",
				"third click starts the authored downward combo stage");
			combat._Process(combat.AttackDuration);
			Require(!combat.IsAttacking &&
				attachment.CurrentPoseName ==
					WeaponAttachmentController.TwoHandIdlePoseName,
				"third attack recovers to the two-handed idle");

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
