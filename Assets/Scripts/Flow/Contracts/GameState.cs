namespace ExpiryGame.Flow
{
    public enum GameState
    {
        Title,
        Playing,
        Paused,
        GameOver,
        Clear,
        // 레벨업 증강 선택 중: 게임은 멈춰 있고, 선택이 끝나야 Playing으로 돌아간다
        AugmentSelect
    }
}
