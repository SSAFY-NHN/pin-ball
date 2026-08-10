#if UNITY_EDITOR
using System.Reflection;

using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class WorldHealthBarControllerTests
{
    private static MethodInfo GetMethod(string name)
    {
        return typeof(WorldHealthBarController).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
    }

    [TestCase(-0.2f, 0f)]
    [TestCase(0.4f, 0.4f)]
    [TestCase(1.2f, 1f)]
    public void ClampRatio_ClampsToHealthRange(float value, float expected)
    {
        MethodInfo method = GetMethod("ClampRatio");
        Assert.That(method, Is.Not.Null);

        var result = (float)method.Invoke(null, new object[] { value });

        Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase(0.4f, 0.8f, 0.1f, 2f, 0.05f, 0.8f)]
    [TestCase(0.4f, 0.8f, 0f, 2f, 0.1f, 0.6f)]
    [TestCase(0.8f, 0.4f, 0f, 2f, 0.1f, 0.8f)]
    public void CalculateDelayedRatio_UsesDelayFollowAndHealRules(
        float current,
        float delayed,
        float remainingDelay,
        float speed,
        float deltaTime,
        float expected)
    {
        MethodInfo method = GetMethod("CalculateDelayedRatio");
        Assert.That(method, Is.Not.Null);

        var result = (float)method.Invoke(
            null,
            new object[] { current, delayed, remainingDelay, speed, deltaTime });

        Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase(
        "Assets/04. Prefabs/AllyUnit.prefab",
        "world_hp_damage_delay_ally",
        "world_hp_fill_ally",
        "world_hp_frame_ally")]
    [TestCase(
        "Assets/04. Prefabs/EnemyUnit.prefab",
        "world_hp_damage_delay_enemy",
        "world_hp_fill_enemy",
        "world_hp_frame_enemy")]
    public void UnitPrefab_HasConfiguredTeamWorldHealthBar(
        string prefabPath,
        string expectedDelay,
        string expectedFill,
        string expectedFrame)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Assert.That(prefab, Is.Not.Null);

        WorldHealthBarController controller =
            prefab.GetComponentInChildren<WorldHealthBarController>(true);
        Assert.That(controller, Is.Not.Null);

        var serializedController = new SerializedObject(controller);
        AssertReference(serializedController, "ownerUnit");
        AssertSprite(
            serializedController,
            "backgroundRenderer",
            "world_hp_background");
        AssertSprite(
            serializedController,
            "delayedFillRenderer",
            expectedDelay);
        AssertSprite(
            serializedController,
            "currentFillRenderer",
            expectedFill);
        AssertSprite(serializedController, "frameRenderer", expectedFrame);
    }

    private static void AssertReference(
        SerializedObject serializedObject,
        string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null);
        Assert.That(property.objectReferenceValue, Is.Not.Null);
    }

    private static void AssertSprite(
        SerializedObject serializedObject,
        string propertyName,
        string expectedName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null);

        var renderer = property.objectReferenceValue as SpriteRenderer;
        Assert.That(renderer, Is.Not.Null);
        Assert.That(renderer.sprite, Is.Not.Null);
        Assert.That(renderer.sprite.name, Does.StartWith(expectedName));
    }
}
#endif
