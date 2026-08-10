public sealed class WolfSprintSkill : EnemySkillBase, IBattleStartSkill
{
    public override string Id => "wolf_sprint";
    public void OnBattleStart(UnitSkillContext c, EnemySkillData d) => c.Caster.ApplyMoveSpeedMultiplier(1f + P(V(d, 0, 2)), V(d, 0, 1));
}
