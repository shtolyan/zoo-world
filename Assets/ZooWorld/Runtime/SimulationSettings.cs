using UnityEngine;

namespace ZooWorld
{
    [CreateAssetMenu(menuName = "Zoo World/Simulation Settings")]
    public sealed class SimulationSettings : ScriptableObject
    {
        public AnimalDefinition[] Species;
        public Vector2 HabitatSize = new Vector2(24, 13);
        [Min(1)] public float MinimumSpawnInterval = 1;
        [Min(1)] public float MaximumSpawnInterval = 2;
        public int RandomSeed = 2026;
        public bool RandomizeSeed = true;

        public void Validate()
        {
            if (Species == null || Species.Length == 0) throw new System.InvalidOperationException("Assign at least one animal species.");
            foreach (var species in Species)
                if (species == null || !species.IsValid)
                    throw new System.InvalidOperationException($"Invalid species '{(species != null ? species.name : "unassigned")}': check prefab visual/shadow references, movement settings, diet, population and finite positive mass/weight/turn speed.");
            if (!Finite(HabitatSize.x) || !Finite(HabitatSize.y) || !Finite(MinimumSpawnInterval) || !Finite(MaximumSpawnInterval)
                || HabitatSize.x < 4 || HabitatSize.y < 4 || MinimumSpawnInterval < 1 || MaximumSpawnInterval > 2 || MaximumSpawnInterval < MinimumSpawnInterval)
                throw new System.InvalidOperationException("Use a habitat of at least 4x4 and a spawn interval within [1, 2] seconds.");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void OnValidate()
        {
            MinimumSpawnInterval = Mathf.Clamp(MinimumSpawnInterval, 1, 2);
            MaximumSpawnInterval = Mathf.Clamp(MaximumSpawnInterval, MinimumSpawnInterval, 2);
        }
    }
}
