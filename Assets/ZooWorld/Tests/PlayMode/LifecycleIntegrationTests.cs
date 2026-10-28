using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZooWorld.Domain;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    public sealed class LifecycleIntegrationTests
    {
        private ZooSimulation simulation;

        private sealed class FailingRandom : IRandomSource
        {
            public float Range(float minimum, float maximum) => throw new InvalidOperationException("Setup failed");
            public int Range(int minimum, int maximum) => throw new InvalidOperationException("Setup failed");
        }

        [UnitySetUp] public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("ZooWorld");
            yield return null;
            simulation = Object.FindAnyObjectByType<ZooSimulation>();
            simulation.ResetSimulation(false);
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            if (simulation != null) simulation.ResetSimulation(false);
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest] public IEnumerator FirstActivationAndPoolReuseAlwaysHaveInjectedDependencies()
        {
            var templateRoot = new GameObject("Inactive test template");
            templateRoot.SetActive(false);
            var prefab = Object.Instantiate(simulation.Settings.Species[0].Prefab, templateRoot.transform);
            prefab.gameObject.AddComponent<LifeInitializationProbe>();
            var definition = Object.Instantiate(simulation.Settings.Species[0]);
            definition.Prefab = prefab;
            try
            {
                var animal = simulation.SpawnAt(definition, Vector3.zero, Vector3.forward);
                var probe = animal.GetComponent<LifeInitializationProbe>();
                Assert.That(probe.EnableCount, Is.EqualTo(1));
                Assert.That(probe.SawUninitializedLife, Is.False);
                var oldLife = animal.Life;
                simulation.ResetSimulation(false);
                var reused = simulation.SpawnAt(definition, Vector3.right * 4, Vector3.right);
                Assert.That(reused, Is.SameAs(animal));
                Assert.That(reused.Life.Id, Is.GreaterThan(oldLife.Id));
                Assert.That(probe.EnableCount, Is.EqualTo(2));
                Assert.That(probe.SawUninitializedLife, Is.False);
                simulation.ResetSimulation(false);
            }
            finally
            {
                Object.Destroy(definition);
                Object.Destroy(templateRoot);
            }
            yield return null;
        }

        [UnityTest] public IEnumerator FailedRuntimePreparationLeavesNoObjectOrPopulationEntry()
        {
            var root = new GameObject("Failed spawn test");
            var population = new AnimalPopulation(root.transform, new HabitatBounds(new Vector2(24, 13), null), new FailingRandom(), null);
            int notifications = 0;
            population.Model.Changed += _ => notifications++;
            try
            {
                Assert.Throws<InvalidOperationException>(() => population.Spawn(simulation.Settings.Species[1], Vector3.zero, Vector3.forward));
                Assert.That(population.Model.Snapshot.TotalSpawned, Is.Zero);
                Assert.That(population.Active.Count, Is.Zero);
                Assert.That(notifications, Is.Zero);
                yield return null;
                Assert.That(root.GetComponentsInChildren<AnimalAgent>(true), Is.Empty);
            }
            finally
            {
                population.Dispose();
                Object.Destroy(root);
            }
        }

        [UnityTest] public IEnumerator NewSpeciesRecombinesExistingPrefabMovementAndDiet()
        {
            var root = new GameObject("Custom catalog test");
            var species = Object.Instantiate(simulation.Settings.Species[0]);
            var movement = ScriptableObject.CreateInstance<LinearMovementDefinition>();
            species.DisplayName = "Gliding predator";
            species.Diet = Diet.Predator;
            species.Movement = movement;
            var bounds = new HabitatBounds(new Vector2(24, 13), null);
            var population = new AnimalPopulation(root.transform, bounds, new SeededRandom(11), null);
            var spawner = new AnimalSpawner(new[] { species }, population, bounds, new SeededRandom(12));
            try
            {
                var predator = spawner.SpawnRandom();
                Assert.That(predator.Definition, Is.SameAs(species));
                var prey = population.Spawn(simulation.Settings.Species[0], predator.Body.position + Vector3.right * 0.3f, Vector3.left);
                yield return new WaitForFixedUpdate();
                yield return new WaitForFixedUpdate();
                Assert.That(predator.IsAlive, Is.True);
                Assert.That(prey.IsAlive, Is.False);
                Assert.That(population.Model.Snapshot.DeadPrey, Is.EqualTo(1));
                Assert.That(population.Model.Snapshot.LivingPredators, Is.EqualTo(1));
            }
            finally
            {
                population.Dispose();
                Object.Destroy(root);
                Object.Destroy(species);
                Object.Destroy(movement);
            }
        }

        [UnityTest] public IEnumerator PauseFreezesBiteAndDisappearanceThenResumeCompletesThem()
        {
            var snake = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.zero, Vector3.forward);
            var frog = simulation.SpawnAt(simulation.Settings.Species[0], Vector3.right * 7, Vector3.forward);
            yield return new WaitForSeconds(0.3f);
            simulation.Collide(snake, frog, true, true);
            simulation.TogglePause();
            float growth = frog.VisualGrowth;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(frog.VisualGrowth, Is.EqualTo(growth));
            Assert.That(frog.gameObject.activeSelf, Is.True);
            Assert.That(snake.IsBiting, Is.True);
            simulation.TogglePause();
            yield return new WaitForSeconds(AnimalAgent.BiteCooldown + 0.05f);
            Assert.That(frog.gameObject.activeSelf, Is.False);
            Assert.That(snake.CanBite, Is.True);
        }

        [UnityTest] public IEnumerator VerticalHeadingFallsBackToAValidPlanarDirection()
        {
            var animal = simulation.SpawnAt(simulation.Settings.Species[1], Vector3.zero, Vector3.up);
            yield return new WaitForFixedUpdate();
            Assert.That(animal.Body.linearVelocity.z, Is.GreaterThan(0));
            Assert.That(float.IsNaN(animal.Body.position.x), Is.False);
        }

        [UnityTest] public IEnumerator NonFiniteSpawnCoordinatesAreRejectedBeforeRegistration()
        {
            Assert.Throws<ArgumentException>(() => simulation.SpawnAt(simulation.Settings.Species[0],
                new Vector3(float.NaN, 0, 0), Vector3.forward));
            Assert.Throws<ArgumentException>(() => simulation.SpawnAt(simulation.Settings.Species[0],
                Vector3.zero, new Vector3(0, 0, float.PositiveInfinity)));
            Assert.That(simulation.Model.Snapshot.TotalSpawned, Is.Zero);
            Assert.That(simulation.ActiveAnimals, Is.Empty);
            yield return null;
        }

        [UnityTest] public IEnumerator SceneTeardownIsSafeWhenHudIsDestroyedBeforeSimulation()
        {
            simulation.enabled = false;
            Object.Destroy(simulation.Hud.gameObject);
            yield return null;
            Object.Destroy(simulation.gameObject);
            yield return null;
            simulation = null;
        }
    }
}
