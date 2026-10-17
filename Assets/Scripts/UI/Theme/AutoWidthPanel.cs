using TMPro;
using UnityEngine;

namespace ExpiryGame.UI
{
    // 여러 줄(행)이 각자 위치에 놓인 패널(음식 상세 등)의 폭을 가장 긴 행에 맞춰 늘린다.
    // 기본 폭이 최소이고, UITheme.detailMaxWidth까지 pixelScale 배수로만 늘어난다. 그 이상은 행의 글자가 줄어든다(ThemedText 맞춤).
    public class AutoWidthPanel : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text[] rows = new TMP_Text[0];

        private float baseWidth;
        private bool captured;

        public void Setup(UITheme source, RectTransform panelRect, TMP_Text[] textRows)
        {
            theme = source;
            panel = panelRect;
            rows = textRows;
            captured = false;
        }

        public void Refresh()
        {
            if (theme == null || panel == null) return;
            if (!captured)
            {
                baseWidth = panel.sizeDelta.x;
                captured = true;
            }

            var scale = Mathf.Max(1, theme.pixelScale);
            var rightPadding = theme.panelPaddingX * scale;
            var needed = baseWidth;

            foreach (var row in rows)
            {
                if (row == null) continue;
                var themed = row.GetComponent<ThemedText>();
                if (themed != null) themed.Apply();   // 비활성 패널의 글자는 Awake 전이라 크기/폰트가 아직 적용 전일 수 있다
                var left = row.rectTransform.anchoredPosition.x;
                needed = Mathf.Max(needed, left + Mathf.Ceil(row.GetPreferredValues(row.text).x) + rightPadding);
            }

            var max = Mathf.Max(baseWidth, theme.detailMaxWidth * scale);
            var width = UITextFit.SnapUp(Mathf.Min(needed, max), scale);
            panel.sizeDelta = new Vector2(width, panel.sizeDelta.y);

            foreach (var row in rows)
            {
                if (row == null) continue;
                var rect = row.rectTransform;
                rect.sizeDelta = new Vector2(Mathf.Max(1f, width - rect.anchoredPosition.x - rightPadding), rect.sizeDelta.y);
            }
        }
    }
}
