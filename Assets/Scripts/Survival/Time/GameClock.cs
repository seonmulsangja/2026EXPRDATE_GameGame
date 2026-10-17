using System;
using UnityEngine;

namespace ExpiryGame
{
    public class GameClock : MonoBehaviour, IGameClock
    {
        [SerializeField] private DayCycleConfig config;

        private float elapsedInDay;

        public float DayLengthSeconds => config.dayLengthSeconds;
        // 이 시각(0~1)부터 밤. UI가 "낮/밤이 끝나기까지 남은 시간"을 보여줄 때 쓴다.
        public float NightStartNormalized => config.nightStartNormalized;
        public float TimeOfDay01 => elapsedInDay / config.dayLengthSeconds;
        public int DayCount { get; private set; } = 1;
        public DayPhase Phase { get; private set; }
        // 게임 시간 배속 (디버그 빨리감기용). 물리/이동은 그대로, 낮밤과 유통기한 감소만 빨라진다.
        public float SpeedMultiplier { get; set; } = 1f;

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
            Advance(Time.deltaTime * SpeedMultiplier);
        }

        public float SecondsToDays(float seconds)
        {
            return seconds / config.dayLengthSeconds;
        }

        // 디버그/연출용: 하루 안의 시각(0~1)으로 즉시 이동. 페이즈 변경 이벤트와 0초 틱을 발행해 구독자가 바로 갱신되게 한다.
        public void JumpToTimeOfDay(float normalized)
        {
            elapsedInDay = Mathf.Repeat(normalized, 1f) * config.dayLengthSeconds;
            var phase = EvaluatePhase();
            if (phase != Phase)
            {
                Phase = phase;
                PhaseChanged?.Invoke(Phase);
            }
            Ticked?.Invoke(0f);
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
