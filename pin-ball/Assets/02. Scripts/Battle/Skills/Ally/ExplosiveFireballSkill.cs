using UnityEngine;

public sealed class ExplosiveFireballSkill : AllySkillBase
{
    public override string Id => "explosive_fireball";
    public override void Execute(UnitSkillContext c, AllySkillData d)
    {
        if (c.PrimaryTarget == null) return;
        c.TargetFinder.GetAliveEnemiesInRadius(c.PrimaryTarget.transform.position, V(d, 0, 1), c.Targets);
        float armorIgnore = P(V(d, 2, 1));
        foreach (var enemy in c.Targets)
        {
            enemy.TakeDamage(c.Caster.AttackDamage * P(V(d, 0, 2)), armorIgnore, c.Caster);
            enemy.ApplyDamageOverTime(c.Caster.AttackDamage * P(V(d, 1, 2)), Mathf.Max(1f, V(d, 1, 3)), armorIgnore);
        }
    }
}
