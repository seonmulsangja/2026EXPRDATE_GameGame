using System;
using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 증강을 실제로 적용하는 쪽. 선택 화면 어댑터는 고른 증강을 여기에 넘기기만 한다.
    public interface IAugmentEffects
    {
        // 지금 레벨 (고른 적 없으면 0)
        int LevelOf(string id);
        // 최대 중첩에 닿지 않았으면 true
        bool CanApply(AugmentDefinition definition);
        // 한 단계 올려 적용한다. 최대 중첩이면 false
        bool Apply(AugmentDefinition definition);
        // 레벨을 한 단계 내리고 효과를 거둔다 (0이 되면 기록에서도 뺀다). 능력치 효과만 되돌린다
        bool Revoke(AugmentDefinition definition);
        event Action<AugmentDefinition, int> Applied;
    }

    // 결과 화면/디버그가 읽는 고른 증강 기록 (처음 고른 순서)
    public interface IAugmentHistory
    {
        IReadOnlyList<AugmentRecord> Records { get; }
    }

    // 팀원 어댑터처럼 증강 적용기와 대체 보상이 필요한 presenter가 구현한다. GameFlowRoot가 만든 뒤 한 번 넘겨 준다.
    public interface IAugmentPresenterConsumer
    {
        void Bind(IAugmentEffects effects, IAugmentSelectionPresenter fallback);
    }

    // 선택 화면을 여는 쪽(팀원 UI). 후보 뽑기/화면 열기/닫기만 안다. 효과 적용은 모른다.
    public interface IAugmentChooser
    {
        // 후보를 뽑는다. 뽑을 수 있는 증강이 없으면(전부 최대 중첩 등) false
        bool Prepare();
        // Prepare가 뽑은 첫 후보. 선택 없이 닫혔을 때 복구용으로 쓴다
        AugmentDefinition FirstCandidate { get; }
        // 화면을 연다. 고르면 그 증강, 선택 없이 닫히면 null로 onResult를 정확히 한 번 부른다. 열다가 실패하면 예외를 던진다
        void Open(Action<AugmentDefinition> onResult);
        // 열려 있으면 접는다 (결과 콜백은 부르지 않는다)
        void Close();
    }

    // F1 디버그 메뉴용 (에디터/Development Build). 팀원 어댑터가 구현한다
    public interface IAugmentDebug
    {
        IReadOnlyList<AugmentDefinition> AllAugments { get; }
        // 다음 선택에 뜰 후보를 강제로 지정한다 (한 번 쓰면 풀린다). 빈 목록이면 해제
        void ForceNextCandidates(IReadOnlyList<string> ids);
        IReadOnlyList<string> ForcedIds { get; }
    }
}
