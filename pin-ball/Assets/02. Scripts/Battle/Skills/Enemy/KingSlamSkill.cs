public sealed class KingSlamSkill : EnemySkillBase, IBasicAttackHitSkill
{
    public override string Id => "king_slam";
    public void OnBasicAttackHit(UnitSkillContext c, EnemySkillData d, UnitBase target, int count)
    {
        if (count % 4 != 0 || target == null) return;
        target.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 1)), 0f, c.Caster);
        target.ApplyStun(V(d, 2, 1));
        c.TargetFinder.GetAliveAlliesInRadius(target.transform.position, V(d, 1, 1), c.Targets);
        foreach (var ally in c.Targets) ally.TakeDamage(c.Caster.AttackDamage * P(V(d, 1, 2)), 0f, c.Caster);
    }
}
