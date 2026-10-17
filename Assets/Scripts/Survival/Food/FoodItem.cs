using System;
using UnityEngine;

namespace ExpiryGame
{
    public class FoodItem
    {
        public FoodData Data { get; }
        public FoodStats Stats { get; }
        public float RemainingShelfLife { get; private set; }
        public int RemainingDurability { get; private set; }

        public bool IsRotten => RemainingShelfLife <= 0f;
        public bool IsBroken => RemainingDurability <= 0;
        public float ShelfLife01 => RemainingShelfLife / Data.shelfLifeSeconds;
        public float CurrentHeal => IsRotten ? Stats.RottenHeal : Stats.Heal * HealMultiplier;
        public float CurrentDamage => IsRotten ? Stats.RottenDamage : Stats.Damage * DamageMultiplier;

        // 강화(모닥불 등): 유통기한→스탯 변환 결과(Stats)는 그대로 두고, 신선할 때의 값에 배율만 곱한다.
        // 음식 인스턴스마다 한 번만 걸린다. 썩은 값은 강화와 무관하다.
        public bool IsEnhanced { get; private set; }
        public float HealMultiplier { get; private set; } = 1f;
        public float DamageMultiplier { get; private set; } = 1f;

        // 이 음식이 지금 월드 어디에 있는지 알려주는 Transform (가방이면 플레이어, 바닥 드롭이면 드롭 오브젝트).
        // 위치 기반 감소 배율(IDecayRateModifier)이 읽는다. 비어 있으면 위치 효과를 받지 않는다.
        public Transform Anchor { get; set; }

        public event Action<FoodItem> Rotted;
        public event Action<FoodItem> Broken;
        public event Action<FoodItem> Enhanced;

        public FoodItem(FoodData data, FoodStats stats)
        {
            Data = data;
            Stats = stats;
            RemainingShelfLife = data.shelfLifeSeconds;
            RemainingDurability = stats.MaxDurability;
        }

        // 드롭처럼 "갓 만든 것이 아닌" 음식을 만들 때 쓴다. 스탯(회복/데미지/내구도)은 최대 유통기한 기준 그대로.
        public void SetShelfLife01(float ratio)
        {
            RemainingShelfLife = Mathf.Clamp01(ratio) * Data.shelfLifeSeconds;
        }

        // 이미 강화됐거나 썩었으면 false. 남은 유통기한은 (남은 값 × (1 + 비율))로 늘고 최대치를 넘지 않는다.
        public bool TryEnhance(float healMultiplier, float damageMultiplier, float shelfLifeBonusRatio)
        {
            if (IsEnhanced || IsRotten) return false;
            IsEnhanced = true;
            HealMultiplier = healMultiplier;
            DamageMultiplier = damageMultiplier;
            RemainingShelfLife = Mathf.Min(Data.shelfLifeSeconds, RemainingShelfLife * (1f + Mathf.Max(0f, shelfLifeBonusRatio)));
            Enhanced?.Invoke(this);
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (IsRotten) return;
            RemainingShelfLife = Mathf.Max(0f, RemainingShelfLife - deltaSeconds);
            if (IsRotten) Rotted?.Invoke(this);
        }

        public void UseDurability()
        {
            if (IsBroken) return;
            RemainingDurability--;
            if (IsBroken) Broken?.Invoke(this);
        }
    }
}
