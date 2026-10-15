using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 무한 맵에서는 카메라가 따라가야 하므로 둔 최소 추적. CameraShake가 매 LateUpdate에 자기 오프셋을 빼고 더하므로
    // 절대 위치를 덮어쓰지 않고 목표 쪽으로 보간해서 흔들림과 공존한다.
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("클수록 빨리 따라감 (지수 보간)")]
        [Min(0.1f)] [SerializeField] private float followSpeed = 12f;

        private void LateUpdate()
        {
            if (target == null) return;
            var position = transform.position;
            var desired = new Vector3(target.position.x, target.position.y, position.z);
            var t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(position, desired, t);
        }
    }
}
