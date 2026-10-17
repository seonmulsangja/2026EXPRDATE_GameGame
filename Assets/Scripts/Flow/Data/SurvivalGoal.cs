using UnityEngine;

namespace ExpiryGame.Flow
{
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Survival Goal", fileName = "SurvivalGoal")]
    public class SurvivalGoal : ScriptableObject
    {
        [Tooltip("이 일수만큼 버티면 클리어. 1일차 새벽에 시작해서 (이 값+1)일차 새벽이 되는 순간 Clear")]
        [Min(1)] public int surviveDays = 5;

        // DayStarted(day)가 오면 day-1일을 버틴 것
        public bool IsCleared(int dayStarted)
        {
            return dayStarted - 1 >= surviveDays;
        }
    }
}
