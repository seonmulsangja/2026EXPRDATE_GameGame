using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불: 반경 안에서 일정 간격마다 조금씩 체력 회복 (수치는 LandmarkData).
    [RequireComponent(typeof(LandmarkZone))]
    public class CampfireHealEffect : MonoBehaviour, ILandmarkPart
    {
        private LandmarkZone zone;
        private LandmarkContext context;
        private float timer;

        private void Awake()
        {
            zone = GetComponent<LandmarkZone>();
        }

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
        }

        private void Update()
        {
            if (context == null || !zone.IsInside)
            {
                timer = 0f;
                return;
            }

            timer += Time.deltaTime;
            var tick = context.Data.healTickSeconds;
            if (timer < tick) return;
            timer -= tick;
            context.PlayerHealth.Heal(context.Data.healPerSecond * tick);
        }
    }
}
