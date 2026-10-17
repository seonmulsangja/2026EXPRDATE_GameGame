using System;

namespace ExpiryGame.Flow
{
    // 능력치 하나를 바꾸는 수정자. source가 같은 수정자끼리는 키별로 하나만 유지한다 (같은 증강의 중첩은 값을 갱신해서 표현)
    public interface IStatModifier
    {
        string Source { get; }
        StatKey Key { get; }
        StatOp Op { get; }
        float Value { get; }
    }

    // 읽는 쪽이 보는 능력치: 기본값 + 수정자를 합친 최종값
    public interface IPlayerStats
    {
        float Get(StatKey key);
        // 수정자가 바뀔 때마다
        event Action Changed;
    }

    // 수정자를 등록/해제하는 쪽(증강 효과)이 보는 면
    public interface IStatModifierRegistry
    {
        void Set(IStatModifier modifier);
        // 같은 source의 수정자를 모두 뺀다. 뺀 게 있으면 true
        bool RemoveSource(string source);
        void Clear();
    }
}
