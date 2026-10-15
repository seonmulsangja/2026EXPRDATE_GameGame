using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ExpiryGame.Drop
{
    [Serializable]
    public struct DropEntry
    {
        public FoodData food;
        [Min(0f)] public float weight;
        [Range(0f, 1f)] public float minShelfLife01;
        [Range(0f, 1f)] public float maxShelfLife01;
    }

    public readonly struct DropResult
    {
        public readonly FoodData Food;
        public readonly float ShelfLife01;

        public DropResult(FoodData food, float shelfLife01)
        {
            Food = food;
            ShelfLife01 = shelfLife01;
        }
    }

    // 무엇을 얼마나 떨굴지의 모든 규칙. "강한 적일수록 강한 음식" 가중치 계산은 이 클래스(EffectiveWeight)에서만 한다.
    [CreateAssetMenu(menuName = "ShelfLife/Drop Table", fileName = "DropTable")]
    public class DropTable : ScriptableObject
    {
        [SerializeField] private DropEntry[] entries = Array.Empty<DropEntry>();
        [Tooltip("적 하나가 죽었을 때 아무것도 안 떨굴 확률의 반대 (1 = 항상 드롭)")]
        [SerializeField, Range(0f, 1f)] private float dropChance = 0.9f;
        [SerializeField, Min(0)] private int minCount = 1;
        [SerializeField, Min(0)] private int maxCount = 2;
        [Tooltip("위협도가 높을 때 강한(유통기한 짧은) 음식 가중치 배율. 1 = 영향 없음. " +
                 "위협도 1에서 가장 강한 음식은 ×이 값, 가장 약한 음식은 ×1/이 값")]
        [SerializeField, Min(1f)] private float threatWeightBias = 3f;

        public IReadOnlyList<DropEntry> Entries => entries;
        public float DropChance => dropChance;

        public void Roll(float threat01, List<DropResult> results)
        {
            if (entries.Length == 0 || Random.value > dropChance) return;

            var count = Random.Range(minCount, Mathf.Max(minCount, maxCount) + 1);
            var range = ShelfLifeRange();
            var total = 0f;
            for (var i = 0; i < entries.Length; i++) total += EffectiveWeight(entries[i], threat01, range);
            if (total <= 0f) return;

            for (var n = 0; n < count; n++)
            {
                var pick = entries[PickIndex(threat01, range, total)];
                var shelf = Random.Range(Mathf.Min(pick.minShelfLife01, pick.maxShelfLife01),
                    Mathf.Max(pick.minShelfLife01, pick.maxShelfLife01));
                results.Add(new DropResult(pick.food, shelf));
            }
        }

        // 0 = 표에서 가장 약한(유통기한 긴) 음식, 1 = 가장 강한(유통기한 짧은) 음식
        public float Strength01(FoodData food)
        {
            return Strength01(food, ShelfLifeRange());
        }

        public float EffectiveWeight(DropEntry entry, float threat01)
        {
            return EffectiveWeight(entry, threat01, ShelfLifeRange());
        }

        private int PickIndex(float threat01, Vector2 range, float total)
        {
            var roll = Random.value * total;
            for (var i = 0; i < entries.Length; i++)
            {
                roll -= EffectiveWeight(entries[i], threat01, range);
                if (roll <= 0f) return i;
            }
            return entries.Length - 1;
        }

        private float EffectiveWeight(DropEntry entry, float threat01, Vector2 range)
        {
            if (entry.food == null || entry.weight <= 0f) return 0f;
            var strength = Strength01(entry.food, range);
            return entry.weight * Mathf.Pow(threatWeightBias, Mathf.Clamp01(threat01) * (2f * strength - 1f));
        }

        private static float Strength01(FoodData food, Vector2 range)
        {
            if (food == null || range.y - range.x < 0.0001f) return 0.5f;
            return 1f - Mathf.InverseLerp(range.x, range.y, food.shelfLifeSeconds);
        }

        private Vector2 ShelfLifeRange()
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var entry in entries)
            {
                if (entry.food == null) continue;
                min = Mathf.Min(min, entry.food.shelfLifeSeconds);
                max = Mathf.Max(max, entry.food.shelfLifeSeconds);
            }
            return min > max ? Vector2.zero : new Vector2(min, max);
        }

        private void OnValidate()
        {
            maxCount = Mathf.Max(minCount, maxCount);
        }
    }
}
