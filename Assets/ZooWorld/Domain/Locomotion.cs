using System;

namespace ZooWorld.Domain
{
    public readonly struct MotionStep
    {
        public float Distance { get; }
        public float Height { get; }
        public MotionStep(float distance, float height) { Distance = distance; Height = height; }
    }

    public interface ILocomotion { MotionStep Step(float deltaTime); }

    public sealed class LinearLocomotion : ILocomotion
    {
        private readonly float speed;
        public LinearLocomotion(float speed)
        {
            if (speed <= 0 || float.IsNaN(speed) || float.IsInfinity(speed)) throw new ArgumentOutOfRangeException(nameof(speed));
            this.speed = speed;
        }
        public MotionStep Step(float deltaTime) => new MotionStep(speed * Math.Max(0, deltaTime), 0);
    }

    /// <summary>Analytical travel keeps each jump's distance fixed even when a tick crosses its end.</summary>
    public sealed class HopLocomotion : ILocomotion
    {
        private readonly double interval, duration;
        private readonly float distance, height;
        private double time;

        public HopLocomotion(float interval, float duration, float distance, float height, float initialPhase = 0)
        {
            if (!Finite(interval) || !Finite(duration) || !Finite(distance) || !Finite(height) || !Finite(initialPhase) ||
                interval <= 0 || duration <= 0 || duration > interval || distance <= 0 || height < 0 || initialPhase < 0)
                throw new ArgumentOutOfRangeException(nameof(interval), "A hop needs positive distance, 0 < duration <= interval and non-negative height/phase.");
            this.interval = interval; this.duration = duration; this.distance = distance; this.height = height;
            time = initialPhase % interval;
        }

        public MotionStep Step(float deltaTime)
        {
            double previous = time;
            time += Math.Max(0, deltaTime);
            double phase = time % interval;
            float elevation = phase < duration ? height * (float)Math.Sin(Math.PI * phase / duration) : 0;
            return new MotionStep((float)(TravelAt(time) - TravelAt(previous)), elevation);
        }

        private double TravelAt(double t) => (Math.Floor(t / interval) + Math.Min(1, (t % interval) / duration)) * distance;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
