using UnityEngine;

namespace ExpiryGame.Flow
{
    // 일차별 난이도 곡선. x축 = 경과 일수 (0 = 1일차 새벽, 1 = 2일차 새벽, ...), y = 배율 (1 = 기본).
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Difficulty Profile", fileName = "DifficultyProfile")]
    public class DifficultyProfile : ScriptableObject
    {
        private static readonly float[] DefaultSpawn = { 0.35f, 0.5f, 0.8f, 1.1f, 1.5f, 1.9f };
        private static readonly float[] DefaultHealth = { 0.6f, 0.8f, 1f, 1.25f, 1.5f, 1.8f };
        private static readonly float[] DefaultSpeed = { 0.7f, 0.8f, 0.9f, 1f, 1.1f, 1.2f };
        private static readonly float[] DefaultDamage = { 0.6f, 0.8f, 1f, 1.2f, 1.5f, 1.8f };

        [Header("일차별 곡선 (1일차는 튜토리얼처럼 쉽게)")]
        public AnimationCurve spawn = Curve(DefaultSpawn);
        public AnimationCurve health = Curve(DefaultHealth);
        public AnimationCurve speed = Curve(DefaultSpeed);
        public AnimationCurve damage = Curve(DefaultDamage);

        [Header("밤 추가 배율 (낮은 1)")]
        [Min(0f)] public float nightSpawn = 1.6f;
        [Min(0f)] public float nightHealth = 1.15f;
        [Min(0f)] public float nightSpeed = 1.1f;
        [Min(0f)] public float nightDamage = 1.25f;

        [Header("방어")]
        [Tooltip("곡선 값이 이보다 작아도 스폰 배율은 이 값 밑으로 내려가지 않는다 (비어 있는 곡선/0 배율로 적이 영구히 안 나오는 사고 방지)")]
        [Min(0.05f)] public float minSpawnMultiplier = 0.2f;

        private void OnValidate()
        {
            EnsureCurves();
        }

        // 곡선이 비어 있으면(키 0개) Evaluate가 항상 0을 돌려줘서 적이 영구히 안 나온다. 기본값으로 채운다. 바꿨으면 true
        public bool EnsureCurves()
        {
            var changed = false;
            changed |= Fill(ref spawn, DefaultSpawn);
            changed |= Fill(ref health, DefaultHealth);
            changed |= Fill(ref speed, DefaultSpeed);
            changed |= Fill(ref damage, DefaultDamage);
            return changed;
        }

        private static bool Fill(ref AnimationCurve curve, float[] defaults)
        {
            if (curve != null && curve.length > 0) return false;
            curve = Curve(defaults);
            return true;
        }

        private static AnimationCurve Curve(params float[] valuesPerDay)
        {
            var keys = new Keyframe[valuesPerDay.Length];
            for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(i, valuesPerDay[i]);
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }
    }
}
