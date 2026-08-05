using UnityEngine;

public enum EBattleTeam
{
    Ally,
    Enemy
}

public enum EBattleActorState
{
    Idle,
    Moving,
    Attacking,
    Hit,
    Dead
}

public class BattleActor : MonoBehaviour
{
    public EBattleTeam Team { get; private set; }
    public EBattleActorState State => _state;
    public float CurrentHp { get; private set; }
    public float MaxHp => _stats.MaxHp;
    public bool IsAlive { get; private set; }
    public int DefenseDamage => _defenseDamage;

    private WaveBattleController _controller;
    private BattleActorStats _stats;
    private BattleActor _currentTarget;
    private Transform _defensePoint;
    private TextMesh _stateLabel;
    private Renderer _renderer;

    private int _defenseDamage;
    private float _nextAttackTime;
    private float _nextDefenseHitTime;
    private float _hitUntilTime;
    private EBattleActorState _state;
    private bool _isAtDefensePoint;

    private const float DefenseHitInterval = 1f;

    private static readonly Color AllyColor = new(0.3f, 0.8f, 1f, 1f);
    private static readonly Color EnemyColor = new(1f, 0.45f, 0.45f, 1f);
    private static readonly Color AttackColor = new(1f, 0.95f, 0.25f, 1f);
    private static readonly Color HitColor = new(1f, 0.2f, 0.2f, 1f);
    private static readonly Color DeadColor = new(0.3f, 0.3f, 0.3f, 1f);

    public void Initialize(
        WaveBattleController controller,
        EBattleTeam team,
        BattleActorStats stats,
        int defenseDamage,
        Transform defensePoint)
    {
        _controller = controller;
        Team = team;
        _stats = stats;
        _defenseDamage = Mathf.Max(1, defenseDamage);
        _defensePoint = defensePoint;

        IsAlive = true;
        _state = EBattleActorState.Idle;
        CurrentHp = stats.MaxHp;
        _nextAttackTime = 0f;
        _nextDefenseHitTime = 0f;
        _isAtDefensePoint = false;

        _renderer = GetComponentInChildren<Renderer>();

        EnsureStateLabel();
        UpdateLabel();
        UpdateVisual();
    }

    private void Update()
    {
        if (!IsAlive)
        {
            return;
        }

        if (Team == EBattleTeam.Ally)
        {
            TickAlly();
        }
        else
        {
            TickEnemy();
        }

        if (_state == EBattleActorState.Hit && Time.time > _hitUntilTime)
        {
            _state = EBattleActorState.Idle;
        }

        UpdateLabel();
        UpdateVisual();
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
        {
            return;
        }

        CurrentHp -= damage;
        _state = EBattleActorState.Hit;
        _hitUntilTime = Time.time + 0.08f;

        if (CurrentHp <= 0f)
        {
            Die();
        }
    }

    public void ForceRemove()
    {
        if (!IsAlive)
        {
            return;
        }

        IsAlive = false;
        _state = EBattleActorState.Dead;
        _currentTarget = null;
        UpdateLabel();
        UpdateVisual();
        Destroy(gameObject);
    }

    private void TickAlly()
    {
        if (!TryKeepOrAcquireTarget())
        {
            _state = EBattleActorState.Idle;
            return;
        }

        MoveOrAttackTarget();
    }

    private void TickEnemy()
    {
        if (TryKeepOrAcquireTarget())
        {
            _isAtDefensePoint = false;
            MoveOrAttackTarget();
            return;
        }

        _currentTarget = null;

        if (_defensePoint == null)
        {
            _state = EBattleActorState.Idle;
            return;
        }

        var currentDistanceToDefense = Vector2.Distance(transform.position, _defensePoint.position);
        if (currentDistanceToDefense <= 0.05f)
        {
            _isAtDefensePoint = true;
            _state = EBattleActorState.Attacking;

            if (Time.time >= _nextDefenseHitTime)
            {
                _nextDefenseHitTime = Time.time + DefenseHitInterval;
                _controller.NotifyEnemyReachedDefense(this);
            }

            return;
        }

        var next = Vector3.MoveTowards(
            transform.position,
            _defensePoint.position,
            _stats.MoveSpeed * Time.deltaTime);

        transform.position = next;
        _state = EBattleActorState.Moving;
    }

    private bool TryKeepOrAcquireTarget()
    {
        if (_currentTarget != null && _currentTarget.IsAlive)
        {
            return true;
        }

        _currentTarget = Team == EBattleTeam.Ally
            ? _controller.FindClosestAliveEnemy(transform.position, float.MaxValue)
            : _controller.FindClosestAliveAlly(transform.position, float.MaxValue);

        return _currentTarget != null;
    }

    private void MoveOrAttackTarget()
    {
        if (_currentTarget == null || !_currentTarget.IsAlive)
        {
            _state = EBattleActorState.Idle;
            return;
        }

        var distance = Vector2.Distance(transform.position, _currentTarget.transform.position);
        if (distance > _stats.AttackRange)
        {
            if (_stats.MoveSpeed <= 0f)
            {
                _state = EBattleActorState.Idle;
                return;
            }

            transform.position = Vector2.MoveTowards(
                transform.position,
                _currentTarget.transform.position,
                _stats.MoveSpeed * Time.deltaTime);

            _state = EBattleActorState.Moving;
            return;
        }

        TryAttack();
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
        {
            _state = EBattleActorState.Attacking;
            return;
        }

        if (_currentTarget == null || !_currentTarget.IsAlive)
        {
            _state = EBattleActorState.Idle;
            return;
        }

        _nextAttackTime = Time.time + (1f / Mathf.Max(0.01f, _stats.AttackRate));
        _state = EBattleActorState.Attacking;
        _currentTarget.TakeDamage(_stats.AttackDamage);
    }

    private void Die()
    {
        if (!IsAlive)
        {
            return;
        }

        IsAlive = false;
        CurrentHp = 0f;
        _state = EBattleActorState.Dead;
        _currentTarget = null;

        UpdateLabel();
        UpdateVisual();

        _controller.NotifyActorDied(this);
        Destroy(gameObject);
    }

    private void EnsureStateLabel()
    {
        _stateLabel = GetComponentInChildren<TextMesh>();
        if (_stateLabel != null)
        {
            return;
        }

        var labelObject = new GameObject("StateLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        _stateLabel = labelObject.AddComponent<TextMesh>();
        _stateLabel.anchor = TextAnchor.MiddleCenter;
        _stateLabel.alignment = TextAlignment.Center;
        _stateLabel.characterSize = 0.12f;
        _stateLabel.fontSize = 36;
        _stateLabel.color = Color.white;
    }

    private void UpdateLabel()
    {
        if (_stateLabel == null)
        {
            return;
        }

        _stateLabel.text = $"{Mathf.CeilToInt(CurrentHp)}/{Mathf.CeilToInt(MaxHp)}\n{_state}";
    }

    private void UpdateVisual()
    {
        if (_renderer == null)
        {
            return;
        }

        var baseColor = Team == EBattleTeam.Ally ? AllyColor : EnemyColor;
        if (_state == EBattleActorState.Dead)
        {
            baseColor = DeadColor;
        }
        else if (_state == EBattleActorState.Hit)
        {
            baseColor = HitColor;
        }
        else if (_state == EBattleActorState.Attacking)
        {
            baseColor = AttackColor;
        }

        _renderer.material.color = baseColor;
    }
}
