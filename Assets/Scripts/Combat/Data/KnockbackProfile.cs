using UnityEngine;

namespace ExpiryGame.Combat
{
    // 넉백 수치 전부. 타격용(Knockback_Hit)과 모닥불 안전지대 밀어내기용(Knockback_SafeZone)을 에셋 두 개로 나눠 쓴다.
    // 거리는 "총 이동 거리(타일)", 시간은 "그 거리를 움직이는 데 걸리는 시간"이라 수치 그대로 눈에 보이는 값이다.
    [CreateAssetMenu(menuName = "ShelfLife/Knockback Profile", fileName = "KnockbackProfile")]
    public class KnockbackProfile : ScriptableObject
    {
        public const string HitResourcePath = "ShelfLife/Knockback_Hit";
        public const string SafeZoneResourcePath = "ShelfLife/Knockback_SafeZone";

        [Header("거리 (타일)")]
        [Tooltip("가장 약한 타격의 거리")]
        [Min(0f)] public float baseDistance = 0.3f;
        [Tooltip("가장 센 타격이 baseDistance에 더하는 거리. 보통 0.3~0.6타일이 되도록 약하게")]
        [Min(0f)] public float bonusDistanceAtMaxDamage = 0.3f;
        [Tooltip("이 데미지 이하면 보너스 0 (FoodBalanceConfig 신선 최저 3)")]
        public float weakDamage = 3f;
        [Tooltip("이 데미지 이상이면 보너스 최대 (FoodBalanceConfig 최대 25)")]
        public float strongDamage = 25f;
        [Tooltip("데미지(0~1) → 보너스 비율(0~1) 변환")]
        public AnimationCurve damageCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("어떤 경우에도 한 번에 밀리는 최대 거리 (연속 피격이 쌓여도 이 값을 넘지 않는다)")]
        [Min(0f)] public float maxDistance = 1f;

        [Header("시간 / 감속")]
        [Tooltip("밀리는 시간 (초). 짧을수록 '툭' 치는 느낌")]
        [Min(0.02f)] public float duration = 0.1f;
        [Tooltip("시간(0~1) → 이동 비율(0~1). 처음에 빠르고 끝에서 느려지는 ease-out. 마지막 값은 1이어야 한다")]
        public AnimationCurve easeOut = new AnimationCurve(
            new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        [Tooltip("움직이는 중 순간 속도(타일/초) 상한. 넘으면 시간을 늘려 속도를 맞춘다 (거리는 유지)")]
        [Min(0.1f)] public float maxSpeed = 14f;

        [Header("경직 (맞은 쪽이 바로 반격하지 못하는 시간)")]
        [Tooltip("넉백 시작부터 이 시간 동안 이동/접촉 공격을 못 한다. 거리를 줄여도 맞는 느낌을 유지")]
        [Min(0f)] public float staggerSeconds = 0.15f;

        [Header("연속 피격 감쇠")]
        [Tooltip("이 시간 안에 또 맞으면 연속 피격으로 본다")]
        [Min(0f)] public float comboWindowSeconds = 0.6f;
        [Tooltip("연속 피격마다 거리에 곱하는 배율 (0.6이면 2타 60%, 3타 36%)")]
        [Range(0f, 1f)] public float comboFalloff = 0.6f;
        [Tooltip("감쇠가 내려갈 수 있는 최저 배율")]
        [Range(0f, 1f)] public float minComboMultiplier = 0.2f;

        [Header("충돌")]
        [Tooltip("장애물/다른 적에 닿기 전에 남기는 간격 (타일)")]
        [Min(0f)] public float skin = 0.02f;

        private static KnockbackProfile hitFallback;
        private static KnockbackProfile safeZoneFallback;

        // 타격 넉백: 연결된 에셋 → Resources/ShelfLife/Knockback_Hit → 코드 기본값(약한 값)
        public static KnockbackProfile ResolveHit(KnockbackProfile assigned)
        {
            return Resolve(assigned, HitResourcePath, ref hitFallback, false);
        }

        // 안전지대 밀어내기: 훨씬 약하고 느리게 경계 밖으로
        public static KnockbackProfile ResolveSafeZone(KnockbackProfile assigned)
        {
            return Resolve(assigned, SafeZoneResourcePath, ref safeZoneFallback, true);
        }

        private static KnockbackProfile Resolve(KnockbackProfile assigned, string path, ref KnockbackProfile fallback, bool safeZone)
        {
            if (assigned != null) return assigned;
            var loaded = Resources.Load<KnockbackProfile>(path);
            if (loaded != null) return loaded;
            if (fallback == null)
            {
                fallback = CreateInstance<KnockbackProfile>();
                if (safeZone) ApplySafeZoneDefaults(fallback);
            }
            return fallback;
        }

        public static void ApplySafeZoneDefaults(KnockbackProfile profile)
        {
            profile.baseDistance = 1.5f;
            profile.bonusDistanceAtMaxDamage = 0f;
            profile.maxDistance = 5f;
            profile.duration = 0.6f;
            profile.maxSpeed = 3f;
            profile.staggerSeconds = 0f;
            profile.comboFalloff = 1f;
            profile.minComboMultiplier = 1f;
        }

        // 데미지 → 밀리는 거리. 상한(maxDistance)을 넘지 않는다.
        public float DistanceFor(float damage)
        {
            var t = Mathf.InverseLerp(weakDamage, Mathf.Max(strongDamage, weakDamage + 0.01f), damage);
            var bonus = Mathf.Clamp01(damageCurve.Evaluate(t)) * bonusDistanceAtMaxDamage;
            return Mathf.Min(baseDistance + bonus, maxDistance);
        }

        // 연속 피격 n번째(0 = 첫 타)의 거리 배율
        public float ComboMultiplier(int chain)
        {
            if (chain <= 0) return 1f;
            return Mathf.Max(minComboMultiplier, Mathf.Pow(comboFalloff, chain));
        }
    }
}
