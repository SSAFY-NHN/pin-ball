using System;
using System.Collections.Generic;

public readonly struct AllyProgressionResult
{
    public string UnitId { get; }
    public int Level { get; }
    public string UnlockedUnitId { get; }

    public AllyProgressionResult(
        string unitId,
        int level,
        string unlockedUnitId)
    {
        UnitId = unitId;
        Level = level;
        UnlockedUnitId = unlockedUnitId;
    }
}

public sealed class AllyProgressionController
{
    public const int MaximumLevel = 10;
    private const int BaseCost = 150;
    private const double CostMultiplier = 1.35d;

    private static readonly IReadOnlyDictionary<string, string[]> Unlocks =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["warrior"] = new[] { "knight", "berserker" },
            ["archer"] = new[] { "ranger", "marksman" },
            ["mage"] = new[] { "pyromancer", "frost" },
            ["spearman"] = new[] { "lancer", "guard" }
        };

    private readonly Dictionary<string, int> levels =
        new(StringComparer.Ordinal);

    public AllyProgressionController()
    {
        Reset();
    }

    public int GetLevel(string rootUnitId)
    {
        return rootUnitId != null && levels.TryGetValue(rootUnitId, out int level)
            ? level
            : 0;
    }

    public int GetNextCost(string rootUnitId)
    {
        int level = GetLevel(rootUnitId);
        if (level <= 0 || level >= MaximumLevel) return 0;

        double cost = BaseCost * Math.Pow(CostMultiplier, level - 1);
        return cost >= int.MaxValue ? int.MaxValue : (int)Math.Ceiling(cost);
    }

    public bool IsUnlocked(string unitId)
    {
        if (unitId == null) return false;
        if (levels.ContainsKey(unitId)) return true;

        foreach (var pair in Unlocks)
        {
            int level = levels[pair.Key];
            if (unitId == pair.Value[0]) return level >= 5;
            if (unitId == pair.Value[1]) return level >= MaximumLevel;
        }

        return false;
    }

    public bool CanLevelUp(string rootUnitId, bool isOwned, int gold)
    {
        int level = GetLevel(rootUnitId);
        return isOwned &&
               level > 0 &&
               level < MaximumLevel &&
               gold >= GetNextCost(rootUnitId);
    }

    public bool TryLevelUp(
        string rootUnitId,
        bool isOwned,
        int gold,
        out AllyProgressionResult result)
    {
        result = default;
        if (!CanLevelUp(rootUnitId, isOwned, gold)) return false;

        int level = ++levels[rootUnitId];
        string unlockedUnitId = level switch
        {
            5 => Unlocks[rootUnitId][0],
            MaximumLevel => Unlocks[rootUnitId][1],
            _ => null
        };
        result = new AllyProgressionResult(
            rootUnitId,
            level,
            unlockedUnitId);
        return true;
    }

    public void Reset()
    {
        levels.Clear();
        foreach (string rootUnitId in Unlocks.Keys)
        {
            levels[rootUnitId] = 1;
        }
    }
}
