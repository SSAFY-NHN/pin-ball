using System.Collections.Generic;

using UnityEngine;

//소유: 프리팹/스폰포인트 참조
//책임: 유닛 생성 + Initialize(...)만 수행 후 반환
//금지: 리스트 등록, 승패/보상 처리
public class UnitSpawner : MonoBehaviour
{
    private const float FormationSpacing = 0.75f;

    [SerializeField] private GameObject allyPrefab;
    [SerializeField] private GameObject enemyPrefab;
    
    [SerializeField] private Transform allySpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private Transform allyPoolParent;
    [SerializeField] private Transform enemyPoolParent;

    private readonly Queue<AllyUnit> _allyPool = new();
    private readonly Queue<EnemyUnit> _enemyPool = new();
    private readonly HashSet<UnitBase> _pooledUnits = new();

    public AllyUnit SpawnAlly(
        BattleUnitSpawnData data,
        AllyUnitData allyData,
        AllyCommonData commonData,
        BattleUnitStats stats,
        UnitCombatContext context,
        UnitManager unitManager = null,
        UnitSkillRegistry skillRegistry = null)
    {
        if (data == null)
        {
            return null;
        }

        var ally = TakeAlly();
        if (ally == null) return null;

        ActivateUnit(
            ally,
            EBattleTeam.Ally,
            data.UnitId,
            stats,
            0,
            context);
        ally.SetData(data.UnitId, data.Level, allyData?.skill, commonData, unitManager, skillRegistry);
        return ally;
    }

    public EnemyUnit SpawnEnemy(
        EnemyUnitData data,
        BattleUnitStats stats,
        int spawnIndex,
        UnitCombatContext context,
        Vector3? spawnPosition = null,
        UnitManager unitManager = null,
        UnitSkillRegistry skillRegistry = null,
        int waveNumber = 1)
    {
        if (data == null)
        {
            return null;
        }

        var unit = TakeEnemy();
        if (unit == null) return null;

        ActivateUnit(
            unit,
            EBattleTeam.Enemy,
            data.id,
            stats,
            spawnIndex,
            context,
            spawnPosition);

        unit?.SetData(data, unitManager, skillRegistry, waveNumber);
        return unit;
    }

    public void ReturnUnit(UnitBase unit)
    {
        if (unit == null || _pooledUnits.Contains(unit)) return;

        _pooledUnits.Add(unit);
        Transform poolParent = unit.Team == EBattleTeam.Ally
            ? allyPoolParent
            : enemyPoolParent;
        unit.transform.SetParent(poolParent != null ? poolParent : transform, false);
        unit.MarkReturnedToPool();

        if (unit is AllyUnit ally)
        {
            _allyPool.Enqueue(ally);
        }
        else if (unit is EnemyUnit enemy)
        {
            _enemyPool.Enqueue(enemy);
        }
    }

    private void ActivateUnit(
        UnitBase unit,
        EBattleTeam team,
        string unitId,
        BattleUnitStats stats,
        int spawnIndex,
        UnitCombatContext context,
        Vector3? overridePosition = null)
    {
        var spawnPoint = team == EBattleTeam.Ally
            ? allySpawnPoint
            : enemySpawnPoint;
        var position = overridePosition ??
            (spawnPoint != null ? spawnPoint.position : transform.position);
        position.x += Random.Range(-0.15f, 0.15f);
        if (team == EBattleTeam.Enemy)
        {
            position.y += GetFormationOffset(spawnIndex);
        }
        
        unit.transform.SetParent(null, true);
        unit.transform.SetPositionAndRotation(position, Quaternion.identity);
        unit.gameObject.SetActive(true);
        unit.name = $"{team}_{unitId}";
        unit.Initialize(stats, context);
    }

    private AllyUnit TakeAlly()
    {
        while (_allyPool.Count > 0)
        {
            var unit = _allyPool.Dequeue();
            if (unit == null) continue;
            _pooledUnits.Remove(unit);
            return unit;
        }

        if (allyPrefab == null)
        {
            Debug.LogError("[UnitSpawner] Ally prefab이 없습니다.");
            return null;
        }

        return Instantiate(allyPrefab).GetComponent<AllyUnit>();
    }

    private EnemyUnit TakeEnemy()
    {
        while (_enemyPool.Count > 0)
        {
            var unit = _enemyPool.Dequeue();
            if (unit == null) continue;
            _pooledUnits.Remove(unit);
            return unit;
        }

        if (enemyPrefab == null)
        {
            Debug.LogError("[UnitSpawner] Enemy prefab이 없습니다.");
            return null;
        }

        return Instantiate(enemyPrefab).GetComponent<EnemyUnit>();
    }

    private float GetFormationOffset(int spawnIndex)
    {
        if (spawnIndex <= 0)
        {
            return 0f;
        }

        return -spawnIndex * FormationSpacing;
    }
}
