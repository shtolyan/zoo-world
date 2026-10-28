using UnityEngine;

namespace ZooWorld.Tests
{
    public sealed class LifeInitializationProbe : MonoBehaviour
    {
        public int EnableCount { get; private set; }
        public bool SawUninitializedLife { get; private set; }

        private void OnEnable()
        {
            EnableCount++;
            var animal = GetComponent<AnimalAgent>();
            SawUninitializedLife |= animal.Life == null || animal.Definition == null || animal.Body == null;
        }
    }
}
