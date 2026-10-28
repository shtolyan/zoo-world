using UnityEngine;

namespace ZooWorld
{
    public interface IAnimalFeedback { void Hop(Vector3 position); }

    /// <summary>Bounded voices, gentle stereo placement and per-cue cooldowns keep a busy habitat quiet.</summary>
    public sealed class HabitatAudio : MonoBehaviour, IAnimalFeedback
    {
        public SoundBank Sounds;
        private readonly AudioSource[] voices = new AudioSource[8];
        private readonly float[] lastAt = { float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity };
        private readonly System.Random variation = new System.Random(73);
        public bool IsMuted { get; private set; }
        public int PlayedCueCount { get; private set; }
        public int EatCueCount { get; private set; }

        private void Awake()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false; source.spatialBlend = 0;
                voices[i] = source;
            }
        }

        public void Hop(Vector3 position) => Play(Sounds != null ? Sounds.Hop : null, 1, 0.6f, 0.12f, position);
        public void Spawn(Vector3 position) => Play(Sounds != null ? Sounds.Spawn : null, 0, 0.5f, 0.8f, position);
        public void Eat(Vector3 position)
        {
            if (Play(Sounds != null ? Sounds.Eat : null, 2, 0.6f, 0.12f, position)) EatCueCount++;
        }
        public void Click() => Play(Sounds != null ? Sounds.Click : null, 3, 0.55f, 0.06f, Vector3.zero);

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            if (IsMuted) foreach (var voice in voices) voice.Stop();
            else Click();
        }

        public void Clear()
        {
            foreach (var voice in voices) voice.Stop();
            for (int i = 0; i < lastAt.Length; i++) lastAt[i] = float.NegativeInfinity;
            PlayedCueCount = EatCueCount = 0;
        }

        private bool Play(AudioClip clip, int cue, float gain, float cooldown, Vector3 position)
        {
            if (IsMuted || Sounds == null || clip == null || Time.unscaledTime - lastAt[cue] < cooldown) return false;
            AudioSource free = null;
            foreach (var voice in voices) if (!voice.isPlaying) { free = voice; break; }
            if (free == null) return false;
            lastAt[cue] = Time.unscaledTime;
            free.clip = clip; free.volume = Sounds.Volume * gain;
            free.pitch = 0.95f + (float)variation.NextDouble() * 0.1f;
            free.panStereo = Mathf.Clamp(position.x / 12, -1, 1) * 0.4f;
            free.Play(); PlayedCueCount++;
            return true;
        }
    }
}
