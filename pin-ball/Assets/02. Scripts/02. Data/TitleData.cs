using System;
using System.Collections.Generic;
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
}

[Serializable]
public class AllyUnitDataCollection
{
    public AllyCommonData common;
    public AllyUnitData[] units;
}
