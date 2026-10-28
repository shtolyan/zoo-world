using System;

namespace ZooWorld.Domain
{
    public interface IRandomSource
    {
        float Range(float minimum, float maximum);
        int Range(int minimum, int exclusiveMaximum);
    }

    public sealed class SeededRandom : IRandomSource
    {
        private readonly Random random;
        public SeededRandom(int seed) => random = new Random(seed);
        public float Range(float minimum, float maximum) => minimum + (float)random.NextDouble() * (maximum - minimum);
        public int Range(int minimum, int exclusiveMaximum) => random.Next(minimum, exclusiveMaximum);
    }

    public sealed class SpawnClock
    {
        private readonly IRandomSource random;
        private readonly float minimum, maximum;
        private double remaining;
        public float Remaining => (float)remaining;

        public SpawnClock(IRandomSource random, float minimum = 1, float maximum = 2)
        {
            if (minimum <= 0 || maximum < minimum || float.IsNaN(minimum) || float.IsNaN(maximum) || float.IsInfinity(maximum))
                throw new ArgumentOutOfRangeException(nameof(minimum));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.minimum = minimum; this.maximum = maximum;
            Reset();
        }

        public void Reset() => remaining = random.Range(minimum, maximum);

        public int Advance(float deltaTime)
        {
            if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            remaining -= deltaTime;
            int due = 0;
            while (remaining <= 0) { due++; remaining += random.Range(minimum, maximum); }
            return due;
        }
    }
}
