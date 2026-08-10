using UnityEngine;

public sealed class UnitCreationService
{
    private readonly IUnitDataSource _dataSource;

    public UnitCreationService(IUnitDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public bool TryCreateAlly(
        BattleUnitSpawnData spawnData,
        float temporaryAttackBonus,
        out AllyUnitData allyData,
        out BattleUnitStats stats)
    {
        allyData = null;
        stats = default;
        if (spawnData == null ||
            _dataSource == null ||
            _dataSource.AllyCommon == null ||
            !_dataSource.TryGetAllyUnit(spawnData.UnitId, out allyData))
        {
            return false;
        }

        int maxLevel = Mathf.Max(1, _dataSource.AllyCommon.maxLevel);
        int classLevel = Mathf.Clamp(
            _dataSource.AllyCommon.classLevel,
            1,
            maxLevel);
        int minLevel = string.IsNullOrEmpty(allyData.previousJob)
            ? 1
            : classLevel;
        spawnData.Level = Mathf.Clamp(spawnData.Level, minLevel, maxLevel);
        stats = allyData.CreateStats(spawnData.Level, classLevel);
        if (!UnitStatsValidator.IsValid(stats)) return false;

        float attackMultiplier = 1f +
            spawnData.Modifier.MergeTier *
            spawnData.Modifier.MergeAttackBonusPerTier;
        float hpMultiplier = 1f +
            spawnData.Modifier.MergeTier *
            spawnData.Modifier.MergeHpBonusPerTier;

        stats.AttackDamage =
            stats.AttackDamage * attackMultiplier +
            spawnData.Modifier.EquipmentAttackBonus;
        stats.MaxHp =
            stats.MaxHp * hpMultiplier +
            spawnData.Modifier.EquipmentHpBonus;
        stats.AttackDamage *= 1f + Mathf.Max(0f, temporaryAttackBonus);

        return UnitStatsValidator.IsValid(stats);
    }

    public bool TryCreateEnemy(
        string enemyId,
        int wave,
        out EnemyUnitData enemyData,
        out BattleUnitStats stats)
    {
        enemyData = null;
        stats = default;
        if (_dataSource == null ||
            _dataSource.EnemyCommon == null ||
            !_dataSource.TryGetEnemyUnit(enemyId, out enemyData))
        {
            return false;
        }

        stats = enemyData.CreateStats(wave, _dataSource.EnemyCommon);
        return UnitStatsValidator.IsValid(stats);
    }
}
