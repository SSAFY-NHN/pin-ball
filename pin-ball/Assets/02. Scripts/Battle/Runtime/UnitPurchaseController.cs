using System;
using System.Collections.Generic;

using UnityEngine;

[Serializable]
public struct UnitPurchaseSettings
{
    public string UnitId;
    [Min(0)] public int BaseCost;
    [Min(1f)] public float CostMultiplier;

    public UnitPurchaseSettings(
        string unitId,
        int baseCost,
        float costMultiplier)
    {
        UnitId = unitId;
        BaseCost = baseCost;
        CostMultiplier = costMultiplier;
    }
}

public readonly struct UnitPurchaseResult
{
    public string UnitId { get; }
    public int PurchaseCount { get; }
    public int Cost { get; }

    public UnitPurchaseResult(string unitId, int purchaseCount, int cost)
    {
        UnitId = unitId;
        PurchaseCount = purchaseCount;
        Cost = cost;
    }
}

public sealed class UnitPurchaseController
{
    private readonly BattleEconomy economy;
    private readonly Dictionary<string, UnitPurchaseSettings> settingsByUnitId =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> purchaseCounts =
        new(StringComparer.Ordinal);

    public UnitPurchaseController(
        BattleEconomy economy,
        params UnitPurchaseSettings[] settings)
    {
        this.economy = economy;
        if (settings == null) return;

        foreach (UnitPurchaseSettings entry in settings)
        {
            if (string.IsNullOrWhiteSpace(entry.UnitId) ||
                entry.BaseCost < 0 ||
                entry.CostMultiplier < 1f)
            {
                continue;
            }

            settingsByUnitId[entry.UnitId] = entry;
            purchaseCounts[entry.UnitId] = 0;
        }
    }

    public int GetPurchaseCount(string unitId)
    {
        return unitId != null && purchaseCounts.TryGetValue(unitId, out int count)
            ? count
            : 0;
    }

    public int GetNextCost(string unitId)
    {
        if (unitId == null ||
            !settingsByUnitId.TryGetValue(unitId, out UnitPurchaseSettings settings))
        {
            return 0;
        }

        double cost = settings.BaseCost * Math.Pow(
            settings.CostMultiplier,
            GetPurchaseCount(unitId));
        return cost >= int.MaxValue
            ? int.MaxValue
            : Mathf.CeilToInt((float)cost);
    }

    public bool CanPurchase(string unitId, bool canDeploy)
    {
        return canDeploy &&
               economy != null &&
               unitId != null &&
               settingsByUnitId.ContainsKey(unitId) &&
               economy.Gold >= GetNextCost(unitId);
    }

    public bool TryPurchase(
        string unitId,
        bool canDeploy,
        Func<BattleUnitSpawnData, bool> trySpawn,
        out UnitPurchaseResult result)
    {
        result = default;
        if (!CanPurchase(unitId, canDeploy) || trySpawn == null) return false;

        int cost = GetNextCost(unitId);
        var spawnData = new BattleUnitSpawnData
        {
            UnitId = unitId,
            Level = 1
        };
        if (!trySpawn(spawnData)) return false;
        if (!economy.TrySpend(cost)) return false;
        if (!RecordSuccessfulPurchase(unitId)) return false;

        result = new UnitPurchaseResult(
            unitId,
            GetPurchaseCount(unitId),
            cost);
        return true;
    }

    public bool RecordSuccessfulPurchase(string unitId)
    {
        if (unitId == null || !purchaseCounts.ContainsKey(unitId)) return false;

        purchaseCounts[unitId]++;
        return true;
    }
}
