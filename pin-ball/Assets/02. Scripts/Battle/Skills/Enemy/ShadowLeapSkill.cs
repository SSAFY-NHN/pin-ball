public sealed class ShadowLeapSkill : EnemySkillBase, IBattleStartSkill
{
    public override string Id => "shadow_leap";
    public void OnBattleStart(UnitSkillContext c, EnemySkillData d)
    {
        var target = c.TargetFinder.FindFarthestAliveAlly(c.Caster.transform.position);
        if (target == null) return;
        var direction = (c.Caster.transform.position - target.transform.position).normalized;
        c.Caster.transform.position = target.transform.position + direction * 0.5f;
        target.TakeDamage(c.Caster.AttackDamage * P(V(d, 1, 1)), 0f, c.Caster);
        c.Caster.ForceTarget(target, float.PositiveInfinity);
    }
}
