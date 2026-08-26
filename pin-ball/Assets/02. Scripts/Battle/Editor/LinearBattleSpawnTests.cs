#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class LinearBattleSpawnTests
{
    [Test]
    public void GameScene_SpawnerUsesDefenseLines()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
        UnitSpawner spawner = Object.FindFirstObjectByType<UnitSpawner>();
        UnitManager manager = Object.FindFirstObjectByType<UnitManager>();
        var spawnerData = new SerializedObject(spawner);
        var managerData = new SerializedObject(manager);
        Transform allySpawn = spawnerData.FindProperty("allySpawnPoint")
            .objectReferenceValue as Transform;
        Transform enemySpawn = spawnerData.FindProperty("enemySpawnPoint")
            .objectReferenceValue as Transform;
        var allyLine = managerData.FindProperty("allyDefenseLine")
            .objectReferenceValue as DefenseLineTrigger;
        var enemyLine = managerData.FindProperty("enemyDefenseLine")
            .objectReferenceValue as DefenseLineTrigger;

        Assert.That(allySpawn, Is.EqualTo(allyLine.transform));
        Assert.That(enemySpawn, Is.EqualTo(enemyLine.transform));

    }

    [Test]
    public void UnitPrefabs_RenderAboveDefenseLines()
    {
        EditorSceneManager.OpenScene("Assets/01. Scenes/02. Game.unity");
        UnitSpawner spawner = Object.FindFirstObjectByType<UnitSpawner>();
        UnitManager manager = Object.FindFirstObjectByType<UnitManager>();
        var spawnerData = new SerializedObject(spawner);
        var managerData = new SerializedObject(manager);
        GameObject allyPrefab = spawnerData.FindProperty("allyPrefab")
            .objectReferenceValue as GameObject;
        GameObject enemyPrefab = spawnerData.FindProperty("enemyPrefab")
            .objectReferenceValue as GameObject;
        var allyLine = managerData.FindProperty("allyDefenseLine")
            .objectReferenceValue as DefenseLineTrigger;
        int defenseOrder = allyLine.GetComponent<SpriteRenderer>().sortingOrder;
        GameObject ally = Object.Instantiate(allyPrefab);
        GameObject enemy = Object.Instantiate(enemyPrefab);
        try
        {
            Assert.That(
                ally.GetComponent<SpriteRenderer>().sortingOrder,
                Is.GreaterThan(defenseOrder));
            Assert.That(
                enemy.GetComponent<SpriteRenderer>().sortingOrder,
                Is.GreaterThan(defenseOrder));
        }
        finally
        {
            Object.DestroyImmediate(ally);
            Object.DestroyImmediate(enemy);
        }
    }

    [Test]
    public void ResolveSpawnPosition_UsesExactTeamSpawnAndSharedBattleLine()
    {
        Vector3 ally = UnitSpawner.ResolveSpawnPosition(
            EBattleTeam.Ally,
            new Vector3(-6f, 2f, 0f),
            null,
            0f);
        Vector3 enemy = UnitSpawner.ResolveSpawnPosition(
            EBattleTeam.Enemy,
            new Vector3(6f, -4f, 0f),
            new Vector3(1f, 9f, 0f),
            0f);

        Assert.That(ally, Is.EqualTo(new Vector3(-6f, 0f, 0f)));
        Assert.That(enemy, Is.EqualTo(new Vector3(6f, 0f, 0f)));
    }
}
#endif
