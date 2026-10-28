using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    [CreateAssetMenu(menuName = "Zoo World/Movement/Hop")]
    public sealed class HopMovementDefinition : MovementDefinition
    {
        [Min(0.1f)] public float Interval = 1.45f;
        [Min(0.05f)] public float Duration = 0.42f;
        [Min(0.1f)] public float Distance = 1.65f;
        [Min(0)] public float Height = 0.65f;
        public override bool IsValid => Finite(Interval) && Finite(Duration) && Finite(Distance) && Finite(Height)
            && Interval > 0 && Duration > 0 && Duration <= Interval && Distance > 0 && Height >= 0;
        // Randomize the initial REST only; spawning halfway through a jump would shorten the first jump.
        public override ILocomotion Create(IRandomSource random) => new HopLocomotion(Interval, Duration, Distance, Height, random.Range(Duration, Interval));
        private void OnValidate() => Duration = Mathf.Clamp(Duration, 0.05f, Interval);
    }
}
