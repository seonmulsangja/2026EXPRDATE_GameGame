using System.Collections;
using ExpiryGame;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthSkill_JCY",
    menuName = "Scriptable Objects/HealthSkill_JCY")]
public class HealthSkill_JCY : SkillActionSO
{
    public override void Execute(GameObject owner, SkillSO skill, int level)
    {
        Health health = owner.GetComponent<Health>();
        health.Heal(1 * level);
    }
}