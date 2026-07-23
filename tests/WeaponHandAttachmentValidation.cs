#nullable enable

using System;
using System.Linq;
using Godot;
using AshwoodCounty3DPrototype.Player;

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

			BoneAttachment3D attachment = player.GetNode<BoneAttachment3D>(
				AttachmentPath["Player/".Length..]);
			Require(attachment.BoneName == "mixamorig_RightHand",
				"weapon attachment remains bound to Remy's right hand");

			Node3D bat = attachment.GetNode<Node3D>("BaseballBat");
			Require(bat.SceneFilePath == "res://assets/weapons/baseball_bat.tscn",
				"right hand instances the reusable baseball bat scene");
			Require(bat.Transform.IsEqualApprox(Transform3D.Identity),
				"bat scene mounts directly at the hand attachment");

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

			combat.SetProcess(false);
			Vector3 readyRotation = weaponPivot.RotationDegrees;
			Require(combat.TryAttack(), "melee attack still starts");
			combat._Process(0.1);
			Require(!weaponPivot.RotationDegrees.IsEqualApprox(readyRotation),
				"WeaponPivot still drives the gameplay attack pose");
			Require(bat.GetParent() == attachment &&
				bat.GlobalTransform.IsEqualApprox(attachment.GlobalTransform),
				"visible bat remains attached to the animated right hand during attack");
			combat._Process(0.6);
			Require(!combat.IsAttacking,
				"melee attack still completes normally");

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
