public sealed class ArrowRainSkill : AllySkillBase
{
    public override string Id => "arrow_rain";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        c.TargetFinder.GetAliveEnemiesInRadius(c.PrimaryTarget.transform.position, V(d, 0, 1), c.Targets);
        foreach (var enemy in c.Targets)
        {
            enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), 0f, c.Caster);
            enemy.ApplyMoveSpeedMultiplier(1f - P(V(d, 1, 2)), V(d, 1, 1));
        }
    }
}
