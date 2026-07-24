#nullable enable

using System;
using Godot;

namespace AshwoodCounty3DPrototype.Weapons;

public partial class WeaponAttachmentController : BoneAttachment3D
{
	public static readonly StringName DefaultPoseName = new("Default");
	public static readonly StringName TwoHandIdlePoseName = new("TwoHandIdle");
	public static readonly StringName LocomotionPoseName = new("Locomotion");
	public static readonly StringName MeleeAttackPoseName = new("MeleeAttack");
	public static readonly StringName MeleeComboPoseName = new("MeleeCombo");

	public WeaponAttachmentDefinition? Definition { get; private set; }
	public Node3D? EquippedWeapon { get; private set; }
	public StringName CurrentPoseName { get; private set; } = DefaultPoseName;
	public Transform3D GripTransform => _gripPivot?.Transform ?? Transform3D.Identity;

	private Node3D? _gripPivot;
	private Transform3D _blendStart = Transform3D.Identity;
	private Transform3D _blendTarget = Transform3D.Identity;
	private float _blendDuration;
	private float _blendElapsed;

	public override void _Process(double delta)
	{
		if (_gripPivot is null || _blendElapsed >= _blendDuration)
		{
			return;
		}

		_blendElapsed = Mathf.Min(_blendElapsed + (float)delta, _blendDuration);
		float progress = _blendDuration <= 0.0f ? 1.0f : _blendElapsed / _blendDuration;
		_gripPivot.Transform = _blendStart.InterpolateWith(
			_blendTarget,
			Mathf.SmoothStep(0.0f, 1.0f, progress));
	}

	public void Equip(WeaponAttachmentDefinition definition)
	{
		if (definition.WeaponScene is null)
		{
			throw new InvalidOperationException(
				"Weapon attachment definition requires a weapon scene.");
		}

		Unequip();
		Definition = definition;
		_gripPivot = new Node3D { Name = "GripPoseOffset" };
		AddChild(_gripPivot);
		EquippedWeapon = definition.WeaponScene.Instantiate<Node3D>();
		_gripPivot.AddChild(EquippedWeapon);
		CurrentPoseName = DefaultPoseName;
		_gripPivot.Transform = definition.DefaultGripTransform;
		_blendStart = _gripPivot.Transform;
		_blendTarget = _gripPivot.Transform;
		_blendDuration = 0.0f;
		_blendElapsed = 0.0f;
	}

	public void Unequip()
	{
		if (_gripPivot is not null)
		{
			RemoveChild(_gripPivot);
			_gripPivot.QueueFree();
		}

		EquippedWeapon = null;
		_gripPivot = null;
		Definition = null;
		CurrentPoseName = DefaultPoseName;
	}

	public void SetGripPose(StringName poseName, bool immediate = false)
	{
		if (Definition is null || CurrentPoseName == poseName)
		{
			return;
		}

		CurrentPoseName = poseName;
		Transform3D target = Definition.DefaultGripTransform;
		float duration = 0.0f;
		if (Definition.TryGetGripPose(poseName, out WeaponGripPose? gripPose) &&
			gripPose is not null)
		{
			target *= gripPose.CreateTransform();
			duration = Mathf.Max(gripPose.BlendDuration, 0.0f);
		}

		if (_gripPivot is null)
		{
			return;
		}

		_blendStart = _gripPivot.Transform;
		_blendTarget = target;
		_blendDuration = immediate ? 0.0f : duration;
		_blendElapsed = 0.0f;
		if (_blendDuration <= 0.0f)
		{
			_gripPivot.Transform = _blendTarget;
		}
	}
}
