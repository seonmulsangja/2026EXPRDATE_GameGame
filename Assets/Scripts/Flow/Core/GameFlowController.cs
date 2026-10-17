using System;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 상태 전환 "규칙"만 담당한다: 플레이어 사망 → GameOver, 목표 일수 달성 → Clear, 일시정지 토글.
    // 화면 표시는 FlowScreens, 시간/입력은 GameplayGate가 상태 변화를 보고 따로 처리한다.
    public class GameFlowController : IDisposable
    {
        private readonly IGameStateMachine machine;
        private readonly SurvivalGoal goal;
        private readonly IGameClock clock;
        private readonly Health playerHealth;

        public GameFlowController(IGameStateMachine machine, SurvivalGoal goal, IGameClock clock, Health playerHealth)
        {
            this.machine = machine;
            this.goal = goal;
            this.clock = clock;
            this.playerHealth = playerHealth;

            playerHealth.Died += OnPlayerDied;
            clock.DayStarted += OnDayStarted;
        }

        public void Dispose()
        {
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            clock.DayStarted -= OnDayStarted;
        }

        // 다시하기/타이틀로 가기 직전 (씬을 다시 불러오기 전에 경험치/레벨업 큐 같은 한 판짜리 상태를 비운다)
        public event Action RunEnding;

        public void StartGame() => machine.StartGame();

        public void TogglePause()
        {
            if (machine.Current == GameState.Playing) machine.Pause();
            else if (machine.Current == GameState.Paused) machine.Resume();
        }

        public void Resume() => machine.Resume();
        public void Retry()
        {
            RunEnding?.Invoke();
            FlowLaunch.Restart();
        }

        public void ToTitle()
        {
            RunEnding?.Invoke();
            FlowLaunch.ToTitle();
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnPlayerDied() => machine.Fail();

        private void OnDayStarted(int day)
        {
            if (goal.IsCleared(day)) machine.Win();
        }
    }
}
