using UnityEngine;

namespace ExpiryGame.Visual
{
    // 인벤토리에서 선택된 음식을 FoodVisual에 연결한다.
    public class HeldFoodVisual : MonoBehaviour
    {
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private FoodVisual visual;

        private void OnEnable()
        {
            ((IFoodInventory)inventory).Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            ((IFoodInventory)inventory).Changed -= Refresh;
        }

        private void Refresh()
        {
            var selected = ((IFoodInventory)inventory).Selected;
            if (selected == null) visual.Clear();
            else visual.Bind(selected);
        }
    }
}
