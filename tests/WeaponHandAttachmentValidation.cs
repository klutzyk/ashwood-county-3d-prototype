#nullable enable

using System;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Player;
using AshwoodCounty3DPrototype.Weapons;

namespace AshwoodCounty3DPrototype.Tests;

public partial class WeaponHandAttachmentValidation : Node
{
	private const string AttachmentPath =
		"Player/Visual/Remy/Skeleton3D/RightHandWeaponAttachment";

	public override async void _Ready()
	{
		try
		{
			PackedScene worldResource =
				GD.Load<PackedScene>("res://scenes/prototype_world.tscn");
			Node world = worldResource.Instantiate();
			ThirdPersonPlayer player = world.GetNode<ThirdPersonPlayer>("Player");
			world.RemoveChild(player);
			world.Free();
			worldResource.Dispose();
			AddChild(player);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			PlayerMeleeCombat combat = player.GetNode<PlayerMeleeCombat>("MeleeCombat");
			Node3D weaponPivot = combat.GetNode<Node3D>("WeaponPivot");
			Require(weaponPivot.GetChildCount() == 0,
				"legacy floating bat visual is removed without removing WeaponPivot");

			WeaponAttachmentController attachment =
				player.GetNode<WeaponAttachmentController>(
				AttachmentPath["Player/".Length..]);
			Require(attachment.BoneName == "mixamorig_RightHand",
				"weapon attachment remains bound to Remy's right hand");
			Require(attachment.Definition is not null &&
				attachment.Definition.Handedness == WeaponHandedness.TwoHanded,
				"baseball bat declares its reusable two-handed attachment profile");

			Node3D bat = attachment.GetNode<Node3D>("GripPoseOffset/BaseballBat");
			Require(bat.SceneFilePath == "res://assets/weapons/baseball_bat.tscn",
				"attachment framework instances the reusable baseball bat scene");
			Require(bat.Transform.IsEqualApprox(Transform3D.Identity),
				"bat scene mounts without duplicating its asset-owned base grip");

			Node3D gripOffset = bat.GetNode<Node3D>("GripOffset");
			Require(gripOffset.Position.IsEqualApprox(
				new Vector3(0, -0.04148422f, -0.04133047f)),
				"reusable bat scene stores its grip position");
			Require(!gripOffset.Basis.IsEqualApprox(Basis.Identity),
				"reusable bat scene stores its grip rotation");
			Require(gripOffset.FindChildren("*", "GeometryInstance3D", true, false)
				.Cast<GeometryInstance3D>().Any(geometry => geometry.Visible),
				"hand attachment contains one visible bat model");

			PlayerAnimationController animation =
				player.GetNode<PlayerAnimationController>("AnimationTree");
			animation._Process(0.2);
			float idleBlend =
				animation.Get("parameters/IdleType/blend_amount").AsSingle();
			Require(Mathf.IsEqualApprox(idleBlend, 1.0f),
				"stationary player retains the two-handed idle animation");
			Require(attachment.CurrentPoseName ==
				WeaponAttachmentController.TwoHandIdlePoseName,
				"stationary animation selects the authored two-hand grip compromise");

			combat.SetProcess(false);
			player.Velocity = Vector3.Forward;
			combat._Process(0.02);
			attachment._Process(0.2);
			Require(attachment.CurrentPoseName ==
					WeaponAttachmentController.LocomotionPoseName &&
				!attachment.GripTransform.IsEqualApprox(Transform3D.Identity),
				"generic locomotion receives a small clipping correction");
			player.Velocity = Vector3.Zero;
			combat._Process(0.02);
			attachment._Process(0.2);

			Vector3 readyRotation = weaponPivot.RotationDegrees;
			Require(combat.TryAttack(), "melee attack still starts");
			combat._Process(0.1);
			attachment._Process(0.1);
			Require(!weaponPivot.RotationDegrees.IsEqualApprox(readyRotation),
				"WeaponPivot still drives the gameplay attack pose");
			Require(attachment.CurrentPoseName ==
					WeaponAttachmentController.MeleeAttackPoseName &&
				attachment.IsAncestorOf(bat),
				"UAL attack selects its corrective grip while remaining hand-bound");
			combat._Process(0.6);
			Require(!combat.IsAttacking,
				"melee attack still completes normally");
			Require(attachment.CurrentPoseName ==
				WeaponAttachmentController.TwoHandIdlePoseName,
				"attachment recovers to the current locomotion grip after attack");

			attachment.SetGripPose("MissingPose", immediate: true);
			Require(attachment.GripTransform.IsEqualApprox(
					attachment.Definition!.DefaultGripTransform),
				"undefined animation poses safely use the weapon's default grip");

			player.QueueFree();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			GD.Print("WEAPON_HAND_ATTACHMENT_VALIDATION: PASS");
			GetTree().Quit(0);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"WEAPON_HAND_ATTACHMENT_VALIDATION: FAIL - {exception.Message}");
			GetTree().Quit(1);
		}
	}

	private static void Require(bool condition, string message)
	{
		if (!condition)
		{
			throw new InvalidOperationException(message);
		}
	}
}
