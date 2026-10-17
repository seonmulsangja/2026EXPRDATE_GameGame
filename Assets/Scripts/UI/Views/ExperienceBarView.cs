using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 체력바 아래의 얇은 경험치 바 + "Lv.N". 값은 SetProgress로 받기만 한다 (경험치 규칙은 모른다).
    //  - 바 프레임/채움은 체력바와 같은 스프라이트(ui_bar_frame / ui_bar_fill)를 쓰고, 크기는 pixelScale 배수로만 잡는다
    //  - 채움 길이는 도트 단위로 끊어서 반 픽셀이 생기지 않는다
    //  - 레벨업 순간에는 Flash로 바가 번쩍인다 (실시간 기준이라 게임이 멈춰 있어도 보인다)
    // 저장된 씬에 없어도 Install이 체력 패널 아래에 만든다 (UISetup이 쓰는 것과 같은 코드).
    public class ExperienceBarView : MonoBehaviour
    {
        public const string RowName = "XpRow";

        // 도트 단위 크기: 프레임 높이 10(테두리 4+4 사이 속 2), 체력 패널 확장 20
        private const int FrameHeightDots = 10;
        private const int BorderDots = 4;
        private const int PanelExtraDots = 20;
        private const int FrameTopGapDots = 1;
        private const int LevelTextLeftDots = 12;

        [SerializeField] private UITheme theme;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text levelText;

        private float progress01;
        private int shownLevel = -1;
        private float flashRemaining;
        private float flashTotal;

        // healthPanel 아래에 경험치 행을 만든다. 이미 있으면 그것을 돌려준다.
        public static ExperienceBarView Install(UITheme theme, RectTransform healthPanel)
        {
            if (healthPanel == null) return null;
            var existing = healthPanel.Find(RowName);
            if (existing != null) return existing.GetComponent<ExperienceBarView>();

            var scale = theme != null ? Mathf.Max(1, theme.pixelScale) : 2;
            var oldHeight = healthPanel.sizeDelta.y;
            healthPanel.sizeDelta = new Vector2(healthPanel.sizeDelta.x, oldHeight + PanelExtraDots * scale);

            // 체력바와 왼쪽/폭을 맞춘다 (없으면 기본 위치)
            var hp = healthPanel.Find("HpBar") as RectTransform;
            var barX = hp != null ? hp.anchoredPosition.x : Mathf.Round(118f * scale / 3f);
            var barWidth = hp != null ? hp.sizeDelta.x : Mathf.Round(366f * scale / 3f);
            var barTop = -(oldHeight + FrameTopGapDots * scale);
            var height = FrameHeightDots * scale;

            var row = NewRect(RowName, healthPanel, Vector2.zero);
            row.anchorMin = Vector2.zero;
            row.anchorMax = Vector2.one;
            row.offsetMin = row.offsetMax = Vector2.zero;

            var frame = NewRect("XpBar", row, new Vector2(barX, barTop));
            frame.sizeDelta = new Vector2(barWidth, height);
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.sprite = theme != null ? theme.sprites.barFrame : null;
            frameImage.type = Image.Type.Sliced;
            frameImage.pixelsPerUnitMultiplier = 1f / scale;   // 보더 4px가 정확히 4 x pixelScale 캔버스 px가 된다
            frameImage.raycastTarget = false;

            var inner = NewRect("Inner", frame, Vector2.zero);
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.pivot = new Vector2(0.5f, 0.5f);
            inner.offsetMin = new Vector2(BorderDots * scale, BorderDots * scale);
            inner.offsetMax = new Vector2(-BorderDots * scale, -BorderDots * scale);

            var fillRect = NewRect("Fill", inner, Vector2.zero);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var image = fillRect.gameObject.AddComponent<Image>();
            image.sprite = theme != null ? theme.sprites.barFill : null;
            image.color = theme != null ? theme.xpFill : Color.cyan;
            image.raycastTarget = false;

            // 레벨 글자: 체력 아이콘 아래 왼쪽, 바와 같은 높이
            var textRect = NewRect("LevelText", row, new Vector2(LevelTextLeftDots * scale, barTop - height * 0.5f));
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.sizeDelta = new Vector2(Mathf.Max(8f * scale, barX - LevelTextLeftDots * scale - scale), height + 4 * scale);
            var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            textRect.gameObject.AddComponent<ThemedText>().Setup(theme, TextRole.Small, false);

            var view = row.gameObject.AddComponent<ExperienceBarView>();
            view.theme = theme;
            view.fill = fillRect;
            view.fillImage = image;
            view.levelText = text;
            return view;
        }

        public void SetProgress(int level, int current, int required)
        {
            progress01 = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;
            ApplyFill(progress01);

            if (level != shownLevel && levelText != null)
            {
                shownLevel = level;
                var format = theme != null ? theme.levelFormat : "Lv.{0}";
                levelText.text = string.Format(format, level);
            }
        }

        // 레벨업 순간: 바를 가득 채운 채 번쩍인 뒤 새 값으로 돌아간다
        public void Flash(float seconds)
        {
            flashTotal = Mathf.Max(0.05f, seconds);
            flashRemaining = flashTotal;
        }

        private void Update()
        {
            if (flashRemaining <= 0f) return;

            flashRemaining -= Time.unscaledDeltaTime;
            var baseColor = theme != null ? theme.xpFill : Color.cyan;
            if (flashRemaining <= 0f)
            {
                fillImage.color = baseColor;
                ApplyFill(progress01);
                return;
            }

            // 0.12초 주기의 깜빡임, 끝으로 갈수록 약해진다
            var t = 1f - flashRemaining / flashTotal;
            var blink = Mathf.FloorToInt(t * flashTotal / 0.06f) % 2 == 0 ? 1f : 0f;
            fillImage.color = Color.Lerp(baseColor, theme != null ? theme.xpFlash : Color.white, blink * (1f - t * 0.5f));
            ApplyFill(1f);
        }

        // 채움 길이를 안쪽 도트 수 단위로 끊는다. 0보다 크면 최소 1도트는 보인다
        private void ApplyFill(float value)
        {
            if (fill == null) return;
            // 저장된 씬에서도 맞도록 안쪽 폭은 매번 레이아웃에서 읽는다 (직렬화되지 않는 값에 기대지 않는다)
            var inner = fill.parent as RectTransform;
            var scale = theme != null ? Mathf.Max(1, theme.pixelScale) : 2;
            var innerDots = inner != null ? Mathf.Max(1, Mathf.RoundToInt(inner.rect.width / scale)) : 1;
            var dots = Mathf.FloorToInt(value * innerDots + 0.0001f);
            if (value > 0f && dots == 0) dots = 1;
            fill.anchorMax = new Vector2(Mathf.Clamp01((float)dots / innerDots), 1f);
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 topLeftPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftPosition;
            return rect;
        }
    }
}
