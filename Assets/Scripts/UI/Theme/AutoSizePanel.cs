using TMPro;
using UnityEngine;

namespace ExpiryGame.UI
{
    // 텍스트 크기에 맞춰 늘어나는 패널 (예: "F  굽기 불가 (이미 강화됨)" 안내 박스).
    // 패딩/최소·최대 폭은 UITheme에서만 조절하고, 최대 폭을 넘으면 줄바꿈하며 높이도 함께 늘어난다.
    // 크기는 pixelScale(도트 1px의 캔버스 px) 배수로만 늘어나 9-slice가 뭉개지지 않는다.
    public class AutoSizePanel : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text label;

        private ThemedText themed;
        private string shown;

        public RectTransform Panel => panel;

        public void Setup(UITheme source, RectTransform panelRect, TMP_Text text)
        {
            theme = source;
            panel = panelRect;
            label = text;
            shown = null;
        }

        public void SetText(string content)
        {
            content = content ?? string.Empty;
            if (content == shown && label.text == content) return;
            shown = content;
            label.text = content;
            Refresh();
        }

        // 현재 텍스트 기준으로 패널/라벨 크기를 다시 맞춘다
        public Vector2 Refresh()
        {
            if (theme == null || panel == null || label == null) return Vector2.zero;
            PrepareLabel();

            var scale = Mathf.Max(1, theme.pixelScale);
            var padding = new Vector2(theme.panelPaddingX, theme.panelPaddingY) * scale;
            var min = new Vector2(theme.panelMinWidth, theme.panelMinHeight) * scale;
            var max = new Vector2(theme.panelMaxWidth * scale, 0f);

            // 최대 폭 안쪽에서 줄바꿈한 결과의 글자 영역 크기
            var textArea = label.GetPreferredValues(label.text, max.x - padding.x * 2f, 0f);
            textArea = new Vector2(Mathf.Ceil(textArea.x), Mathf.Ceil(textArea.y));
            var size = UITextFit.PanelSize(textArea, padding, min, max, scale,
                Mathf.Approximately(panel.pivot.x, 0.5f), Mathf.Approximately(panel.pivot.y, 0.5f));

            panel.sizeDelta = size;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size - padding * 2f;
            return size;
        }

        private void PrepareLabel()
        {
            if (themed == null) themed = label.GetComponent<ThemedText>();
            if (themed != null)
            {
                themed.SetShrink(false);   // 박스가 글자에 맞춰 변하므로 글자를 줄일 필요가 없다
                themed.Apply();
            }
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.horizontalAlignment = HorizontalAlignmentOptions.Center;
            label.verticalAlignment = theme.panelTextAlign;
            label.lineSpacing = theme.lineSpacing;
        }
    }
}
