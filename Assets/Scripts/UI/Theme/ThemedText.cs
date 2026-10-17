using TMPro;
using UnityEngine;

namespace ExpiryGame.UI
{
    // TMP 텍스트에 UITheme의 폰트/크기/기본 색을 입힌다. 폰트를 바꿀 때 개별 텍스트를 손대지 않게 하는 지점.
    [RequireComponent(typeof(TMP_Text))]
    public class ThemedText : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private TextRole role = TextRole.Body;
        [Tooltip("켜면 역할 기본 색 대신 인스펙터/코드에서 정한 색을 유지한다")]
        [SerializeField] private bool keepColor;
        [Tooltip("고정 크기 박스에서 글자가 넘치면 줄여서 맞춘다 (박스가 글자에 맞춰 변하는 AutoSizePanel 라벨은 끈다)")]
        [SerializeField] private bool shrinkToFit = true;

        public UITheme Theme => theme;

        private void Awake()
        {
            Apply();
        }

        public void SetShrink(bool enabled)
        {
            shrinkToFit = enabled;
        }

        public void Apply()
        {
            if (theme == null) return;
            var text = GetComponent<TMP_Text>();
            var style = theme.StyleFor(role);
            if (theme.font != null) text.font = theme.font;
            text.fontSize = UITextFit.SnapFontSize(Mathf.Round(style.size * theme.TextScale), theme.fontSnapUnit);
            text.lineSpacing = theme.lineSpacing;
            if (!keepColor) text.color = style.color;
            if (shrinkToFit) UITextFit.ConfigureShrink(text, theme.minFitRatio);
            else text.enableAutoSizing = false;
        }

        public void Setup(UITheme source, TextRole textRole, bool preserveColor)
        {
            theme = source;
            role = textRole;
            keepColor = preserveColor;
            Apply();
        }
    }
}
