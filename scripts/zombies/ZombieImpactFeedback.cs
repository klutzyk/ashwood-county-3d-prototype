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
	public float ImpactLightDuration { get; set; } = 0.04f;

	[Export(PropertyHint.Range, "0,6,0.05")]
	public float ImpactLightEnergy { get; set; } = 0.14f;

	[Export(PropertyHint.Range, "0.02,0.3,0.01")]
	public float ContactFlashDuration { get; set; } = 0.035f;

	private GpuParticles3D _bloodSpray = null!;
	private GpuParticles3D _bloodMist = null!;
	private GpuParticles3D _debrisSpray = null!;
	private OmniLight3D _impactLight = null!;
	private Node3D _contactFlash = null!;
	private float _lightRemaining;
	private float _contactFlashRemaining;

	public bool IsContactFlashActive => _contactFlashRemaining > 0.0f;
	public int PresentationCount { get; private set; }
	public Vector3 LastContactWorldPosition { get; private set; }
	public Vector3 LastImpactDirection { get; private set; }

	public override void _Ready()
	{
		_bloodSpray = GetNode<GpuParticles3D>("BloodSpray");
		_bloodMist = GetNode<GpuParticles3D>("BloodMist");
		_debrisSpray = GetNode<GpuParticles3D>("DebrisSpray");
		_impactLight = GetNode<OmniLight3D>("ImpactLight");
		_contactFlash = GetNode<Node3D>("ContactFlash");
		_impactLight.LightEnergy = 0.0f;
		_contactFlash.Visible = false;
		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		_lightRemaining = Mathf.Max(_lightRemaining - (float)delta, 0.0f);
		_contactFlashRemaining = Mathf.Max(
			_contactFlashRemaining - (float)delta,
			0.0f);
		float duration = Mathf.Max(ImpactLightDuration, 0.001f);
		_impactLight.LightEnergy = ImpactLightEnergy *
			Mathf.SmoothStep(0.0f, 1.0f, _lightRemaining / duration);

		float flashDuration = Mathf.Max(ContactFlashDuration, 0.001f);
		float flashFade = Mathf.Clamp(
			_contactFlashRemaining / flashDuration,
			0.0f,
			1.0f);
		_contactFlash.Visible = flashFade > 0.0f;
		if (_contactFlash.Visible)
		{
			float scale = Mathf.Lerp(0.28f, 1.0f, Mathf.Sqrt(flashFade));
			_contactFlash.Scale = new Vector3(
				scale * 0.42f,
				scale * 0.72f,
				scale * 0.22f);
		}

		if (_lightRemaining <= 0.0f && _contactFlashRemaining <= 0.0f)
		{
			_impactLight.LightEnergy = 0.0f;
			_contactFlash.Visible = false;
			SetProcess(false);
		}
	}

	public void PlayHit(
		Vector3 worldDirection,
		bool lethal,
		Vector3 contactWorldPosition)
	{
		Vector3 direction = worldDirection.Normalized();
		if (direction.LengthSquared() > 0.001f)
		{
			GlobalPosition = contactWorldPosition;
			LookAt(GlobalPosition + direction, Vector3.Up);
		}
		else
		{
			GlobalPosition = contactWorldPosition;
			direction = -GlobalBasis.Z;
		}

		_bloodSpray.Amount = lethal ? 27 : 17;
		_bloodSpray.Restart();
		_bloodSpray.Emitting = true;
		_bloodMist.Amount = lethal ? 10 : 6;
		_bloodMist.Restart();
		_bloodMist.Emitting = true;
		_debrisSpray.Amount = lethal ? 8 : 5;
		_debrisSpray.Restart();
		_debrisSpray.Emitting = true;
		_impactLight.LightColor = lethal
			? new Color(0.45f, 0.035f, 0.018f)
			: new Color(0.52f, 0.055f, 0.025f);
		_lightRemaining = Mathf.Max(ImpactLightDuration, 0.0f);
		_contactFlashRemaining = Mathf.Max(ContactFlashDuration, 0.0f);
		_impactLight.LightEnergy = ImpactLightEnergy;
		_contactFlash.Visible = _contactFlashRemaining > 0.0f;
		_contactFlash.Scale = new Vector3(0.42f, 0.72f, 0.22f);
		PresentationCount++;
		LastContactWorldPosition = contactWorldPosition;
		LastImpactDirection = direction;
		SetProcess(_lightRemaining > 0.0f || _contactFlashRemaining > 0.0f);
	}
}
