using UnityEngine;

namespace ExpiryGame.Flow
{
    // 일차 + 낮밤으로 난이도 배율을 계산한다. 곡선은 DifficultyProfile, 시각은 GameClock.
    public class DifficultyService : MonoBehaviour, IDifficultyService
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private DifficultyProfile profile;

        private IGameClock Clock => clock;

        public float DayPosition => Mathf.Max(0, Clock.DayCount - 1) + Clock.TimeOfDay01;
        public float SpawnMultiplier => Evaluate(profile.spawn, profile.nightSpawn);
        public float HealthMultiplier => Evaluate(profile.health, profile.nightHealth);
        public float SpeedMultiplier => Evaluate(profile.speed, profile.nightSpeed);
        public float DamageMultiplier => Evaluate(profile.damage, profile.nightDamage);

        private float Evaluate(AnimationCurve curve, float nightMultiplier)
        {
            var value = Mathf.Max(0f, curve.Evaluate(DayPosition));
            return Clock.Phase == DayPhase.Night ? value * nightMultiplier : value;
        }
    }
}
