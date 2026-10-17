using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불: 반경 안에서 일정 간격마다 조금씩 체력 회복 (수치는 LandmarkData).
    [RequireComponent(typeof(LandmarkZone))]
    public class CampfireHealEffect : MonoBehaviour, ILandmarkPart
    {
        private LandmarkZone zone;
        private CampfireFuel fuel;
        private LandmarkContext context;
        private float timer;

        private void Awake()
        {
            zone = GetComponent<LandmarkZone>();
        }

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
            fuel = GetComponent<CampfireFuel>();
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
            // 연료가 줄면 회복도 느려지고, 꺼지면 멈춘다 (연료가 없는 모닥불은 항상 1배)
            var strength = fuel != null && fuel.HasProfile ? fuel.Strength(fuel.Profile.healAtEmpty) : 1f;
            if (strength <= 0f) return;
            context.PlayerHealth.Heal(context.Data.healPerSecond * tick * strength);
        }
    }
}
