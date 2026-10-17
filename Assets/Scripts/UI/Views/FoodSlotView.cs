using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 음식 슬롯 하나: 아이콘, 남은 사용 횟수 점, 아래 신선도 바. 선택 슬롯은 ui_slot_selected로 강조.
    public class FoodSlotView : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text number;
        [SerializeField] private Image icon;
        [SerializeField] private PipRow pips;
        [SerializeField] private RectTransform freshFill;
        [SerializeField] private Image freshFillImage;

        private Image enhancedBadge;
        private FoodItem item;
        private float shownFresh = -1f;

        public RectTransform Rect => (RectTransform)transform;

        public void SetNumber(int value)
        {
            number.text = value.ToString();
        }

        public void Show(FoodItem food, bool selected)
        {
            item = food;
            frame.sprite = selected ? theme.sprites.slotSelected : theme.sprites.slot;
            EnsureBadge();
            enhancedBadge.gameObject.SetActive(food != null && food.IsEnhanced);
            icon.gameObject.SetActive(food != null);
            freshFill.parent.gameObject.SetActive(food != null);
            shownFresh = -1f;

            if (food == null)
            {
                pips.Hide();
                return;
            }

            icon.sprite = food.Data.icon != null ? food.Data.icon : food.Data.sprite;
            RefreshUses();
            RefreshFreshness();
        }

        // 슬롯 우상단 작은 배지. 슬롯을 다시 설치하지 않아도 되도록 처음 필요할 때 만든다.
        private void EnsureBadge()
        {
            if (enhancedBadge != null) return;
            enhancedBadge = EnhanceBadge.Create(Rect, theme, new Vector2(1f, 1f), new Vector2(-Rect.sizeDelta.x * 0.16f, -Rect.sizeDelta.y * 0.16f), Rect.sizeDelta.x * 0.17f);
        }

        public void RefreshUses()
        {
            if (item != null) pips.Set(item.RemainingDurability, item.Stats.MaxDurability);
        }

        // 신선도는 시계 틱마다 조금씩 변하므로, 눈에 띄는 변화(0.5%)가 있을 때만 바를 갱신한다
        public void RefreshFreshness()
        {
            if (item == null) return;
            var fresh = Mathf.Clamp01(item.ShelfLife01);
            if (Mathf.Abs(fresh - shownFresh) < 0.005f) return;
            shownFresh = fresh;
            freshFill.anchorMax = new Vector2(fresh, 1f);
            freshFillImage.color = theme.FreshnessColor(fresh);
        }
    }
}
