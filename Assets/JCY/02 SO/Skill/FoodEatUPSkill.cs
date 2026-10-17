using UnityEngine;

[CreateAssetMenu(fileName = "FoodEatUPSkill", menuName = "Scriptable Objects/FoodEatUPSkill")]
public class FoodEatUPSkill : SkillActionSO
{
    [SerializeField] private float hpIncreasePerLevel = 1f;

    public override void Execute(GameObject owner, SkillSO skill, int level)
    {
        if (owner == null)
            return;

        if (!owner.TryGetComponent<ExpiryGame.Health>(out var health))
            return;

        float amount = hpIncreasePerLevel * level;
        health.IncreaseMaxHealth(amount);
    }
}