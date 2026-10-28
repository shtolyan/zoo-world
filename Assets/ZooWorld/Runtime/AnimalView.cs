using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    /// <summary>Visual animation only. Never changes a life, collider or physics velocity.</summary>
    public sealed class AnimalView
    {
        private const float ArrivalDuration = 0.24f;
        private readonly Transform visual;
        private readonly Transform shadow;
        private readonly Transform jaw;
        private readonly TrailBody articulatedBody;
        private readonly Vector3 visualScale;
        private readonly Vector3 shadowScale;
        private readonly Quaternion jawRestRotation;
        private Vector3 consumedScale;
        private float consumedGrowth;
        private float age;
        private float height;

        public float Growth { get; private set; }

        public AnimalView(Transform visual, Transform shadow, Transform jaw, TrailBody articulatedBody)
        {
            this.visual = visual;
            this.shadow = shadow;
            this.jaw = jaw;
            this.articulatedBody = articulatedBody;
            visualScale = visual.localScale;
            shadowScale = shadow.localScale;
            jawRestRotation = jaw != null ? jaw.localRotation : Quaternion.identity;
        }

        public void Reset(Vector3 heading)
        {
            age = height = 0;
            Growth = 0.05f;
            visual.localPosition = Vector3.zero;
            visual.rotation = Quaternion.LookRotation(heading);
            visual.localScale = visualScale * Growth;
            shadow.localScale = shadowScale;
            if (jaw != null) jaw.localRotation = jawRestRotation;
        }

        public void PresentMotion(Vector3 direction, float elevation, float deltaTime)
        {
            height = elevation;
            visual.rotation = Quaternion.RotateTowards(visual.rotation, Quaternion.LookRotation(direction), deltaTime * 500);
            float shade = Mathf.Clamp01(1 - height * 0.22f);
            shadow.localScale = Vector3.Scale(shadowScale, new Vector3(shade, 1, shade));
        }

        public void CaptureConsumption()
        {
            consumedScale = visual.localScale;
            consumedGrowth = Growth;
        }

        public void Tick(float deltaTime, AnimalActivity activity)
        {
            if (activity.IsConsuming)
            {
                float scale = 1 - Mathf.SmoothStep(0, 1, activity.ConsumptionProgress);
                Growth = consumedGrowth * scale;
                visual.localScale = consumedScale * scale;
                shadow.localScale = shadowScale * scale;
                if (articulatedBody != null) articulatedBody.ShowConsumption(Growth);
                return;
            }

            age += deltaTime;
            float t = Mathf.Clamp01(age / ArrivalDuration);
            Growth = Mathf.Lerp(0.05f, 1, 1 - Mathf.Pow(1 - t, 3));
            float snap = activity.IsBiting ? Mathf.Sin(activity.BiteProgress * Mathf.PI) : 0;
            Vector3 lunge = visual.parent.InverseTransformDirection(visual.forward) * (snap * 0.2f);
            visual.localPosition = Vector3.up * height + lunge;
            visual.localScale = Vector3.Scale(visualScale, new Vector3(1 + snap * 0.12f, 1 - snap * 0.1f, 1 + snap * 0.18f)) * Growth;
            if (jaw != null) jaw.localRotation = jawRestRotation * Quaternion.Euler(snap * 34, 0, 0);
        }
    }
}
