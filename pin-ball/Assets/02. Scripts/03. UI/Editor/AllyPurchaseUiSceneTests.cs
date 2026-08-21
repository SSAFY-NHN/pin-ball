#if UNITY_EDITOR
using System.Collections.Generic;

using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class AllyPurchaseUiSceneTests
{
    [Test]
    public void GameScene_WiresOnePurchaseCardForEachBaseAlly()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");

        AllyPurchaseDisplayController[] cards =
            Object.FindObjectsByType<AllyPurchaseDisplayController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        var unitIds = new HashSet<string>();

        Assert.That(cards, Has.Length.EqualTo(3));
        foreach (AllyPurchaseDisplayController card in cards)
        {
            Assert.That(card.gameObject.activeInHierarchy, Is.True);
            unitIds.Add(ReadString(card, "unitId"));
            Assert.That(ReadReference<Button>(card, "purchaseButton"), Is.Not.Null);
            Assert.That(ReadReference<TextMeshProUGUI>(card, "displayText"), Is.Not.Null);
        }

        Assert.That(unitIds, Is.EquivalentTo(new[]
        {
            "warrior",
            "archer",
            "mage"
        }));
    }

    [TestCase("전사", "근접 탱커", 30, 0, "전사\n근접 탱커\n0회 · 30G")]
    [TestCase("궁수", "원거리 단일", 49, 1, "궁수\n원거리 단일\n1회 · 49G")]
    [TestCase("마법사", "원거리 범위", 79, 2, "마법사\n원거리 범위\n2회 · 79G")]
    public void FormatDisplay_CombinesRoleCountAndCost(
        string unitName,
        string role,
        int cost,
        int purchaseCount,
        string expected)
    {
        Assert.That(
            AllyPurchaseDisplayController.FormatDisplay(
                unitName,
                role,
                cost,
                purchaseCount),
            Is.EqualTo(expected));
    }

    private static string ReadString(Object target, string propertyName)
    {
        SerializedProperty property =
            new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.stringValue;
    }

    private static T ReadReference<T>(Object target, string propertyName)
        where T : Object
    {
        SerializedProperty property =
            new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.objectReferenceValue as T;
    }
}
#endif
