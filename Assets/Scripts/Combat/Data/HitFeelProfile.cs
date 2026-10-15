using System;
using UnityEngine;

namespace ExpiryGame.Combat
{
    [Serializable]
    public struct FloatRange
    {
        public float atWeak;
        public float atStrong;

        public FloatRange(float weak, float strong)
        {
            atWeak = weak;
            atStrong = strong;
        }

        public float Evaluate(float intensity01) => Mathf.Lerp(atWeak, atStrong, intensity01);
    }

    [Serializable]
    public struct WeaponFxEntry
    {
        public FoodData food;
        public ParticleKind kind;
    }

    // 타격감 수치 전부. 데미지 → 연출 강도(0~1) 변환은 IntensityFor 한 곳에서만 한다.
    [CreateAssetMenu(menuName = "ShelfLife/Hit Feel Profile", fileName = "HitFeelProfile")]
    public class HitFeelProfile : ScriptableObject
    {
        [Header("Damage → Intensity (유일한 변환 지점)")]
        [Tooltip("이 데미지 이하면 강도 0 (FoodBalanceConfig 기준 신선 최저 3)")]
        public float weakDamage = 3f;
        [Tooltip("이 데미지 이상이면 강도 1 (FoodBalanceConfig 최대 25)")]
        public float strongDamage = 25f;
        public AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Hit stop (초)")]
        public FloatRange hitStopSeconds = new FloatRange(0.04f, 0.08f);

        [Header("Camera shake")]
        public FloatRange shakeAmplitude = new FloatRange(0.05f, 0.28f);
        public FloatRange shakeSeconds = new FloatRange(0.08f, 0.18f);

        [Header("Knockback")]
        public FloatRange knockbackDistance = new FloatRange(0.3f, 1.4f);
        [Tooltip("0=스윙(조준) 방향 그대로, 1=플레이어→적 방향")]
        [Range(0f, 1f)] public float knockbackRadialBlend = 0.4f;

        [Header("Flash")]
        [Min(0.01f)] public float flashSeconds = 0.06f;

        [Header("Particles")]
        public FloatRange particleCount = new FloatRange(3f, 9f);
        public FloatRange particleSpeedScale = new FloatRange(0.8f, 1.4f);
        [Range(0f, 180f)] public float particleSpreadDegrees = 70f;
        public ParticleKind defaultKind = ParticleKind.Crumb;
        public WeaponFxEntry[] weaponFx = Array.Empty<WeaponFxEntry>();

        [Header("Weapon break (내구도 0)")]
        [Min(0)] public int breakParticleCount = 16;
        public float breakShakeAmplitude = 0.16f;
        public float breakShakeSeconds = 0.14f;
        public float breakHitStopSeconds = 0.05f;

        public float IntensityFor(float damage)
        {
            var t = Mathf.InverseLerp(weakDamage, Mathf.Max(strongDamage, weakDamage + 0.01f), damage);
            return Mathf.Clamp01(intensityCurve.Evaluate(t));
        }

        public ParticleKind ParticleKindFor(FoodItem weapon)
        {
            if (weapon == null) return defaultKind;
            if (weapon.IsRotten) return ParticleKind.Rot;
            foreach (var entry in weaponFx)
                if (entry.food == weapon.Data) return entry.kind;
            return defaultKind;
        }
    }
}
