using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ZooWorld.Domain;

namespace ZooWorld
{
    /// <summary>Scene composition and player controls. Animal lifecycle belongs to AnimalPopulation.</summary>
    public sealed class ZooSimulation : MonoBehaviour
    {
        public SimulationSettings Settings;
        public Camera WorldCamera;
        public ZooHud Hud;
        public Transform AnimalContainer;
        public HabitatAudio Audio;

        private AnimalPopulation population;
        private AnimalSpawner spawner;
        private SpawnClock spawnClock;
        private TastyPresenter tasty;
        private bool paused;
        private bool fast;
        private float elapsed;

        public PopulationModel Model => population.Model;
        public IReadOnlyCollection<AnimalAgent> ActiveAnimals => population.Active;
        public bool IsPaused => paused;
        public int VisibleTastyLabels => tasty.VisibleCount;
        public float Elapsed => elapsed;

        private void Start()
        {
            if (Settings == null || WorldCamera == null || Hud == null || AnimalContainer == null)
                throw new InvalidOperationException("Assign simulation settings, camera, HUD and animal container.");
            Settings.Validate();

            int seed = Settings.RandomizeSeed ? Environment.TickCount : Settings.RandomSeed;
            var bounds = new HabitatBounds(Settings.HabitatSize, WorldCamera);
            population = new AnimalPopulation(AnimalContainer, bounds, new SeededRandom(unchecked(seed + 7919)), Audio);
            spawner = new AnimalSpawner(Settings.Species, population, bounds, new SeededRandom(seed));
            // Arrival intervals are independent of species selection and movement randomness.
            spawnClock = new SpawnClock(new SeededRandom(unchecked(seed + 104729)),
                Settings.MinimumSpawnInterval, Settings.MaximumSpawnInterval);
            tasty = new TastyPresenter(Hud.ToastLayer, WorldCamera, Hud.DeadPrey.font);

            population.Model.Changed += Hud.Present;
            population.Ate += PresentMeal;
            BindControls();
            ResetSimulation();
        }

        private void Update()
        {
            if (population == null) return;
            ReadKeyboard();
            elapsed += Time.deltaTime;
            int due = spawnClock.Advance(Time.deltaTime);
            for (int i = 0; i < due; i++) PresentArrival(spawner.SpawnRandom());
            Hud.PresentTime(elapsed, spawnClock.Remaining, paused, fast);
            Hud.PresentAudio(Audio != null && Audio.IsMuted);
        }

        private void LateUpdate()
        {
            if (population == null) return;
            // All animal Update animations have advanced before finished victims are returned.
            population.ReleaseConsumed();
            tasty.Tick(Time.deltaTime);
        }

        public void SpawnSpecies(int index) => PresentArrival(spawner.SpawnSpecies(index));

        public AnimalAgent SpawnAt(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            var animal = population.Spawn(definition, position, direction);
            PresentArrival(animal);
            return animal;
        }

        public void Collide(AnimalAgent first, AnimalAgent second, bool firstIsHead, bool secondIsHead)
            => population.Collide(first, second, firstIsHead, secondIsHead);

        public void TogglePause()
        {
            paused = !paused;
            ApplyTimeScale();
            if (Audio != null) Audio.Click();
        }

        public void ToggleSpeed()
        {
            fast = !fast;
            ApplyTimeScale();
            if (Audio != null) Audio.Click();
        }

        public void ToggleSound()
        {
            if (Audio != null) Audio.ToggleMute();
        }

        public void ResetSimulation(bool seedPopulation = true)
        {
            tasty.Clear();
            population.Reset();
            spawnClock.Reset();
            elapsed = 0;
            paused = fast = false;
            ApplyTimeScale();
            if (Audio != null) Audio.Clear();
            if (seedPopulation)
            {
                spawner.Seed();
                if (Audio != null) Audio.Spawn(Vector3.zero);
            }
        }

        private void PresentArrival(AnimalAgent animal)
        {
            if (animal != null && Audio != null) Audio.Spawn(animal.transform.position);
        }

        private void PresentMeal(AnimalAgent eater)
        {
            tasty.Show(eater);
            if (Audio != null) Audio.Eat(eater.transform.position);
        }

        private void ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
            if (keyboard.rKey.wasPressedThisFrame) Restart();
            if (keyboard.digit1Key.wasPressedThisFrame) SpawnSpecies(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SpawnSpecies(1);
            if (keyboard.mKey.wasPressedThisFrame) ToggleSound();
        }

        private void ApplyTimeScale() => Time.timeScale = paused ? 0 : fast ? 2 : 1;
        private void Restart() => ResetSimulation();

        private void BindControls()
        {
            Hud.PauseRequested += TogglePause;
            Hud.SpeedRequested += ToggleSpeed;
            Hud.ResetRequested += Restart;
            Hud.SpawnRequested += SpawnSpecies;
            Hud.SoundRequested += ToggleSound;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
            if (population == null) return;
            population.Ate -= PresentMeal;
            // A destroyed Unity object still has managed event delegates to detach.
            if (!ReferenceEquals(Hud, null))
            {
                population.Model.Changed -= Hud.Present;
                Hud.PauseRequested -= TogglePause;
                Hud.SpeedRequested -= ToggleSpeed;
                Hud.ResetRequested -= Restart;
                Hud.SpawnRequested -= SpawnSpecies;
                Hud.SoundRequested -= ToggleSound;
            }
            population.Dispose();
        }
    }
}
