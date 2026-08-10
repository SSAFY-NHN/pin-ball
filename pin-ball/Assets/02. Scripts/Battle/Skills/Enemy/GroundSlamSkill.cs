public sealed class GroundSlamSkill : EnemySkillBase, IBasicAttackHitSkill
{
    public override string Id => "ground_slam";
    public void OnBasicAttackHit(UnitSkillContext c, EnemySkillData d, UnitBase target, int count)
    {
        if (count % 3 != 0) return;
        c.Caster.PlayEnemySkillFeedback(Id, null, true);
        c.TargetFinder.GetAliveAlliesInRadius(c.Caster.transform.position, V(d, 0, 1), c.Targets);
        foreach (var ally in c.Targets)
        {
            ally.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), 0f, c.Caster);
            ally.ApplyStun(V(d, 1, 1));
            ally.ApplyKnockback(ally.transform.position - c.Caster.transform.position, V(d, 2, 1));
        }
    }
}
