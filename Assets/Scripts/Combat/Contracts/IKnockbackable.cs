using UnityEngine;

namespace ExpiryGame.Combat
{
    // 밀려날 수 있는 대상의 계약. 적 AI는 KnockbackReceiver를 붙이거나 직접 구현한다.
    public interface IKnockbackable
    {
        // true인 동안(밀리는 중 + 경직) AI는 자체 이동/접촉 공격을 멈춘다 (AI 이동이 넉백을 덮어쓰지 않도록)
        bool IsKnockedBack { get; }

        // 0~1. 1이면 밀리지 않는다. 적 SO/프리팹의 knockbackResistance를 어댑터가 여기에 넣는다 (없으면 0)
        float Resistance { get; set; }

        // direction: 밀려날 방향(정규화), distance: 밀리는 총 거리(타일). 수치는 대상의 기본 프로필을 따른다 (예: 플레이어 반동)
        void ApplyKnockback(Vector2 direction, float distance);

        // 프로필을 지정해서 민다 (타격 / 안전지대 밀어내기를 서로 다른 수치로 분리). 거리는 profile.maxDistance로 잘린다
        void ApplyKnockback(Vector2 direction, float distance, KnockbackProfile profile);
    }
}
