using ExpiryGame;
using UnityEngine;

[CreateAssetMenu(fileName = "Blood_SKill",
    menuName = "Scriptable Objects/Blood_SKill")]
public class Blood_SKill : SkillActionSO
{
    public override void Execute(GameObject owner, SkillSO skill, int level)
    {
        Health health = owner.GetComponent<Health>();

        if (health == null)
            return;

        if (health.Current <= health.Max / 5f)
        {
            health.Heal(level * 10f);
        }
    }
}