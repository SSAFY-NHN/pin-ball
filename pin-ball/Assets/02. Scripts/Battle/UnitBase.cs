using UnityEngine;

//소유: 개인 전투 상태(HP, 타겟, 쿨다운, 상태)
//책임: 탐색/이동/공격/피격/사망
//금지: 웨이브 상태 변경, 골드 지급, UI 갱신
public abstract class UnitBase : MonoBehaviour
{
    public virtual EBattleTeam Team { get; protected set; }
    public EBattleUnitState State => _state;
    public float CurrentHp { get; private set; }
    public float MaxHp => _stats.MaxHp;
    public bool IsAlive => _state != EBattleUnitState.Dead;
    
    private UnitManager _unitManager;
    protected BattleUnitStats _stats;
    protected UnitBase _currentTarget;
    protected EBattleUnitState _state;

    [SerializeField] private TextMesh _stateLabel;
    private Renderer _renderer;
    private float _nextAttackTime;
    private float _hitUntilTime;

    protected virtual Color IdleColor => new(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color AttackColor = new(1f, 0.95f, 0.25f, 1f);
    private static readonly Color HitColor = new(1f, 0.2f, 0.2f, 1f);
    private static readonly Color DeadColor = new(0.3f, 0.3f, 0.3f, 1f);

    public void Initialize(BattleUnitStats stats)
    {
        _unitManager = App.Get<UnitManager>();
        _stats = stats;

        _state = EBattleUnitState.Idle;
        CurrentHp = stats.MaxHp;
        _nextAttackTime = 0f;
        _hitUntilTime = 0f;
        _currentTarget = null;

        _renderer = GetComponentInChildren<Renderer>();

        UpdateLabel();
        UpdateVisual();
    }

    private void Update()
    {
        if (!IsAlive) return;

        Tick();

        if (_state == EBattleUnitState.Hit && Time.time > _hitUntilTime)
        {
            _state = EBattleUnitState.Idle;
        }

        UpdateLabel();
        UpdateVisual();
    }

    protected abstract void Tick();

    public void TakeDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
        {
            return;
        }

        CurrentHp -= damage;
        _state = EBattleUnitState.Hit;
        _hitUntilTime = Time.time + 0.08f;

        if (CurrentHp <= 0f)
        {
            Die();
        }
    }

    protected bool TryKeepOrAcquireTarget()
    {
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

        var distance = Vector2.Distance(transform.position, _currentTarget.transform.position);
        if (distance > _stats.AttackRange)
        {
            if (_stats.MoveSpeed <= 0f)
            {
                _state = EBattleUnitState.Idle;
                return;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _currentTarget.transform.position,
                _stats.MoveSpeed * Time.deltaTime);

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

        _nextAttackTime = Time.time + (1f / Mathf.Max(0.01f, _stats.AttackRate));
        _state = EBattleUnitState.Attacking;
        _currentTarget.TakeDamage(_stats.AttackDamage);
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
        if (_renderer == null)
        {
            return;
        }

        var baseColor = _state switch
        {
            EBattleUnitState.Dead => DeadColor,
            EBattleUnitState.Hit => HitColor,
            EBattleUnitState.Attacking => AttackColor,
            _ => IdleColor
        };

        _renderer.material.color = baseColor;
    }
}
