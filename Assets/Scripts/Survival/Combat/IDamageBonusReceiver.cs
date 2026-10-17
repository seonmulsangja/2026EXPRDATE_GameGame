namespace ExpiryGame
{
    // 공격 연출/판정을 맡은 쪽(IAttackDriver)이 "공격 한 번에 더해지는 고정 피해"를 알아야 할 때 구현한다.
    // FoodWeapon이 스윙을 시작하기 직전에 현재 값을 넘긴다 (능력치 파이프라인의 AttackPower).
    public interface IDamageBonusReceiver
    {
        void SetDamageBonus(float bonus);
    }
}
