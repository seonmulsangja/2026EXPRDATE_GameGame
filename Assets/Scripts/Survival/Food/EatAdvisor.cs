using UnityEngine;

namespace ExpiryGame
{
    public enum EatWarning
    {
        None,
        FullHealth,
        Rotten
    }

    // 지금 먹으면 아쉬운 상태인지 판단 (체력 가득 / 썩은 음식). 먹기를 막지는 않고, 이유를 안내 문구로 보여주기 위한 판정만 한다.
    public static class EatAdvisor
    {
        public static EatWarning Evaluate(FoodItem item, float currentHealth, float maxHealth)
        {
            if (item == null) return EatWarning.None;
            if (item.IsRotten) return EatWarning.Rotten;
            if (currentHealth >= maxHealth - 0.01f) return EatWarning.FullHealth;
            return EatWarning.None;
        }

        public static string NoticeFor(EatWarning warning, FoodItem item, EatSettings settings)
        {
            switch (warning)
            {
                case EatWarning.FullHealth:
                    return settings.fullHealthNotice;
                case EatWarning.Rotten:
                    var heal = Mathf.RoundToInt(item.CurrentHeal);
                    return string.Format(settings.rottenNotice, heal >= 0 ? $"+{heal}" : heal.ToString());
                default:
                    return string.Empty;
            }
        }
    }
}
