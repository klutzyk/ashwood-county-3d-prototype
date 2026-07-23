#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Weapons;

[GlobalClass]
public partial class WeaponGripPose : Resource
{
	[Export] public StringName PoseName { get; set; } = new("Default");
	[Export] public Vector3 Position { get; set; }
	[Export] public Vector3 RotationDegrees { get; set; }
	[Export] public Vector3 Scale { get; set; } = Vector3.One;
	[Export(PropertyHint.Range, "0,0.5,0.01")] public float BlendDuration { get; set; } = 0.1f;

	public Transform3D CreateTransform()
	{
		Basis basis = Basis.FromEuler(new Vector3(
			Mathf.DegToRad(RotationDegrees.X),
			Mathf.DegToRad(RotationDegrees.Y),
			Mathf.DegToRad(RotationDegrees.Z)));
		return new Transform3D(basis.Scaled(Scale), Position);
	}
}
