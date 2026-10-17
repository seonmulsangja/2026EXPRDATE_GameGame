using System;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 능력치 최종값을 실제 컴포넌트에 쓴다. 능력치가 바뀔 때만 쓰고, 기본값은 만들 때 컴포넌트에서 읽어 둔다.
    //  - 이동 속도 → PlayerMover.MoveSpeed
    //  - 공격력/쿨다운 감소 → FoodWeapon.SetAttackStats (쿨다운은 "빼는 초")
    // 새 능력치는 Apply()에 줄 하나를 더하면 된다.
    public sealed class PlayerStatApplier : IDisposable
    {
        public const float MaxCooldownReduction = 10f;

        private readonly PlayerStatSheet sheet;
        private readonly PlayerMover mover;
        private readonly FoodWeapon weapon;

        public PlayerStatApplier(PlayerStatSheet sheet, PlayerMover mover, FoodWeapon weapon)
        {
            this.sheet = sheet;
            this.mover = mover;
            this.weapon = weapon;

            if (mover != null) sheet.SetBase(StatKey.MoveSpeed, mover.MoveSpeed);
            sheet.Changed += Apply;
            Apply();
        }

        public void Dispose()
        {
            sheet.Changed -= Apply;
        }

        private void Apply()
        {
            if (mover != null) mover.MoveSpeed = sheet.Get(StatKey.MoveSpeed);
            if (weapon != null)
            {
                var power = Mathf.RoundToInt(sheet.Get(StatKey.AttackPower));
                var reduction = Mathf.Clamp(sheet.Get(StatKey.AttackCooldownReduction), 0f, MaxCooldownReduction);
                weapon.SetAttackStats(power, reduction);
            }
        }
    }
}
