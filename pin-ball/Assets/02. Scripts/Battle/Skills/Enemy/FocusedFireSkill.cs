using UnityEngine;

public sealed class FocusedFireSkill : EnemySkillBase, IBasicAttackDamageSkill, IBasicAttackHitSkill
{
    private UnitBase _target;
    private int _stacks;
    public override string Id => "focused_fire";
    public float ModifyDamage(UnitSkillContext c, EnemySkillData d, UnitBase target, float damage) => target == _target ? damage * (1f + P(V(d, 0, 1)) * _stacks) : damage;
    public void OnBasicAttackHit(UnitSkillContext c, EnemySkillData d, UnitBase target, int count)
    {
        if (_target != target) { _target = target; _stacks = 1; return; }
        _stacks = Mathf.Min(Mathf.RoundToInt(V(d, 0, 2)), _stacks + 1);
    }
}
