using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

//소유: activeAllies, activeEnemies
//책임: 전투 중 목록 관리, 죽음/제거 반영, 웨이브 클리어 조건 계산
//금지: 골드/HP/웨이브 상태 변경
public class UnitManager : AppService, IItemEventListener, IEnemyBattleActions
{
    public const int MaxDeployedAllyCount = 5;

    public event Action<AllyUnitData, AllyUnitData> OnEvolutionRequested;
    public event Action<AllyUnit> OnAllyDetailRequested;
    public event Action<int> OnDeployedAllyCountChanged;
    public event Action<int> OnAlliesMerged;

    private UnitRoster _roster;
    private UnitTargetFinder _targetFinder;
    private UnitCreationService _creationService;
    private BattleUnitModifiers _unitModifiers;
    private UnitPlacementService _placementService;
    private UnitMergeService _mergeService;
    private UnitCombatContext _combatContext;

    public IReadOnlyList<AllyUnit> OwnedAllies => _roster.OwnedAllies;
    public BattleAreaBounds BattleArea => battleArea;
    public UnitTargetFinder TargetFinder => _targetFinder;

    public int DeployedAllyCount => _roster.OwnedAllyCount;
    public int RemainingAllyCount => _roster.ActiveAllyCount;
    public int RemainingEnemyCount => _roster.ActiveEnemyCount;
    public bool CanStartWaveWithCurrentRoster =>
        CanStartWaveWithAllyCount(DeployedAllyCount);
    
    private BattleManager _battleManager;
    private UnitSpawner _spawner;
    private TitleData _titleData;
    [SerializeField] private BattleAreaBounds battleArea;
    [SerializeField] private EvolutionGlowEffect evolutionGlowEffect;
    private int _enemySpawnIndex;
    private ItemManager _itemManager;

    protected override void Awake()
    {
        base.Awake();
        _spawner = GetComponent<UnitSpawner>();
        _roster = new UnitRoster();
        _targetFinder = new UnitTargetFinder(_roster);
        _unitModifiers = new BattleUnitModifiers();
        _placementService = new UnitPlacementService(battleArea);
    }

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _titleData = App.Get<TitleData>();
        _creationService = new UnitCreationService(_titleData);
        _mergeService = new UnitMergeService(_titleData);
        _combatContext = new UnitCombatContext(
            _targetFinder,
            battleArea,
            NotifyUnitDied);
        _battleManager.OnStateChanged += OnStateChanged;

