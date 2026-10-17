namespace ExpiryGame.Flow
{
    // 적 한 마리의 이동 속도가 플레이어 속도의 몇 배여야 하는지 정한다.
    public interface IEnemySpeedPolicy
    {
        // kindFactor: 종류별 상대 속도 (기준 종류 = 1). claimant: 빠른 적 슬롯을 점유할 주체(보통 적 컴포넌트)
        // 반환값은 플레이어 속도 대비 비율이고 전역 속도 배율(IDifficultyService.SpeedMultiplier)은 포함하지 않는다.
        float Resolve(float kindFactor, object claimant);

        // 죽거나 사라질 때 슬롯을 돌려준다
        void Release(object claimant);
    }
}
