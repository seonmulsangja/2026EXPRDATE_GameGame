using System;

namespace ExpiryGame.Flow
{
    // 게임 진행 상태 하나만 관리한다. 시간/입력/화면은 StateChanged를 구독한 쪽이 처리.
    public class GameStateMachine : IGameStateMachine
    {
        public GameState Current { get; private set; }

        public event Action<GameState, GameState> StateChanged;

        public GameStateMachine(GameState initial)
        {
            Current = initial;
        }

        public bool StartGame() => Move(GameState.Title, GameState.Playing);
        public bool Pause() => Move(GameState.Playing, GameState.Paused);
        public bool Resume() => Move(GameState.Paused, GameState.Playing);
        // 증강 선택 중에 죽은 경우도 게임 오버로 끝낸다 (선택을 마치기를 기다렸다가 죽은 채로 재개되는 일이 없도록)
        public bool Fail() => Move(GameState.Playing, GameState.GameOver) || Move(GameState.AugmentSelect, GameState.GameOver);
        public bool Win() => Move(GameState.Playing, GameState.Clear);
        public bool BeginAugmentSelect() => Move(GameState.Playing, GameState.AugmentSelect);
        public bool EndAugmentSelect() => Move(GameState.AugmentSelect, GameState.Playing);

        private bool Move(GameState from, GameState to)
        {
            if (Current != from) return false;
            Current = to;
            StateChanged?.Invoke(from, to);
            return true;
        }
    }
}
