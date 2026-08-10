using UnityEngine;

// Owns the Unity lifecycle and coordinates focused unit-domain objects.
public abstract class UnitBase : MonoBehaviour
{
    public abstract EBattleTeam Team { get; }
    public EBattleUnitState State => _state;
    public float CurrentHp => _health.CurrentHp;
    public float MaxHp => _health.MaxHp;
    public float MaxMana => _stats.MaxMana;
    public float AttackDamage => _stats.AttackDamage;
    public float CurrentDefense => _stats.Defense * _statusEffects.DefenseMultiplier;
    public float MoveSpeed => _stats.MoveSpeed;
    public float AttackRate => _stats.AttackRate;
    public float AttackRange => _stats.AttackRange;
    public float HpRatio => _health.HpRatio;
    public bool IsAlive => _state != EBattleUnitState.Dead;
    public bool IsInPool { get; private set; }
    public bool IsStunned => _statusEffects.IsStunned(Time.time);
    public float LastDamagedTime => _health.LastDamagedTime;
    public UnitBase CurrentTarget => _currentTarget;

    protected BattleUnitStats _stats;
    protected UnitBase _currentTarget;
    protected EBattleUnitState _state;

    private readonly UnitHealth _health = new();
    private readonly UnitStatusEffects _statusEffects = new();
    private readonly UnitEffectScheduler _effectScheduler = new();
    private readonly UnitAttack _attack = new();

    private UnitCombatContext _context;
    private SpriteRenderer _renderer;
    private BattleUnitStats _initialStats;
    private UnitBase _forcedTarget;
    private float _forcedTargetUntil;
    private float _hitUntilTime;

    protected virtual Color IdleColor => new(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color AttackColor = Color.white;
    private static readonly Color HitColor = new(1f, 0.2f, 0.2f, 1f);
    private static readonly Color DeadColor = new(0.3f, 0.3f, 0.3f, 1f);

    public void Initialize(BattleUnitStats stats, UnitCombatContext context)
    {
        _context = context;
        _stats = stats;
        _initialStats = stats;
        IsInPool = false;
        ResetCombatState();
        _renderer = GetComponentInChildren<SpriteRenderer>();
        UpdateVisual();
    }

    public void ResetCombatState()
    {
        _stats = _initialStats;
        _state = EBattleUnitState.Idle;
        _health.Reset(_stats.MaxHp);
        _statusEffects.Reset();
        _effectScheduler.Reset();
        _attack.Reset();
        _hitUntilTime = 0f;
        _forcedTargetUntil = 0f;
        _currentTarget = null;
        _forcedTarget = null;
        UpdateVisual();
    }

    public void RestoreForPreparation(Vector3 position)
    {
        IsInPool = false;
        transform.position = position;
        gameObject.SetActive(true);
        ResetCombatState();
    }

    public void MarkReturnedToPool()
    {
        _effectScheduler.Reset();
        _statusEffects.Reset();
        _attack.Reset();
        _health.MarkDead();
        _state = EBattleUnitState.Dead;
        _currentTarget = null;
        _forcedTarget = null;
        IsInPool = true;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsAlive) return;

        float now = Time.time;
        _health.Refresh(now);
        _statusEffects.Refresh(now);
        _effectScheduler.Tick(now, ApplyScheduledDamage, ApplyScheduledSlow);
        if (!IsAlive) return;

        if (_statusEffects.IsStunned(now))
        {
            _state = EBattleUnitState.Idle;
        }
        else
        {
            Tick();
        }

        if (_state == EBattleUnitState.Hit && now > _hitUntilTime)
        {
            _state = EBattleUnitState.Idle;
        }

        UpdateVisual();
    }

    protected abstract void Tick();

    public void TakeDamage(
        float damage,
        float armorIgnoreRatio = 0f,
        UnitBase source = null)
    {
        if (!IsAlive || damage <= 0f) return;

        float now = Time.time;
        damage = ModifyIncomingDamage(damage, source);
        _statusEffects.Refresh(now);
        UnitDamageResult result = _health.TakeDamage(
            damage,
            CurrentDefense,
            armorIgnoreRatio,
            _statusEffects.DamageReduction,
            now);
        if (result.AppliedDamage <= 0f) return;

        _state = EBattleUnitState.Hit;
        _hitUntilTime = now + 0.08f;
        OnDamaged();

        if (result.Died) Die();
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        _health.Heal(amount);
    }

    public void ApplyShield(float amount, float duration)
    {
        if (!IsAlive) return;
        _health.ApplyShield(amount, duration, Time.time);
    }

    public void ApplyAttackRateMultiplier(float multiplier, float duration)
    {
        _statusEffects.ApplyAttackRateMultiplier(multiplier, duration, Time.time);
    }

    public void ApplyAttackDamageMultiplier(float multiplier, float duration)
    {
        _statusEffects.ApplyAttackDamageMultiplier(multiplier, duration, Time.time);
    }

    public void ApplyDefenseMultiplier(float multiplier, float duration)
    {
        _statusEffects.ApplyDefenseMultiplier(multiplier, duration, Time.time);
    }

    public void ApplyPermanentSpeedMultiplier(
        float moveSpeedMultiplier,
        float attackRateMultiplier)
    {
        _stats.MoveSpeed *= Mathf.Max(0f, moveSpeedMultiplier);
        _stats.AttackRate *= Mathf.Max(0.01f, attackRateMultiplier);
        _initialStats.MoveSpeed = _stats.MoveSpeed;
        _initialStats.AttackRate = _stats.AttackRate;
    }

