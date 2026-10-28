using System.Collections.Generic;
using UnityEngine;

namespace ZooWorld
{
    /// <summary>Per-species pools; active population is unlimited, retained inactive instances are bounded.</summary>
    public sealed class AnimalPool
    {
        private const int RetainedPerSpecies = 64;
        private readonly Transform parent;
        private readonly Dictionary<AnimalDefinition, Stack<AnimalAgent>> available = new Dictionary<AnimalDefinition, Stack<AnimalAgent>>();
        private readonly HashSet<AnimalAgent> active = new HashSet<AnimalAgent>();
        private readonly List<AnimalAgent> releaseBuffer = new List<AnimalAgent>();
        public IReadOnlyCollection<AnimalAgent> Active => active;

        public AnimalPool(Transform parent) => this.parent = parent;

        public AnimalAgent Rent(AnimalDefinition definition, Vector3 position)
        {
            if (!available.TryGetValue(definition, out var stack)) available.Add(definition, stack = new Stack<AnimalAgent>());
            AnimalAgent animal = stack.Count > 0 ? stack.Pop() : Object.Instantiate(definition.Prefab, parent);
            animal.gameObject.SetActive(false);
            animal.transform.SetPositionAndRotation(position, Quaternion.identity);
            active.Add(animal);
            return animal;
        }

        public void Release(AnimalAgent animal)
        {
            if (!active.Remove(animal)) return;
            animal.gameObject.SetActive(false); // immediate: no second physical encounter with a dead body
            var stack = available[animal.Definition];
            if (stack.Count < RetainedPerSpecies) stack.Push(animal);
            else Object.Destroy(animal.gameObject);
        }

        public void ReleaseAll()
        {
            releaseBuffer.Clear(); releaseBuffer.AddRange(active);
            foreach (var animal in releaseBuffer) Release(animal);
            releaseBuffer.Clear();
        }
    }
}
