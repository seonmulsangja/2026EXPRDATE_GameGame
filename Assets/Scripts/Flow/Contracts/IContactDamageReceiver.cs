namespace ExpiryGame.Flow
{
    // 적의 접촉 공격을 받는 쪽. 무적 시간처럼 "맞을 수 있는지"를 정하는 규칙은 받는 쪽이 갖는다.
    public interface IContactDamageReceiver
    {
        // 피해를 실제로 입혔으면 true. 무적 중이거나 이미 죽었으면 false (공격은 헛친 것으로 본다)
        bool TryHit(float damage);
    }
}
