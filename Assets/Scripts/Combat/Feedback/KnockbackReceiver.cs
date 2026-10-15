using UnityEngine;

namespace ExpiryGame.Combat
{
    // 밀려나는 쪽에 붙이는 컴포넌트 (적/플레이어 공통). 속도를 지수 감쇠시키며 위치에 더한다.
    // 적 AI가 매 프레임 velocity를 덮어써도 영향받지 않도록 MovePosition/위치 가산 방식을 쓴다.
    public class KnockbackReceiver : MonoBehaviour, IKnockbackable
    {
        [Tooltip("1이면 그대로, 0.5면 절반만 밀림, 0이면 면역 (무거운 적)")]
        [Range(0f, 2f)] [SerializeField] private float multiplier = 1f;
        [Tooltip("클수록 빨리 멈춘다. 총 이동거리 = 초기속도 / damping")]
        [Min(1f)] [SerializeField] private float damping = 12f;
        [Min(0f)] [SerializeField] private float stopSpeed = 0.3f;

        private Rigidbody2D body;
        private Vector2 velocity;

        public bool IsKnockedBack => velocity.sqrMagnitude > stopSpeed * stopSpeed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public void ApplyKnockback(Vector2 direction, float distance)
        {
            if (direction.sqrMagnitude <= 0f) return;
            velocity += direction.normalized * (distance * multiplier * damping);
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            Step(Time.fixedDeltaTime, true);
        }

        private void Update()
        {
            if (body != null) return;
            Step(Time.deltaTime, false);
        }

        private void Step(float dt, bool physics)
        {
            if (velocity == Vector2.zero) return;

            var delta = velocity * dt;
            if (physics) body.MovePosition(body.position + delta);
            else transform.position += (Vector3)delta;

            velocity *= Mathf.Exp(-damping * dt);
            if (velocity.sqrMagnitude < 0.0001f) velocity = Vector2.zero;
        }
    }
}
