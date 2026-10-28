using System;
using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    /// <summary>Validated catalog and spawn placement. Existing sessions keep a stable copy of the catalog.</summary>
    public sealed class AnimalSpawner
    {
        private readonly AnimalDefinition[] species;
        private readonly WeightedDistribution distribution;
        private readonly AnimalPopulation population;
        private readonly HabitatBounds bounds;
        private readonly IRandomSource random;

        public AnimalSpawner(AnimalDefinition[] catalog, AnimalPopulation population, HabitatBounds bounds, IRandomSource random)
        {
            if (catalog == null || catalog.Length == 0) throw new ArgumentException("A species catalog is required.", nameof(catalog));
            species = (AnimalDefinition[])catalog.Clone();
            this.population = population;
            this.bounds = bounds;
            this.random = random;
            var weights = new float[species.Length];
            for (int i = 0; i < species.Length; i++)
            {
                if (species[i] == null || !species[i].IsValid) throw new ArgumentException($"Invalid species at index {i}.", nameof(catalog));
                weights[i] = species[i].SpawnWeight;
            }
            distribution = new WeightedDistribution(weights);
        }

        public void Seed()
        {
            foreach (var definition in species)
                for (int i = 0; i < definition.InitialPopulation; i++) Spawn(definition);
        }

        public AnimalAgent SpawnRandom() => Spawn(species[distribution.Pick(random)]);

        public AnimalAgent SpawnSpecies(int index) => index >= 0 && index < species.Length ? Spawn(species[index]) : null;

        private AnimalAgent Spawn(AnimalDefinition definition)
        {
            Vector3 heading = Quaternion.Euler(0, random.Range(0, 360f), 0) * Vector3.forward;
            Vector3 position = bounds.RandomPoint(random, heading * definition.Prefab.RestLength);
            return population.Spawn(definition, position, heading);
        }
    }
}
