#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Zombies;

/// <summary>
/// Owns short-lived, local zombie impact presentation. The particles use
/// project-owned primitive geometry so combat feedback remains available in
/// the Compatibility renderer without modifying the imported character.
/// </summary>
public partial class ZombieImpactFeedback : Node3D
{
	[Export(PropertyHint.Range, "0.02,0.4,0.01")]
	public float ImpactLightDuration { get; set; } = 0.11f;

	[Export(PropertyHint.Range, "0,4,0.05")]
	public float ImpactLightEnergy { get; set; } = 1.15f;

	private GpuParticles3D _bloodSpray = null!;
	private OmniLight3D _impactLight = null!;
	private float _lightRemaining;

	public override void _Ready()
	{
		_bloodSpray = GetNode<GpuParticles3D>("BloodSpray");
		_impactLight = GetNode<OmniLight3D>("ImpactLight");
		_impactLight.LightEnergy = 0.0f;
		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		_lightRemaining = Mathf.Max(_lightRemaining - (float)delta, 0.0f);
		float duration = Mathf.Max(ImpactLightDuration, 0.001f);
		_impactLight.LightEnergy = ImpactLightEnergy *
			Mathf.SmoothStep(0.0f, 1.0f, _lightRemaining / duration);
		if (_lightRemaining <= 0.0f)
		{
			_impactLight.LightEnergy = 0.0f;
			SetProcess(false);
		}
	}

	public void PlayHit(Vector3 worldDirection, bool lethal)
	{
		Vector3 direction = worldDirection.Normalized();
		if (direction.LengthSquared() > 0.001f)
		{
			LookAt(GlobalPosition + direction, Vector3.Up);
		}

		_bloodSpray.Amount = lethal ? 34 : 22;
		_bloodSpray.Restart();
		_bloodSpray.Emitting = true;
		_impactLight.LightColor = lethal
			? new Color(0.72f, 0.045f, 0.018f)
			: new Color(0.9f, 0.12f, 0.035f);
		_lightRemaining = Mathf.Max(ImpactLightDuration, 0.0f);
		_impactLight.LightEnergy = ImpactLightEnergy;
		SetProcess(_lightRemaining > 0.0f);
	}
}
