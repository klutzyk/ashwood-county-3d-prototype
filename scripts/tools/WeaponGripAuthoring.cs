#nullable enable

using System;
using Godot;
using AshwoodCounty3DPrototype.Weapons;

namespace AshwoodCounty3DPrototype.Tools;

[Tool]
public partial class WeaponGripAuthoring : Node3D
{
	public enum GripPoseSelection
	{
		TwoHandIdle,
		OneHandIdle,
		Locomotion,
		MeleeAttack,
	}

	private const string SourceAnimationName = "mixamo_com";
	private const string TwoHandIdlePath =
		"res://assets/characters/player/2hand Idle.fbx";
	private const string TwoHandLocomotionPath =
		"res://assets/characters/player/Walking.fbx";
	private const string TwoHandMeleeAttackPath =
		"res://assets/characters/player/anim/Standing Melee Attack Downward.fbx";
	private const string OneHandAnimationDirectory =
		"res://assets/characters/player/mix_anim/1h/";
	private const string DefaultAttachmentDefinitionPath =
		"res://assets/weapons/baseball_bat_attachment.tres";

	private string _attachmentDefinitionPath = DefaultAttachmentDefinitionPath;
	private WeaponAttachmentDefinition? _attachmentDefinition;
	private GripPoseSelection _selectedPose = GripPoseSelection.TwoHandIdle;
	private bool _playAnimationPreview = true;
	private Node3D? _gripPoseOffset;
	private AnimationPlayer? _animationPlayer;
	private string _previewAnimationName = string.Empty;
	private int _resourceLoadAttempts;

	[Export(PropertyHint.File, "*.tres")]
	public string AttachmentDefinitionPath
	{
		get => _attachmentDefinitionPath;
		set
		{
			if (_attachmentDefinitionPath == value)
			{
				return;
			}

			_attachmentDefinitionPath = value;
			QueuePreviewRefresh();
		}
	}

	[Export]
	public GripPoseSelection SelectedPose
	{
		get => _selectedPose;
		set
		{
			if (_selectedPose == value)
			{
				return;
			}

			_selectedPose = value;
			QueuePreviewRefresh();
		}
	}

	[Export]
	public bool PlayAnimationPreview
	{
		get => _playAnimationPreview;
		set
		{
			_playAnimationPreview = value;
			if (_animationPlayer is null ||
				string.IsNullOrEmpty(_previewAnimationName))
			{
				return;
			}

			if (_playAnimationPreview)
			{
				_animationPlayer.Play(_previewAnimationName);
			}
			else
			{
				_animationPlayer.Pause();
			}
		}
	}

	[ExportToolButton("Save Current Grip Pose", Icon = "Save")]
	public Callable SaveCurrentGripPoseButton =>
		Callable.From(SaveCurrentGripPose);

