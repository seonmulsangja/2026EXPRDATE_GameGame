using System;
using TMPro;
using UnityEngine;
using ExpiryGame.UI;

namespace ExpiryGame.Flow
{
    // 시작 직후 hintSeconds 동안 조작 힌트를 보여준다. 첫 실행에만 (PlayerPrefs로 기억).
    public class ControlsHintView : MonoBehaviour
    {
        private const float FadeOutSeconds = 1f;

        private FlowSettings settings;
        private CanvasGroup group;
        private float remaining;

        public void Build(UITheme theme, FlowSettings flowSettings)
        {
            settings = flowSettings;
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var panel = FlowUi.Box("Panel", transform, new Vector2(0f, -330f), new Vector2(900f, 60f + settings.hintLines.Length * 48f));
            var background = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            var count = settings.hintLines.Length;
            for (var i = 0; i < count; i++)
            {
                var y = (count - 1) * 24f - i * 48f;
                FlowUi.Text(theme, panel, settings.hintLines[i], 34, Color.white, new Vector2(0f, y), new Vector2(860f, 46f));
            }
        }

        // 이미 본 적이 있으면 아무것도 하지 않는다
        public void BeginIfFirstRun()
        {
            if (ReadSeen()) return;
            WriteSeen();
            remaining = settings.hintSeconds;
        }

        private void Update()
        {
            if (remaining <= 0f)
            {
                group.alpha = 0f;
                return;
            }

            remaining -= Time.deltaTime;
            group.alpha = Mathf.Clamp01(remaining / FadeOutSeconds);
        }

        private bool ReadSeen()
        {
            try { return PlayerPrefs.GetInt(settings.hintPrefsKey, 0) == 1; }
            catch (Exception) { return true; }
        }

        private void WriteSeen()
        {
            try
            {
                PlayerPrefs.SetInt(settings.hintPrefsKey, 1);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }
    }
}
