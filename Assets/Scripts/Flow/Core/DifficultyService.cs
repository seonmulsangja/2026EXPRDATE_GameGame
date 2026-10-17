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
        public float SpawnMultiplier => Mathf.Max(profile.minSpawnMultiplier, Evaluate(profile.spawn, profile.nightSpawn));
        public float HealthMultiplier => Evaluate(profile.health, profile.nightHealth);
        public float SpeedMultiplier => Evaluate(profile.speed, profile.nightSpeed);
        public float DamageMultiplier => Evaluate(profile.damage, profile.nightDamage);

        private void Awake()
        {
            if (profile != null && profile.EnsureCurves())
                Debug.LogWarning("[ShelfLife] DifficultyProfile의 곡선이 비어 있어 기본값으로 채웠습니다 (비면 배율이 0이 되어 적이 안 나옵니다).", profile);
        }

        private float Evaluate(AnimationCurve curve, float nightMultiplier)
        {
            var value = Mathf.Max(0f, curve.Evaluate(DayPosition));
            return Clock.Phase == DayPhase.Night ? value * nightMultiplier : value;
        }
    }
}
