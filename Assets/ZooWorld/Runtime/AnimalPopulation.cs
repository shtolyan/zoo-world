using System;
using System.Collections.Generic;
using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    /// <summary>Runtime animal lifecycle. Owns spawning, encounters and pool returns; knows no UI or input.</summary>
    public sealed class AnimalPopulation : IAnimalCollisionSink, IDisposable
    {
        private readonly AnimalPool pool;
        private readonly HabitatBounds bounds;
        private readonly IRandomSource movementRandom;
        private readonly IAnimalFeedback feedback;
        private readonly List<AnimalAgent> consumed = new List<AnimalAgent>();

        public PopulationModel Model { get; }
        public IReadOnlyCollection<AnimalAgent> Active => pool.Active;
        public event Action<AnimalAgent> Ate;

        public AnimalPopulation(Transform parent, HabitatBounds bounds, IRandomSource movementRandom, IAnimalFeedback feedback)
        {
            this.bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
            this.movementRandom = movementRandom ?? throw new ArgumentNullException(nameof(movementRandom));
            this.feedback = feedback;
            pool = new AnimalPool(parent);
            Model = new PopulationModel(new FoodChain());
        }

        public AnimalAgent Spawn(AnimalDefinition definition, Vector3 position, Vector3 direction)
        {
            if (definition == null || !definition.IsValid)
                throw new ArgumentException("A valid species is required.", nameof(definition));
            if (!Finite(position) || !Finite(direction))
                throw new ArgumentException("Spawn position and direction must be finite.");

            ILocomotion movement = definition.Movement.Create(movementRandom);
            if (movement == null) throw new InvalidOperationException("Movement factory returned no strategy.");

            AnimalAgent animal = null;
            Model.Register(definition.Diet, life =>
            {
                try
                {
                    animal = pool.Rent(definition, position);
                    animal.Initialize(life, definition, movement, movementRandom, this, bounds, direction, feedback);
                    animal.ActivateAt(position);
                }
                catch
                {
                    if (animal != null) pool.Discard(animal);
                    throw;
                }
            });
            return animal;
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);

        public void Collide(AnimalAgent first, AnimalAgent second, bool firstIsHead, bool secondIsHead)
        {
            if (first == null || second == null || !first.IsAlive || !second.IsAlive) return;
            if (!Model.TryResolve(first.Life, second.Life, out var predation)) return;

            AnimalAgent eater = first.Life == predation.Eater ? first : second;
            AnimalAgent victim = eater == first ? second : first;
            bool eaterHeadTouched = eater == first ? firstIsHead : secondIsHead;
            if (!eaterHeadTouched || !eater.CanBite) return;
            if (!Model.TryEncounter(first.Life, second.Life, out _)) return;

            eater.BeginBite();
            victim.BeginConsumption();
            consumed.Add(victim);
            Ate?.Invoke(eater);
        }

        public void ReleaseConsumed()
        {
            for (int i = consumed.Count - 1; i >= 0; i--)
            {
                if (!consumed[i].ConsumptionFinished) continue;
                pool.Release(consumed[i]);
                consumed.RemoveAt(i);
            }
        }

        public void Reset()
        {
            consumed.Clear();
            pool.ReleaseAll();
            Model.Reset();
        }

        public void Dispose()
        {
            consumed.Clear();
            Model.Reset();
            pool.Dispose();
            Ate = null;
        }
    }
}
