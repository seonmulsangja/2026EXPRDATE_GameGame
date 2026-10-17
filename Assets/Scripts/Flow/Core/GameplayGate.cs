using UnityEngine;

namespace ExpiryGame.Flow
{
    // 상태에 따라 시간과 플레이어 입력을 켜고 끈다. Playing일 때만 시간이 흐르고 입력이 들어간다.
    public class GameplayGate
    {
        private readonly PlayerInputReader input;
        private GameState state;

        public GameplayGate(PlayerInputReader input, GameState initial)
        {
            this.input = input;
            Apply(initial);
        }

        public void Apply(GameState newState)
        {
            state = newState;
            var playing = state == GameState.Playing;
            Time.timeScale = playing ? 1f : 0f;
            if (input != null) input.enabled = playing;
        }

        // 히트스톱이 복구 시점에 timeScale을 1로 되돌려 일시정지를 풀어 버릴 수 있어서 매 프레임 확인한다.
        // 플레이 중의 timeScale 0은 히트스톱이 정당하게 만든 것이라 건드리지 않는다.
        public void Enforce()
        {
            if (state != GameState.Playing && Time.timeScale != 0f) Time.timeScale = 0f;
        }
    }
}
