using System.Collections.Generic;

using UnityEngine;

public class AllyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Ally;
    protected override Color IdleColor => Color.white;

    public string UnitId { get; private set; }
    public int Level { get; private set; }
    public float CurrentMana { get; private set; }

    private readonly List<UnitBase> _targets = new();

    private UnitManager _unitManager;
    private AllySkillData _skill;
    private AllyCommonData _common;
    private float _nextHitManaTime;

    public void SetData(
        string unitId,
        int level,
        AllySkillData skill,
        AllyCommonData common)
    {
        UnitId = unitId;
        Level = level;
        _skill = skill;
        _common = common;
        _unitManager = App.Get<UnitManager>();
        CurrentMana = Mathf.Clamp(common?.startMana ?? 0f, 0f, _stats.MaxMana);
    }

    protected override void Tick()
    {
        if (!TryKeepOrAcquireTarget())
        {
            _state = EBattleUnitState.Idle;
            ClearTarget();
            return;
        }

        if (_skill != null &&
            _stats.MaxMana > 0f &&
            CurrentMana >= _stats.MaxMana)
        {
            CastSkill();
            return;
        }

        MoveOrAttackTarget();
    }

    protected override void OnBasicAttackHit(UnitBase target)
    {
        AddMana(_common?.basicAttackManaGain ?? 0f);
    }

    protected override void OnDamaged()
    {
        if (_common == null || Time.time < _nextHitManaTime) return;

        AddMana(_common.hitManaGain);
        _nextHitManaTime = Time.time + _common.hitManaGainCooldown;
    }

    private void AddMana(float amount)
    {
        if (_stats.MaxMana <= 0f || amount <= 0f) return;
        CurrentMana = Mathf.Min(_stats.MaxMana, CurrentMana + amount);
    }

    private void CastSkill()
    {
        CurrentMana = 0f;
        _state = EBattleUnitState.Attacking;

        switch (_skill.id)
        {
            case "shield_judgment":
                CastShieldJudgment();
                break;
            case "blood_whirlwind":
                CastBloodWhirlwind();
                break;
            case "arrow_rain":
                CastArrowRain();
                break;
            case "piercing_shot":
                CastPiercingShot();
                break;
            case "explosive_fireball":
                CastExplosiveFireball();
                break;
            case "frost_storm":
                CastFrostStorm();
                break;
            case "piercing_charge":
                CastPiercingCharge();
                break;
            case "phalanx_formation":
                CastPhalanxFormation();
                break;
            default:
                Debug.LogWarning($"[AllyUnit] Skill not implemented: {_skill.id}");
                break;
        }
    }

    private void CastShieldJudgment()
    {
        if (_currentTarget == null) return;

        _currentTarget.TakeDamage(AttackDamage * Percent(Value(0, 1)));
        _unitManager.GetAliveEnemiesInRadius(
            _currentTarget.transform.position,
            Value(1, 1),
            _targets);

        foreach (var enemy in _targets)
        {
            enemy.ForceTarget(this, Value(1, 2));
        }

        ApplyShield(MaxHp * Percent(Value(2, 2)), Value(2, 1));
    }

    private void CastBloodWhirlwind()
    {
        _unitManager.GetAliveEnemiesInRadius(
            transform.position,
            Value(0, 1),
            _targets);

        foreach (var enemy in _targets)
        {
            enemy.TakeDamage(AttackDamage * Percent(Value(0, 2)));
        }

        float healRatio = Mathf.Min(
            Percent(Value(1, 2)),
            _targets.Count * Percent(Value(1, 1)));
        Heal(MaxHp * healRatio);
        ApplyAttackRateMultiplier(
            1f + Percent(Value(2, 2)),
            Value(2, 1));
    }

    private void CastArrowRain()
    {
        if (_currentTarget == null) return;

        _unitManager.GetAliveEnemiesInRadius(
            _currentTarget.transform.position,
            Value(0, 1),
            _targets);

        foreach (var enemy in _targets)
        {
            enemy.TakeDamage(AttackDamage * Percent(Value(0, 2)));
            enemy.ApplyMoveSpeedMultiplier(
                1f - Percent(Value(1, 2)),
                Value(1, 1));
        }
    }

    private void CastPiercingShot()
    {
        if (_currentTarget == null) return;

        Vector3 direction = _currentTarget.transform.position - transform.position;
        _unitManager.GetEnemiesInLine(
            transform.position,
            direction,
            _stats.AttackRange * 2f,
            0.5f,
            _targets);

        float armorIgnore = Percent(Value(4, 1));
        int hitCount = Mathf.Min(4, _targets.Count);
        for (var i = 0; i < hitCount; i++)
        {
            _targets[i].TakeDamage(
                AttackDamage * Percent(Value(i, 1)),
                armorIgnore);
        }
    }

    private void CastExplosiveFireball()
    {
        if (_currentTarget == null) return;

        _unitManager.GetAliveEnemiesInRadius(
            _currentTarget.transform.position,
            Value(0, 1),
            _targets);

        float armorIgnore = Percent(Value(2, 1));
        foreach (var enemy in _targets)
        {
            enemy.TakeDamage(
                AttackDamage * Percent(Value(0, 2)),
                armorIgnore);
            enemy.ApplyDamageOverTime(
                AttackDamage * Percent(Value(1, 2)),
                Mathf.Max(1f, Value(1, 3)),
                armorIgnore);
        }
    }

    private void CastFrostStorm()
    {
        if (_currentTarget == null) return;

        _unitManager.GetAliveEnemiesInRadius(
            _currentTarget.transform.position,
            Value(0, 1),
            _targets);

        float armorIgnore = Percent(Value(4, 1));
        foreach (var enemy in _targets)
        {
            enemy.TakeDamage(
                AttackDamage * Percent(Value(0, 2)),
                armorIgnore);
            enemy.ApplyStun(Value(1, 1));
            enemy.ApplySlowAfterDelay(
                1f - Percent(Value(2, 2)),
                1f - Percent(Value(3, 2)),
                Value(2, 1),
                Value(1, 1));
        }
    }

    private void CastPiercingCharge()
    {
        if (_currentTarget == null) return;

        Vector3 direction = (_currentTarget.transform.position - transform.position).normalized;
        float distance = Value(0, 1);
        int maxTargets = Mathf.RoundToInt(Value(0, 2));

        _unitManager.GetEnemiesInLine(
            transform.position,
            direction,
            distance,
            0.6f,
            _targets);

        int hitCount = Mathf.Min(maxTargets, _targets.Count);
        for (var i = 0; i < hitCount; i++)
        {
            _targets[i].TakeDamage(AttackDamage * Percent(Value(2, 1)));
            _targets[i].ApplyKnockback(direction, Value(1, 1));
        }

        transform.position += direction * distance;
    }

    private void CastPhalanxFormation()
    {
        if (_currentTarget == null) return;

        Vector3 direction = (_currentTarget.transform.position - transform.position).normalized;
        _unitManager.GetEnemiesInLine(
            transform.position,
            direction,
            2.5f,
            1.5f,
            _targets);

        foreach (var enemy in _targets)
        {
            enemy.TakeDamage(AttackDamage * Percent(Value(1, 1)));
            enemy.ApplyStun(Value(2, 1));
        }

        _unitManager.GetAliveAlliesInRadius(
            transform.position,
            Value(3, 1),
            _targets);

        foreach (var ally in _targets)
        {
            ally.ApplyDamageReduction(
                Percent(Value(3, 3)),
                Value(3, 2));
            ally.ApplyKnockbackImmunity(Value(4, 2));
        }
    }

    private float Value(int effectIndex, int valueIndex)
    {
        if (_skill?.effects == null ||
            effectIndex < 0 ||
            effectIndex >= _skill.effects.Length)
        {
            return 0f;
        }

        var effect = _skill.effects[effectIndex];
        return valueIndex switch
        {
            1 => effect.value1,
            2 => effect.value2,
            3 => effect.value3,
            _ => 0f
        };
    }

    private static float Percent(float value)
    {
        return value * 0.01f;
    }
}
