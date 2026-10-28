using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    [CreateAssetMenu(menuName = "Zoo World/Animal Species")]
    public sealed class AnimalDefinition : ScriptableObject
    {
        public string DisplayName = "Animal";
        public Diet Diet;
        public AnimalAgent Prefab;
        public MovementDefinition Movement;
        [Min(0.1f)] public float Mass = 1;
        [Min(1)] public float TurnSpeed = 360;
        [Min(0.01f)] public float SpawnWeight = 1;
        [Min(0)] public int InitialPopulation = 4;
        public Color Accent = Color.green;

        public bool IsValid => Prefab != null && Movement != null && Mass > 0 && SpawnWeight > 0;
    }
}
