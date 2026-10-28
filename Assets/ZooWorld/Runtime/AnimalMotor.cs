using System;
using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    /// <summary>Planar physics adapter: wandering, steering and yielding to contact impulses.</summary>
    public sealed class AnimalMotor
    {
        private const float CollisionRecovery = 0.18f;
        private readonly Rigidbody body;
        private readonly TrailBody articulatedBody;
        private ILocomotion locomotion;
        private IRandomSource random;
        private HabitatBounds bounds;
        private float turnSpeed;
        private float turnIn;
        private float recoilFor;
        private Vector3 desiredDirection;

        public Vector3 Heading { get; private set; }
        public Vector3 TravelDirection { get; private set; }
        public float Height { get; private set; }

        public AnimalMotor(Rigidbody body, TrailBody articulatedBody)
        {
            this.body = body;
            this.articulatedBody = articulatedBody;
        }

        public void Reset(ILocomotion movement, IRandomSource randomSource, HabitatBounds habitat,
            Vector3 heading, float degreesPerSecond)
        {
            locomotion = movement ?? throw new ArgumentNullException(nameof(movement));
            random = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            bounds = habitat ?? throw new ArgumentNullException(nameof(habitat));
            heading.y = 0;
            Heading = heading.sqrMagnitude > 0.001f ? heading.normalized : Vector3.forward;
            desiredDirection = TravelDirection = Heading;
            turnSpeed = degreesPerSecond * Mathf.Deg2Rad;
            turnIn = random.Range(1.2f, 3.2f);
            recoilFor = Height = 0;
        }

        public void YieldToContact() => recoilFor = CollisionRecovery;

        public void Step(float deltaTime, bool biting)
        {
            MotionStep step = locomotion.Step(deltaTime);
            Height = step.Height;
            UpdateHeading(deltaTime);
            TravelDirection = articulatedBody != null
                ? articulatedBody.Advance(Heading, deltaTime)
                : Heading;

            if (biting)
                body.linearVelocity = Vector3.zero;
            else if (recoilFor > 0)
                recoilFor = Mathf.Max(0, recoilFor - deltaTime);
            else
                body.linearVelocity = TravelDirection * (step.Distance / deltaTime);
        }

        private void UpdateHeading(float deltaTime)
        {
            turnIn -= deltaTime;
            if (turnIn <= 0 && Height < 0.03f)
            {
                desiredDirection = Quaternion.Euler(0, random.Range(-115f, 115f), 0) * Heading;
                turnIn = random.Range(1.2f, 3.2f);
            }

            Vector3 inward = bounds.Redirect(body.position, desiredDirection);
            if (inward != desiredDirection)
            {
                desiredDirection = inward;
                Heading = inward;
            }
            else if (Height <= 0.001f)
            {
                Heading = Vector3.RotateTowards(Heading, desiredDirection, turnSpeed * deltaTime, 0);
            }
        }
    }
}
