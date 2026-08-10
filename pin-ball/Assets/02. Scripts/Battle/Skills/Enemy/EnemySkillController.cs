using System;
using System.Collections.Generic;

public sealed class EnemySkillController
{
    private sealed class Entry
    {
        public IUnitSkill Skill;
        public EnemySkillData Data;
    }

    private readonly List<Entry> _skills = new();
    public int BasicAttackCount { get; private set; }

    public void Initialize(EnemyUnitData data, UnitSkillRegistry registry)
    {
        if (registry == null) throw new ArgumentNullException(nameof(registry));
        _skills.Clear();
        BasicAttackCount = 0;
        if (data?.Skills == null) return;
        foreach (var skillData in data.Skills)
        {
            if (skillData != null && registry.TryCreate(skillData.SkillId, out var skill))
                _skills.Add(new Entry { Skill = skill, Data = skillData });
        }
    }

    public void OnBattleStart(UnitSkillContext context)
    {
        foreach (var entry in _skills) if (entry.Skill is IBattleStartSkill skill) skill.OnBattleStart(context, entry.Data);
    }

    public void Tick(UnitSkillContext context, float now)
    {
        foreach (var entry in _skills) if (entry.Skill is IUnitTickSkill skill) skill.Tick(context, entry.Data, now);
    }

    public float ModifyBasicAttackDamage(UnitSkillContext context, UnitBase target, float damage)
    {
        foreach (var entry in _skills) if (entry.Skill is IBasicAttackDamageSkill skill) damage = skill.ModifyDamage(context, entry.Data, target, damage);
        return damage;
    }

    public void OnBasicAttackHit(UnitSkillContext context, UnitBase target)
    {
        BasicAttackCount++;
        foreach (var entry in _skills) if (entry.Skill is IBasicAttackHitSkill skill) skill.OnBasicAttackHit(context, entry.Data, target, BasicAttackCount);
    }

    public void OnDamaged(UnitSkillContext context)
    {
        foreach (var entry in _skills) if (entry.Skill is IUnitDamagedSkill skill) skill.OnDamaged(context, entry.Data);
    }

    public float ModifyIncomingDamage(UnitSkillContext context, float damage, UnitBase source)
    {
        foreach (var entry in _skills) if (entry.Skill is IIncomingDamageSkill skill) damage = skill.ModifyIncomingDamage(context, entry.Data, damage, source);
        return damage;
    }

    public float ModifyCrowdControlDuration(UnitSkillContext context, float duration)
    {
        foreach (var entry in _skills) if (entry.Skill is ICrowdControlDurationSkill skill) duration = skill.ModifyDuration(context, entry.Data, duration);
        return duration;
    }
}
