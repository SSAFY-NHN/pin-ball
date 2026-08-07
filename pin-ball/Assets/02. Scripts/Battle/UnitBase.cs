using System.Collections;

using UnityEngine;

//소유: 개인 전투 상태(HP, 타겟, 쿨다운, 상태이상)
//책임: 탐색/이동/공격/피격/사망
//금지: 웨이브 상태 변경, 골드 지급, UI 갱신
public abstract class UnitBase : MonoBehaviour
{
    public abstract EBattleTeam Team { get; }
    public EBattleUnitState State => _state;
    public float CurrentHp { get; private set; }
    public float MaxHp => _stats.MaxHp;
    public float AttackDamage => _stats.AttackDamage;
    public bool IsAlive => _state != EBattleUnitState.Dead;

    protected BattleUnitStats _stats;
    protected UnitBase _currentTarget;
    protected EBattleUnitState _state;

    [SerializeField] private TextMesh _stateLabel;

    private UnitManager _unitManager;
    private SpriteRenderer _renderer;
    private BattleUnitStats _initialStats;
    private UnitBase _forcedTarget;
    private float _forcedTargetUntil;
    private float _nextAttackTime;
    private float _hitUntilTime;
    private float _stunnedUntil;
    private float _shieldUntil;
    private float _shieldAmount;
    private float _attackRateMultiplier = 1f;
    private float _attackRateMultiplierUntil;
    private float _moveSpeedMultiplier = 1f;
    private float _moveSpeedMultiplierUntil;
    private float _damageReduction;
    private float _damageReductionUntil;
    private float _knockbackImmuneUntil;
    private int _damageOverTimeVersion;

