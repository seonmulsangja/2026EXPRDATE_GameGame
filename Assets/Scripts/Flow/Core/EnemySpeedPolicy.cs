using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 속도 규칙: 기본은 (일반 적 비율 × 종류 보정)이고 일차별 상한을 넘지 못한다.
    // 상한을 넘으려는 "빠른 종류"는 빠른 적 슬롯(동시 N마리)을 얻은 경우에만 상한 대신 빠른 적 상한까지 허용된다.
    public sealed class EnemySpeedPolicy : IEnemySpeedPolicy
    {
        private readonly IDifficultyService difficulty;
        private readonly List<object> holders = new List<object>();

        public EnemySpeedPolicy(IDifficultyService difficulty)
        {
            this.difficulty = difficulty;
        }

        public int FastHolders => holders.Count;

        public float Resolve(float kindFactor, object claimant)
        {
            var raw = difficulty.EnemySpeedRatio * kindFactor;
            var cap = difficulty.NormalSpeedCapRatio;
            if (raw <= cap)
            {
                holders.Remove(claimant); // 더 이상 상한을 넘지 않으면 슬롯을 돌려준다
                return raw;
            }

            var slots = difficulty.FastEnemySlots;
            if (slots > 0 && (holders.Contains(claimant) || holders.Count < slots))
            {
                if (!holders.Contains(claimant)) holders.Add(claimant);
                return System.Math.Min(raw, difficulty.FastEnemySpeedRatio);
            }
            return cap;
        }

        public void Release(object claimant)
        {
            holders.Remove(claimant);
        }
    }
}
