using UnityEngine;

public enum AugmentType
{
    Stat,
    Skill
}

public enum AugmentEffectType
{
    AttackPower,
    MoveSpeed ,
    AttackSpeed,
}

[CreateAssetMenu(fileName = "NewAugment", menuName = "Game/Augment")]
public class AugmentSO : ScriptableObject
{
    [Header("Info")]
    public string augmentName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Type")]
    public AugmentType augmentType;

    [Header("Stat Effect")]
    public AugmentEffectType effectType;
    public float value;

    [Header("Skill Effect")]
    public SkillSO skill;

    [Header("Level")]
    [Min(1)] public int maxLevel = 5;
}