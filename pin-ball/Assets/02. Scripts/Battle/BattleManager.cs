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
    [Header("Stage Transition")]
    [SerializeField, Min(0f)] private float stageTransitionDuration = 2f;

    [Header("Enemy Stage Prototype")]
    // TODO: 플레이 테스트 후 프로토타입 적 수와 증가 구간을 조정한다.
    [SerializeField] private string stageEnemyId = "goblin";
    [SerializeField, Min(1)] private int baseEnemyCount = 3;
    [SerializeField, Min(1)] private int enemyCountGrowthInterval = 3;
    [SerializeField, Min(0)] private int enemyCountGrowthAmount = 1;
    [SerializeField, Min(1)] private int maximumEnemyCount = 10;

    [Header("Boss Stage Prototype")]
    [SerializeField] private string bossEnemyId = "goblin_king";
    [SerializeField, Min(1)] private int bossStageInterval = 10;
    // TODO: 플레이 관찰 후 보스 체력과 공격력 배율을 각각 조정한다.
    [SerializeField, Min(0.01f)] private float bossHealthMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float bossAttackMultiplier = 1f;

    [Header("Battle Upgrades")]
    // TODO: 플레이 테스트 후 프로토타입 비용과 효과 수치를 조정한다.
    [SerializeField] private UnitPurchaseSettings warriorPurchaseSettings =
        new("warrior", 30, 1.4f);
    [SerializeField] private UnitPurchaseSettings archerPurchaseSettings =
        new("archer", 35, 1.4f);
    [SerializeField] private UnitPurchaseSettings magePurchaseSettings =
        new("mage", 40, 1.4f);
    [SerializeField, Min(1)] private int reinforcementComboThreshold = 5;
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
    public bool IsCurrentStageBoss =>
        stageController?.IsCurrentStageBoss ?? false;
    public bool IsRunEnded => State == EWaveState.Ended;
    public bool HasTacticalReinforcement =>
        tacticalReinforcementController?.HasTicket ?? false;

    public event Action<EWaveState> OnStateChanged;
    public event Action OnInitialized;
    public event Action<int> OnWaveChanged;
    public event Action<int> OnHpChanged;
    public event Action<int> OnGoldChanged;
    public event Action OnBattleUpgradeChanged;
    public event Action<bool> OnPreparationAvailabilityChanged;
    public event Action<EWaveResolutionResult, int> OnWaveResolutionStarted;
    public event Action<BattleStageStartedData> OnStageStarted;
    public event Action<BattleStageResolvedData> OnStageResolved;
    public event Action<BattleUpgradePurchasedData> OnBattleUpgradePurchased;
    public event Action<UnitPurchaseResult> OnAllyPurchased;
    public event Action<bool> OnTacticalReinforcementChanged;
    public event Action<int> OnBossDefeated;
    public event Action<int> OnRunEnded;

    private UnitManager unitManager;
    private PinballManager pinballManager;
    private BattleRunState runState;
    private BattleStageController stageController;
    private EnemyStageScalingController enemyScalingController;
    private BattleEconomy economy = new(0);
    private BattleUpgradeController battleUpgradeController;
    private UnitPurchaseController unitPurchaseController;
    private TacticalReinforcementController tacticalReinforcementController;
    private int barrierDamageReduction;
    private int minimumBarrierDamage = 1;
    private Coroutine transitionCoroutine;
    private bool isRunInitialized;
    private bool isPreparationLocked;
    private float currentStageStartedAt;
    private bool currentBossDefeated;
    private int currentStageDefenseLineDamage;

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
        unitManager.OnEnemyDefeated += OnEnemyDefeated;

        var titleData = App.Get<TitleData>();
        runState = new BattleRunState(playerMaxHp);
        stageController = new BattleStageController(bossStageInterval);
        enemyScalingController = new EnemyStageScalingController(
            baseEnemyCount,
            enemyCountGrowthInterval,
            enemyCountGrowthAmount,
            maximumEnemyCount);
        economy = new BattleEconomy(
            titleData.BattleRunCommon?.StartingGold ?? 0);
        battleUpgradeController = new BattleUpgradeController(
            allyAttackSettings,
            defenseLineHpSettings);
        unitPurchaseController = new UnitPurchaseController(
            economy,
            warriorPurchaseSettings,
            archerPurchaseSettings,
            magePurchaseSettings);
        tacticalReinforcementController = new TacticalReinforcementController(
            reinforcementComboThreshold);
        pinballManager = App.Get<PinballManager>();
        pinballManager.OnComboChanged += OnPinballComboChanged;
        pinballManager.OnJackpotTriggered += OnJackpotTriggered;

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
        if (unitManager == null)
        {
            return;
        }

        bool isInitialStage = State == EWaveState.Starting;
        bool canStart = isInitialStage
            ? stageController.TryStart()
            : State == EWaveState.Active;
        if (!canStart) return;

        bool isBossStage = IsCurrentStageBoss;
        string enemyId = isBossStage ? bossEnemyId : stageEnemyId;
        int enemyCount = isBossStage
            ? 1
            : enemyScalingController.CalculateEnemyCount(CurrentStageNumber);
        int spawnedCount = unitManager.BeginStage(
            enemyId,
            enemyCount,
            CurrentStageNumber,
            isBossStage ? bossHealthMultiplier : 1f,
            isBossStage ? bossAttackMultiplier : 1f);
        if (spawnedCount <= 0)
        {
            Debug.LogError(
                $"[BattleManager] Stage {CurrentStageNumber} enemy spawn failed: {enemyId}");
            stageController.TryAbortStart();
            OnStateChanged?.Invoke(State);
            return;
        }

        currentStageStartedAt = Time.time;
        currentBossDefeated = false;
        currentStageDefenseLineDamage = 0;
        OnStateChanged?.Invoke(State);
        if (isInitialStage) OnWaveChanged?.Invoke(CurrentStageNumber);
        OnStageStarted?.Invoke(new BattleStageStartedData(
            CurrentStageNumber,
            isBossStage,
            spawnedCount));
        SoundManager.PlaySFXIfAvailable(SoundName.WaveStart);
    }

    private void OnEnemyDefeated(string enemyId)
    {
        if (!IsCurrentStageBoss || currentBossDefeated ||
            !string.Equals(enemyId, bossEnemyId, StringComparison.Ordinal)) return;

        currentBossDefeated = true;
        OnBossDefeated?.Invoke(CurrentStageNumber);
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

    public void TryApplyDefenseLineAttack(
        EnemyUnit enemy,
        float attackDamage)
    {
        if (State != EWaveState.Active || enemy == null ||
            !unitManager.IsActiveEnemy(enemy)) return;

        int defenseLineDamage = BarrierDamageCalculator.Calculate(
            Mathf.RoundToInt(attackDamage),
            barrierDamageReduction,
            minimumBarrierDamage);
        runState.ApplyPlayerDamage(defenseLineDamage);
        currentStageDefenseLineDamage += defenseLineDamage;
        OnHpChanged?.Invoke(PlayerHp);

        if (PlayerHp <= 0) EndRun();
    }

    private void BeginStageTransition(EWaveResolutionResult result)
    {
        if (result == EWaveResolutionResult.Cleared)
        {
            ScheduleNextStage();
            return;
        }

        EndRun();
    }

    private void ScheduleNextStage()
    {
        int completedStage = CurrentStageNumber;
        bool completedStageWasBoss = IsCurrentStageBoss;
        if (!stageController.TryScheduleNextStage(
                Time.time,
                stageTransitionDuration)) return;

        float battleDuration = Mathf.Max(0f, Time.time - currentStageStartedAt);
        OnStageResolved?.Invoke(new BattleStageResolvedData(
            completedStage,
            EWaveResolutionResult.Cleared,
            battleDuration,
            currentStageDefenseLineDamage,
            completedStageWasBoss));
        OnWaveChanged?.Invoke(CurrentStageNumber);
        SoundManager.PlaySFXIfAvailable(SoundName.WaveWin);

        transitionCoroutine = StartCoroutine(WaitForNextStage());
    }

    private void EndRun()
    {
        if (!stageController.TryEndRun()) return;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        float battleDuration = Mathf.Max(0f, Time.time - currentStageStartedAt);
        unitManager.StopBattle();
        OnStateChanged?.Invoke(State);
        OnWaveResolutionStarted?.Invoke(
            EWaveResolutionResult.Failed,
            CurrentStageNumber);
        OnStageResolved?.Invoke(new BattleStageResolvedData(
            CurrentStageNumber,
            EWaveResolutionResult.Failed,
            battleDuration,
            currentStageDefenseLineDamage,
            IsCurrentStageBoss));
        SoundManager.PlaySFXIfAvailable(SoundName.WaveFailed);
        OnRunEnded?.Invoke(CurrentStageNumber);
    }

    private IEnumerator WaitForNextStage()
    {
        yield return new WaitForSeconds(stageTransitionDuration);
        transitionCoroutine = null;

        if (!stageController.TryCompleteNextStageSchedule(Time.time)) yield break;
        StartCurrentStage();
    }

    public bool TrySpendGold(int amount)
    {
        if (IsRunEnded) return false;

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
        return battleUpgradeController?.GetLevel(upgrade) ?? 0;
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
        return battleUpgradeController?.GetEffect(upgrade) ?? 0f;
    }

    public float GetNextBattleUpgradeEffect(EBattleUpgrade upgrade)
    {
        return battleUpgradeController?.GetNextEffect(upgrade) ?? 0f;
    }

    public bool CanPurchaseBattleUpgrade(EBattleUpgrade upgrade)
    {
        if (!IsInitialized || IsRunEnded || battleUpgradeController == null)
        {
            return false;
        }

        if (battleUpgradeController.IsMaxLevel(upgrade))
        {
            return false;
        }

        return Gold >= battleUpgradeController.GetNextCost(upgrade);
    }

    public bool TryPurchaseBattleUpgrade(EBattleUpgrade upgrade)
    {
        if (!CanPurchaseBattleUpgrade(upgrade)) return false;

        int cost = GetBattleUpgradeCost(upgrade);
        if (!TrySpendGold(cost))
        {
            return false;
        }

        battleUpgradeController.ConfirmPurchase(upgrade);
        ApplyBattleUpgrade(upgrade);
        OnBattleUpgradeChanged?.Invoke();
        OnBattleUpgradePurchased?.Invoke(new BattleUpgradePurchasedData(
            upgrade,
            GetBattleUpgradeLevel(upgrade),
            cost));
        return true;
    }

    public int GetAllyPurchaseCount(string unitId)
    {
        return unitPurchaseController?.GetPurchaseCount(unitId) ?? 0;
    }

    public int GetAllyPurchaseCost(string unitId)
    {
        return unitPurchaseController?.GetNextCost(unitId) ?? 0;
    }

    public bool CanPurchaseAlly(string unitId)
    {
        if (!IsInitialized || IsRunEnded ||
            unitManager == null || unitPurchaseController == null)
        {
            return false;
        }

        return HasTacticalReinforcement
            ? unitPurchaseController.CanPurchaseFree(
                unitId,
                unitManager.CanPurchaseAlly)
            : unitPurchaseController.CanPurchase(
                unitId,
                unitManager.CanPurchaseAlly);
    }

    public bool TryPurchaseAlly(string unitId)
    {
        if (!IsInitialized || IsRunEnded ||
            unitManager == null || unitPurchaseController == null)
        {
            return false;
        }

        bool isFreePurchase = HasTacticalReinforcement;
        UnitPurchaseResult result = default;
        bool purchased = isFreePurchase
            ? tacticalReinforcementController.TryUse(() =>
                unitPurchaseController.TryPurchaseFree(
                    unitId,
                    unitManager.CanPurchaseAlly,
                    TrySpawnPurchasedAlly,
                    out result))
            : unitPurchaseController.TryPurchase(
                unitId,
                unitManager.CanPurchaseAlly,
                TrySpawnPurchasedAlly,
                out result);
        if (!purchased) return false;

        if (isFreePurchase)
        {
            OnTacticalReinforcementChanged?.Invoke(false);
        }
        else
        {
            OnGoldChanged?.Invoke(Gold);
        }

        OnAllyPurchased?.Invoke(result);
        return true;
    }

    private bool TrySpawnPurchasedAlly(BattleUnitSpawnData spawnData)
    {
        return unitManager.TryPurchaseAlly(
            spawnData,
            State == EWaveState.Active) != null;
    }

    private void OnPinballComboChanged(int combo)
    {
        if (tacticalReinforcementController.ObserveCombo(combo))
        {
            OnTacticalReinforcementChanged?.Invoke(true);
        }
    }

    private void OnJackpotTriggered(Pinball _, int __)
    {
        if (tacticalReinforcementController.GrantFromJackpot())
        {
            OnTacticalReinforcementChanged?.Invoke(true);
        }
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
        if (IsRunEnded) return;

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
            unitManager.OnEnemyDefeated -= OnEnemyDefeated;
        }

        if (pinballManager != null)
        {
            pinballManager.OnComboChanged -= OnPinballComboChanged;
            pinballManager.OnJackpotTriggered -= OnJackpotTriggered;
        }

        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.Unsubscribe(EItem.BarrierReinforcement, this);
        }

        base.OnDestroy();
    }
}

