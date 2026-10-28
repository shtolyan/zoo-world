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
        private Rigidbody body;
        private ILocomotion locomotion;
        private IRandomSource random;
        private IAnimalCollisionSink collisions;
        private HabitatBounds bounds;
        private Vector3 direction;
        private float turnIn, recoilFor, age;
        public AnimalLife Life { get; private set; }
        public AnimalDefinition Definition { get; private set; }
        public bool IsAlive => Life != null && Life.IsAlive && gameObject.activeSelf;
        public Rigidbody Body => body;

        public void ConfigureVisuals(Transform model, Transform groundShadow) { visual = model; shadow = groundShadow; }

        public void Initialize(AnimalLife life, AnimalDefinition definition, ILocomotion movement,
            IRandomSource randomSource, IAnimalCollisionSink collisionSink, HabitatBounds habitat, Vector3 heading)
        {
            body = GetComponent<Rigidbody>();
            Life = life; Definition = definition; locomotion = movement; random = randomSource; collisions = collisionSink; bounds = habitat;
            body.mass = definition.Mass;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            direction = heading.sqrMagnitude > 0.001f ? heading.normalized : Vector3.forward;
            direction.y = 0;
            turnIn = random.Range(1.2f, 3.2f); recoilFor = 0; age = 0;
            visual.localPosition = Vector3.zero;
            visual.rotation = Quaternion.LookRotation(direction);
            visual.localScale = Vector3.one * 0.05f;
            shadow.localScale = new Vector3(1.2f, 0.012f, 0.9f);
            name = $"{definition.DisplayName} #{life.Id}";
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;
            float dt = Time.fixedDeltaTime;
            var step = locomotion.Step(dt);
            turnIn -= dt;
            if (turnIn <= 0 && step.Height < 0.03f)
            {
                direction = Quaternion.Euler(0, random.Range(-115f, 115f), 0) * direction;
                turnIn = random.Range(1.2f, 3.2f);
            }
            direction = bounds.Redirect(body.position, direction);
            // Leave the solver's impulse intact briefly: prey really bounce, including between jumps.
            if (recoilFor <= 0) body.linearVelocity = direction * (step.Distance / dt);
            else recoilFor -= dt;
            visual.localPosition = Vector3.up * step.Height;
            visual.rotation = Quaternion.RotateTowards(visual.rotation, Quaternion.LookRotation(direction), dt * 500);
            float shade = 1 - step.Height * 0.22f;
            shadow.localScale = new Vector3(1.2f * shade, 0.012f, 0.9f * shade);
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
            if (!IsAlive || !collision.gameObject.TryGetComponent<AnimalAgent>(out var other)) return;
            recoilFor = 0.18f;
            collisions.Collide(this, other);
        }
    }
}
