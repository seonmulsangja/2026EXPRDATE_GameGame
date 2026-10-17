using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 우상단: 일차, 낮/밤 아이콘, 현재 구간이 끝나기까지 남은 시간, 하루 진행 바.
    public class TimeHudView : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private GameClock clock;
        [SerializeField] private Image phaseIcon;
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private RectTransform barFill;

        private int shownSecond = -1;
        private float shownProgress = -1f;

        private void OnEnable()
        {
            var source = (IGameClock)clock;
            source.Ticked += OnTicked;
            source.PhaseChanged += OnPhaseChanged;
            source.DayStarted += OnDayStarted;
            OnDayStarted(source.DayCount);
            OnPhaseChanged(source.Phase);
            OnTicked(0f);
        }

        // 다른 컴포넌트의 Awake(초기값 설정)가 끝난 뒤 한 번 더 그린다
        private void Start()
        {
            OnPhaseChanged(((IGameClock)clock).Phase);
            OnTicked(0f);
        }

        private void OnDisable()
        {
            var source = (IGameClock)clock;
            source.Ticked -= OnTicked;
            source.PhaseChanged -= OnPhaseChanged;
            source.DayStarted -= OnDayStarted;
        }

        private void OnDayStarted(int day)
        {
            dayText.text = string.Format(theme.dayFormat, day);
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            phaseIcon.sprite = phase == DayPhase.Day ? theme.sprites.sun : theme.sprites.moon;
            shownSecond = -1;
        }

        private void OnTicked(float _)
        {
            var source = (IGameClock)clock;
            var time = source.TimeOfDay01;

            if (Mathf.Abs(time - shownProgress) > 0.002f)
            {
                shownProgress = time;
                barFill.anchorMax = new Vector2(time, 1f);
            }

            var phaseEnd = source.Phase == DayPhase.Day ? clock.NightStartNormalized : 1f;
            var remaining = Mathf.Max(0f, (phaseEnd - time) * source.DayLengthSeconds);
            var second = Mathf.CeilToInt(remaining);
            if (second == shownSecond) return;
            shownSecond = second;
            var label = source.Phase == DayPhase.Day ? theme.dayLabel : theme.nightLabel;
            phaseText.text = $"{label} · {second / 60}:{second % 60:00}";
        }
    }
}
