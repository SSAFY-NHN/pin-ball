#if UNITY_EDITOR
using NUnit.Framework;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class WebGlFullscreenQualitySceneTests
{
    [Test]
    public void DeveloperScene_WiresQualityControllerOnPersistentApp()
    {
        EditorSceneManager.OpenScene(
            "Assets/01. Scenes/00. Developer.unity");

        App[] apps = Object.FindObjectsByType<App>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        WebGlFullscreenQualityController[] controllers =
            Object.FindObjectsByType<WebGlFullscreenQualityController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        Assert.That(apps, Has.Length.EqualTo(1));
        Assert.That(controllers, Has.Length.EqualTo(1));
        Assert.That(controllers[0].gameObject, Is.SameAs(apps[0].gameObject));

        SerializedProperty property = new SerializedObject(controllers[0])
            .FindProperty("pipelineAsset");
        Assert.That(property, Is.Not.Null);
        Assert.That(property.objectReferenceValue, Is.TypeOf<
            UniversalRenderPipelineAsset>());
        var asset = (UniversalRenderPipelineAsset)
            property.objectReferenceValue;
        Assert.That(asset.renderScale, Is.EqualTo(1f));
    }
}
#endif
