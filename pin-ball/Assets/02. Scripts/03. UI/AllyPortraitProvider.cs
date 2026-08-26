using System;
using System.Collections.Generic;

using UnityEngine;

public static class AllyPortraitProvider
{
    private const string ResourceRoot = "UI/Portraits/";

    private static readonly Dictionary<string, string> FileNames = new()
    {
        ["bear1"] = "ui_character_portrait_01_bear_swordsman",
        ["bear2"] = "ui_character_portrait_05_bear_knight",
        ["bear3"] = "ui_character_portrait_06_bear_berserker",
        ["bear4"] = "ui_character_portrait_05_bear_knight",
        ["bear5"] = "ui_character_portrait_06_bear_berserker",
        ["cat1"] = "ui_character_portrait_02_cat_archer",
        ["cat2"] = "ui_character_portrait_07_cat_ranger",
        ["cat3"] = "ui_character_portrait_08_cat_crossbow",
        ["cat4"] = "ui_character_portrait_07_cat_ranger",
        ["cat5"] = "ui_character_portrait_08_cat_crossbow",
        ["rabbit1"] = "ui_character_portrait_03_rabbit_mage",
        ["rabbit2"] = "ui_character_portrait_10_rabbit_ice_mage",
        ["rabbit3"] = "ui_character_portrait_09_rabbit_fire_mage",
        ["rabbit4"] = "ui_character_portrait_10_rabbit_ice_mage",
        ["rabbit5"] = "ui_character_portrait_09_rabbit_fire_mage",
        ["dog1"] = "ui_character_portrait_04_dog_lancer",
        ["dog2"] = "ui_character_portrait_12_dog_guardian",
        ["dog3"] = "ui_character_portrait_11_dog_lancer",
        ["dog4"] = "ui_character_portrait_12_dog_guardian",
        ["dog5"] = "ui_character_portrait_11_dog_lancer"
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
