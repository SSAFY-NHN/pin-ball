using System.Collections.Generic;

using UnityEngine;

public class EnemyUnit : UnitBase
{
    public override EBattleTeam Team => EBattleTeam.Enemy;
    protected override Color IdleColor => Color.white;

    public string UnitId { get; private set; }
    public int Rank { get; private set; }
    public int BreachDamage { get; private set; }

    private readonly List<UnitBase> _targets = new();

    private UnitManager _unitManager;
    private EnemyUnitData _data;
    private int _basicAttackCount;
    private UnitBase _focusedFireTarget;
    private int _focusedFireStacks;
    private float _nextRegenerationTime;
    private bool _rageActivated;
    private bool _finalOrderActivated;
    private int _summonPhase;

    public void SetData(EnemyUnitData data)
    {
        _data = data;
        UnitId = data?.id ?? string.Empty;
        Rank = data?.rank ?? 0;
        BreachDamage = Mathf.Max(0, data?.BreachDamage ?? 0);
        _unitManager = App.Get<UnitManager>();
        _nextRegenerationTime = Time.time + 1f;

        ApplyBattleStartSkill();
    }

    protected override void Tick()
    {
        UpdateRegeneration();

        if (TryKeepOrAcquireTarget())
        {
            MoveOrAttackTarget();
            return;
        }

        _state = EBattleUnitState.Idle;
        ClearTarget();
    }

    protected override float GetBasicAttackDamage(UnitBase target)
    {
        float damage = base.GetBasicAttackDamage(target);
        var skill = FindSkill("focused_fire");
        if (skill == null || target != _focusedFireTarget)
        {
            return damage;
        }

        float bonusPerStack = Percent(Value(skill, 0, 1));
        return damage * (1f + bonusPerStack * _focusedFireStacks);
    }

    protected override void OnBasicAttackHit(UnitBase target)
    {
        _basicAttackCount++;
        UpdateFocusedFire(target);

        if (UnitId == "shaman" && _basicAttackCount % 4 == 0)
        {
            CastDarkBlast(target);
        }
        else if (UnitId == "ogre_elite" && _basicAttackCount % 3 == 0)
        {
            CastGroundSlam();
        }
        else if (UnitId == "dark_mage_elite" && _basicAttackCount % 4 == 0)
        {
            CastWeakeningCurse();
        }
        else if (UnitId == "goblin_king" && _basicAttackCount % 4 == 0)
        {
            CastKingSlam(target);
        }
    }

    protected override void OnDamaged()
    {
        if (UnitId == "orc_warrior" && !_rageActivated && HpRatio <= 0.5f)
        {
            ActivateRage();
        }

        if (UnitId != "goblin_king") return;

        while (_summonPhase < 3 && HpRatio <= GetSummonThreshold(_summonPhase))
        {
            SummonMinions();
            _summonPhase++;
        }

        if (!_finalOrderActivated && HpRatio <= 0.25f)
        {
            ActivateFinalOrder();
        }
    }

    protected override float ModifyIncomingDamage(float damage, UnitBase source)
    {
        var skill = FindSkill("shield_block");
        if (skill == null || source == null || _currentTarget == null)
        {
            return damage;
        }

        Vector3 facing = (_currentTarget.transform.position - transform.position).normalized;
        Vector3 incoming = (source.transform.position - transform.position).normalized;
        bool isFront = Vector3.Dot(facing, incoming) > 0f;

        return isFront
            ? damage * (1f - Percent(Value(skill, 0, 1)))
            : damage;
    }

    protected override float ModifyCrowdControlDuration(float duration)
    {
        var skill = FindSkill("shield_block");
        return skill == null
            ? duration
            : duration * (1f - Percent(Value(skill, 1, 1)));
    }

    private void ApplyBattleStartSkill()
    {
        var sprint = FindSkill("wolf_sprint");
        if (sprint != null)
        {
            ApplyMoveSpeedMultiplier(
                1f + Percent(Value(sprint, 0, 2)),
                Value(sprint, 0, 1));
        }

        var shadowLeap = FindSkill("shadow_leap");
        if (shadowLeap == null) return;

        var target = _unitManager.FindFarthestAliveAlly(transform.position);
        if (target == null) return;

        Vector3 direction = (transform.position - target.transform.position).normalized;
        transform.position = target.transform.position + direction * 0.5f;
        target.TakeDamage(
            AttackDamage * Percent(Value(shadowLeap, 1, 1)),
            0f,
            this);
        ForceTarget(target, float.PositiveInfinity);
    }

    private void UpdateFocusedFire(UnitBase target)
    {
        var skill = FindSkill("focused_fire");
        if (skill == null) return;

        if (_focusedFireTarget != target)
        {
            _focusedFireTarget = target;
            _focusedFireStacks = 1;
            return;
        }

        _focusedFireStacks = Mathf.Min(
            Mathf.RoundToInt(Value(skill, 0, 2)),
            _focusedFireStacks + 1);
    }

