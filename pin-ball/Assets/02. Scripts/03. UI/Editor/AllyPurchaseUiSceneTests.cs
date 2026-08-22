#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class AllyPurchaseUiSceneTests
{
    [Test]
    public void GameScene_WiresPurchasePanelCardsAndReinforcementNotice()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");

        AllyPurchasePanelController[] panels =
            Object.FindObjectsByType<AllyPurchasePanelController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        Assert.That(panels, Has.Length.EqualTo(1));
        AllyPurchasePanelController panel = panels[0];
        Assert.That(panel.gameObject.activeInHierarchy, Is.True);
        AssertCardReferences(panel, "warrior");
        AssertCardReferences(panel, "archer");
        AssertCardReferences(panel, "mage");
        Assert.That(
            ReadReference<TextMeshProUGUI>(panel, "reinforcementNotice"),
            Is.Not.Null);
    }

    [TestCase("전사", "근접 탱커", 30, 1, false,
        "전사\n근접 탱커\n보유 1 · 30G")]
    [TestCase("궁수", "원거리 지속 공격", 49, 2, true,
        "궁수\n원거리 지속 공격\n보유 2 · 무료")]
    [TestCase("마법사", "원거리 범위 공격", 79, 0, false,
        "마법사\n원거리 범위 공격\n보유 0 · 79G")]
    public void FormatCard_CombinesRoleOwnedCountAndPurchaseState(
        string unitName,
        string role,
        int cost,
        int ownedCount,
        bool isFree,
        string expected)
    {
        Assert.That(
            AllyPurchasePanelController.FormatCard(
                unitName,
                role,
                cost,
                ownedCount,
                isFree),
            Is.EqualTo(expected));
    }

    [TestCase(false, "")]
    [TestCase(true, "다음 유닛 무료")]
    public void FormatReinforcementNotice_ShowsOnlyHeldTicket(
        bool hasTicket,
        string expected)
    {
        Assert.That(
            AllyPurchasePanelController.FormatReinforcementNotice(hasTicket),
            Is.EqualTo(expected));
    }

    private static void AssertCardReferences(
        AllyPurchasePanelController panel,
        string prefix)
    {
        Assert.That(
            ReadReference<Button>(panel, $"{prefix}PurchaseButton"),
            Is.Not.Null);
        Assert.That(
            ReadReference<TextMeshProUGUI>(panel, $"{prefix}DisplayText"),
            Is.Not.Null);
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
