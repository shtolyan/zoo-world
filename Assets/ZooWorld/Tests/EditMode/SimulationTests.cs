using System;
using NUnit.Framework;
using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld.Tests
{
    public sealed class FoodChainTests
    {
        private PopulationModel model;
        [SetUp] public void SetUp() => model = new PopulationModel(new FoodChain());

        [Test] public void PreyDoNotEatEachOther()
        {
            var first = model.Register(Diet.Prey); var second = model.Register(Diet.Prey);
            Assert.That(model.TryEncounter(first, second, out _), Is.False);
            Assert.That(model.Snapshot.LivingPrey, Is.EqualTo(2));
            Assert.That(model.Snapshot.DeadPrey, Is.Zero);
        }

        [TestCase(true)] [TestCase(false)]
        public void PredatorEatsPreyInEitherCallbackOrder(bool predatorFirst)
        {
            var prey = model.Register(Diet.Prey); var predator = model.Register(Diet.Predator);
            Assert.That(model.TryEncounter(predatorFirst ? predator : prey, predatorFirst ? prey : predator, out var result), Is.True);
            Assert.That(result.Eater, Is.SameAs(predator)); Assert.That(result.Victim, Is.SameAs(prey));
            Assert.That(prey.IsAlive, Is.False); Assert.That(predator.IsAlive, Is.True);
            Assert.That(model.Snapshot.DeadPrey, Is.EqualTo(1));
        }

        [TestCase(true)] [TestCase(false)]
        public void PredatorDuelHasExactlyOneOrderIndependentSurvivor(bool reverse)
        {
            var older = model.Register(Diet.Predator); var newer = model.Register(Diet.Predator);
            model.TryEncounter(reverse ? newer : older, reverse ? older : newer, out var result);
            Assert.That(result.Eater, Is.SameAs(older));
            Assert.That(model.Snapshot.LivingPredators, Is.EqualTo(1));
            Assert.That(model.Snapshot.DeadPredators, Is.EqualTo(1));
        }

        [Test] public void DuplicateCallbacksCannotCountDeathTwice()
        {
            var prey = model.Register(Diet.Prey); var predator = model.Register(Diet.Predator);
            Assert.That(model.TryEncounter(prey, predator, out _), Is.True);
            Assert.That(model.TryEncounter(predator, prey, out _), Is.False);
            Assert.That(model.Snapshot.DeadPrey, Is.EqualTo(1));
        }

        [Test] public void DeadPredatorCannotEatAnotherAnimalInSamePhysicsStep()
        {
            var first = model.Register(Diet.Predator); var second = model.Register(Diet.Predator); var prey = model.Register(Diet.Prey);
            model.TryEncounter(first, second, out _);
            Assert.That(model.TryEncounter(second, prey, out _), Is.False);
            Assert.That(prey.IsAlive, Is.True);
        }

        [Test] public void AForeignLifeIsRejectedEvenWhenItsIdMatches()
        {
            var local = model.Register(Diet.Prey);
            var foreignModel = new PopulationModel(new FoodChain());
            var foreign = foreignModel.Register(Diet.Predator);
            Assert.That(local.Id, Is.EqualTo(foreign.Id));
            Assert.That(model.TryEncounter(local, foreign, out _), Is.False);
        }

        [Test] public void ResetInvalidatesOldLivesAndKeepsIdsUnique()
        {
            var old = model.Register(Diet.Predator); model.Reset();
            var current = model.Register(Diet.Prey);
            Assert.That(old.IsAlive, Is.False); Assert.That(current.Id, Is.GreaterThan(old.Id));
            Assert.That(model.TryEncounter(old, current, out _), Is.False);
            Assert.That(model.Snapshot.TotalSpawned, Is.EqualTo(1));
            Assert.That(model.Snapshot.DeadPredators, Is.Zero);
        }

        [Test] public void DeathIsCommittedBeforeObserversRun()
        {
            var prey = model.Register(Diet.Prey); var predator = model.Register(Diet.Predator);
            bool observed = false;
            model.Changed += snapshot => { observed = true; Assert.That(prey.IsAlive, Is.False); Assert.That(snapshot.DeadPrey, Is.EqualTo(1)); };
            model.TryEncounter(prey, predator, out _);
            Assert.That(observed, Is.True);
        }

        [Test] public void SelfCollisionIsIgnored()
        {
            var predator = model.Register(Diet.Predator);
            Assert.That(model.TryEncounter(predator, predator, out _), Is.False);
        }
    }

    public sealed class LocomotionTests
    {
        [TestCase(0.02f)] [TestCase(0.033f)] [TestCase(0.17f)]
        public void HopTravelsFixedDistanceAcrossIrregularTicks(float tick)
        {
            var hop = new HopLocomotion(1, 0.4f, 2, 0.8f);
            float total = 0; float elapsed = 0;
            while (elapsed < 0.99f)
            {
                float dt = Mathf.Min(tick, 0.99f - elapsed);
                total += hop.Step(dt).Distance; elapsed += dt;
            }
            Assert.That(total, Is.EqualTo(2).Within(0.001f));
        }

        [Test] public void HopRestsBetweenJumps()
        {
            var hop = new HopLocomotion(1.5f, 0.4f, 2, 1);
            hop.Step(0.5f);
            var rest = hop.Step(0.5f);
            Assert.That(rest.Distance, Is.Zero); Assert.That(rest.Height, Is.Zero);
        }

        [Test] public void HopReachesApexAndLands()
        {
            var hop = new HopLocomotion(1.5f, 0.4f, 2, 0.8f);
            Assert.That(hop.Step(0.2f).Height, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(hop.Step(0.2f).Height, Is.EqualTo(0).Within(0.001f));
        }

        [Test] public void LargeTickPreservesAllJumpDistances()
        {
            var hop = new HopLocomotion(1, 0.4f, 2, 1);
            Assert.That(hop.Step(3.5f).Distance, Is.EqualTo(8).Within(0.001f));
        }

        [Test] public void LinearTravelIsIndependentOfTickSize()
        {
            var linear = new LinearLocomotion(1.9f);
            Assert.That(linear.Step(0.1f).Distance + linear.Step(0.9f).Distance, Is.EqualTo(1.9f).Within(0.0001f));
        }

        [Test] public void InvalidHopSettingsAreRejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new HopLocomotion(1, 2, 1, 1));
    }

    public sealed class SpawnClockTests
    {
        private sealed class FixedRandom : IRandomSource
        {
            public float Range(float min, float max) => 1.5f;
            public int Range(int min, int max) => min;
        }

        [Test] public void ClockWaitsThenSchedulesExactlyOneArrival()
        {
            var clock = new SpawnClock(new FixedRandom());
            Assert.That(clock.Advance(1.4f), Is.Zero);
            Assert.That(clock.Advance(0.11f), Is.EqualTo(1));
            Assert.That(clock.Remaining, Is.EqualTo(1.49f).Within(0.001f));
        }

        [Test] public void LongFrameDoesNotLoseArrivals()
        {
            var clock = new SpawnClock(new FixedRandom());
            Assert.That(clock.Advance(4.6f), Is.EqualTo(3));
        }

        [Test] public void AllRandomizedIntervalsStayBetweenOneAndTwoSeconds()
        {
            var clock = new SpawnClock(new SeededRandom(2026));
            for (int i = 0; i < 1000; i++) { clock.Reset(); Assert.That(clock.Remaining, Is.InRange(1f, 2f)); }
        }

        [Test] public void PauseDoesNotAdvanceClock()
        {
            var clock = new SpawnClock(new FixedRandom());
            float before = clock.Remaining;
            Assert.That(clock.Advance(0), Is.Zero); Assert.That(clock.Remaining, Is.EqualTo(before));
        }
    }

    public sealed class HabitatBoundsTests
    {
        [TestCase(13, 0)] [TestCase(-13, 0)] [TestCase(0, 8)] [TestCase(0, -8)] [TestCase(20, 15)]
        public void EscapedAnimalTurnsTowardHabitat(float x, float z)
        {
            var position = new Vector3(x, 0, z);
            var heading = new HabitatBounds(new Vector2(24, 13), null).Redirect(position, position.normalized);
            Assert.That(Vector3.Dot(heading, -position.normalized), Is.GreaterThan(0.99f));
        }

        [Test] public void AnimalsInsideKeepTheirHeading()
        {
            var bounds = new HabitatBounds(new Vector2(24, 13), null);
            Assert.That(bounds.Redirect(Vector3.zero, Vector3.right), Is.EqualTo(Vector3.right));
        }
    }
}
