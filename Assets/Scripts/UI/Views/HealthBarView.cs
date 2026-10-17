using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 좌상단: 체력바 + "72 / 100" + 선택한 음식을 먹었을 때의 회복(또는 피해) 구간 미리보기.
    // 회복량은 FoodItem.CurrentHeal(현재 신선도가 이미 반영된 값)을 그대로 읽는다. 여기서 다시 계산하지 않는다.
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private Health health;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private RectTransform fill;
        [SerializeField] private RectTransform ghost;
        [SerializeField] private Image ghostImage;
        [SerializeField] private TMP_Text hpText;
        [SerializeField] private TMP_Text previewText;

        private FoodItem watched;
        private int shownCurrent = -1;
        private int shownMax = -1;
        private int shownPreview = int.MinValue;

        private void OnEnable()
        {
            health.HealthChanged += OnHealthChanged;
            ((IFoodInventory)inventory).Changed += OnInventoryChanged;
            Watch(((IFoodInventory)inventory).Selected);
            Refresh();
        }

        // 다른 컴포넌트의 Awake(초기값 설정)가 끝난 뒤 한 번 더 그린다
        private void Start()
        {
            Refresh();
        }

        private void OnDisable()
        {
            health.HealthChanged -= OnHealthChanged;
            ((IFoodInventory)inventory).Changed -= OnInventoryChanged;
            Watch(null);
        }

        private void OnHealthChanged(float current, float max)
        {
            Refresh();
        }

        private void OnInventoryChanged()
        {
            Watch(((IFoodInventory)inventory).Selected);
            Refresh();
        }

        // 선택 음식이 썩으면 회복량이 바뀌므로 그때만 다시 그린다
        private void Watch(FoodItem item)
        {
            if (watched != null) watched.Rotted -= OnRotted;
            watched = item;
            if (watched != null) watched.Rotted += OnRotted;
        }

        private void OnRotted(FoodItem item)
        {
            Refresh();
        }

        private void Refresh()
        {
            var max = health.Max;
            var current = health.Current;
            SetAnchors(fill, 0f, current / max);

            var currentInt = Mathf.RoundToInt(current);
            var maxInt = Mathf.RoundToInt(max);
            if (currentInt != shownCurrent || maxInt != shownMax)
            {
                shownCurrent = currentInt;
                shownMax = maxInt;
                hpText.text = string.Format(theme.hpFormat, currentInt, maxInt);
            }

            RefreshPreview(current, max);
        }

        private void RefreshPreview(float current, float max)
        {
            if (watched == null)
            {
                ghost.gameObject.SetActive(false);
                previewText.gameObject.SetActive(false);
                shownPreview = int.MinValue;
                return;
            }

            // 실제로 변하는 양 (가득 찬 체력 이상은 회복되지 않는다)
            var heal = watched.CurrentHeal;
            var delta = heal >= 0f ? Mathf.Min(heal, max - current) : -Mathf.Min(-heal, current);
            var deltaInt = Mathf.RoundToInt(delta);

            var from = Mathf.Min(current, current + delta) / max;
            var to = Mathf.Max(current, current + delta) / max;
            SetAnchors(ghost, from, to);
            ghostImage.color = delta >= 0f ? theme.ghostHeal : theme.ghostDamage;
            ghost.gameObject.SetActive(Mathf.Abs(delta) > 0.01f);

            // 실제로 회복될 때만 "+N 예상"을 보인다 (회복 0, 체력 가득, 피해는 숨김. 피해는 붉은 구간으로 표시)
            var showText = deltaInt > 0;
            previewText.gameObject.SetActive(showText);
            if (!showText)
            {
                shownPreview = int.MinValue;
                return;
            }
            if (deltaInt == shownPreview) return;
            shownPreview = deltaInt;
            previewText.color = theme.healText;
            previewText.text = string.Format(theme.previewFormat, $"+{deltaInt}");
        }

        private static void SetAnchors(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(Mathf.Clamp01(from), 0f);
            rect.anchorMax = new Vector2(Mathf.Clamp01(to), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
