using System;

namespace ZooWorld.Domain
{
    public enum Diet { Prey, Predator }

    public sealed class AnimalLife
    {
        public long Id { get; }
        public Diet Diet { get; }
        public bool IsAlive { get; private set; } = true;

        internal AnimalLife(long id, Diet diet) { Id = id; Diet = diet; }
        internal void End() => IsAlive = false;
    }

    public readonly struct Predation
    {
        public AnimalLife Eater { get; }
        public AnimalLife Victim { get; }
        public Predation(AnimalLife eater, AnimalLife victim) { Eater = eater; Victim = victim; }
    }

    /// <summary>Order-independent rule. The older predator wins a predator/predator encounter.</summary>
    public sealed class FoodChain
    {
        public bool TryResolve(AnimalLife first, AnimalLife second, out Predation result)
        {
            result = default;
            if (first == null || second == null || first == second || !first.IsAlive || !second.IsAlive)
                return false;
            if (first.Diet == Diet.Prey && second.Diet == Diet.Prey) return false;

            AnimalLife eater;
            if (first.Diet != second.Diet) eater = first.Diet == Diet.Predator ? first : second;
            else eater = first.Id < second.Id ? first : second;
            result = new Predation(eater, eater == first ? second : first);
            return true;
        }
    }
}
