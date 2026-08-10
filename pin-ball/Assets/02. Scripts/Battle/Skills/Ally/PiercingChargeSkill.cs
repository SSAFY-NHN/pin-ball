using UnityEngine;

public sealed class PiercingChargeSkill : AllySkillBase
{
    public override string Id => "piercing_charge";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        var direction = (c.PrimaryTarget.transform.position - c.Caster.transform.position).normalized;
        float distance = V(d, 0, 1);
        c.TargetFinder.GetEnemiesInLine(c.Caster.transform.position, direction, distance, 0.6f, c.Targets);
        int count = Mathf.Min(Mathf.RoundToInt(V(d, 0, 2)), c.Targets.Count);
        for (var i = 0; i < count; i++)
        {
            c.Targets[i].TakeDamage(c.Caster.AttackDamage * P(V(d, 2, 1)), 0f, c.Caster);
            c.Targets[i].ApplyKnockback(direction, V(d, 1, 1));
        }
        c.Caster.transform.position += direction * distance;
    }
}
