using System;
using UnityEngine;

public enum EBattleUpgrade
{
    AllyAttack = 1,
    DefenseLineHp = 2
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
    private readonly BattleUpgradeSettings allyAttack;
    private readonly BattleUpgradeSettings defenseLineHp;
    private int allyAttackLevel;
    private int defenseLineHpLevel;

    public BattleUpgradeController(
        BattleUpgradeSettings allyAttack,
        BattleUpgradeSettings defenseLineHp)
    {
        this.allyAttack = allyAttack;
        this.defenseLineHp = defenseLineHp;
    }

    public int GetLevel(EBattleUpgrade upgrade)
    {
        return upgrade switch
        {
            EBattleUpgrade.AllyAttack => allyAttackLevel,
            EBattleUpgrade.DefenseLineHp => defenseLineHpLevel,
            _ => 0
        };
    }

    public int GetMaxLevel(EBattleUpgrade upgrade)
    {
        return upgrade switch
        {
            EBattleUpgrade.AllyAttack => Mathf.Max(0, allyAttack.MaxLevel),
            EBattleUpgrade.DefenseLineHp => Mathf.Max(0, defenseLineHp.MaxLevel),
            _ => 0
        };
    }

    public bool IsMaxLevel(EBattleUpgrade upgrade)
    {
        return GetLevel(upgrade) >= GetMaxLevel(upgrade);
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
        BattleUpgradeSettings settings = GetSettings(upgrade);
        return settings.BaseEffect + settings.EffectPerLevel * GetLevel(upgrade);
    }

    public float GetNextEffect(EBattleUpgrade upgrade)
    {
        if (GetLevel(upgrade) >= GetMaxLevel(upgrade)) return GetEffect(upgrade);
        BattleUpgradeSettings settings = GetSettings(upgrade);
        return settings.BaseEffect +
               settings.EffectPerLevel * (GetLevel(upgrade) + 1);
    }

    public void ConfirmPurchase(EBattleUpgrade upgrade)
    {
        if (upgrade == EBattleUpgrade.AllyAttack)
        {
            allyAttackLevel++;
        }
        else if (upgrade == EBattleUpgrade.DefenseLineHp)
        {
            defenseLineHpLevel++;
        }
    }

    private BattleUpgradeSettings GetSettings(EBattleUpgrade upgrade)
    {
        return upgrade switch
        {
            EBattleUpgrade.AllyAttack => allyAttack,
            EBattleUpgrade.DefenseLineHp => defenseLineHp,
            _ => default
        };
    }
}
