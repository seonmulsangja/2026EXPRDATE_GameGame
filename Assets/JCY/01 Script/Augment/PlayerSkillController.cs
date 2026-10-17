using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    private class SkillRuntime
    {
        public SkillSO skill;
        public int level;
        public Coroutine routine;
        
        public float nextActivationTime = 0f;
    }

    private readonly Dictionary<SkillSO, SkillRuntime> _skills = new();

    public void AddOrLevelUpSkill(SkillSO skill, int level)
    {
        if (skill == null || skill.skillAction == null)
            return;

        level = Mathf.Clamp(level, 1, skill.maxLevel);

        // 이미 보유한 스킬이면 레벨만 갱신
        if (_skills.TryGetValue(skill, out SkillRuntime runtime))
        {
            runtime.level = level;
            return;
        }

        // 새 스킬 획득
        SkillRuntime newRuntime = new SkillRuntime
        {
            skill = skill,
            level = level
        };

        _skills.Add(skill, newRuntime);
        newRuntime.routine = StartCoroutine(SkillRoutine(newRuntime));
    }

    private IEnumerator SkillRoutine(SkillRuntime runtime)
    {
        while (true)
        {
            SkillSO skill = runtime.skill;
            SkillActionSO action = skill.skillAction;
            int level = runtime.level;

            // Blood 스킬은 자체 쿨타임으로 관리
            if (action is Blood_SKill)
            {
                while (true)
                {
                    if (Time.time >= runtime.nextActivationTime)
                    {
                        action.Execute(gameObject, skill, level);
    
                        runtime.nextActivationTime = Time.time + 120f;
                    }

                    yield return null;
                }
            }
            
            // 스킬 발동
            action.Execute(gameObject, skill, level);

            // 지속시간 동안 효과 유지
            float duration = skill.GetDuration(level);

            if (duration > 0f)
                yield return new WaitForSeconds(duration);

            // 지속시간 종료
            action.EndSkill(gameObject, skill, level);

            // 쿨타임 대기
            float cooldown = skill.GetCooldown(runtime.level);

            if (cooldown > 0f)
                yield return new WaitForSeconds(cooldown);
        }
    }

    public int GetSkillLevel(SkillSO skill)
    {
        if (_skills.TryGetValue(skill, out SkillRuntime runtime))
            return runtime.level;

        return 0;
    }

    private void OnDestroy()
    {
        foreach (SkillRuntime runtime in _skills.Values)
        {
            if (runtime.routine != null)
                StopCoroutine(runtime.routine);
        }
    }
}
