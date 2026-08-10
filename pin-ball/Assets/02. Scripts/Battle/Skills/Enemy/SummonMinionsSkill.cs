public sealed class SummonMinionsSkill : EnemySkillBase, IUnitDamagedSkill
{
    private int _phase;
    public override string Id => "summon_minions";
    public void OnDamaged(UnitSkillContext c, EnemySkillData d)
    {
        if (c.EnemyActions == null) throw new System.InvalidOperationException("Enemy actions are required.");
        while (_phase < 3 && c.Caster.HpRatio <= (_phase == 0 ? 0.75f : _phase == 1 ? 0.5f : 0.25f))
        {
            c.Caster.PlayEnemySkillFeedback(Id, null, true);
            c.EnemyActions.SpawnEnemyReinforcement("goblin", UnityEngine.Mathf.RoundToInt(V(d, 0, 1)), c.Caster.transform.position);
            c.EnemyActions.SpawnEnemyReinforcement("wolf", UnityEngine.Mathf.RoundToInt(V(d, 1, 1)), c.Caster.transform.position);
            _phase++;
        }
    }
}