	[ExportToolButton("Reload Pose", Icon = "Reload")]
	public Callable ReloadPoseButton =>
		Callable.From(ReloadPose);

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			SetProcess(false);
			return;
		}

		CallDeferred(MethodName.RefreshPreview);
	}

	private void RefreshPreview()
	{
		if (!Engine.IsEditorHint() || !IsInsideTree())
		{
			return;
		}

		_gripPoseOffset = GetNodeOrNull<Node3D>(
			"PreviewCharacter/Warrior/Skeleton3D/RightHandWeaponAttachment/GripPoseOffset");
		_animationPlayer = GetNodeOrNull<AnimationPlayer>("GripAnimationPreview");
		if (_animationPlayer is null)
		{
			_animationPlayer = new AnimationPlayer
			{
				Name = "GripAnimationPreview",
				RootNode = new NodePath("../PreviewCharacter/Warrior"),
			};
			AddChild(_animationPlayer);
			_animationPlayer.AddAnimationLibrary("", new AnimationLibrary());
		}
		Resource? attachmentResource =
			string.IsNullOrWhiteSpace(_attachmentDefinitionPath)
				? null
				: ResourceLoader.Load(
				_attachmentDefinitionPath,
				cacheMode: ResourceLoader.CacheMode.Ignore);
		_attachmentDefinition =
			attachmentResource as WeaponAttachmentDefinition;

		if (_gripPoseOffset is null)
		{
			GD.PushError("Weapon grip authoring scene is missing GripPoseOffset.");
			return;
		}

		if (attachmentResource is not null && _attachmentDefinition is null)
		{
			if (_resourceLoadAttempts++ < 120)
			{
				CallDeferred(MethodName.RefreshPreview);
			}
			else
			{
				GD.PushError(
					$"Could not load {_attachmentDefinitionPath} as a " +
					$"{nameof(WeaponAttachmentDefinition)} after Mono initialized.");
			}
			return;
		}

		_resourceLoadAttempts = 0;
		ConfigureAnimationPreview();
		RebuildWeaponPreview();
		ReloadPose();
	}

	private void RebuildWeaponPreview()
	{
		if (_gripPoseOffset is null)
		{
			return;
		}

		foreach (Node child in _gripPoseOffset.GetChildren())
		{
			_gripPoseOffset.RemoveChild(child);
			child.QueueFree();
		}

		if (_attachmentDefinition?.WeaponScene is null)
		{
			GD.PushWarning(
				"Select a weapon attachment definition with a weapon scene.");
			return;
		}

		Node3D weapon = _attachmentDefinition.WeaponScene.Instantiate<Node3D>();
		_gripPoseOffset.AddChild(weapon);
	}

	private void ReloadPose()
	{
		if (!TryGetSelectedPose(out WeaponGripPose? gripPose) ||
			_gripPoseOffset is null ||
			_attachmentDefinition is null)
		{
			return;
		}

		_gripPoseOffset.Transform =
			_attachmentDefinition.CreateGripTransform(gripPose);
	}

	private void SaveCurrentGripPose()
	{
		if (!Engine.IsEditorHint() ||
			!TryGetSelectedPose(out WeaponGripPose? gripPose) ||
			gripPose is null ||
			_gripPoseOffset is null ||
			_attachmentDefinition is null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(_attachmentDefinition.ResourcePath))
		{
			GD.PushError("The attachment definition must be an existing saved resource.");
			return;
		}

		Transform3D poseTransform =
			_attachmentDefinition.DefaultGripTransform.AffineInverse() *
			_gripPoseOffset.Transform;
		Basis rotationBasis = poseTransform.Basis.Orthonormalized();
		Vector3 rotationRadians = rotationBasis.GetEuler();
		Vector3 oldPosition = gripPose.Position;
		Vector3 oldRotationDegrees = gripPose.RotationDegrees;
		Vector3 preservedScale = gripPose.Scale;
		Vector3 newPosition = poseTransform.Origin;
		Vector3 newRotationDegrees = new(
			Mathf.RadToDeg(rotationRadians.X),
			Mathf.RadToDeg(rotationRadians.Y),
			Mathf.RadToDeg(rotationRadians.Z));

		GD.Print(
			$"{_selectedPose} grip pose before save: " +
			$"Position={oldPosition}, RotationDegrees={oldRotationDegrees}, " +
			$"Scale={preservedScale}");
		GD.Print(
			$"{_selectedPose} grip pose after save: " +
			$"Position={newPosition}, RotationDegrees={newRotationDegrees}, " +
			$"Scale={preservedScale} (preserved)");

		gripPose.Position = newPosition;
		gripPose.RotationDegrees = newRotationDegrees;
		gripPose.EmitChanged();
		_attachmentDefinition.EmitChanged();

		Error result = ResourceSaver.Save(
			_attachmentDefinition,
			_attachmentDefinition.ResourcePath);
		if (result != Error.Ok)
		{
			GD.PushError(
				$"Failed to save grip pose to {_attachmentDefinition.ResourcePath}: {result}");
			return;
		}

		GD.Print(
			$"Saved {_selectedPose} grip pose to {_attachmentDefinition.ResourcePath}.");
		ReloadPose();
	}

	private bool TryGetSelectedPose(out WeaponGripPose? gripPose)
	{
		gripPose = null;
		if (_attachmentDefinition is null)
		{
			GD.PushWarning("Select a weapon attachment definition.");
			return false;
		}

		StringName poseName = new(_selectedPose.ToString());
		if (_attachmentDefinition.TryGetGripPose(poseName, out gripPose) &&
			gripPose is not null)
		{
			return true;
		}

		GD.PushError(
			$"{_attachmentDefinition.ResourcePath} does not define the {_selectedPose} pose.");
		return false;
	}

	private void ConfigureAnimationPreview()
	{
		if (_animationPlayer is null)
		{
			GD.PushError("Preview character is missing its AnimationPlayer.");
			return;
		}

		(string animationName, string animationPath) =
			GetSelectedPreviewAnimation();
		_previewAnimationName = animationName;
		if (_animationPlayer.HasAnimation(_previewAnimationName))
		{
			StartSelectedAnimationPreview();
			return;
		}

		PackedScene animationScene =
			ResourceLoader.Load<PackedScene>(animationPath);
		Node sourceRoot = animationScene.Instantiate();
		AnimationPlayer? sourcePlayer =
			FindDescendant<AnimationPlayer>(sourceRoot);
		if (sourcePlayer is null ||
			!sourcePlayer.HasAnimation(SourceAnimationName))
		{
			sourceRoot.Free();
			GD.PushError($"{animationPath} is missing {SourceAnimationName}.");
			return;
		}

		Animation animation =
			(Animation)sourcePlayer.GetAnimation(SourceAnimationName).Duplicate(true);
		animation.LoopMode = Animation.LoopModeEnum.Linear;
		RemoveHipsTranslation(animation);
		_animationPlayer
			.GetAnimationLibrary("")
			.AddAnimation(_previewAnimationName, animation);
		sourceRoot.Free();

		StartSelectedAnimationPreview();
	}

	private void StartSelectedAnimationPreview()
	{
		if (!string.IsNullOrEmpty(_previewAnimationName) &&
			_animationPlayer?.HasAnimation(_previewAnimationName) == true)
		{
			_animationPlayer.SpeedScale = 1.0f;
			_animationPlayer.Play(_previewAnimationName);
			if (!_playAnimationPreview)
			{
				_animationPlayer.Pause();
			}
		}
	}

	private (string Name, string Path) GetSelectedPreviewAnimation()
	{
		bool isOneHanded =
			_attachmentDefinition?.Handedness == WeaponHandedness.OneHanded;
		return _selectedPose switch
		{
			GripPoseSelection.OneHandIdle => (
				"WeaponGripAuthoringOneHandIdle",
				OneHandAnimationDirectory + "standing idle.fbx"),
			GripPoseSelection.Locomotion when isOneHanded => (
				"WeaponGripAuthoringOneHandWalk",
				OneHandAnimationDirectory + "standing walk forward.fbx"),
			GripPoseSelection.Locomotion => (
				"WeaponGripAuthoringTwoHandWalk",
				TwoHandLocomotionPath),
			GripPoseSelection.MeleeAttack when isOneHanded => (
				"WeaponGripAuthoringOneHandAttack",
				OneHandAnimationDirectory +
					"standing melee combo attack ver. 1.fbx"),
			GripPoseSelection.MeleeAttack => (
				"WeaponGripAuthoringTwoHandAttack",
				TwoHandMeleeAttackPath),
			_ => ("WeaponGripAuthoringTwoHandIdle", TwoHandIdlePath),
		};
	}

	private void QueuePreviewRefresh()
	{
		if (Engine.IsEditorHint() && IsInsideTree())
		{
			CallDeferred(MethodName.RefreshPreview);
		}
	}

	private static void RemoveHipsTranslation(Animation animation)
	{
		for (int track = animation.GetTrackCount() - 1; track >= 0; track--)
		{
			if (animation.TrackGetType(track) == Animation.TrackType.Position3D &&
				animation.TrackGetPath(track).ToString().EndsWith(":mixamorig_Hips"))
			{
				animation.RemoveTrack(track);
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
