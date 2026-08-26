using System;
using System.Collections;
using UnityEngine;

// 소유: 유한 웨이브, 양측 방어선 HP, 기회, 골드
// 책임: 수동 웨이브 시작/판정/재시도/최종 결과와 UI 이벤트 발행
// 금지: 유닛 탐색/이동/공격, Instantiate 직접 처리
public class BattleManager : AppService, IItemEventListener
{
    public const float BaseKnockbackDistance = 3f;

    [Header("Run Chances")]
    [SerializeField, Min(1)] public int playerMaxHp = 3;
    [Header("Defense Lines")]
    [SerializeField, Min(1)] private int allyDefenseLineMaxHp = 20;
    [SerializeField, Min(1)] private int enemyDefenseLineMaxHp = 20;
    [Header("Wave Resolution")]
    [SerializeField, Min(0f)] private float waveResolutionDuration = 2f;

    [Header("Battle Upgrades")]
    // TODO: 플레이 테스트 후 프로토타입 비용과 효과 수치를 조정한다.
    [SerializeField] private UnitPurchaseSettings warriorPurchaseSettings =
        new("warrior", 30, 1.4f, 4f);
    [SerializeField] private UnitPurchaseSettings archerPurchaseSettings =
        new("archer", 35, 1.4f, 5f);
    [SerializeField] private UnitPurchaseSettings magePurchaseSettings =
        new("mage", 40, 1.4f, 7f);
    [SerializeField] private UnitPurchaseSettings spearmanPurchaseSettings =
        new("spearman", 35, 1.4f, 5f);
    [SerializeField, Min(1)] private int reinforcementComboThreshold = 5;
    [SerializeField] private BattleUpgradeSettings allyAttackSettings =
        new(1f, 0.25f, 75, 1.7f, 20);
    [SerializeField] private BattleUpgradeSettings defenseLineHpSettings =
        new(0f, 10f, 80, 1.7f, 20);

    public BattleWaveData CurrentWave => runState?.CurrentWave;
    public int CurrentWaveNumber => runState?.CurrentWaveNumber ?? 1;
    public int TotalWaveCount => runState?.TotalWaveCount ?? 0;
    public int PlayerHp => runState?.PlayerHp ?? playerMaxHp;
    public int MaximumPlayerHp => runState?.MaximumPlayerHp ?? playerMaxHp;
    public int Gold => economy?.Gold ?? 0;
    public bool IsInitialized { get; private set; }
    public EWaveState State => runState?.State ?? EWaveState.Pending;
    public bool IsPreparationPhase => State == EWaveState.Pending;
    public bool CanUsePreparationActions =>
        IsPreparationPhase && !isPreparationLocked;
    public bool IsCurrentWaveBoss => CurrentWave?.IsBoss ?? false;
    public bool IsRunEnded => State is EWaveState.Victory or EWaveState.Defeat;
    public bool CanStartCurrentWave =>
        IsInitialized && CanUsePreparationActions &&
        CurrentWave != null && unitManager != null;
    public bool HasTacticalReinforcement =>
        tacticalReinforcementController?.HasTicket ?? false;
    public float AssaultElapsedTime => assaultController?.ElapsedTime ?? 0f;
    public EBattleAssaultPhase AssaultPhase =>
        assaultController?.Phase ?? EBattleAssaultPhase.Initial;
    public EBaseKnockbackSkillState BaseKnockbackSkillState =>
        baseKnockbackSkillController?.State ??
        EBaseKnockbackSkillState.Locked;
    public float BaseKnockbackRemainingTime =>
        baseKnockbackSkillController?.RemainingTime ??
        BaseKnockbackSkillController.UnlockSeconds;
    public bool CanUseBaseKnockbackSkill =>
        IsInitialized && State == EWaveState.Active &&
        baseKnockbackSkillController?.CanUse == true &&
        unitManager?.HasAliveActiveEnemy == true;

