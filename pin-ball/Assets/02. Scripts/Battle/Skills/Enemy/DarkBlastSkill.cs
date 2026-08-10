public sealed class DarkBlastSkill : EnemySkillBase, IBasicAttackHitSkill
{
    public override string Id => "dark_blast";
    public void OnBasicAttackHit(UnitSkillContext c, EnemySkillData d, UnitBase target, int count)
    {
        if (count % 4 != 0 || target == null) return;
        c.TargetFinder.GetAliveAlliesInRadius(target.transform.position, V(d, 0, 1), c.Targets);
        foreach (var ally in c.Targets)
        {
            ally.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), 0f, c.Caster);
            ally.ApplyAttackRateMultiplier(1f - P(V(d, 1, 1)), V(d, 1, 2));
        }
    }
}
