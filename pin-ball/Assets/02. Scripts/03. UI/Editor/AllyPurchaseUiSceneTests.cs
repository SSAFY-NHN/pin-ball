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
        AssertCardReferences(panel, "spearman");
        Assert.That(
            ReadReference<TextMeshProUGUI>(panel, "reinforcementNotice"),
            Is.Not.Null);

        var battleManager = Object.FindFirstObjectByType<BattleManager>(
            FindObjectsInactive.Include);
        AssertPurchaseSettings(battleManager, "warriorPurchaseSettings", 4f);
        AssertPurchaseSettings(battleManager, "archerPurchaseSettings", 5f);
        AssertPurchaseSettings(battleManager, "magePurchaseSettings", 7f);
        AssertPurchaseSettings(battleManager, "spearmanPurchaseSettings", 5f);
    }

    [TestCase("전사", "돌진 저지 · 전열 방어", 30, 1, false,
        "전사\n돌진 저지 · 전열 방어\n보유 1 · 30G")]
    [TestCase("궁수", "장거리 · 단일 지속 피해", 49, 2, true,
        "궁수\n장거리 · 단일 지속 피해\n보유 2 · 무료")]
    [TestCase("마법사", "원거리 · 범위 피해", 79, 0, false,
        "마법사\n원거리 · 범위 피해\n보유 0 · 79G")]
    [TestCase("창병", "중거리 · 방어 관통", 35, 0, false,
        "창병\n중거리 · 방어 관통\n보유 0 · 35G")]
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

    [TestCase(0f, "")]
    [TestCase(0.01f, "1")]
    [TestCase(3f, "3")]
    [TestCase(3.01f, "4")]
    public void FormatCooldown_CeilsPositiveRemainingSeconds(
        float remaining,
        string expected)
    {
        Assert.That(
            AllyPurchasePanelController.FormatCooldown(remaining),
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
        Image portrait = ReadReference<Image>(panel, $"{prefix}PortraitImage");
        Assert.That(portrait, Is.Not.Null);
        Assert.That(portrait.sprite, Is.Not.Null);
        Assert.That(
            ReadReference<Image>(panel, $"{prefix}CooldownMask"),
            Is.Not.Null);
        Assert.That(
            ReadReference<TextMeshProUGUI>(panel, $"{prefix}CooldownText"),
            Is.Not.Null);
    }

    private static void AssertPurchaseSettings(
        BattleManager manager,
        string propertyName,
        float expectedCooldown)
    {
        SerializedProperty property =
            new SerializedObject(manager).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        Assert.That(
            property.FindPropertyRelative("CooldownSeconds").floatValue,
            Is.EqualTo(expectedCooldown));
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
