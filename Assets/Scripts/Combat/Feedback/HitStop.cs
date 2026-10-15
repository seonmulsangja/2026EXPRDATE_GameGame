using UnityEngine;

namespace ExpiryGame.Combat
{
    // 짧은 시간 정지. 실시간(unscaled)으로 복구 시점을 잰다.
    public class HitStop : MonoBehaviour, IHitStop
    {
        [Range(0f, 1f)] [SerializeField] private float stopTimeScale = 0f;

        private float restoreScale = 1f;
        private float endRealtime;
        private bool active;

        public void Play(float seconds)
        {
            if (seconds <= 0f) return;
            var end = Time.unscaledTime + seconds;
            if (active)
            {
                endRealtime = Mathf.Max(endRealtime, end);
                return;
            }

            // 이미 다른 곳(일시정지 등)이 시간을 멈춰 뒀다면 건드리지 않는다
            if (Time.timeScale <= stopTimeScale) return;
            restoreScale = Time.timeScale;
            endRealtime = end;
            active = true;
            Time.timeScale = stopTimeScale;
        }

        private void Update()
        {
            if (active && Time.unscaledTime >= endRealtime) Restore();
        }

        private void OnDisable()
        {
            if (active) Restore();
        }

        private void Restore()
        {
            active = false;
            Time.timeScale = restoreScale;
        }
    }
}
