using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ZooWorld
{
    /// <summary>Inactive creation and per-species reuse. Rentals own their key even if initialization fails.</summary>
    public sealed class AnimalPool : IDisposable
    {
        private const int RetainedPerSpecies = 64;
        private readonly Transform parent;
        private readonly Transform inactiveRoot;
        private readonly Dictionary<AnimalDefinition, Stack<AnimalAgent>> available = new Dictionary<AnimalDefinition, Stack<AnimalAgent>>();
        private readonly Dictionary<AnimalAgent, AnimalDefinition> active = new Dictionary<AnimalAgent, AnimalDefinition>();
        private readonly List<AnimalAgent> releaseBuffer = new List<AnimalAgent>();
        public IReadOnlyCollection<AnimalAgent> Active => active.Keys;

        public AnimalPool(Transform parent)
        {
            this.parent = parent;
            inactiveRoot = new GameObject("Inactive animals").transform;
            inactiveRoot.SetParent(parent, false);
            inactiveRoot.gameObject.SetActive(false);
        }

        public AnimalAgent Rent(AnimalDefinition definition, Vector3 position)
        {
            if (!available.TryGetValue(definition, out var stack))
            {
                stack = new Stack<AnimalAgent>();
                available.Add(definition, stack);
            }

            // An active prefab instantiated under this root cannot run OnEnable before injection.
            AnimalAgent animal = stack.Count > 0 ? stack.Pop() : Object.Instantiate(definition.Prefab, inactiveRoot);
            animal.gameObject.SetActive(false);
            animal.transform.SetParent(parent, false);
            animal.transform.SetPositionAndRotation(position, Quaternion.identity);
            active.Add(animal, definition);
            return animal;
        }

        public void Release(AnimalAgent animal)
        {
            if (!active.TryGetValue(animal, out var definition)) return;
            active.Remove(animal);
            animal.gameObject.SetActive(false);
            animal.transform.SetParent(inactiveRoot, false);
            var stack = available[definition];
            if (stack.Count < RetainedPerSpecies) stack.Push(animal);
            else Object.Destroy(animal.gameObject);
        }

        public void Discard(AnimalAgent animal)
        {
            if (!active.Remove(animal)) return;
            animal.gameObject.SetActive(false);
            Object.Destroy(animal.gameObject);
        }

        public void ReleaseAll()
        {
            releaseBuffer.Clear();
            releaseBuffer.AddRange(active.Keys);
            foreach (var animal in releaseBuffer) Release(animal);
            releaseBuffer.Clear();
        }

        public void Dispose()
        {
            // Scene unload may have already destroyed Unity objects; managed ownership still clears.
            foreach (var animal in active.Keys)
                if (animal != null) Object.Destroy(animal.gameObject);
            active.Clear();
            available.Clear();
            releaseBuffer.Clear();
            if (inactiveRoot != null) Object.Destroy(inactiveRoot.gameObject);
        }
    }
}
