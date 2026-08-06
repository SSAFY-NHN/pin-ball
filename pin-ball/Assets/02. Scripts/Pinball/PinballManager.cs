using System;
using System.Collections.Generic;

using UnityEngine;

public class PinballManager : AppService, IItemEventListener
{
    private static readonly EItem[] SupportedItems =
    {
        EItem.PrecisionAimRail,
        EItem.WeightedCore,
        EItem.ElasticCoating,
        EItem.RecoveryInsurance,
        EItem.GoldenBall,
        EItem.AutoBallFeeder,
        EItem.SplitCapsule,
        EItem.ReinforcedBumper,
        EItem.GoldenBumper,
        EItem.WidePocket,
        EItem.SafetyNet,
        EItem.ChargedPin,
        EItem.OverloadBumper
    };

    public event Action<EPinballState> OnStateChanged;

    [Header("Economy")]
    [SerializeField, Min(0)] private int launchCost = 2;

    [Header("Launcher")]
    [SerializeField] private Vector2 launchPosition = new(6.4f, 10f);
    [SerializeField, Min(0f)] private float launchMoveSpeed = 6f;
    [SerializeField, Min(0f)] private float launchHalfRange = 4f;
    [SerializeField] private KeyCode precisionKey = KeyCode.LeftShift;

    [SerializeField] private List<Pinball> pooledBalls = new();

    private readonly Queue<Pinball> _availableBalls = new();
    private readonly HashSet<Pinball> _activeBalls = new();

    private BattleManager _battleManager;
    private UnitManager _unitManager;
    private ItemManager _itemManager;
    private PinballGoal _goal;

    private Vector2 _currentLaunchPosition;

    private float _precisionSpeedMultiplier = 1f;
    private float _precisionRangeBonus;
    private float _horizontalChangeReduction;
    private float _collisionRetentionBonus;
    private float _maxCollisionRetention = 1f;
    private float _recoveryRefundRatio;
    private int _goldenBallRequiredHits;
    private int _goldenBallReward;
    private int _goldenBallMaxReward;
    private int _launchCostDiscount;
    private int _minimumLaunchCost;
    private int _splitCount;
    private float _splitSpeedMultiplier;
    private float _bumperForceBonus;
    private int _goldenBumperReward;
    private int _goldenBumperMaxReward;
    private float _widePocketBonus;
    private int _safetyNetCount;
    private int _chargedPinRequiredHits;
    private float _chargedPinAttackBonus;
    private int _overloadRequiredHits;
    private int _overloadSpawnCount;
    private int _overloadMaxCount;

    private int _remainingSafetyNetCount;

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _unitManager = App.Get<UnitManager>();
        _itemManager = App.Get<ItemManager>();

        SubscribeItems();
        _battleManager.OnStateChanged += OnBattleStateChanged;

