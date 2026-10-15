using System;
using UnityEngine;

namespace ExpiryGame.Drop
{
    // 테스트용 적. IDamageable + IDropSource만 구현한 최소 예시 (팀원 적 코드가 직접 구현하는 경우의 참고).
    // 죽으면 Died 발행 → 숨김 → respawnSeconds 뒤 체력 회복하고 다시 등장.
    public class DropTestDummy : MonoBehaviour, IDamageable, IDropSource
    {
        [Min(1f)] [SerializeField] private float maxHealth = 40f;
        [SerializeField] private DropTable table;
        [Range(0f, 1f)] [SerializeField] private float threat01 = 0.5f;
        [Min(0.1f)] [SerializeField] private float respawnSeconds = 2f;
        [SerializeField] private SpriteRenderer[] renderers;

        private float current;
        private float respawnAt;
        private bool dead;

        public DropTable Table => table;
        public float Threat01 => threat01;
        public Vector3 DropPosition => transform.position;

        public event Action<IDropSource> Died;

        private void Awake()
        {
            current = maxHealth;
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void OnEnable()
        {
            DropSourceRegistry.Register(this);
        }

        private void OnDisable()
        {
            DropSourceRegistry.Unregister(this);
        }

        public void TakeDamage(float amount)
        {
            if (dead || amount <= 0f) return;
            current -= amount;
            if (current > 0f) return;

            dead = true;
            respawnAt = Time.time + respawnSeconds;
            SetVisible(false);
            Died?.Invoke(this);
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
