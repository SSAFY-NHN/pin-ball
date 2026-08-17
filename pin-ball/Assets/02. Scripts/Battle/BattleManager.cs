using System;
using System.Collections;
using UnityEngine;

// 소유: 전투 단계, 방어선 HP, 골드, 지속 전투 상태
// 책임: 자동 단계 시작/전환/재정비 결정과 UI 이벤트 발행
// 금지: 유닛 탐색/이동/공격, Instantiate 직접 처리
public class BattleManager : AppService, IItemEventListener
{
    [Header("Defense Line")]
    [SerializeField, Min(1)] public int playerMaxHp = 20;
    [SerializeField, Range(0f, 1f)] private float recoveryHpRatio = 1f;

    [Header("Stage Transition")]
    [SerializeField, Min(0f)] private float stageTransitionDuration = 2f;
    [SerializeField, Min(0f)] private float recoveryDuration = 3f;

    [Header("Enemy Stage Prototype")]
    // TODO: 플레이 테스트 후 프로토타입 적 수와 증가 구간을 조정한다.
    [SerializeField] private string stageEnemyId = "goblin";
    [SerializeField, Min(1)] private int baseEnemyCount = 3;
    [SerializeField, Min(1)] private int enemyCountGrowthInterval = 3;
    [SerializeField, Min(0)] private int enemyCountGrowthAmount = 1;
    [SerializeField, Min(1)] private int maximumEnemyCount = 10;

    [Header("Battle Upgrades")]
    // TODO: 플레이 테스트 후 프로토타입 비용과 효과 수치를 조정한다.
    [SerializeField] private BattleUnitSpawnData purchasedAlly = new()
    {
        UnitId = "warrior",
        Level = 1
    };
    [SerializeField, Min(0)] private int unitPurchaseBaseCost = 100;
    [SerializeField, Min(1f)] private float unitPurchaseCostMultiplier = 1.8f;
    [SerializeField] private BattleUpgradeSettings allyAttackSettings =
        new(1f, 0.25f, 75, 1.7f, 20);
    [SerializeField] private BattleUpgradeSettings defenseLineHpSettings =
        new(0f, 10f, 80, 1.7f, 20);

    public int CurrentWaveNumber => stageController?.CurrentStage ?? 1;
    public int CurrentStageNumber => CurrentWaveNumber;
    public int PlayerHp => runState?.PlayerHp ?? playerMaxHp;
    public int MaximumPlayerHp => runState?.MaximumPlayerHp ?? playerMaxHp;
    public int Gold => economy?.Gold ?? 0;
    public bool IsInitialized { get; private set; }
    public EWaveState State => stageController?.State ?? EWaveState.Starting;
    public bool IsPreparationPhase => State != EWaveState.Active;
    public bool CanUsePreparationActions =>
        IsPreparationPhase && !isPreparationLocked;

    public event Action<EWaveState> OnStateChanged;
    public event Action OnInitialized;
    public event Action<int> OnWaveChanged;
    public event Action<int> OnHpChanged;
    public event Action<int> OnGoldChanged;
    public event Action OnBattleUpgradeChanged;
    public event Action<bool> OnPreparationAvailabilityChanged;
    public event Action<EWaveResolutionResult, int> OnWaveResolutionStarted;

    private UnitManager unitManager;
    private BattleRunState runState;
    private BattleStageController stageController;
    private EnemyStageScalingController enemyScalingController;
    private BattleEconomy economy = new(0);
    private BattleUpgradeController battleUpgradeController;
    private int barrierDamageReduction;
    private int minimumBarrierDamage = 1;
    private Coroutine transitionCoroutine;
    private bool isRunInitialized;
    private bool isPreparationLocked;

    private void Start()
    {
        InitializeNewRun();
    }

