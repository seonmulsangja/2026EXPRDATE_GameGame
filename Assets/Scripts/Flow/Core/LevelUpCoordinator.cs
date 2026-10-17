using System;
using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 레벨업을 줄 세워 하나씩 증강 선택으로 연결한다.
    //  - OnLevelUp마다 큐에 쌓는다 (한 번에 여러 레벨이 올라도 레벨마다 선택 한 번)
    //  - Playing일 때만 꺼내 AugmentSelect 상태로 바꾸고 presenter에 선택을 요청한다
    //  - presenter가 완료를 알려야만 Playing으로 돌아오고, 큐에 남은 게 있으면 Playing을 거치지 않고 곧바로 다음 선택을 연다
    //  - 일시정지/게임오버 등 다른 상태에서 온 레벨업은 Playing이 될 때까지 큐에서 기다린다 (게임오버/클리어면 버린다)
    public sealed class LevelUpCoordinator : IDisposable
    {
        private readonly IGameStateMachine machine;
        private readonly IExperienceSource experience;
        private readonly IAugmentSelectionPresenter presenter;
        private readonly Queue<int> pending = new Queue<int>();
        private int requestId; // 선택 요청마다 올라간다. 오래된/중복 완료 콜백을 걸러내는 표
        private bool selecting;

        public LevelUpCoordinator(IGameStateMachine machine, IExperienceSource experience, IAugmentSelectionPresenter presenter)
        {
            this.machine = machine;
            this.experience = experience;
            this.presenter = presenter;
            experience.OnLevelUp += OnLevelUp;
            machine.StateChanged += OnStateChanged;
        }

        public int PendingCount => pending.Count;
        public bool IsSelecting => selecting;

        public void Dispose()
        {
            experience.OnLevelUp -= OnLevelUp;
            machine.StateChanged -= OnStateChanged;
        }

        // 다시하기: 큐와 진행 중인 선택을 비운다. 늦게 도착한 완료 콜백은 요청 번호가 달라 무시된다
        public void Reset()
        {
            requestId++;
            pending.Clear();
            var wasSelecting = selecting;
            selecting = false;
            if (wasSelecting) presenter.Cancel();
            if (machine.Current == GameState.AugmentSelect) machine.EndAugmentSelect();
        }

        private void OnLevelUp(int level)
        {
            pending.Enqueue(level);
            TryOpenNext();
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.GameOver || to == GameState.Clear)
            {
                pending.Clear();
                // 선택 중에 게임이 끝났으면(플레이어 사망) 열려 있는 선택 화면을 접는다
                if (selecting)
                {
                    requestId++;
                    selecting = false;
                    presenter.Cancel();
                }
                return;
            }
            if (to == GameState.Playing) TryOpenNext();
        }

        private void TryOpenNext()
        {
            if (selecting || pending.Count == 0) return;
            if (machine.Current != GameState.Playing) return;

            selecting = true;
            machine.BeginAugmentSelect();
            OpenNext();
        }

        private void OpenNext()
        {
            var level = pending.Dequeue();
            var myId = ++requestId;
            presenter.RequestSelection(level, () => Complete(myId));
        }

        private void Complete(int completedId)
        {
            if (completedId != requestId || !selecting) return;
            // 큐에 남은 레벨업이 있으면 Playing으로 돌아가지 않고 AugmentSelect를 유지한 채 다음 선택을 연다 (중간에 재개되지 않는다)
            if (pending.Count > 0 && machine.Current == GameState.AugmentSelect)
            {
                OpenNext();
                return;
            }
            selecting = false;
            machine.EndAugmentSelect();
            TryOpenNext();
        }
    }
}
