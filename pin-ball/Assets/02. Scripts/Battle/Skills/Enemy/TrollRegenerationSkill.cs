public sealed class TrollRegenerationSkill : EnemySkillBase, IUnitTickSkill
{
    private float _nextTime;
    public override string Id => "troll_regeneration";
    public void Tick(UnitSkillContext c, EnemySkillData d, float now)
    {
        if (c.Caster.IsStunned || now < _nextTime) return;
        _nextTime = now + 1f;
        bool outOfCombat = now - c.Caster.LastDamagedTime >= V(d, 1, 1);
        c.Caster.Heal(c.Caster.MaxHp * P(outOfCombat ? V(d, 1, 2) : V(d, 0, 1)));
    }
}
