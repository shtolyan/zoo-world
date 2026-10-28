using UnityEngine;

namespace ZooWorld
{
    [CreateAssetMenu(menuName = "Zoo World/Sound Bank")]
    public sealed class SoundBank : ScriptableObject
    {
        public AudioClip Spawn, Hop, Eat, Click;
        [Range(0, 1)] public float Volume = 0.65f;
    }
}
