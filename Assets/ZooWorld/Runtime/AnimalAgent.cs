using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    public interface IAnimalCollisionSink
    {
        void Collide(AnimalAgent first, AnimalAgent second, bool firstIsHead, bool secondIsHead);
    }

    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class AnimalAgent : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Transform shadow;
        [SerializeField] private TrailBody articulatedBody;
        [SerializeField] private Transform jaw;
        private Rigidbody body;
        private Rigidbody[] physicalBodies;
        private ILocomotion locomotion;
        private IRandomSource random;
        private IAnimalCollisionSink collisions;
        private HabitatBounds bounds;
        private Vector3 direction, desiredDirection;
        private IAnimalFeedback feedback;
        private bool wasJumping;
        private float turnIn, recoilFor, age;
        private float biteRemaining, consumedFor, hopHeight;
        private Vector3 consumedVisualScale;
        private bool beingConsumed;
        public const float BiteDuration = 0.32f, BiteCooldown = 0.48f, ConsumptionDuration = 0.24f;
        public AnimalLife Life { get; private set; }
        public AnimalDefinition Definition { get; private set; }
        public bool IsAlive => Life != null && Life.IsAlive && gameObject.activeSelf;
        public Rigidbody Body => body;
        public float VisualGrowth => visual.localScale.x;
        public bool CanBite => IsAlive && Life.Diet == Diet.Predator && biteRemaining <= 0;
        public bool IsBiting => biteRemaining > BiteCooldown - BiteDuration;
        public bool ConsumptionFinished => beingConsumed && consumedFor >= ConsumptionDuration;

        public void ConfigureVisuals(Transform model, Transform groundShadow) { visual = model; shadow = groundShadow; }
        public void ConfigureBody(TrailBody value) => articulatedBody = value;
        public void ConfigureJaw(Transform value) => jaw = value;

        public void Initialize(AnimalLife life, AnimalDefinition definition, ILocomotion movement,
            IRandomSource randomSource, IAnimalCollisionSink collisionSink, HabitatBounds habitat, Vector3 heading, IAnimalFeedback animalFeedback = null)
        {
            body = GetComponent<Rigidbody>();
            if (physicalBodies == null) physicalBodies = GetComponentsInChildren<Rigidbody>(true);
            Life = life; Definition = definition; locomotion = movement; random = randomSource; collisions = collisionSink; bounds = habitat;
            feedback = animalFeedback; wasJumping = false;
            direction = heading.sqrMagnitude > 0.001f ? heading.normalized : Vector3.forward;
            direction.y = 0;
            direction.Normalize(); desiredDirection = direction;
            turnIn = random.Range(1.2f, 3.2f); recoilFor = 0; age = 0;
            biteRemaining = 0; consumedFor = 0; beingConsumed = false; hopHeight = 0;
            if (jaw != null) jaw.localRotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;
            visual.rotation = Quaternion.LookRotation(direction);
            visual.localScale = Vector3.one * 0.05f;
            shadow.localScale = new Vector3(1.2f, 0.012f, 0.9f);
            name = $"{definition.DisplayName} #{life.Id}";
        }

        public void ActivateAt(Vector3 position)
        {
            // PhysX does not reliably update a disabled body's native pose. Inject dependencies
            // first, activate the whole hierarchy, then reset every body before the next physics tick.
            gameObject.SetActive(true);
            foreach (var part in physicalBodies) part.detectCollisions = true;
            body.mass = Definition.Mass;
            body.interpolation = RigidbodyInterpolation.None;
            body.position = position; body.rotation = Quaternion.identity;
            body.transform.SetPositionAndRotation(position, Quaternion.identity);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (articulatedBody != null) articulatedBody.Initialize(this, direction);
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;
            float dt = Time.fixedDeltaTime;
            var step = locomotion.Step(dt);
            turnIn -= dt;
            if (turnIn <= 0 && step.Height < 0.03f)
            {
                desiredDirection = Quaternion.Euler(0, random.Range(-115f, 115f), 0) * direction;
                turnIn = random.Range(1.2f, 3.2f);
            }
            var inward = bounds.Redirect(body.position, desiredDirection);
            if (inward != desiredDirection) { desiredDirection = inward; direction = inward; }
            else if (step.Height <= 0.001f)
                direction = Vector3.RotateTowards(direction, desiredDirection, Definition.TurnSpeed * Mathf.Deg2Rad * dt, 0);
            Vector3 travelDirection = articulatedBody != null ? articulatedBody.Advance(direction, dt) : direction;
            // Leave the solver's impulse intact briefly: prey really bounce, including between jumps.
            if (IsBiting) body.linearVelocity = Vector3.zero;
            else if (recoilFor <= 0) body.linearVelocity = travelDirection * (step.Distance / dt);
            else recoilFor -= dt;
            hopHeight = step.Height;
            visual.rotation = Quaternion.RotateTowards(visual.rotation, Quaternion.LookRotation(travelDirection), dt * 500);
            float shade = 1 - step.Height * 0.22f;
            shadow.localScale = new Vector3(1.2f * shade, 0.012f, 0.9f * shade);
            bool jumping = step.Height > 0.01f;
            if (jumping && !wasJumping) feedback?.Hop(body.position);
            wasJumping = jumping;
        }

        private void Update()
        {
            if (beingConsumed)
            {
                consumedFor += Time.deltaTime;
                float scale = 1 - Mathf.SmoothStep(0, 1, consumedFor / ConsumptionDuration);
                visual.localScale = consumedVisualScale * scale;
                shadow.localScale = new Vector3(1.2f, 0.012f, 0.9f) * scale;
                if (articulatedBody != null) articulatedBody.ShowConsumption(consumedVisualScale.x * scale);
                return;
            }
            if (!IsAlive) return;
            age += Time.deltaTime;
            biteRemaining = Mathf.Max(0, biteRemaining - Time.deltaTime);
            float t = Mathf.Clamp01(age / 0.24f);
            float growth = Mathf.Lerp(0.05f, 1, 1 - Mathf.Pow(1 - t, 3));
            float bitePhase = Mathf.Clamp01((BiteCooldown - biteRemaining) / BiteDuration);
            float snap = IsBiting ? Mathf.Sin(bitePhase * Mathf.PI) : 0;
            visual.localPosition = Vector3.up * hopHeight + visual.forward * (snap * 0.2f);
            visual.localScale = new Vector3(1 + snap * 0.12f, 1 - snap * 0.1f, 1 + snap * 0.18f) * growth;
            if (jaw != null) jaw.localRotation = Quaternion.Euler(snap * 34, 0, 0);
        }

        public void BeginBite() => biteRemaining = BiteCooldown;

        public void BeginConsumption()
        {
            beingConsumed = true; consumedFor = 0; consumedVisualScale = visual.localScale;
            foreach (var part in physicalBodies)
            {
                part.detectCollisions = false;
                part.linearVelocity = Vector3.zero; part.angularVelocity = Vector3.zero;
            }
        }

        private void OnCollisionEnter(Collision collision) => HeadContact(collision, true);
        private void OnCollisionStay(Collision collision) => HeadContact(collision, false);

        private void HeadContact(Collision collision, bool entering)
        {
            ReportContact(AnimalContactRelay.Resolve(collision.collider), true,
                collision.collider.TryGetComponent<AnimalAgent>(out _), entering);
        }

        public void ReportContact(AnimalAgent other, bool ownHead, bool otherHead, bool entering)
        {
            if (!IsAlive || other == null || other == this || !other.IsAlive) return;
            // Staying in contact can retry a cooled-down bite without holding the motor forever.
            if (entering) recoilFor = 0.18f;
            collisions.Collide(this, other, ownHead, otherHead);
        }
    }
}
