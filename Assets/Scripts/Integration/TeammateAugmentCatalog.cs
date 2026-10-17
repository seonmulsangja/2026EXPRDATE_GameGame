using System.Collections.Generic;
using ExpiryGame.Flow;
using UnityEngine;

namespace ExpiryGame.Integration
{
    // 팀원 AugmentSO → 우리 AugmentDefinition 변환. 팀원 타입을 아는 곳은 Integration 폴더뿐이라 변환도 여기서 한다.
    //
    // 팀원 증강이 바꾸는 값 → 우리 능력치(StatKey)
    //   AugmentEffectType.AttackPower  → StatKey.AttackPower              (더하기, value × attackPowerPerValue)
    //   AugmentEffectType.MoveSpeed    → StatKey.MoveSpeed                (더하기, value × moveSpeedPerValue)
    //   AugmentEffectType.AttackSpeed  → StatKey.AttackCooldownReduction  (더하기, value × attackCooldownReductionPerValue)
    //   AugmentType.Skill              → 능력치가 아니라 PlayerSkillController.AddOrLevelUpSkill (팀원 스킬 런타임이 실행)
    public sealed class TeammateAugmentCatalog
    {
        private readonly AugmentIntegrationSettings settings;
        private readonly PlayerSkillController skills;
        private readonly Dictionary<AugmentSO, AugmentDefinition> cache = new Dictionary<AugmentSO, AugmentDefinition>();

        public TeammateAugmentCatalog(AugmentIntegrationSettings settings, PlayerSkillController skills)
        {
            this.settings = settings;
            this.skills = skills;
        }

        // 쓸 수 없는 증강(스킬 증강인데 스킬이 비어 있음 등)이면 null
        public AugmentDefinition Get(AugmentSO augment)
        {
            if (augment == null) return null;
            if (cache.TryGetValue(augment, out var cached)) return cached;

            var definition = Convert(augment);
            cache[augment] = definition;
            return definition;
        }

        private AugmentDefinition Convert(AugmentSO augment)
        {
            var maxLevel = Mathf.Max(1, augment.maxLevel);

            if (augment.augmentType == AugmentType.Skill)
            {
                var skill = augment.skill;
                if (skill == null)
                {
                    Debug.LogWarning($"[Augment] '{augment.name}' 은 스킬 증강인데 skill이 비어 있어 후보에서 제외합니다.", augment);
                    return null;
                }
                System.Action<int> grant = null;
                if (skills != null) grant = level => skills.AddOrLevelUpSkill(skill, level);
                else Debug.LogWarning("[Augment] 플레이어에 PlayerSkillController가 없어 스킬 증강은 선택해도 효과가 없습니다. Install Augments To Open Scene을 실행하세요.");

                return new AugmentDefinition(augment.name, First(skill.skillName, augment.augmentName, augment.name),
                    First(skill.description, augment.description, ""), Mathf.Min(maxLevel, Mathf.Max(1, skill.maxLevel)),
                    null, grant);
            }

            return new AugmentDefinition(augment.name, First(augment.augmentName, augment.name), augment.description ?? "",
                maxLevel, new[] { StatOf(augment) });
        }

        private StatEffect StatOf(AugmentSO augment)
        {
            switch (augment.effectType)
            {
                case AugmentEffectType.MoveSpeed:
                    return new StatEffect(StatKey.MoveSpeed, StatOp.Add, augment.value * settings.moveSpeedPerValue);
                case AugmentEffectType.AttackSpeed:
                    return new StatEffect(StatKey.AttackCooldownReduction, StatOp.Add, augment.value * settings.attackCooldownReductionPerValue);
                default:
                    return new StatEffect(StatKey.AttackPower, StatOp.Add, augment.value * settings.attackPowerPerValue);
            }
        }

        private static string First(params string[] candidates)
        {
            foreach (var candidate in candidates)
                if (!string.IsNullOrWhiteSpace(candidate)) return candidate;
            return "";
        }
    }
}
