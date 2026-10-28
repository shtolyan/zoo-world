using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    [CreateAssetMenu(menuName = "Zoo World/Movement/Linear")]
    public sealed class LinearMovementDefinition : MovementDefinition
    {
        [Min(0.1f)] public float Speed = 1.9f;
        public override ILocomotion Create(IRandomSource random) => new LinearLocomotion(Speed);
    }
}
