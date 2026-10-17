using System;
using TMPro;
using UnityEngine;
using ExpiryGame.UI;

namespace ExpiryGame.Flow
{
    // "레벨 업!" 배너. 게임이 멈춘 상태(AugmentSelect)에서 보이므로 실시간(unscaled) 기준으로 움직인다.
    // 정해진 시간이 지나면 완료 콜백을 한 번 부른다. 취소하면 콜백 없이 사라진다.
    public class LevelUpBannerView : MonoBehaviour
    {
        private const float FadeFraction = 0.2f;

        private FlowSettings settings;
        private CanvasGroup group;
        private TextMeshProUGUI title;
        private TextMeshProUGUI subtitle;
        private float total;
        private float remaining;
        private Action onDone;

        public bool IsPlaying => remaining > 0f;

        public void Build(UITheme theme, FlowSettings flowSettings)
        {
            settings = flowSettings;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            FlowUi.Fill("Dim", transform, new Color(0.02f, 0.02f, 0.05f, 0.35f), false);
            title = FlowUi.Text(theme, transform, "", 120, theme != null ? theme.accent : Color.white, new Vector2(0f, 80f), new Vector2(1400f, 170f));
            subtitle = FlowUi.Text(theme, transform, "", 56, Color.white, new Vector2(0f, -30f), new Vector2(1000f, 80f));
        }

        public void Play(int level, float seconds, Action done)
        {
            title.text = settings.levelUpTitle;
            subtitle.text = string.Format(settings.levelUpSubtitle, level);
            total = Mathf.Max(0.05f, seconds);
            remaining = total;
            onDone = done;
            group.alpha = 0f;
        }

        public void Cancel()
        {
            remaining = 0f;
            onDone = null;
            if (group != null) group.alpha = 0f;
        }

        private void Update()
        {
            if (remaining <= 0f) return;

            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                group.alpha = 0f;
                var callback = onDone;
                onDone = null;
                callback?.Invoke();
                return;
            }

            var t = 1f - remaining / total;
            group.alpha = Mathf.Min(Mathf.Clamp01(t / FadeFraction), Mathf.Clamp01((1f - t) / FadeFraction));
        }
    }
}
