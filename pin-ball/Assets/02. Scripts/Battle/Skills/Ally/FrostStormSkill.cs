public sealed class FrostStormSkill : AllySkillBase
{
    public override string Id => "frost_storm";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        c.TargetFinder.GetAliveEnemiesInRadius(c.PrimaryTarget.transform.position, V(d, 0, 1), c.Targets);
        float armorIgnore = P(V(d, 4, 1));
        foreach (var enemy in c.Targets)
        {
            enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), armorIgnore, c.Caster);
            enemy.ApplyStun(V(d, 1, 1));
            enemy.ApplySlowAfterDelay(1f - P(V(d, 2, 2)), 1f - P(V(d, 3, 2)), V(d, 2, 1), V(d, 1, 1));
        }
    }
}
