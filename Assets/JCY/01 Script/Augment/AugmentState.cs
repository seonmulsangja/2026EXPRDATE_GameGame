using System.Collections.Generic;
using UnityEngine;

public class AugmentState : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseAttackPower = 10f;
    [SerializeField] private float baseMoveSpeed = 5f;

    [Header("Current Stats")]
    [SerializeField] private float attackPower;
    [SerializeField] private float moveSpeed;

    private readonly Dictionary<SkillSO, int> _skillLevels = new();

    public float AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;

    private PlayerSkillController _skillController;

    private void Awake()
    {
        attackPower = baseAttackPower;
        moveSpeed = baseMoveSpeed;

        _skillController = GetComponent<PlayerSkillController>();
    }

    public void ApplyAugment(AugmentSO augment)
    {
        switch (augment.effectType)
        {
            case AugmentEffectType.AttackPower:
                attackPower += augment.value;
                break;

            case AugmentEffectType.MoveSpeed:
                moveSpeed += augment.value;
                break;
        }

        Debug.Log($"{augment.augmentName} 획득");
    }

    public int GetSkillLevel(SkillSO skill)
    {
        return _skillLevels.TryGetValue(skill, out int level)
            ? level
            : 0;
    }

    public bool AcquireSkill(SkillSO skill)
    {
        if (skill == null)
            return false;

        int currentLevel = GetSkillLevel(skill);

        if (currentLevel >= skill.maxLevel)
            return false;

        int newLevel = currentLevel + 1;
        _skillLevels[skill] = newLevel;

        if (_skillController != null)
        {
            _skillController.AddOrLevelUpSkill(skill, newLevel);
        }
        else
        {
            Debug.LogWarning("PlayerSkillController가 플레이어에 없습니다.");
        }

        Debug.Log($"{skill.skillName} 획득! Lv.{newLevel}");
        return true;
    }
}