using System;
using System.Collections.Generic;

namespace ZooWorld.Domain
{
    public readonly struct PopulationSnapshot
    {
        public int LivingPrey { get; }
        public int LivingPredators { get; }
        public int DeadPrey { get; }
        public int DeadPredators { get; }
        public int TotalSpawned { get; }
        public int Living => LivingPrey + LivingPredators;
        public PopulationSnapshot(int prey, int predators, int deadPrey, int deadPredators, int spawned)
        { LivingPrey = prey; LivingPredators = predators; DeadPrey = deadPrey; DeadPredators = deadPredators; TotalSpawned = spawned; }
    }

    /// <summary>Owns lives and death accounting. No Unity, scene lookup, static state or species switches.</summary>
    public sealed class PopulationModel
    {
        private readonly FoodChain foodChain;
        private readonly Dictionary<long, AnimalLife> living = new Dictionary<long, AnimalLife>();
        private long nextId;
        public PopulationSnapshot Snapshot { get; private set; }
        public event Action<PopulationSnapshot> Changed;

        public PopulationModel(FoodChain foodChain) => this.foodChain = foodChain ?? throw new ArgumentNullException(nameof(foodChain));

        public AnimalLife Register(Diet diet)
        {
            if (diet != Diet.Prey && diet != Diet.Predator) throw new ArgumentOutOfRangeException(nameof(diet));
            var life = new AnimalLife(++nextId, diet);
            living.Add(life.Id, life);
            var s = Snapshot;
            Snapshot = new PopulationSnapshot(s.LivingPrey + (diet == Diet.Prey ? 1 : 0),
                s.LivingPredators + (diet == Diet.Predator ? 1 : 0), s.DeadPrey, s.DeadPredators, s.TotalSpawned + 1);
            Changed?.Invoke(Snapshot);
            return life;
        }

        public bool TryEncounter(AnimalLife first, AnimalLife second, out Predation predation)
        {
            predation = default;
            if (!Owns(first) || !Owns(second) || !foodChain.TryResolve(first, second, out predation)) return false;
            // End the life BEFORE events or Unity deactivation: both collision callbacks may arrive this step.
            predation.Victim.End();
            living.Remove(predation.Victim.Id);
            var s = Snapshot;
            bool prey = predation.Victim.Diet == Diet.Prey;
            Snapshot = new PopulationSnapshot(s.LivingPrey - (prey ? 1 : 0), s.LivingPredators - (prey ? 0 : 1),
                s.DeadPrey + (prey ? 1 : 0), s.DeadPredators + (prey ? 0 : 1), s.TotalSpawned);
            Changed?.Invoke(Snapshot);
            return true;
        }

        public void Reset()
        {
            foreach (var life in living.Values) life.End();
            living.Clear();
            // IDs stay monotonic, so callbacks from a previous run cannot match a new pooled life.
            Snapshot = default;
            Changed?.Invoke(Snapshot);
        }

        private bool Owns(AnimalLife life) => life != null && living.TryGetValue(life.Id, out var current) && ReferenceEquals(current, life);
    }
}
