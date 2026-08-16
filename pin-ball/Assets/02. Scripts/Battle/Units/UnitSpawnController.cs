using UnityEngine;

public sealed class UnitSpawnController
{
    private readonly IUnitDataSource _dataSource;
    private readonly UnitSpawner _spawner;
    private readonly UnitCombatContext _combatContext;
    private readonly UnitManager _unitManager;
    private readonly UnitCreationService _creationService;
    private readonly UnitSkillRegistry _skillRegistry;
    private int _enemySpawnIndex;

    public UnitSpawnController(
        IUnitDataSource dataSource,
        UnitSpawner spawner,
        UnitCombatContext combatContext,
        UnitManager unitManager)
    {
        _dataSource = dataSource;
        _spawner = spawner;
        _combatContext = combatContext;
        _unitManager = unitManager;
        _creationService = new UnitCreationService(dataSource);
        _skillRegistry = UnitSkillRegistry.CreateDefault();
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

        if (!_creationService.TryCreateAlly(
                unitData,
                temporaryAttackBonus,
                out AllyUnitData allyData,
                out BattleUnitStats finalStats))
        {
            Debug.LogWarning($"[UnitManager] Invalid ally stats: {unitData.UnitId}");
            return null;
        }

        return _spawner.SpawnAlly(
            unitData,
            allyData,
            _dataSource.AllyCommon,
            finalStats,
            _combatContext,
            _unitManager,
            _skillRegistry);
    }

    public void BeginEnemyWave()
    {
        _enemySpawnIndex = 0;
    }

    public EnemyUnit SpawnEnemy(
        string enemyId,
        int wave,
        Vector3? spawnPosition)
    {
        if (!_creationService.TryCreateEnemy(
                enemyId,
                wave,
                out EnemyUnitData enemyData,
                out BattleUnitStats stats))
        {
            Debug.LogWarning($"[UnitManager] Invalid enemy stats: {enemyId}");
            return null;
        }

        return _spawner.SpawnEnemy(
            enemyData,
            stats,
            _enemySpawnIndex++,
            _combatContext,
            spawnPosition,
            _unitManager,
            _skillRegistry);
    }
}
