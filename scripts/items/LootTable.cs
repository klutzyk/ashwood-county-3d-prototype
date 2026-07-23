#nullable enable

using Godot;

namespace AshwoodCounty3DPrototype.Items;

[GlobalClass]
public partial class LootTable : Resource
{
	[Export] public Godot.Collections.Array<LootEntry> Entries { get; set; } = new();
	[Export(PropertyHint.Range, "0,20,1")] public int MinimumRolls { get; set; } = 1;
	[Export(PropertyHint.Range, "0,20,1")] public int MaximumRolls { get; set; } = 1;

	public void GenerateInto(ItemStorage inventory, RandomNumberGenerator random)
	{
		int minimumRolls = Mathf.Max(MinimumRolls, 0);
		int maximumRolls = Mathf.Max(MaximumRolls, minimumRolls);
		int rollCount = random.RandiRange(minimumRolls, maximumRolls);
		for (int rollIndex = 0; rollIndex < rollCount; rollIndex++)
		{
			GenerateRoll(inventory, random);
		}
	}

	private void GenerateRoll(ItemStorage inventory, RandomNumberGenerator random)
	{
		float totalWeight = 0.0f;
		foreach (LootEntry entry in Entries)
		{
			totalWeight += Mathf.Max(entry.Weight, 0.0f);
		}

		if (totalWeight <= 0.0f)
		{
			return;
		}

		float roll = random.RandfRange(0.0f, totalWeight);
		foreach (LootEntry entry in Entries)
		{
			roll -= Mathf.Max(entry.Weight, 0.0f);
			if (roll > 0.0f)
			{
				continue;
			}

			if (entry.Item is not null)
			{
				int minimum = Mathf.Max(entry.MinimumQuantity, 1);
				int maximum = Mathf.Max(entry.MaximumQuantity, minimum);
				inventory.AddItem(entry.Item, random.RandiRange(minimum, maximum));
			}
			return;
		}
	}
}
