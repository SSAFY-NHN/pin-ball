using System;
using System.Collections.Generic;

using UnityEngine;

public sealed class AllyBasicAttackController
{
    private const float MageSplashRadius = 1.5f;
    private const float MageSplashDamageRatio = 0.6f;
    private const int MageSplashTargetCount = 2;
    private const float SpearmanArmorIgnoreRatio = 0.4f;

    private readonly List<UnitBase> targets = new();

    public float GetArmorIgnoreRatio(string unitId, UnitBase target)
    {
        return target != null && IsSpearmanFamily(unitId)
            ? SpearmanArmorIgnoreRatio
            : 0f;
    }

    public int ApplySecondaryHits(
        string unitId,
        UnitBase source,
        UnitBase primaryTarget,
        float basicAttackDamage,
        UnitTargetFinder targetFinder,
        Action<UnitBase> playEffect)
    {
        if (!IsMageFamily(unitId) || source == null || primaryTarget == null ||
            targetFinder == null || basicAttackDamage <= 0f)
        {
            return 0;
        }

        targetFinder.GetAliveEnemiesInRadius(
            primaryTarget.transform.position,
            MageSplashRadius,
            targets);
        targets.Remove(primaryTarget);
        targets.Sort((left, right) => Vector2.SqrMagnitude(
                left.transform.position - primaryTarget.transform.position)
            .CompareTo(Vector2.SqrMagnitude(
                right.transform.position - primaryTarget.transform.position)));

        int count = Mathf.Min(MageSplashTargetCount, targets.Count);
        for (int index = 0; index < count; index++)
        {
            UnitBase target = targets[index];
            target.TakeDamage(
                basicAttackDamage * MageSplashDamageRatio,
                0f,
                source);
            playEffect?.Invoke(target);
        }

        return count;
    }

    private static bool IsMageFamily(string unitId)
    {
        return unitId == "mage" || unitId == "pyromancer" || unitId == "frost";
    }

    private static bool IsSpearmanFamily(string unitId)
    {
        return unitId == "spearman" || unitId == "lancer" || unitId == "guard";
    }
}
