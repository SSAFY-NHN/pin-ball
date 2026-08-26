#if UNITY_EDITOR
using System;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class WebGlFullscreenQualitySceneSetup
{
    private const string DeveloperScenePath =
        "Assets/01. Scenes/00. Developer.unity";
    private const string PipelineAssetPath =
        "Assets/Settings/UniversalRP.asset";

    [MenuItem("Tools/Pin-Ball/Configure WebGL Fullscreen Quality")]
    public static void Apply()
    {
        Scene scene = EditorSceneManager.OpenScene(DeveloperScenePath);
        App app = UnityEngine.Object.FindFirstObjectByType<App>();
        if (app == null)
        {
            throw new InvalidOperationException(
                "Developer scene must contain one App component.");
        }

        UniversalRenderPipelineAsset asset =
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                PipelineAssetPath);
        if (asset == null)
        {
            throw new InvalidOperationException(
                $"Missing URP asset: {PipelineAssetPath}");
        }

        bool changed = false;
        WebGlFullscreenQualityController controller =
            app.GetComponent<WebGlFullscreenQualityController>();
        if (controller == null)
        {
            controller = app.gameObject.AddComponent<
                WebGlFullscreenQualityController>();
            changed = true;
        }

        var serializedController = new SerializedObject(controller);
        SerializedProperty pipelineProperty = serializedController
            .FindProperty("pipelineAsset");
        if (pipelineProperty.objectReferenceValue != asset)
        {
            pipelineProperty.objectReferenceValue = asset;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            changed = true;
        }

        if (!changed) return;

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException(
                $"Failed to save scene: {DeveloperScenePath}");
        }
    }
}
#endif
