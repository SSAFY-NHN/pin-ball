using System;
using System.Collections;
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
    internal event Action OnBattleRosterChanged;

    private UnitRoster _roster;
    private UnitTargetFinder _targetFinder;
    private UnitCombatContext _combatContext;
    private UnitSpawnController _spawnController;
    private UnitPreparationController _preparationController;
    private UnitItemController _itemController;

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
    private ItemManager _itemManager;
    private Coroutine _automaticPotionCoroutine;

    protected override void Awake()
    {
        base.Awake();
        _spawner = GetComponent<UnitSpawner>();
        _roster = new UnitRoster();
        _targetFinder = new UnitTargetFinder(_roster);
    }

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _titleData = App.Get<TitleData>();
        _combatContext = new UnitCombatContext(
            _targetFinder,
            battleArea,
            NotifyUnitDied,
            NotifyUnitDamaged);
        _spawnController = new UnitSpawnController(
            _titleData,
            _spawner,
            _combatContext,
            this);
        _preparationController = new UnitPreparationController(
            _roster,
            _titleData,
            battleArea);
        _battleManager.OnStateChanged += OnStateChanged;

        _itemManager = App.Get<ItemManager>();
        _itemController = new UnitItemController(_itemManager);
        _itemManager.Subscribe(EItem.BattleClock, this);
        _itemManager.Subscribe(EItem.FieldArmor, this);
        _itemManager.Subscribe(EItem.DiversityEmblem, this);
    }

    private void NotifyUnitDamaged(UnitBase unit)
    {
        if (unit == null || unit.Team != EBattleTeam.Ally ||
            _automaticPotionCoroutine != null) return;

        _automaticPotionCoroutine = StartCoroutine(CheckAutomaticPotionNextFrame());
    }

    private IEnumerator CheckAutomaticPotionNextFrame()
    {
        yield return null;
        _automaticPotionCoroutine = null;
        if (_battleManager != null && _battleManager.State == EWaveState.Active)
        {
            _itemController.TryUseAutomaticPotion(_roster.ActiveAllies);
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

        var spawnedUnit = _spawnController?.SpawnAlly(
            unitData,
            temporaryAttackBonus);
        if (spawnedUnit == null)
        {
            return null;
        }

        if (!_preparationController.TryPlaceInFreeGridSlot(spawnedUnit))
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

        _spawnController.BeginEnemyWave();
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
        var enemy = _spawnController?.SpawnEnemy(
            enemyId,
            wave,
            spawnPosition);
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

        OnBattleRosterChanged?.Invoke();
    }

    private bool RemoveOwnedAlly(AllyUnit ally)
    {
        if (ally == null) return false;

        int previousOwnedCount = _roster.OwnedAllyCount;
        _roster.RemoveUnit(ally);
        _preparationController.Remove(ally);
        bool removed = _roster.OwnedAllyCount != previousOwnedCount;
        if (removed)
        {
            OnDeployedAllyCountChanged?.Invoke(DeployedAllyCount);
            OnBattleRosterChanged?.Invoke();
        }

        return removed;
    }

    public void AddEnemy(UnitBase enemy)
    {
        if (_roster.AddEnemy(enemy)) OnBattleRosterChanged?.Invoke();
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
        OnBattleRosterChanged?.Invoke();
    }

    public void CleanupDestroyedUnits()
    {
        int previousAllyCount = _roster.ActiveAllyCount;
        int previousEnemyCount = _roster.ActiveEnemyCount;
        _roster.CleanupDestroyedUnits();
        if (previousAllyCount != _roster.ActiveAllyCount ||
            previousEnemyCount != _roster.ActiveEnemyCount)
        {
            OnBattleRosterChanged?.Invoke();
        }
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
            if (_roster.RemoveUnit(unit)) OnBattleRosterChanged?.Invoke();
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

            if (!_preparationController.TryGetSavedPosition(
                    ally,
                    out Vector3 preparationPosition) &&
                (!_preparationController.TryPlaceInFreeGridSlot(ally) ||
                 !_preparationController.TryGetSavedPosition(
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
        return _preparationController != null &&
               _preparationController.CanDrag(
                   ally,
                   _battleManager != null &&
                   _battleManager.CanUsePreparationActions);
    }

    public void BeginAllyDragHighlight(AllyUnit source)
    {
        _preparationController?.BeginLineageHighlight(source);
    }

    public void EndAllyDragHighlight()
    {
        _preparationController?.EndLineageHighlight();
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
               _preparationController.IsValidPlacement(ally, position);
    }

    public void SaveAllyPreparationPosition(AllyUnit ally)
    {
        if (!CanDragAlly(ally) ||
            !IsValidAllyPlacement(ally, ally.transform.position)) return;

        _preparationController.TrySave(ally, ally.transform.position);
    }

    public bool TryMergeAllies(
        AllyUnit source,
        AllyUnit target,
        Vector3 sourceOriginalPosition)
    {
        if (!CanDragAlly(source) || !CanDragAlly(target)) return false;
        UnitMergeDecision decision = _preparationController.TryBeginMerge(
            source,
            target,
            sourceOriginalPosition);
        if (decision.Type == UnitMergeDecisionType.Rejected) return false;

        if (decision.Type == UnitMergeDecisionType.Immediate)
        {
            ConsumeReservedInputs(decision);
            SpawnMergedAlly(
                decision.ResultUnitId,
                decision.ResultLevel,
                decision.ResultPosition);
            _preparationController.Complete(decision);
            OnAlliesMerged?.Invoke(decision.ResultLevel);
            return true;
        }

        _battleManager.SetPreparationLock(true);
        if (OnEvolutionRequested == null)
        {
            CancelPendingEvolution();
            return false;
        }

        OnEvolutionRequested.Invoke(decision.FirstChoice, decision.SecondChoice);
        return true;
    }

    public bool ChooseEvolution(string unitId)
    {
        if (_preparationController == null ||
            !_preparationController.TryChooseEvolution(
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
        _preparationController.Complete(decision);
        _battleManager.SetPreparationLock(false);

        if (evolvedAlly != null)
        {
            evolutionGlowEffect?.Play(evolvedAlly.transform.position);
            SoundManager.PlaySFXIfAvailable(SoundName.Evolution);
        }

        OnAlliesMerged?.Invoke(decision.ResultLevel);
        return true;
    }

    internal void CancelPendingEvolution()
    {
        _preparationController?.CancelPendingEvolution();
        _battleManager?.SetPreparationLock(false);
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
            _preparationController.TrySave(result, position);
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
        _itemController.Apply(item, _roster.ActiveAllies);
    }

    private void RefreshAllyItemModifiers()
    {
        _itemController?.Refresh(_roster.ActiveAllies);
    }

    protected override void OnDestroy()
    {
        if (_automaticPotionCoroutine != null)
        {
            StopCoroutine(_automaticPotionCoroutine);
            _automaticPotionCoroutine = null;
        }
        _preparationController?.CancelPendingEvolution();
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
