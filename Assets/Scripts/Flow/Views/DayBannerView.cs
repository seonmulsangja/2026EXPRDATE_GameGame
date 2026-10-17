using TMPro;
using UnityEngine;
using ExpiryGame.UI;

namespace ExpiryGame.Flow
{
    // 새벽마다 "N일차"를 크게 보여주고 사라진다. 실시간(unscaled) 기준이라 timeScale과 무관.
    public class DayBannerView : MonoBehaviour
    {
        private const float FadeFraction = 0.25f;

        private FlowSettings settings;
        private UITheme theme;
        private CanvasGroup group;
        private TextMeshProUGUI title;
        private TextMeshProUGUI subtitle;
        private float remaining;

        public void Build(UITheme uiTheme, FlowSettings flowSettings)
        {
            theme = uiTheme;
            settings = flowSettings;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            title = FlowUi.Text(theme, transform, "", 130, Color.white, new Vector2(0f, 150f), new Vector2(1400f, 190f));
            subtitle = FlowUi.Text(theme, transform, "", 44, theme != null ? theme.accent : Color.white,
                new Vector2(0f, 50f), new Vector2(1000f, 70f));
        }

        public void Show(int day, bool isLastDay)
        {
            title.text = string.Format(theme != null ? theme.dayFormat : "{0}일차", day);
            subtitle.text = isLastDay ? settings.lastDayLabel : string.Empty;
            remaining = settings.dayBannerSeconds;
        }

        private void Update()
        {
            if (remaining <= 0f)
            {
                group.alpha = 0f;
                return;
            }

            remaining -= Time.unscaledDeltaTime;
            var t = 1f - Mathf.Clamp01(remaining / settings.dayBannerSeconds); // 0 → 1
            var fadeIn = Mathf.Clamp01(t / FadeFraction);
            var fadeOut = Mathf.Clamp01((1f - t) / FadeFraction);
            group.alpha = Mathf.Min(fadeIn, fadeOut);
        }
    }
}
