using System;

namespace ExpiryGame.Flow
{
    // 읽는 쪽(HUD/통계)이 보는 경험치 상태
    public interface IExperienceSource
    {
        // 1부터 시작
        int Level { get; }
        // 이번 레벨에서 모은 경험치와 다음 레벨까지 필요한 경험치
        int Current { get; }
        int Required { get; }

        // 레벨이 하나 오를 때마다 한 번씩, 오른 순서대로 발행 (한 번에 여러 레벨이 오르면 여러 번)
        event Action<int> OnLevelUp;
        // 경험치가 바뀔 때마다 (현재, 필요량). 레벨업 직후에는 새 레벨 기준 값
        event Action<int, int> OnExperienceChanged;
    }

    public interface IExperienceSink
    {
        void AddExperience(int amount);
    }

    // 레벨별 필요 경험치 표. 레벨 N에서 N+1로 오르는 데 필요한 양
    public interface IXpTable
    {
        int RequiredFor(int level);
    }

    // 죽였을 때 경험치를 주는 적 (IEnemyActor와 함께 구현한다). 팀원 적 코드는 어댑터가 이걸 구현한다
    public interface IExperienceReward
    {
        int ExperienceReward { get; }
    }
}
