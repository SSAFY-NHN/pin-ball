#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class BaseSkillSceneTests
{
    [Test]
    public void GameScene_WiresOneActiveBaseSkillPanel()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
        BaseSkillPanel[] panels = Object.FindObjectsByType<BaseSkillPanel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Assert.That(panels, Has.Length.EqualTo(1));
        BaseSkillPanel panel = panels[0];
        Assert.That(panel.gameObject.name, Is.EqualTo("BaseSkillPanel"));
        Assert.That(panel.gameObject.activeInHierarchy, Is.True);
        Button button = ReadReference<Button>(panel, "useButton");
        TextMeshProUGUI nameText =
            ReadReference<TextMeshProUGUI>(panel, "skillNameText");
        TextMeshProUGUI statusText =
            ReadReference<TextMeshProUGUI>(panel, "statusText");
        Assert.That(button, Is.Not.Null);
        Assert.That(nameText, Is.Not.Null);
        Assert.That(statusText, Is.Not.Null);
        Assert.That(nameText.text, Is.EqualTo("전선 밀어내기"));
        Assert.That(statusText.text, Is.EqualTo("대기"));
        Assert.That(button.onClick.GetPersistentEventCount(), Is.Zero);
    }

    [Test]
    public void GameScene_HasNoMissingScripts()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");

        foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!gameObject.scene.IsValid()) continue;
            Component[] components = gameObject.GetComponents<Component>();
            Assert.That(
                components,
                Has.None.Null,
                $"Missing Script: {GetPath(gameObject.transform)}");
        }
    }

    private static T ReadReference<T>(Object target, string propertyName)
        where T : Object
    {
        SerializedProperty property =
            new SerializedObject(target).FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, propertyName);
        return property.objectReferenceValue as T;
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = $"{target.name}/{path}";
        }

        return path;
    }
}
#endif
