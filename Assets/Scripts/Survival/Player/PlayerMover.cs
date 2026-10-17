using UnityEngine;

namespace ExpiryGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;

        // 적이 "플레이어 속도의 몇 %"로 움직일지 정할 때 읽고, 능력치 파이프라인(PlayerStatApplier)이 쓴다
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        private Rigidbody2D body;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Reset()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            body.linearVelocity =
                Vector2.ClampMagnitude(((IPlayerInput)input).Move, 1f)
                * moveSpeed;
        }
    }
}