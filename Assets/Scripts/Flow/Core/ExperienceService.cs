using System;

namespace ExpiryGame.Flow
{
    // 경험치 누적과 레벨 계산만 한다 (순수 클래스). 레벨업 후의 처리(증강 선택, 일시정지)는 구독자가 한다.
    // 한 번에 여러 레벨이 오르면 OnLevelUp을 오른 순서대로 여러 번 발행하고, 남은 경험치는 새 레벨로 이월한다.
    public sealed class ExperienceService : IExperienceSource, IExperienceSink
    {
        // 필요량이 0 이하로 새어 나오는 표가 있어도 무한 루프에 빠지지 않게 하는 안전장치
        private const int MaxLevelUpsPerAdd = 100;

        private readonly IXpTable table;

        public int Level { get; private set; } = 1;
        public int Current { get; private set; }
        public int Required => Math.Max(1, table.RequiredFor(Level));
        public int TotalGained { get; private set; }

        public event Action<int> OnLevelUp;
        public event Action<int, int> OnExperienceChanged;

        public ExperienceService(IXpTable table)
        {
            this.table = table;
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;

            TotalGained += amount;
            Current += amount;

            var guard = 0;
            while (Current >= Required && guard++ < MaxLevelUpsPerAdd)
            {
                Current -= Required;
                Level++;
                OnLevelUp?.Invoke(Level);
            }

            OnExperienceChanged?.Invoke(Current, Required);
        }

        // 다시하기: 레벨 1, 경험치 0으로 되돌린다
        public void Reset()
        {
            Level = 1;
            Current = 0;
            TotalGained = 0;
            OnExperienceChanged?.Invoke(Current, Required);
        }
    }
}
