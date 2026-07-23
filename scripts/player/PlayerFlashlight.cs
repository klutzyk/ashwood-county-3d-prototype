#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerFlashlight : SpotLight3D
{
	[Export(PropertyHint.Range, "1,40,0.5")]
	public float BeamRange { get; set; } = 18.0f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	public float BeamEnergy { get; set; } = 3.2f;

	[Export(PropertyHint.Range, "5,80,1")]
	public float BeamConeAngle { get; set; } = 30.0f;

	public bool IsEnabled => Visible;

	public override void _Ready()
	{
		SpotRange = BeamRange;
		LightEnergy = BeamEnergy;
		SpotAngle = BeamConeAngle;
		ShadowEnabled = false;
		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Echo: true })
		{
			return;
		}

		if (@event.IsActionPressed("toggle_flashlight"))
		{
			Toggle();
			GetViewport().SetInputAsHandled();
		}
	}

	public void Toggle()
	{
		Visible = !Visible;
	}
}
