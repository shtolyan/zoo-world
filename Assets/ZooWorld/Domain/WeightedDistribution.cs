using System;
using System.Collections.Generic;

namespace ZooWorld.Domain
{
    /// <summary>Immutable cumulative weights: one validation pass, logarithmic selection, no float-sum overflow.</summary>
    public sealed class WeightedDistribution
    {
        private readonly double[] cumulative;

        public WeightedDistribution(IReadOnlyList<float> weights)
        {
            if (weights == null || weights.Count == 0) throw new ArgumentException("At least one weight is required.", nameof(weights));
            cumulative = new double[weights.Count];
            double total = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                float weight = weights[i];
                if (weight <= 0 || float.IsNaN(weight) || float.IsInfinity(weight))
                    throw new ArgumentOutOfRangeException(nameof(weights), "Weights must be finite and positive.");
                total += weight;
                cumulative[i] = total;
            }
        }

        public int Pick(IRandomSource random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            double target = random.Range(0f, 1f) * cumulative[cumulative.Length - 1];
            int low = 0;
            int high = cumulative.Length - 1;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (target <= cumulative[middle]) high = middle;
                else low = middle + 1;
            }
            return low;
        }
    }
}
