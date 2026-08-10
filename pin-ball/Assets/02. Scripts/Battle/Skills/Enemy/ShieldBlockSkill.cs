using UnityEngine;

public sealed class ShieldBlockSkill : EnemySkillBase, IIncomingDamageSkill, ICrowdControlDurationSkill
{
    public override string Id => "shield_block";
    public float ModifyIncomingDamage(UnitSkillContext c, EnemySkillData d, float damage, UnitBase source)
    {
        if (source == null || c.PrimaryTarget == null) return damage;
        var facing = (c.PrimaryTarget.transform.position - c.Caster.transform.position).normalized;
        var incoming = (source.transform.position - c.Caster.transform.position).normalized;
        return Vector3.Dot(facing, incoming) > 0f ? damage * (1f - P(V(d, 0, 1))) : damage;
    }
    public float ModifyDuration(UnitSkillContext c, EnemySkillData d, float duration) => duration * (1f - P(V(d, 1, 1)));
}
