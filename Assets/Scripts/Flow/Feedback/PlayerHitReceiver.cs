using UnityEngine;

namespace ExpiryGame.Flow
{
    // 플레이어가 적의 접촉 공격을 받는 지점. 무적 시간(HitInvulnerability)을 지키고, 무적 동안 몸을 깜빡인다.
    // 적 접촉 피해만 여기를 지난다 (썩은 음식을 먹은 자해 피해는 Health로 직접 간다).
    // 씬 저장본에 없어도 GameFlowRoot가 런타임에 붙인다.
    [DisallowMultipleComponent]
    public class PlayerHitReceiver : MonoBehaviour, IContactDamageReceiver
    {
        private const float BlinkInterval = 0.08f;
        private const float BlinkAlpha = 0.35f;

        private readonly HitInvulnerability invulnerability = new HitInvulnerability();
        private Health health;
        private IDifficultyService difficulty;
        private SpriteRenderer body;
        private float baseAlpha = 1f;
        private bool blinking;

        public bool IsInvulnerable => invulnerability.IsInvulnerable(Time.time);

        public void Bind(IDifficultyService source)
        {
            difficulty = source;
        }

        private void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<SpriteRenderer>();
        }

        public bool TryHit(float damage)
        {
            if (health == null) health = GetComponent<Health>();
            if (health == null || health.IsDead || damage <= 0f) return false;

            var duration = difficulty != null ? difficulty.PlayerInvulnerableSeconds : 0.8f;
            if (!invulnerability.TryPass(Time.time, duration)) return false;

            health.TakeDamage(damage);
            if (duration > 0f && !health.IsDead) BeginBlink();
            return true;
        }

        private void BeginBlink()
        {
            if (body == null) return;
            if (!blinking) baseAlpha = body.color.a;
            blinking = true;
        }

        private void Update()
        {
            if (!blinking) return;

            if (!invulnerability.IsInvulnerable(Time.time))
            {
                SetAlpha(baseAlpha);
                blinking = false;
                return;
            }

            // 일시정지(timeScale 0) 중에는 Time.time이 멈춰 있으므로 깜빡임도 같이 멈춘다
            var phase = Mathf.FloorToInt(Time.time / BlinkInterval) % 2 == 0;
            SetAlpha(phase ? baseAlpha : baseAlpha * BlinkAlpha);
        }

        private void OnDisable()
        {
            if (blinking) SetAlpha(baseAlpha);
            blinking = false;
        }

        private void SetAlpha(float alpha)
        {
            if (body == null) return;
            var color = body.color;
            color.a = alpha;
            body.color = color;
        }
    }
}