    public void ApplyMoveSpeedMultiplier(float multiplier, float duration)
    {
        if (multiplier < 1f) duration = ModifyCrowdControlDuration(duration);
        _statusEffects.ApplyMoveSpeedMultiplier(multiplier, duration, Time.time);
    }

    public void ApplySlowAfterDelay(
        float moveSpeedMultiplier,
        float attackRateMultiplier,
        float duration,
        float delay)
    {
        _effectScheduler.ScheduleSlow(
            moveSpeedMultiplier,
            attackRateMultiplier,
            duration,
            delay,
            Time.time);
    }

    public void ApplyStun(float duration)
    {
        _statusEffects.ApplyStun(
            ModifyCrowdControlDuration(duration),
            Time.time);
    }

    public void ApplyDamageReduction(float ratio, float duration)
    {
        _statusEffects.ApplyDamageReduction(ratio, duration, Time.time);
    }

    public void ApplyKnockbackImmunity(float duration)
    {
        _statusEffects.ApplyKnockbackImmunity(duration, Time.time);
    }

    public void ApplyKnockback(Vector3 direction, float distance)
    {
        transform.position = UnitMovement.ApplyKnockback(
            transform.position,
            direction,
            distance,
            _statusEffects.IsKnockbackImmune(Time.time));
    }

    public void ApplyDamageOverTime(
        float totalDamage,
        float duration,
        float armorIgnoreRatio = 0f)
    {
        _effectScheduler.ScheduleDamageOverTime(
            totalDamage,
            duration,
            armorIgnoreRatio,
            Time.time);
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
        _stats.AttackDamage = _initialStats.AttackDamage * attackMultiplier;
        _stats.AttackRate = _initialStats.AttackRate * attackRateMultiplier;
        _stats.MaxHp = _initialStats.MaxHp * hpMultiplier;
        _health.ScaleMaximumHp(_stats.MaxHp / previousMaxHp);
    }

    protected virtual void OnBasicAttackHit(UnitBase target)
    {
    }

    protected virtual float GetBasicAttackDamage(UnitBase target)
    {
        return _stats.AttackDamage * _statusEffects.AttackDamageMultiplier;
    }

    protected virtual float ModifyIncomingDamage(float damage, UnitBase source)
    {
        return damage;
    }

    protected virtual float ModifyCrowdControlDuration(float duration)
    {
        return duration;
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

        if (_currentTarget != null && _currentTarget.IsAlive) return true;
        if (_context == null) return false;

        _currentTarget = Team == EBattleTeam.Ally
            ? _context.TargetFinder.FindClosestAliveEnemy(
                transform.position,
                float.MaxValue)
            : _context.TargetFinder.FindClosestAliveAlly(
                transform.position,
                float.MaxValue);
        return _currentTarget != null;
    }

    protected void MoveOrAttackTarget()
    {
        if (_currentTarget == null || !_currentTarget.IsAlive)
        {
            _state = EBattleUnitState.Idle;
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            _currentTarget.transform.position);
        if (distance > _stats.AttackRange)
        {
            float moveSpeed = _stats.MoveSpeed * _statusEffects.MoveSpeedMultiplier;
            if (moveSpeed <= 0f)
            {
                _state = EBattleUnitState.Idle;
                return;
            }

            Vector3 nextPosition = UnitMovement.CalculateNextPosition(
                transform.position,
                _currentTarget.transform.position,
                moveSpeed,
                Time.deltaTime);
            if (_context?.BattleArea != null)
            {
                nextPosition = _context.BattleArea.Clamp(
                    nextPosition,
                    GetMovementPadding());
            }

            transform.position = nextPosition;
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
        if (_currentTarget == null || !_currentTarget.IsAlive)
        {
            _state = EBattleUnitState.Idle;
            return;
        }

        float attackRate = _stats.AttackRate * _statusEffects.AttackRateMultiplier;
        _state = EBattleUnitState.Attacking;
        if (!_attack.TrySchedule(Time.time, attackRate)) return;

        _currentTarget.TakeDamage(GetBasicAttackDamage(_currentTarget), 0f, this);
        OnBasicAttackHit(_currentTarget);
    }

    private void ApplyScheduledDamage(float damage, float armorIgnoreRatio)
    {
        TakeDamage(damage, armorIgnoreRatio);
    }

    private void ApplyScheduledSlow(
        float moveSpeedMultiplier,
        float attackRateMultiplier,
        float duration)
    {
        if (!IsAlive) return;
        ApplyMoveSpeedMultiplier(moveSpeedMultiplier, duration);
        ApplyAttackRateMultiplier(attackRateMultiplier, duration);
    }

    private float GetMovementPadding()
    {
        if (_renderer == null) return 0f;
        return Mathf.Max(
            _renderer.bounds.extents.x,
            _renderer.bounds.extents.y);
    }

    private void Die()
    {
        if (!IsAlive) return;

        _health.MarkDead();
        _state = EBattleUnitState.Dead;
        _effectScheduler.Reset();
        _context?.NotifyUnitDied(this);

        if (Team == EBattleTeam.Ally) gameObject.SetActive(false);
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
