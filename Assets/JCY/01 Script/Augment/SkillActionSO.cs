using UnityEngine;

public abstract class SkillActionSO : ScriptableObject
{
    public abstract void Execute(
        GameObject owner,
        SkillSO skill,
        int level
    );

    public virtual void EndSkill(
        GameObject owner,
        SkillSO skill,
        int level
    )
    {
    }
}