    public event Action<EWaveState> OnStateChanged;
    public event Action OnInitialized;
    public event Action<int> OnWaveChanged;
    public event Action<int> OnHpChanged;
    public event Action<int> OnGoldChanged;
    public event Action OnBattleUpgradeChanged;
    public event Action<bool> OnPreparationAvailabilityChanged;
    public event Action<EWaveResolutionResult, int> OnWaveResolutionStarted;
    public event Action<BattleWaveStartedData> OnWaveStarted;
    public event Action<BattleWaveResolvedData> OnWaveResolved;
    public event Action<EBattleTeam, int, int> OnDefenseLineHpChanged;
    public event Action<BattleUpgradePurchasedData> OnBattleUpgradePurchased;
    public event Action<UnitPurchaseResult> OnAllyPurchased;
    public event Action<string> OnAllyProgressionChanged;
    public event Action<bool> OnTacticalReinforcementChanged;
    public event Action<EBattleAssaultPhase> OnAssaultPhaseChanged;
    public event Action OnBaseKnockbackSkillDisplayChanged;
    public event Action<int> OnBossDefeated;
    public event Action<int> OnRunEnded;

    private UnitManager unitManager;
    private PinballManager pinballManager;
    private BattleRunState runState;
    private BattleDefenseLineController defenseLineController;
    private WaveResolutionState waveResolution;
    private BattleEconomy economy = new(0);
    private BattleUpgradeController battleUpgradeController;
    private UnitPurchaseController unitPurchaseController;
    private AllyProgressionController allyProgressionController = new();
    private TitleData titleData;
    private TacticalReinforcementController tacticalReinforcementController;
    private BattleAssaultController assaultController;
    private BaseKnockbackSkillController baseKnockbackSkillController = new();
    private int barrierDamageReduction;
    private int minimumBarrierDamage = 1;
    private Coroutine waveResolutionCoroutine;
    private bool isRunInitialized;
    private bool isPreparationLocked;
    private float currentWaveStartedAt;
    private bool currentBossDefeated;
    private int currentWaveAllyDefenseDamage;

    private void Start()
    {
        InitializeNewRun();
    }

    private void Update()
    {
        if (!IsInitialized) return;

        unitPurchaseController?.Advance(Time.deltaTime);
        if (State != EWaveState.Active) return;

        AdvanceBaseKnockbackSkill(Time.deltaTime);
        if (assaultController == null) return;

        assaultController.Advance(
            Time.deltaTime,
            unitManager.RemainingEnemyCount,
            enemyId => unitManager.TrySpawnScheduledEnemy(
                enemyId,
                CurrentWaveNumber));
    }

    internal void InitializeNewRun()
    {
        if (isRunInitialized) return;
        isRunInitialized = true;

        unitManager = App.Get<UnitManager>();
        unitManager.InitializeNewRun();
        unitManager.OnEnemyDefeated += OnEnemyDefeated;
        unitManager.OnDefenseLineAttackRequested += TryApplyDefenseLineAttack;
        unitManager.OnBattleRosterChanged += OnBattleRosterChanged;

        titleData = App.Get<TitleData>();
        runState = new BattleRunState(
            titleData.BattleWaves,
            titleData.HasValidBattleRun,
            playerMaxHp);
        defenseLineController = new BattleDefenseLineController(
            allyDefenseLineMaxHp,
            enemyDefenseLineMaxHp);
        waveResolution = new WaveResolutionState();
        economy = new BattleEconomy(
            titleData.BattleRunCommon?.StartingGold ?? 0);
        battleUpgradeController = new BattleUpgradeController(
            allyAttackSettings,
            defenseLineHpSettings);
        allyProgressionController.Reset();
        unitPurchaseController = new UnitPurchaseController(
            economy,
            IsAllyJobUnlocked,
            ResolveAllyPurchaseLevel,
            warriorPurchaseSettings,
            archerPurchaseSettings,
            magePurchaseSettings,
            spearmanPurchaseSettings,
            CreateAdvancedPurchaseSettings("knight", warriorPurchaseSettings),
            CreateAdvancedPurchaseSettings("berserker", warriorPurchaseSettings),
            CreateAdvancedPurchaseSettings("ranger", archerPurchaseSettings),
            CreateAdvancedPurchaseSettings("marksman", archerPurchaseSettings),
            CreateAdvancedPurchaseSettings("pyromancer", magePurchaseSettings),
            CreateAdvancedPurchaseSettings("frost", magePurchaseSettings),
            CreateAdvancedPurchaseSettings("lancer", spearmanPurchaseSettings),
            CreateAdvancedPurchaseSettings("guard", spearmanPurchaseSettings));
        assaultController = new BattleAssaultController();
        assaultController.PhaseChanged += OnAssaultPhaseChangedInternal;
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
        OnWaveChanged?.Invoke(CurrentWaveNumber);
        NotifyAllDefenseLineHp();
    }

