using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemData
{
    public string id;
    public int key;
    public int type;
    public float value1;
    public float value2;
    public float value3;
    public int cost;
    public string name;
    public string desc;
}

public class TitleData : AppService, IUnitDataSource
{
    public Dictionary<string, ItemData> Item { get; private set; } = new();
    public Dictionary<string, AllyUnitData> AllyUnit { get; private set; } = new();
    public AllyCommonData AllyCommon { get; private set; }
    public Dictionary<string, EnemyUnitData> EnemyUnit { get; private set; } = new();
    public EnemyCommonData EnemyCommon { get; private set; }
    public BattleRunCommonData BattleRunCommon { get; private set; }
    public IReadOnlyList<BattleWaveData> BattleWaves => _battleWaves;
    public bool HasValidBattleRun { get; private set; }

    private readonly List<BattleWaveData> _battleWaves = new();
   
    #region Data Path
    private const string ITEM_PATH = "Data/ItemData";
    private const string ALLY_UNIT_PATH = "Data/AllyUnitData";
    private const string ENEMY_UNIT_PATH = "Data/EnemyUnitData";
    private const string BATTLE_WAVE_PATH = "Data/BattleWaveData";
    #endregion

    protected override void Awake()
    {
        base.Awake();

        LoadData();
    }

    private void LoadData()
    {
        var itemDataRaw = DataLoader.LoadData<ItemData>(ITEM_PATH);
        foreach (var data in itemDataRaw)
        {
            Item.Add(data.id, data);
        }

        var allyData = DataLoader.LoadObject<AllyUnitDataCollection>(ALLY_UNIT_PATH);
        if (allyData != null)
        {
            AllyCommon = allyData.common;
            foreach (var data in allyData.units)
            {
                AllyUnit.Add(data.id, data);
            }
        }

        var enemyData = DataLoader.LoadObject<EnemyUnitDataCollection>(ENEMY_UNIT_PATH);
        if (enemyData != null)
        {
            EnemyCommon = enemyData.common;
            foreach (var data in enemyData.units)
            {
                EnemyUnit.Add(data.id, data);
            }
        }

        LoadBattleWaveData();
    }

    private void LoadBattleWaveData()
    {
        HasValidBattleRun = false;
        BattleRunCommon = null;
        _battleWaves.Clear();

        var collection =
            DataLoader.LoadObject<BattleWaveDataCollection>(BATTLE_WAVE_PATH);
        if (collection == null || collection.common == null)
        {
            Debug.LogError("[TitleData] Battle wave common data is missing.");
            return;
        }

        BattleRunCommon = collection.common;
        if (BattleRunCommon.StartingGold < 0 ||
            BattleRunCommon.BaseLaunchCost < 0 ||
            BattleRunCommon.LaunchCostIncrease < 0)
        {
            Debug.LogError(
                "[TitleData] Battle run economy values cannot be negative.");
            return;
        }

        if (!ValidateBattleWaves(collection.waves)) return;

        _battleWaves.AddRange(collection.waves);
        HasValidBattleRun = true;
    }

    private bool ValidateBattleWaves(BattleWaveData[] waves)
    {
        if (waves == null || waves.Length != 10)
        {
            Debug.LogError(
                $"[TitleData] Battle wave count must be 10: {waves?.Length ?? 0}");
            return false;
        }

        bool isValid = true;
        for (var waveIndex = 0; waveIndex < waves.Length; waveIndex++)
        {
            var wave = waves[waveIndex];
            int waveNumber = waveIndex + 1;
            if (wave == null)
            {
                Debug.LogError($"[TitleData] Wave {waveNumber} is null.");
                isValid = false;
                continue;
            }

            if (wave.Enemies == null || wave.Enemies.Count == 0)
            {
                Debug.LogError(
                    $"[TitleData] Wave {waveNumber} has no enemies.");
                isValid = false;
                continue;
            }

            foreach (var enemy in wave.Enemies)
            {
                if (enemy == null ||
                    string.IsNullOrEmpty(enemy.EnemyId) ||
                    !EnemyUnit.ContainsKey(enemy.EnemyId))
                {
                    Debug.LogError(
                        $"[TitleData] Wave {waveNumber} has an invalid enemy: " +
                        $"{enemy?.EnemyId ?? "null"}");
                    isValid = false;
                    continue;
                }

                if (enemy.Count < 1)
                {
                    Debug.LogError(
                        $"[TitleData] Wave {waveNumber} enemy count is invalid: " +
                        $"{enemy.EnemyId}={enemy.Count}");
                    isValid = false;
                }
            }
        }

        var finalWave = waves[9];
        bool hasFinalBoss = false;
        if (finalWave?.Enemies != null)
        {
            foreach (var enemy in finalWave.Enemies)
            {
                if (enemy?.EnemyId == "goblin_king")
                {
                    hasFinalBoss = true;
                    break;
                }
            }
        }

        if (!hasFinalBoss)
        {
            Debug.LogError("[TitleData] Wave 10 must contain goblin_king.");
            isValid = false;
        }

        return isValid;
    }

    public bool TryGetAllyUnit(string id, out AllyUnitData result)
    {
        return AllyUnit.TryGetValue(id, out result);
    }

    public bool TryGetEnemyUnit(string id, out EnemyUnitData result)
    {
        return EnemyUnit.TryGetValue(id, out result);
    }

    public bool TryGetRootAllyJob(
        string unitId,
        out AllyUnitData rootJob)
    {
        rootJob = null;
        var visitedIds = new HashSet<string>();
        var currentId = unitId;

        while (!string.IsNullOrEmpty(currentId))
        {
            if (!visitedIds.Add(currentId))
            {
                Debug.LogError(
                    $"[TitleData] Ally job cycle detected: {unitId}");
                return false;
            }

            if (!AllyUnit.TryGetValue(currentId, out var currentJob))
            {
                Debug.LogError(
                    $"[TitleData] Ally job not found: {currentId}");
                return false;
            }

            if (string.IsNullOrEmpty(currentJob.previousJob))
            {
                rootJob = currentJob;
                return true;
            }

            currentId = currentJob.previousJob;
        }

        return false;
    }

    public void GetNextAllyJobs(
        string previousJobId,
        List<AllyUnitData> result)
    {
        if (result == null) return;

        result.Clear();
        foreach (var unit in AllyUnit.Values)
        {
            if (unit != null && unit.previousJob == previousJobId)
            {
                result.Add(unit);
            }
        }

        result.Sort((left, right) =>
            string.CompareOrdinal(left.id, right.id));
    }
}

[Serializable]
public class AllyUnitDataCollection
{
    public AllyCommonData common;
    public AllyUnitData[] units;
}
