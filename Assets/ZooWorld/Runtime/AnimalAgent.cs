using System;
using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    public interface IAnimalCollisionSink
    {
        void Collide(AnimalAgent first, AnimalAgent second, bool firstIsHead, bool secondIsHead);
    }

    /// <summary>Unity lifecycle and contact adapter. Movement, animation and action timing are delegated.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class AnimalAgent : MonoBehaviour
    {
        // Keep the serialized bindings on the adapter so existing prefabs need no migration.
        [SerializeField] private Transform visual;
        [SerializeField] private Transform shadow;
        [SerializeField] private TrailBody articulatedBody;
        [SerializeField] private Transform jaw;

        private readonly AnimalActivity activity = new AnimalActivity();
        private Rigidbody body;
        private Rigidbody[] physicalBodies;
        private AnimalMotor motor;
        private AnimalView view;
        private IAnimalCollisionSink collisions;
        private IAnimalFeedback feedback;
        private bool wasJumping;

        public const float BiteDuration = AnimalActivity.BiteDuration;
        public const float BiteCooldown = AnimalActivity.BiteCooldown;
        public const float ConsumptionDuration = AnimalActivity.ConsumptionDuration;
        public AnimalLife Life { get; private set; }
        public AnimalDefinition Definition { get; private set; }
        public bool IsAlive => Life != null && Life.IsAlive && gameObject.activeInHierarchy;
        public Rigidbody Body => body;
        public float VisualGrowth => view != null ? view.Growth : 0;
        public bool CanBite => IsAlive && Life.Diet == Diet.Predator && activity.CanBite;
        public bool IsBiting => activity.IsBiting;
        public bool ConsumptionFinished => activity.ConsumptionFinished;
        public float RestLength => articulatedBody != null ? articulatedBody.RestLength : 0;
        public bool HasValidPresentation => visual != null && shadow != null && visual != transform && shadow != transform
            && visual != shadow && visual.IsChildOf(transform) && shadow.IsChildOf(transform)
            && (articulatedBody == null || articulatedBody.IsConfigured);

        public void ConfigureVisuals(Transform model, Transform groundShadow)
        {
            visual = model;
            shadow = groundShadow;
        }

        public void ConfigureBody(TrailBody value) => articulatedBody = value;
        public void ConfigureJaw(Transform value) => jaw = value;

        public void Initialize(AnimalLife life, AnimalDefinition definition, ILocomotion movement,
            IRandomSource randomSource, IAnimalCollisionSink collisionSink, HabitatBounds habitat,
            Vector3 heading, IAnimalFeedback animalFeedback = null)
        {
            if (life == null) throw new ArgumentNullException(nameof(life));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!HasValidPresentation) throw new InvalidOperationException("Animal prefab has incomplete visual or body bindings.");
            collisions = collisionSink ?? throw new ArgumentNullException(nameof(collisionSink));

            if (body == null) body = GetComponent<Rigidbody>();
            if (physicalBodies == null) physicalBodies = GetComponentsInChildren<Rigidbody>(true);
            if (motor == null) motor = new AnimalMotor(body, articulatedBody);
            if (view == null) view = new AnimalView(visual, shadow, jaw, articulatedBody);

            motor.Reset(movement, randomSource, habitat, heading, definition.TurnSpeed);
            activity.Reset();
            view.Reset(motor.Heading);
            feedback = animalFeedback;
            wasJumping = false;
            Definition = definition;
            Life = life;
            name = $"{definition.DisplayName} #{life.Id}";
        }

        public void ActivateAt(Vector3 position)
        {
            // Inactive rigid bodies cannot reliably accept native poses. Initialize first,
            // activate, then resync native/rendered poses before the first physics tick.
            gameObject.SetActive(true);
            foreach (var part in physicalBodies) part.detectCollisions = true;
            body.mass = Definition.Mass;
            body.interpolation = RigidbodyInterpolation.None;
            body.position = position;
            body.rotation = Quaternion.identity;
            body.transform.SetPositionAndRotation(position, Quaternion.identity);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (articulatedBody != null) articulatedBody.Initialize(this, motor.Heading);
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;
            float deltaTime = Time.fixedDeltaTime;
            motor.Step(deltaTime, activity.IsBiting);
            view.PresentMotion(motor.TravelDirection, motor.Height, deltaTime);

            bool jumping = motor.Height > 0.01f;
            if (jumping && !wasJumping) feedback?.Hop(body.position);
            wasJumping = jumping;
        }

        private void Update()
        {
            if (!IsAlive && !activity.IsConsuming) return;
            activity.Tick(Time.deltaTime);
            view.Tick(Time.deltaTime, activity);
        }

        public void BeginBite() => activity.TryBeginBite();

        public void BeginConsumption()
        {
            if (activity.IsConsuming) return;
            view.CaptureConsumption();
            activity.BeginConsumption();
            foreach (var part in physicalBodies)
            {
                part.detectCollisions = false;
                part.linearVelocity = Vector3.zero;
                part.angularVelocity = Vector3.zero;
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
            if (entering) motor.YieldToContact();
            collisions.Collide(this, other, ownHead, otherHead);
        }
    }
}
