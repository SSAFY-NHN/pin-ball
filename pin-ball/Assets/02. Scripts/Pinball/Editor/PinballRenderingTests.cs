#if UNITY_EDITOR
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PinballRenderingTests
{
    [Test]
    public void BallPrefabSortingOrder_IsGreaterThanGameCanvas()
    {
        var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/04. Prefabs/Ball.prefab");
        Assert.That(ballPrefab, Is.Not.Null);

        var renderer = ballPrefab.GetComponent<SpriteRenderer>();
        Assert.That(renderer, Is.Not.Null);

        var gameScene = EditorSceneManager.OpenScene(
            "Assets/01. Scenes/02. Game.unity", OpenSceneMode.Additive);

        try
        {
            var highestCanvasOrder = gameScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
                .Max(canvas => canvas.sortingOrder);

            Assert.That(renderer.sortingOrder, Is.GreaterThan(highestCanvasOrder));
        }
        finally
        {
            EditorSceneManager.CloseScene(gameScene, true);
        }
    }
}
#endif
