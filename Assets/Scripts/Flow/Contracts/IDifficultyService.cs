namespace ExpiryGame.Flow
{
    // 지금 시각의 난이도 값. 적 스폰/어댑터/피격 처리가 읽는다. 곱셈 배율은 1 = 기본.
    public interface IDifficultyService
    {
        // 0 = 1일차 새벽, 1 = 2일차 새벽 ... (연속값). DifficultyProfile 곡선의 x축
        float DayPosition { get; }
        // 1 = 1일차
        int DayNumber { get; }

        // --- 배율 (디버그 슬라이더 배율까지 반영된 값) ---
        float SpawnMultiplier { get; }
        float HealthMultiplier { get; }
        float DamageMultiplier { get; }
        // 완성된 적 속도에 마지막으로 곱하는 전역 배율 (디버그용, 기본 1)
        float SpeedMultiplier { get; }

        // --- 일차별 값 ---
        // 일반 적 속도 ÷ 플레이어 속도 (밤 반영)
        float EnemySpeedRatio { get; }
        // 일반 적이 넘을 수 없는 속도 비율
        float NormalSpeedCapRatio { get; }
        // 빠른 적 슬롯 수 (허용 일차 전에는 0)와 그 속도 상한
        int FastEnemySlots { get; }
        float FastEnemySpeedRatio { get; }
        int MaxAlive { get; }
        float ContactInterval { get; }
        float DetectRadius { get; }
        float PlayerInvulnerableSeconds { get; }

        // --- 초반 스폰 ---
        float SpawnGraceSeconds { get; }
        float SingleEnemySeconds { get; }

        // 적이 속도를 정할 때 쓰는 규칙 (빠른 적 슬롯 포함)
        IEnemySpeedPolicy SpeedPolicy { get; }
    }
}
