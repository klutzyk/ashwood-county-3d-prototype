#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Player;

public partial class PlayerFlashlight : SpotLight3D
{
	[Export(PropertyHint.Range, "1,40,0.5")]
	public float BeamRange { get; set; } = 20.0f;

	[Export(PropertyHint.Range, "0,10,0.1")]
	public float BeamEnergy { get; set; } = 2.7f;

	[Export(PropertyHint.Range, "5,80,1")]
	public float BeamConeAngle { get; set; } = 28.0f;

	[Export(PropertyHint.Range, "0.05,1,0.05")]
	public float ToggleFadeDuration { get; set; } = 0.18f;

	[Export(PropertyHint.Range, "0.1,4,0.05")]
	public float BeamAttenuation { get; set; } = 1.0f;

	[Export(PropertyHint.Range, "0.1,4,0.05")]
	public float ConeAttenuation { get; set; } = 0.72f;

	[Export]
	public Vector3 BeamOriginOffset { get; set; } = new(0.16f, -0.12f, -0.55f);

	private bool _targetEnabled;

	public bool IsEnabled => _targetEnabled;

	public override void _Ready()
	{
		SpotRange = BeamRange;
		SpotAngle = BeamConeAngle;
		SpotAttenuation = BeamAttenuation;
		SpotAngleAttenuation = ConeAttenuation;
		Position = BeamOriginOffset;
		ShadowEnabled = false;
		LightEnergy = 0.0f;
		Visible = false;
	}

	public override void _Process(double delta)
	{
		float targetEnergy = _targetEnabled ? BeamEnergy : 0.0f;
		float fadeDuration = Mathf.Max(ToggleFadeDuration, 0.001f);
		float fadeSpeed = Mathf.Max(BeamEnergy, 0.0f) / fadeDuration;
		LightEnergy = Mathf.MoveToward(LightEnergy, targetEnergy, fadeSpeed * (float)delta);
		if (!_targetEnabled && LightEnergy <= 0.001f)
		{
			LightEnergy = 0.0f;
			Visible = false;
		}
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
		_targetEnabled = !_targetEnabled;
		if (_targetEnabled)
		{
			Visible = true;
		}
	}
}
