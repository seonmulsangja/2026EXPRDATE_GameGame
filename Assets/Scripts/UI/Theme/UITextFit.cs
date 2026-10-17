using TMPro;
using UnityEngine;

namespace ExpiryGame.UI
{
    // "글자가 박스에 맞게" 만드는 계산 모음 (씬을 모르는 순수 함수라 테스트할 수 있다).
    // 패널은 도트 정수 배율(pixelScale) 단위로만 늘려 9-slice 테두리가 뭉개지지 않게 한다.
    public static class UITextFit
    {
        // value 이상인 unit의 배수 (부동소수 오차로 한 칸 더 커지지 않게 약간 여유를 둔다)
        public static float SnapUp(float value, float unit)
        {
            if (unit <= 0f) return value;
            return Mathf.Ceil(value / unit - 0.0001f) * unit;
        }

        // 텍스트 크기 + 패딩 → 패널 크기. 최소/최대로 자르고 unit 배수로 올린다.
        // even: 가운데 피벗 패널이 반 픽셀 위치로 흐려지지 않게 2*unit 배수로 맞춘다.
        public static Vector2 PanelSize(Vector2 text, Vector2 padding, Vector2 min, Vector2 max, float unit, bool evenX, bool evenY)
        {
            var width = Mathf.Clamp(text.x + padding.x * 2f, min.x, Mathf.Max(min.x, max.x));
            var height = Mathf.Max(text.y + padding.y * 2f, min.y);
            if (max.y > 0f) height = Mathf.Min(height, Mathf.Max(min.y, max.y));
            return new Vector2(SnapUp(width, evenX ? unit * 2f : unit), SnapUp(height, evenY ? unit * 2f : unit));
        }

        // 폰트 원본 도트 크기의 정수배로 맞춘다 (0이면 그대로). 도트 폰트가 정수 배율이 아니면 번진다.
        public static float SnapFontSize(float size, int unit)
        {
            if (unit <= 0) return size;
            return Mathf.Max(unit, Mathf.Round(size / unit) * unit);
        }

        // 고정 크기 박스용: 글자가 박스를 넘으면 maxSize → maxSize*minRatio까지 줄이고, 그래도 넘으면 말줄임(…)
        public static void ConfigureShrink(TMP_Text text, float minRatio)
        {
            if (text == null) return;
            var max = text.fontSize;
            if (minRatio >= 0.999f || max <= 1f)
            {
                text.enableAutoSizing = false;
                return;
            }
            text.enableAutoSizing = true;
            text.fontSizeMax = max;
            text.fontSizeMin = Mathf.Max(1f, Mathf.Round(max * Mathf.Clamp(minRatio, 0.2f, 1f)));
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        // 박스 없는 떠 있는 라벨: 글자 크기에 맞춰 폭을 줄이고 최대 폭을 넘으면 줄이지 않고 그대로 둔다(말줄임은 ConfigureShrink가)
        public static void FitLabelWidth(TMP_Text label, float maxWidth)
        {
            var preferred = label.GetPreferredValues(label.text);
            var rect = label.rectTransform;
            var size = rect.sizeDelta;
            size.x = Mathf.Min(Mathf.Ceil(preferred.x), maxWidth);
            rect.sizeDelta = size;
        }
    }
}
