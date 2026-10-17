using System;
using UnityEngine;

namespace ExpiryGame
{
    public class Health : MonoBehaviour, IDamageable, IHealable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public bool IsDead => Current <= 0f;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            SetCurrent(Current - amount);
            if (IsDead) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            SetCurrent(Current + amount);
        }

        private void SetCurrent(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxHealth);
            HealthChanged?.Invoke(Current, maxHealth);
        }
        
        public void IncreaseMaxHealth(float amount)
        {
            if (amount <= 0f)
                return;

            maxHealth += amount;
            Current += amount;

            HealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
