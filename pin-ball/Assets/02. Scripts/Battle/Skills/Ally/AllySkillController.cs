using System;
using UnityEngine;

public sealed class AllySkillController
{
    private readonly UnitSkillRegistry _registry;
    private AllyCommonData _common;
    private AllySkillData _skill;
    private float _nextHitManaTime;

    public float CurrentMana { get; private set; }

    public AllySkillController(UnitSkillRegistry registry) => _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public bool CanCast(float maxMana) => maxMana > 0f && CurrentMana >= maxMana;

    public void Initialize(AllyCommonData common, AllySkillData skill, float maxMana)
    {
        _common = common;
        _skill = skill;
        Reset(maxMana);
    }

    public void Reset(float maxMana)
    {
        CurrentMana = Mathf.Clamp(_common?.startMana ?? 0f, 0f, maxMana);
        _nextHitManaTime = 0f;
    }

    public void GainFromBasicAttack(float maxMana) => Add(_common?.basicAttackManaGain ?? 0f, maxMana);

    public void GainFromDamage(float now, float maxMana)
    {
        if (_common == null || now < _nextHitManaTime) return;
        Add(_common.hitManaGain, maxMana);
        _nextHitManaTime = now + _common.hitManaGainCooldown;
    }

    public bool TryCast(UnitSkillContext context, float maxMana, Action<string> warn)
    {
        if (!CanCast(maxMana)) return false;
        CurrentMana = 0f;
        if (_skill == null || !_registry.TryCreate(_skill.id, out var skill) || skill is not IActiveUnitSkill active)
        {
            warn?.Invoke($"[AllyUnit] Skill not implemented: {_skill?.id}");
            return false;
        }
        active.Execute(context, _skill);
        return true;
    }

    private void Add(float amount, float maxMana)
    {
        if (maxMana <= 0f || amount <= 0f) return;
        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
    }
}
