using System;

namespace ExpiryGame
{
    // FoodWeapon이 "공격 연출/판정"을 위임하는 계약. 스윙 모션처럼 시간이 걸리는 공격을 붙일 때 구현한다.
    public interface IAttackDriver
    {
        bool IsBusy { get; }
        float CooldownSeconds { get; }

        // onCommitted(hitCount): 타격 구간이 끝나 내구도를 소모해도 되는 시점에 한 번 호출
        void Begin(FoodItem item, Action<int> onCommitted);
    }
}
