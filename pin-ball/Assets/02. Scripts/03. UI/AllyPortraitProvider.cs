using System;
using System.Collections.Generic;

using UnityEngine;

public static class AllyPortraitProvider
{
    private const string ResourceRoot = "UI/Portraits/";

    private static readonly Dictionary<string, string> FileNames = new()
    {
        ["bear1"] = "ui_character_portrait_bear1_fighter",
        ["bear2"] = "ui_character_portrait_bear2_fighter",
        ["bear3"] = "ui_character_portrait_bear3_ninja_assassin",
        ["bear4"] = "ui_character_portrait_bear4_evo_fighter",
        ["bear5"] = "ui_character_portrait_bear5_evo_ninja_assassin",
        ["dog1"] = "ui_character_portrait_dog1_farmer_sword",
        ["dog2"] = "ui_character_portrait_dog2_warrior",
        ["dog3"] = "ui_character_portrait_dog3_lancer",
        ["dog4"] = "ui_character_portrait_dog4_evo_warrior",
        ["dog5"] = "ui_character_portrait_dog5_evo_lancer",
        ["cat1"] = "ui_character_portrait_cat1_archer",
        ["cat2"] = "ui_character_portrait_cat2_gunslinger",
        ["cat3"] = "ui_character_portrait_cat3_crossbow",
        ["cat4"] = "ui_character_portrait_cat4_evo_gunslinger",
        ["cat5"] = "ui_character_portrait_cat5_evo_bow",
        ["rabbit1"] = "ui_character_portrait_rabbit1_mage",
        ["rabbit2"] = "ui_character_portrait_rabbit2_healer",
        ["rabbit3"] = "ui_character_portrait_rabbit3_evo_mage",
        ["rabbit4"] = "ui_character_portrait_rabbit4_evo_healer",
        ["rabbit5"] = "ui_character_portrait_rabbit5_evo_mage2"
    };

    private static readonly Dictionary<string, Sprite> Cache = new();

    public static string GetResourcePath(string unitId)
    {
        string normalizedId = unitId?.Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(normalizedId) &&
               FileNames.TryGetValue(normalizedId, out var fileName)
            ? ResourceRoot + fileName
            : null;
    }

    public static Sprite Load(string unitId)
    {
        string normalizedId = unitId?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalizedId)) return null;
        if (Cache.TryGetValue(normalizedId, out var cached)) return cached;

        string path = GetResourcePath(normalizedId);
        if (path == null) return null;

        var sprites = Resources.LoadAll<Sprite>(path);
        Sprite portrait = sprites.Length > 0
            ? sprites[0]
            : Resources.Load<Sprite>(path);

        if (portrait == null)
        {
            string fileName = FileNames[normalizedId];
            var allPortraits = Resources.LoadAll<Sprite>(ResourceRoot.TrimEnd('/'));
            portrait = Array.Find(allPortraits, sprite =>
                sprite.name.StartsWith(fileName, StringComparison.OrdinalIgnoreCase));
        }

        if (portrait == null)
        {
            Debug.LogWarning($"[AllyPortraitProvider] 프로필 이미지를 찾지 못했습니다: {normalizedId} ({path})");
            return null;
        }

        Cache[normalizedId] = portrait;
        return portrait;
    }
}
