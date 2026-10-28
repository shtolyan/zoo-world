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
        public Button PauseButton, SpeedButton, ResetButton;
        public Button[] SpawnButtons;
        public RectTransform ToastLayer;
        public event Action PauseRequested, SpeedRequested, ResetRequested;
        public event Action<int> SpawnRequested;

        private void Awake()
        {
            PauseButton.onClick.AddListener(() => PauseRequested?.Invoke());
            SpeedButton.onClick.AddListener(() => SpeedRequested?.Invoke());
            ResetButton.onClick.AddListener(() => ResetRequested?.Invoke());
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
            Elapsed.text = $"{(int)elapsed / 60:00}:{(int)elapsed % 60:00}";
            NextSpawn.text = paused ? "SPAWNING PAUSED" : $"NEXT ARRIVAL   {nextSpawn:0.0}s";
            PauseCaption.text = paused ? "Resume" : "Pause";
            SpeedCaption.text = fast ? "Speed  2x" : "Speed  1x";
            StateCaption.text = paused ? "II   PAUSED" : "●   LIVE HABITAT";
        }
    }
}
