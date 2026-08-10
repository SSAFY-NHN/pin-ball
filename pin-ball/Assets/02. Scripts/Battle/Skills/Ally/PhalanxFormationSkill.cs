public sealed class PhalanxFormationSkill : AllySkillBase
{
    public override string Id => "phalanx_formation";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        var direction = (c.PrimaryTarget.transform.position - c.Caster.transform.position).normalized;
        c.TargetFinder.GetEnemiesInLine(c.Caster.transform.position, direction, 2.5f, 1.5f, c.Targets);
        foreach (var enemy in c.Targets)
        {
            enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 1, 1)), 0f, c.Caster);
            enemy.ApplyStun(V(d, 2, 1));
        }
        c.TargetFinder.GetAliveAlliesInRadius(c.Caster.transform.position, V(d, 3, 1), c.Targets);
        foreach (var ally in c.Targets)
        {
            ally.ApplyDamageReduction(P(V(d, 3, 3)), V(d, 3, 2));
            ally.ApplyKnockbackImmunity(V(d, 4, 2));
        }
    }
}
