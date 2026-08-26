#if UNITY_EDITOR
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class ProjectTmpFontTests
{
    private const string TargetFontPath =
        "Assets/07. Fonts/GmarketSansTTFLight SDF.asset";

    [TestCase("Assets/01. Scenes/01. Title.unity")]
    [TestCase("Assets/01. Scenes/02. Game.unity")]
    public void Scene_AllTmpTextsUseGmarketSans(string scenePath)
    {
        TMP_FontAsset expected = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            TargetFontPath);
        EditorSceneManager.OpenScene(scenePath);
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        Assert.That(expected, Is.Not.Null);
        Assert.That(texts, Is.Not.Empty);
        foreach (TextMeshProUGUI text in texts)
        {
            Assert.That(text.font, Is.EqualTo(expected), text.name);
            Assert.That(
                text.fontSharedMaterial,
                Is.EqualTo(expected.material),
                text.name);
        }
    }

    [Test]
    public void UiPrefab_AllTmpTextsUseGmarketSans()
    {
        TMP_FontAsset expected = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            TargetFontPath);
        GameObject root = PrefabUtility.LoadPrefabContents(
            "Assets/04. Prefabs/UI.prefab");
        try
        {
            TextMeshProUGUI[] texts =
                root.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.That(texts, Is.Not.Empty);
            foreach (TextMeshProUGUI text in texts)
            {
                Assert.That(text.font, Is.EqualTo(expected), text.name);
                Assert.That(
                    text.fontSharedMaterial,
                    Is.EqualTo(expected.material),
                    text.name);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
