using System;

namespace ZooWorld.Domain
{
    /// <summary>Per-life action timing. All durations use simulation time, so pause is a zero tick.</summary>
    public sealed class AnimalActivity
    {
        public const float BiteDuration = 0.32f;
        public const float BiteCooldown = 0.48f;
        public const float ConsumptionDuration = 0.24f;

        private float biteRemaining;
        private float consumedFor;

        public bool IsConsuming { get; private set; }
        public bool CanBite => !IsConsuming && biteRemaining <= 0;
        public bool IsBiting => !IsConsuming && biteRemaining > BiteCooldown - BiteDuration;
        public float BiteProgress => Math.Min(1, (BiteCooldown - biteRemaining) / BiteDuration);
        public float ConsumptionProgress => Math.Min(1, consumedFor / ConsumptionDuration);
        public bool ConsumptionFinished => IsConsuming && consumedFor >= ConsumptionDuration;

        public void Reset()
        {
            biteRemaining = 0;
            consumedFor = 0;
            IsConsuming = false;
        }

        public void Tick(float deltaTime)
        {
            SimulationTime.ValidateDelta(deltaTime);

            if (IsConsuming)
                consumedFor = Math.Min(ConsumptionDuration, consumedFor + deltaTime);
            else
                biteRemaining = Math.Max(0, biteRemaining - deltaTime);
        }

        public bool TryBeginBite()
        {
            if (!CanBite) return false;
            biteRemaining = BiteCooldown;
            return true;
        }

        public void BeginConsumption()
        {
            if (IsConsuming) return;
            IsConsuming = true;
            consumedFor = 0;
            biteRemaining = 0;
        }
    }
}
