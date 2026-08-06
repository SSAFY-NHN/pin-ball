using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

//소유: activeAllies, activeEnemies
//책임: 전투 중 목록 관리, 죽음/제거 반영, 웨이브 클리어 조건 계산
//금지: 골드/HP/웨이브 상태 변경
public class UnitManager : AppService, IItemEventListener
{
    private readonly List<UnitBase> _activeAllies = new();
    private readonly List<UnitBase> _activeEnemies = new();

    public int RemainingAllyCount => _activeAllies.Count;
    public int RemainingEnemyCount => _activeEnemies.Count;
    
    private BattleManager _battleManager;
    private UnitSpawner _spawner;
    private float _attackMultiplier = 1f;
    private float _attackRateMultiplier = 1f;
    private float _hpMultiplier = 1f;
    private float _diversityBonusPerType;
    private float _diversityMaxBonus;
    private float _duplicationChance;
    private int _duplicationTier;
    private int _duplicationCount;

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnStateChanged;
        _spawner = GetComponent<UnitSpawner>();

        var itemManager = App.Get<ItemManager>();
        itemManager.Subscribe(EItem.AttackManual, this);
        itemManager.Subscribe(EItem.BattleClock, this);
        itemManager.Subscribe(EItem.FieldArmor, this);
        itemManager.Subscribe(EItem.DuplicationSeal, this);
        itemManager.Subscribe(EItem.DiversityEmblem, this);
    }

    private void OnStateChanged(EWaveState state)
    {
        if (state is EWaveState.Active)
        {
            ClearAllEnemies();
            CleanupDestroyedUnits();
            SpawnEnemies(_battleManager.CurrentWave);
        }
    }

    private bool TryBuildUnitStats(BattleUnitSpawnData data, out BattleUnitStats finalStats)
    {
        finalStats = data.BaseStats;
        if (!IsValidStats(finalStats))
        {
            return false;
        }

        // TODO: 합성/장착 수치는 JSON 데이터(유닛/장비 테이블)에서 계산하도록 교체
        var attackMultiplier = 1f + (data.Modifier.MergeTier * data.Modifier.MergeAttackBonusPerTier);
        var hpMultiplier = 1f + (data.Modifier.MergeTier * data.Modifier.MergeHpBonusPerTier);

        finalStats.AttackDamage = (finalStats.AttackDamage * attackMultiplier) + data.Modifier.EquipmentAttackBonus;
        finalStats.MaxHp = (finalStats.MaxHp * hpMultiplier) + data.Modifier.EquipmentHpBonus;

        return IsValidStats(finalStats);
    }

    private static bool IsValidStats(BattleUnitStats stats)
    {
        return stats.MaxHp > 0f
               && stats.AttackDamage >= 0f
               && stats.AttackRate > 0f
               && stats.AttackRange > 0f
               && stats.MoveSpeed >= 0f;
    }
    
    public void SpawnAlly(BattleUnitSpawnData unitData)
    {
        SpawnAlly(unitData, 0f);
    }

    public void SpawnAlly(BattleUnitSpawnData unitData, float temporaryAttackBonus)
    {
        if (unitData == null)
        {
            Debug.LogWarning("[UnitManager] Ally spawn data is null.");
            return;
        }

        if (!TryBuildUnitStats(unitData, out var finalStats))
        {
            Debug.LogWarning($"[UnitManager] Invalid ally stats: {unitData.UnitId}");
            return;
        }

        finalStats.AttackDamage *= 1f + Mathf.Max(0f, temporaryAttackBonus);

        var spawnedUnit = _spawner.SpawnAlly(unitData, finalStats);
        AddAlly(spawnedUnit);
    }

    public bool TryDuplicateAlly(BattleUnitSpawnData unitData)
    {
        if (_duplicationChance <= 0f || unitData == null) return false;
        if (unitData.Modifier.MergeTier != _duplicationTier) return false;
        if (UnityEngine.Random.value > _duplicationChance) return false;

        for (var i = 0; i < _duplicationCount; i++)
        {
            SpawnAlly(unitData);
        }

        return true;
    }

    private void SpawnEnemies(BattleWaveData wave)
    {
        if (wave == null || wave.Enemies == null)
        {
            return;
        }

        for (var spawnIndex = 0; spawnIndex < wave.Enemies.Count; spawnIndex++)
        {
            var enemyData = wave.Enemies[spawnIndex];
            if (enemyData == null || !IsValidStats(enemyData.Stats))
            {
                Debug.LogWarning("[WaveBattleManager] Invalid enemy data skipped.");
                continue;
            }

            var enemy = _spawner.SpawnEnemy(enemyData, spawnIndex);
            if (enemy != null)
            {
                AddEnemy(enemy);
            }
        }
    }

    public void AddAlly(UnitBase ally)
    {
        if (ally == null) return;
        _activeAllies.Add(ally);
        RefreshAllyItemModifiers();
    }

    public void AddEnemy(UnitBase enemy)
    {
        if (enemy == null) return;
        _activeEnemies.Add(enemy);
    }

    public void NotifyUnitDied(UnitBase unit)
    {
        if (unit == null) return;

        if (unit.Team == EBattleTeam.Ally)
        {
            _activeAllies.Remove(unit);
            RefreshAllyItemModifiers();
        }
        else
        {
            _activeEnemies.Remove(unit);
        }
    }

    public void CleanupDestroyedUnits()
    {
        _activeAllies.RemoveAll(unit => unit == null || !unit.IsAlive);
        _activeEnemies.RemoveAll(unit => unit == null || !unit.IsAlive);
    }

    private void ClearAllEnemies()
    {
        for (var i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = _activeEnemies[i];
            if (enemy != null)
            {
                enemy.ForceRemove();
            }
        }

        _activeEnemies.Clear();
    }

    public UnitBase FindClosestAliveEnemy(Vector3 fromPosition, float maxDistance)
    {
        return FindClosest(fromPosition, maxDistance, _activeEnemies);
    }

    public UnitBase FindClosestAliveAlly(Vector3 fromPosition, float maxDistance)
    {
        return FindClosest(fromPosition, maxDistance, _activeAllies);
    }

    private static UnitBase FindClosest(
        Vector3 fromPosition,
        float maxDistance,
        List<UnitBase> candidates)
    {
        UnitBase best = null;
        var bestDistance = maxDistance;

        foreach (var candidate in candidates)
        {
            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }

            var distance = Vector2.Distance(fromPosition, candidate.transform.position);
            if (distance > bestDistance)
            {
                continue;
            }

            best = candidate;
            bestDistance = distance;
        }

        return best;
    }

    public void OnItemEvent(Item item)
    {
        switch (item.Key)
        {
            case EItem.AttackManual:
                _attackMultiplier = 1f + item.Value1;
                break;
            case EItem.BattleClock:
                _attackRateMultiplier = 1f + item.Value1;
                break;
            case EItem.FieldArmor:
                _hpMultiplier = 1f + item.Value1;
                break;
            case EItem.DuplicationSeal:
                _duplicationChance = item.Value1;
                _duplicationTier = Mathf.RoundToInt(item.Value2);
                _duplicationCount = Mathf.RoundToInt(item.Value3);
                break;
            case EItem.DiversityEmblem:
                _diversityBonusPerType = item.Value1;
                _diversityMaxBonus = item.Value2;
                break;
        }

        RefreshAllyItemModifiers();
    }

    private void RefreshAllyItemModifiers()
    {
        var unitTypes = new HashSet<string>();
        foreach (var ally in _activeAllies)
        {
            if (ally != null)
            {
                unitTypes.Add(ally.name);
            }
        }

        var diversityBonus = Mathf.Min(
            _diversityMaxBonus,
            unitTypes.Count * _diversityBonusPerType);

        foreach (var ally in _activeAllies)
        {
            if (ally == null) continue;

            ally.ApplyItemModifiers(
                _attackMultiplier + diversityBonus,
                _attackRateMultiplier,
                _hpMultiplier + diversityBonus);
        }
    }

    protected override void OnDestroy()
    {
        if (_battleManager != null)
        {
            _battleManager.OnStateChanged -= OnStateChanged;
        }

        if (App.TryGet<ItemManager>(out var itemManager))
        {
            itemManager.Unsubscribe(EItem.AttackManual, this);
            itemManager.Unsubscribe(EItem.BattleClock, this);
            itemManager.Unsubscribe(EItem.FieldArmor, this);
            itemManager.Unsubscribe(EItem.DuplicationSeal, this);
            itemManager.Unsubscribe(EItem.DiversityEmblem, this);
        }

        base.OnDestroy();
    }
}
