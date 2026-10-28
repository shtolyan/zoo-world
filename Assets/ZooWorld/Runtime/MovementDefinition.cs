using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    public abstract class MovementDefinition : ScriptableObject
    {
        // Custom strategies can reject their serialized settings before any lives are registered.
        public virtual bool IsValid => true;
        protected static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public abstract ILocomotion Create(IRandomSource random);
    }
}
