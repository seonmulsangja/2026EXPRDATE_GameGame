using System;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 테스트용 타깃. IDamageable만 구현한 최소 예시 (팀원 적 코드가 따라 할 형태).
    // invulnerable이면 허수아비(안 죽음), 아니면 죽은 뒤 respawnSeconds 후 체력 회복.
    public class TrainingTarget : MonoBehaviour, IDamageable
    {
        [Min(1f)] [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool invulnerable;
        [Min(0.1f)] [SerializeField] private float respawnSeconds = 1.5f;
        [SerializeField] private bool logDamage = true;
        [Tooltip("이 대상의 렌더러들 (죽었을 때 숨김용). 비어 있으면 자식 전부")]
        [SerializeField] private SpriteRenderer[] renderers;

        private float current;
        private float respawnAt;
        private bool dead;

        public event Action<float> DamageTaken;

        private void Awake()
        {
            current = maxHealth;
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        public void TakeDamage(float amount)
        {
            if (dead || amount <= 0f) return;
            DamageTaken?.Invoke(amount);
            if (logDamage) Debug.Log($"[{name}] 피해 {amount:0.#} (남은 {Mathf.Max(0f, current - amount):0.#}/{maxHealth:0})");
            if (invulnerable) return;

            current -= amount;
            if (current > 0f) return;
            dead = true;
            respawnAt = Time.time + respawnSeconds;
            SetVisible(false);
        }

        private void Update()
        {
            if (!dead || Time.time < respawnAt) return;
            dead = false;
            current = maxHealth;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in renderers) if (r != null) r.enabled = visible;
            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = visible;
        }
    }
}
