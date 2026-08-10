using UnityEngine;

public sealed class PiercingShotSkill : AllySkillBase
{
    public override string Id => "piercing_shot";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        var direction = c.PrimaryTarget.transform.position - c.Caster.transform.position;
        c.TargetFinder.GetEnemiesInLine(c.Caster.transform.position, direction, c.Caster.AttackRange * 2f, 0.5f, c.Targets);
        float armorIgnore = P(V(d, 4, 1));
        int count = Mathf.Min(4, c.Targets.Count);
        for (var i = 0; i < count; i++) c.Targets[i].TakeDamage(c.Caster.AttackDamage * P(V(d, i, 1)), armorIgnore, c.Caster);
    }
}
