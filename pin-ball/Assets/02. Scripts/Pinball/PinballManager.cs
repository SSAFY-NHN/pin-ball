using System;
using System.Collections.Generic;

using UnityEngine;

public class PinballManager : AppService, IItemEventListener
{
    private static readonly EItem[] SupportedItems =
    {
        EItem.GoldenBall,
        EItem.AutoBallFeeder,
        EItem.TargetMagnet,
        EItem.SplitCapsule,
        EItem.GoldenBumper,
        EItem.FocusedPocket,
        EItem.SwapLever,
        EItem.ChargedPin,
        EItem.OverloadBumper
    };

    public event Action<EPinballState> OnStateChanged;
    public event Action<int> OnLaunchCostChanged;

    public int CurrentLaunchCost => CalculateLaunchCost();
    public bool HasAvailableBall => _loadedBall != null || _availableBalls.Count > 0;
    public bool HasActiveBalls => _activeBalls.Count > 0;
    public IReadOnlyCollection<Pinball> ActiveBalls => _activeBalls;

    [Header("Launcher")]
    [SerializeField] private Vector2 launchPosition = new(6.4f, 10f);
    [SerializeField, Min(0f)] private float minimumLaunchSpeed = 5.5f;
    [SerializeField, Min(0f)] private float maximumLaunchSpeed = 8f;
    [SerializeField] private PinballLauncherController launcherController;

    [SerializeField] private List<Pinball> pooledBalls = new();

    private readonly Queue<Pinball> _availableBalls = new();
    private readonly HashSet<Pinball> _activeBalls = new();
    private readonly List<PinballGoal> _goals = new();

    private BattleManager _battleManager;
    private UnitManager _unitManager;
    private ItemManager _itemManager;

    private int _baseLaunchCost = 50;
    private int _launchCostIncrease = 30;
    private int _successfulLaunchCount;
    private Pinball _loadedBall;

    private Vector2 _currentLaunchPosition;
    private int _selectedGoalIndex;
    private int _pendingSwapGoalIndex = -1;

    private int _goldenBallReward = 1;
    private int _launchCostDiscount;
    private int _minimumLaunchCost;
    private float _targetMagnetDistanceMultiplier;
    private float _targetMagnetStrength;
    private int _targetMagnetCount;
    private int _splitCount;
    private float _splitSpeedMultiplier;
    private int _goldenBumperReward;
    private int _goldenBumperMaxReward;
    private float _focusedPocketBonus;
    private float _otherPocketPenalty;
    private int _swapCount;
    private int _chargedPinRequiredHits;
    private float _chargedPinAttackBonus;
    private int _overloadRequiredHits;
    private int _overloadSpawnCount;
    private int _overloadMaxCount;

    private int _remainingSwapCount;

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _unitManager = App.Get<UnitManager>();
        _itemManager = App.Get<ItemManager>();

        var runCommon = App.Get<TitleData>().BattleRunCommon;
        if (runCommon != null)
        {
            _baseLaunchCost = Mathf.Max(0, runCommon.BaseLaunchCost);
            _launchCostIncrease = Mathf.Max(0, runCommon.LaunchCostIncrease);
        }

        SubscribeItems();
        _battleManager.OnStateChanged += OnBattleStateChanged;

        PrepareBallPool();
        LoadNextBall();
        NotifyLaunchCostChanged();
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

    public void LaunchBall()
    {
        TryLaunchLoadedBall(1f);
    }

    public void LaunchBall(Vector2 position)
    {
        launchPosition = position;
        if (_loadedBall != null)
        {
            _loadedBall.LoadAt(position);
        }

        TryLaunchLoadedBall(1f);
    }

    public bool TryLaunchLoadedBall(float normalizedPull)
    {
        if (_battleManager == null ||
            !_battleManager.CanUsePreparationActions) return false;
        if (_loadedBall == null) return false;

        var cost = CurrentLaunchCost;
        if (!_battleManager.TrySpendPreparationGold(cost)) return false;

        var ball = _loadedBall;
        _loadedBall = null;
        var direction = launcherController != null
            ? launcherController.LaunchDirection
            : Vector2.up;
        var speed = Mathf.Lerp(
            minimumLaunchSpeed,
            maximumLaunchSpeed,
            Mathf.Clamp01(normalizedPull));
        ball.LaunchLoaded(direction.normalized * speed);
        ball.PlayLaunchCameraFeedback(normalizedPull);
        _activeBalls.Add(ball);
        _successfulLaunchCount++;
        NotifyLaunchCostChanged();
        SoundManager.PlaySFXIfAvailable(SoundName.Spring);
        OnStateChanged?.Invoke(EPinballState.Launched);
        return true;
    }

