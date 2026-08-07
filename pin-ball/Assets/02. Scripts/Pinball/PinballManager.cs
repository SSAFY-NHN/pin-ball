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
        EItem.TargetMagnet,
        EItem.SplitCapsule,
        EItem.ReinforcedBumper,
        EItem.GoldenBumper,
        EItem.WidePocket,
        EItem.FocusedPocket,
        EItem.SafetyNet,
        EItem.SwapLever,
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
    private readonly List<PinballGoal> _goals = new();

    private BattleManager _battleManager;
    private UnitManager _unitManager;
    private ItemManager _itemManager;

    private Vector2 _currentLaunchPosition;
    private int _selectedGoalIndex;
    private int _pendingSwapGoalIndex = -1;

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
    private float _targetMagnetDistanceMultiplier;
    private float _targetMagnetStrength;
    private int _targetMagnetCount;
    private int _splitCount;
    private float _splitSpeedMultiplier;
    private float _bumperForceBonus;
    private int _goldenBumperReward;
    private int _goldenBumperMaxReward;
    private float _widePocketBonus;
    private float _focusedPocketBonus;
    private float _otherPocketPenalty;
    private int _safetyNetCount;
    private int _swapCount;
    private int _chargedPinRequiredHits;
    private float _chargedPinAttackBonus;
    private int _overloadRequiredHits;
    private int _overloadSpawnCount;
    private int _overloadMaxCount;

    private int _remainingSafetyNetCount;
    private int _remainingSwapCount;

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
        if (_battleManager == null || !_battleManager.IsPreparationPhase) return;
        if (_availableBalls.Count <= 0) return;

        var discountedCost = Mathf.Max(0, launchCost - _launchCostDiscount);
        var cost = launchCost >= _minimumLaunchCost
            ? Mathf.Max(_minimumLaunchCost, discountedCost)
            : discountedCost;
        if (!_battleManager.TrySpendPreparationGold(cost)) return;

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

    internal void ApplyTargetMagnet(Pinball ball)
    {
        if (_targetMagnetCount <= 0 ||
            ball.TargetMagnetUseCount >= _targetMagnetCount) return;
        if (_goals.Count == 0) return;

        var goalIndex = Mathf.Clamp(_selectedGoalIndex, 0, _goals.Count - 1);
        var goal = _goals[goalIndex];
        if (goal == null) return;

        var maxDistance = ball.Diameter * _targetMagnetDistanceMultiplier;
        var offset = goal.transform.position - ball.transform.position;
        if (offset.sqrMagnitude > maxDistance * maxDistance) return;

        var velocity = ball.Velocity;
        var correction = Mathf.Abs(velocity.x) * _targetMagnetStrength;
        if (correction <= 0.001f)
        {
            correction = velocity.magnitude * _targetMagnetStrength;
        }

        velocity.x += Mathf.Sign(offset.x) * correction;
        ball.SetVelocity(velocity);
        ball.TargetMagnetUseCount++;
    }

    public void OnGoalBall(Pinball ball, PinballGoal goal)
    {
        if (ball == null || goal == null) return;

        var unitData = goal.UnitData;

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
        if (goal == null || _goals.Contains(goal)) return;

        _goals.Add(goal);
        _goals.Sort((left, right) =>
            left.transform.position.x.CompareTo(right.transform.position.x));
        RefreshGoalWidths();
    }

    internal void UnregisterGoal(PinballGoal goal)
    {
        _goals.Remove(goal);
        _selectedGoalIndex = Mathf.Clamp(
            _selectedGoalIndex,
            0,
            Mathf.Max(0, _goals.Count - 1));
    }

    internal void SelectGoal(PinballGoal goal)
    {
        if (_battleManager == null || !_battleManager.IsPreparationPhase) return;

        var goalIndex = _goals.IndexOf(goal);
        if (goalIndex < 0) return;

        _selectedGoalIndex = goalIndex;
        RefreshGoalWidths();
    }

    internal void SelectSwapGoal(PinballGoal goal)
    {
        if (_battleManager == null || !_battleManager.IsPreparationPhase) return;

        var goalIndex = _goals.IndexOf(goal);
        if (goalIndex < 0 || _remainingSwapCount <= 0) return;
        if (_activeBalls.Count > 0) return;

        if (_pendingSwapGoalIndex < 0)
        {
            _pendingSwapGoalIndex = goalIndex;
            return;
        }

        if (_pendingSwapGoalIndex != goalIndex)
        {
            var firstGoal = _goals[_pendingSwapGoalIndex];
            var firstData = firstGoal.UnitData;
            firstGoal.SetUnitData(goal.UnitData);
            goal.SetUnitData(firstData);
            _remainingSwapCount--;
        }

        _pendingSwapGoalIndex = -1;
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
            case EItem.TargetMagnet:
                _targetMagnetDistanceMultiplier = item.Value1;
                _targetMagnetStrength = item.Value2;
                _targetMagnetCount = Mathf.RoundToInt(item.Value3);
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
            case EItem.FocusedPocket:
                _focusedPocketBonus = item.Value1;
                _otherPocketPenalty = item.Value2;
                RefreshGoalWidths();
                break;
            case EItem.SafetyNet:
                _safetyNetCount = Mathf.RoundToInt(item.Value1);
                _remainingSafetyNetCount = _safetyNetCount;
                break;
            case EItem.SwapLever:
                _swapCount = Mathf.RoundToInt(item.Value1);
                _remainingSwapCount = _swapCount;
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
                true);
            clone.SetVelocity(source.Velocity * _splitSpeedMultiplier);
            _activeBalls.Add(clone);
        }
    }

    private void RefreshGoalWidths()
    {
        for (var i = 0; i < _goals.Count; i++)
        {
            var multiplier = 1f + _widePocketBonus;
            if (_focusedPocketBonus > 0f)
            {
                multiplier += i == _selectedGoalIndex
                    ? _focusedPocketBonus
                    : -_otherPocketPenalty;
            }

            var maxWorldWidth = GetMaximumGoalWidth(i);
            _goals[i].SetWidthMultiplier(
                Mathf.Max(0.1f, multiplier),
                maxWorldWidth);
        }
    }

    private float GetMaximumGoalWidth(int goalIndex)
    {
        var goal = _goals[goalIndex];
        var nearestDistance = float.MaxValue;

        if (goalIndex > 0)
        {
            nearestDistance = Mathf.Min(
                nearestDistance,
                Mathf.Abs(goal.transform.position.x -
                          _goals[goalIndex - 1].transform.position.x));
        }

        if (goalIndex + 1 < _goals.Count)
        {
            nearestDistance = Mathf.Min(
                nearestDistance,
                Mathf.Abs(goal.transform.position.x -
                          _goals[goalIndex + 1].transform.position.x));
        }

        return nearestDistance;
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Pending) return;

        _remainingSafetyNetCount = _safetyNetCount;
        _remainingSwapCount = _swapCount;
        _pendingSwapGoalIndex = -1;
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
