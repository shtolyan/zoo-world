using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ZooWorld.Tests
{
    public sealed class HabitatIntegrationTests
    {
        private ZooSimulation simulation;

        [UnitySetUp] public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("ZooWorld");
            yield return null;
            simulation = Object.FindAnyObjectByType<ZooSimulation>();
            Assert.That(simulation, Is.Not.Null);
            simulation.ResetSimulation(false);
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            if (simulation != null) simulation.ResetSimulation(false);
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest] public IEnumerator RealPhysicsContactEatsPreyUpdatesUguiAndShowsTasty()
        {
            var prey = simulation.SpawnAt(simulation.Settings.Species[0], Vector3.left * 0.3f, Vector3.right);
            var predator = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.right * 0.3f, Vector3.left);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(prey.IsAlive, Is.False); Assert.That(prey.gameObject.activeSelf, Is.False);
            Assert.That(predator.IsAlive, Is.True);
            Assert.That(simulation.Model.Snapshot.DeadPrey, Is.EqualTo(1));
            Assert.That(simulation.Hud.DeadPrey.text, Is.EqualTo("01"));
            Assert.That(simulation.VisibleTastyLabels, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator RealPredatorContactLeavesOneSurvivorAndOneDeath()
        {
            var first = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.left * 0.3f, Vector3.right);
            var second = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.right * 0.3f, Vector3.left);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(first.IsAlive, Is.True); Assert.That(second.IsAlive, Is.False);
            Assert.That(simulation.Model.Snapshot.DeadPredators, Is.EqualTo(1));
            Assert.That(simulation.Hud.DeadPredators.text, Is.EqualTo("01"));
        }

        [UnityTest] public IEnumerator PreyContactSeparatesBodiesWithoutDeaths()
        {
            var first = simulation.SpawnAt(simulation.Settings.Species[0], Vector3.left * 0.3f, Vector3.right);
            var second = simulation.SpawnAt(simulation.Settings.Species[0], Vector3.right * 0.3f, Vector3.left);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(first.IsAlive && second.IsAlive, Is.True);
            Assert.That(simulation.Model.Snapshot.DeadPrey, Is.Zero);
            Assert.That(Vector3.Distance(first.Body.position, second.Body.position), Is.GreaterThan(0.7f));
        }

        [UnityTest] public IEnumerator PooledAnimalGetsAFreshLifeAndRejectsOldCallbacks()
        {
            var prey = simulation.SpawnAt(simulation.Settings.Species[0], Vector3.zero, Vector3.forward);
            var predator = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.right * 0.3f, Vector3.left);
            var oldLife = prey.Life;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            var reborn = simulation.SpawnAt(simulation.Settings.Species[0], Vector3.left * 7, Vector3.forward);
            Assert.That(reborn, Is.SameAs(prey)); Assert.That(reborn.Life.Id, Is.Not.EqualTo(oldLife.Id));
            Assert.That(simulation.Model.TryEncounter(predator.Life, oldLife, out _), Is.False);
            Assert.That(reborn.IsAlive, Is.True); Assert.That(simulation.Model.Snapshot.DeadPrey, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator PauseFreezesSimulationButUguiResumeStillWorks()
        {
            simulation.Hud.PauseButton.onClick.Invoke();
            float elapsed = simulation.Elapsed;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(simulation.IsPaused, Is.True); Assert.That(simulation.Elapsed, Is.EqualTo(elapsed).Within(0.001f));
            simulation.Hud.PauseButton.onClick.Invoke();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1)); Assert.That(simulation.IsPaused, Is.False);
        }

        [UnityTest] public IEnumerator SpawnAndRestartButtonsAreWired()
        {
            simulation.Hud.SpawnButtons[0].onClick.Invoke(); simulation.Hud.SpawnButtons[1].onClick.Invoke();
            Assert.That(simulation.Model.Snapshot.TotalSpawned, Is.EqualTo(2));
            simulation.Hud.SpeedButton.onClick.Invoke(); Assert.That(Time.timeScale, Is.EqualTo(2));
            simulation.Hud.ResetButton.onClick.Invoke();
            Assert.That(simulation.Model.Snapshot.DeadPrey + simulation.Model.Snapshot.DeadPredators, Is.Zero);
            Assert.That(simulation.Model.Snapshot.TotalSpawned, Is.EqualTo(16));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest] public IEnumerator EscapedAnimalActuallyReturnsWithoutWalls()
        {
            var animal = simulation.SpawnAt(simulation.Settings.Species[1], new Vector3(13, 0, 0), Vector3.right);
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(animal.Body.position.x, Is.LessThan(12.5f));
            Assert.That(animal.Body.linearVelocity.x, Is.LessThan(0));
        }

        [UnityTest] public IEnumerator ScheduledArrivalHappensAfterOneToTwoSeconds()
        {
            float began = Time.time;
            yield return new WaitForSeconds(0.9f);
            Assert.That(simulation.Model.Snapshot.TotalSpawned, Is.Zero);
            while (simulation.Model.Snapshot.TotalSpawned == 0 && Time.time - began < 2.2f) yield return null;
            Assert.That(simulation.Model.Snapshot.TotalSpawned, Is.EqualTo(1));
            Assert.That(Time.time - began, Is.InRange(0.99f, 2.15f));
        }

        [UnityTest] public IEnumerator SnakeHasDynamicConnectedBodyThatBendsBehindItsHead()
        {
            var snake = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.zero, Vector3.forward);
            var body = snake.GetComponent<TrailBody>();
            Assert.That(body.SegmentCount, Is.EqualTo(12));
            for (int i = 0; i < 35; i++) yield return new WaitForFixedUpdate();
            float bend = 0;
            Vector3 previous = snake.Body.position + Vector3.up * 0.3f;
            for (int i = 0; i < body.SegmentCount; i++)
            {
                var segment = body.GetSegment(i);
                Assert.That(segment.isKinematic, Is.False);
                Assert.That(segment.GetComponent<Collider>().isTrigger, Is.False);
                Assert.That(segment.GetComponent<ConfigurableJoint>().connectedBody, Is.Not.Null);
                Assert.That(Vector3.Distance(segment.position, previous), Is.LessThan(body.Spacing + 0.14f));
                bend = Mathf.Max(bend, Mathf.Abs(segment.position.x - snake.Body.position.x));
                previous = segment.position;
            }
            Assert.That(bend, Is.GreaterThan(0.08f));
            Assert.That(snake.IsAlive, Is.True);
        }

        [UnityTest] public IEnumerator ContactWithTheTailCountsOnePreyDeathAndOneEatingSound()
        {
            var snake = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.right * 4, Vector3.right);
            var tail = snake.GetComponent<TrailBody>();
            Vector3 tailPosition = tail.GetSegment(tail.SegmentCount - 1).position; tailPosition.y = 0;
            var frog = simulation.SpawnAt(simulation.Settings.Species[0], tailPosition, Vector3.back);
            Assert.That(Vector3.Distance(frog.transform.position, snake.transform.position), Is.GreaterThan(2));
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(frog.IsAlive, Is.False); Assert.That(snake.IsAlive, Is.True);
            Assert.That(simulation.Model.Snapshot.DeadPrey, Is.EqualTo(1));
            Assert.That(simulation.Audio.EatCueCount, Is.EqualTo(1));
        }

        [UnityTest] public IEnumerator ReusedSnakeResetsEveryPhysicalSegmentAndItsTrail()
        {
            var snake = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.zero, Vector3.forward);
            long oldId = snake.Life.Id;
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            simulation.ResetSimulation(false);
            var reused = simulation.SpawnAt(simulation.Settings.Species[1], new Vector3(-7, 0, 4), Vector3.right);
            Assert.That(reused, Is.SameAs(snake)); Assert.That(reused.Life.Id, Is.GreaterThan(oldId));
            var body = reused.GetComponent<TrailBody>();
            for (int i = 0; i < body.SegmentCount; i++)
            {
                Vector3 expected = new Vector3(-7 - (i + 1) * body.Spacing, 0.3f, 4);
                Assert.That(Vector3.Distance(body.GetSegment(i).position, expected), Is.LessThan(0.001f),
                    $"Segment {i}: physics={body.GetSegment(i).position}, transform={body.GetSegment(i).transform.position}, expected={expected}");
                Assert.That(body.GetSegment(i).linearVelocity.sqrMagnitude, Is.LessThan(0.001f));
            }
        }

        [UnityTest] public IEnumerator SoundButtonMutesTheCuesAndEatingCooldownPreventsStacking()
        {
            Assert.That(simulation.Audio.Sounds.Spawn, Is.Not.Null);
            Assert.That(simulation.Audio.Sounds.Hop, Is.Not.Null);
            Assert.That(simulation.Audio.Sounds.Eat, Is.Not.Null);
            Assert.That(simulation.Audio.Sounds.Click, Is.Not.Null);
            simulation.Hud.SoundButton.onClick.Invoke();
            Assert.That(simulation.Audio.IsMuted, Is.True);
            simulation.Audio.Eat(Vector3.zero);
            Assert.That(simulation.Audio.EatCueCount, Is.Zero);
            simulation.Hud.SoundButton.onClick.Invoke();
            Assert.That(simulation.Audio.IsMuted, Is.False);
            simulation.Audio.Clear(); simulation.Audio.Eat(Vector3.zero); simulation.Audio.Eat(Vector3.zero);
            Assert.That(simulation.Audio.EatCueCount, Is.EqualTo(1));
            yield return null;
        }
    }
}
