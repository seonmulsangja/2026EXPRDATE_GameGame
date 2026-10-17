using UnityEngine;

namespace ExpiryGame.Flow
{
    // 점수 공식 수치. 계산은 ScoreCalculator 한 곳.
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Score Rules", fileName = "ScoreRules")]
    public class ScoreRules : ScriptableObject
    {
        public int perKill = 10;
        public int perDaySurvived = 100;
        public int perFoodEaten = 5;
        [Tooltip("썩은 음식을 먹을 때마다 (음수면 감점)")]
        public int perRottenEaten = -15;
        [Tooltip("클리어 보너스")]
        public int clearBonus = 500;
    }
}