    internal void InitializeNewRun()
    {
        if (isRunInitialized) return;
        isRunInitialized = true;

        unitManager = App.Get<UnitManager>();
        unitManager.InitializeNewRun();
        unitManager.OnBattleRosterChanged += OnBattleRosterChanged;

        var titleData = App.Get<TitleData>();
        runState = new BattleRunState(playerMaxHp);
        stageController = new BattleStageController();
        enemyScalingController = new EnemyStageScalingController(
            baseEnemyCount,
            enemyCountGrowthInterval,
            enemyCountGrowthAmount,
            maximumEnemyCount);
        economy = new BattleEconomy(
            titleData.BattleRunCommon?.StartingGold ?? 0);
        battleUpgradeController = new BattleUpgradeController(
            new BattleUpgradeSettings(
                0f,
                1f,
                unitPurchaseBaseCost,
                unitPurchaseCostMultiplier,
                UnitManager.MaxDeployedAllyCount),
            allyAttackSettings,
            defenseLineHpSettings);

        App.Get<ItemManager>().Subscribe(EItem.BarrierReinforcement, this);

        IsInitialized = true;
        OnInitialized?.Invoke();
        OnStateChanged?.Invoke(State);
        OnHpChanged?.Invoke(PlayerHp);
        OnGoldChanged?.Invoke(Gold);

        StartCurrentStage();
    }

    private void StartCurrentStage()
    {
        if (unitManager == null || unitManager.DeployedAllyCount <= 0)
        {
            Debug.LogError(
                "[BattleManager] 지속 전투를 시작할 기본 아군이 없습니다.");
            return;
        }

        bool canStart = State == EWaveState.Starting
            ? stageController.TryStart()
            : State == EWaveState.Active;
        if (!canStart) return;

        int enemyCount = enemyScalingController.CalculateEnemyCount(
            CurrentStageNumber);
        unitManager.BeginStage(stageEnemyId, enemyCount, CurrentStageNumber);
        OnStateChanged?.Invoke(State);
        OnWaveChanged?.Invoke(CurrentStageNumber);
        SoundManager.PlaySFXIfAvailable(SoundName.WaveStart);
    }

    private void OnBattleRosterChanged()
    {
        if (State != EWaveState.Active) return;

        if (BattleResolutionPolicy.TryDetectWipe(
                unitManager.RemainingAllyCount,
                unitManager.RemainingEnemyCount,
                out EWaveResolutionResult result))
        {
            BeginStageTransition(result);
        }
    }

    private void BeginStageTransition(EWaveResolutionResult result)
    {
        float duration = result == EWaveResolutionResult.Cleared
            ? stageTransitionDuration
            : recoveryDuration;
        if (!stageController.TryBeginTransition(
                result,
                Time.time,
                duration)) return;

        if (result == EWaveResolutionResult.Failed)
        {
            int damage = BarrierDamageCalculator.Calculate(
                unitManager.CalculateRemainingBreachDamage(),
                barrierDamageReduction,
                minimumBarrierDamage);
            runState.ApplyPlayerDamage(damage);
            OnHpChanged?.Invoke(PlayerHp);
        }

        unitManager.ResolveStageResult();
        OnStateChanged?.Invoke(State);
        OnWaveResolutionStarted?.Invoke(result, CurrentStageNumber);
        SoundManager.PlaySFXIfAvailable(
            result == EWaveResolutionResult.Cleared
                ? SoundName.WaveWin
                : SoundName.WaveFailed);

        transitionCoroutine = StartCoroutine(WaitForTransition(duration));
    }

    private IEnumerator WaitForTransition(float duration)
    {
        yield return new WaitForSeconds(duration);
        transitionCoroutine = null;

        bool wasRecovering = State == EWaveState.Recovering;
        if (!stageController.TryCompleteTransition(Time.time)) yield break;

        if (wasRecovering)
        {
            runState.RestorePlayerHp(recoveryHpRatio);
            OnHpChanged?.Invoke(PlayerHp);
        }

        StartCurrentStage();
    }

    public bool TrySpendGold(int amount)
    {
        int previousGold = economy.Gold;
        if (!economy.TrySpend(amount)) return false;
        if (economy.Gold != previousGold)
        {
            OnGoldChanged?.Invoke(economy.Gold);
        }
        return true;
    }

    public int GetBattleUpgradeLevel(EBattleUpgrade upgrade)
    {
        return upgrade == EBattleUpgrade.UnitPurchase
            ? unitManager?.DeployedAllyCount ?? 0
            : battleUpgradeController?.GetLevel(upgrade) ?? 0;
    }

