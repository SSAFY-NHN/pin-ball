using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

public class TitleData : AppService
{
    public Dictionary<string, ItemData> Item { get; private set; } = new();
    public Dictionary<string, AllyUnitData> AllyUnit { get; private set; } = new();
    public AllyCommonData AllyCommon { get; private set; }
    public Dictionary<string, EnemyUnitData> EnemyUnit { get; private set; } = new();
    public EnemyCommonData EnemyCommon { get; private set; }
   
    #region Data Path
    private const string ITEM_PATH = "Data/ItemData";
    private const string ALLY_UNIT_PATH = "Data/AllyUnitData";
    private const string ENEMY_UNIT_PATH = "Data/EnemyUnitData";
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
