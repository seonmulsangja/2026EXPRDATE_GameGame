using UnityEngine;

namespace ExpiryGame.Flow
{
    // 일차별 난이도 곡선. x축 = 경과 일수 (0 = 1일차 새벽, 1 = 2일차 새벽, ...), y = 배율 (1 = 기본).
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Difficulty Profile", fileName = "DifficultyProfile")]
    public class DifficultyProfile : ScriptableObject
    {
        [Header("일차별 곡선 (1일차는 튜토리얼처럼 쉽게)")]
        public AnimationCurve spawn = Curve(0.35f, 0.5f, 0.8f, 1.1f, 1.5f, 1.9f);
        public AnimationCurve health = Curve(0.6f, 0.8f, 1f, 1.25f, 1.5f, 1.8f);
        public AnimationCurve speed = Curve(0.7f, 0.8f, 0.9f, 1f, 1.1f, 1.2f);
        public AnimationCurve damage = Curve(0.6f, 0.8f, 1f, 1.2f, 1.5f, 1.8f);

        [Header("밤 추가 배율 (낮은 1)")]
        [Min(0f)] public float nightSpawn = 1.6f;
        [Min(0f)] public float nightHealth = 1.15f;
        [Min(0f)] public float nightSpeed = 1.1f;
        [Min(0f)] public float nightDamage = 1.25f;

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
