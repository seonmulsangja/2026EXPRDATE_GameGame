using System;
using UnityEngine;

namespace ExpiryGame
{
    [Serializable]
    public struct StatRange
    {
        public float atLongestShelfLife;
        public float atShortestShelfLife;

        public StatRange(float atLongest, float atShortest)
        {
            atLongestShelfLife = atLongest;
            atShortestShelfLife = atShortest;
        }

        public float Evaluate(float shortness01)
        {
            return Mathf.Lerp(atLongestShelfLife, atShortestShelfLife, shortness01);
        }
    }

    [CreateAssetMenu(menuName = "ShelfLife/Food Balance Config", fileName = "FoodBalanceConfig")]
    public class FoodBalanceConfig : ScriptableObject
    {
        [Min(1f)] public float shortestShelfLifeSeconds = 30f;
        [Min(2f)] public float longestShelfLifeSeconds = 300f;

        public StatRange heal = new StatRange(5f, 40f);
        public StatRange damage = new StatRange(3f, 25f);
        public StatRange durability = new StatRange(8f, 2f);

        public float rottenHealMultiplier = -0.5f;
        public float rottenDamageMultiplier = 0.5f;
    }
}
