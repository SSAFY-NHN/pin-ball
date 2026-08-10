public sealed class OrcRageSkill : EnemySkillBase, IUnitDamagedSkill
{
    private bool _activated;
    public override string Id => "orc_rage";
    public void OnDamaged(UnitSkillContext c, EnemySkillData d)
    {
        if (_activated || c.Caster.HpRatio > 0.5f) return;
        _activated = true;
        c.Caster.ApplyAttackDamageMultiplier(1f + P(V(d, 0, 1)), float.PositiveInfinity);
        c.Caster.ApplyAttackRateMultiplier(1f + P(V(d, 1, 1)), float.PositiveInfinity);
    }
}
