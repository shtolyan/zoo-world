using UnityEngine;

namespace ZooWorld
{
    /// <summary>
    /// Physical articulated body following the head's arc-length trail. Joints limit stretch;
    /// soft velocity drives allow contacts to deflect the body instead of teleporting it.
    /// Suitable for snakes, worms or another long-bodied species.
    /// </summary>
    public sealed class TrailBody : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] segments;
        [SerializeField] private Transform[] models;
        [SerializeField] private Vector3[] modelScales;
        [Min(0.1f)] public float Spacing = 0.28f;
        [Range(0, 45)] public float WeaveAngle = 28;
        [Min(0.5f)] public float Wavelength = 3.8f;
        [Min(1)] public float FollowGain = 12;
        private readonly DistanceTrail trail = new DistanceTrail();
        private Vector3[] previousTargets;
        private Collider[] colliders;
        private AnimalAgent owner;
        private Rigidbody head;
        private Vector3 previousHead;
        private float travel;
        private bool selfContactsIgnored;
        private const float HeadHeight = 0.3f;
        public int SegmentCount => segments.Length;
        public float RestLength => segments.Length * Spacing;
        public Rigidbody GetSegment(int index) => segments[index];
        public Vector3 PointOnTrail(float behind) => trail.Sample(behind);

        public void Configure(Rigidbody[] bodies, Transform[] meshes)
        {
            segments = bodies; models = meshes; modelScales = new Vector3[meshes.Length];
            for (int i = 0; i < meshes.Length; i++) modelScales[i] = meshes[i].localScale;
        }

        public void Initialize(AnimalAgent animal, Vector3 heading)
        {
            owner = animal; head = animal.Body;
            previousHead = head.position + Vector3.up * HeadHeight;
            travel = 0; selfContactsIgnored = false;
            if (previousTargets == null) previousTargets = new Vector3[segments.Length];
            if (colliders == null)
            {
                colliders = new Collider[segments.Length + 1]; colliders[0] = animal.GetComponent<Collider>();
                for (int i = 0; i < segments.Length; i++) colliders[i + 1] = segments[i].GetComponent<Collider>();
            }
            trail.Reset(previousHead, heading, (segments.Length + 2) * Spacing);
            for (int i = 0; i < segments.Length; i++)
            {
                Vector3 position = trail.Sample((i + 1) * Spacing);
                // Reset native and rendered poses together and clear the interpolation cache;
                // a previous life's curved shape must not leak into the newly activated body.
                segments[i].interpolation = RigidbodyInterpolation.None;
                segments[i].transform.SetPositionAndRotation(position, Quaternion.identity);
                segments[i].position = position; segments[i].rotation = Quaternion.identity;
                segments[i].linearVelocity = Vector3.zero; segments[i].angularVelocity = Vector3.zero;
                segments[i].interpolation = RigidbodyInterpolation.Interpolate;
                previousTargets[i] = position;
                models[i].localScale = modelScales[i]; models[i].rotation = Quaternion.LookRotation(heading);
            }
        }

        public Vector3 Advance(Vector3 forward, float dt)
        {
            if (!selfContactsIgnored)
            {
                // Reapply after activation: Unity does not preserve IgnoreCollision across pooling.
                for (int i = 0; i < colliders.Length; i++)
                    for (int j = i + 1; j < colliders.Length; j++) Physics.IgnoreCollision(colliders[i], colliders[j]);
                selfContactsIgnored = true;
            }
            Vector3 point = head.position + Vector3.up * HeadHeight;
            travel += Vector3.Distance(point, previousHead); previousHead = point;
            trail.Record(point);
            for (int i = 0; i < segments.Length; i++)
            {
                Vector3 target = trail.Sample((i + 1) * Spacing);
                Vector3 feedForward = (target - previousTargets[i]) / dt;
                previousTargets[i] = target;
                Vector3 desired = Vector3.ClampMagnitude(feedForward + (target - segments[i].position) * FollowGain, 7);
                Vector3 acceleration = Vector3.ClampMagnitude((desired - segments[i].linearVelocity) * 10, 45);
                acceleration.y = 0;
                segments[i].AddForce(acceleration, ForceMode.Acceleration);
            }
            float angle = Mathf.Sin(travel * (Mathf.PI * 2 / Wavelength)) * WeaveAngle;
            return Quaternion.AngleAxis(angle, Vector3.up) * forward;
        }

        private void LateUpdate()
        {
            if (owner == null || !owner.IsAlive) return;
            Vector3 preceding = owner.transform.position + Vector3.up * HeadHeight;
            for (int i = 0; i < segments.Length; i++)
            {
                Vector3 towardHead = preceding - segments[i].transform.position; towardHead.y = 0;
                if (towardHead.sqrMagnitude > 0.0001f) models[i].rotation = Quaternion.LookRotation(towardHead);
                models[i].localScale = modelScales[i] * owner.VisualGrowth;
                preceding = segments[i].transform.position;
            }
        }

        public void ShowConsumption(float scale)
        {
            for (int i = 0; i < models.Length; i++) models[i].localScale = modelScales[i] * scale;
        }
    }
}
