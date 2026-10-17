using System;

namespace ExpiryGame.Flow
{
    // 상태 → 화면 표시. 상태가 바뀌면 해당 View만 보이게 하고, 결과 화면에는 통계와 점수를 채운다.
    // View의 버튼은 GameFlowController의 동작으로 연결한다.
    public class FlowScreens : IDisposable
    {
        private readonly IGameStateMachine machine;
        private readonly StatsTracker stats;
        private readonly ScoreRules scoreRules;
        private readonly TitleView title;
        private readonly PauseView pause;
        private readonly ResultView gameOver;
        private readonly ResultView clear;
        private readonly GameFlowController controller;

        public FlowScreens(IGameStateMachine machine, GameFlowController controller, StatsTracker stats, ScoreRules scoreRules,
            TitleView title, PauseView pause, ResultView gameOver, ResultView clear)
        {
            this.machine = machine;
            this.controller = controller;
            this.stats = stats;
            this.scoreRules = scoreRules;
            this.title = title;
            this.pause = pause;
            this.gameOver = gameOver;
            this.clear = clear;

            title.StartClicked += controller.StartGame;
            title.QuitClicked += controller.Quit;
            pause.ResumeClicked += controller.Resume;
            pause.TitleClicked += controller.ToTitle;
            pause.QuitClicked += controller.Quit;
            gameOver.RetryClicked += controller.Retry;
            gameOver.TitleClicked += controller.ToTitle;
            clear.RetryClicked += controller.Retry;
            clear.TitleClicked += controller.ToTitle;

            machine.StateChanged += OnStateChanged;
        }

        public void Dispose()
        {
            machine.StateChanged -= OnStateChanged;
            if (title != null)
            {
                title.StartClicked -= controller.StartGame;
                title.QuitClicked -= controller.Quit;
            }
            if (pause != null)
            {
                pause.ResumeClicked -= controller.Resume;
                pause.TitleClicked -= controller.ToTitle;
                pause.QuitClicked -= controller.Quit;
            }
            if (gameOver != null)
            {
                gameOver.RetryClicked -= controller.Retry;
                gameOver.TitleClicked -= controller.ToTitle;
            }
            if (clear != null)
            {
                clear.RetryClicked -= controller.Retry;
                clear.TitleClicked -= controller.ToTitle;
            }
        }

        public void Apply(GameState state)
        {
            SetVisible(title, state == GameState.Title);
            SetVisible(pause, state == GameState.Paused);
            SetVisible(gameOver, state == GameState.GameOver);
            SetVisible(clear, state == GameState.Clear);

            if (state != GameState.GameOver && state != GameState.Clear) return;

            stats.Freeze();
            var snapshot = stats.Snapshot();
            var cleared = state == GameState.Clear;
            var view = cleared ? clear : gameOver;
            view.SetResult(snapshot, ScoreCalculator.Calculate(snapshot, scoreRules, cleared));
        }

        private void OnStateChanged(GameState from, GameState to) => Apply(to);

        private static void SetVisible(ScreenView view, bool visible)
        {
            if (visible) view.Show();
            else view.Hide();
        }
    }
}