        _currentLaunchPosition = launchPosition;
        PrepareBallPool();
    }

    private void Update()
    {
        UpdateLauncherPosition();
    }

    private void PrepareBallPool()
    {
        _availableBalls.Clear();
        _activeBalls.Clear();

        foreach (var ball in pooledBalls)
        {
            if (ball == null) continue;

            ball.SetManager(this);
            _availableBalls.Enqueue(ball);
        }
    }

    private void SubscribeItems()
    {
        foreach (var item in SupportedItems)
        {
            _itemManager.Subscribe(item, this);
        }
    }

    private void UpdateLauncherPosition()
    {
        var input = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(input) < 0.01f) return;

        var isPrecisionMode = _precisionSpeedMultiplier < 1f && Input.GetKey(precisionKey);
        var speedMultiplier = isPrecisionMode ? _precisionSpeedMultiplier : 1f;
        var rangeMultiplier = 1f + (isPrecisionMode ? _precisionRangeBonus : 0f);
        var halfRange = launchHalfRange * rangeMultiplier;

        _currentLaunchPosition.x += input * launchMoveSpeed * speedMultiplier * Time.deltaTime;
        _currentLaunchPosition.x = Mathf.Clamp(
            _currentLaunchPosition.x,
            launchPosition.x - halfRange,
            launchPosition.x + halfRange);
    }

    public void LaunchBall()
    {
        LaunchBall(_currentLaunchPosition);
    }

    public void LaunchBall(Vector2 position)
    {
        if (_availableBalls.Count <= 0) return;

        var discountedCost = Mathf.Max(0, launchCost - _launchCostDiscount);
        var cost = launchCost >= _minimumLaunchCost
            ? Mathf.Max(_minimumLaunchCost, discountedCost)
            : discountedCost;
        if (!_battleManager.TrySpendGold(cost)) return;

        var ball = _availableBalls.Dequeue();
        ball.Activate(position, Vector2.down, cost, false);
        _activeBalls.Add(ball);
        OnStateChanged?.Invoke(EPinballState.Launched);
    }

    internal void OnBallHit(Pinball ball, EPinballObstacle obstacle)
    {
        if (ball == null) return;

        if (obstacle == EPinballObstacle.SmallPin)
        {
            ball.SmallPinHitCount++;
            ApplyWeightedCore(ball);
            ApplyGoldenBall(ball);
            return;
        }

        ball.BigBumperHitCount++;
        ApplyReinforcedBumper(ball);
        ApplyGoldenBumper(ball);
        ApplySplitCapsule(ball);
    }

    internal void ApplyCollisionRetention(Pinball ball, Vector2 previousVelocity)
    {
        if (_collisionRetentionBonus <= 0f || ball == null) return;

        var previousSpeed = previousVelocity.magnitude;
        var currentVelocity = ball.Velocity;
        var currentSpeed = currentVelocity.magnitude;
        if (previousSpeed <= 0.001f || currentSpeed <= 0.001f) return;

        var currentRetention = currentSpeed / previousSpeed;
        var targetRetention = Mathf.Min(
            _maxCollisionRetention,
            currentRetention + _collisionRetentionBonus);

        ball.SetVelocity(currentVelocity.normalized * previousSpeed * targetRetention);
    }

    public void OnGoalBall(Pinball ball)
    {
        if (ball == null) return;

        var unitData = ball.AllyData;

        var attackBonus = ball.SmallPinHitCount >= _chargedPinRequiredHits
            ? _chargedPinAttackBonus
            : 0f;

        _unitManager.SpawnAlly(unitData, attackBonus);

        if (_overloadRequiredHits > 0 &&
            ball.BigBumperHitCount >= _overloadRequiredHits &&
            ball.OverloadUseCount < _overloadMaxCount)
        {
            for (var i = 0; i < _overloadSpawnCount; i++)
            {
                _unitManager.SpawnAlly(unitData, attackBonus);
            }

            ball.OverloadUseCount++;
        }

        _unitManager.TryDuplicateAlly(unitData);
        ReleaseBall(ball);
    }

    public void OnMissedBall(Pinball ball)
    {
        if (ball == null) return;

        if (_recoveryRefundRatio > 0f && !ball.IsClone)
        {
            _battleManager.AddGold(Mathf.FloorToInt(ball.PaidLaunchCost * _recoveryRefundRatio));
        }

        if (_remainingSafetyNetCount > 0 && !ball.WasRescued && !ball.IsClone)
        {
            _remainingSafetyNetCount--;
            ball.WasRescued = true;
            ball.ResetPosition(launchPosition, Vector2.down);
            return;
        }

        ReleaseBall(ball);
    }

    public void ReleaseBall(Pinball ball)
    {
        if (ball == null || !_activeBalls.Remove(ball)) return;

        ball.Deactivate();
        _availableBalls.Enqueue(ball);

        if (_activeBalls.Count <= 0)
        {
            OnStateChanged?.Invoke(EPinballState.Idle);
        }
    }

    internal void RegisterGoal(PinballGoal goal)
    {
        if (goal == null) return;

        if (_goal != null && _goal != goal)
        {
            Debug.LogWarning("[PinballManager] PinballGoal이 두 개 이상 등록되었습니다.");
            return;
        }

        _goal = goal;
        RefreshGoalWidths();
    }

    internal void UnregisterGoal(PinballGoal goal)
    {
        if (_goal == goal)
        {
            _goal = null;
        }
    }

    public void OnItemEvent(Item item)
    {
        switch (item.Key)
        {
            case EItem.PrecisionAimRail:
                _precisionSpeedMultiplier = item.Value1;
                _precisionRangeBonus = item.Value2;
                break;
            case EItem.WeightedCore:
                _horizontalChangeReduction = item.Value1;
                break;
            case EItem.ElasticCoating:
                _collisionRetentionBonus = item.Value1;
                _maxCollisionRetention = item.Value2;
                break;
            case EItem.RecoveryInsurance:
                _recoveryRefundRatio = item.Value1;
                break;
            case EItem.GoldenBall:
                _goldenBallRequiredHits = Mathf.RoundToInt(item.Value1);
                _goldenBallReward = Mathf.RoundToInt(item.Value2);
                _goldenBallMaxReward = Mathf.RoundToInt(item.Value3);
                break;
            case EItem.AutoBallFeeder:
                _launchCostDiscount = Mathf.RoundToInt(item.Value1);
                _minimumLaunchCost = Mathf.RoundToInt(item.Value2);
                break;
            case EItem.SplitCapsule:
                _splitCount = Mathf.RoundToInt(item.Value1);
                _splitSpeedMultiplier = item.Value2;
                break;
            case EItem.ReinforcedBumper:
                _bumperForceBonus = item.Value1;
                break;
            case EItem.GoldenBumper:
                _goldenBumperReward = Mathf.RoundToInt(item.Value1);
                _goldenBumperMaxReward = Mathf.RoundToInt(item.Value2);
                break;
            case EItem.WidePocket:
                _widePocketBonus = item.Value1;
                RefreshGoalWidths();
                break;
            case EItem.SafetyNet:
                _safetyNetCount = Mathf.RoundToInt(item.Value1);
                _remainingSafetyNetCount = _safetyNetCount;
                break;
            case EItem.ChargedPin:
                _chargedPinRequiredHits = Mathf.RoundToInt(item.Value1);
                _chargedPinAttackBonus = item.Value2;
                break;
            case EItem.OverloadBumper:
                _overloadRequiredHits = Mathf.RoundToInt(item.Value1);
                _overloadSpawnCount = Mathf.RoundToInt(item.Value2);
                _overloadMaxCount = Mathf.RoundToInt(item.Value3);
                break;
        }
    }

    private void ApplyWeightedCore(Pinball ball)
    {
        if (_horizontalChangeReduction <= 0f) return;

        var velocity = ball.Velocity;
        var horizontalChange = velocity.x - ball.PreviousVelocity.x;
        velocity.x = ball.PreviousVelocity.x + horizontalChange * (1f - _horizontalChangeReduction);
        ball.SetVelocity(velocity);
    }

    private void ApplyGoldenBall(Pinball ball)
    {
        if (_goldenBallRequiredHits <= 0) return;
        if (ball.SmallPinHitCount % _goldenBallRequiredHits != 0) return;
        if (ball.GoldenBallGold >= _goldenBallMaxReward) return;

        var reward = Mathf.Min(
            _goldenBallReward,
            _goldenBallMaxReward - ball.GoldenBallGold);

        ball.GoldenBallGold += reward;
        _battleManager.AddGold(reward);
    }

    private void ApplyReinforcedBumper(Pinball ball)
    {
        if (_bumperForceBonus <= 0f) return;
        ball.SetVelocity(ball.Velocity * (1f + _bumperForceBonus));
    }

    private void ApplyGoldenBumper(Pinball ball)
    {
        if (_goldenBumperReward <= 0 || ball.GoldenBumperGold >= _goldenBumperMaxReward) return;

        var reward = Mathf.Min(
            _goldenBumperReward,
            _goldenBumperMaxReward - ball.GoldenBumperGold);

        ball.GoldenBumperGold += reward;
        _battleManager.AddGold(reward);
    }

    private void ApplySplitCapsule(Pinball source)
    {
        if (_splitCount <= 0 || source.IsClone || source.HasSplit) return;

        source.HasSplit = true;
        for (var i = 0; i < _splitCount; i++)
        {
            if (_availableBalls.Count <= 0) break;

            var clone = _availableBalls.Dequeue();
            clone.Activate(
                source.transform.position,
                source.Velocity.normalized,
                0,
                true,
                source.AllyData);
            clone.SetVelocity(source.Velocity * _splitSpeedMultiplier);
            _activeBalls.Add(clone);
        }
    }

    private void RefreshGoalWidths()
    {
        if (_goal == null) return;

        _goal.SetWidthMultiplier(1f + _widePocketBonus);
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Pending) return;

        _remainingSafetyNetCount = _safetyNetCount;
        RefreshGoalWidths();
    }

    protected override void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnBattleStateChanged;
        }

        if (_itemManager != null)
        {
            foreach (var item in SupportedItems)
            {
                _itemManager.Unsubscribe(item, this);
            }
        }

        base.OnDestroy();
    }
}
