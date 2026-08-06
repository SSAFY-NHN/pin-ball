using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

//소유: activeAllies, activeEnemies
//책임: 전투 중 목록 관리, 죽음/제거 반영, 웨이브 클리어 조건 계산
//금지: 골드/HP/웨이브 상태 변경
public class UnitManager : AppService
{
    private readonly List<UnitBase> _activeAllies = new();
    private readonly List<UnitBase> _activeEnemies = new();

    public int RemainingAllyCount => _activeAllies.Count;
    public int RemainingEnemyCount => _activeEnemies.Count;
    
    private BattleManager _battleManager;
    private UnitSpawner _spawner;

    private void Start()
    {
        _battleManager = App.Get<BattleManager>();
        _battleManager.OnStateChanged += OnStateChanged;
        _spawner = GetComponent<UnitSpawner>();
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
        if (unitData == null) return;
        if (!TryBuildUnitStats(unitData, out var finalStats)) return;

        var spawnedUnit = _spawner.SpawnAlly(unitData, finalStats);
        AddAlly(spawnedUnit);
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
        foreach (var enemy in _activeEnemies)
        {
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
}
