namespace ExpiryGame.Flow
{
    // 지금 시각의 난이도 배율. 적 스폰/어댑터가 읽는다 (1 = 기본).
    public interface IDifficultyService
    {
        // 0 = 1일차 새벽, 1 = 2일차 새벽 ... (연속값). DifficultyProfile 곡선의 x축
        float DayPosition { get; }
        float SpawnMultiplier { get; }
        float HealthMultiplier { get; }
        float SpeedMultiplier { get; }
        float DamageMultiplier { get; }
    }
}
