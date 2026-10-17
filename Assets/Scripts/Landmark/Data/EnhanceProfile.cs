using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불 음식 강화 규칙의 모든 수치/문구. 강화 로직(FoodEnhancer)은 이 값만 읽는다.
    [CreateAssetMenu(menuName = "ShelfLife/Landmark/Enhance Profile", fileName = "EnhanceProfile")]
    public class EnhanceProfile : ScriptableObject
    {
        [Header("규칙 (음식당 1회, 썩은 음식 불가)")]
        [Tooltip("회복량 배율 (기존 유통기한→스탯 변환 결과에 곱한다)")]
        [Min(1f)] public float healMultiplier = 1.3f;
        [Tooltip("무기 데미지 배율")]
        [Min(1f)] public float damageMultiplier = 1.3f;
        [Tooltip("남은 유통기한이 이 비율만큼 늘어난다 (0.5 = +50%). 음식의 최대 유통기한을 넘지 않는다")]
        [Min(0f)] public float shelfLifeBonusRatio = 0.5f;
        [Tooltip("한 번 굽고 나서 이 모닥불로 다시 구울 수 있을 때까지 (초)")]
        [Min(0f)] public float cooldownSeconds = 10f;

        [Header("프롬프트 문구 ('F  ' 키 표기는 LandmarkInteractor가 앞에 붙인다)")]
        [Tooltip("{0} = 현재 회복, {1} = 강화 후 회복")]
        public string promptFormat = "굽기 (회복 {0} → {1})";
        [Tooltip("{0} = 사유")]
        public string deniedFormat = "굽기 불가 ({0})";
        public string reasonNoFood = "음식 없음";
        public string reasonAlreadyEnhanced = "이미 강화됨";
        public string reasonRotten = "썩음";
        [Tooltip("{0} = 남은 초")]
        public string reasonCooldown = "쿨다운 {0}초";
        public string reasonCampfireOut = "꺼짐";
        public string reasonLowFuel = "연료 부족";

        public string ReasonFor(EnhanceBlock block, float cooldownLeft)
        {
            switch (block)
            {
                case EnhanceBlock.NoFood: return reasonNoFood;
                case EnhanceBlock.AlreadyEnhanced: return reasonAlreadyEnhanced;
                case EnhanceBlock.Rotten: return reasonRotten;
                case EnhanceBlock.CampfireOut: return reasonCampfireOut;
                case EnhanceBlock.LowFuel: return reasonLowFuel;
                case EnhanceBlock.Cooldown: return string.Format(reasonCooldown, Mathf.CeilToInt(cooldownLeft));
                default: return string.Empty;
            }
        }

        public string PromptFor(in EnhanceCheck check)
        {
            return check.CanEnhance
                ? string.Format(promptFormat, FormatHeal(check.HealBefore), FormatHeal(check.HealAfter))
                : string.Format(deniedFormat, ReasonFor(check.Block, check.CooldownLeft));
        }

        private static string FormatHeal(float value)
        {
            var rounded = Mathf.RoundToInt(value);
            return rounded >= 0 ? $"+{rounded}" : rounded.ToString();
        }
    }
}
