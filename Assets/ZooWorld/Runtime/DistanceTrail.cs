using System;
using UnityEngine;

namespace ZooWorld
{
    /// <summary>Bounded head history sampled by travelled distance, not by frame count.</summary>
    public sealed class DistanceTrail
    {
        private const float MinimumSampleDistance = 0.025f;
        private readonly Vector3[] points;
        private readonly double[] distances;
        private int start, count;
        public int Count => count;
        public int Capacity => points.Length;

        public DistanceTrail(int capacity = 512)
        {
            if (capacity < 4) throw new ArgumentOutOfRangeException(nameof(capacity));
            points = new Vector3[capacity]; distances = new double[capacity];
        }

        public void Reset(Vector3 head, Vector3 forward, float length)
        {
            start = count = 0;
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            int seeds = Mathf.Min(Capacity / 2, Mathf.Max(2, Mathf.CeilToInt(length / 0.05f)));
            for (int i = 0; i <= seeds; i++)
            {
                float behind = length * (1 - (float)i / seeds);
                Append(head - forward * behind, -behind);
            }
        }

        public void Record(Vector3 point)
        {
            if (count == 0) { Append(point, 0); return; }
            int latest = Index(count - 1);
            float moved = Vector3.Distance(point, points[latest]);
            // Coalesce tiny solver jitter; standing still must never consume the tail's history.
            if (moved < MinimumSampleDistance) return;
            Append(point, distances[latest] + moved);
        }

        public Vector3 Sample(float distanceBehindHead)
        {
            if (count == 0) throw new InvalidOperationException("Reset the trail before sampling.");
            double target = distances[Index(count - 1)] - Mathf.Max(0, distanceBehindHead);
            if (target <= distances[start]) return points[start];
            int low = 0, high = count - 1;
            while (high - low > 1)
            {
                int mid = (low + high) / 2;
                if (distances[Index(mid)] < target) low = mid; else high = mid;
            }
            int first = Index(low), second = Index(high);
            double span = distances[second] - distances[first];
            float blend = span > 0.000001 ? (float)((target - distances[first]) / span) : 0;
            return Vector3.LerpUnclamped(points[first], points[second], blend);
        }

        private int Index(int offset) => (start + offset) % Capacity;
        private void Append(Vector3 point, double distance)
        {
            int slot;
            if (count < Capacity) slot = Index(count++);
            else { slot = start; start = (start + 1) % Capacity; }
            points[slot] = point; distances[slot] = distance;
        }
    }
}
