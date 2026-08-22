#if UNITY_EDITOR
using System.Linq;

using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class UnitAttackEffectPlayerTests
{
    private const string AllyPrefabPath =
        "Assets/04. Prefabs/AllyUnit.prefab";
    private const string EnemyPrefabPath =
        "Assets/04. Prefabs/EnemyUnit.prefab";
    private const string GameScenePath =
        "Assets/01. Scenes/02. Game.unity";

    [Test]
    public void AllyPrefab_MapsAttackEffectsToMatchingCharacters()
    {
        var player = LoadEffectPlayer(AllyPrefabPath);
        var serializedPlayer = new SerializedObject(player);

        AssertPrefabName(
            serializedPlayer,
            "arrowEffectPrefab",
            "AllyArcherArrowEffect");
        AssertIds(serializedPlayer, "arrowUnitIds", "archer", "marksman");
        AssertPrefabName(
            serializedPlayer,
            "fireEffectPrefab",
            "AllyMageFireballEffect");
        AssertIds(serializedPlayer, "fireUnitIds", "mage", "pyromancer");
        AssertPrefabName(
            serializedPlayer,
            "muzzleFlashEffectPrefab",
            "Cat2PistolMuzzleFlashEffect");
        AssertIds(serializedPlayer, "muzzleFlashUnitIds", "ranger");
    }

    [Test]
    public void EnemyPrefab_MapsGoblinArcherToEnemyArrow()
    {
        var player = LoadEffectPlayer(EnemyPrefabPath);
        var serializedPlayer = new SerializedObject(player);

        AssertPrefabName(
            serializedPlayer,
            "arrowEffectPrefab",
            "EnemyArcherArrowEffect");
        AssertIds(serializedPlayer, "arrowUnitIds", "goblin_archer");
    }

    [TestCase(AllyPrefabPath)]
    [TestCase(EnemyPrefabPath)]
    public void UnitPrefab_ShadowOverlapsLowerBody(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null);

        var shadow = prefab.transform.Find("GroundShadow");
        Assert.That(shadow, Is.Not.Null);
        Assert.That(shadow.localPosition.y, Is.EqualTo(-0.1f).Within(0.001f));

        var shadowRenderer = shadow.GetComponent<SpriteRenderer>();
        Assert.That(shadowRenderer, Is.Not.Null);
        Assert.That(shadowRenderer.sortingOrder, Is.EqualTo(9));
        Assert.That(prefab.GetComponent<SpriteRenderer>().sortingOrder,
            Is.EqualTo(10));
    }

    private static UnitAttackEffectPlayer LoadEffectPlayer(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null);

        var player = prefab.GetComponent<UnitAttackEffectPlayer>();
        Assert.That(player, Is.Not.Null);
        return player;
    }

    private static void AssertPrefabName(
        SerializedObject serializedPlayer,
        string propertyName,
        string expectedName)
    {
        var property = serializedPlayer.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null);
        Assert.That(property.objectReferenceValue, Is.Not.Null);
        Assert.That(property.objectReferenceValue.name, Is.EqualTo(expectedName));
    }

    private static void AssertIds(
        SerializedObject serializedPlayer,
        string propertyName,
        params string[] expectedIds)
    {
        var property = serializedPlayer.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null);
        var ids = Enumerable.Range(0, property.arraySize)
            .Select(index => property.GetArrayElementAtIndex(index).stringValue)
            .ToArray();
        Assert.That(ids, Is.EqualTo(expectedIds));
    }
}
#endif
