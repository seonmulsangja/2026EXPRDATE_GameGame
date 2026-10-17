using UnityEngine;

namespace ExpiryGame.Combat
{
    // 밀려나는 쪽에 붙이는 컴포넌트 (적/플레이어 공통). 움직임 계산은 KnockbackMotion, 수치는 KnockbackProfile(SO).
    // 이 컴포넌트는 "계산된 변위를 물리에 적용하고, 장애물에서 멈추게 하는 것"만 한다.
    // 적 AI가 매 프레임 velocity를 덮어써도 영향받지 않도록 MovePosition/위치 가산 방식을 쓴다.
    public class KnockbackReceiver : MonoBehaviour, IKnockbackable
    {
        private const int MaxCastHits = 8;

        [Tooltip("프로필을 직접 넘기지 않는 밀림(예: 플레이어 반동)에 쓰는 수치. 비우면 Resources/ShelfLife/Knockback_Hit")]
        [SerializeField] private KnockbackProfile defaultProfile;
        [Tooltip("0~1. 1이면 밀리지 않는다. 적 쪽에서는 EnemyAdapter가 덮어쓴다")]
        [Range(0f, 1f)] [SerializeField] private float resistance;

        private readonly KnockbackMotion motion = new KnockbackMotion();
        private readonly RaycastHit2D[] hits = new RaycastHit2D[MaxCastHits];
        private ContactFilter2D filter;
        private Rigidbody2D body;
        private float skin = 0.02f;

        public bool IsKnockedBack => motion.IsActive;
        public bool IsStaggered => motion.IsStaggered;

        public float Resistance
        {
            get => resistance;
            set => resistance = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        }

        private void OnDisable()
        {
            motion.Cancel();
        }

        public void ApplyKnockback(Vector2 direction, float distance)
        {
            ApplyKnockback(direction, distance, KnockbackProfile.ResolveHit(defaultProfile));
        }

        public void ApplyKnockback(Vector2 direction, float distance, KnockbackProfile profile)
        {
            if (profile == null) profile = KnockbackProfile.ResolveHit(defaultProfile);
            skin = profile.skin;
            motion.Apply(direction, distance, resistance, profile, Time.time);
        }

        private void FixedUpdate()
        {
            if (body == null || !motion.IsActive) return;
            var delta = motion.Step(Time.fixedDeltaTime, Clip);
            if (delta != Vector2.zero) body.MovePosition(body.position + delta);
        }

        private void Update()
        {
            if (body != null || !motion.IsActive) return;
            transform.position += (Vector3)motion.Step(Time.deltaTime, null);
        }

        // 가려는 방향의 장애물/다른 적까지 갈 수 있는 거리. 멀어지는 쪽에 닿아 있는 물체는 무시한다.
        private float Clip(Vector2 direction, float wanted)
        {
            var count = body.Cast(direction, filter, hits, wanted + skin);
            var allowed = wanted;
            for (var i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.attachedRigidbody == body) continue;
                if (Vector2.Dot(hit.normal, direction) >= -0.01f) continue;
                allowed = Mathf.Min(allowed, hit.distance - skin);
            }
            return Mathf.Max(0f, allowed);
        }
    }
}