public readonly struct BattleStageStartedData
{
    public int Stage { get; }
    public bool IsBoss { get; }
    public int SpawnedEnemyCount { get; }

    public BattleStageStartedData(int stage, bool isBoss, int spawnedEnemyCount)
    {
        Stage = stage;
        IsBoss = isBoss;
        SpawnedEnemyCount = spawnedEnemyCount;
    }
}

public readonly struct BattleStageResolvedData
{
    public int Stage { get; }
    public EWaveResolutionResult Result { get; }
    public float Duration { get; }
    public int DefenseLineDamage { get; }
    public bool IsBoss { get; }

    public BattleStageResolvedData(
        int stage,
        EWaveResolutionResult result,
        float duration,
        int defenseLineDamage,
        bool isBoss)
    {
        Stage = stage;
        Result = result;
        Duration = duration;
        DefenseLineDamage = defenseLineDamage;
        IsBoss = isBoss;
    }
}

public readonly struct BattleUpgradePurchasedData
{
    public EBattleUpgrade Upgrade { get; }
    public int Level { get; }
    public int Cost { get; }

    public BattleUpgradePurchasedData(
        EBattleUpgrade upgrade,
        int level,
        int cost)
    {
        Upgrade = upgrade;
        Level = level;
        Cost = cost;
    }
}
