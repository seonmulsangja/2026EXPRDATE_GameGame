using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Game/Skill")]
public class SkillSO : ScriptableObject
{
    [Header("Info")]
    public string skillName;
    public Sprite icon;

    [TextArea(2, 4)]
    public string description;

    [Header("Level")]
    [Min(1)] public int maxLevel = 5;

    [Header("Damage")]
    public float damage = 10f;
    public float damagePerLevel = 5f;

    [Header("Duration")]
    [Min(0f)] public float duration = 2f;
    public float durationPerLevel = 0.5f;

    [Header("Cooldown")]
    [Min(0f)] public float cooldown = 5f;
    [Min(0f)] public float cooldownReductionPerLevel = 0.2f;
    [Min(0f)] public float minCooldown = 1f;

    [Header("Range")]
    public float range = 5f;
    public float rangePerLevel = 0.5f;

    [Header("Action")]
    public SkillActionSO skillAction;

    public float GetDamage(int level)
    {
        return damage + damagePerLevel * (level - 1);
    }

    public float GetDuration(int level)
    {
        return Mathf.Max(0f,
            duration + durationPerLevel * (level - 1));
    }

    public float GetCooldown(int level)
    {
        return Mathf.Max(minCooldown,
            cooldown - cooldownReductionPerLevel * (level - 1));
    }

    public float GetRange(int level)
    {
        return range + rangePerLevel * (level - 1);
    }
}