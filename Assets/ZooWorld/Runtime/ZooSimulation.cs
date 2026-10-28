using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ZooWorld.Domain;

namespace ZooWorld
{
    /// <summary>Composition root: constructs and wires dependencies; no global container or singleton.</summary>
    public sealed class ZooSimulation : MonoBehaviour, IAnimalCollisionSink
    {
        public SimulationSettings Settings;
        public Camera WorldCamera;
        public ZooHud Hud;
        public Transform AnimalContainer;
        public HabitatAudio Audio;
        private PopulationModel model;
        private AnimalPool animals;
        private HabitatBounds bounds;
        private IRandomSource random, movementRandom;
        private SpawnClock spawnClock;
        private TastyPresenter tasty;
        private bool paused, fast, seeding;
        private float elapsed;
        public PopulationModel Model => model;
        public IReadOnlyCollection<AnimalAgent> ActiveAnimals => animals.Active;
        public bool IsPaused => paused;
        public int VisibleTastyLabels => tasty.VisibleCount;
        public float Elapsed => elapsed;

        private void Start()
        {
            Settings.Validate();
            int seed = Settings.RandomizeSeed ? Environment.TickCount : Settings.RandomSeed;
            random = new SeededRandom(seed);
            // Species/movement random calls do not affect the 1–2 second arrival sequence.
            movementRandom = new SeededRandom(unchecked(seed + 7919));
            spawnClock = new SpawnClock(new SeededRandom(unchecked(seed + 104729)), Settings.MinimumSpawnInterval, Settings.MaximumSpawnInterval);
            bounds = new HabitatBounds(Settings.HabitatSize, WorldCamera);
            model = new PopulationModel(new FoodChain());
            animals = new AnimalPool(AnimalContainer);
            tasty = new TastyPresenter(Hud.ToastLayer, WorldCamera, Hud.DeadPrey.font);
            model.Changed += Hud.Present;
            Hud.PauseRequested += TogglePause; Hud.SpeedRequested += ToggleSpeed; Hud.ResetRequested += Restart;
            Hud.SpawnRequested += SpawnSpecies;
            Hud.SoundRequested += ToggleSound;
            ResetSimulation();
        }

        private void Update()
        {
            if (model == null) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
                if (keyboard.rKey.wasPressedThisFrame) Restart();
                if (keyboard.digit1Key.wasPressedThisFrame) SpawnSpecies(0);
                if (keyboard.digit2Key.wasPressedThisFrame) SpawnSpecies(1);
                if (keyboard.mKey.wasPressedThisFrame) ToggleSound();
            }
            elapsed += Time.deltaTime;
            int due = spawnClock.Advance(Time.deltaTime);
            for (int i = 0; i < due; i++) SpawnRandom();
            tasty.Tick(Time.deltaTime);
            Hud.PresentTime(elapsed, spawnClock.Remaining, paused, fast);
            Hud.PresentAudio(Audio != null && Audio.IsMuted);
        }

        public void SpawnSpecies(int index)
        {
            if (index >= 0 && index < Settings.Species.Length)
                SpawnWanderer(Settings.Species[index]);
        }

        public AnimalAgent SpawnAt(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            if (definition == null || !definition.IsValid) throw new ArgumentException("A valid species is required.", nameof(definition));
            var movement = definition.Movement.Create(movementRandom);
            var life = model.Register(definition.Diet);
            var animal = animals.Rent(definition, position);
            animal.Initialize(life, definition, movement, movementRandom, this, bounds, direction, Audio);
            animal.ActivateAt(position);
            if (!seeding && Audio != null) Audio.Spawn(position);
            return animal;
        }

        public void Collide(AnimalAgent first, AnimalAgent second)
        {
            if (!model.TryEncounter(first.Life, second.Life, out var predation)) return;
            AnimalAgent eater = first.Life == predation.Eater ? first : second;
            AnimalAgent victim = eater == first ? second : first;
            animals.Release(victim);
            tasty.Show(eater);
            if (Audio != null) Audio.Eat(eater.transform.position);
        }

        public void TogglePause() { paused = !paused; ApplyTimeScale(); if (Audio != null) Audio.Click(); }
        public void ToggleSpeed() { fast = !fast; ApplyTimeScale(); if (Audio != null) Audio.Click(); }
        public void ToggleSound() { if (Audio != null) Audio.ToggleMute(); }
        private void ApplyTimeScale() => Time.timeScale = paused ? 0 : fast ? 2 : 1;
        private void Restart() => ResetSimulation();

        public void ResetSimulation(bool seedPopulation = true)
        {
            tasty.Clear(); animals.ReleaseAll(); model.Reset(); spawnClock.Reset(); elapsed = 0;
            if (Audio != null) Audio.Clear();
            paused = false; fast = false; ApplyTimeScale();
            seeding = true;
            if (seedPopulation)
                for (int i = 0; i < Settings.Species.Length; i++)
                    for (int j = 0; j < Settings.Species[i].InitialPopulation; j++) SpawnSpecies(i);
            seeding = false;
            if (seedPopulation && Audio != null) Audio.Spawn(Vector3.zero);
            Hud.Present(model.Snapshot);
        }

        private void SpawnRandom()
        {
            float total = 0;
            foreach (var species in Settings.Species) total += species.SpawnWeight;
            float selection = random.Range(0, total);
            foreach (var species in Settings.Species)
            {
                selection -= species.SpawnWeight;
                if (selection <= 0) { SpawnWanderer(species); return; }
            }
            SpawnSpecies(Settings.Species.Length - 1);
        }

        private Vector3 RandomHeading() => Quaternion.Euler(0, random.Range(0, 360f), 0) * Vector3.forward;

        private void SpawnWanderer(AnimalDefinition species)
        {
            Vector3 heading = RandomHeading();
            var longBody = species.Prefab.GetComponent<TrailBody>();
            Vector3 trailingOffset = longBody != null ? heading * longBody.RestLength : Vector3.zero;
            SpawnAt(species, bounds.RandomPoint(random, trailingOffset), heading);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
            if (model == null) return;
            model.Changed -= Hud.Present;
            Hud.PauseRequested -= TogglePause; Hud.SpeedRequested -= ToggleSpeed; Hud.ResetRequested -= Restart;
            Hud.SpawnRequested -= SpawnSpecies;
            Hud.SoundRequested -= ToggleSound;
        }
    }
}
