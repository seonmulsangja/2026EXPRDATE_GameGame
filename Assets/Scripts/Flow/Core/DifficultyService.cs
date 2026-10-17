using UnityEngine;

namespace ExpiryGame.Flow
{
    // 씬에 놓는 난이도 서비스. 시계(GameClock)에서 일차/낮밤만 읽어 DifficultyEvaluator에 넘기고, 계산은 거기서 한다.
    // 곡선은 DifficultyProfile, 디버그 임시 값은 Tuning.
    public class DifficultyService : MonoBehaviour, IDifficultyService
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private DifficultyProfile profile;

        private DifficultyTuning tuning;
        private DifficultyEvaluator evaluator;

        private IGameClock Clock => clock;

        private DifficultyEvaluator Now
        {
            get
            {
                Ensure();
                evaluator.SetTime(Mathf.Max(0, Clock.DayCount - 1) + Clock.TimeOfDay01, Clock.Phase == DayPhase.Night);
                return evaluator;
            }
        }

        public DifficultyProfile Profile => profile;

        public DifficultyTuning Tuning
        {
            get
            {
                Ensure();
                return tuning;
            }
        }

        public float DayPosition => Now.DayPosition;
        public int DayNumber => Now.DayNumber;
        public float SpawnMultiplier => Now.SpawnMultiplier;
        public float HealthMultiplier => Now.HealthMultiplier;
        public float DamageMultiplier => Now.DamageMultiplier;
        public float SpeedMultiplier => Now.SpeedMultiplier;
        public float EnemySpeedRatio => Now.EnemySpeedRatio;
        public float NormalSpeedCapRatio => Now.NormalSpeedCapRatio;
        public int FastEnemySlots => Now.FastEnemySlots;
        public float FastEnemySpeedRatio => Now.FastEnemySpeedRatio;
        public int MaxAlive => Now.MaxAlive;
        public float ContactInterval => Now.ContactInterval;
        public float DetectRadius => Now.DetectRadius;
        public float PlayerInvulnerableSeconds => Now.PlayerInvulnerableSeconds;
        public float SpawnGraceSeconds => Now.SpawnGraceSeconds;
        public float SingleEnemySeconds => Now.SingleEnemySeconds;

        // 정책은 평가기의 현재 시각을 읽으므로 Now로 시각을 맞춘 뒤 돌려준다
        public IEnemySpeedPolicy SpeedPolicy => Now.SpeedPolicy;

        // 디버그 임시 값을 프로필에 굽는다 (에디터에서만 에셋에 저장된다)
        public void BakeTuning()
        {
            Tuning.BakeInto(profile, Mathf.Max(0, Now.DayNumber - 1));
        }

        private void Awake()
        {
            if (profile != null && profile.EnsureCurves())
                Debug.LogWarning("[ShelfLife] DifficultyProfile의 곡선이 비어 있어 기본값으로 채웠습니다 (비면 배율이 0이 되어 적이 안 나옵니다).", profile);
            Ensure();
        }

        private void Ensure()
        {
            if (evaluator != null) return;
            tuning = new DifficultyTuning();
            evaluator = new DifficultyEvaluator(profile, tuning);
        }
    }
}
