using System;
using UnityEngine;

public enum EBattleUpgrade
{
    UnitPurchase,
    AllyAttack,
    DefenseLineHp
}

[Serializable]
public struct BattleUpgradeSettings
{
    public float BaseEffect;
    public float EffectPerLevel;
    public int BaseCost;
    public float CostMultiplier;
    public int MaxLevel;

    public BattleUpgradeSettings(
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

public sealed class BattleUpgradeController
{
    private readonly BattleUpgradeSettings unitPurchase;
    private readonly BattleUpgradeSettings allyAttack;
    private readonly BattleUpgradeSettings defenseLineHp;
    private readonly int[] levels = new int[3];

    public BattleUpgradeController(
        BattleUpgradeSettings unitPurchase,
        BattleUpgradeSettings allyAttack,
        BattleUpgradeSettings defenseLineHp)
    {
        this.unitPurchase = unitPurchase;
        this.allyAttack = allyAttack;
        this.defenseLineHp = defenseLineHp;
    }

    public int GetLevel(EBattleUpgrade upgrade) => levels[(int)upgrade];

    public int GetMaxLevel(EBattleUpgrade upgrade)
    {
        return upgrade switch
        {
            EBattleUpgrade.UnitPurchase => UnitManager.MaxDeployedAllyCount,
            EBattleUpgrade.AllyAttack => Mathf.Max(0, allyAttack.MaxLevel),
            EBattleUpgrade.DefenseLineHp => Mathf.Max(0, defenseLineHp.MaxLevel),
            _ => 0
        };
    }

    public bool IsMaxLevel(EBattleUpgrade upgrade, int ownedAllyCount)
    {
        return upgrade == EBattleUpgrade.UnitPurchase
            ? ownedAllyCount >= UnitManager.MaxDeployedAllyCount
            : GetLevel(upgrade) >= GetMaxLevel(upgrade);
    }

    public int GetNextCost(EBattleUpgrade upgrade)
    {
        BattleUpgradeSettings settings = GetSettings(upgrade);
        int level = GetLevel(upgrade);
        double cost = settings.BaseCost * Math.Pow(settings.CostMultiplier, level);
        return cost >= int.MaxValue ? int.MaxValue : Mathf.CeilToInt((float)cost);
    }

    public float GetEffect(EBattleUpgrade upgrade)
    {
        if (upgrade == EBattleUpgrade.UnitPurchase) return 0f;
        BattleUpgradeSettings settings = GetSettings(upgrade);
        return settings.BaseEffect + settings.EffectPerLevel * GetLevel(upgrade);
    }

    public float GetNextEffect(EBattleUpgrade upgrade)
    {
        if (upgrade == EBattleUpgrade.UnitPurchase) return 0f;
        if (GetLevel(upgrade) >= GetMaxLevel(upgrade)) return GetEffect(upgrade);
        BattleUpgradeSettings settings = GetSettings(upgrade);
        return settings.BaseEffect +
               settings.EffectPerLevel * (GetLevel(upgrade) + 1);
    }

    public void ConfirmPurchase(EBattleUpgrade upgrade)
    {
        levels[(int)upgrade]++;
    }

    private BattleUpgradeSettings GetSettings(EBattleUpgrade upgrade)
    {
        return upgrade switch
        {
            EBattleUpgrade.UnitPurchase => unitPurchase,
            EBattleUpgrade.AllyAttack => allyAttack,
            EBattleUpgrade.DefenseLineHp => defenseLineHp,
            _ => default
        };
    }
}
