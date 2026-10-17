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

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            if (theme == null) return;
            var text = GetComponent<TMP_Text>();
            var style = theme.StyleFor(role);
            if (theme.font != null) text.font = theme.font;
            text.fontSize = Mathf.Round(style.size * theme.TextScale);
            if (!keepColor) text.color = style.color;
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