    protected virtual Color IdleColor => new(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color AttackColor = Color.white;
    private static readonly Color HitColor = new(1f, 0.2f, 0.2f, 1f);
    private static readonly Color DeadColor = new(0.3f, 0.3f, 0.3f, 1f);

    public void Initialize(BattleUnitStats stats)
    {
        _unitManager = App.Get<UnitManager>();
        _stats = stats;
        _initialStats = stats;

        _state = EBattleUnitState.Idle;
        CurrentHp = stats.MaxHp;
        _nextAttackTime = 0f;
        _hitUntilTime = 0f;
        _currentTarget = null;
        _forcedTarget = null;

        _renderer = GetComponentInChildren<SpriteRenderer>();

        UpdateLabel();
        UpdateVisual();
    }

    private void Update()
    {
        if (!IsAlive) return;

        RefreshTimedEffects();

        if (Time.time < _stunnedUntil)
        {
            _state = EBattleUnitState.Idle;
        }
        else
        {
            Tick();
        }

        if (_state == EBattleUnitState.Hit && Time.time > _hitUntilTime)
        {
            _state = EBattleUnitState.Idle;
        }

        UpdateLabel();
        UpdateVisual();
    }

    protected abstract void Tick();

    public void TakeDamage(float damage, float armorIgnoreRatio = 0f)
    {
        if (!IsAlive || damage <= 0f) return;
        
        var effectiveDefense =
            _stats.Defense * (1f - Mathf.Clamp01(armorIgnoreRatio));
        var finalDamage = Mathf.Floor(
            damage * 100f / (100f + effectiveDefense));

        if (Time.time < _damageReductionUntil)
        {
            finalDamage *= 1f - Mathf.Clamp01(_damageReduction);
        }

        if (Time.time < _shieldUntil && _shieldAmount > 0f)
        {
            float absorbedDamage = Mathf.Min(_shieldAmount, finalDamage);
            _shieldAmount -= absorbedDamage;
            finalDamage -= absorbedDamage;
        }

        if (finalDamage <= 0f) return;

        CurrentHp -= finalDamage;
        _state = EBattleUnitState.Hit;
        _hitUntilTime = Time.time + 0.08f;
        OnDamaged();

        if (CurrentHp <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
    }

    public void ApplyShield(float amount, float duration)
    {
        if (!IsAlive || amount <= 0f || duration <= 0f) return;
        _shieldAmount = Mathf.Max(_shieldAmount, amount);
        _shieldUntil = Mathf.Max(_shieldUntil, Time.time + duration);
    }

    public void ApplyAttackRateMultiplier(float multiplier, float duration)
    {
        _attackRateMultiplier = Mathf.Max(0.01f, multiplier);
        _attackRateMultiplierUntil = Mathf.Max(
            _attackRateMultiplierUntil,
            Time.time + duration);
    }

    public void ApplyMoveSpeedMultiplier(float multiplier, float duration)
    {
        _moveSpeedMultiplier = Mathf.Max(0f, multiplier);
        _moveSpeedMultiplierUntil = Mathf.Max(
            _moveSpeedMultiplierUntil,
            Time.time + duration);
    }

    public void ApplySlowAfterDelay(
        float moveSpeedMultiplier,
        float attackRateMultiplier,
        float duration,
        float delay)
    {
        StartCoroutine(ApplySlowAfterDelayRoutine(
            moveSpeedMultiplier,
            attackRateMultiplier,
            duration,
            delay));
    }

    public void ApplyStun(float duration)
    {
        _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + duration);
    }

    public void ApplyDamageReduction(float ratio, float duration)
    {
        _damageReduction = Mathf.Max(_damageReduction, Mathf.Clamp01(ratio));
        _damageReductionUntil = Mathf.Max(_damageReductionUntil, Time.time + duration);
    }

    public void ApplyKnockbackImmunity(float duration)
    {
        _knockbackImmuneUntil = Mathf.Max(_knockbackImmuneUntil, Time.time + duration);
    }

    public void ApplyKnockback(Vector3 direction, float distance)
    {
        if (Time.time < _knockbackImmuneUntil || direction.sqrMagnitude <= 0.001f) return;
        transform.position += direction.normalized * distance;
    }

    public void ApplyDamageOverTime(
        float totalDamage,
        float duration,
        float armorIgnoreRatio = 0f)
    {
        if (totalDamage <= 0f || duration <= 0f) return;
        _damageOverTimeVersion++;
        StartCoroutine(DamageOverTime(
            totalDamage,
            duration,
            armorIgnoreRatio,
            _damageOverTimeVersion));
    }

    public void ForceTarget(UnitBase target, float duration)
    {
        if (target == null || !target.IsAlive || duration <= 0f) return;
        _forcedTarget = target;
        _forcedTargetUntil = Time.time + duration;
        _currentTarget = target;
    }

    public void ApplyItemModifiers(
        float attackMultiplier,
        float attackRateMultiplier,
        float hpMultiplier)
    {
        if (Team != EBattleTeam.Ally) return;

        float previousMaxHp = Mathf.Max(0.01f, _stats.MaxHp);
        float hpRatio = Mathf.Clamp01(CurrentHp / previousMaxHp);

        _stats.AttackDamage = _initialStats.AttackDamage * attackMultiplier;
        _stats.AttackRate = _initialStats.AttackRate * attackRateMultiplier;
        _stats.MaxHp = _initialStats.MaxHp * hpMultiplier;
        CurrentHp = _stats.MaxHp * hpRatio;
    }

    protected virtual void OnBasicAttackHit(UnitBase target)
    {
    }

    protected virtual void OnDamaged()
    {
    }

    protected bool TryKeepOrAcquireTarget()
    {
        if (_forcedTarget != null &&
            _forcedTarget.IsAlive &&
            Time.time < _forcedTargetUntil)
        {
            _currentTarget = _forcedTarget;
            return true;
        }

        if (_currentTarget != null && _currentTarget.IsAlive)
        {
            return true;
        }

        _currentTarget = Team == EBattleTeam.Ally
            ? _unitManager.FindClosestAliveEnemy(transform.position, float.MaxValue)
            : _unitManager.FindClosestAliveAlly(transform.position, float.MaxValue);

        return _currentTarget != null;
    }

    protected void MoveOrAttackTarget()
    {
        if (_currentTarget == null || !_currentTarget.IsAlive)
        {
            _state = EBattleUnitState.Idle;
            return;
        }

        float distance = Vector2.Distance(transform.position, _currentTarget.transform.position);
        if (distance > _stats.AttackRange)
        {
            float moveSpeed = _stats.MoveSpeed * _moveSpeedMultiplier;
            if (moveSpeed <= 0f)
            {
                _state = EBattleUnitState.Idle;
                return;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _currentTarget.transform.position,
                moveSpeed * Time.deltaTime);

            _state = EBattleUnitState.Moving;
            return;
        }

        TryAttack();
    }

    protected void ClearTarget()
    {
        _currentTarget = null;
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
        {
            _state = EBattleUnitState.Attacking;
            return;
        }

        if (_currentTarget == null || !_currentTarget.IsAlive)
        {
            _state = EBattleUnitState.Idle;
            return;
        }

        float attackRate = Mathf.Max(0.01f, _stats.AttackRate * _attackRateMultiplier);
        _nextAttackTime = Time.time + 1f / attackRate;
        _state = EBattleUnitState.Attacking;
        _currentTarget.TakeDamage(_stats.AttackDamage);
        OnBasicAttackHit(_currentTarget);
    }

    private void RefreshTimedEffects()
    {
        if (Time.time >= _attackRateMultiplierUntil)
        {
            _attackRateMultiplier = 1f;
        }

        if (Time.time >= _moveSpeedMultiplierUntil)
        {
            _moveSpeedMultiplier = 1f;
        }

        if (Time.time >= _damageReductionUntil)
        {
            _damageReduction = 0f;
        }

        if (Time.time >= _shieldUntil)
        {
            _shieldAmount = 0f;
        }
    }

    private IEnumerator DamageOverTime(
        float totalDamage,
        float duration,
        float armorIgnoreRatio,
        int version)
    {
        int tickCount = Mathf.Max(1, Mathf.CeilToInt(duration));
        float damagePerTick = totalDamage / tickCount;
        float interval = duration / tickCount;

        for (var i = 0; i < tickCount && IsAlive && version == _damageOverTimeVersion; i++)
        {
            yield return new WaitForSeconds(interval);
            TakeDamage(damagePerTick, armorIgnoreRatio);
        }
    }

    private IEnumerator ApplySlowAfterDelayRoutine(
        float moveSpeedMultiplier,
        float attackRateMultiplier,
        float duration,
        float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsAlive) yield break;

        ApplyMoveSpeedMultiplier(moveSpeedMultiplier, duration);
        ApplyAttackRateMultiplier(attackRateMultiplier, duration);
    }

    public void ForceRemove()
    {
        Die();
    }

    private void Die()
    {
        if (!IsAlive) return;

        CurrentHp = 0f;
        _state = EBattleUnitState.Dead;
        _unitManager.NotifyUnitDied(this);
        Destroy(gameObject);
    }

    private void UpdateLabel()
    {
        if (_stateLabel == null) return;
        _stateLabel.text = $"{Mathf.CeilToInt(CurrentHp)}/{Mathf.CeilToInt(MaxHp)}\n{_state}";
    }

    private void UpdateVisual()
    {
        if (_renderer == null) return;

        _renderer.color = _state switch
        {
            EBattleUnitState.Dead => DeadColor,
            EBattleUnitState.Hit => HitColor,
            EBattleUnitState.Attacking => AttackColor,
            _ => IdleColor
        };
    }
}
