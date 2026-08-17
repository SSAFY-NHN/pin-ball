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
    public event Action<int> OnComboChanged;
    public event Action<Pinball, int> OnJackpotTriggered;
    public event Action OnProductionChanged;
    public event Action<PinballBallActivatedData> OnPermanentBallActivated;
    public event Action<PinballBallReleasedData> OnPermanentBallReleased;
    public event Action<PinballBumperRewardData> OnBumperRewarded;
    public event Action<PinballUpgradePurchasedData> OnProductionUpgradePurchased;

    public int CurrentLaunchCost => _launchState.CurrentCost;
    public bool HasAvailableBall => false;
    public bool HasActiveBalls => _ballPool?.HasActiveBalls ?? false;
    public int CurrentCombo => _comboController.Count;
    public float CurrentComboProgress =>
        _comboController.GetRemainingProgress(Time.unscaledTime);
    public float CurrentComboMultiplier => _comboController.GetRewardMultiplier(
        comboHitsPerStep,
        comboMultiplierPerStep,
        maximumComboMultiplier);
    public IReadOnlyCollection<Pinball> ActiveBalls =>
        _ballPool?.ActiveBalls ?? Array.Empty<Pinball>();
    public int BumperIncome => _productionUpgradeController?.BumperIncome ?? 1;
    public int PermanentBallCount =>
        _productionUpgradeController?.PermanentBallCount ?? 1;
    public float RespawnDelay =>
        _productionUpgradeController?.RespawnDelay ?? 3f;
    public int ActiveCloneCount => _ballPool?.ActiveCloneCount ?? 0;

    [Header("Launcher")]
    [SerializeField] private Vector2 launchPosition = new(6.4f, 10f);
    [SerializeField, Min(0f)] private float minimumLaunchSpeed = 5.5f;
    [SerializeField, Min(0f)] private float maximumLaunchSpeed = 8f;
    [SerializeField] private PinballLauncherController launcherController;

    [Header("Automatic Cycle")]
    [SerializeField] private Transform autoSpawnPoint;
    [SerializeField] private Vector2 autoSpawnDirection = Vector2.down;
    [SerializeField] private List<Pinball> permanentBalls = new();
    [SerializeField] private List<Pinball> cloneBalls = new();

    [Header("Production Upgrades")]
    [SerializeField] private PinballProductionUpgradeSettings bumperIncomeSettings =
        new(1, 1, 25, 1.6f, 20);
    [SerializeField] private PinballProductionUpgradeSettings addBallSettings =
        new(1, 1, 150, 2f, 9);
    [SerializeField] private PinballProductionUpgradeSettings supplySpeedSettings =
        new(3f, -0.25f, 50, 1.7f, 9);
    [SerializeField, Min(0.01f)] private float minimumRespawnDelay = 0.75f;

    [Header("Golden Ball")]
    // TODO: Tune golden ball values after production-rate measurement.
    [SerializeField, Range(0f, 1f)] private float goldenChance = 0.05f;
    [SerializeField, Min(1f)] private float goldenRewardMultiplier = 3f;

    [Header("Combo Reward")]
    [SerializeField, Min(1)] private int comboHitsPerStep = 2;
    [SerializeField, Min(0f)] private float comboMultiplierPerStep = 0.5f;
    [SerializeField, Min(1f)] private float maximumComboMultiplier = 2f;

    [Header("Jackpot")]
    // TODO: Tune jackpot values against the 30-90 second production target.
    [SerializeField, Min(1)] private int jackpotRequiredCombo = 5;
    [SerializeField, Min(0)] private int jackpotBaseReward = 100;
    [SerializeField, Min(0f)] private float jackpotIncomeMultiplier = 30f;

    private BattleManager _battleManager;
    private ItemManager _itemManager;

    private Vector2 _currentLaunchPosition;
    private PinballBallPool _ballPool;
    private PinballLaunchState _launchState = new(50, 30);
    private readonly PinballItemModifiers _itemModifiers = new();
    private readonly PinballGoalController _goalController = new();
    private readonly PinballComboController _comboController = new();
    private PinballRewardController _rewardController;
    private PinballProductionUpgradeController _productionUpgradeController;
    private readonly PinballAutoCycleController _autoCycleController = new();
    private bool _isRunInitialized;

    private void Start()
    {
        InitializeNewRun();
    }

    private void Update()
    {
        if (_comboController.TryExpire(Time.unscaledTime))
        {
            OnComboChanged?.Invoke(0);
        }

        while (_autoCycleController.TryTakeReady(Time.time, out var ball))
        {
            ReactivateBall(ball);
        }
    }

    internal void InitializeNewRun()
    {
        if (_isRunInitialized) return;
        _isRunInitialized = true;

        _battleManager = App.Get<BattleManager>();
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
        _ballPool = new PinballBallPool(permanentBalls, cloneBalls);
        _productionUpgradeController = new PinballProductionUpgradeController(
            bumperIncomeSettings,
            addBallSettings,
            supplySpeedSettings,
            minimumRespawnDelay);
        _rewardController = new PinballRewardController(
            _battleManager,
            _ballPool,
            _itemModifiers);
        ResetForNewRun();
        SubscribeItems();
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
    }

    public void LaunchBall(Vector2 position)
    {
    }

    public bool TryLaunchLoadedBall(float normalizedPull)
    {
        return false;
    }

    internal void MoveLoadedBall(Vector2 position)
    {
    }

    private void NotifyLaunchCostChanged()
    {
        OnLaunchCostChanged?.Invoke(CurrentLaunchCost);
    }

    internal void OnBallHit(
        Pinball ball,
        PinballObstacle obstacle,
        Vector2 hitPosition)
    {
        if (ball == null || obstacle == null) return;

        if (obstacle.Type == EPinballObstacle.SmallPin)
        {
            SoundManager.PlaySFXIfAvailable(SoundName.SmallPinHit);
            ball.SmallPinHitCount++;
            return;
        }

        SoundManager.PlaySFXIfAvailable(SoundName.BumperHit);
        ball.BigBumperHitCount++;
        int combo = _comboController.RegisterBumperHit(Time.unscaledTime);
        float comboMultiplier = CurrentComboMultiplier;
        bool grantsJackpot =
            !ball.IsClone &&
            ball.IsGolden &&
            !ball.HasTriggeredJackpot &&
            combo >= Mathf.Max(1, jackpotRequiredCombo) &&
            obstacle.IsJackpotBumper;
        if (grantsJackpot) ball.HasTriggeredJackpot = true;

        PinballRewardResult reward = _rewardController.ApplyBumperReward(
            ball,
            _productionUpgradeController.BumperIncome,
            comboMultiplier,
            goldenRewardMultiplier,
            grantsJackpot,
            jackpotBaseReward,
            jackpotIncomeMultiplier);
        int normalReward = reward.TotalReward - reward.JackpotReward;
        OnBumperRewarded?.Invoke(new PinballBumperRewardData(
            ball.IsClone,
            normalReward,
            reward.JackpotReward));
        ball.PlayGoldRewardFeedback(hitPosition, normalReward);
        OnComboChanged?.Invoke(combo);

        if (!grantsJackpot) return;

        ball.PlayJackpotFeedback(hitPosition, reward.JackpotReward);
        obstacle.PlayJackpotFeedback();
        OnJackpotTriggered?.Invoke(ball, reward.JackpotReward);
        // TODO: Play a dedicated jackpot SFX when a suitable project asset exists.
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
        ReleaseBall(ball);
    }

    public void OnMissedBall(Pinball ball)
    {
        if (ball == null) return;
        ReleaseBall(ball);
    }

    public void ReleaseBall(Pinball ball)
    {
        if (_ballPool == null) return;
        int bumperHitCount = ball != null ? ball.BigBumperHitCount : 0;
        EPinballReleaseType releaseType = _ballPool.Release(ball);
        if (releaseType == EPinballReleaseType.Permanent)
        {
            OnPermanentBallReleased?.Invoke(new PinballBallReleasedData(
                bumperHitCount));
            _autoCycleController.Schedule(ball, Time.time + RespawnDelay);
        }
    }

    public int GetProductionLevel(EPinballProductionUpgrade upgrade)
    {
        return _productionUpgradeController?.GetLevel(upgrade) ?? 0;
    }

    public int GetProductionMaxLevel(EPinballProductionUpgrade upgrade)
    {
        return _productionUpgradeController?.GetMaxLevel(upgrade) ?? 0;
    }

    public int GetProductionCost(EPinballProductionUpgrade upgrade)
    {
        return _productionUpgradeController?.GetNextCost(upgrade) ?? 0;
    }

    public float GetProductionEffect(EPinballProductionUpgrade upgrade)
    {
        return _productionUpgradeController?.GetEffect(upgrade) ?? 0f;
    }

    public float GetNextProductionEffect(EPinballProductionUpgrade upgrade)
    {
        return _productionUpgradeController?.GetNextEffect(upgrade) ?? 0f;
    }

    public bool CanPurchaseProductionUpgrade(EPinballProductionUpgrade upgrade)
    {
        if (_battleManager == null || _productionUpgradeController == null)
        {
            return false;
        }

        if (upgrade == EPinballProductionUpgrade.AddBall &&
            PermanentBallCount >= permanentBalls.Count)
        {
            return false;
        }

        return _productionUpgradeController.CanPurchase(
            upgrade,
            _battleManager.Gold);
    }

    public bool TryPurchaseProductionUpgrade(EPinballProductionUpgrade upgrade)
    {
        if (!CanPurchaseProductionUpgrade(upgrade)) return false;
        int cost = _productionUpgradeController.GetNextCost(upgrade);
        if (!_battleManager.TrySpendGold(cost)) return false;
        if (!_productionUpgradeController.TryPurchase(upgrade, int.MaxValue))
        {
            _battleManager.AddGold(cost);
            return false;
        }

        if (upgrade == EPinballProductionUpgrade.AddBall)
        {
            SpawnNextPermanentBall();
        }

        OnProductionChanged?.Invoke();
        OnProductionUpgradePurchased?.Invoke(new PinballUpgradePurchasedData(
            upgrade,
            _productionUpgradeController.GetLevel(upgrade),
            cost));
        return true;
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


    private void SpawnInitialBall()
    {
        if (_ballPool == null ||
            !_ballPool.TryAcquirePermanent(out var ball)) return;

        ActivateAtSpawnPoint(ball);
    }

    private void ReactivateBall(Pinball ball)
    {
        if (_ballPool == null || !_ballPool.TryReactivatePermanent(ball)) return;
        ActivateAtSpawnPoint(ball);
    }

    private void ActivateAtSpawnPoint(Pinball ball)
    {
        var position = autoSpawnPoint != null
            ? (Vector2)autoSpawnPoint.position
            : launchPosition;
        bool isGolden = UnityEngine.Random.value < Mathf.Clamp01(goldenChance);
        ball.Activate(position, autoSpawnDirection, false, isGolden);
        OnPermanentBallActivated?.Invoke(new PinballBallActivatedData(isGolden));
    }

    private void SpawnNextPermanentBall()
    {
        if (_ballPool == null ||
            !_ballPool.TryAcquirePermanent(out var ball)) return;
        ActivateAtSpawnPoint(ball);
    }

    private void LoadNextBall()
    {
    }

    internal void ResetForNewRun()
    {
        _ballPool?.ResetForNewRun();
        _autoCycleController.Reset();
        _productionUpgradeController?.ResetForNewRun();
        _launchState.ResetForNewRun();
        _goalController.ResetForNewRun();
        _comboController.Reset();
        _itemModifiers.ResetForNewRun();
        launcherController?.SetLoaded(false);
        SpawnInitialBall();
        NotifyLaunchCostChanged();
        OnProductionChanged?.Invoke();
    }

    protected override void OnDestroy()
    {
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

public readonly struct PinballBallActivatedData
{
    public bool IsGolden { get; }

    public PinballBallActivatedData(bool isGolden)
    {
        IsGolden = isGolden;
    }
}

public readonly struct PinballBallReleasedData
{
    public int BumperHitCount { get; }

    public PinballBallReleasedData(int bumperHitCount)
    {
        BumperHitCount = bumperHitCount;
    }
}

public readonly struct PinballBumperRewardData
{
    public bool IsClone { get; }
    public int NormalGold { get; }
    public int JackpotGold { get; }

    public PinballBumperRewardData(
        bool isClone,
        int normalGold,
        int jackpotGold)
    {
        IsClone = isClone;
        NormalGold = normalGold;
        JackpotGold = jackpotGold;
    }
}

public readonly struct PinballUpgradePurchasedData
{
    public EPinballProductionUpgrade Upgrade { get; }
    public int Level { get; }
    public int Cost { get; }

    public PinballUpgradePurchasedData(
        EPinballProductionUpgrade upgrade,
        int level,
        int cost)
    {
        Upgrade = upgrade;
        Level = level;
        Cost = cost;
    }
}
