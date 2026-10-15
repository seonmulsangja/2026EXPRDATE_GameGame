using System;
using UnityEngine;

namespace ExpiryGame
{
    public class GameClock : MonoBehaviour, IGameClock
    {
        [SerializeField] private DayCycleConfig config;

        private float elapsedInDay;

        public float DayLengthSeconds => config.dayLengthSeconds;
        public float TimeOfDay01 => elapsedInDay / config.dayLengthSeconds;
        public int DayCount { get; private set; } = 1;
        public DayPhase Phase { get; private set; }

        public event Action<float> Ticked;
        public event Action<DayPhase> PhaseChanged;
        public event Action<int> DayStarted;

        private void Awake()
        {
            elapsedInDay = config.startTimeNormalized * config.dayLengthSeconds;
            Phase = EvaluatePhase();
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public float SecondsToDays(float seconds)
        {
            return seconds / config.dayLengthSeconds;
        }

        private void Advance(float deltaSeconds)
        {
            elapsedInDay += deltaSeconds;
            if (elapsedInDay >= config.dayLengthSeconds)
            {
                elapsedInDay %= config.dayLengthSeconds;
                DayCount++;
                DayStarted?.Invoke(DayCount);
            }

            var phase = EvaluatePhase();
            if (phase != Phase)
            {
                Phase = phase;
                PhaseChanged?.Invoke(Phase);
            }

            Ticked?.Invoke(deltaSeconds);
        }

        private DayPhase EvaluatePhase()
        {
            return TimeOfDay01 < config.nightStartNormalized ? DayPhase.Day : DayPhase.Night;
        }
    }
}
