using System;

namespace ExpiryGame.Flow
{
    // 게임 진행 상태만 아는 계약. 화면/시간/입력 처리는 이 이벤트를 구독하는 쪽이 한다.
    public interface IGameStateMachine
    {
        GameState Current { get; }

        // (이전, 새) 상태
        event Action<GameState, GameState> StateChanged;

        bool StartGame();
        bool Pause();
        bool Resume();
        bool Fail();
        bool Win();

        // 레벨업 증강 선택: Playing ↔ AugmentSelect. 선택이 끝나기 전에는 Resume으로 풀 수 없다
        bool BeginAugmentSelect();
        bool EndAugmentSelect();
    }
}
