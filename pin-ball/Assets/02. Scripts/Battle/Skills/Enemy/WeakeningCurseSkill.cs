public sealed class WeakeningCurseSkill : EnemySkillBase, IBasicAttackHitSkill
{
    public override string Id => "weakening_curse";
    public void OnBasicAttackHit(UnitSkillContext c, EnemySkillData d, UnitBase ignored, int count)
    {
        if (count % 4 != 0) return;
        var target = c.TargetFinder.FindHighestHpAliveAlly();
        if (target == null) return;
        target.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 1)), 0f, c.Caster);
        target.ApplyDefenseMultiplier(1f - P(V(d, 1, 1)), V(d, 1, 2));
    }
}
