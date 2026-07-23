#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Weapons;

public enum WeaponHandedness
{
	OneHanded,
	TwoHanded,
}

[GlobalClass]
public partial class WeaponAttachmentDefinition : Resource
{
	[Export] public PackedScene? WeaponScene { get; set; }
	[Export] public WeaponHandedness Handedness { get; set; } = WeaponHandedness.TwoHanded;
	[Export] public Transform3D DefaultGripTransform { get; set; } = Transform3D.Identity;
	[Export] public Godot.Collections.Array<WeaponGripPose> GripPoses { get; set; } = new();

	public bool TryGetGripPose(StringName poseName, out WeaponGripPose? gripPose)
	{
		foreach (WeaponGripPose candidate in GripPoses)
		{
			if (candidate is not null && candidate.PoseName == poseName)
			{
				gripPose = candidate;
				return true;
			}
		}

		gripPose = null;
		return false;
	}
}
