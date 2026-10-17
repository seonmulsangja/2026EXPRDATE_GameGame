using System;

namespace ExpiryGame.Flow
{
    // 레벨업 때 열리는 증강 선택 화면. 팀원의 증강 화면이 이 인터페이스를 구현하면 임시 구현(NullAugmentPresenter)을 대신한다.
    // 게임은 RequestSelection이 불린 시점에 이미 멈춰(AugmentSelect 상태) 있다. 선택이 끝나면 onComplete를 정확히 한 번 부른다 —
    // 그 전에는 게임이 재개되지 않는다.
    public interface IAugmentSelectionPresenter
    {
        // level: 방금 오른 레벨
        void RequestSelection(int level, Action onComplete);

        // 다시하기/타이틀 등으로 진행 중인 선택을 접어야 할 때. 이때는 onComplete를 부르지 않아도 된다
        void Cancel();
    }
}
