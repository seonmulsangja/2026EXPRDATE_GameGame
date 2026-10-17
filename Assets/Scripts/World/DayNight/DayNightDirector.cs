using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame.World
{
    // 시계(IGameClock)를 한 번만 읽어 DayNightProfile로 샘플을 만들고, 전역 조명을 적용한 뒤 Changed로 알린다.
    // 낮밤에 반응하는 모든 연출은 이 샘플을 구독할 뿐 시간 계산을 따로 하지 않는다.
    public class DayNightDirector : MonoBehaviour, IDayNightState
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private DayNightProfile profile;
        [SerializeField] private Light2D globalLight;

        public DayNightSample Current { get; private set; }
        public DayNightProfile Profile => profile;

        public event Action<DayNightSample> Changed;

        private void OnEnable()
        {
            DayNightState.Instance = this;
            ((IGameClock)clock).Ticked += OnTicked;
            Refresh();
        }

        private void OnDisable()
        {
            ((IGameClock)clock).Ticked -= OnTicked;
            if (ReferenceEquals(DayNightState.Instance, this)) DayNightState.Instance = null;
        }

        private void OnTicked(float _)
        {
            Refresh();
        }

        private void Refresh()
        {
            Current = profile.Evaluate(((IGameClock)clock).TimeOfDay01);
            if (globalLight != null)
            {
                globalLight.color = Current.AmbientColor;
                globalLight.intensity = Current.AmbientIntensity;
            }
            Changed?.Invoke(Current);
        }
    }
}
