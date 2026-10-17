using UnityEngine;

namespace ExpiryGame.Landmark
{
    public enum EnhanceBlock
    {
        None,
        NoFood,
        AlreadyEnhanced,
        Rotten,
        Cooldown
    }

    // "지금 이 음식을 강화할 수 있나, 안 되면 왜, 되면 회복이 얼마에서 얼마로 바뀌나"의 결과
    public readonly struct EnhanceCheck
    {
        public readonly EnhanceBlock Block;
        public readonly float CooldownLeft;
        public readonly float HealBefore;
        public readonly float HealAfter;

        public EnhanceCheck(EnhanceBlock block, float cooldownLeft, float healBefore, float healAfter)
        {
            Block = block;
            CooldownLeft = cooldownLeft;
            HealBefore = healBefore;
            HealAfter = healAfter;
        }

        public bool CanEnhance => Block == EnhanceBlock.None;
    }

    // 강화 규칙 판단/적용만 한다 (씬/입력/UI를 모른다). 스탯 변환은 FoodItem이 이미 가진 값에 배율만 곱하므로 복사하지 않는다.
    public class FoodEnhancer
    {
        private readonly EnhanceProfile profile;

        public FoodEnhancer(EnhanceProfile profile)
        {
            this.profile = profile;
        }

        public EnhanceCheck Check(FoodItem item, float cooldownLeft)
        {
            if (item == null) return new EnhanceCheck(EnhanceBlock.NoFood, cooldownLeft, 0f, 0f);

            var before = item.CurrentHeal;
            var after = before * profile.healMultiplier;
            if (item.IsEnhanced) return new EnhanceCheck(EnhanceBlock.AlreadyEnhanced, cooldownLeft, before, before);
            if (item.IsRotten) return new EnhanceCheck(EnhanceBlock.Rotten, cooldownLeft, before, before);
            if (cooldownLeft > 0f) return new EnhanceCheck(EnhanceBlock.Cooldown, cooldownLeft, before, after);
            return new EnhanceCheck(EnhanceBlock.None, 0f, before, after);
        }

        // 성공하면 true. 규칙(1회/썩음)은 FoodItem.TryEnhance가 한 번 더 지킨다.
        public bool Apply(FoodItem item)
        {
            return item != null && item.TryEnhance(profile.healMultiplier, profile.damageMultiplier, profile.shelfLifeBonusRatio);
        }
    }

    // 시계를 직접 읽지 않는 쿨다운 (now를 넘겨받으므로 테스트에서 시간을 마음대로 움직일 수 있다)
    public class CooldownTimer
    {
        private float readyAt = float.NegativeInfinity;

        public float Remaining(float now)
        {
            return Mathf.Max(0f, readyAt - now);
        }

        public bool IsReady(float now)
        {
            return Remaining(now) <= 0f;
        }

        public void Start(float now, float seconds)
        {
            readyAt = now + Mathf.Max(0f, seconds);
        }
    }
}
