#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class GameplayFeedbackSceneTests
{
    [Test]
    public void GameScene_WiresResultCostAndInteractionGlow()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");

        var resultPanel = Object.FindFirstObjectByType<WaveResultPanel>(
            FindObjectsInactive.Include);
        Assert.That(resultPanel, Is.Not.Null);
        AssertReference(resultPanel, "panelRect");
        AssertReference(resultPanel, "canvasGroup");
        AssertReference(resultPanel, "resultText");

        var gameOverPanel = Object.FindFirstObjectByType<ResultPanel>(
            FindObjectsInactive.Include);
        Assert.That(gameOverPanel, Is.Not.Null);
        AssertReference(gameOverPanel, "messageText");
        AssertReference(gameOverPanel, "restartButton");
        AssertReference(gameOverPanel, "titleButton");

        var wavePanel = Object.FindFirstObjectByType<WavePanel>(
            FindObjectsInactive.Include);
        Assert.That(
            ReadReference<TextMeshProUGUI>(wavePanel, "launchCostText"),
            Is.Not.Null);

        GameObject boardGlow = GameObject.Find("BoardGlow");
        Assert.That(boardGlow, Is.Not.Null);
        var boardRenderer = boardGlow.GetComponent<SpriteRenderer>();
        Assert.That(
            AssetDatabase.GetAssetPath(boardRenderer.sprite),
            Is.EqualTo(
                "Assets/03. Images/Pinball/Arcane/" +
                "pinball_board_arcane_mask.png"));
        Assert.That(
            AssetDatabase.GetAssetPath(boardRenderer.sharedMaterial),
            Is.EqualTo(
                "Assets/09. Materials/Pinball/ArcaneDeviceAdditive.mat"));

        GameObject lever = GameObject.Find("PlungerLever");
        Assert.That(lever, Is.Not.Null);
        Assert.That(
            lever.GetComponent<PinballLauncherGlowController>(),
            Is.Not.Null);
        AssertReference(
            lever.GetComponent<PinballLauncherController>(),
            "glowController");
        Assert.That(GameObject.Find("PlungerLeverGlow"), Is.Not.Null);
        Assert.That(lever.GetComponentInChildren<TMP_Text>(true), Is.Null);
    }

    private static void AssertReference(Object target, string propertyName)
    {
        Assert.That(ReadReference<Object>(target, propertyName), Is.Not.Null);
    }

    private static T ReadReference<T>(Object target, string propertyName)
        where T : Object
    {
        var property = new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.objectReferenceValue as T;
    }
}
#endif
