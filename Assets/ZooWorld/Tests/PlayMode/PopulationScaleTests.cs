using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using ZooWorld.Domain;
using Object = UnityEngine.Object;

namespace ZooWorld.Tests
{
    /// <summary>Reproducible Editor physics probe, not a player FPS benchmark or a collision-crowding test.</summary>
    public sealed class PopulationScaleTests
    {
        [Serializable] private sealed class Sample
        {
            public int animals;
            public int rigidBodies;
            public double meanPhysicsMilliseconds;
            public double p95PhysicsMilliseconds;
            public double maxPhysicsMilliseconds;
        }

        [Serializable] private sealed class Report
        {
            public string unityVersion;
            public string processor;
            public string scope = "Editor; separated animals; Physics.Simulate only; excludes rendering and script Update/FixedUpdate";
            public int measuredSteps = 30;
            public List<Sample> samples = new List<Sample>();
        }

        [UnityTest] public IEnumerator GrowingPopulationKeepsFiniteBodiesAndRecordsPhysicsCost()
        {
            yield return SceneManager.LoadSceneAsync("ZooWorld");
            yield return null;
            var simulation = Object.FindAnyObjectByType<ZooSimulation>();
            simulation.ResetSimulation(false);
            simulation.enabled = false;
            var previousMode = Physics.simulationMode;
            var report = new Report { unityVersion = Application.unityVersion, processor = SystemInfo.processorType };
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                foreach (int count in new[] { 25, 100, 200 })
                {
                    var root = new GameObject("Population scale probe");
                    var population = new AnimalPopulation(root.transform,
                        new HabitatBounds(new Vector2(1000, 1000), null), new SeededRandom(2026), null);
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var position = new Vector3((i % 20) * 10 - 100, 0, (i / 20) * 10 - 50);
                            population.Spawn(simulation.Settings.Species[i % 2], position, Vector3.forward);
                        }
                        var bodies = root.GetComponentsInChildren<Rigidbody>();
                        var durations = new double[report.measuredSteps];
                        var timer = new Stopwatch();
                        for (int step = 0; step < durations.Length + 5; step++)
                        {
                            yield return new WaitForFixedUpdate();
                            timer.Restart();
                            Physics.Simulate(Time.fixedDeltaTime);
                            timer.Stop();
                            if (step >= 5) durations[step - 5] = timer.Elapsed.TotalMilliseconds;
                        }

                        Assert.That(population.Model.Snapshot.Living, Is.EqualTo(count));
                        foreach (var body in bodies)
                        {
                            Assert.That(float.IsNaN(body.position.sqrMagnitude) || float.IsInfinity(body.position.sqrMagnitude), Is.False);
                            Assert.That(float.IsNaN(body.linearVelocity.sqrMagnitude) || float.IsInfinity(body.linearVelocity.sqrMagnitude), Is.False);
                        }
                        Array.Sort(durations);
                        double total = 0;
                        foreach (double duration in durations) total += duration;
                        report.samples.Add(new Sample
                        {
                            animals = count,
                            rigidBodies = bodies.Length,
                            meanPhysicsMilliseconds = total / durations.Length,
                            p95PhysicsMilliseconds = durations[(int)Math.Ceiling(durations.Length * 0.95) - 1],
                            maxPhysicsMilliseconds = durations[durations.Length - 1]
                        });
                    }
                    finally
                    {
                        population.Dispose();
                        Object.Destroy(root);
                    }
                    yield return null;
                }
            }
            finally
            {
                Physics.simulationMode = previousMode;
                simulation.enabled = true;
                Time.timeScale = 1;
            }
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/population-scale.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
        }
    }
}
