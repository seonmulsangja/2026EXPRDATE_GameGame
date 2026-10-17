using UnityEngine;

namespace ExpiryGame.Flow
{
    // 점수 공식은 여기 한 곳. 수치는 ScoreRules.
    public static class ScoreCalculator
    {
        public static int Calculate(GameStats stats, ScoreRules rules, bool cleared)
        {
            var score = stats.Kills * rules.perKill
                        + stats.DaysSurvived * rules.perDaySurvived
                        + stats.FoodEaten * rules.perFoodEaten
                        + stats.RottenEaten * rules.perRottenEaten
                        + (cleared ? rules.clearBonus : 0);
            return Mathf.Max(0, score);
        }
    }
}