    public bool TryStartWave()
    {
        if (!CanStartCurrentWave) return false;

        defenseLineController.ResetForWave();
        NotifyAllDefenseLineHp();
        unitManager.PrepareWave();
        assaultController.Start(CurrentWave);
        assaultController.Advance(
            0f,
            unitManager.RemainingEnemyCount,
            enemyId => unitManager.TrySpawnScheduledEnemy(
                enemyId,
                CurrentWaveNumber));
        int spawnedCount = unitManager.RemainingEnemyCount;
        if (spawnedCount <= 0)
        {
            assaultController.Stop();
            Debug.LogError(
                $"[BattleManager] Wave {CurrentWaveNumber} enemy spawn failed.");
            return false;
        }

        unitManager.StartPreparedWave();
        currentWaveStartedAt = Time.time;
        currentBossDefeated = false;
        currentWaveAllyDefenseDamage = 0;
        baseKnockbackSkillController.StartWave();
        ChangeState(EWaveState.Active);
        OnBaseKnockbackSkillDisplayChanged?.Invoke();
        OnWaveStarted?.Invoke(new BattleWaveStartedData(
            CurrentWaveNumber,
            IsCurrentWaveBoss,
            spawnedCount));
        SoundManager.PlaySFXIfAvailable(SoundName.WaveStart);
        return true;
    }

    internal void AdvanceBaseKnockbackSkill(float deltaTime)
    {
        if (baseKnockbackSkillController == null) return;
        if (baseKnockbackSkillController.Advance(
                deltaTime,
                State == EWaveState.Active))
        {
            OnBaseKnockbackSkillDisplayChanged?.Invoke();
        }
    }

    public bool TryUseBaseKnockbackSkill()
    {
        if (!CanUseBaseKnockbackSkill) return false;

        int appliedCount = unitManager.TryApplyBaseKnockback(
            BaseKnockbackDistance);
        if (appliedCount <= 0 ||
            !baseKnockbackSkillController.TryConfirmUse(true)) return false;

        OnBaseKnockbackSkillDisplayChanged?.Invoke();
        return true;
    }

    private void OnBattleRosterChanged()
    {
        OnBaseKnockbackSkillDisplayChanged?.Invoke();
    }

    private void OnEnemyDefeated(string enemyId)
    {
        if (!IsCurrentWaveBoss || currentBossDefeated ||
            !string.Equals(enemyId, "goblin_king", StringComparison.Ordinal)) return;

        currentBossDefeated = true;
        OnBossDefeated?.Invoke(CurrentWaveNumber);
    }

    public void TryApplyDefenseLineAttack(
        UnitBase attacker,
        EBattleTeam defenseTeam,
        float attackDamage)
    {
        if (State != EWaveState.Active || attacker == null ||
            attacker.Team == defenseTeam || !unitManager.IsActiveUnit(attacker))
            return;

        int damage = defenseTeam == EBattleTeam.Ally
            ? BarrierDamageCalculator.Calculate(
                Mathf.RoundToInt(attackDamage),
                barrierDamageReduction,
                minimumBarrierDamage)
            : Mathf.Max(1, Mathf.RoundToInt(attackDamage));
        int previousHp = defenseLineController.GetCurrentHp(defenseTeam);
        if (!defenseLineController.ApplyDamage(defenseTeam, damage)) return;
        if (defenseTeam == EBattleTeam.Ally)
        {
            currentWaveAllyDefenseDamage +=
                previousHp - defenseLineController.GetCurrentHp(defenseTeam);
        }
        NotifyDefenseLineHp(defenseTeam);

        if (BattleResolutionPolicy.TryResolveDefenseLines(
                GetDefenseLineHp(EBattleTeam.Ally),
                GetDefenseLineHp(EBattleTeam.Enemy),
                out EWaveResolutionResult result))
        {
            BeginWaveResolution(result);
        }
    }

