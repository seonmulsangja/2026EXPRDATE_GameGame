using UnityEngine;

namespace ExpiryGame
{
    public class FoodStatCalculator : IFoodStatCalculator
    {
        private readonly FoodBalanceConfig config;

        public FoodStatCalculator(FoodBalanceConfig config)
        {
            this.config = config;
        }

        public FoodStats Calculate(float shelfLifeSeconds)
        {
            var range = Mathf.Max(config.longestShelfLifeSeconds - config.shortestShelfLifeSeconds, Mathf.Epsilon);
            var shortness = Mathf.Clamp01((config.longestShelfLifeSeconds - shelfLifeSeconds) / range);

            var heal = config.heal.Evaluate(shortness);
            var damage = config.damage.Evaluate(shortness);
            var durability = Mathf.Max(1, Mathf.RoundToInt(config.durability.Evaluate(shortness)));

            return new FoodStats(
                heal,
                damage,
                durability,
                heal * config.rottenHealMultiplier,
                damage * config.rottenDamageMultiplier);
        }
    }
}