    private void ActivateRage()
    {
        var skill = FindSkill("orc_rage");
        if (skill == null) return;

        _rageActivated = true;
        ApplyAttackDamageMultiplier(
            1f + Percent(Value(skill, 0, 1)),
            float.PositiveInfinity);
        ApplyAttackRateMultiplier(
            1f + Percent(Value(skill, 1, 1)),
            float.PositiveInfinity);
    }

    private void CastDarkBlast(UnitBase target)
    {
        var skill = FindSkill("dark_blast");
        if (skill == null || target == null) return;

        _unitManager.GetAliveAlliesInRadius(
            target.transform.position,
            Value(skill, 0, 1),
            _targets);

        foreach (var ally in _targets)
        {
            ally.TakeDamage(
                AttackDamage * Percent(Value(skill, 0, 2)),
                0f,
                this);
            ally.ApplyAttackRateMultiplier(
                1f - Percent(Value(skill, 1, 1)),
                Value(skill, 1, 2));
        }
    }

    private void UpdateRegeneration()
    {
        var skill = FindSkill("troll_regeneration");
        if (skill == null || IsStunned || Time.time < _nextRegenerationTime) return;

        _nextRegenerationTime = Time.time + 1f;
        bool isOutOfCombat = Time.time - LastDamagedTime >= Value(skill, 1, 1);
        float healPercent = isOutOfCombat
            ? Value(skill, 1, 2)
            : Value(skill, 0, 1);
        Heal(MaxHp * Percent(healPercent));
    }

    private void CastGroundSlam()
    {
        var skill = FindSkill("ground_slam");
        if (skill == null) return;

        _unitManager.GetAliveAlliesInRadius(
            transform.position,
            Value(skill, 0, 1),
            _targets);

        foreach (var ally in _targets)
        {
            ally.TakeDamage(
                AttackDamage * Percent(Value(skill, 0, 2)),
                0f,
                this);
            ally.ApplyStun(Value(skill, 1, 1));
            ally.ApplyKnockback(
                ally.transform.position - transform.position,
                Value(skill, 2, 1));
        }
    }

    private void CastWeakeningCurse()
    {
        var skill = FindSkill("weakening_curse");
        if (skill == null) return;

        var target = _unitManager.FindHighestHpAliveAlly();
        if (target == null) return;

        target.TakeDamage(
            AttackDamage * Percent(Value(skill, 0, 1)),
            0f,
            this);
        target.ApplyDefenseMultiplier(
            1f - Percent(Value(skill, 1, 1)),
            Value(skill, 1, 2));
    }

    private void SummonMinions()
    {
        var skill = FindSkill("summon_minions");
        if (skill == null) return;

        _unitManager.SpawnEnemyReinforcement(
            "goblin",
            Mathf.RoundToInt(Value(skill, 0, 1)),
            transform.position);
        _unitManager.SpawnEnemyReinforcement(
            "wolf",
            Mathf.RoundToInt(Value(skill, 1, 1)),
            transform.position);
    }

    private void CastKingSlam(UnitBase primaryTarget)
    {
        var skill = FindSkill("king_slam");
        if (skill == null || primaryTarget == null) return;

        primaryTarget.TakeDamage(
            AttackDamage * Percent(Value(skill, 0, 1)),
            0f,
            this);
        primaryTarget.ApplyStun(Value(skill, 2, 1));

        _unitManager.GetAliveAlliesInRadius(
            primaryTarget.transform.position,
            Value(skill, 1, 1),
            _targets);

        foreach (var ally in _targets)
        {
            ally.TakeDamage(
                AttackDamage * Percent(Value(skill, 1, 2)),
                0f,
                this);
        }
    }

    private void ActivateFinalOrder()
    {
        var skill = FindSkill("final_order");
        if (skill == null) return;

        _finalOrderActivated = true;
        _unitManager.ApplyEnemySpeedBuff(
            1f + Percent(Value(skill, 0, 1)),
            1f + Percent(Value(skill, 1, 1)));
    }

    private EnemySkillData FindSkill(string id)
    {
        if (_data?.Skills == null) return null;

        foreach (var skill in _data.Skills)
        {
            if (skill != null && skill.SkillId == id)
            {
                return skill;
            }
        }

        return null;
    }

    private static float GetSummonThreshold(int phase)
    {
        return phase switch
        {
            0 => 0.75f,
            1 => 0.5f,
            _ => 0.25f
        };
    }

    private static float Value(EnemySkillData skill, int effectIndex, int valueIndex)
    {
        if (skill?.effects == null ||
            effectIndex < 0 ||
            effectIndex >= skill.effects.Length)
        {
            return 0f;
        }

        var effect = skill.effects[effectIndex];
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
