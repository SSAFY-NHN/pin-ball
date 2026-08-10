public sealed class ShieldJudgmentSkill : AllySkillBase
{
    public override string Id => "shield_judgment";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        c.PrimaryTarget.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 1)), 0f, c.Caster);
        c.TargetFinder.GetAliveEnemiesInRadius(c.PrimaryTarget.transform.position, V(d, 1, 1), c.Targets);
        foreach (var enemy in c.Targets) enemy.ForceTarget(c.Caster, V(d, 1, 2));
        c.Caster.ApplyShield(c.Caster.MaxHp * P(V(d, 2, 2)), V(d, 2, 1));
    }
}