    internal void MoveLoadedBall(Vector2 position)
    {
        if (_loadedBall == null) return;
        _loadedBall.LoadAt(position);
        launchPosition = position;
    }

    private int CalculateLaunchCost()
    {
        int escalatedCost =
            _baseLaunchCost + _successfulLaunchCount * _launchCostIncrease;
        int discountedCost = Mathf.Max(
            0,
            escalatedCost - _launchCostDiscount);
        return Mathf.Max(_minimumLaunchCost, discountedCost);
    }

    private void NotifyLaunchCostChanged()
    {
        OnLaunchCostChanged?.Invoke(CurrentLaunchCost);
    }

    internal void OnBallHit(
        Pinball ball,
        EPinballObstacle obstacle,
        Vector2 hitPosition)
    {
        if (ball == null) return;

        if (obstacle == EPinballObstacle.SmallPin)
        {
            SoundManager.PlaySFXIfAvailable(SoundName.SmallPinHit);
            ball.SmallPinHitCount++;
            return;
        }

        SoundManager.PlaySFXIfAvailable(SoundName.BumperHit);
        ball.BigBumperHitCount++;
        _battleManager.AddGold(_goldenBallReward);
        int totalReward = _goldenBallReward + ApplyGoldenBumper(ball);
        ball.PlayGoldRewardFeedback(hitPosition, totalReward);
        ApplySplitCapsule(ball);
    }

    internal void OnBallHitSurface()
    {
        SoundManager.PlaySFXIfAvailable(SoundName.BallHit);
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

        ReleaseBall(ball);
    }

    public void OnMissedBall(Pinball ball)
    {
        if (ball == null) return;
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
            LoadNextBall();
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
        if (_battleManager == null ||
            !_battleManager.CanUsePreparationActions) return;

        var goalIndex = _goals.IndexOf(goal);
        if (goalIndex < 0) return;

        _selectedGoalIndex = goalIndex;
        RefreshGoalWidths();
    }

    internal void SelectSwapGoal(PinballGoal goal)
    {
        if (_battleManager == null ||
            !_battleManager.CanUsePreparationActions) return;

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
            case EItem.GoldenBall:
                _goldenBallReward = Mathf.Max(1, Mathf.RoundToInt(item.Value1));
                break;
            case EItem.AutoBallFeeder:
                _launchCostDiscount = Mathf.RoundToInt(item.Value1);
                _minimumLaunchCost = Mathf.RoundToInt(item.Value2);
                NotifyLaunchCostChanged();
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
            case EItem.GoldenBumper:
                _goldenBumperReward = Mathf.RoundToInt(item.Value1);
                _goldenBumperMaxReward = Mathf.RoundToInt(item.Value2);
                break;
            case EItem.FocusedPocket:
                _focusedPocketBonus = item.Value1;
                _otherPocketPenalty = item.Value2;
                RefreshGoalWidths();
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

    private int ApplyGoldenBumper(Pinball ball)
    {
        if (_goldenBumperReward <= 0 ||
            ball.GoldenBumperGold >= _goldenBumperMaxReward) return 0;

        var reward = Mathf.Min(
            _goldenBumperReward,
            _goldenBumperMaxReward - ball.GoldenBumperGold);

        ball.GoldenBumperGold += reward;
        _battleManager.AddGold(reward);
        return reward;
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
                true);
            clone.SetVelocity(source.Velocity * _splitSpeedMultiplier);
            _activeBalls.Add(clone);
        }
    }

    private void RefreshGoalWidths()
    {
        for (var i = 0; i < _goals.Count; i++)
        {
            var multiplier = 1f;
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

        _successfulLaunchCount = 0;
        _remainingSwapCount = _swapCount;
        _pendingSwapGoalIndex = -1;
        RefreshGoalWidths();
        if (_activeBalls.Count <= 0)
        {
            LoadNextBall();
        }
        NotifyLaunchCostChanged();
    }

    private void LoadNextBall()
    {
        if (_loadedBall != null || _availableBalls.Count <= 0) return;

        _loadedBall = _availableBalls.Dequeue();
        var position = launcherController != null
            ? launcherController.LoadPosition
            : launchPosition;
        launchPosition = position;
        _loadedBall.LoadAt(position);
        _loadedBall.PlayLoadedFeedback();
        launcherController?.SetLoaded(true);
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
