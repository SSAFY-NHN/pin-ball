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
    public event Action<BattleUnitSpawnData> OnGoalReached;

    public int CurrentLaunchCost => _launchState.CurrentCost;
    public bool HasAvailableBall => _ballPool?.HasAvailableBall ?? false;
    public bool HasActiveBalls => _ballPool?.HasActiveBalls ?? false;
    public IReadOnlyCollection<Pinball> ActiveBalls =>
        _ballPool?.ActiveBalls ?? Array.Empty<Pinball>();

    [Header("Launcher")]
    [SerializeField] private Vector2 launchPosition = new(6.4f, 10f);
    [SerializeField, Min(0f)] private float minimumLaunchSpeed = 5.5f;
    [SerializeField, Min(0f)] private float maximumLaunchSpeed = 8f;
    [SerializeField] private PinballLauncherController launcherController;

    [SerializeField] private List<Pinball> pooledBalls = new();

    private BattleManager _battleManager;
    private UnitManager _unitManager;
    private ItemManager _itemManager;

    private Vector2 _currentLaunchPosition;
    private PinballBallPool _ballPool;
    private PinballLaunchState _launchState = new(50, 30);
    private readonly PinballItemModifiers _itemModifiers = new();
    private readonly PinballGoalController _goalController = new();

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _unitManager = App.Get<UnitManager>();
        _itemManager = App.Get<ItemManager>();

        int baseLaunchCost = 50;
        int launchCostIncrease = 30;
        var runCommon = App.Get<TitleData>().BattleRunCommon;
        if (runCommon != null)
        {
            baseLaunchCost = runCommon.BaseLaunchCost;
            launchCostIncrease = runCommon.LaunchCostIncrease;
        }

        _launchState = new PinballLaunchState(
            baseLaunchCost,
            launchCostIncrease);
        SubscribeItems();
        _battleManager.OnStateChanged += OnBattleStateChanged;

        _ballPool = new PinballBallPool(pooledBalls);
        LoadNextBall();
        NotifyLaunchCostChanged();
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
        if (_ballPool?.LoadedBall != null)
        {
            _ballPool.LoadedBall.LoadAt(position);
        }

        TryLaunchLoadedBall(1f);
    }

    public bool TryLaunchLoadedBall(float normalizedPull)
    {
        if (_battleManager == null ||
            !_battleManager.CanUsePreparationActions) return false;
        if (_ballPool?.LoadedBall == null) return false;

        var cost = CurrentLaunchCost;
        if (!_battleManager.TrySpendPreparationGold(cost)) return false;

        if (!_ballPool.TryLaunchLoaded(out var ball)) return false;
        var direction = launcherController != null
            ? launcherController.LaunchDirection
            : Vector2.up;
        var speed = Mathf.Lerp(
            minimumLaunchSpeed,
            maximumLaunchSpeed,
            Mathf.Clamp01(normalizedPull));
        ball.LaunchLoaded(direction.normalized * speed);
        ball.PlayLaunchCameraFeedback(normalizedPull);
        _launchState.RecordSuccessfulLaunch();
        NotifyLaunchCostChanged();
        SoundManager.PlaySFXIfAvailable(SoundName.Spring);
        OnStateChanged?.Invoke(EPinballState.Launched);
        return true;
    }

    internal void MoveLoadedBall(Vector2 position)
    {
        if (_ballPool?.LoadedBall == null) return;
        _ballPool.LoadedBall.LoadAt(position);
        launchPosition = position;
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
        int baseReward = _itemModifiers.GoldenBallReward;
        _battleManager.AddGold(baseReward);
        int totalReward = baseReward + ApplyGoldenBumper(ball);
        ball.PlayGoldRewardFeedback(hitPosition, totalReward);
        ApplySplitCapsule(ball);
    }

    internal void OnBallHitSurface()
    {
        SoundManager.PlaySFXIfAvailable(SoundName.BallHit);
    }

    internal void ApplyTargetMagnet(Pinball ball)
    {
        if (_itemModifiers.TargetMagnetCount <= 0 ||
            ball.TargetMagnetUseCount >=
            _itemModifiers.TargetMagnetCount) return;

        var goal = _goalController.SelectedGoal;
        if (goal == null) return;

        var maxDistance =
            ball.Diameter *
            _itemModifiers.TargetMagnetDistanceMultiplier;
        var offset = goal.transform.position - ball.transform.position;
        if (offset.sqrMagnitude > maxDistance * maxDistance) return;

        var velocity = ball.Velocity;
        var correction =
            Mathf.Abs(velocity.x) *
            _itemModifiers.TargetMagnetStrength;
        if (correction <= 0.001f)
        {
            correction =
                velocity.magnitude *
                _itemModifiers.TargetMagnetStrength;
        }

        velocity.x += Mathf.Sign(offset.x) * correction;
        ball.SetVelocity(velocity);
        ball.TargetMagnetUseCount++;
    }

    public void OnGoalBall(Pinball ball, PinballGoal goal)
    {
        if (ball == null || goal == null) return;

        var goalUnitData = goal.UnitData;
        var unitData = new BattleUnitSpawnData
        {
            UnitId = goalUnitData.UnitId,
            Level = goalUnitData.Level
        };
        OnGoalReached?.Invoke(unitData);

        var attackBonus =
            _itemModifiers.CalculateChargedPinAttackBonus(
                ball.SmallPinHitCount);

        _unitManager.SpawnAlly(unitData, attackBonus);

        if (_itemModifiers.CanApplyOverload(
                ball.BigBumperHitCount,
                ball.OverloadUseCount))
        {
            for (var i = 0;
                 i < _itemModifiers.OverloadSpawnCount;
                 i++)
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
        if (_ballPool == null ||
            !_ballPool.Release(ball, out bool hasNoActiveBalls))
        {
            return;
        }

        if (hasNoActiveBalls)
        {
            OnStateChanged?.Invoke(EPinballState.Idle);
            LoadNextBall();
        }
    }

    internal void RegisterGoal(PinballGoal goal)
    {
        _goalController.Register(goal);
    }

    internal void UnregisterGoal(PinballGoal goal)
    {
        _goalController.Unregister(goal);
    }

    internal void SelectGoal(PinballGoal goal)
    {
        if (_battleManager == null ||
            !_battleManager.CanUsePreparationActions) return;

        _goalController.Select(goal);
    }

    internal void SelectSwapGoal(PinballGoal goal)
    {
        if (_battleManager == null ||
            !_battleManager.CanUsePreparationActions) return;

        _goalController.SelectSwap(
            goal,
            _ballPool?.HasActiveBalls ?? false);
    }

    public void OnItemEvent(Item item)
    {
        _itemModifiers.Apply(item);

        switch (item.Key)
        {
            case EItem.AutoBallFeeder:
                _launchState.SetCostModifiers(
                    _itemModifiers.LaunchCostDiscount,
                    _itemModifiers.MinimumLaunchCost);
                NotifyLaunchCostChanged();
                break;
            case EItem.FocusedPocket:
                _goalController.SetFocusedPocket(
                    _itemModifiers.FocusedPocketBonus,
                    _itemModifiers.OtherPocketPenalty);
                break;
            case EItem.SwapLever:
                _goalController.SetSwapCount(
                    _itemModifiers.SwapCount);
                break;
        }
    }

    private int ApplyGoldenBumper(Pinball ball)
    {
        int reward =
            _itemModifiers.CalculateGoldenBumperReward(
                ball.GoldenBumperGold);
        if (reward <= 0) return 0;

        ball.GoldenBumperGold += reward;
        _battleManager.AddGold(reward);
        return reward;
    }

    private void ApplySplitCapsule(Pinball source)
    {
        if (_itemModifiers.SplitCount <= 0 ||
            source.IsClone ||
            source.HasSplit)
        {
            return;
        }

        source.HasSplit = true;
        for (var i = 0; i < _itemModifiers.SplitCount; i++)
        {
            if (_ballPool == null ||
                !_ballPool.TryAcquireActive(out var clone))
            {
                break;
            }

            clone.Activate(
                source.transform.position,
                source.Velocity.normalized,
                true);
            clone.SetVelocity(
                source.Velocity *
                _itemModifiers.SplitSpeedMultiplier);
        }
    }

    private void OnBattleStateChanged(EWaveState state)
    {
        if (state != EWaveState.Pending) return;

        _launchState.ResetSuccessfulLaunches();
        _goalController.ResetForPreparation();
        if (_ballPool == null || !_ballPool.HasActiveBalls)
        {
            LoadNextBall();
        }
        NotifyLaunchCostChanged();
    }

    private void LoadNextBall()
    {
        if (_ballPool == null ||
            !_ballPool.TryLoadNext(out var ball))
        {
            return;
        }

        var position = launcherController != null
            ? launcherController.LoadPosition
            : launchPosition;
        launchPosition = position;
        ball.LoadAt(position);
        ball.PlayLoadedFeedback();
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
