#nullable enable

using Godot;
using AshwoodCounty3DPrototype.Player;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class NeedRestoringItem : ItemDefinition
{
	[Export] public float HungerRestored { get; set; }
	[Export] public float ThirstRestored { get; set; }

	public override string UseFeedback
	{
		get
		{
			if (HungerRestored > 0.0f && ThirstRestored > 0.0f)
			{
				return $"Restored {Mathf.RoundToInt(ThirstRestored)} thirst and " +
					$"{Mathf.RoundToInt(HungerRestored)} hunger.";
			}
			if (HungerRestored > 0.0f)
			{
				return $"Restored {Mathf.RoundToInt(HungerRestored)} hunger.";
			}
			return $"Restored {Mathf.RoundToInt(ThirstRestored)} thirst.";
		}
	}

	public override string EffectDescription
	{
		get
		{
			if (HungerRestored > 0.0f && ThirstRestored > 0.0f)
			{
				return $"Restores {Mathf.RoundToInt(ThirstRestored)} thirst and " +
					$"{Mathf.RoundToInt(HungerRestored)} hunger.";
			}
			if (HungerRestored > 0.0f)
			{
				return $"Restores {Mathf.RoundToInt(HungerRestored)} hunger.";
			}
			return $"Restores {Mathf.RoundToInt(ThirstRestored)} thirst.";
		}
	}

	public override bool CanUse(Node user)
	{
		PlayerNeeds? needs = user.GetNodeOrNull<PlayerNeeds>("Needs");
		return needs is not null &&
			((HungerRestored > 0.0f && needs.CurrentHunger < needs.MaximumHunger) ||
			(ThirstRestored > 0.0f && needs.CurrentThirst < needs.MaximumThirst));
	}

	public override bool Use(Node user)
	{
		PlayerNeeds? needs = user.GetNodeOrNull<PlayerNeeds>("Needs");
		if (needs is null)
		{
			return false;
		}

		bool restored = false;
		if (HungerRestored > 0.0f)
		{
			restored = needs.RestoreHunger(HungerRestored);
		}
		if (ThirstRestored > 0.0f)
		{
			restored = needs.RestoreThirst(ThirstRestored) || restored;
		}
		return restored;
	}
}
