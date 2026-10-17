namespace ExpiryGame.Flow
{
    // 증강이 바꿀 수 있는 플레이어 능력치. 새 능력치는 여기에 키를 추가하고 PlayerStatApplier에 반영 줄 하나를 더한다.
    public enum StatKey
    {
        // 공격 한 번에 더해지는 고정 피해 (FoodWeapon)
        AttackPower,
        // 이동 속도 (PlayerMover)
        MoveSpeed,
        // 공격 쿨다운에서 빼는 초 (FoodWeapon). 클수록 빨리 휘두른다
        AttackCooldownReduction,
    }

    public enum StatOp
    {
        // 기본값에 더한다
        Add,
        // (기본값 + 더한 값)에 곱한다. 0.1이면 +10%
        Multiply,
    }
}
