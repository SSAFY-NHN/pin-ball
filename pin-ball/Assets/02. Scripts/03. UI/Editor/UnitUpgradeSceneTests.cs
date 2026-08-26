#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class UnitUpgradeSceneTests
{
    [Test]
    public void GameScene_HasPreparationUpgradePanelWithFourCards()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
        UnitUpgradePanel panel = Object.FindFirstObjectByType<UnitUpgradePanel>(
            FindObjectsInactive.Include);
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.IsDefaultPanel, Is.True);
        Assert.That(panel.IsManagedByStack, Is.False);

        UnitUpgradeCard[] cards = panel.GetComponentsInChildren<UnitUpgradeCard>(true);
        Assert.That(cards, Has.Length.EqualTo(4));
        Assert.That(cards, Has.Exactly(1).Matches<UnitUpgradeCard>(c => c.RootUnitId == "warrior"));
        Assert.That(cards, Has.Exactly(1).Matches<UnitUpgradeCard>(c => c.RootUnitId == "archer"));
        Assert.That(cards, Has.Exactly(1).Matches<UnitUpgradeCard>(c => c.RootUnitId == "mage"));
        Assert.That(cards, Has.Exactly(1).Matches<UnitUpgradeCard>(c => c.RootUnitId == "spearman"));
        foreach (UnitUpgradeCard card in cards)
        {
            var serialized = new SerializedObject(card);
            Assert.That(serialized.FindProperty("displayText").objectReferenceValue, Is.TypeOf<TextMeshProUGUI>());
            Assert.That(serialized.FindProperty("levelUpButton").objectReferenceValue, Is.TypeOf<Button>());
        }
    }

    [Test]
    public void GameScene_ComboDisplayUsesOneTextAndFilledGauge()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
        PinballComboDisplay display = Object.FindFirstObjectByType<PinballComboDisplay>(
            FindObjectsInactive.Include);
        Assert.That(display, Is.Not.Null);
        Assert.That(display.GetComponentsInChildren<TextMeshProUGUI>(true), Has.Length.EqualTo(1));
        var serialized = new SerializedObject(display);
        Assert.That(serialized.FindProperty("comboText").objectReferenceValue, Is.Not.Null);
        var fill = serialized.FindProperty("timeFillImage").objectReferenceValue as Image;
        Assert.That(fill, Is.Not.Null);
        Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
    }
}
#endif
