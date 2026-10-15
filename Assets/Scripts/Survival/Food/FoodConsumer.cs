using System;
using UnityEngine;

namespace ExpiryGame
{
    public class FoodConsumer : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private Health health;

        public event Action<FoodItem> FoodEaten;

        private void OnEnable()
        {
            ((IPlayerInput)input).EatPerformed += Eat;
        }

        private void OnDisable()
        {
            ((IPlayerInput)input).EatPerformed -= Eat;
        }

        private void Eat()
        {
            var item = ((IFoodInventory)inventory).Selected;
            if (item == null) return;

            var amount = item.CurrentHeal;
            if (amount >= 0f) ((IHealable)health).Heal(amount);
            else ((IDamageable)health).TakeDamage(-amount);

            ((IFoodInventory)inventory).Remove(item);
            FoodEaten?.Invoke(item);
        }
    }
}
