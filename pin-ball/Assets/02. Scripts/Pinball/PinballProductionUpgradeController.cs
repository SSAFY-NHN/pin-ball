using System;
using UnityEngine;

public enum EPinballProductionUpgrade
{
    BumperIncome,
    AddBall,
    SupplySpeed
}

[Serializable]
public struct PinballProductionUpgradeSettings
{
    public float BaseEffect;
    public float EffectPerLevel;
    public int BaseCost;
    public float CostMultiplier;
    public int MaxLevel;

    public PinballProductionUpgradeSettings(
        float baseEffect,
        float effectPerLevel,
        int baseCost,
        float costMultiplier,
        int maxLevel)
    {
        BaseEffect = baseEffect;
        EffectPerLevel = effectPerLevel;
        BaseCost = baseCost;
        CostMultiplier = costMultiplier;
        MaxLevel = maxLevel;
    }
}

public sealed class PinballProductionUpgradeController
{
    private readonly PinballProductionUpgradeSettings _bumperIncome;
    private readonly PinballProductionUpgradeSettings _addBall;
    private readonly PinballProductionUpgradeSettings _supplySpeed;
    private readonly float _minimumRespawnDelay;
    private readonly int[] _levels = new int[3];

    public int BumperIncome => Mathf.RoundToInt(GetEffect(EPinballProductionUpgrade.BumperIncome));
    public int PermanentBallCount => Mathf.RoundToInt(GetEffect(EPinballProductionUpgrade.AddBall));
    public float RespawnDelay => Mathf.Max(
        _minimumRespawnDelay,
        GetEffect(EPinballProductionUpgrade.SupplySpeed));

    public PinballProductionUpgradeController(
        PinballProductionUpgradeSettings bumperIncome,
        PinballProductionUpgradeSettings addBall,
        PinballProductionUpgradeSettings supplySpeed,
        float minimumRespawnDelay)
    {
        _bumperIncome = bumperIncome;
        _addBall = addBall;
        _supplySpeed = supplySpeed;
        _minimumRespawnDelay = Mathf.Max(0.01f, minimumRespawnDelay);
    }

    public int GetLevel(EPinballProductionUpgrade upgrade)
    {
        return _levels[(int)upgrade];
    }

    public int GetMaxLevel(EPinballProductionUpgrade upgrade)
    {
        return GetSettings(upgrade).MaxLevel;
    }

    public bool IsMaxLevel(EPinballProductionUpgrade upgrade)
    {
        return GetLevel(upgrade) >= GetMaxLevel(upgrade);
    }

    public int GetNextCost(EPinballProductionUpgrade upgrade)
    {
        PinballProductionUpgradeSettings settings = GetSettings(upgrade);
        double cost = settings.BaseCost * Math.Pow(
            settings.CostMultiplier,
            GetLevel(upgrade));
        return cost >= int.MaxValue ? int.MaxValue : Mathf.CeilToInt((float)cost);
    }

    public float GetEffect(EPinballProductionUpgrade upgrade)
    {
        PinballProductionUpgradeSettings settings = GetSettings(upgrade);
        return settings.BaseEffect + settings.EffectPerLevel * GetLevel(upgrade);
    }

    public float GetNextEffect(EPinballProductionUpgrade upgrade)
    {
        if (IsMaxLevel(upgrade)) return GetEffect(upgrade);
        PinballProductionUpgradeSettings settings = GetSettings(upgrade);
        float effect = settings.BaseEffect +
                       settings.EffectPerLevel * (GetLevel(upgrade) + 1);
        return upgrade == EPinballProductionUpgrade.SupplySpeed
            ? Mathf.Max(_minimumRespawnDelay, effect)
            : effect;
    }

    public bool CanPurchase(EPinballProductionUpgrade upgrade, int availableGold)
    {
        return !IsMaxLevel(upgrade) && availableGold >= GetNextCost(upgrade);
    }

    public bool TryPurchase(EPinballProductionUpgrade upgrade, int availableGold)
    {
        if (!CanPurchase(upgrade, availableGold)) return false;
        _levels[(int)upgrade]++;
        return true;
    }

    public void ResetForNewRun()
    {
        Array.Clear(_levels, 0, _levels.Length);
    }

    private PinballProductionUpgradeSettings GetSettings(
        EPinballProductionUpgrade upgrade)
    {
        return upgrade switch
        {
            EPinballProductionUpgrade.BumperIncome => _bumperIncome,
            EPinballProductionUpgrade.AddBall => _addBall,
            EPinballProductionUpgrade.SupplySpeed => _supplySpeed,
            _ => throw new ArgumentOutOfRangeException(nameof(upgrade), upgrade, null)
        };
    }
}