    public int GetDefenseLineHp(EBattleTeam team) =>
        defenseLineController?.GetCurrentHp(team) ?? 20;

    public int GetDefenseLineMaximumHp(EBattleTeam team) =>
        defenseLineController?.GetMaximumHp(team) ?? 20;

    private void NotifyDefenseLineHp(EBattleTeam team)
    {
        unitManager?.SetDefenseLineHealth(
            team,
            GetDefenseLineHp(team),
            GetDefenseLineMaximumHp(team));
        OnDefenseLineHpChanged?.Invoke(
            team,
            GetDefenseLineHp(team),
            GetDefenseLineMaximumHp(team));
    }

    private void NotifyAllDefenseLineHp()
    {
        NotifyDefenseLineHp(EBattleTeam.Ally);
        NotifyDefenseLineHp(EBattleTeam.Enemy);
    }

    private void BeginWaveResolution(EWaveResolutionResult result)
    {
        if (State != EWaveState.Active ||
            !waveResolution.TryBegin(
                result,
                CurrentWaveNumber,
                Time.time,
                waveResolutionDuration)) return;

        assaultController?.Stop();
        if (result == EWaveResolutionResult.Failed)
        {
            runState.ConsumeChance();
            OnHpChanged?.Invoke(PlayerHp);
        }

        float duration = Mathf.Max(0f, Time.time - currentWaveStartedAt);
        unitManager.StopBattle();
        ChangeState(EWaveState.Resolving);
        OnWaveResolutionStarted?.Invoke(result, CurrentWaveNumber);
        OnWaveResolved?.Invoke(new BattleWaveResolvedData(
            CurrentWaveNumber,
            result,
            duration,
            currentWaveAllyDefenseDamage,
            IsCurrentWaveBoss));
        SoundManager.PlaySFXIfAvailable(
            result == EWaveResolutionResult.Cleared
                ? SoundName.WaveWin
                : SoundName.WaveFailed);
        waveResolutionCoroutine = StartCoroutine(WaitForWaveResolution());
    }

    private IEnumerator WaitForWaveResolution()
    {
        yield return new WaitForSeconds(waveResolutionDuration);
        waveResolutionCoroutine = null;
        FinishWaveResolution();
    }

    private void FinishWaveResolution()
    {
        if (State != EWaveState.Resolving || !waveResolution.IsPending) return;

        EWaveResolutionResult result = waveResolution.Result;
        bool isFinalWave = runState.CurrentWaveIndex + 1 >=
                           runState.TotalWaveCount;
        unitManager.ResolveWaveResult();
        unitPurchaseController.ResetForWave();
        waveResolution.Clear();

        EWaveState nextState = BattleResolutionPolicy.ResolveNextState(
            result,
            isFinalWave,
            PlayerHp);
        if (result == EWaveResolutionResult.Cleared &&
            nextState == EWaveState.Pending)
        {
            runState.AdvanceWave();
            OnWaveChanged?.Invoke(CurrentWaveNumber);
        }

        ChangeState(nextState);
        if (nextState is EWaveState.Victory or EWaveState.Defeat)
        {
            OnRunEnded?.Invoke(CurrentWaveNumber);
        }
    }

