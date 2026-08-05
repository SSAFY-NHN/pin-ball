using UnityEngine;

//소유: 프리팹/스폰포인트 참조
//책임: 유닛 생성 + Initialize(...)만 수행 후 반환
//금지: 리스트 등록, 승패/보상 처리
public class UnitSpawner : MonoBehaviour
{
    [SerializeField] private GameObject allyPrefab;
    [SerializeField] private GameObject enemyPrefab;
    
    [SerializeField] private Transform allySpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;

    public UnitBase SpawnAlly(
        BattleUnitSpawnData data,
        BattleUnitStats stats)
    {
        if (data == null)
        {
            return null;
        }

        return Spawn(
            EBattleTeam.Ally,
            data.UnitId,
            stats);
    }

    public UnitBase SpawnEnemy(BattleEnemySpawnData data)
    {
        if (data == null)
        {
            return null;
        }

        return Spawn(
            EBattleTeam.Enemy,
            data.EnemyId,
            data.Stats);
    }

    private UnitBase Spawn(
        EBattleTeam team,
        string unitId,
        BattleUnitStats stats)
    {
        var position = team == EBattleTeam.Ally ? allySpawnPoint.position : enemySpawnPoint.position;
        position.x += Random.Range(-0.5f, 0.5f);
        position.y += Random.Range(-0.5f, 0.5f);
        
        var unit = Instantiate(team == EBattleTeam.Ally ? allyPrefab : enemyPrefab, 
            position, 
            Quaternion.identity).GetComponent<UnitBase>();

        unit.name = $"{team}_{unitId}";
  
        unit.Initialize(stats);
        return unit;
    }
}
