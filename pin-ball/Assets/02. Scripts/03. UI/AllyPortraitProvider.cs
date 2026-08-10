using System.Collections.Generic;

using UnityEngine;

public static class AllyPortraitProvider
{
    private const string ResourceRoot = "UI/Portraits/";

    private static readonly Dictionary<string, string> FileNames = new()
    {
        ["warrior"] = "ui_character_portrait_01_bear_swordsman",
        ["archer"] = "ui_character_portrait_02_cat_archer",
        ["mage"] = "ui_character_portrait_03_rabbit_mage",
        ["spearman"] = "ui_character_portrait_04_dog_lancer",
        ["knight"] = "ui_character_portrait_05_bear_knight",
        ["berserker"] = "ui_character_portrait_06_bear_berserker",
        ["ranger"] = "ui_character_portrait_07_cat_ranger",
        ["marksman"] = "ui_character_portrait_08_cat_crossbow",
        ["pyromancer"] = "ui_character_portrait_09_rabbit_fire_mage",
        ["frost"] = "ui_character_portrait_10_rabbit_ice_mage",
        ["lancer"] = "ui_character_portrait_11_dog_lancer",
        ["guard"] = "ui_character_portrait_12_dog_guardian"
    };

    private static readonly Dictionary<string, Sprite> Cache = new();

    public static string GetResourcePath(string unitId)
    {
        return !string.IsNullOrEmpty(unitId) &&
               FileNames.TryGetValue(unitId, out var fileName)
            ? ResourceRoot + fileName
            : null;
    }

    public static Sprite Load(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return null;
        if (Cache.TryGetValue(unitId, out var cached)) return cached;

        string path = GetResourcePath(unitId);
        if (path == null) return null;

        var sprites = Resources.LoadAll<Sprite>(path);
        Sprite portrait = sprites.Length > 0
            ? sprites[0]
            : Resources.Load<Sprite>(path);
        Cache[unitId] = portrait;
        return portrait;
    }
}