    public int GetBattleUpgradeMaxLevel(EBattleUpgrade upgrade)
    {
        return battleUpgradeController?.GetMaxLevel(upgrade) ?? 0;
    }

    public int GetBattleUpgradeCost(EBattleUpgrade upgrade)
    {
        return battleUpgradeController?.GetNextCost(upgrade) ?? 0;
    }

    public float GetBattleUpgradeEffect(EBattleUpgrade upgrade)
    {
        return upgrade == EBattleUpgrade.UnitPurchase
            ? unitManager?.DeployedAllyCount ?? 0
            : battleUpgradeController?.GetEffect(upgrade) ?? 0f;
    }

    public float GetNextBattleUpgradeEffect(EBattleUpgrade upgrade)
    {
        return upgrade == EBattleUpgrade.UnitPurchase
            ? Mathf.Min(
                UnitManager.MaxDeployedAllyCount,
                (unitManager?.DeployedAllyCount ?? 0) + 1)
            : battleUpgradeController?.GetNextEffect(upgrade) ?? 0f;
    }

    public bool CanPurchaseBattleUpgrade(EBattleUpgrade upgrade)
    {
        if (!IsInitialized || battleUpgradeController == null || unitManager == null)
        {
            return false;
        }

        int ownedAllyCount = unitManager.DeployedAllyCount;
        if (battleUpgradeController.IsMaxLevel(upgrade, ownedAllyCount))
        {
            return false;
        }

        if (upgrade == EBattleUpgrade.UnitPurchase && !unitManager.CanPurchaseAlly)
        {
            return false;
        }

        return Gold >= battleUpgradeController.GetNextCost(upgrade);
    }

    public bool TryPurchaseBattleUpgrade(EBattleUpgrade upgrade)
    {
        if (!CanPurchaseBattleUpgrade(upgrade)) return false;

        int cost = GetBattleUpgradeCost(upgrade);
        AllyUnit purchasedUnit = null;
        if (upgrade == EBattleUpgrade.UnitPurchase)
        {
            purchasedUnit = unitManager.TryPurchaseAlly(
                purchasedAlly,
                State == EWaveState.Active);
            if (purchasedUnit == null) return false;
        }

        if (!TrySpendGold(cost))
        {
            if (purchasedUnit != null) unitManager.ReleaseUnit(purchasedUnit);
            return false;
        }

        battleUpgradeController.ConfirmPurchase(upgrade);
        ApplyBattleUpgrade(upgrade);
        OnBattleUpgradeChanged?.Invoke();
        return true;
    }

    private void ApplyBattleUpgrade(EBattleUpgrade upgrade)
    {
        if (upgrade == EBattleUpgrade.AllyAttack)
        {
            unitManager.SetSharedAttackMultiplier(
                battleUpgradeController.GetEffect(upgrade));
        }
        else if (upgrade == EBattleUpgrade.DefenseLineHp)
        {
            int increase = Mathf.RoundToInt(defenseLineHpSettings.EffectPerLevel);
            if (runState.IncreaseMaximumPlayerHp(increase))
            {
                OnHpChanged?.Invoke(PlayerHp);
            }
        }
    }

    public bool TrySpendPreparationGold(int amount)
    {
        return CanUsePreparationActions && TrySpendGold(amount);
    }

    public void SetPreparationLock(bool isLocked)
    {
        if (isPreparationLocked == isLocked) return;
        isPreparationLocked = isLocked;
        OnPreparationAvailabilityChanged?.Invoke(CanUsePreparationActions);
    }

    public void AddGold(int amount)
    {
        if (!economy.Add(amount)) return;
        OnGoldChanged?.Invoke(economy.Gold);
    }

    public void OnItemEvent(Item item)
    {
        barrierDamageReduction = Mathf.RoundToInt(item.Value1);
        minimumBarrierDamage = Mathf.Max(1, Mathf.RoundToInt(item.Value2));
    }

    protected override void OnDestroy()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (unitManager != null)
        {
            unitManager.OnBattleRosterChanged -= OnBattleRosterChanged;
        }

        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.Unsubscribe(EItem.BarrierReinforcement, this);
        }

        base.OnDestroy();
    }
}
