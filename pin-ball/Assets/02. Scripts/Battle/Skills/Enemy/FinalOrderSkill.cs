public sealed class FinalOrderSkill : EnemySkillBase, IUnitDamagedSkill
{
    private bool _activated;
    public override string Id => "final_order";
    public void OnDamaged(UnitSkillContext c, EnemySkillData d)
    {
        if (_activated || c.Caster.HpRatio > 0.25f) return;
        if (c.EnemyActions == null) throw new System.InvalidOperationException("Enemy actions are required.");
        _activated = true;
        c.EnemyActions.ApplyEnemySpeedBuff(1f + P(V(d, 0, 1)), 1f + P(V(d, 1, 1)));
    }
}
