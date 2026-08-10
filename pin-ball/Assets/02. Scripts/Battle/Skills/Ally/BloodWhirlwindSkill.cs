using UnityEngine;

public sealed class BloodWhirlwindSkill : AllySkillBase
{
    public override string Id => "blood_whirlwind";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        c.TargetFinder.GetAliveEnemiesInRadius(c.Caster.transform.position, V(d, 0, 1), c.Targets);
        foreach (var enemy in c.Targets) enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), 0f, c.Caster);
        float ratio = Mathf.Min(P(V(d, 1, 2)), c.Targets.Count * P(V(d, 1, 1)));
        c.Caster.Heal(c.Caster.MaxHp * ratio);
        c.Caster.ApplyAttackRateMultiplier(1f + P(V(d, 2, 2)), V(d, 2, 1));
    }
}