        _itemManager = App.Get<ItemManager>();
        _itemManager.Subscribe(EItem.BattleClock, this);
        _itemManager.Subscribe(EItem.FieldArmor, this);
        _itemManager.Subscribe(EItem.DiversityEmblem, this);
    }

    private void Update()
    {
        if (_battleManager == null || _battleManager.State != EWaveState.Active) return;

        for (int i = 0; i < _roster.ActiveAllies.Count; i++)
        {
            var ally = _roster.ActiveAllies[i];
            if (ally == null || !ally.IsAlive || ally.HpRatio >= 0.5f) continue;

            if (_itemManager.TryConsume(EItem.PartyHealingPotion))
            {
                HealAllActiveAllies(0.25f);
            }
            else if (_itemManager.TryConsume(EItem.PersonalHealingPotion))
            {
                ally.Heal(ally.MaxHp * 0.5f);
            }

            break;
        }
    }

    private void HealAllActiveAllies(float ratio)
    {
        foreach (var ally in _roster.ActiveAllies)
        {
            if (ally != null && ally.IsAlive) ally.Heal(ally.MaxHp * ratio);
        }
    }

    private void OnStateChanged(EWaveState state)
    {
        if (state is EWaveState.Active)
        {
            ReturnAllEnemies();
            CleanupDestroyedUnits();
            SpawnEnemies(_battleManager.CurrentWave);
        }
    }

    public AllyUnit SpawnAlly(BattleUnitSpawnData unitData)
    {
        return SpawnAlly(unitData, 0f);
    }

    public AllyUnit SpawnAlly(
        BattleUnitSpawnData unitData,
        float temporaryAttackBonus)
    {
        if (unitData == null)
        {
            Debug.LogWarning("[UnitManager] Ally spawn data is null.");
            return null;
        }

        if (_creationService == null ||
            !_creationService.TryCreateAlly(
                unitData,
                temporaryAttackBonus,
                out AllyUnitData allyData,
                out BattleUnitStats finalStats))
        {
            Debug.LogWarning($"[UnitManager] Invalid ally stats: {unitData.UnitId}");
            return null;
        }

        var spawnedUnit = _spawner.SpawnAlly(
            unitData,
            allyData,
            _titleData.AllyCommon,
            finalStats,
            _combatContext,
            this,
            UnitSkillRegistry.CreateDefault());
        if (spawnedUnit == null)
        {
            return null;
        }

        if (!_placementService.TryPlaceInFreeGridSlot(spawnedUnit))
        {
            Debug.LogWarning(
                "[UnitManager] No available ally preparation grid slot.");
            _spawner.ReturnUnit(spawnedUnit);
            return null;
        }

        AddOwnedAlly(spawnedUnit);
        SoundManager.PlaySFXIfAvailable(SoundName.UnitSpawn);
        return spawnedUnit;
    }

    private void SpawnEnemies(BattleWaveData wave)
    {
        if (wave == null || wave.Enemies == null)
        {
            return;
        }

        _enemySpawnIndex = 0;
        for (var entryIndex = 0; entryIndex < wave.Enemies.Count; entryIndex++)
        {
            var spawnData = wave.Enemies[entryIndex];
            if (spawnData == null)
            {
                continue;
            }

            for (var count = 0; count < Mathf.Max(1, spawnData.Count); count++)
            {
                SpawnEnemy(spawnData.EnemyId, null);
            }
        }
    }

    private EnemyUnit SpawnEnemy(string enemyId, Vector3? spawnPosition)
    {
        int wave = _battleManager != null
            ? _battleManager.CurrentWaveNumber
            : _titleData != null && _titleData.EnemyCommon != null
                ? _titleData.EnemyCommon.BaseWave
                : 0;
        if (_creationService == null ||
            !_creationService.TryCreateEnemy(
                enemyId,
                wave,
                out EnemyUnitData enemyData,
                out BattleUnitStats stats))
        {
            Debug.LogWarning($"[UnitManager] Invalid enemy stats: {enemyId}");
            return null;
        }

        var enemy = _spawner.SpawnEnemy(
            enemyData,
            stats,
            _enemySpawnIndex++,
            _combatContext,
            spawnPosition,
            this,
            UnitSkillRegistry.CreateDefault());
        AddEnemy(enemy);
        return enemy;
    }

    public void SpawnEnemyReinforcement(
        string enemyId,
        int count,
        Vector3 center)
    {
        for (var i = 0; i < Mathf.Max(0, count); i++)
        {
            var offset = new Vector3(
                UnityEngine.Random.Range(-0.5f, 0.5f),
                UnityEngine.Random.Range(-0.5f, 0.5f),
                0f);
            SpawnEnemy(enemyId, center + offset);
        }
    }

    private void AddOwnedAlly(AllyUnit ally)
    {
        if (ally == null) return;

        int previousOwnedCount = _roster.OwnedAllyCount;
        _roster.AddOwnedAlly(ally);
        RefreshAllyItemModifiers();

        if (_roster.OwnedAllyCount != previousOwnedCount)
        {
            OnDeployedAllyCountChanged?.Invoke(DeployedAllyCount);
        }
    }

    private bool RemoveOwnedAlly(AllyUnit ally)
    {
        if (ally == null) return false;

        int previousOwnedCount = _roster.OwnedAllyCount;
        _roster.RemoveUnit(ally);
        _placementService.Remove(ally);
        bool removed = _roster.OwnedAllyCount != previousOwnedCount;
        if (removed)
        {
            OnDeployedAllyCountChanged?.Invoke(DeployedAllyCount);
        }

        return removed;
    }

    public void AddEnemy(UnitBase enemy)
    {
        _roster.AddEnemy(enemy);
    }

    public void NotifyUnitDied(UnitBase unit)
    {
        if (unit == null) return;

        if (unit is AllyUnit ally)
        {
            RemoveOwnedAlly(ally);
            RefreshAllyItemModifiers();
            _spawner.ReturnUnit(ally);
            return;
        }

        _roster.NotifyUnitDied(unit);
        _spawner.ReturnUnit(unit);
    }

    public void CleanupDestroyedUnits()
    {
        _roster.CleanupDestroyedUnits();
    }

    public void ResolveWaveResult()
    {
        ReturnAllEnemies();
        RestoreAlliesForPreparation();
    }

    public void ReleaseUnit(UnitBase unit)
    {
        if (unit == null) return;

        if (unit is AllyUnit ally)
        {
            RemoveOwnedAlly(ally);
        }
        else
        {
            _roster.RemoveUnit(unit);
        }

        _spawner.ReturnUnit(unit);
    }

    public static bool CanStartWaveWithAllyCount(int count)
    {
        return count >= 1 && count <= MaxDeployedAllyCount;
    }

    private void ReturnAllEnemies()
    {
        var snapshot = _roster.DrainEnemies();
        foreach (var unit in snapshot)
        {
            if (unit != null)
            {
                _spawner.ReturnUnit(unit);
            }
        }
    }

    private void RestoreAlliesForPreparation()
    {
        _roster.ClearActiveAllies();

        for (var i = 0; i < _roster.OwnedAllyCount; i++)
        {
            var ally = _roster.OwnedAllies[i];
            if (ally == null) continue;

            if (!_placementService.TryGetSavedPosition(
                    ally,
                    out Vector3 preparationPosition) &&
                (!_placementService.TryPlaceInFreeGridSlot(ally) ||
                 !_placementService.TryGetSavedPosition(
                     ally,
                     out preparationPosition)))
            {
                Debug.LogWarning(
                    "[UnitManager] Failed to restore ally preparation position.");
                continue;
            }

            ally.RestoreForPreparation(preparationPosition);
            ally.ResetMana();
            _roster.AddActiveAlly(ally);
        }

        RefreshAllyItemModifiers();
    }

    public bool CanDragAlly(AllyUnit ally)
    {
        return ally != null &&
               _battleManager != null &&
               _battleManager.CanUsePreparationActions &&
               _roster.OwnedAllies.Contains(ally) &&
               (_mergeService == null || !_mergeService.IsReserved(ally)) &&
               ally.IsAlive;
    }

    public void BeginAllyDragHighlight(AllyUnit source)
    {
        EndAllyDragHighlight();
        if (source == null || _titleData == null ||
            !_titleData.TryGetRootAllyJob(source.UnitId, out var sourceRoot))
        {
            return;
        }

        foreach (var ally in _roster.OwnedAllies)
        {
            if (ally == null || ally == source || ally.IsInPool ||
                !ally.IsAlive || !ally.gameObject.activeInHierarchy ||
                !_titleData.TryGetRootAllyJob(ally.UnitId, out var allyRoot))
            {
                continue;
            }

            ally.SetLineageHighlighted(allyRoot.id == sourceRoot.id);
        }
    }

    public void EndAllyDragHighlight()
    {
        if (_roster == null) return;

        foreach (var ally in _roster.OwnedAllies)
        {
            ally?.SetLineageHighlighted(false);
        }
    }

    public void RequestAllyDetail(AllyUnit ally)
    {
        if (ally == null ||
            !_roster.OwnedAllies.Contains(ally) ||
            ally.IsInPool) return;

        OnAllyDetailRequested?.Invoke(ally);
    }

    public bool IsValidAllyPlacement(
        AllyUnit ally,
        Vector3 position)
    {
        return CanDragAlly(ally) &&
               _placementService.IsValidPlacement(ally, position);
    }

    public void SaveAllyPreparationPosition(AllyUnit ally)
    {
        if (!CanDragAlly(ally) ||
            !IsValidAllyPlacement(ally, ally.transform.position)) return;

        _placementService.TrySave(ally, ally.transform.position);
    }

    public bool TryMergeAllies(
        AllyUnit source,
        AllyUnit target,
        Vector3 sourceOriginalPosition)
    {
        if (!CanDragAlly(source) || !CanDragAlly(target)) return false;
        UnitMergeDecision decision = _mergeService.TryBegin(source, target);
        if (decision.Type == UnitMergeDecisionType.Rejected)
        {
            if (decision.RestoreSourcePosition)
            {
                source.transform.position = sourceOriginalPosition;
            }

            return false;
        }

        if (decision.Type == UnitMergeDecisionType.Immediate)
        {
            ConsumeReservedInputs(decision);
            SpawnMergedAlly(
                decision.ResultUnitId,
                decision.ResultLevel,
                decision.ResultPosition);
            _mergeService.Complete(decision);
            OnAlliesMerged?.Invoke(decision.ResultLevel);
            return true;
        }

        if (!ChooseAutomaticEvolution())
        {
            source.transform.position = sourceOriginalPosition;
            _mergeService.CancelPendingEvolution();
            return false;
        }

        OnAlliesMerged?.Invoke(decision.ResultLevel);
        return true;
    }

    private bool ChooseAutomaticEvolution()
    {
        if (_mergeService == null ||
            !_mergeService.TryChooseAutomaticEvolution(
                out UnitMergeDecision decision)) return false;

        return CompleteEvolution(decision);
    }

    public bool ChooseEvolution(string unitId)
    {
        if (_mergeService == null ||
            !_mergeService.TryChooseEvolution(
                unitId,
                out UnitMergeDecision decision)) return false;

        return CompleteEvolution(decision);
    }

    private bool CompleteEvolution(UnitMergeDecision decision)
    {
        ConsumeReservedInputs(decision);
        AllyUnit evolvedAlly = SpawnMergedAlly(
            decision.ResultUnitId,
            decision.ResultLevel,
            decision.ResultPosition);
        _mergeService.Complete(decision);
        _battleManager.SetPreparationLock(false);

        if (evolvedAlly != null)
        {
            evolutionGlowEffect?.Play(evolvedAlly.transform.position);
            SoundManager.PlaySFXIfAvailable(SoundName.Evolution);
        }

        return true;
    }

    private void ConsumeReservedInputs(UnitMergeDecision decision)
    {
        ReleaseUnit(decision.Source);
        ReleaseUnit(decision.Target);
    }

    private AllyUnit SpawnMergedAlly(
        string unitId,
        int level,
        Vector3 position)
    {
        var data = new BattleUnitSpawnData
        {
            UnitId = unitId,
            Level = level
        };
        var result = SpawnAlly(data);
        if (result != null)
        {
            result.transform.position = position;
            _placementService.TrySave(result, position);
        }

        return result;
    }

    public UnitBase FindClosestAliveEnemy(Vector3 fromPosition, float maxDistance)
    {
        return _targetFinder.FindClosestAliveEnemy(fromPosition, maxDistance);
    }

    public UnitBase FindClosestAliveAlly(Vector3 fromPosition, float maxDistance)
    {
        return _targetFinder.FindClosestAliveAlly(fromPosition, maxDistance);
    }

    public UnitBase FindFarthestAliveAlly(Vector3 fromPosition)
    {
        return _targetFinder.FindFarthestAliveAlly(fromPosition);
    }

    public UnitBase FindHighestHpAliveAlly()
    {
        return _targetFinder.FindHighestHpAliveAlly();
    }

    public void ApplyEnemySpeedBuff(
        float moveSpeedMultiplier,
        float attackRateMultiplier)
    {
        foreach (var enemy in _roster.ActiveEnemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;

            enemy.ApplyPermanentSpeedMultiplier(
                moveSpeedMultiplier,
                attackRateMultiplier);
        }
    }

    public int CalculateRemainingBreachDamage()
    {
        int damage = 0;

        foreach (var enemy in _roster.ActiveEnemies)
        {
            if (enemy is EnemyUnit enemyUnit && enemyUnit.IsAlive)
            {
                damage += enemyUnit.BreachDamage;
            }
        }

        return damage;
    }

    public void GetAliveEnemiesInRadius(
        Vector3 center,
        float radius,
        List<UnitBase> result)
    {
        _targetFinder.GetAliveEnemiesInRadius(center, radius, result);
    }

    public void GetAliveAlliesInRadius(
        Vector3 center,
        float radius,
        List<UnitBase> result)
    {
        _targetFinder.GetAliveAlliesInRadius(center, radius, result);
    }

    public void GetEnemiesInLine(
        Vector3 origin,
        Vector3 direction,
        float distance,
        float halfWidth,
        List<UnitBase> result)
    {
        _targetFinder.GetEnemiesInLine(
            origin,
            direction,
            distance,
            halfWidth,
            result);
    }

    public void OnItemEvent(Item item)
    {
        _unitModifiers.Apply(
            item.Key,
            item.Value1,
            item.Value2,
            item.Value3);

        RefreshAllyItemModifiers();
    }

    private void RefreshAllyItemModifiers()
    {
        var unitTypes = new HashSet<string>();
        foreach (var ally in _roster.ActiveAllies)
        {
            if (ally != null)
            {
                unitTypes.Add(ally.name);
            }
        }

        UnitModifierSnapshot snapshot =
            _unitModifiers.GetRosterSnapshot(unitTypes.Count);

        foreach (var ally in _roster.ActiveAllies)
        {
            if (ally == null) continue;

            ally.ApplyItemModifiers(
                snapshot.AttackMultiplier,
                snapshot.AttackRateMultiplier,
                snapshot.HpMultiplier);
        }
    }

    protected override void OnDestroy()
    {
        _mergeService?.CancelPendingEvolution();
        if (App.TryGet<BattleManager>(out var registeredBattleManager))
        {
            registeredBattleManager.SetPreparationLock(false);
        }

        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnStateChanged;
        }

        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.Unsubscribe(EItem.BattleClock, this);
            itemManager.Unsubscribe(EItem.FieldArmor, this);
            itemManager.Unsubscribe(EItem.DiversityEmblem, this);
        }

        base.OnDestroy();
    }
}
