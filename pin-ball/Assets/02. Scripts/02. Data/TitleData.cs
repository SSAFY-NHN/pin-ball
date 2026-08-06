using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[Serializable]
public class ItemData
{
    public string id;
    public int type;
    public int value1;
    public int value2;
    public int value3;
    public int cost;
    public string name;
    public string desc;
}

public class TitleData : AppService
{
    public Dictionary<string, ItemData> Item { get; private set; } = new();
   
    #region Data Path
    private const string ITEM_PATH = "Data/ItemData";
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
    }
}