    private void ChangeState(EWaveState nextState)
    {
        if (!runState.ChangeState(nextState)) return;
        OnStateChanged?.Invoke(State);
        OnPreparationAvailabilityChanged?.Invoke(CanUsePreparationActions);
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

    public int GetAllyJobLevel(string rootUnitId)
    {
        return allyProgressionController?.GetLevel(rootUnitId) ?? 0;
    }

    public int GetAllyJobLevelUpCost(string rootUnitId)
    {
        return allyProgressionController?.GetNextCost(rootUnitId) ?? 0;
    }

    public bool IsAllyJobUnlocked(string unitId)
    {
        return allyProgressionController?.IsUnlocked(unitId) == true;
    }

    public bool CanLevelUpAllyJob(string rootUnitId)
    {
        return IsInitialized && CanUsePreparationActions &&
               unitManager != null && allyProgressionController != null &&
               allyProgressionController.CanLevelUp(
                   rootUnitId,
                   true,
                   Gold);
    }

    public bool TryLevelUpAllyJob(string rootUnitId)
    {
        if (!CanLevelUpAllyJob(rootUnitId)) return false;

        int cost = GetAllyJobLevelUpCost(rootUnitId);
        if (!allyProgressionController.TryLevelUp(
                rootUnitId,
                true,
                Gold,
                out AllyProgressionResult result))
        {
            return false;
        }

        if (!TrySpendGold(cost)) return false;

        unitManager.RefreshOwnedAlliesForRootJob(
            rootUnitId,
            result.Level,
            titleData);
        OnAllyProgressionChanged?.Invoke(rootUnitId);
        return true;
    }

    private int ResolveAllyPurchaseLevel(string unitId)
    {
        if (titleData != null &&
            titleData.TryGetRootAllyJob(unitId, out AllyUnitData rootJob))
        {
            return Mathf.Max(1, GetAllyJobLevel(rootJob.id));
        }

        return 1;
    }

    private static UnitPurchaseSettings CreateAdvancedPurchaseSettings(
        string unitId,
        UnitPurchaseSettings root)
    {
        int baseCost = root.BaseCost > int.MaxValue / 2
            ? int.MaxValue
            : root.BaseCost * 2;
        return new UnitPurchaseSettings(
            unitId,
            baseCost,
            root.CostMultiplier,
            root.CooldownSeconds);
    }

    public int GetAllyPurchaseCost(string unitId)
    {
        return unitPurchaseController?.GetNextCost(unitId) ?? 0;
    }

    public float GetAllyRemainingCooldown(string unitId)
    {
        return unitPurchaseController?.GetRemainingCooldown(unitId) ?? 0f;
    }

    public bool IsAllyCoolingDown(string unitId)
    {
        return unitPurchaseController?.IsCoolingDown(unitId) ?? false;
    }

    public bool CanPurchaseAlly(string unitId)
    {
        if (!IsInitialized || State != EWaveState.Active ||
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
        if (!IsInitialized || State != EWaveState.Active ||
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

    private void OnAssaultPhaseChangedInternal(EBattleAssaultPhase phase)
    {
        OnAssaultPhaseChanged?.Invoke(phase);
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
            if (defenseLineController.IncreaseAllyMaximumHp(increase))
            {
                NotifyDefenseLineHp(EBattleTeam.Ally);
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
        if (waveResolutionCoroutine != null)
        {
            StopCoroutine(waveResolutionCoroutine);
            waveResolutionCoroutine = null;
        }

        if (unitManager != null)
        {
            unitManager.OnEnemyDefeated -= OnEnemyDefeated;
            unitManager.OnDefenseLineAttackRequested -= TryApplyDefenseLineAttack;
            unitManager.OnBattleRosterChanged -= OnBattleRosterChanged;
        }

        if (pinballManager != null)
        {
            pinballManager.OnComboChanged -= OnPinballComboChanged;
            pinballManager.OnJackpotTriggered -= OnJackpotTriggered;
        }

        if (assaultController != null)
        {
            assaultController.PhaseChanged -= OnAssaultPhaseChangedInternal;
        }

        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.Unsubscribe(EItem.BarrierReinforcement, this);
        }

        base.OnDestroy();
    }
}

public readonly struct BattleWaveStartedData
{
    public int Wave { get; }
    public bool IsBoss { get; }
    public int SpawnedEnemyCount { get; }

    public BattleWaveStartedData(int wave, bool isBoss, int spawnedEnemyCount)
    {
        Wave = wave;
        IsBoss = isBoss;
        SpawnedEnemyCount = spawnedEnemyCount;
    }
}

public readonly struct BattleWaveResolvedData
{
    public int Wave { get; }
    public EWaveResolutionResult Result { get; }
    public float Duration { get; }
    public int AllyDefenseLineDamage { get; }
    public bool IsBoss { get; }

    public BattleWaveResolvedData(
        int wave,
        EWaveResolutionResult result,
        float duration,
        int allyDefenseLineDamage,
        bool isBoss)
    {
        Wave = wave;
        Result = result;
        Duration = duration;
        AllyDefenseLineDamage = allyDefenseLineDamage;
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
