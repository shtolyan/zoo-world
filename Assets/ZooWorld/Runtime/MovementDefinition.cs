using UnityEngine;
using ZooWorld.Domain;

namespace ZooWorld
{
    public abstract class MovementDefinition : ScriptableObject
    {
        public abstract ILocomotion Create(IRandomSource random);
    }
}
