#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class GameplayFeedbackSceneTests
{
    private const string ArcanePinballRoot =
        "Assets/03. Images/Pinball/Arcane/";
    private const string MoonlitWorkshopRoot =
        "Assets/03. Images/Pinball/MoonlitWorkshop/";

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
        AssertReference(gameOverPanel, "titleText");
        AssertReference(gameOverPanel, "messageText");
        AssertReference(gameOverPanel, "restartButton");
        AssertReference(gameOverPanel, "titleButton");
        AssertReference(gameOverPanel, "overlayImage");
        AssertReference(gameOverPanel, "titleImage");
        AssertReference(gameOverPanel, "iconImage");
        AssertReference(gameOverPanel, "buttonAccentImage");
        AssertReference(gameOverPanel, "victoryOverlaySprite");
        AssertReference(gameOverPanel, "defeatOverlaySprite");
        AssertReference(gameOverPanel, "victoryTitleSprite");
        AssertReference(gameOverPanel, "defeatTitleSprite");
        AssertReference(gameOverPanel, "victoryIconSprite");
        AssertReference(gameOverPanel, "defeatIconSprite");
        AssertReference(gameOverPanel, "victoryButtonAccentSprite");
        AssertReference(gameOverPanel, "defeatButtonAccentSprite");

        var wavePanel = Object.FindFirstObjectByType<WavePanel>(
            FindObjectsInactive.Include);
        AssertReference(wavePanel, "startButton");
        Assert.That(
            ReadReference<TextMeshProUGUI>(wavePanel, "launchCostText"),
            Is.Not.Null);

        var statusPanel = Object.FindFirstObjectByType<StatusPanel>(
            FindObjectsInactive.Include);
        AssertArrayReferences(statusPanel, "waveNodes", 10);
        AssertArrayReferences(statusPanel, "waveConnectors", 9);
        AssertReference(statusPanel, "idleNodeSprite");
        AssertReference(statusPanel, "lockedNodeSprite");
        AssertReference(statusPanel, "currentNodeSprite");
        AssertReference(statusPanel, "completeNodeSprite");
        AssertReference(statusPanel, "elite05NodeSprite");
        AssertReference(statusPanel, "elite09NodeSprite");
        AssertReference(statusPanel, "boss10NodeSprite");
        AssertReference(statusPanel, "idleConnectorSprite");
        AssertReference(statusPanel, "completeConnectorSprite");

        GameObject boardGlow = GameObject.Find("BoardGlow");
        Assert.That(boardGlow, Is.Not.Null);
        var boardRenderer = boardGlow.GetComponent<SpriteRenderer>();
        Assert.That(
            AssetDatabase.GetAssetPath(boardRenderer.sprite),
            Is.EqualTo(MoonlitWorkshopRoot + "board_base_mask.png"));
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

    [Test]
    public void GameScene_UsesMoonlitWorkshopBoardAndKeepsArcaneBall()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");

        GameObject board = GameObject.Find("ArcaneBoard");
        Assert.That(board, Is.Not.Null);

        var boardRenderers = board
            .GetComponentsInChildren<SpriteRenderer>(true)
            .Where(renderer => renderer.sprite != null)
            .ToArray();
        Assert.That(boardRenderers, Is.Not.Empty);
        AssertSpritePathsStartWith(
            boardRenderers.Select(renderer => renderer.sprite),
            MoonlitWorkshopRoot);

        GameObject ball = GameObject.Find("Ball");
        Assert.That(ball, Is.Not.Null);
        Assert.That(
            AssetDatabase.GetAssetPath(ball.GetComponent<SpriteRenderer>().sprite),
            Is.EqualTo(ArcanePinballRoot + "ball_arcane.png"));
    }

    [Test]
    public void VfxCatalog_UsesMoonlitBoardSpritesAndArcaneBallSprites()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ArcaneVfxCatalog>(
            "Assets/Resources/ArcaneVFX/ArcaneVfxCatalog.asset");
        Assert.That(catalog, Is.Not.Null);

        AssertSpritePathsStartWith(
            new[] { catalog.ballMask }
                .Concat(catalog.ballTrail)
                .Concat(catalog.ballImpact)
                .Concat(catalog.ballRing),
            ArcanePinballRoot);

        AssertSpritePathsStartWith(
            new[]
            {
                catalog.standardBumperMask,
                catalog.specialBumperMask,
                catalog.magnetMask,
                catalog.reflectorMask,
                catalog.guardianRuneMask,
                catalog.rangerRuneMask,
                catalog.mageRuneMask,
                catalog.lancerRuneMask
            }
            .Concat(catalog.magnetArc)
            .Concat(catalog.magnetSpark)
            .Concat(catalog.goalRing)
            .Concat(catalog.goalArcTopLeft)
            .Concat(catalog.goalArcTopRight)
            .Concat(catalog.goalArcBottomLeft)
            .Concat(catalog.goalArcBottomRight)
            .Concat(catalog.goalSpark),
            MoonlitWorkshopRoot);
    }

    [Test]
    public void PinballGoal_InitializesItsRingEffectsWithMoonlitSprites()
    {
        var goalObject = new GameObject("Goal VFX Test");
        goalObject.SetActive(false);
        var absorptionObject = new GameObject("Absorption Ring");
        var burstObject = new GameObject("Goal Burst");
        absorptionObject.transform.SetParent(goalObject.transform);
        burstObject.transform.SetParent(goalObject.transform);

        try
        {
            var goal = goalObject.AddComponent<PinballGoal>();
            var absorption = absorptionObject.AddComponent<ArcaneSpriteEffect>();
            var burst = burstObject.AddComponent<ArcaneSpriteEffect>();
            var serializedGoal = new SerializedObject(goal);
            serializedGoal.FindProperty("absorptionRing").objectReferenceValue = absorption;
            serializedGoal.FindProperty("goalBurst").objectReferenceValue = burst;
            serializedGoal.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo initializeVfx = typeof(PinballGoal).GetMethod(
                "InitializeVfx",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(initializeVfx, Is.Not.Null);
            initializeVfx.Invoke(goal, null);

            AssertEffectSpritePathsStartWith(absorption, MoonlitWorkshopRoot);
            AssertEffectSpritePathsStartWith(burst, MoonlitWorkshopRoot);
        }
        finally
        {
            Object.DestroyImmediate(goalObject);
        }
    }

    private static void AssertReference(Object target, string propertyName)
    {
        Assert.That(ReadReference<Object>(target, propertyName), Is.Not.Null);
    }

    private static void AssertArrayReferences(
        Object target,
        string propertyName,
        int expectedSize)
    {
        SerializedProperty property =
            new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        Assert.That(property.arraySize, Is.EqualTo(expectedSize), propertyName);
        for (var index = 0; index < property.arraySize; index++)
        {
            Assert.That(
                property.GetArrayElementAtIndex(index).objectReferenceValue,
                Is.Not.Null,
                $"{propertyName}[{index}]");
        }
    }

    private static T ReadReference<T>(Object target, string propertyName)
        where T : Object
    {
        var property = new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.objectReferenceValue as T;
    }

    private static void AssertSpritePathsStartWith(
        IEnumerable<Sprite> sprites,
        string expectedRoot)
    {
        string[] invalidPaths = sprites
            .Select(AssetDatabase.GetAssetPath)
            .Where(path => !path.StartsWith(expectedRoot))
            .ToArray();
        Assert.That(
            invalidPaths,
            Is.Empty,
            $"Expected sprite paths below {expectedRoot}: " +
            string.Join(", ", invalidPaths));
    }

    private static void AssertEffectSpritePathsStartWith(
        ArcaneSpriteEffect effect,
        string expectedRoot)
    {
        FieldInfo framesField = typeof(ArcaneSpriteEffect).GetField(
            "frames",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(framesField, Is.Not.Null);
        var frames = framesField.GetValue(effect) as Sprite[];
        Assert.That(frames, Is.Not.Null.And.Not.Empty);
        AssertSpritePathsStartWith(frames, expectedRoot);
    }
}
#endif
