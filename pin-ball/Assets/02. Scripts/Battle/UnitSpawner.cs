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

    private int _allySpawnIndex;

    public UnitBase SpawnAlly(
        BattleUnitSpawnData data,
        AllyUnitData allyData,
        AllyCommonData commonData,
        BattleUnitStats stats)
    {
        if (data == null)
        {
            return null;
        }

        var unit = Spawn(
            EBattleTeam.Ally,
            data.UnitId,
            stats,
            _allySpawnIndex++);

        if (unit is AllyUnit ally)
        {
            ally.SetData(data.UnitId, data.Level, allyData?.skill, commonData);
        }

        return unit;
    }

    public EnemyUnit SpawnEnemy(
        EnemyUnitData data,
        BattleUnitStats stats,
        int spawnIndex,
        Vector3? spawnPosition = null)
    {
        if (data == null)
        {
            return null;
        }

        var unit = Spawn(
            EBattleTeam.Enemy,
            data.id,
            stats,
            spawnIndex,
            spawnPosition) as EnemyUnit;

        unit?.SetData(data);
        return unit;
    }

    private UnitBase Spawn(
        EBattleTeam team,
        string unitId,
        BattleUnitStats stats,
        int spawnIndex,
        Vector3? overridePosition = null)
    {
        var position = overridePosition ??
            (team == EBattleTeam.Ally ? allySpawnPoint.position : enemySpawnPoint.position);
        position.x += Random.Range(-0.15f, 0.15f);
        position.y += GetFormationOffset(spawnIndex);
        
        var unit = Instantiate(team == EBattleTeam.Ally ? allyPrefab : enemyPrefab, 
            position, 
            Quaternion.identity).GetComponent<UnitBase>();

        unit.name = $"{team}_{unitId}";
  
        unit.Initialize(stats);
        return unit;
    }

    private static float GetFormationOffset(int spawnIndex)
    {
        if (spawnIndex <= 0)
        {
            return 0f;
        }

        return -spawnIndex * FormationSpacing;
    }
}
