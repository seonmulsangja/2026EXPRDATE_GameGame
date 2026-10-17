namespace ExpiryGame.Flow
{
    // 한 판의 통계 (결과 화면/점수 계산 입력)
    public struct GameStats
    {
        public int DaysSurvived;       // 끝까지 버틴 날 수 (완료한 날)
        public int Kills;
        public int LevelReached;       // 도달한 레벨 (1부터)
        public int FoodEaten;
        public int RottenEaten;
        public float PlaySeconds;
    }

    public interface IStatsSource
    {
        GameStats Snapshot();
    }
}
