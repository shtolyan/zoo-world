using System;

namespace ZooWorld.Domain
{
    internal static class SimulationTime
    {
        public static void ValidateDelta(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Simulation time must be finite and non-negative.");
        }
    }
}
