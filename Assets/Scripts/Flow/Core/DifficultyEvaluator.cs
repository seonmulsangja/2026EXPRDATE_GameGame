using UnityEngine;

namespace ExpiryGame.Flow
{
    // 프로필 곡선 + 낮밤 + 디버그 임시 값으로 지금의 난이도 값을 계산한다. 시계는 모르고 SetTime으로만 받는다 (그래서 테스트가 쉽다).
    public sealed class DifficultyEvaluator : IDifficultyService
    {
        private readonly DifficultyProfile profile;
        private readonly DifficultyTuning tuning;
        private readonly EnemySpeedPolicy speedPolicy;
        private float dayPosition;
        private bool night;

        public DifficultyEvaluator(DifficultyProfile profile, DifficultyTuning tuning)
        {
            this.profile = profile;
            this.tuning = tuning;
            speedPolicy = new EnemySpeedPolicy(this);
        }

        public void SetTime(float day, bool isNight)
        {
            dayPosition = Mathf.Max(0f, day);
            night = isNight;
        }

        public float DayPosition => dayPosition;
        // 부동소수 오차로 새벽 직후가 전날로 읽히지 않게 살짝 더해 내린다
        public int DayNumber => Mathf.FloorToInt(dayPosition + 0.0001f) + 1;
        private int DayIndex => DayNumber - 1;

        public float SpawnMultiplier => Mathf.Max(profile.minSpawnMultiplier, Smooth(profile.spawn, profile.nightSpawn)) * tuning.SpawnScale;
        public float HealthMultiplier => Smooth(profile.health, profile.nightHealth);
        public float DamageMultiplier => Smooth(profile.damage, profile.nightDamage) * tuning.DamageScale;
        public float SpeedMultiplier => tuning.SpeedScale;

        public float EnemySpeedRatio => Mathf.Max(0f, profile.speedRatio.Evaluate(DayIndex)) * (night ? profile.nightSpeed : 1f);
        public float NormalSpeedCapRatio => Mathf.Max(0f, profile.speedCapRatio.Evaluate(DayIndex));
        public int FastEnemySlots => DayNumber >= profile.fastEnemyUnlockDay ? profile.fastEnemySlots : 0;
        public float FastEnemySpeedRatio => profile.fastSpeedRatio;

        public int MaxAlive
        {
            get
            {
                if (tuning.MaxAliveOverride >= 0) return tuning.MaxAliveOverride;
                var value = Mathf.Max(0f, profile.maxAlive.Evaluate(DayIndex)) * (night ? profile.nightMaxAlive : 1f);
                return Mathf.RoundToInt(value);
            }
        }

        public float ContactInterval => Mathf.Max(0.1f, profile.contactInterval.Evaluate(DayIndex));
        public float DetectRadius => Mathf.Max(1f, profile.detectRadius.Evaluate(DayIndex));
        public float PlayerInvulnerableSeconds => tuning.InvulnerableOverride >= 0f ? tuning.InvulnerableOverride : profile.playerInvulnerableSeconds;

        public float SpawnGraceSeconds => profile.spawnGraceSeconds;
        public float SingleEnemySeconds => profile.singleEnemySeconds;

        public IEnemySpeedPolicy SpeedPolicy => speedPolicy;

        private float Smooth(AnimationCurve curve, float nightMultiplier)
        {
            var value = Mathf.Max(0f, curve.Evaluate(dayPosition));
            return night ? value * nightMultiplier : value;
        }
    }
}
