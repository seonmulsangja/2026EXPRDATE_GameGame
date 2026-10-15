using UnityEngine;

namespace ExpiryGame.Combat
{
    // 밀려날 수 있는 대상의 계약. 적 AI는 KnockbackReceiver를 붙이거나 직접 구현한다.
    public interface IKnockbackable
    {
        // true인 동안 AI는 자체 이동을 멈추는 것이 좋다 (AI 이동이 넉백을 덮어쓰지 않도록)
        bool IsKnockedBack { get; }

        // direction: 밀려날 방향(정규화), distance: 대략 밀려나는 총 거리(월드 유닛)
        void ApplyKnockback(Vector2 direction, float distance);
    }
}
