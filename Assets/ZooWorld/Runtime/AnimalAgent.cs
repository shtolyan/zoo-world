using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    public interface IAnimalCollisionSink { void Collide(AnimalAgent first, AnimalAgent second); }

    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class AnimalAgent : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Transform shadow;
        [SerializeField] private TrailBody articulatedBody;
        private Rigidbody body;
        private ILocomotion locomotion;
        private IRandomSource random;
        private IAnimalCollisionSink collisions;
        private HabitatBounds bounds;
        private Vector3 direction, desiredDirection;
        private IAnimalFeedback feedback;
        private bool wasJumping;
        private float turnIn, recoilFor, age;
        public AnimalLife Life { get; private set; }
        public AnimalDefinition Definition { get; private set; }
        public bool IsAlive => Life != null && Life.IsAlive && gameObject.activeSelf;
        public Rigidbody Body => body;
        public float VisualGrowth => visual.localScale.x;

        public void ConfigureVisuals(Transform model, Transform groundShadow) { visual = model; shadow = groundShadow; }
        public void ConfigureBody(TrailBody value) => articulatedBody = value;

        public void Initialize(AnimalLife life, AnimalDefinition definition, ILocomotion movement,
            IRandomSource randomSource, IAnimalCollisionSink collisionSink, HabitatBounds habitat, Vector3 heading, IAnimalFeedback animalFeedback = null)
        {
            body = GetComponent<Rigidbody>();
            Life = life; Definition = definition; locomotion = movement; random = randomSource; collisions = collisionSink; bounds = habitat;
            feedback = animalFeedback; wasJumping = false;
            direction = heading.sqrMagnitude > 0.001f ? heading.normalized : Vector3.forward;
            direction.y = 0;
            direction.Normalize(); desiredDirection = direction;
            turnIn = random.Range(1.2f, 3.2f); recoilFor = 0; age = 0;
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
            if (recoilFor <= 0) body.linearVelocity = travelDirection * (step.Distance / dt);
            else recoilFor -= dt;
            visual.localPosition = Vector3.up * step.Height;
            visual.rotation = Quaternion.RotateTowards(visual.rotation, Quaternion.LookRotation(travelDirection), dt * 500);
            float shade = 1 - step.Height * 0.22f;
            shadow.localScale = new Vector3(1.2f * shade, 0.012f, 0.9f * shade);
            bool jumping = step.Height > 0.01f;
            if (jumping && !wasJumping) feedback?.Hop(body.position);
            wasJumping = jumping;
        }

        private void Update()
        {
            if (!IsAlive) return;
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / 0.24f);
            visual.localScale = Vector3.one * Mathf.Lerp(0.05f, 1, 1 - Mathf.Pow(1 - t, 3));
        }

        private void OnCollisionEnter(Collision collision)
        {
            ReportContact(AnimalContactRelay.Resolve(collision.collider));
        }

        public void ReportContact(AnimalAgent other)
        {
            if (!IsAlive || other == null || other == this || !other.IsAlive) return;
            recoilFor = 0.18f;
            collisions.Collide(this, other);
        }
    }
}
