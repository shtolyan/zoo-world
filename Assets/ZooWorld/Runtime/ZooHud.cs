using System;
using UnityEngine;
using UnityEngine.UI;
using ZooWorld.Domain;

namespace ZooWorld
{
    public sealed class ZooHud : MonoBehaviour
    {
        public Text DeadPrey, DeadPredators, Living, PopulationDetail, Elapsed, NextSpawn;
        public Text PauseCaption, SpeedCaption, StateCaption;
        public Text SoundCaption;
        public Button PauseButton, SpeedButton, ResetButton;
        public Button SoundButton;
        public Button[] SpawnButtons;
        public RectTransform ToastLayer;
        public event Action PauseRequested, SpeedRequested, ResetRequested;
        public event Action SoundRequested;
        public event Action<int> SpawnRequested;
        private int lastElapsedSecond = -1;
        private int lastArrivalTenth = -1;
        private bool? lastPaused;
        private bool? lastFast;
        private bool? lastMuted;

        private void Awake()
        {
            PauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
            SpeedButton.onClick.AddListener(() => SpeedRequested?.Invoke());
            ResetButton.onClick.AddListener(() => ResetRequested?.Invoke());
            if (SoundButton != null) SoundButton.onClick.AddListener(() => SoundRequested?.Invoke());
            for (int i = 0; i < SpawnButtons.Length; i++)
            {
                int index = i;
                SpawnButtons[i].onClick.AddListener(() => SpawnRequested?.Invoke(index));
            }
        }

        public void Present(PopulationSnapshot population)
        {
            DeadPrey.text = population.DeadPrey.ToString("00");
            DeadPredators.text = population.DeadPredators.ToString("00");
            Living.text = population.Living.ToString("00");
            PopulationDetail.text = $"{population.LivingPrey} prey    /    {population.LivingPredators} predators";
        }

        public void PresentTime(float elapsed, float nextSpawn, bool paused, bool fast)
        {
            int seconds = (int)elapsed;
            int tenths = Mathf.RoundToInt(nextSpawn * 10);
            if (lastElapsedSecond != seconds)
            {
                Elapsed.text = $"{seconds / 60:00}:{seconds % 60:00}";
                lastElapsedSecond = seconds;
            }
            if (lastPaused != paused || (!paused && lastArrivalTenth != tenths))
            {
                NextSpawn.text = paused ? "SPAWNING PAUSED" : $"NEXT ARRIVAL   {tenths / 10f:0.0}s";
                lastArrivalTenth = tenths;
            }
            if (lastPaused != paused)
            {
                PauseCaption.text = paused ? "Resume" : "Pause";
                StateCaption.text = paused ? "II   PAUSED" : "●   LIVE HABITAT";
                lastPaused = paused;
            }
            if (lastFast != fast)
            {
                SpeedCaption.text = fast ? "Speed  2x" : "Speed  1x";
                lastFast = fast;
            }
        }

        public void PresentAudio(bool muted)
        {
            if (lastMuted == muted) return;
            if (SoundCaption != null) SoundCaption.text = muted ? "Sound off [M]" : "Sound on [M]";
            lastMuted = muted;
        }
    }
}
