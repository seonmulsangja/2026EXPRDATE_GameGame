using UnityEngine;

namespace ExpiryGame.UI
{
    // 하단 중앙 음식 슬롯 바. 인벤토리 변경/무기 사용/시계 틱을 구독해 슬롯을 갱신한다.
    public class FoodSlotBarView : MonoBehaviour
    {
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private FoodWeapon weapon;
        [SerializeField] private GameClock clock;
        [SerializeField] private FoodSlotView[] slots = new FoodSlotView[0];

        public int SlotCount => slots.Length;

        public RectTransform SlotRect(int index)
        {
            return slots[Mathf.Clamp(index, 0, slots.Length - 1)].Rect;
        }

        private void Awake()
        {
            for (var i = 0; i < slots.Length; i++) slots[i].SetNumber(i + 1);
        }

        private void OnEnable()
        {
            ((IFoodInventory)inventory).Changed += Rebuild;
            weapon.WeaponUsed += OnWeaponUsed;
            ((IGameClock)clock).Ticked += OnTicked;
            Rebuild();
        }

        private void OnDisable()
        {
            ((IFoodInventory)inventory).Changed -= Rebuild;
            weapon.WeaponUsed -= OnWeaponUsed;
            ((IGameClock)clock).Ticked -= OnTicked;
        }

        private void Rebuild()
        {
            var foods = (IFoodInventory)inventory;
            for (var i = 0; i < slots.Length; i++)
            {
                var item = i < foods.Items.Count ? foods.Items[i] : null;
                slots[i].Show(item, item != null && item == foods.Selected);
            }
        }

        private void OnWeaponUsed(FoodItem item, int hitCount)
        {
            foreach (var slot in slots) slot.RefreshUses();
        }

        private void OnTicked(float _)
        {
            foreach (var slot in slots) slot.RefreshFreshness();
        }
    }
}
