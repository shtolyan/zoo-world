using UnityEngine;

namespace ZooWorld
{
    /// <summary>A physical body part belongs to one animal life, never a separate population entry.</summary>
    public sealed class AnimalContactRelay : MonoBehaviour
    {
        public AnimalAgent Owner;
        private void OnCollisionEnter(Collision collision)
        {
            if (Owner != null) Owner.ReportContact(Resolve(collision.collider));
        }

        public static AnimalAgent Resolve(Collider collider)
        {
            if (collider.TryGetComponent<AnimalAgent>(out var animal)) return animal;
            return collider.TryGetComponent<AnimalContactRelay>(out var part) ? part.Owner : null;
        }
    }
}
