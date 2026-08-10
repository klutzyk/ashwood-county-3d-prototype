#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using AshwoodCounty3DPrototype.World.County;

namespace AshwoodCounty3DPrototype.Tests;

/// <summary>Samples the complete landmass and verifies ecological coverage and variety.</summary>
public partial class CountyNatureCoverageValidation : Node
{
    public override void _Ready()
    {
        try
        {
            var habitatCounts = new Dictionary<CountyMap.Habitat, int>();
            var regionalHabitats = new Dictionary<CountyMap.RegionId, HashSet<CountyMap.Habitat>>();
            var regionalNaturalSamples = new Dictionary<CountyMap.RegionId, int>();
            foreach (CountyMap.Region region in CountyMap.Regions)
            {
                regionalHabitats[region.Id] = new HashSet<CountyMap.Habitat>();
                regionalNaturalSamples[region.Id] = 0;
            }

            int playable = 0;
            int natural = 0;
            const float step = 96.0f;
            for (float z = CountyMap.NorthZ; z <= CountyMap.SouthZ; z += step)
            {
                for (float x = CountyMap.WestX; x <= CountyMap.EastX; x += step)
                {
                    if (!CountyMap.IsPlayable(x, z)) continue;
                    playable++;

                    CountyMap.Habitat habitat = CountyMap.HabitatAt(x, z);
                    habitatCounts[habitat] = habitatCounts.GetValueOrDefault(habitat) + 1;
                    CountyMap.RegionId region = CountyMap.RegionAt(x, z).Id;
                    regionalHabitats[region].Add(habitat);
                    if (habitat != CountyMap.Habitat.Settled)
                    {
                        natural++;
                        regionalNaturalSamples[region]++;
                    }
                }
            }

            Require(playable > 3500, "County coverage sample is unexpectedly small.");
            Require(natural >= playable * 0.88f,
                $"Only {natural}/{playable} county samples receive natural habitat.");
            Require(habitatCounts.Count >= 9,
                $"Only {habitatCounts.Count} habitat types appear across the county.");

            foreach (CountyMap.Region region in CountyMap.Regions)
            {
                int variety = regionalHabitats[region.Id].Count;
                Require(variety >= 4, $"{region.Name} has only {variety} habitat types.");
                Require(regionalNaturalSamples[region.Id] > 35,
                    $"{region.Name} has too little natural coverage.");
                GD.Print($"COUNTY_NATURE_REGION: {region.Name} " +
                         $"habitats={variety} natural_samples={regionalNaturalSamples[region.Id]}");
            }

            foreach ((CountyMap.Habitat habitat, int count) in habitatCounts)
            {
                GD.Print($"COUNTY_NATURE_HABITAT: {habitat} samples={count}");
            }

            GD.Print($"COUNTY_NATURE_COVERAGE: PASS playable={playable} natural={natural} " +
                     $"coverage={natural / (float)playable:P1}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("COUNTY_NATURE_COVERAGE: FAIL - " + exception.Message);
            GetTree().Quit(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
