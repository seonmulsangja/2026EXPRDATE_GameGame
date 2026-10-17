using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 먹는 중에만 플레이어 머리 위에 뜨는 작고 얇은 진행 바 + (체력 가득/썩은 음식일 때) 안내 문구.
    // 하단 HUD 패널과 겹치지 않도록 화면 고정 UI가 아니라 월드 좌표 캔버스를 쓴다. 상태는 FoodConsumer 이벤트만 구독한다.
    // 씬에 직접 붙이지 않아도 FoodConsumer가 있으면 씬 로드 직후 자동으로 붙는다 (Bootstrap).
    public class EatProgressView : MonoBehaviour
    {
        private const float PixelsPerUnit = 100f;

        [SerializeField] private FoodConsumer consumer;

        private GameObject root;
        private RectTransform fill;
        private TMP_Text notice;
        private float headHeight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            foreach (var found in FindObjectsByType<FoodConsumer>(FindObjectsSortMode.None))
                if (found.GetComponent<EatProgressView>() == null)
                    found.gameObject.AddComponent<EatProgressView>();
        }

        private void Awake()
        {
            if (consumer == null) consumer = GetComponent<FoodConsumer>();
        }

        private void OnEnable()
        {
            consumer.EatStarted += OnStarted;
            consumer.EatCanceled += Hide;
            consumer.FoodEaten += OnEaten;
        }

        private void OnDisable()
        {
            consumer.EatStarted -= OnStarted;
            consumer.EatCanceled -= Hide;
            consumer.FoodEaten -= OnEaten;
            Hide();
        }

        private void OnDestroy()
        {
            if (root != null) Destroy(root);
        }

        private void OnEaten(FoodItem item)
        {
            Hide();
        }

        private void OnStarted(FoodItem item)
        {
            if (root == null) Build(consumer.Settings);
            headHeight = HeadHeight(consumer.Settings);

            var text = EatAdvisor.NoticeFor(consumer.CurrentWarning, item, consumer.Settings);
            notice.text = text;
            notice.gameObject.SetActive(text.Length > 0);
            notice.color = item.IsRotten ? new Color(0.95f, 0.3f, 0.3f) : new Color(0.95f, 0.78f, 0.25f);

            SetProgress(0f);
            Follow();
            root.SetActive(true);
        }

        private void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void LateUpdate()
        {
            if (root == null || !root.activeSelf) return;
            SetProgress(consumer.Progress01);
            Follow();
        }

        private void Follow()
        {
            // 플레이어가 좌우 반전(scale.x < 0)되어도 바는 뒤집히지 않게 별도 루트로 따라다닌다
            root.transform.position = transform.position + new Vector3(0f, headHeight, 0f);
        }

        private void SetProgress(float value)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        // 플레이어 콜라이더 윗면 + 여백. 콜라이더가 없으면 설정의 기본 높이
        private float HeadHeight(EatSettings settings)
        {
            var col = GetComponentInChildren<Collider2D>();
            if (col == null) return settings.fallbackHeight;
            return col.bounds.max.y - transform.position.y + settings.headGap;
        }

        private void Build(EatSettings settings)
        {
            root = new GameObject("EatProgressBar");
            root.SetActive(false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;

            var size = settings.barSize * PixelsPerUnit;
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one / PixelsPerUnit;

            var back = NewImage("Back", rect, settings.barBackColor);
            Stretch(back.rectTransform);
            fill = NewImage("Fill", back.rectTransform, settings.barFillColor).rectTransform;
            Stretch(fill);

            var textObject = new GameObject("Notice", typeof(RectTransform));
            textObject.transform.SetParent(rect, false);
            notice = textObject.AddComponent<TextMeshProUGUI>();
            notice.raycastTarget = false;
            notice.alignment = TextAlignmentOptions.Bottom;
            notice.enableWordWrapping = false;
            notice.fontSize = settings.noticeFontSize * PixelsPerUnit;
            notice.font = FindThemeFont();
            var textRect = notice.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 1f);
            textRect.anchorMax = new Vector2(0.5f, 1f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.anchoredPosition = new Vector2(0f, size.y * 0.8f);
            textRect.sizeDelta = new Vector2(size.x * 6f, notice.fontSize * 1.4f);
        }

        // 한글이 나오는 HUD와 같은 폰트 (UITheme.font 한 곳). 이미 로드된 테마가 없으면 TMP 기본 폰트
        private static TMP_FontAsset FindThemeFont()
        {
            foreach (var theme in Resources.FindObjectsOfTypeAll<UITheme>())
                if (theme.font != null) return theme.font;
            return TMP_Settings.defaultFontAsset;
        }

        private static Image NewImage(string name, RectTransform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
