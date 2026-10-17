
using System.Collections.Generic;
using UnityEngine;
using ExpiryGame;

public class AugmentState : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private int baseAttackPower = 10;
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float baseAttackSpeed = 0.4f;

    [Header("Current Stats")]
    [SerializeField] private int attackPower;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float attackCooldown;

    private readonly Dictionary<SkillSO, int> _skillLevels = new();

    public int AttackPower => attackPower;
    public float MoveSpeed => moveSpeed;
    public float AttackCooldown => attackCooldown;

    private PlayerSkillController _skillController;
    private PlayerMover _playerMover;
    [SerializeField] private FoodWeapon foodWeapon;

    private void Awake()
    {
        attackPower = baseAttackPower;
        moveSpeed = baseMoveSpeed;
        attackCooldown = baseAttackSpeed;

        _skillController = GetComponent<PlayerSkillController>();
        _playerMover = GetComponent<PlayerMover>();

        if (foodWeapon == null)
            foodWeapon = GetComponent<FoodWeapon>();

        ApplyMoveSpeed();
        ApplyWeaponStats();
    }

    public void ApplyAugment(AugmentSO augment)
    {
        if (augment == null)
            return;

        switch (augment.augmentType)
        {
            case AugmentType.Stat:
                switch (augment.effectType)
                {
                    case AugmentEffectType.AttackPower:
                        AddAttackPower(Mathf.RoundToInt(augment.value));
                        break;

                    case AugmentEffectType.MoveSpeed:
                        AddMoveSpeed(augment.value);
                        break;

                    case AugmentEffectType.AttackSpeed:
                        AddAttackSpeed(augment.value);
                        break;
                }
                break;

            case AugmentType.Skill:
                if (augment.skill != null)
                    AcquireSkill(augment.skill);
                break;
        }

        Debug.Log($"{augment.augmentName} 획득");
    }

    public int GetSkillLevel(SkillSO skill)
    {
        return _skillLevels.TryGetValue(skill, out int level) ? level : 0;
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
            _skillController.AddOrLevelUpSkill(skill, newLevel);
        else
            Debug.LogWarning("PlayerSkillController가 플레이어에 없습니다.");

        Debug.Log($"{skill.skillName} 획득! Lv.{newLevel}");
        return true;
    }
    
    public int AddAttackPower(int amount)
    {
        attackPower += amount;
        ApplyWeaponStats();
        return attackPower;
    }

    public float AddAttackSpeed(float amount)
    {
        attackCooldown = Mathf.Max(0.01f, attackCooldown - amount);
        ApplyWeaponStats();
        return attackCooldown;
    }
    
    public float AddMoveSpeed(float amount)
    {
        moveSpeed = Mathf.Max(0f, moveSpeed + amount);
        ApplyMoveSpeed();
        return moveSpeed;
    }

    private void ApplyMoveSpeed()
    {
        if (_playerMover != null)
            _playerMover.MoveSpeed = moveSpeed;
    }
    
    private void ApplyWeaponStats()
    {
        if (foodWeapon == null)
            return;

        foodWeapon.SetAttackStats(attackPower, attackCooldown);
    }
}
