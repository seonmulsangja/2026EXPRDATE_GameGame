using UnityEngine;

namespace ExpiryGame.Combat
{
    // 카메라 위치 오프셋 흔들림. 방향 성분(스윙 방향 쪽 킥)과 랜덤 성분을 섞는다.
    // 매 LateUpdate마다 지난 오프셋을 빼고 새 오프셋을 더하므로, 카메라 추적 스크립트와 공존한다.
    public class CameraShake : MonoBehaviour, ICameraShaker
    {
        [Min(1f)] [SerializeField] private float frequency = 38f;
        [Range(0f, 1f)] [SerializeField] private float directionalBias = 0.7f;

        private Vector2 direction;
        private float amplitude;
        private float duration;
        private float elapsed;
        private Vector3 lastOffset;

        public void Shake(Vector2 shakeDirection, float shakeAmplitude, float seconds)
        {
            if (shakeAmplitude <= 0f || seconds <= 0f) return;

            // 진행 중인 흔들림보다 약하면 무시
            var remaining = elapsed >= duration ? 0f : amplitude * (1f - elapsed / duration);
            if (shakeAmplitude < remaining) return;

            direction = shakeDirection.sqrMagnitude > 0f ? shakeDirection.normalized : Random.insideUnitCircle.normalized;
            amplitude = shakeAmplitude;
            duration = seconds;
            elapsed = 0f;
        }

        private void LateUpdate()
        {
            transform.position -= lastOffset;
            lastOffset = Vector3.zero;

            if (elapsed >= duration) return;
            elapsed += Time.unscaledDeltaTime;

            var t = Mathf.Clamp01(elapsed / duration);
            var envelope = (1f - t) * (1f - t) * amplitude;
            var wave = Mathf.Cos(elapsed * frequency);
            var noise = Random.insideUnitCircle;
            var offset = direction * (wave * directionalBias) + noise * (1f - directionalBias);
            lastOffset = (Vector3)(offset * envelope);
            transform.position += lastOffset;
        }

        private void OnDisable()
        {
            transform.position -= lastOffset;
            lastOffset = Vector3.zero;
        }
    }
}
