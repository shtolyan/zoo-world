using System;
using NUnit.Framework;
using ZooWorld.Domain;

namespace ZooWorld.Tests
{
    public sealed class ArchitectureTests
    {
        private sealed class FractionRandom : IRandomSource
        {
            private readonly float fraction;
            public FractionRandom(float fraction) => this.fraction = fraction;
            public float Range(float minimum, float maximum) => minimum + (maximum - minimum) * fraction;
            public int Range(int minimum, int maximum) => minimum;
        }

        [Test] public void BiteAnimationEndsBeforeAnotherMealIsAllowed()
        {
            var activity = new AnimalActivity();
            Assert.That(activity.TryBeginBite(), Is.True);
            Assert.That(activity.TryBeginBite(), Is.False);
            activity.Tick(AnimalActivity.BiteDuration + 0.001f);
            Assert.That(activity.IsBiting, Is.False);
            Assert.That(activity.CanBite, Is.False);
            activity.Tick(AnimalActivity.BiteCooldown - AnimalActivity.BiteDuration);
            Assert.That(activity.CanBite, Is.True);
        }

        [Test] public void ConsumptionIsIdempotentAndResetRestoresActions()
        {
            var activity = new AnimalActivity();
            activity.TryBeginBite();
            activity.BeginConsumption();
            activity.Tick(0.1f);
            activity.BeginConsumption();
            activity.Tick(0.15f);
            Assert.That(activity.ConsumptionFinished, Is.True);
            Assert.That(activity.TryBeginBite(), Is.False);
            activity.Reset();
            Assert.That(activity.IsConsuming, Is.False);
            Assert.That(activity.ConsumptionFinished, Is.False);
            Assert.That(activity.CanBite, Is.True);
        }

        [Test] public void ZeroTickFreezesConsumption()
        {
            var activity = new AnimalActivity();
            activity.BeginConsumption();
            activity.Tick(0.1f);
            float before = activity.ConsumptionProgress;
            activity.Tick(0);
            Assert.That(activity.ConsumptionProgress, Is.EqualTo(before));
        }

        [TestCase(-0.1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidTimeCannotPoisonMovementOrActionState(float deltaTime)
        {
            var activity = new AnimalActivity();
            var hop = new HopLocomotion(1, 0.4f, 2, 0.8f);
            var linear = new LinearLocomotion(2);
            Assert.Throws<ArgumentOutOfRangeException>(() => activity.Tick(deltaTime));
            Assert.Throws<ArgumentOutOfRangeException>(() => hop.Step(deltaTime));
            Assert.Throws<ArgumentOutOfRangeException>(() => linear.Step(deltaTime));
            Assert.That(hop.Step(1).Distance, Is.EqualTo(2).Within(0.001f));
            Assert.That(linear.Step(1).Distance, Is.EqualTo(2));
            Assert.That(activity.CanBite, Is.True);
        }

        [Test] public void FailedSpawnPreparationDoesNotPublishOrCountALife()
        {
            var model = new PopulationModel(new FoodChain());
            AnimalLife rejected = null;
            int notifications = 0;
            model.Changed += _ => notifications++;
            Assert.Throws<InvalidOperationException>(() => model.Register(Diet.Prey, life =>
            {
                rejected = life;
                throw new InvalidOperationException("Failed runtime setup");
            }));
            Assert.That(rejected.IsAlive, Is.False);
            Assert.That(model.Snapshot.TotalSpawned, Is.Zero);
            Assert.That(model.Snapshot.Living, Is.Zero);
            Assert.That(notifications, Is.Zero);
            Assert.That(model.Register(Diet.Prey).Id, Is.GreaterThan(rejected.Id));
        }

        [Test] public void SpawnObserversSeeAnAlreadyPreparedLife()
        {
            var model = new PopulationModel(new FoodChain());
            bool prepared = false;
            int notifications = 0;
            model.Changed += snapshot =>
            {
                notifications++;
                Assert.That(prepared, Is.True);
                Assert.That(snapshot.Living, Is.EqualTo(1));
            };
            model.Register(Diet.Prey, _ => prepared = true);
            Assert.That(notifications, Is.EqualTo(1));
        }

        [Test] public void EveryEntryInAThousandSpeciesCatalogCanBeSelected()
        {
            var weights = new float[1000];
            for (int i = 0; i < weights.Length; i++) weights[i] = 1;
            var distribution = new WeightedDistribution(weights);
            for (int i = 0; i < weights.Length; i++)
                Assert.That(distribution.Pick(new FractionRandom((i + 0.5f) / weights.Length)), Is.EqualTo(i));
        }

        [TestCase(0f, 0)] [TestCase(0.74f, 0)] [TestCase(0.76f, 1)] [TestCase(1f, 1)]
        public void WeightedSelectionRespectsBoundaries(float fraction, int expected)
        {
            var distribution = new WeightedDistribution(new[] { 3f, 1f });
            Assert.That(distribution.Pick(new FractionRandom(fraction)), Is.EqualTo(expected));
        }

        [Test] public void LargeFiniteWeightsDoNotOverflowTheirSum()
        {
            var distribution = new WeightedDistribution(new[] { float.MaxValue, float.MaxValue });
            Assert.That(distribution.Pick(new FractionRandom(0.25f)), Is.Zero);
            Assert.That(distribution.Pick(new FractionRandom(0.75f)), Is.EqualTo(1));
        }

        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidWeightsAreRejected(float weight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeightedDistribution(new[] { weight }));
        }
    }
}
